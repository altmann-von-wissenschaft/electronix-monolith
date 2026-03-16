#!/bin/bash

# Test Environment Runner Script
# Usage: ./run-tests.sh [command] [options]
# Commands: start, stop, restart, test, test-watch, logs, clean

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$SCRIPT_DIR"
COMPOSE_FILE="$PROJECT_ROOT/docker-compose.test.yml"
APP_DIR="$PROJECT_ROOT/Application"
TEST_DIR="$PROJECT_ROOT/Tests"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Functions
print_header() {
    echo -e "${GREEN}=== $1 ===${NC}"
}

print_error() {
    echo -e "${RED}ERROR: $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}WARNING: $1${NC}"
}

start_test_environment() {
    print_header "Starting Test Environment"
    
    cd "$PROJECT_ROOT"
    docker compose -f "$COMPOSE_FILE" up -d
    
    print_header "Waiting for services to be ready..."
    sleep 20
    
    # Check if containers are running
    if ! docker compose -f "$COMPOSE_FILE" ps | grep -q "application-test"; then
        print_error "Application container failed to start"
        docker compose -f "$COMPOSE_FILE" logs application-test
        exit 1
    fi
    
    print_header "Waiting for PostgreSQL to be ready..."
    # Give postgres more time to be fully ready
    for i in {1..40}; do
        if timeout 1 bash -c "echo > /dev/tcp/localhost/5433" 2>/dev/null; then
            echo "PostgreSQL port 5433 is open!"
            break
        fi
        echo "Attempt $i/40: Waiting for PostgreSQL (port check)..."
        sleep 1
    done
    
    sleep 3  # Extra buffer to ensure postgres is fully initialized
    
    print_header "Applying database migrations..."
    cd "$APP_DIR"
    
    # Run all migrations for test environment
    for context in UsersDbContext ProductsDbContext OrdersDbContext CartDbContext ReviewsDbContext SupportDbContext; do
        echo ""
        echo "=== Migrating $context ==="
        if dotnet ef database update --context "$context" -- --environment Testing 2>&1 | head -20; then
            echo "✓ $context migration completed"
        else
            echo "✗ $context migration failed, checking status..."
            sleep 2
        fi
    done
    
    print_header "Seeding test data..."
    cd "$PROJECT_ROOT"
    "$PROJECT_ROOT/seed-test-data.sh"
    
    print_header "Test environment is ready!"
    echo -e "Access application at: ${GREEN}http://localhost:8080${NC}"
    echo -e "PostgreSQL at: ${GREEN}localhost:5433${NC}"
    echo -e "MinIO at: ${GREEN}localhost:9010${NC}"
    echo ""
    echo -e "Test accounts:"
    echo -e "  Admin:  altmannvonw@icloud.com / 12345678"
    echo -e "  User:   testuser@example.com / TestPassword123"
}

stop_test_environment() {
    print_header "Stopping Test Environment"
    cd "$PROJECT_ROOT"
    docker compose -f "$COMPOSE_FILE" down
    echo -e "${GREEN}Test environment stopped${NC}"
}

restart_test_environment() {
    stop_test_environment
    sleep 2
    start_test_environment
}

run_tests() {
    print_header "Running Tests"
    cd "$TEST_DIR"
    
    # Check if environment is running
    if ! docker compose -f "$COMPOSE_FILE" ps | grep -q "application-test.*Up"; then
        print_error "Test environment is not running. Start it with: ./run-tests.sh start"
        exit 1
    fi
    
    # Run tests
    dotnet test --verbosity normal "$@"
}

run_tests_watch() {
    print_header "Running Tests in Watch Mode"
    cd "$TEST_DIR"
    
    # Check if environment is running
    if ! docker compose -f "$COMPOSE_FILE" ps | grep -q "application-test.*Up"; then
        print_error "Test environment is not running. Start it with: ./run-tests.sh start"
        exit 1
    fi
    
    # Watch tests
    dotnet watch test
}

show_logs() {
    print_header "Test Environment Logs"
    cd "$PROJECT_ROOT"
    
    if [ -z "$1" ]; then
        docker compose -f "$COMPOSE_FILE" logs -f
    else
        docker compose -f "$COMPOSE_FILE" logs -f "$1"
    fi
}

clean_test_environment() {
    print_header "Cleaning Test Environment"
    cd "$PROJECT_ROOT"
    
    docker compose -f "$COMPOSE_FILE" down -v
    echo -e "${GREEN}Test environment and volumes removed${NC}"
}

show_status() {
    print_header "Test Environment Status"
    cd "$PROJECT_ROOT"
    docker compose -f "$COMPOSE_FILE" ps
}

# Main script logic
COMMAND=${1:-help}

case "$COMMAND" in
    start)
        start_test_environment
        ;;
    stop)
        stop_test_environment
        ;;
    restart)
        restart_test_environment
        ;;
    test)
        shift
        run_tests "$@"
        ;;
    test-watch)
        run_tests_watch
        ;;
    logs)
        show_logs "$2"
        ;;
    status)
        show_status
        ;;
    clean)
        clean_test_environment
        ;;
    *)
        echo "Test Environment Management Script"
        echo ""
        echo "Usage: $0 <command> [options]"
        echo ""
        echo "Commands:"
        echo "  start           - Start test environment (PostgreSQL, MinIO, Application)"
        echo "  stop            - Stop test environment"
        echo "  restart         - Restart test environment"
        echo "  test [options]  - Run tests (options passed to dotnet test)"
        echo "  test-watch      - Run tests in watch mode"
        echo "  logs [service]  - View container logs (service optional)"
        echo "  status          - Show test environment status"
        echo "  clean           - Remove test environment and volumes"
        echo ""
        echo "Examples:"
        echo "  $0 start                    # Start test environment"
        echo "  $0 test                     # Run all tests"
        echo "  $0 test --filter 'Auth'     # Run authentication tests only"
        echo "  $0 logs application-test    # View application logs"
        echo "  $0 clean                    # Clean up test environment"
        ;;
esac
