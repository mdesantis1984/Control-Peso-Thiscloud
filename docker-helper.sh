#!/bin/bash
# =============================================================================
# Control Peso Thiscloud - Docker Helper Script
# =============================================================================

set -e  # Exit on error

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Functions
print_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if Docker is running
check_docker() {
    if ! docker info > /dev/null 2>&1; then
        print_error "Docker is not running. Please start Docker Desktop."
        exit 1
    fi
}

# Check if .env file exists
check_env_file() {
    if [ ! -f ".env" ]; then
        print_warning ".env file not found. Creating from .env.example..."
        cp .env.example .env
        print_info "Please edit .env with your OAuth credentials:"
        print_info "  - GOOGLE_CLIENT_ID"
        print_info "  - GOOGLE_CLIENT_SECRET"
        print_info "  - LINKEDIN_CLIENT_ID"
        print_info "  - LINKEDIN_CLIENT_SECRET"
        exit 1
    fi
}

# Main menu
show_menu() {
    echo ""
    echo "========================================"
    echo " Control Peso Thiscloud - Docker Helper"
    echo "========================================"
    echo "1. Build & Start (first time)"
    echo "2. Start services"
    echo "3. Stop services"
    echo "4. Restart services"
    echo "5. View logs"
    echo "6. View real-time logs"
    echo "7. Status"
    echo "8. Backup database"
    echo "9. Restore database"
    echo "10. Clean all (⚠️ DELETES DATA)"
    echo "11. Shell access"
    echo "0. Exit"
    echo "========================================"
    echo -n "Select option: "
}

# Build and start
build_start() {
    print_info "Building image and starting services..."
    docker-compose up -d --build
    print_success "Services started!"
    print_info "Access application at: http://localhost:8080"
}

# Start services
start() {
    print_info "Starting services..."
    docker-compose up -d
    print_success "Services started!"
}

# Stop services
stop() {
    print_info "Stopping services..."
    docker-compose down
    print_success "Services stopped!"
}

# Restart services
restart() {
    print_info "Restarting services..."
    docker-compose restart
    print_success "Services restarted!"
}

# View logs
logs() {
    print_info "Showing last 100 lines of logs..."
    docker-compose logs --tail=100 controlpeso-web
}

# View real-time logs
logs_follow() {
    print_info "Following logs (Ctrl+C to exit)..."
    docker-compose logs -f controlpeso-web
}

# Status
status() {
    print_info "Service status:"
    docker-compose ps
    echo ""
    print_info "Volume status:"
    docker volume ls | grep controlpeso
}

# Backup database
backup_db() {
    BACKUP_FILE="backup-controlpeso-$(date +%Y%m%d-%H%M%S).db"
    print_info "Creating database backup: $BACKUP_FILE"
    docker cp controlpeso-web:/app/data/controlpeso.db "./$BACKUP_FILE"
    print_success "Backup created: $BACKUP_FILE"
}

# Restore database
restore_db() {
    echo -n "Enter backup file path: "
    read BACKUP_FILE
    
    if [ ! -f "$BACKUP_FILE" ]; then
        print_error "Backup file not found: $BACKUP_FILE"
        return
    fi
    
    print_warning "This will overwrite the current database!"
    echo -n "Are you sure? (yes/no): "
    read CONFIRM
    
    if [ "$CONFIRM" != "yes" ]; then
        print_info "Restore cancelled."
        return
    fi
    
    print_info "Restoring database from: $BACKUP_FILE"
    docker cp "$BACKUP_FILE" controlpeso-web:/app/data/controlpeso.db
    print_success "Database restored! Restarting services..."
    docker-compose restart
}

# Clean all
clean_all() {
    print_warning "⚠️  This will DELETE ALL DATA (containers, images, volumes)!"
    echo -n "Are you sure? (yes/no): "
    read CONFIRM
    
    if [ "$CONFIRM" != "yes" ]; then
        print_info "Clean cancelled."
        return
    fi
    
    print_info "Stopping services..."
    docker-compose down -v
    
    print_info "Removing image..."
    docker rmi controlpeso-controlpeso-web 2>/dev/null || true
    
    print_success "All cleaned!"
}

# Shell access
shell() {
    print_info "Opening shell in container (type 'exit' to leave)..."
    docker exec -it controlpeso-web /bin/bash
}

# Main script
main() {
    check_docker
    check_env_file
    
    while true; do
        show_menu
        read option
        
        case $option in
            1) build_start ;;
            2) start ;;
            3) stop ;;
            4) restart ;;
            5) logs ;;
            6) logs_follow ;;
            7) status ;;
            8) backup_db ;;
            9) restore_db ;;
            10) clean_all ;;
            11) shell ;;
            0) 
                print_info "Goodbye!"
                exit 0
                ;;
            *)
                print_error "Invalid option: $option"
                ;;
        esac
        
        echo ""
        echo -n "Press Enter to continue..."
        read
    done
}

# Run main
main
