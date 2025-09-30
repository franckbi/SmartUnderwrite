#!/bin/bash

# SmartUnderwrite Development Environment Setup Script

set -e

echo "🚀 Setting up SmartUnderwrite development environment..."

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker and try again."
    exit 1
fi

# Check if Docker Compose is available
if ! command -v docker-compose > /dev/null 2>&1; then
    echo "❌ Docker Compose is not installed. Please install Docker Compose and try again."
    exit 1
fi

# Create necessary directories
echo "📁 Creating necessary directories..."
mkdir -p scripts
mkdir -p SmartUnderwrite.Api/logs
mkdir -p SmartUnderwrite.Frontend/dist

# Stop any existing containers
echo "🛑 Stopping existing containers..."
docker-compose down --remove-orphans

# Build and start services
echo "🏗️  Building and starting services..."
docker-compose up --build -d

# Wait for services to be healthy
echo "⏳ Waiting for services to be ready..."
sleep 10

# Check service health
echo "🔍 Checking service health..."

# Check PostgreSQL
if docker-compose exec -T postgres pg_isready -U postgres > /dev/null 2>&1; then
    echo "✅ PostgreSQL is ready"
else
    echo "❌ PostgreSQL is not ready"
fi

# Check MinIO
if curl -f http://localhost:9000/minio/health/live > /dev/null 2>&1; then
    echo "✅ MinIO is ready"
else
    echo "❌ MinIO is not ready"
fi

# Check API
if curl -f http://localhost:8080/healthz > /dev/null 2>&1; then
    echo "✅ API is ready"
else
    echo "❌ API is not ready"
fi

# Check Frontend
if curl -f http://localhost:3000/health > /dev/null 2>&1; then
    echo "✅ Frontend is ready"
else
    echo "❌ Frontend is not ready"
fi

echo ""
echo "🎉 Development environment setup complete!"
echo ""
echo "📋 Service URLs:"
echo "   • Frontend: http://localhost:3000"
echo "   • API: http://localhost:8080"
echo "   • API Swagger: http://localhost:8080/swagger"
echo "   • MinIO Console: http://localhost:9001 (minioadmin/minioadmin123)"
echo "   • PostgreSQL: localhost:5432 (postgres/postgres123)"
echo ""
echo "🔧 Useful commands:"
echo "   • View logs: docker-compose logs -f [service-name]"
echo "   • Stop services: docker-compose down"
echo "   • Restart services: docker-compose restart"
echo "   • Run migrations: docker-compose exec api dotnet ef database update"
echo ""
echo "📖 For development with hot reload, use:"
echo "   docker-compose -f docker-compose.yml -f docker-compose.override.yml up --build"