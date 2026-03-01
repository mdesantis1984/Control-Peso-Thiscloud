#!/bin/bash
###############################################################################
# Script: backup-now.sh
# Purpose: Run SQL Server backup on-demand (outside cron schedule)
# Usage: ./scripts/backup-now.sh
###############################################################################

set -e

echo "=========================================="
echo "On-Demand SQL Server Backup"
echo "=========================================="
echo ""

# Check if Docker is running
if ! docker ps &> /dev/null; then
    echo "❌ Error: Docker is not running or not accessible"
    exit 1
fi

# Check if SQL Server container is running
if ! docker ps --filter "name=controlpeso-sqlserver" --format "{{.Names}}" | grep -q "controlpeso-sqlserver"; then
    echo "❌ Error: controlpeso-sqlserver container is not running"
    echo "Start it with: docker-compose -f docker-compose.production.yml up -d controlpeso-sqlserver"
    exit 1
fi

echo "✓ SQL Server container is running"
echo ""

# Execute backup script inside container
echo "Running backup..."
docker exec controlpeso-sqlserver /opt/scripts/backup-sqlserver.sh

# Check exit code
if [ $? -eq 0 ]; then
    echo ""
    echo "=========================================="
    echo "✓ Backup completed successfully"
    echo "=========================================="
    echo ""
    echo "View backup files:"
    echo "  docker exec controlpeso-sqlserver ls -lh /var/opt/mssql/backups"
    echo ""
    echo "View backup logs:"
    echo "  docker exec controlpeso-sqlserver tail -n 50 /var/opt/mssql/backups/backup.log"
else
    echo ""
    echo "=========================================="
    echo "❌ Backup failed"
    echo "=========================================="
    echo ""
    echo "Check logs:"
    echo "  docker exec controlpeso-sqlserver cat /var/opt/mssql/backups/backup.log"
    exit 1
fi
