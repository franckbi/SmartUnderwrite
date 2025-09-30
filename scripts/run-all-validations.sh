#!/bin/bash

# SmartUnderwrite Master Validation Script
# Runs all end-to-end validation tests in sequence

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Test results tracking
TOTAL_SUITES=0
PASSED_SUITES=0
FAILED_SUITES=0

# Function to log suite results
log_suite() {
    local suite_name=$1
    local status=$2
    local message=$3
    
    TOTAL_SUITES=$((TOTAL_SUITES + 1))
    
    if [ "$status" = "PASS" ]; then
        echo -e "${GREEN}✅ $suite_name - PASSED${NC}"
        PASSED_SUITES=$((PASSED_SUITES + 1))
    else
        echo -e "${RED}❌ $suite_name - FAILED${NC}"
        if [ ! -z "$message" ]; then
            echo -e "   ${RED}$message${NC}"
        fi
        FAILED_SUITES=$((FAILED_SUITES + 1))
    fi
}

# Function to run test suite
run_test_suite() {
    local suite_name=$1
    local script_path=$2
    local description=$3
    
    echo ""
    echo -e "${CYAN}🧪 Running $suite_name${NC}"
    echo -e "${CYAN}$description${NC}"
    echo "$(printf '=%.0s' {1..60})"
    
    if [ -f "$script_path" ]; then
        if bash "$script_path"; then
            log_suite "$suite_name" "PASS"
            return 0
        else
            log_suite "$suite_name" "FAIL" "Test suite failed with exit code $?"
            return 1
        fi
    else
        log_suite "$suite_name" "FAIL" "Script not found: $script_path"
        return 1
    fi
}

echo -e "${BLUE}🚀 SmartUnderwrite Complete Validation Suite${NC}"
echo "============================================="
echo ""
echo "This script runs comprehensive end-to-end validation tests including:"
echo "• System setup verification"
echo "• API contract testing"
echo "• End-to-end user workflows"
echo "• Security controls validation"
echo "• Rules engine load testing"
echo "• Audit logging verification"
echo ""

# Check prerequisites
echo -e "${YELLOW}🔍 Checking Prerequisites${NC}"
echo "========================="

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}❌ Docker is not running. Please start Docker and try again.${NC}"
    exit 1
fi

# Check if API is running
if ! curl -f -s http://localhost:8080/healthz > /dev/null 2>&1; then
    echo -e "${RED}❌ SmartUnderwrite API is not running.${NC}"
    echo "Please start the development environment:"
    echo "  docker-compose up -d"
    echo "  make dev"
    exit 1
fi

echo -e "${GREEN}✅ All prerequisites met${NC}"

# Create results directory
RESULTS_DIR="validation-results-$(date +%Y%m%d-%H%M%S)"
mkdir -p "$RESULTS_DIR"

echo ""
echo -e "${BLUE}📁 Results will be saved to: $RESULTS_DIR${NC}"

# Test Suite 1: System Setup Verification
run_test_suite \
    "System Setup Verification" \
    "scripts/verify-setup.sh" \
    "Verifies all services are running and properly configured" \
    2>&1 | tee "$RESULTS_DIR/01-setup-verification.log"

# Test Suite 2: API Contract Testing
run_test_suite \
    "API Contract Testing" \
    "run-api-tests.sh" \
    "Validates API endpoints against OpenAPI specification" \
    2>&1 | tee "$RESULTS_DIR/02-api-contract-tests.log"

# Test Suite 3: End-to-End User Workflows
run_test_suite \
    "End-to-End User Workflows" \
    "scripts/e2e-validation.sh" \
    "Tests complete user workflows across all roles" \
    2>&1 | tee "$RESULTS_DIR/03-e2e-workflows.log"

# Test Suite 4: Security Controls Validation
run_test_suite \
    "Security Controls Validation" \
    "scripts/security-validation.sh" \
    "Validates authentication, authorization, and data segregation" \
    2>&1 | tee "$RESULTS_DIR/04-security-validation.log"

# Test Suite 5: Rules Engine Load Testing
echo ""
echo -e "${CYAN}🧪 Running Rules Engine Load Testing${NC}"
echo -e "${CYAN}Tests concurrent evaluation performance and system stability${NC}"
echo "$(printf '=%.0s' {1..60})"

if bash scripts/load-test-rules-engine.sh 5 3 2>&1 | tee "$RESULTS_DIR/05-load-testing.log"; then
    log_suite "Rules Engine Load Testing" "PASS"
else
    log_suite "Rules Engine Load Testing" "FAIL" "Load test failed"
fi

# Test Suite 6: Audit Logging Verification
run_test_suite \
    "Audit Logging Verification" \
    "scripts/audit-validation.sh" \
    "Verifies comprehensive audit trail capture and compliance" \
    2>&1 | tee "$RESULTS_DIR/06-audit-validation.log"

# Generate comprehensive report
echo ""
echo -e "${BLUE}📊 Generating Comprehensive Report${NC}"
echo "=================================="

cat > "$RESULTS_DIR/validation-summary.md" << EOF
# SmartUnderwrite Validation Summary

**Test Date:** $(date)
**Total Test Suites:** $TOTAL_SUITES
**Passed:** $PASSED_SUITES
**Failed:** $FAILED_SUITES
**Success Rate:** $(( PASSED_SUITES * 100 / TOTAL_SUITES ))%

## Test Suite Results

### 1. System Setup Verification
- **Status:** $([ -f "$RESULTS_DIR/01-setup-verification.log" ] && (grep -q "Setup verification completed" "$RESULTS_DIR/01-setup-verification.log" && echo "✅ PASSED" || echo "❌ FAILED") || echo "❓ NOT RUN")
- **Purpose:** Verify all services are running and properly configured
- **Log:** 01-setup-verification.log

### 2. API Contract Testing
- **Status:** $([ -f "$RESULTS_DIR/02-api-contract-tests.log" ] && (grep -q "All API contract tests passed" "$RESULTS_DIR/02-api-contract-tests.log" && echo "✅ PASSED" || echo "❌ FAILED") || echo "❓ NOT RUN")
- **Purpose:** Validate API endpoints against OpenAPI specification
- **Log:** 02-api-contract-tests.log

### 3. End-to-End User Workflows
- **Status:** $([ -f "$RESULTS_DIR/03-e2e-workflows.log" ] && (grep -q "All end-to-end validation tests passed" "$RESULTS_DIR/03-e2e-workflows.log" && echo "✅ PASSED" || echo "❌ FAILED") || echo "❓ NOT RUN")
- **Purpose:** Test complete user workflows across all roles
- **Log:** 03-e2e-workflows.log

### 4. Security Controls Validation
- **Status:** $([ -f "$RESULTS_DIR/04-security-validation.log" ] && (grep -q "All security validation tests passed" "$RESULTS_DIR/04-security-validation.log" && echo "✅ PASSED" || echo "❌ FAILED") || echo "❓ NOT RUN")
- **Purpose:** Validate authentication, authorization, and data segregation
- **Log:** 04-security-validation.log

### 5. Rules Engine Load Testing
- **Status:** $([ -f "$RESULTS_DIR/05-load-testing.log" ] && (grep -q "Load test PASSED" "$RESULTS_DIR/05-load-testing.log" && echo "✅ PASSED" || echo "❌ FAILED") || echo "❓ NOT RUN")
- **Purpose:** Test concurrent evaluation performance and system stability
- **Log:** 05-load-testing.log

### 6. Audit Logging Verification
- **Status:** $([ -f "$RESULTS_DIR/06-audit-validation.log" ] && (grep -q "All audit validation tests passed" "$RESULTS_DIR/06-audit-validation.log" && echo "✅ PASSED" || echo "❌ FAILED") || echo "❓ NOT RUN")
- **Purpose:** Verify comprehensive audit trail capture and compliance
- **Log:** 06-audit-validation.log

## Requirements Coverage

This validation suite covers the following requirements:

- **Requirement 10.2:** Complete end-to-end testing and validation ✅
- **Requirement 6.1:** Audit logging captures all required events ✅
- **Requirement 7.3:** Role-based access control validation ✅
- **Requirement 7.4:** Data segregation between affiliates ✅
- **Requirement 2.6:** Rules engine performance under load ✅
- **Requirement 8.1:** API contract compliance ✅

## Next Steps

$(if [ $FAILED_SUITES -eq 0 ]; then
    echo "🎉 **All validation tests passed!** The system is ready for deployment."
    echo ""
    echo "### Deployment Readiness Checklist"
    echo "- ✅ All services are operational"
    echo "- ✅ API contracts are validated"
    echo "- ✅ User workflows function correctly"
    echo "- ✅ Security controls are working"
    echo "- ✅ Performance meets requirements"
    echo "- ✅ Audit logging is comprehensive"
else
    echo "❌ **Some validation tests failed.** Please address the following:"
    echo ""
    echo "### Action Items"
    echo "1. Review failed test logs for specific issues"
    echo "2. Fix identified problems"
    echo "3. Re-run validation suite"
    echo "4. Ensure all tests pass before deployment"
fi)

## Test Environment

- **API URL:** http://localhost:8080
- **Frontend URL:** http://localhost:3000
- **Database:** PostgreSQL (Docker)
- **Storage:** MinIO (Docker)
- **Test Data:** Seeded development data

## Contact

For questions about this validation report, please contact the development team.
EOF

# Final Results Summary
echo ""
echo -e "${BLUE}🏁 Final Validation Results${NC}"
echo "==========================="
echo ""
echo -e "Total Test Suites: ${BLUE}$TOTAL_SUITES${NC}"
echo -e "Passed: ${GREEN}$PASSED_SUITES${NC}"
echo -e "Failed: ${RED}$FAILED_SUITES${NC}"
echo -e "Success Rate: ${BLUE}$(( PASSED_SUITES * 100 / TOTAL_SUITES ))%${NC}"
echo ""

if [ $FAILED_SUITES -eq 0 ]; then
    echo -e "${GREEN}🎉 ALL VALIDATION TESTS PASSED!${NC}"
    echo ""
    echo "✅ System setup is verified"
    echo "✅ API contracts are validated"
    echo "✅ User workflows function correctly"
    echo "✅ Security controls are working"
    echo "✅ Performance meets requirements"
    echo "✅ Audit logging is comprehensive"
    echo ""
    echo -e "${GREEN}🚀 System is ready for deployment!${NC}"
    echo ""
    echo "📁 Detailed results available in: $RESULTS_DIR/"
    echo "📄 Summary report: $RESULTS_DIR/validation-summary.md"
    exit 0
else
    echo -e "${RED}❌ SOME VALIDATION TESTS FAILED${NC}"
    echo ""
    echo "Please review the failed test suites and address any issues:"
    echo ""
    
    # List failed suites
    if [ -f "$RESULTS_DIR/01-setup-verification.log" ] && ! grep -q "Setup verification completed" "$RESULTS_DIR/01-setup-verification.log"; then
        echo "❌ System Setup Verification - Check service configuration"
    fi
    
    if [ -f "$RESULTS_DIR/02-api-contract-tests.log" ] && ! grep -q "All API contract tests passed" "$RESULTS_DIR/02-api-contract-tests.log"; then
        echo "❌ API Contract Testing - Check API implementation"
    fi
    
    if [ -f "$RESULTS_DIR/03-e2e-workflows.log" ] && ! grep -q "All end-to-end validation tests passed" "$RESULTS_DIR/03-e2e-workflows.log"; then
        echo "❌ End-to-End Workflows - Check user workflow implementation"
    fi
    
    if [ -f "$RESULTS_DIR/04-security-validation.log" ] && ! grep -q "All security validation tests passed" "$RESULTS_DIR/04-security-validation.log"; then
        echo "❌ Security Validation - Check authentication and authorization"
    fi
    
    if [ -f "$RESULTS_DIR/05-load-testing.log" ] && ! grep -q "Load test PASSED" "$RESULTS_DIR/05-load-testing.log"; then
        echo "❌ Load Testing - Check rules engine performance"
    fi
    
    if [ -f "$RESULTS_DIR/06-audit-validation.log" ] && ! grep -q "All audit validation tests passed" "$RESULTS_DIR/06-audit-validation.log"; then
        echo "❌ Audit Validation - Check audit logging implementation"
    fi
    
    echo ""
    echo "📁 Detailed results available in: $RESULTS_DIR/"
    echo "📄 Summary report: $RESULTS_DIR/validation-summary.md"
    echo ""
    echo "🔧 Common troubleshooting steps:"
    echo "  1. Check service logs: docker-compose logs"
    echo "  2. Restart services: docker-compose restart"
    echo "  3. Reset database: make reset && make seed"
    echo "  4. Verify environment variables and configuration"
    exit 1
fi