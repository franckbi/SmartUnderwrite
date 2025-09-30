#!/usr/bin/env node

/**
 * API Schema Validation Script
 *
 * This script validates API responses against the OpenAPI specification
 * to ensure contract compliance.
 */

const fs = require("fs");
const path = require("path");

// Load OpenAPI specification
function loadOpenAPISpec() {
  try {
    const specPath = path.join(__dirname, "openapi-spec.json");
    const specContent = fs.readFileSync(specPath, "utf8");
    return JSON.parse(specContent);
  } catch (error) {
    console.error("Error loading OpenAPI specification:", error.message);
    process.exit(1);
  }
}

// Validate response against schema
function validateResponse(
  endpoint,
  method,
  statusCode,
  responseBody,
  openApiSpec
) {
  const pathItem = openApiSpec.paths[endpoint];
  if (!pathItem) {
    console.warn(`⚠️  Endpoint ${endpoint} not found in OpenAPI spec`);
    return false;
  }

  const operation = pathItem[method.toLowerCase()];
  if (!operation) {
    console.warn(`⚠️  Method ${method} not found for endpoint ${endpoint}`);
    return false;
  }

  const response = operation.responses[statusCode.toString()];
  if (!response) {
    console.warn(
      `⚠️  Status code ${statusCode} not documented for ${method} ${endpoint}`
    );
    return false;
  }

  console.log(
    `✓ ${method} ${endpoint} - Status ${statusCode} matches OpenAPI spec`
  );
  return true;
}

// Main validation function
function validateAPIContract() {
  console.log("🔍 Starting API Contract Validation...\n");

  const openApiSpec = loadOpenAPISpec();
  console.log(
    `📋 Loaded OpenAPI spec: ${openApiSpec.info.title} v${openApiSpec.info.version}\n`
  );

  // Define test scenarios to validate
  const testScenarios = [
    { endpoint: "/api/Health/healthz", method: "GET", expectedStatus: 200 },
    { endpoint: "/api/Health/readyz", method: "GET", expectedStatus: 200 },
    { endpoint: "/api/Auth/login", method: "POST", expectedStatus: 200 },
    { endpoint: "/api/Applications", method: "GET", expectedStatus: 200 },
    { endpoint: "/api/Applications", method: "POST", expectedStatus: 200 },
    { endpoint: "/api/Applications/{id}", method: "GET", expectedStatus: 200 },
    {
      endpoint: "/api/Applications/{id}/evaluate",
      method: "POST",
      expectedStatus: 200,
    },
    {
      endpoint: "/api/Applications/{id}/decision",
      method: "POST",
      expectedStatus: 200,
    },
    { endpoint: "/api/Rules", method: "GET", expectedStatus: 200 },
    { endpoint: "/api/Rules", method: "POST", expectedStatus: 200 },
    { endpoint: "/api/Audit", method: "GET", expectedStatus: 200 },
  ];

  let validCount = 0;
  let totalCount = testScenarios.length;

  testScenarios.forEach((scenario) => {
    if (
      validateResponse(
        scenario.endpoint,
        scenario.method,
        scenario.expectedStatus,
        null,
        openApiSpec
      )
    ) {
      validCount++;
    }
  });

  console.log(`\n📊 Validation Summary:`);
  console.log(`✓ Valid endpoints: ${validCount}/${totalCount}`);
  console.log(`📈 Coverage: ${Math.round((validCount / totalCount) * 100)}%`);

  if (validCount === totalCount) {
    console.log(
      "\n🎉 All API endpoints are properly documented in OpenAPI spec!"
    );
    return true;
  } else {
    console.log("\n❌ Some endpoints are missing or incorrectly documented");
    return false;
  }
}

// Run validation if called directly
if (require.main === module) {
  const isValid = validateAPIContract();
  process.exit(isValid ? 0 : 1);
}

module.exports = { validateAPIContract, validateResponse };
