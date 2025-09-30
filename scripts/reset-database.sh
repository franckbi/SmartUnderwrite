#!/bin/bash

# SmartUnderwrite Database Reset Script

set -e

echo "🔄 SmartUnderwrite Database Reset Script"
echo "========================================"

# Confirmation prompt
read -p "⚠️  This will completely reset the database and all data will be lost. Continue? (y/N): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "❌ Database reset cancelled."
    exit 1
fi

echo "🛑 Stopping all services..."
docker-compose down

echo "🗑️  Removing database volumes..."
docker volume rm smartunderwrite_postgres_data 2>/dev/null || echo "   PostgreSQL volume not found (already removed)"
docker volume rm smartunderwrite_minio_data 2>/dev/null || echo "   MinIO volume not found (already removed)"

echo "🚀 Starting database services..."
docker-compose up -d postgres minio

# Wait for PostgreSQL to be ready
echo "⏳ Waiting for PostgreSQL to be ready..."
sleep 5
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

echo "🏗️  Starting API service..."
docker-compose up -d api

# Wait for API to be ready
echo "⏳ Waiting for API to be ready..."
sleep 10

echo "🗄️  Running database migrations..."
docker-compose exec -T api dotnet ef database update

echo "🌱 Seeding database with fresh test data..."
docker-compose exec -T api dotnet run --project SmartUnderwrite.Api -- --seed

echo "📁 Setting up MinIO bucket..."
sleep 2
docker-compose exec -T minio mc alias set local http://localhost:9000 minioadmin minioadmin123 > /dev/null 2>&1 || true
docker-compose exec -T minio mc mb local/smartunderwrite-documents --ignore-existing > /dev/null 2>&1 || true

echo ""
echo "🎉 Database reset completed successfully!"
echo ""
echo "📊 Fresh database with:"
echo "   • 3 Affiliates with different configurations"
echo "   • 5 Test users (1 admin, 1 underwriter, 3 affiliates)"
echo "   • 30 Sample loan applications with varied risk profiles"
echo "   • 2 Default underwriting rules"
echo ""
echo "👥 Test User Credentials:"
echo "   • Admin: admin@smartunderwrite.com / Admin123!"
echo "   • Underwriter: underwriter@smartunderwrite.com / Under123!"
echo "   • Affiliate 1: affiliate1@pfp001.com / Affiliate123!"
echo "   • Affiliate 2: affiliate2@ccs002.com / Affiliate123!"
echo "   • Affiliate 3: affiliate3@mvl003.com / Affiliate123!"