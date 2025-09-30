#!/bin/bash

# SmartUnderwrite End-to-End Validation Script
# Comprehensive testing of user workflows, security controls, and system validation

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Test results tracking
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

# Function to log test results
log_test() {
    local test_name=$1
    local status=$2
    local message=$3
    
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
    
    if [ "$status" = "PASS" ]; then
        echo -e "   ${GREEN}✅ $test_name${NC}"
        PASSED_TESTS=$((PASSED_TESTS + 1))
    else
        echo -e "   ${RED}❌ $test_name${NC}"
        if [ ! -z "$message" ]; then
            echo -e "      ${RED}$message${NC}"
        fi
        FAILED_TESTS=$((FAILED_TESTS + 1))
    fi
}

# Function to make authenticated API request
api_request() {
    local method=$1
    local endpoint=$2
    local token=$3
    local data=$4
    
    if [ -z "$data" ]; then
        curl -s -X "$method" "http://localhost:8080$endpoint" \
            -H "Authorization: Bearer $token" \
            -H "Content-Type: application/json"
    else
        curl -s -X "$method" "http://localhost:8080$endpoint" \
            -H "Authorization: Bearer $token" \
            -H "Content-Type: application/json" \
            -d "$data"
    fi
}

# Function to authenticate user and get token
authenticate_user() {
    local email=$1
    local password=$2
    
    local response=$(curl -s -X POST http://localhost:8080/api/auth/login \
        -H "Content-Type: application/json" \
        -d "{\"email\":\"$email\",\"password\":\"$password\"}")
    
    echo "$response" | grep -o '"token":"[^"]*"' | cut -d'"' -f4
}

echo -e "${BLUE}🧪 SmartUnderwrite End-to-End Validation${NC}"
echo "========================================"
echo ""

# Check prerequisites
echo "🔍 Checking prerequisites..."
if ! curl -f -s http://localhost:8080/healthz > /dev/null 2>&1; then
    echo -e "${RED}❌ API is not running. Please start the development environment first.${NC}"
    echo "Run: docker-compose up -d"
    exit 1
fi

echo -e "${GREEN}✅ API is running and healthy${NC}"
echo ""

# Test 1: Complete User Workflows Across All Roles
echo -e "${BLUE}1️⃣ Testing Complete User Workflows${NC}"
echo "=================================="

# Authenticate all user types
echo "🔐 Authenticating users..."
ADMIN_TOKEN=$(authenticate_user "admin@smartunderwrite.com" "Admin123!")
UNDERWRITER_TOKEN=$(authenticate_user "underwriter@smartunderwrite.com" "Under123!")
AFFILIATE_TOKEN=$(authenticate_user "affiliate1@pfp001.com" "Affiliate123!")

if [ ! -z "$ADMIN_TOKEN" ]; then
    log_test "Admin Authentication" "PASS"
else
    log_test "Admin Authentication" "FAIL" "Failed to get admin token"
fi

if [ ! -z "$UNDERWRITER_TOKEN" ]; then
    log_test "Underwriter Authentication" "PASS"
else
    log_test "Underwriter Authentication" "FAIL" "Failed to get underwriter token"
fi

if [ ! -z "$AFFILIATE_TOKEN" ]; then
    log_test "Affiliate Authentication" "PASS"
else
    log_test "Affiliate Authentication" "FAIL" "Failed to get affiliate token"
fi

# Test Affiliate Workflow: Create Application
echo ""
echo "👤 Testing Affiliate Workflow..."
APPLICATION_DATA='{
    "applicant": {
        "firstName": "John",
        "lastName": "Doe",
        "email": "john.doe@example.com",
        "phone": "555-0123",
        "dateOfBirth": "1985-06-15T00:00:00Z",
        "ssn": "123456789",
        "address": {
            "street": "123 Main St",
            "city": "Anytown",
            "state": "CA",
            "zipCode": "12345"
        }
    },
    "productType": "Personal Loan",
    "amount": 25000,
    "incomeMonthly": 5000,
    "employmentType": "Full-time",
    "creditScore": 720
}'

CREATE_RESPONSE=$(api_request "POST" "/api/applications" "$AFFILIATE_TOKEN" "$APPLICATION_DATA")
APPLICATION_ID=$(echo "$CREATE_RESPONSE" | grep -o '"id":[0-9]*' | cut -d':' -f2)

if [ ! -z "$APPLICATION_ID" ] && [ "$APPLICATION_ID" != "null" ]; then
    log_test "Affiliate Create Application" "PASS"
else
    log_test "Affiliate Create Application" "FAIL" "Failed to create application"
fi

# Test Affiliate can view their own applications
AFFILIATE_APPS=$(api_request "GET" "/api/applications" "$AFFILIATE_TOKEN")
if echo "$AFFILIATE_APPS" | grep -q '"id"'; then
    log_test "Affiliate View Own Applications" "PASS"
else
    log_test "Affiliate View Own Applications" "FAIL" "No applications returned"
fi

# Test Underwriter Workflow: Evaluate Application
echo ""
echo "⚖️ Testing Underwriter Workflow..."
if [ ! -z "$APPLICATION_ID" ]; then
    EVAL_RESPONSE=$(api_request "POST" "/api/applications/$APPLICATION_ID/evaluate" "$UNDERWRITER_TOKEN")
    if echo "$EVAL_RESPONSE" | grep -q '"outcome"'; then
        log_test "Underwriter Evaluate Application" "PASS"
    else
        log_test "Underwriter Evaluate Application" "FAIL" "Evaluation failed"
    fi
    
    # Test manual decision
    MANUAL_DECISION='{
        "outcome": "Approve",
        "reasons": ["Manual approval after review"],
        "justification": "Applicant meets all criteria"
    }'
    
    DECISION_RESPONSE=$(api_request "POST" "/api/applications/$APPLICATION_ID/decision" "$UNDERWRITER_TOKEN" "$MANUAL_DECISION")
    if echo "$DECISION_RESPONSE" | grep -q '"outcome"'; then
        log_test "Underwriter Manual Decision" "PASS"
    else
        log_test "Underwriter Manual Decision" "FAIL" "Manual decision failed"
    fi
fi

# Test Admin Workflow: Rule Management
echo ""
echo "👑 Testing Admin Workflow..."
RULE_DATA='{
    "name": "Test Rule",
    "description": "Test rule for validation",
    "definition": {
        "name": "Test Credit Rule",
        "priority": 100,
        "clauses": [
            {
                "if": "CreditScore < 600",
                "then": "REJECT",
                "reason": "Low credit score"
            }
        ]
    },
    "isActive": true
}'

RULE_RESPONSE=$(api_request "POST" "/api/rules" "$ADMIN_TOKEN" "$RULE_DATA")
if echo "$RULE_RESPONSE" | grep -q '"id"'; then
    log_test "Admin Create Rule" "PASS"
else
    log_test "Admin Create Rule" "FAIL" "Rule creation failed"
fi

# Test 2: Security Controls and Data Segregation
echo ""
echo -e "${BLUE}2️⃣ Testing Security Controls and Data Segregation${NC}"
echo "================================================"

# Test cross-affiliate access denial
echo "🔒 Testing data segregation..."
AFFILIATE2_TOKEN=$(authenticate_user "affiliate2@pfp002.com" "Affiliate123!")

if [ ! -z "$AFFILIATE2_TOKEN" ] && [ ! -z "$APPLICATION_ID" ]; then
    # Try to access another affiliate's application
    CROSS_ACCESS=$(api_request "GET" "/api/applications/$APPLICATION_ID" "$AFFILIATE2_TOKEN")
    if echo "$CROSS_ACCESS" | grep -q "Unauthorized\|Forbidden\|404"; then
        log_test "Cross-Affiliate Access Denied" "PASS"
    else
        log_test "Cross-Affiliate Access Denied" "FAIL" "Cross-affiliate access was allowed"
    fi
fi

# Test unauthorized access without token
UNAUTH_RESPONSE=$(curl -s -w "%{http_code}" -o /dev/null http://localhost:8080/api/applications)
if [ "$UNAUTH_RESPONSE" = "401" ]; then
    log_test "Unauthorized Access Blocked" "PASS"
else
    log_test "Unauthorized Access Blocked" "FAIL" "Expected 401, got $UNAUTH_RESPONSE"
fi

# Test role-based access control
AFFILIATE_ADMIN_ACCESS=$(api_request "GET" "/api/rules" "$AFFILIATE_TOKEN")
if echo "$AFFILIATE_ADMIN_ACCESS" | grep -q "Forbidden\|Unauthorized"; then
    log_test "Role-Based Access Control" "PASS"
else
    log_test "Role-Based Access Control" "FAIL" "Affiliate accessed admin endpoint"
fi

# Test 3: Load Testing on Rules Engine
echo ""
echo -e "${BLUE}3️⃣ Testing Rules Engine Performance${NC}"
echo "=================================="

echo "⚡ Running concurrent evaluation tests..."

# Create multiple applications for load testing
LOAD_TEST_APPS=()
for i in {1..5}; do
    LOAD_APP_DATA='{
        "applicant": {
            "firstName": "LoadTest",
            "lastName": "User'$i'",
            "email": "loadtest'$i'@example.com",
            "phone": "555-010'$i'",
            "dateOfBirth": "1985-06-15T00:00:00Z",
            "ssn": "12345678'$i'",
            "address": {
                "street": "123 Test St",
                "city": "Testtown",
                "state": "CA",
                "zipCode": "12345"
            }
        },
        "productType": "Personal Loan",
        "amount": '$((20000 + i * 5000))',
        "incomeMonthly": '$((4000 + i * 500))',
        "employmentType": "Full-time",
        "creditScore": '$((650 + i * 10))'
    }'
    
    LOAD_APP_RESPONSE=$(api_request "POST" "/api/applications" "$AFFILIATE_TOKEN" "$LOAD_APP_DATA")
    LOAD_APP_ID=$(echo "$LOAD_APP_RESPONSE" | grep -o '"id":[0-9]*' | cut -d':' -f2)
    if [ ! -z "$LOAD_APP_ID" ]; then
        LOAD_TEST_APPS+=($LOAD_APP_ID)
    fi
done

# Run concurrent evaluations
CONCURRENT_SUCCESS=0
CONCURRENT_TOTAL=${#LOAD_TEST_APPS[@]}

for app_id in "${LOAD_TEST_APPS[@]}"; do
    (
        EVAL_START=$(date +%s%N)
        EVAL_RESULT=$(api_request "POST" "/api/applications/$app_id/evaluate" "$UNDERWRITER_TOKEN")
        EVAL_END=$(date +%s%N)
        EVAL_TIME=$(( (EVAL_END - EVAL_START) / 1000000 ))
        
        if echo "$EVAL_RESULT" | grep -q '"outcome"'; then
            echo "Evaluation $app_id: SUCCESS (${EVAL_TIME}ms)"
        else
            echo "Evaluation $app_id: FAILED"
        fi
    ) &
done

wait

# Check if concurrent evaluations completed successfully
sleep 2
COMPLETED_EVALUATIONS=0
for app_id in "${LOAD_TEST_APPS[@]}"; do
    APP_STATUS=$(api_request "GET" "/api/applications/$app_id" "$UNDERWRITER_TOKEN")
    if echo "$APP_STATUS" | grep -q '"status":"Evaluated"'; then
        COMPLETED_EVALUATIONS=$((COMPLETED_EVALUATIONS + 1))
    fi
done

if [ $COMPLETED_EVALUATIONS -eq $CONCURRENT_TOTAL ]; then
    log_test "Concurrent Rules Engine Evaluation" "PASS"
else
    log_test "Concurrent Rules Engine Evaluation" "FAIL" "$COMPLETED_EVALUATIONS/$CONCURRENT_TOTAL completed"
fi

# Test 4: Audit Logging Validation
echo ""
echo -e "${BLUE}4️⃣ Testing Audit Logging${NC}"
echo "========================"

echo "📋 Validating audit trail capture..."

# Get audit logs
AUDIT_LOGS=$(api_request "GET" "/api/audit" "$ADMIN_TOKEN")

# Check for application creation audit
if echo "$AUDIT_LOGS" | grep -q "LoanApplication.*Created"; then
    log_test "Application Creation Audit" "PASS"
else
    log_test "Application Creation Audit" "FAIL" "No application creation audit found"
fi

# Check for decision audit
if echo "$AUDIT_LOGS" | grep -q "Decision.*Created"; then
    log_test "Decision Creation Audit" "PASS"
else
    log_test "Decision Creation Audit" "FAIL" "No decision audit found"
fi

# Check for rule creation audit
if echo "$AUDIT_LOGS" | grep -q "Rule.*Created"; then
    log_test "Rule Creation Audit" "PASS"
else
    log_test "Rule Creation Audit" "FAIL" "No rule creation audit found"
fi

# Check for authentication audit
if echo "$AUDIT_LOGS" | grep -q "User.*Login"; then
    log_test "Authentication Audit" "PASS"
else
    log_test "Authentication Audit" "FAIL" "No authentication audit found"
fi

# Test 5: System Integration Validation
echo ""
echo -e "${BLUE}5️⃣ Testing System Integration${NC}"
echo "============================="

# Test database connectivity
DB_CHECK=$(docker-compose exec -T postgres psql -U postgres -d smartunderwrite -c "SELECT COUNT(*) FROM \"LoanApplications\";" 2>/dev/null || echo "ERROR")
if [ "$DB_CHECK" != "ERROR" ]; then
    log_test "Database Connectivity" "PASS"
else
    log_test "Database Connectivity" "FAIL" "Database connection failed"
fi

# Test MinIO connectivity
MINIO_CHECK=$(curl -f -s http://localhost:9000/minio/health/live 2>/dev/null && echo "OK" || echo "ERROR")
if [ "$MINIO_CHECK" = "OK" ]; then
    log_test "MinIO Storage Connectivity" "PASS"
else
    log_test "MinIO Storage Connectivity" "FAIL" "MinIO connection failed"
fi

# Test API health endpoints
HEALTH_CHECK=$(curl -f -s http://localhost:8080/healthz 2>/dev/null && echo "OK" || echo "ERROR")
if [ "$HEALTH_CHECK" = "OK" ]; then
    log_test "API Health Endpoint" "PASS"
else
    log_test "API Health Endpoint" "FAIL" "Health check failed"
fi

READINESS_CHECK=$(curl -f -s http://localhost:8080/readyz 2>/dev/null && echo "OK" || echo "ERROR")
if [ "$READINESS_CHECK" = "OK" ]; then
    log_test "API Readiness Endpoint" "PASS"
else
    log_test "API Readiness Endpoint" "FAIL" "Readiness check failed"
fi

# Final Results Summary
echo ""
echo -e "${BLUE}📊 End-to-End Validation Results${NC}"
echo "================================"
echo ""
echo -e "Total Tests: ${BLUE}$TOTAL_TESTS${NC}"
echo -e "Passed: ${GREEN}$PASSED_TESTS${NC}"
echo -e "Failed: ${RED}$FAILED_TESTS${NC}"
echo ""

if [ $FAILED_TESTS -eq 0 ]; then
    echo -e "${GREEN}🎉 All end-to-end validation tests passed!${NC}"
    echo ""
    echo "✅ User workflows function correctly across all roles"
    echo "✅ Security controls and data segregation are working"
    echo "✅ Rules engine handles concurrent evaluations"
    echo "✅ Audit logging captures all required events"
    echo "✅ System integration is functioning properly"
    echo ""
    exit 0
else
    echo -e "${RED}❌ Some validation tests failed.${NC}"
    echo ""
    echo "Please review the failed tests above and address any issues."
    echo "Common troubleshooting steps:"
    echo "  1. Ensure all services are running: docker-compose ps"
    echo "  2. Check service logs: docker-compose logs"
    echo "  3. Verify database is seeded: make seed"
    echo "  4. Restart services if needed: make restart"
    echo ""
    exit 1
fi