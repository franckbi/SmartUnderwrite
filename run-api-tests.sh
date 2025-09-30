#!/bin/bash

# SmartUnderwrite API Contract Test Runner
# This script runs comprehensive API contract tests using Bruno CLI

set -e

echo "🚀 Starting SmartUnderwrite API Contract Tests"
echo "=============================================="

# Check if Bruno CLI is installed
if ! command -v bru &> /dev/null; then
    echo "❌ Bruno CLI not found. Please install it with: npm install -g @usebruno/cli"
    exit 1
fi

# Check if API is running
API_URL="http://localhost:5000"
echo "🔍 Checking if API is running at $API_URL..."

if ! curl -s "$API_URL/api/Health/healthz" > /dev/null; then
    echo "❌ API is not running at $API_URL"
    echo "Please start the SmartUnderwrite API first:"
    echo "  dotnet run --project SmartUnderwrite.Api --urls=\"http://localhost:5000\""
    exit 1
fi

echo "✅ API is running and healthy"

# Validate OpenAPI schema
echo "📋 Validating API contract against OpenAPI specification..."
if node validate-api-schema.js; then
    echo "✅ OpenAPI schema validation passed"
else
    echo "❌ OpenAPI schema validation failed"
    exit 1
fi

# Run Bruno tests
echo "🧪 Running Bruno API contract tests..."
echo ""

# Set environment
export BRUNO_ENV="local"

# Run tests in order
echo "1️⃣ Running Health Check tests..."
bru run api-tests/Health --env local --reporter json > test-results-health.json || true

echo "2️⃣ Running Authentication tests..."
bru run api-tests/Auth --env local --reporter json > test-results-auth.json || true

echo "3️⃣ Running Application Management tests..."
bru run api-tests/Applications --env local --reporter json > test-results-applications.json || true

echo "4️⃣ Running Rules Management tests..."
bru run api-tests/Rules --env local --reporter json > test-results-rules.json || true

echo "5️⃣ Running Audit tests..."
bru run api-tests/Audit --env local --reporter json > test-results-audit.json || true

# Generate summary report
echo ""
echo "📊 Test Results Summary"
echo "======================"

# Count passed/failed tests from JSON results
total_tests=0
passed_tests=0
failed_tests=0

for result_file in test-results-*.json; do
    if [ -f "$result_file" ]; then
        # Simple JSON parsing to count results
        tests_in_file=$(grep -o '"status"' "$result_file" | wc -l || echo "0")
        passed_in_file=$(grep -o '"status":"pass"' "$result_file" | wc -l || echo "0")
        failed_in_file=$(grep -o '"status":"fail"' "$result_file" | wc -l || echo "0")
        
        total_tests=$((total_tests + tests_in_file))
        passed_tests=$((passed_tests + passed_in_file))
        failed_tests=$((failed_tests + failed_in_file))
        
        echo "📁 $(basename "$result_file" .json): $passed_in_file passed, $failed_in_file failed"
    fi
done

echo ""
echo "🎯 Overall Results:"
echo "   Total Tests: $total_tests"
echo "   ✅ Passed: $passed_tests"
echo "   ❌ Failed: $failed_tests"

if [ $failed_tests -eq 0 ]; then
    echo ""
    echo "🎉 All API contract tests passed!"
    echo "✅ API responses match OpenAPI specification"
    echo "✅ Happy path scenarios work correctly"
    echo "✅ Error scenarios are handled properly"
    echo "✅ Authentication and authorization work as expected"
    echo "✅ Data validation is functioning"
    exit 0
else
    echo ""
    echo "❌ Some tests failed. Please check the detailed results above."
    echo "💡 Review the test-results-*.json files for detailed failure information."
    exit 1
fi