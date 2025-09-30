#!/bin/bash

# SmartUnderwrite Database Migration Script

set -e

echo "🗄️  SmartUnderwrite Database Migration Script"
echo "============================================="

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker and try again."
    exit 1
fi

# Start database if not running
echo "🚀 Ensuring database is running..."
docker-compose up -d postgres

# Wait for PostgreSQL to be ready
echo "⏳ Waiting for PostgreSQL to be ready..."
max_attempts=30
attempt=1

while [ $attempt -le $max_attempts ]; do
    if docker-compose exec -T postgres pg_isready -U postgres > /dev/null 2>&1; then
        echo "✅ PostgreSQL is ready"
        break
    fi
    
    echo "   Attempt $attempt/$max_attempts - PostgreSQL not ready yet..."
    sleep 2
    attempt=$((attempt + 1))
done

if [ $attempt -gt $max_attempts ]; then
    echo "❌ PostgreSQL failed to become ready"
    exit 1
fi

# Check if API is running, start if needed
if ! docker-compose ps api | grep -q "Up"; then
    echo "🏗️  Starting API service..."
    docker-compose up -d api
    sleep 10
fi

# Run migrations
echo "🔄 Running database migrations..."
if docker-compose exec -T api dotnet ef database update; then
    echo "✅ Database migrations completed successfully"
else
    echo "❌ Database migrations failed"
    exit 1
fi

# Show migration status
echo ""
echo "📋 Migration Status:"
echo "==================="
docker-compose exec -T api dotnet ef migrations list

echo ""
echo "🎉 Database migration completed!"