#!/bin/bash
###############################################################################
# Script: backup-sqlserver.sh
# Purpose: Automatic SQL Server backup with rotation (30 days retention)
# Usage: Run inside controlpeso-sqlserver container via cron
# Location: /opt/scripts/backup-sqlserver.sh
###############################################################################

set -euo pipefail  # Exit on error, undefined var, pipe failure

# Configuration
DATABASE="ControlPeso"
BACKUP_DIR="/var/opt/mssql/backups"
RETENTION_DAYS=30
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
BACKUP_FILE="${BACKUP_DIR}/${DATABASE}_${TIMESTAMP}.bak"
LOG_FILE="${BACKUP_DIR}/backup.log"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Logging function
log() {
    local level=$1
    shift
    local message="$@"
    local timestamp=$(date +"%Y-%m-%d %H:%M:%S")
    
    echo "[${timestamp}] [${level}] ${message}" | tee -a "$LOG_FILE"
    
    # Send to syslog if available
    if command -v logger &> /dev/null; then
        logger -t "sqlserver-backup" "[${level}] ${message}"
    fi
}

# Check if SQL Server is running
check_sqlserver() {
    if ! /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "SELECT 1" &> /dev/null; then
        log "ERROR" "SQL Server is not responding"
        exit 1
    fi
    log "INFO" "SQL Server health check passed"
}

# Create backup
create_backup() {
    log "INFO" "Starting backup of database: $DATABASE"
    
    # Check disk space before backup
    local available_space=$(df -BG "$BACKUP_DIR" | awk 'NR==2 {print $4}' | sed 's/G//')
    if [ "$available_space" -lt 2 ]; then
        log "WARNING" "Low disk space: ${available_space}GB available in $BACKUP_DIR"
    fi
    
    # Create backup using T-SQL BACKUP DATABASE
    local sql_command="BACKUP DATABASE [$DATABASE] 
    TO DISK = N'$BACKUP_FILE' 
    WITH 
        FORMAT,
        INIT,
        NAME = N'$DATABASE-Full Database Backup',
        SKIP,
        NOREWIND,
        NOUNLOAD,
        COMPRESSION,
        STATS = 10,
        CHECKSUM;"
    
    if /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "$sql_command" &>> "$LOG_FILE"; then
        local backup_size=$(du -h "$BACKUP_FILE" | cut -f1)
        log "INFO" "Backup completed successfully: $BACKUP_FILE ($backup_size)"
        
        # Verify backup integrity
        verify_backup
        
        return 0
    else
        log "ERROR" "Backup failed for database: $DATABASE"
        return 1
    fi
}

# Verify backup integrity
verify_backup() {
    log "INFO" "Verifying backup integrity..."
    
    local sql_verify="RESTORE VERIFYONLY 
    FROM DISK = N'$BACKUP_FILE' 
    WITH CHECKSUM;"
    
    if /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "$sql_verify" &>> "$LOG_FILE"; then
        log "INFO" "Backup verification passed"
        return 0
    else
        log "ERROR" "Backup verification failed"
        return 1
    fi
}

# Rotate old backups (delete backups older than RETENTION_DAYS)
rotate_backups() {
    log "INFO" "Rotating backups (retention: $RETENTION_DAYS days)"
    
    local deleted_count=0
    
    # Find and delete backups older than RETENTION_DAYS
    while IFS= read -r old_backup; do
        if [ -n "$old_backup" ]; then
            local backup_size=$(du -h "$old_backup" | cut -f1)
            rm -f "$old_backup"
            log "INFO" "Deleted old backup: $(basename "$old_backup") ($backup_size)"
            ((deleted_count++))
        fi
    done < <(find "$BACKUP_DIR" -name "${DATABASE}_*.bak" -type f -mtime +$RETENTION_DAYS 2>/dev/null || true)
    
    if [ $deleted_count -eq 0 ]; then
        log "INFO" "No old backups to delete"
    else
        log "INFO" "Deleted $deleted_count old backup(s)"
    fi
}

# Report backup statistics
report_statistics() {
    local backup_count=$(find "$BACKUP_DIR" -name "${DATABASE}_*.bak" -type f | wc -l)
    local total_size=$(du -sh "$BACKUP_DIR" | cut -f1)
    local oldest_backup=$(find "$BACKUP_DIR" -name "${DATABASE}_*.bak" -type f -printf '%T+ %p\n' 2>/dev/null | sort | head -n1 | awk '{print $2}')
    local newest_backup=$(find "$BACKUP_DIR" -name "${DATABASE}_*.bak" -type f -printf '%T+ %p\n' 2>/dev/null | sort | tail -n1 | awk '{print $2}')
    
    log "INFO" "Backup statistics:"
    log "INFO" "  - Total backups: $backup_count"
    log "INFO" "  - Total size: $total_size"
    if [ -n "$oldest_backup" ]; then
        log "INFO" "  - Oldest: $(basename "$oldest_backup")"
    fi
    if [ -n "$newest_backup" ]; then
        log "INFO" "  - Newest: $(basename "$newest_backup")"
    fi
}

# Main execution
main() {
    log "INFO" "========================================="
    log "INFO" "SQL Server Backup Script Started"
    log "INFO" "========================================="
    
    # Create backup directory if it doesn't exist
    mkdir -p "$BACKUP_DIR"
    
    # Check SQL Server health
    check_sqlserver
    
    # Create backup
    if create_backup; then
        # Rotate old backups
        rotate_backups
        
        # Report statistics
        report_statistics
        
        log "INFO" "========================================="
        log "INFO" "Backup process completed successfully"
        log "INFO" "========================================="
        exit 0
    else
        log "ERROR" "========================================="
        log "ERROR" "Backup process failed"
        log "ERROR" "========================================="
        exit 1
    fi
}

# Run main function
main
