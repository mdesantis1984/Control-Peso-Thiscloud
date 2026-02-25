#!/bin/bash
###############################################################################
# Script: restore-backup.sh
# Purpose: Restore SQL Server database from backup
# Usage: ./scripts/restore-backup.sh [backup_file]
# Example: ./scripts/restore-backup.sh ControlPeso_20260225_020000.bak
###############################################################################

set -e

# Configuration
DATABASE="ControlPeso"
BACKUP_DIR="/var/opt/mssql/backups"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${GREEN}=========================================${NC}"
echo -e "${GREEN}SQL Server Database Restore${NC}"
echo -e "${GREEN}=========================================${NC}"
echo ""

# Check if backup file argument provided
if [ -z "$1" ]; then
    echo -e "${YELLOW}Available backups:${NC}"
    docker exec controlpeso-sqlserver ls -lh "$BACKUP_DIR" | grep "\.bak$" || echo "No backup files found"
    echo ""
    echo -e "${RED}Usage: $0 <backup_filename>${NC}"
    echo -e "Example: $0 ControlPeso_20260225_020000.bak"
    exit 1
fi

BACKUP_FILE="$BACKUP_DIR/$1"

# Check if Docker is running
if ! docker ps &> /dev/null; then
    echo -e "${RED}Error: Docker is not running or not accessible${NC}"
    exit 1
fi

# Check if SQL Server container is running
if ! docker ps --filter "name=controlpeso-sqlserver" --format "{{.Names}}" | grep -q "controlpeso-sqlserver"; then
    echo -e "${RED}Error: controlpeso-sqlserver container is not running${NC}"
    exit 1
fi

# Check if backup file exists
if ! docker exec controlpeso-sqlserver test -f "$BACKUP_FILE"; then
    echo -e "${RED}Error: Backup file not found: $BACKUP_FILE${NC}"
    echo ""
    echo -e "${YELLOW}Available backups:${NC}"
    docker exec controlpeso-sqlserver ls -lh "$BACKUP_DIR" | grep "\.bak$"
    exit 1
fi

echo -e "${YELLOW}⚠️  WARNING: This will REPLACE the current database with the backup${NC}"
echo -e "Backup file: ${GREEN}$1${NC}"
echo -e "Database: ${GREEN}$DATABASE${NC}"
echo ""
read -p "Are you sure you want to continue? (yes/no): " confirm

if [ "$confirm" != "yes" ]; then
    echo -e "${YELLOW}Restore cancelled${NC}"
    exit 0
fi

echo ""
echo -e "${YELLOW}Step 1: Setting database to SINGLE_USER mode...${NC}"
docker exec controlpeso-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "ALTER DATABASE [$DATABASE] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;"

echo -e "${YELLOW}Step 2: Restoring database from backup...${NC}"
docker exec controlpeso-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "
RESTORE DATABASE [$DATABASE]
FROM DISK = N'$BACKUP_FILE'
WITH
    FILE = 1,
    NOUNLOAD,
    REPLACE,
    STATS = 10,
    CHECKSUM;
"

if [ $? -eq 0 ]; then
    echo -e "${YELLOW}Step 3: Setting database back to MULTI_USER mode...${NC}"
    docker exec controlpeso-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "ALTER DATABASE [$DATABASE] SET MULTI_USER;"
    
    echo ""
    echo -e "${GREEN}=========================================${NC}"
    echo -e "${GREEN}✓ Database restored successfully${NC}"
    echo -e "${GREEN}=========================================${NC}"
    echo ""
    echo -e "${YELLOW}Next steps:${NC}"
    echo "1. Restart the web application:"
    echo "   docker-compose -f docker-compose.production.yml restart controlpeso-web"
    echo ""
    echo "2. Verify the restore:"
    echo "   docker exec controlpeso-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P \$MSSQL_SA_PASSWORD -C -Q \"SELECT COUNT(*) FROM [$DATABASE].dbo.Users\""
else
    echo ""
    echo -e "${RED}=========================================${NC}"
    echo -e "${RED}❌ Restore failed${NC}"
    echo -e "${RED}=========================================${NC}"
    
    # Try to set back to MULTI_USER on failure
    echo -e "${YELLOW}Attempting to recover database state...${NC}"
    docker exec controlpeso-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "ALTER DATABASE [$DATABASE] SET MULTI_USER;" || true
    
    exit 1
fi
