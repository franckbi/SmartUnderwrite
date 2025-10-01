#!/bin/bash

# SmartUnderwrite Setup Verification Script

set -e

echo "🔍 SmartUnderwrite Setup Verification"
echo "===================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to check service health
check_service() {
    local service_name=$1
    local health_check=$2
    local url=$3
    
    echo -n "   Checking $service_name... "
    
    if eval $health_check > /dev/null 2>&1; then
        echo -e "${GREEN}✅ Healthy${NC}"
        if [ ! -z "$url" ]; then
            echo "      URL: $url"
        fi
        return 0
    else
        echo -e "${RED}❌ Unhealthy${NC}"
        return 1
    fi
}

# Function to check database data
check_database_data() {
    echo "📊 Checking database data..."
    
    local affiliate_count=$(docker-compose exec -T postgres psql -U postgres -d smartunderwrite -t -c "SELECT COUNT(*) FROM \"Affiliates\";" 2>/dev/null | tr -d ' \n' || echo "0")
    local user_count=$(docker-compose exec -T postgres psql -U postgres -d smartunderwrite -t -c "SELECT COUNT(*) FROM \"AspNetUsers\";" 2>/dev/null | tr -d ' \n' || echo "0")
    local application_count=$(docker-compose exec -T postgres psql -U postgres -d smartunderwrite -t -c "SELECT COUNT(*) FROM \"LoanApplications\";" 2>/dev/null | tr -d ' \n' || echo "0")
    local rule_count=$(docker-compose exec -T postgres psql -U postgres -d smartunderwrite -t -c "SELECT COUNT(*) FROM \"Rules\";" 2>/dev/null | tr -d ' \n' || echo "0")
    
    echo "   • Affiliates: $affiliate_count (expected: 3)"
    echo "   • Users: $user_count (expected: 5)"
    echo "   • Applications: $application_count (expected: 30)"
    echo "   • Rules: $rule_count (expected: 2)"
    
    if [ "$affiliate_count" -eq 3 ] && [ "$user_count" -eq 5 ] && [ "$application_count" -eq 30 ] && [ "$rule_count" -eq 2 ]; then
        echo -e "   ${GREEN}✅ Database data looks correct${NC}"
        return 0
    else
        echo -e "   ${YELLOW}⚠️  Database data counts don't match expected values${NC}"
        return 1
    fi
}

# Function to test API endpoints
test_api_endpoints() {
    echo "🔗 Testing API endpoints..."
    
    # Test health endpoint
    if curl -f -s http://localhost:8080/api/health/healthz > /dev/null 2>&1; then
        echo -e "   Health endpoint: ${GREEN}✅ Working${NC}"
    else
        echo -e "   Health endpoint: ${RED}❌ Failed${NC}"
        return 1
    fi
    
    # Test OpenAPI endpoint (Swagger JSON)
    if curl -f -s http://localhost:8080/openapi/v1.json > /dev/null 2>&1; then
        echo -e "   OpenAPI Spec: ${GREEN}✅ Working${NC}"
    else
        echo -e "   OpenAPI Spec: ${RED}❌ Failed${NC}"
        return 1
    fi
    
    return 0
}

# Function to test authentication
test_authentication() {
    echo "🔐 Testing authentication..."
    
    # Test admin login
    local login_response=$(curl -s -X POST http://localhost:8080/api/auth/login \
        -H "Content-Type: application/json" \
        -d '{"email":"admin@smartunderwrite.com","password":"Admin123!"}' 2>/dev/null)
    
    if echo "$login_response" | grep -q "accessToken"; then
        echo -e "   Admin login: ${GREEN}✅ Working${NC}"
        return 0
    else
        echo -e "   Admin login: ${RED}❌ Failed${NC}"
        echo "   Response: $login_response"
        return 1
    fi
}

# Main verification process
echo "🚀 Starting verification process..."
echo ""

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}❌ Docker is not running. Please start Docker and try again.${NC}"
    exit 1
fi

echo "🐳 Docker Services:"
check_service "PostgreSQL" "docker-compose exec -T postgres pg_isready -U postgres" "localhost:5432"
check_service "MinIO" "curl -f http://localhost:9000/minio/health/live" "http://localhost:9001"
check_service "API" "curl -f http://localhost:8080/api/health/healthz" "http://localhost:8080"
check_service "Frontend" "curl -f http://localhost:3000/health" "http://localhost:3000"

echo ""
check_database_data

echo ""
test_api_endpoints

echo ""
test_authentication

echo ""
echo "📋 Service URLs:"
echo "==============="
echo "   • Frontend: http://localhost:3000"
echo "   • API: http://localhost:8080"
echo "   • Swagger: http://localhost:8080/swagger"
echo "   • MinIO Console: http://localhost:9001 (minioadmin/minioadmin123)"

echo ""
echo "👥 Test Credentials:"
echo "==================="
echo "   • Admin: admin@smartunderwrite.com / Admin123!"
echo "   • Underwriter: underwriter@smartunderwrite.com / Under123!"
echo "   • Affiliate 1: affiliate1@pfp001.com / Affiliate123!"

echo ""
echo "🔧 Useful Commands:"
echo "=================="
echo "   • View logs: make logs"
echo "   • Reset database: make reset"
echo "   • Run tests: make test"
echo "   • Stop services: make down"

echo ""
echo -e "${GREEN}🎉 Setup verification completed!${NC}"
echo ""
echo "Your SmartUnderwrite development environment is ready to use."