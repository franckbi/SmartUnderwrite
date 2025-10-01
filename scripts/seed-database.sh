#!/bin/bash

# SmartUnderwrite Database Seeding Script

set -e

echo "🌱 SmartUnderwrite Database Seeding Script"
echo "=========================================="

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker and try again."
    exit 1
fi

# Function to wait for service to be ready
wait_for_service() {
    local service_name=$1
    local health_check=$2
    local max_attempts=30
    local attempt=1
    
    echo "⏳ Waiting for $service_name to be ready..."
    
    while [ $attempt -le $max_attempts ]; do
        if eval $health_check > /dev/null 2>&1; then
            echo "✅ $service_name is ready"
            return 0
        fi
        
        echo "   Attempt $attempt/$max_attempts - $service_name not ready yet..."
        sleep 2
        attempt=$((attempt + 1))
    done
    
    echo "❌ $service_name failed to become ready after $max_attempts attempts"
    return 1
}

# Start database if not running
echo "🚀 Starting database services..."
docker-compose up -d postgres minio

# Wait for services to be ready
wait_for_service "PostgreSQL" "docker-compose exec -T postgres pg_isready -U postgres"
wait_for_service "MinIO" "curl -f http://localhost:9000/minio/health/live"

# Check if API is running, start if needed
if ! docker-compose ps api | grep -q "Up"; then
    echo "🏗️  Starting API service..."
    docker-compose up -d api
    sleep 10
fi

echo "🌱 Running database creation and seeding..."
if docker-compose exec -T api dotnet SmartUnderwrite.Api.dll --seed; then
    echo "✅ Database seeding completed successfully"
else
    echo "❌ Database seeding failed"
    exit 1
fi

# Create MinIO bucket if it doesn't exist
echo "📁 Setting up MinIO bucket..."
if docker-compose exec -T minio mc alias set local http://localhost:9000 minioadmin minioadmin123 > /dev/null 2>&1; then
    if docker-compose exec -T minio mc mb local/smartunderwrite-documents --ignore-existing > /dev/null 2>&1; then
        echo "✅ MinIO bucket created/verified"
    else
        echo "⚠️  MinIO bucket may already exist"
    fi
else
    echo "⚠️  Could not configure MinIO client"
fi

# Display seeded data summary
echo ""
echo "📊 Seeded Data Summary:"
echo "======================"

# Get counts from database
AFFILIATE_COUNT=$(docker-compose exec -T postgres psql -U postgres -d smartunderwrite -t -c "SELECT COUNT(*) FROM \"Affiliates\";" | tr -d ' \n')
USER_COUNT=$(docker-compose exec -T postgres psql -U postgres -d smartunderwrite -t -c "SELECT COUNT(*) FROM \"AspNetUsers\";" | tr -d ' \n')
APPLICATION_COUNT=$(docker-compose exec -T postgres psql -U postgres -d smartunderwrite -t -c "SELECT COUNT(*) FROM \"LoanApplications\";" | tr -d ' \n')
RULE_COUNT=$(docker-compose exec -T postgres psql -U postgres -d smartunderwrite -t -c "SELECT COUNT(*) FROM \"Rules\";" | tr -d ' \n')

echo "   • Affiliates: $AFFILIATE_COUNT"
echo "   • Users: $USER_COUNT"
echo "   • Loan Applications: $APPLICATION_COUNT"
echo "   • Rules: $RULE_COUNT"

echo ""
echo "👥 Test User Credentials:"
echo "========================"
echo "   • Admin: admin@smartunderwrite.com / Admin123!"
echo "   • Underwriter: underwriter@smartunderwrite.com / Under123!"
echo "   • Affiliate 1: affiliate1@pfp001.com / Affiliate123!"
echo "   • Affiliate 2: affiliate2@ccs002.com / Affiliate123!"
echo "   • Affiliate 3: affiliate3@mvl003.com / Affiliate123!"

echo ""
echo "🎉 Database seeding completed successfully!"
echo ""
echo "🔗 Access URLs:"
echo "   • Frontend: http://localhost:3000"
echo "   • API: http://localhost:8080"
echo "   • Swagger: http://localhost:8080/swagger"
echo "   • MinIO Console: http://localhost:9001"