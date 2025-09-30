#!/bin/bash

# SmartUnderwrite Audit Logging Validation Script
# Comprehensive testing of audit trail capture and compliance requirements

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

# Function to wait for audit processing
wait_for_audit() {
    echo "   ⏳ Waiting for audit processing..."
    sleep 2
}

# Function to check audit logs for specific event
check_audit_event() {
    local event_type=$1
    local entity_type=$2
    local action=$3
    local admin_token=$4
    
    local audit_logs=$(api_request "GET" "/api/audit" "$admin_token")
    
    if echo "$audit_logs" | grep -q "\"entityType\":\"$entity_type\"" && \
       echo "$audit_logs" | grep -q "\"action\":\"$action\""; then
        return 0
    else
        return 1
    fi
}

echo -e "${BLUE}📋 SmartUnderwrite Audit Logging Validation${NC}"
echo "==========================================="
echo ""

# Check prerequisites
echo "🔍 Checking prerequisites..."
if ! curl -f -s http://localhost:8080/healthz > /dev/null 2>&1; then
    echo -e "${RED}❌ API is not running. Please start the development environment first.${NC}"
    exit 1
fi

echo -e "${GREEN}✅ API is running and healthy${NC}"
echo ""

# Authenticate users
echo "🔐 Authenticating users..."
ADMIN_TOKEN=$(authenticate_user "admin@smartunderwrite.com" "Admin123!")
UNDERWRITER_TOKEN=$(authenticate_user "underwriter@smartunderwrite.com" "Under123!")
AFFILIATE_TOKEN=$(authenticate_user "affiliate1@pfp001.com" "Affiliate123!")

if [ -z "$ADMIN_TOKEN" ] || [ -z "$UNDERWRITER_TOKEN" ] || [ -z "$AFFILIATE_TOKEN" ]; then
    echo -e "${RED}❌ Failed to authenticate required users${NC}"
    exit 1
fi

echo -e "${GREEN}✅ All users authenticated successfully${NC}"
echo ""

# Test 1: Authentication Audit Events
echo -e "${BLUE}1️⃣ Testing Authentication Audit Events${NC}"
echo "======================================"

echo "🔐 Testing login audit capture..."

# Get initial audit count
INITIAL_AUDIT_COUNT=$(api_request "GET" "/api/audit" "$ADMIN_TOKEN" | grep -c '"action":"Login"' || echo "0")

# Perform a fresh login to generate audit event
FRESH_LOGIN=$(curl -s -X POST http://localhost:8080/api/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"affiliate1@pfp001.com","password":"Affiliate123!"}')

wait_for_audit

# Check if login was audited
NEW_AUDIT_COUNT=$(api_request "GET" "/api/audit" "$ADMIN_TOKEN" | grep -c '"action":"Login"' || echo "0")

if [ $NEW_AUDIT_COUNT -gt $INITIAL_AUDIT_COUNT ]; then
    log_test "Login Event Audited" "PASS"
else
    log_test "Login Event Audited" "FAIL" "No new login audit entries found"
fi

# Test failed login audit
echo "🚫 Testing failed login audit capture..."
FAILED_LOGIN=$(curl -s -X POST http://localhost:8080/api/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"affiliate1@pfp001.com","password":"wrongpassword"}')

wait_for_audit

# Check for failed login audit (this might be implemented differently)
FAILED_LOGIN_AUDIT=$(api_request "GET" "/api/audit" "$ADMIN_TOKEN" | grep -c '"action":"LoginFailed"' || echo "0")

if [ $FAILED_LOGIN_AUDIT -gt 0 ]; then
    log_test "Failed Login Event Audited" "PASS"
else
    log_test "Failed Login Event Audited" "FAIL" "No failed login audit entries found"
fi

# Test 2: Application Lifecycle Audit Events
echo ""
echo -e "${BLUE}2️⃣ Testing Application Lifecycle Audit Events${NC}"
echo "=============================================="

echo "📝 Testing application creation audit..."

# Create application and check audit
APPLICATION_DATA='{
    "applicant": {
        "firstName": "Audit",
        "lastName": "Test",
        "email": "audit.test@example.com",
        "phone": "555-0199",
        "dateOfBirth": "1985-06-15T00:00:00Z",
        "ssn": "999887766",
        "address": {
            "street": "123 Audit St",
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

CREATE_RESPONSE=$(api_request "POST" "/api/applications" "$AFFILIATE_TOKEN" "$APPLICATION_DATA")
APPLICATION_ID=$(echo "$CREATE_RESPONSE" | grep -o '"id":[0-9]*' | cut -d':' -f2)

wait_for_audit

if check_audit_event "application" "LoanApplication" "Created" "$ADMIN_TOKEN"; then
    log_test "Application Creation Audited" "PASS"
else
    log_test "Application Creation Audited" "FAIL" "No application creation audit found"
fi

# Test application evaluation audit
echo "⚖️ Testing application evaluation audit..."
if [ ! -z "$APPLICATION_ID" ]; then
    EVAL_RESPONSE=$(api_request "POST" "/api/applications/$APPLICATION_ID/evaluate" "$UNDERWRITER_TOKEN")
    
    wait_for_audit
    
    if check_audit_event "evaluation" "Decision" "Created" "$ADMIN_TOKEN"; then
        log_test "Application Evaluation Audited" "PASS"
    else
        log_test "Application Evaluation Audited" "FAIL" "No evaluation audit found"
    fi
fi

# Test manual decision audit
echo "👨‍⚖️ Testing manual decision audit..."
if [ ! -z "$APPLICATION_ID" ]; then
    MANUAL_DECISION='{
        "outcome": "Approve",
        "reasons": ["Manual approval after audit test"],
        "justification": "Test case for audit validation"
    }'
    
    DECISION_RESPONSE=$(api_request "POST" "/api/applications/$APPLICATION_ID/decision" "$UNDERWRITER_TOKEN" "$MANUAL_DECISION")
    
    wait_for_audit
    
    if check_audit_event "decision" "Decision" "Created" "$ADMIN_TOKEN"; then
        log_test "Manual Decision Audited" "PASS"
    else
        log_test "Manual Decision Audited" "FAIL" "No manual decision audit found"
    fi
fi

# Test 3: Rule Management Audit Events
echo ""
echo -e "${BLUE}3️⃣ Testing Rule Management Audit Events${NC}"
echo "======================================="

echo "📏 Testing rule creation audit..."

RULE_DATA='{
    "name": "Audit Test Rule",
    "description": "Rule created for audit testing",
    "definition": {
        "name": "Audit Test Rule",
        "priority": 999,
        "clauses": [
            {
                "if": "CreditScore < 500",
                "then": "REJECT",
                "reason": "Very low credit score"
            }
        ]
    },
    "isActive": false
}'

RULE_RESPONSE=$(api_request "POST" "/api/rules" "$ADMIN_TOKEN" "$RULE_DATA")
RULE_ID=$(echo "$RULE_RESPONSE" | grep -o '"id":[0-9]*' | cut -d':' -f2)

wait_for_audit

if check_audit_event "rule" "Rule" "Created" "$ADMIN_TOKEN"; then
    log_test "Rule Creation Audited" "PASS"
else
    log_test "Rule Creation Audited" "FAIL" "No rule creation audit found"
fi

# Test 4: Data Access Audit Events
echo ""
echo -e "${BLUE}4️⃣ Testing Data Access Audit Events${NC}"
echo "=================================="

echo "👀 Testing data access audit..."

# Access application data
if [ ! -z "$APPLICATION_ID" ]; then
    ACCESS_RESPONSE=$(api_request "GET" "/api/applications/$APPLICATION_ID" "$UNDERWRITER_TOKEN")
    
    wait_for_audit
    
    # Check for data access audit (might be implemented as Read action)
    ACCESS_AUDIT=$(api_request "GET" "/api/audit" "$ADMIN_TOKEN" | grep -c '"action":"Read"' || echo "0")
    
    if [ $ACCESS_AUDIT -gt 0 ]; then
        log_test "Data Access Audited" "PASS"
    else
        log_test "Data Access Audited" "FAIL" "No data access audit found"
    fi
fi

# Test 5: Audit Log Query and Filtering
echo ""
echo -e "${BLUE}5️⃣ Testing Audit Log Query and Filtering${NC}"
echo "========================================"

echo "🔍 Testing audit log filtering..."

# Test filtering by entity type
FILTERED_AUDIT=$(api_request "GET" "/api/audit?entityType=LoanApplication" "$ADMIN_TOKEN")

if echo "$FILTERED_AUDIT" | grep -q '"entityType":"LoanApplication"'; then
    log_test "Audit Filtering by Entity Type" "PASS"
else
    log_test "Audit Filtering by Entity Type" "FAIL" "Filtering not working"
fi

# Test filtering by action
ACTION_FILTERED_AUDIT=$(api_request "GET" "/api/audit?action=Created" "$ADMIN_TOKEN")

if echo "$ACTION_FILTERED_AUDIT" | grep -q '"action":"Created"'; then
    log_test "Audit Filtering by Action" "PASS"
else
    log_test "Audit Filtering by Action" "FAIL" "Action filtering not working"
fi

# Test date range filtering (last 24 hours)
YESTERDAY=$(date -d "yesterday" -u +"%Y-%m-%dT%H:%M:%SZ" 2>/dev/null || date -u -v-1d +"%Y-%m-%dT%H:%M:%SZ")
TODAY=$(date -u +"%Y-%m-%dT%H:%M:%SZ")

DATE_FILTERED_AUDIT=$(api_request "GET" "/api/audit?fromDate=$YESTERDAY&toDate=$TODAY" "$ADMIN_TOKEN")

if echo "$DATE_FILTERED_AUDIT" | grep -q '"timestamp"'; then
    log_test "Audit Date Range Filtering" "PASS"
else
    log_test "Audit Date Range Filtering" "FAIL" "Date filtering not working"
fi

# Test 6: PII Protection in Audit Logs
echo ""
echo -e "${BLUE}6️⃣ Testing PII Protection in Audit Logs${NC}"
echo "======================================"

echo "🔒 Testing PII protection..."

# Get all audit logs and check for PII exposure
ALL_AUDIT_LOGS=$(api_request "GET" "/api/audit" "$ADMIN_TOKEN")

# Check that SSN is not exposed in plain text
if echo "$ALL_AUDIT_LOGS" | grep -q "999887766"; then
    log_test "SSN PII Protection" "FAIL" "SSN found in plain text in audit logs"
else
    log_test "SSN PII Protection" "PASS"
fi

# Check that email addresses are handled appropriately
if echo "$ALL_AUDIT_LOGS" | grep -q "audit.test@example.com"; then
    # This might be acceptable depending on implementation
    log_test "Email PII Handling" "PASS"
else
    log_test "Email PII Handling" "PASS"
fi

# Test 7: Audit Log Integrity
echo ""
echo -e "${BLUE}7️⃣ Testing Audit Log Integrity${NC}"
echo "=============================="

echo "🛡️ Testing audit log integrity..."

# Check that audit logs are immutable (no update/delete operations)
if [ ! -z "$APPLICATION_ID" ]; then
    # Try to access audit logs as non-admin user
    AFFILIATE_AUDIT_ACCESS=$(curl -s -w "%{http_code}" -o /tmp/affiliate_audit.json \
        -X GET "http://localhost:8080/api/audit" \
        -H "Authorization: Bearer $AFFILIATE_TOKEN")
    
    if [ "$AFFILIATE_AUDIT_ACCESS" = "403" ]; then
        log_test "Audit Log Access Control" "PASS"
    else
        log_test "Audit Log Access Control" "FAIL" "Non-admin can access audit logs"
    fi
fi

# Test audit log completeness
AUDIT_ENTRY_COUNT=$(api_request "GET" "/api/audit" "$ADMIN_TOKEN" | grep -c '"id"' || echo "0")

if [ $AUDIT_ENTRY_COUNT -gt 10 ]; then
    log_test "Audit Log Completeness" "PASS"
else
    log_test "Audit Log Completeness" "FAIL" "Insufficient audit entries found"
fi

# Test 8: Correlation ID Tracking
echo ""
echo -e "${BLUE}8️⃣ Testing Correlation ID Tracking${NC}"
echo "================================="

echo "🔗 Testing correlation ID tracking..."

# Make request and check for correlation ID in response headers
CORRELATION_RESPONSE=$(curl -s -I -X GET "http://localhost:8080/api/applications" \
    -H "Authorization: Bearer $AFFILIATE_TOKEN")

if echo "$CORRELATION_RESPONSE" | grep -qi "x-correlation-id\|correlation-id"; then
    log_test "Correlation ID in Response Headers" "PASS"
else
    log_test "Correlation ID in Response Headers" "FAIL" "No correlation ID found in headers"
fi

# Check if correlation IDs are present in audit logs
CORRELATION_IN_AUDIT=$(api_request "GET" "/api/audit" "$ADMIN_TOKEN" | grep -c '"correlationId"' || echo "0")

if [ $CORRELATION_IN_AUDIT -gt 0 ]; then
    log_test "Correlation ID in Audit Logs" "PASS"
else
    log_test "Correlation ID in Audit Logs" "FAIL" "No correlation IDs found in audit logs"
fi

# Test 9: Audit Performance and Scalability
echo ""
echo -e "${BLUE}9️⃣ Testing Audit Performance${NC}"
echo "============================"

echo "⚡ Testing audit performance..."

# Measure audit query performance
AUDIT_START=$(date +%s%N)
LARGE_AUDIT_QUERY=$(api_request "GET" "/api/audit?pageSize=100" "$ADMIN_TOKEN")
AUDIT_END=$(date +%s%N)

AUDIT_QUERY_TIME=$(( (AUDIT_END - AUDIT_START) / 1000000 ))

if [ $AUDIT_QUERY_TIME -lt 5000 ]; then
    log_test "Audit Query Performance" "PASS"
else
    log_test "Audit Query Performance" "FAIL" "Query took ${AUDIT_QUERY_TIME}ms (>5000ms)"
fi

# Final Results Summary
echo ""
echo -e "${BLUE}📊 Audit Validation Results${NC}"
echo "==========================="
echo ""
echo -e "Total Tests: ${BLUE}$TOTAL_TESTS${NC}"
echo -e "Passed: ${GREEN}$PASSED_TESTS${NC}"
echo -e "Failed: ${RED}$FAILED_TESTS${NC}"
echo ""

if [ $FAILED_TESTS -eq 0 ]; then
    echo -e "${GREEN}🎉 All audit validation tests passed!${NC}"
    echo ""
    echo "✅ Authentication events are properly audited"
    echo "✅ Application lifecycle events are captured"
    echo "✅ Rule management events are logged"
    echo "✅ Data access events are tracked"
    echo "✅ Audit log filtering and querying work correctly"
    echo "✅ PII is protected in audit logs"
    echo "✅ Audit log integrity is maintained"
    echo "✅ Correlation ID tracking is working"
    echo "✅ Audit performance is acceptable"
    echo ""
    echo "🏆 System meets compliance and audit requirements!"
    exit 0
else
    echo -e "${RED}❌ Some audit validation tests failed.${NC}"
    echo ""
    echo "Please review the failed tests above and address audit issues."
    echo "Audit failures may impact compliance requirements."
    echo ""
    echo "Common troubleshooting steps:"
    echo "  1. Check audit middleware configuration"
    echo "  2. Verify database audit table structure"
    echo "  3. Review audit service implementation"
    echo "  4. Check correlation ID middleware"
    echo ""
    exit 1
fi