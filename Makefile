# SmartUnderwrite Development Makefile

.PHONY: help build up down logs clean test migrate seed reset verify dev-up dev-down validate-e2e validate-security validate-audit validate-load validate-all

# Default target
help:
	@echo "SmartUnderwrite Development Commands:"
	@echo ""
	@echo "  make build          - Build all Docker images"
	@echo "  make up             - Start all services"
	@echo "  make down           - Stop all services"
	@echo "  make logs           - View logs from all services"
	@echo "  make clean          - Clean up containers, volumes, and images"
	@echo "  make test           - Run all tests"
	@echo "  make migrate        - Run database migrations"
	@echo "  make seed           - Seed database with test data"
	@echo "  make reset          - Reset database completely"
	@echo "  make verify         - Verify setup and test all services"
	@echo "  make dev-up         - Start development environment with hot reload"
	@echo "  make dev-down       - Stop development environment"
	@echo ""
	@echo "Validation Commands:"
	@echo "  make validate-e2e      - Run end-to-end workflow validation"
	@echo "  make validate-security - Run security controls validation"
	@echo "  make validate-audit    - Run audit logging validation"
	@echo "  make validate-load     - Run rules engine load testing"
	@echo "  make validate-all      - Run complete validation suite"
	@echo ""

# Build all images
build:
	@echo "🏗️  Building Docker images..."
	docker-compose build

# Start all services
up:
	@echo "🚀 Starting services..."
	docker-compose up -d
	@echo "✅ Services started. Check 'make logs' for status."

# Stop all services
down:
	@echo "🛑 Stopping services..."
	docker-compose down

# View logs
logs:
	@echo "📋 Viewing logs (Ctrl+C to exit)..."
	docker-compose logs -f

# Clean up everything
clean:
	@echo "🧹 Cleaning up containers, volumes, and images..."
	docker-compose down --volumes --remove-orphans
	docker system prune -f
	@echo "✅ Cleanup complete."

# Run tests
test:
	@echo "🧪 Running tests..."
	docker-compose exec api dotnet test
	@echo "✅ Tests completed."

# Run database migrations
migrate:
	@echo "🗄️  Running database migrations..."
	./scripts/migrate-database.sh

# Seed database with test data
seed:
	@echo "🌱 Seeding database with test data..."
	./scripts/seed-database.sh

# Reset database completely
reset:
	@echo "🔄 Resetting database..."
	./scripts/reset-database.sh

# Verify setup and test all services
verify:
	@echo "🔍 Verifying setup..."
	./scripts/verify-setup.sh

# Start development environment with hot reload
dev-up:
	@echo "🚀 Starting development environment with hot reload..."
	docker-compose -f docker-compose.yml -f docker-compose.override.yml up --build -d
	@echo "✅ Development environment started."

# Stop development environment
dev-down:
	@echo "🛑 Stopping development environment..."
	docker-compose -f docker-compose.yml -f docker-compose.override.yml down
	@echo "✅ Development environment stopped."

# Validation commands
validate-e2e:
	@echo "🧪 Running end-to-end workflow validation..."
	./scripts/e2e-validation.sh

validate-security:
	@echo "🔒 Running security controls validation..."
	./scripts/security-validation.sh

validate-audit:
	@echo "📋 Running audit logging validation..."
	./scripts/audit-validation.sh

validate-load:
	@echo "⚡ Running rules engine load testing..."
	./scripts/load-test-rules-engine.sh 10 5

validate-all:
	@echo "🚀 Running complete validation suite..."
	./scripts/run-all-validations.sh

# Quick setup for new developers
setup: build up migrate seed
	@echo "🎉 Complete setup finished!"
	@echo "Frontend: http://localhost:3000"
	@echo "API: http://localhost:8080"
	@echo "Swagger: http://localhost:8080/swagger"