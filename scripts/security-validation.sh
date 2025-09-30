#!/bin/bash

# SmartUnderwrite Security Validation Script
# Comprehensive security testing including authentication, authorization, and data segregation

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

# Function to make API request with response code
api_request_with_code() {
    local method=$1
    local endpoint=$2
    local token=$3
    local data=$4
    
    if [ -z "$data" ]; then
        curl -s -w "%{http_code}" -o /tmp/response.json -X "$method" "http://localhost:8080$endpoint" \
            -H "Authorization: Bearer $token" \
            -H "Content-Type: application/json"
    else
        curl -s -w "%{http_code}" -o /tmp/response.json -X "$method" "http://localhost:8080$endpoint" \
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

echo -e "${BLUE}🔒 SmartUnderwrite Security Validation${NC}"
echo "====================================="
echo ""

# Check prerequisites
echo "🔍 Checking prerequisites..."
if ! curl -f -s http://localhost:8080/healthz > /dev/null 2>&1; then
    echo -e "${RED}❌ API is not running. Please start the development environment first.${NC}"
    exit 1
fi

echo -e "${GREEN}✅ API is running and healthy${NC}"
echo ""

# Test 1: Authentication Security
echo -e "${BLUE}1️⃣ Testing Authentication Security${NC}"
echo "================================="

# Test valid authentication
echo "🔐 Testing valid authentication..."
ADMIN_TOKEN=$(authenticate_user "admin@smartunderwrite.com" "Admin123!")
UNDERWRITER_TOKEN=$(authenticate_user "underwriter@smartunderwrite.com" "Under123!")
AFFILIATE1_TOKEN=$(authenticate_user "affiliate1@pfp001.com" "Affiliate123!")
AFFILIATE2_TOKEN=$(authenticate_user "affiliate2@pfp002.com" "Affiliate123!")

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

if [ ! -z "$AFFILIATE1_TOKEN" ]; then
    log_test "Affiliate Authentication" "PASS"
else
    log_test "Affiliate Authentication" "FAIL" "Failed to get affiliate token"
fi

# Test invalid credentials
echo ""
echo "🚫 Testing invalid credentials..."
INVALID_RESPONSE=$(curl -s -w "%{http_code}" -o /tmp/invalid_login.json -X POST http://localhost:8080/api/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"invalid@example.com","password":"wrongpassword"}')

if [ "$INVALID_RESPONSE" = "401" ]; then
    log_test "Invalid Credentials Rejected" "PASS"
else
    log_test "Invalid Credentials Rejected" "FAIL" "Expected 401, got $INVALID_RESPONSE"
fi

# Test malformed login requests
MALFORMED_RESPONSE=$(curl -s -w "%{http_code}" -o /tmp/malformed.json -X POST http://localhost:8080/api/auth/login \
    -H "Content-Type: application/json" \
    -d '{"invalid":"json"}')

if [ "$MALFORMED_RESPONSE" = "400" ]; then
    log_test "Malformed Login Request Rejected" "PASS"
else
    log_test "Malformed Login Request Rejected" "FAIL" "Expected 400, got $MALFORMED_RESPONSE"
fi

# Test 2: Authorization and Role-Based Access Control
echo ""
echo -e "${BLUE}2️⃣ Testing Authorization and RBAC${NC}"
echo "================================="

echo "👑 Testing admin-only endpoints..."

# Test affiliate trying to access admin endpoints
AFFILIATE_ADMIN_CODE=$(api_request_with_code "GET" "/api/rules" "$AFFILIATE1_TOKEN")
if [ "$AFFILIATE_ADMIN_CODE" = "403" ]; then
    log_test "Affiliate Blocked from Admin Endpoints" "PASS"
else
    log_test "Affiliate Blocked from Admin Endpoints" "FAIL" "Expected 403, got $AFFILIATE_ADMIN_CODE"
fi

# Test underwriter trying to create rules (admin only)
UNDERWRITER_RULE_CODE=$(api_request_with_code "POST" "/api/rules" "$UNDERWRITER_TOKEN" '{"name":"test","definition":{}}')
if [ "$UNDERWRITER_RULE_CODE" = "403" ]; then
    log_test "Underwriter Blocked from Rule Creation" "PASS"
else
    log_test "Underwriter Blocked from Rule Creation" "FAIL" "Expected 403, got $UNDERWRITER_RULE_CODE"
fi

# Test admin can access admin endpoints
ADMIN_RULES_CODE=$(api_request_with_code "GET" "/api/rules" "$ADMIN_TOKEN")
if [ "$ADMIN_RULES_CODE" = "200" ]; then
    log_test "Admin Can Access Admin Endpoints" "PASS"
else
    log_test "Admin Can Access Admin Endpoints" "FAIL" "Expected 200, got $ADMIN_RULES_CODE"
fi

# Test 3: Data Segregation Between Affiliates
echo ""
echo -e "${BLUE}3️⃣ Testing Data Segregation${NC}"
echo "==========================="

echo "🏢 Testing affiliate data segregation..."

# Create application with affiliate 1
APPLICATION_DATA='{
    "applicant": {
        "firstName": "Security",
        "lastName": "Test",
        "email": "security.test@example.com",
        "phone": "555-0199",
        "dateOfBirth": "1985-06-15T00:00:00Z",
        "ssn": "999887766",
        "address": {
            "street": "123 Security St",
            "city": "Testtown",
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

# Create application with affiliate 1
CREATE_CODE=$(api_request_with_code "POST" "/api/applications" "$AFFILIATE1_TOKEN" "$APPLICATION_DATA")
if [ "$CREATE_CODE" = "201" ]; then
    APPLICATION_ID=$(cat /tmp/response.json | grep -o '"id":[0-9]*' | cut -d':' -f2)
    log_test "Affiliate 1 Can Create Applications" "PASS"
else
    log_test "Affiliate 1 Can Create Applications" "FAIL" "Expected 201, got $CREATE_CODE"
fi

# Test affiliate 2 cannot access affiliate 1's application
if [ ! -z "$APPLICATION_ID" ]; then
    CROSS_ACCESS_CODE=$(api_request_with_code "GET" "/api/applications/$APPLICATION_ID" "$AFFILIATE2_TOKEN")
    if [ "$CROSS_ACCESS_CODE" = "404" ] || [ "$CROSS_ACCESS_CODE" = "403" ]; then
        log_test "Cross-Affiliate Access Denied" "PASS"
    else
        log_test "Cross-Affiliate Access Denied" "FAIL" "Expected 403/404, got $CROSS_ACCESS_CODE"
    fi
fi

# Test affiliate 1 can access their own application
if [ ! -z "$APPLICATION_ID" ]; then
    OWN_ACCESS_CODE=$(api_request_with_code "GET" "/api/applications/$APPLICATION_ID" "$AFFILIATE1_TOKEN")
    if [ "$OWN_ACCESS_CODE" = "200" ]; then
        log_test "Affiliate Can Access Own Applications" "PASS"
    else
        log_test "Affiliate Can Access Own Applications" "FAIL" "Expected 200, got $OWN_ACCESS_CODE"
    fi
fi

# Test underwriter can access all applications
if [ ! -z "$APPLICATION_ID" ]; then
    UNDERWRITER_ACCESS_CODE=$(api_request_with_code "GET" "/api/applications/$APPLICATION_ID" "$UNDERWRITER_TOKEN")
    if [ "$UNDERWRITER_ACCESS_CODE" = "200" ]; then
        log_test "Underwriter Can Access All Applications" "PASS"
    else
        log_test "Underwriter Can Access All Applications" "FAIL" "Expected 200, got $UNDERWRITER_ACCESS_CODE"
    fi
fi

# Test 4: Token Security
echo ""
echo -e "${BLUE}4️⃣ Testing Token Security${NC}"
echo "========================="

echo "🎫 Testing JWT token security..."

# Test request without token
NO_TOKEN_CODE=$(curl -s -w "%{http_code}" -o /tmp/no_token.json -X GET http://localhost:8080/api/applications)
if [ "$NO_TOKEN_CODE" = "401" ]; then
    log_test "No Token Request Rejected" "PASS"
else
    log_test "No Token Request Rejected" "FAIL" "Expected 401, got $NO_TOKEN_CODE"
fi

# Test request with invalid token
INVALID_TOKEN_CODE=$(curl -s -w "%{http_code}" -o /tmp/invalid_token.json -X GET http://localhost:8080/api/applications \
    -H "Authorization: Bearer invalid.token.here")
if [ "$INVALID_TOKEN_CODE" = "401" ]; then
    log_test "Invalid Token Rejected" "PASS"
else
    log_test "Invalid Token Rejected" "FAIL" "Expected 401, got $INVALID_TOKEN_CODE"
fi

# Test request with malformed token
MALFORMED_TOKEN_CODE=$(curl -s -w "%{http_code}" -o /tmp/malformed_token.json -X GET http://localhost:8080/api/applications \
    -H "Authorization: Bearer notajwttoken")
if [ "$MALFORMED_TOKEN_CODE" = "401" ]; then
    log_test "Malformed Token Rejected" "PASS"
else
    log_test "Malformed Token Rejected" "FAIL" "Expected 401, got $MALFORMED_TOKEN_CODE"
fi

# Test 5: Input Validation Security
echo ""
echo -e "${BLUE}5️⃣ Testing Input Validation Security${NC}"
echo "===================================="

echo "🛡️ Testing input validation..."

# Test SQL injection attempt in application creation
SQL_INJECTION_DATA='{
    "applicant": {
        "firstName": "Robert\"; DROP TABLE LoanApplications; --",
        "lastName": "Tables",
        "email": "sqli@example.com",
        "phone": "555-0100",
        "dateOfBirth": "1985-06-15T00:00:00Z",
        "ssn": "123456789",
        "address": {
            "street": "123 Injection St",
            "city": "Testtown",
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

SQL_INJECTION_CODE=$(api_request_with_code "POST" "/api/applications" "$AFFILIATE1_TOKEN" "$SQL_INJECTION_DATA")
if [ "$SQL_INJECTION_CODE" = "201" ] || [ "$SQL_INJECTION_CODE" = "400" ]; then
    # Check if database still exists (SQL injection failed)
    DB_CHECK=$(docker-compose exec -T postgres psql -U postgres -d smartunderwrite -c "SELECT COUNT(*) FROM \"LoanApplications\";" 2>/dev/null || echo "ERROR")
    if [ "$DB_CHECK" != "ERROR" ]; then
        log_test "SQL Injection Protection" "PASS"
    else
        log_test "SQL Injection Protection" "FAIL" "Database appears to be compromised"
    fi
else
    log_test "SQL Injection Protection" "PASS"
fi

# Test XSS attempt in application data
XSS_DATA='{
    "applicant": {
        "firstName": "<script>alert(\"XSS\")</script>",
        "lastName": "Test",
        "email": "xss@example.com",
        "phone": "555-0101",
        "dateOfBirth": "1985-06-15T00:00:00Z",
        "ssn": "123456789",
        "address": {
            "street": "123 XSS St",
            "city": "Testtown",
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

XSS_CODE=$(api_request_with_code "POST" "/api/applications" "$AFFILIATE1_TOKEN" "$XSS_DATA")
if [ "$XSS_CODE" = "201" ] || [ "$XSS_CODE" = "400" ]; then
    log_test "XSS Input Handling" "PASS"
else
    log_test "XSS Input Handling" "FAIL" "Unexpected response code: $XSS_CODE"
fi

# Test oversized payload
LARGE_STRING=$(python3 -c "print('A' * 10000)")
OVERSIZED_DATA='{
    "applicant": {
        "firstName": "'$LARGE_STRING'",
        "lastName": "Test",
        "email": "large@example.com",
        "phone": "555-0102",
        "dateOfBirth": "1985-06-15T00:00:00Z",
        "ssn": "123456789",
        "address": {
            "street": "123 Large St",
            "city": "Testtown",
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

OVERSIZED_CODE=$(api_request_with_code "POST" "/api/applications" "$AFFILIATE1_TOKEN" "$OVERSIZED_DATA")
if [ "$OVERSIZED_CODE" = "400" ] || [ "$OVERSIZED_CODE" = "413" ]; then
    log_test "Oversized Payload Protection" "PASS"
else
    log_test "Oversized Payload Protection" "FAIL" "Expected 400/413, got $OVERSIZED_CODE"
fi

# Test 6: Rate Limiting and DoS Protection
echo ""
echo -e "${BLUE}6️⃣ Testing Rate Limiting${NC}"
echo "========================"

echo "🚦 Testing rate limiting..."

# Make rapid requests to test rate limiting
RATE_LIMIT_FAILURES=0
for i in {1..20}; do
    RATE_CODE=$(curl -s -w "%{http_code}" -o /dev/null http://localhost:8080/api/applications \
        -H "Authorization: Bearer $AFFILIATE1_TOKEN")
    if [ "$RATE_CODE" = "429" ]; then
        RATE_LIMIT_FAILURES=$((RATE_LIMIT_FAILURES + 1))
    fi
done

if [ $RATE_LIMIT_FAILURES -gt 0 ]; then
    log_test "Rate Limiting Active" "PASS"
else
    log_test "Rate Limiting Active" "FAIL" "No rate limiting detected"
fi

# Test 7: HTTPS and Security Headers
echo ""
echo -e "${BLUE}7️⃣ Testing Security Headers${NC}"
echo "============================"

echo "🔒 Testing security headers..."

# Test security headers
HEADERS_RESPONSE=$(curl -s -I http://localhost:8080/api/applications \
    -H "Authorization: Bearer $AFFILIATE1_TOKEN")

if echo "$HEADERS_RESPONSE" | grep -qi "x-content-type-options"; then
    log_test "X-Content-Type-Options Header" "PASS"
else
    log_test "X-Content-Type-Options Header" "FAIL" "Header not found"
fi

if echo "$HEADERS_RESPONSE" | grep -qi "x-frame-options"; then
    log_test "X-Frame-Options Header" "PASS"
else
    log_test "X-Frame-Options Header" "FAIL" "Header not found"
fi

# Final Results Summary
echo ""
echo -e "${BLUE}📊 Security Validation Results${NC}"
echo "=============================="
echo ""
echo -e "Total Tests: ${BLUE}$TOTAL_TESTS${NC}"
echo -e "Passed: ${GREEN}$PASSED_TESTS${NC}"
echo -e "Failed: ${RED}$FAILED_TESTS${NC}"
echo ""

if [ $FAILED_TESTS -eq 0 ]; then
    echo -e "${GREEN}🎉 All security validation tests passed!${NC}"
    echo ""
    echo "✅ Authentication is working correctly"
    echo "✅ Authorization and RBAC are enforced"
    echo "✅ Data segregation between affiliates is working"
    echo "✅ Token security is properly implemented"
    echo "✅ Input validation protects against common attacks"
    echo "✅ Rate limiting is active"
    echo "✅ Security headers are present"
    echo ""
    exit 0
else
    echo -e "${RED}❌ Some security validation tests failed.${NC}"
    echo ""
    echo "Please review the failed tests above and address security issues."
    echo "Security failures should be treated as high priority."
    echo ""
    exit 1
fi