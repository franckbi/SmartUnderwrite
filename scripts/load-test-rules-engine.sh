#!/bin/bash

# SmartUnderwrite Rules Engine Load Testing Script
# Tests concurrent evaluation performance and system stability

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
CONCURRENT_USERS=${1:-10}
APPLICATIONS_PER_USER=${2:-5}
TOTAL_APPLICATIONS=$((CONCURRENT_USERS * APPLICATIONS_PER_USER))

echo -e "${BLUE}⚡ SmartUnderwrite Rules Engine Load Test${NC}"
echo "========================================"
echo "Concurrent Users: $CONCURRENT_USERS"
echo "Applications per User: $APPLICATIONS_PER_USER"
echo "Total Applications: $TOTAL_APPLICATIONS"
echo ""

# Function to authenticate and get token
authenticate_user() {
    local email=$1
    local password=$2
    
    local response=$(curl -s -X POST http://localhost:8080/api/auth/login \
        -H "Content-Type: application/json" \
        -d "{\"email\":\"$email\",\"password\":\"$password\"}")
    
    echo "$response" | grep -o '"token":"[^"]*"' | cut -d'"' -f4
}

# Function to create application
create_application() {
    local token=$1
    local user_id=$2
    local app_num=$3
    
    local credit_score=$((550 + RANDOM % 200))  # Random score 550-750
    local income=$((3000 + RANDOM % 7000))      # Random income 3000-10000
    local amount=$((10000 + RANDOM % 40000))    # Random amount 10000-50000
    
    local app_data='{
        "applicant": {
            "firstName": "LoadTest",
            "lastName": "User'$user_id'App'$app_num'",
            "email": "loadtest'$user_id'app'$app_num'@example.com",
            "phone": "555-'$(printf "%04d" $((user_id * 100 + app_num)))'",
            "dateOfBirth": "1985-06-15T00:00:00Z",
            "ssn": "'$(printf "%09d" $((123456000 + user_id * 100 + app_num)))'",
            "address": {
                "street": "'$user_id$app_num' Test St",
                "city": "Testtown",
                "state": "CA",
                "zipCode": "12345"
            }
        },
        "productType": "Personal Loan",
        "amount": '$amount',
        "incomeMonthly": '$income',
        "employmentType": "Full-time",
        "creditScore": '$credit_score'
    }'
    
    local response=$(curl -s -X POST http://localhost:8080/api/applications \
        -H "Authorization: Bearer $token" \
        -H "Content-Type: application/json" \
        -d "$app_data")
    
    echo "$response" | grep -o '"id":[0-9]*' | cut -d':' -f2
}

# Function to evaluate application
evaluate_application() {
    local token=$1
    local app_id=$2
    local start_time=$3
    
    local eval_start=$(date +%s%N)
    local response=$(curl -s -X POST "http://localhost:8080/api/applications/$app_id/evaluate" \
        -H "Authorization: Bearer $token" \
        -H "Content-Type: application/json")
    local eval_end=$(date +%s%N)
    
    local eval_time=$(( (eval_end - eval_start) / 1000000 ))
    local total_time=$(( (eval_end - start_time) / 1000000 ))
    
    if echo "$response" | grep -q '"outcome"'; then
        local outcome=$(echo "$response" | grep -o '"outcome":"[^"]*"' | cut -d'"' -f4)
        local score=$(echo "$response" | grep -o '"score":[0-9]*' | cut -d':' -f2)
        echo "SUCCESS,$app_id,$outcome,$score,$eval_time,$total_time"
    else
        echo "FAILED,$app_id,ERROR,0,$eval_time,$total_time"
    fi
}

# Function to simulate user workflow
simulate_user() {
    local user_id=$1
    local token=$2
    local start_time=$3
    
    local user_results=()
    
    for ((app=1; app<=APPLICATIONS_PER_USER; app++)); do
        # Create application
        local app_id=$(create_application "$token" "$user_id" "$app")
        
        if [ ! -z "$app_id" ] && [ "$app_id" != "null" ]; then
            # Evaluate application
            local result=$(evaluate_application "$token" "$app_id" "$start_time")
            user_results+=("$result")
        else
            user_results+=("FAILED,$user_id-$app,CREATE_ERROR,0,0,0")
        fi
    done
    
    # Output results for this user
    for result in "${user_results[@]}"; do
        echo "$result"
    done
}

# Check prerequisites
echo "🔍 Checking prerequisites..."
if ! curl -f -s http://localhost:8080/healthz > /dev/null 2>&1; then
    echo -e "${RED}❌ API is not running. Please start the development environment first.${NC}"
    exit 1
fi

# Authenticate users
echo "🔐 Authenticating test users..."
AFFILIATE_TOKEN=$(authenticate_user "affiliate1@pfp001.com" "Affiliate123!")
UNDERWRITER_TOKEN=$(authenticate_user "underwriter@smartunderwrite.com" "Under123!")

if [ -z "$AFFILIATE_TOKEN" ] || [ -z "$UNDERWRITER_TOKEN" ]; then
    echo -e "${RED}❌ Failed to authenticate test users${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Authentication successful${NC}"
echo ""

# Create results directory
RESULTS_DIR="load-test-results-$(date +%Y%m%d-%H%M%S)"
mkdir -p "$RESULTS_DIR"

echo "🚀 Starting load test..."
echo "Results will be saved to: $RESULTS_DIR"
echo ""

# Record start time
TEST_START=$(date +%s%N)

# Start concurrent user simulations
echo "👥 Spawning $CONCURRENT_USERS concurrent users..."
for ((user=1; user<=CONCURRENT_USERS; user++)); do
    (
        simulate_user "$user" "$AFFILIATE_TOKEN" "$TEST_START"
    ) > "$RESULTS_DIR/user-$user.csv" &
done

# Wait for all background processes to complete
echo "⏳ Waiting for all evaluations to complete..."
wait

# Record end time
TEST_END=$(date +%s%N)
TOTAL_TEST_TIME=$(( (TEST_END - TEST_START) / 1000000 ))

echo ""
echo "📊 Analyzing results..."

# Combine all results
cat "$RESULTS_DIR"/user-*.csv > "$RESULTS_DIR/combined-results.csv"

# Calculate statistics
TOTAL_REQUESTS=$(wc -l < "$RESULTS_DIR/combined-results.csv")
SUCCESSFUL_REQUESTS=$(grep -c "SUCCESS" "$RESULTS_DIR/combined-results.csv" || echo "0")
FAILED_REQUESTS=$(grep -c "FAILED" "$RESULTS_DIR/combined-results.csv" || echo "0")

SUCCESS_RATE=$(( SUCCESSFUL_REQUESTS * 100 / TOTAL_REQUESTS ))

# Calculate response time statistics
if [ $SUCCESSFUL_REQUESTS -gt 0 ]; then
    grep "SUCCESS" "$RESULTS_DIR/combined-results.csv" | cut -d',' -f5 > "$RESULTS_DIR/response-times.txt"
    
    MIN_TIME=$(sort -n "$RESULTS_DIR/response-times.txt" | head -1)
    MAX_TIME=$(sort -n "$RESULTS_DIR/response-times.txt" | tail -1)
    
    # Calculate average (simple approach)
    TOTAL_TIME=0
    while read time; do
        TOTAL_TIME=$((TOTAL_TIME + time))
    done < "$RESULTS_DIR/response-times.txt"
    AVG_TIME=$((TOTAL_TIME / SUCCESSFUL_REQUESTS))
    
    # Calculate median
    MEDIAN_LINE=$(( (SUCCESSFUL_REQUESTS + 1) / 2 ))
    MEDIAN_TIME=$(sort -n "$RESULTS_DIR/response-times.txt" | sed -n "${MEDIAN_LINE}p")
    
    # Calculate 95th percentile
    P95_LINE=$(( SUCCESSFUL_REQUESTS * 95 / 100 ))
    P95_TIME=$(sort -n "$RESULTS_DIR/response-times.txt" | sed -n "${P95_LINE}p")
else
    MIN_TIME=0
    MAX_TIME=0
    AVG_TIME=0
    MEDIAN_TIME=0
    P95_TIME=0
fi

# Calculate throughput
THROUGHPUT=$(( SUCCESSFUL_REQUESTS * 1000 / (TOTAL_TEST_TIME / 1000) ))

# Analyze decision outcomes
APPROVALS=$(grep "SUCCESS.*Approve" "$RESULTS_DIR/combined-results.csv" | wc -l || echo "0")
REJECTIONS=$(grep "SUCCESS.*Reject" "$RESULTS_DIR/combined-results.csv" | wc -l || echo "0")
MANUAL_REVIEWS=$(grep "SUCCESS.*ManualReview" "$RESULTS_DIR/combined-results.csv" | wc -l || echo "0")

# Generate summary report
cat > "$RESULTS_DIR/load-test-report.txt" << EOF
SmartUnderwrite Rules Engine Load Test Report
============================================

Test Configuration:
- Concurrent Users: $CONCURRENT_USERS
- Applications per User: $APPLICATIONS_PER_USER
- Total Applications: $TOTAL_APPLICATIONS
- Test Duration: ${TOTAL_TEST_TIME}ms

Performance Results:
- Total Requests: $TOTAL_REQUESTS
- Successful Requests: $SUCCESSFUL_REQUESTS
- Failed Requests: $FAILED_REQUESTS
- Success Rate: $SUCCESS_RATE%
- Throughput: $THROUGHPUT requests/second

Response Time Statistics (ms):
- Minimum: $MIN_TIME
- Maximum: $MAX_TIME
- Average: $AVG_TIME
- Median: $MEDIAN_TIME
- 95th Percentile: $P95_TIME

Decision Outcomes:
- Approvals: $APPROVALS
- Rejections: $REJECTIONS
- Manual Reviews: $MANUAL_REVIEWS

Test Status: $([ $SUCCESS_RATE -ge 95 ] && echo "PASSED" || echo "FAILED")
EOF

# Display results
echo ""
echo -e "${BLUE}📈 Load Test Results${NC}"
echo "==================="
echo ""
echo "🎯 Performance Metrics:"
echo "   • Total Requests: $TOTAL_REQUESTS"
echo "   • Success Rate: $SUCCESS_RATE%"
echo "   • Throughput: $THROUGHPUT req/sec"
echo "   • Test Duration: ${TOTAL_TEST_TIME}ms"
echo ""
echo "⏱️ Response Times (ms):"
echo "   • Average: $AVG_TIME"
echo "   • Median: $MEDIAN_TIME"
echo "   • 95th Percentile: $P95_TIME"
echo "   • Min/Max: $MIN_TIME/$MAX_TIME"
echo ""
echo "📋 Decision Distribution:"
echo "   • Approvals: $APPROVALS"
echo "   • Rejections: $REJECTIONS"
echo "   • Manual Reviews: $MANUAL_REVIEWS"
echo ""

# Determine test result
if [ $SUCCESS_RATE -ge 95 ] && [ $AVG_TIME -le 2000 ] && [ $P95_TIME -le 5000 ]; then
    echo -e "${GREEN}🎉 Load test PASSED!${NC}"
    echo "✅ Success rate >= 95%"
    echo "✅ Average response time <= 2000ms"
    echo "✅ 95th percentile <= 5000ms"
    TEST_RESULT=0
else
    echo -e "${RED}❌ Load test FAILED!${NC}"
    [ $SUCCESS_RATE -lt 95 ] && echo "❌ Success rate < 95%"
    [ $AVG_TIME -gt 2000 ] && echo "❌ Average response time > 2000ms"
    [ $P95_TIME -gt 5000 ] && echo "❌ 95th percentile > 5000ms"
    TEST_RESULT=1
fi

echo ""
echo "📁 Detailed results saved to: $RESULTS_DIR/"
echo "   • load-test-report.txt - Summary report"
echo "   • combined-results.csv - All test results"
echo "   • user-*.csv - Individual user results"

exit $TEST_RESULT