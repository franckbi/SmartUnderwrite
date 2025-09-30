# SmartUnderwrite API Contract Tests

This collection contains comprehensive API contract tests for the SmartUnderwrite system, covering both happy path and error scenarios.

## Test Execution Order

The tests should be executed in the following order to ensure proper setup and data flow:

### 1. Health Checks

- `Health/Health Check.bru` - Verify API is healthy
- `Health/Readiness Check.bru` - Verify system is ready

### 2. Authentication Setup

- `Auth/Login Admin.bru` - Get admin token
- `Auth/Login Underwriter.bru` - Get underwriter token
- `Auth/Login Affiliate.bru` - Get affiliate token
- `Auth/Login Invalid Credentials.bru` - Test invalid login

### 3. Application Management (Happy Path)

- `Applications/Create Application.bru` - Create test application
- `Applications/Get Applications.bru` - List applications
- `Applications/Get Application by ID.bru` - Get specific application
- `Applications/Evaluate Application.bru` - Evaluate application
- `Applications/Manual Decision.bru` - Make manual decision

### 4. Application Management (Error Cases)

- `Applications/Create Application - Validation Error.bru` - Test validation
- `Applications/Unauthorized Access.bru` - Test unauthorized access
- `Applications/Cross-Affiliate Access Denied.bru` - Test data segregation

### 5. Rules Management

- `Rules/Get All Rules.bru` - List existing rules
- `Rules/Create Rule.bru` - Create new rule
- `Rules/Create Rule - Invalid JSON.bru` - Test rule validation

### 6. Audit and Compliance

- `Audit/Get Audit Logs.bru` - Verify audit trail

## Test Scenarios Coverage

### Happy Path Scenarios (6 tests)

1. **Health Check** - System health verification
2. **Authentication Flow** - Login for all user roles
3. **Application Submission** - Complete application creation
4. **Application Evaluation** - Automated decision making
5. **Manual Decision Override** - Underwriter manual decision
6. **Rule Management** - Admin rule configuration

### Error Scenarios (5 tests)

1. **Validation Errors** - Invalid application data
2. **Authentication Failures** - Invalid credentials
3. **Authorization Failures** - Unauthorized access attempts
4. **Data Segregation** - Cross-affiliate access prevention
5. **Rule Validation** - Invalid rule definitions

### Edge Cases (2 tests)

1. **Readiness Check** - System readiness verification
2. **Audit Trail** - Compliance logging verification

## Environment Variables

The following environment variables are used across tests:

- `baseUrl` - API base URL (default: http://localhost:5000)
- `adminToken` - Admin JWT token (set by login test)
- `underwriterToken` - Underwriter JWT token (set by login test)
- `affiliateToken` - Affiliate JWT token (set by login test)
- `testApplicationId` - Created application ID (set by create test)
- `testRuleId` - Created rule ID (set by rule creation test)

## OpenAPI Schema Validation

All test responses are validated against the OpenAPI specification located at `/openapi/v1.json`. The tests verify:

- Response status codes match expected values
- Response body structure matches schema definitions
- Required fields are present
- Data types are correct
- Business logic constraints are enforced

## Running the Tests

1. Ensure the SmartUnderwrite API is running on the configured base URL
2. Run the tests in the specified order using Bruno CLI or GUI
3. Check that all assertions pass and environment variables are properly set
4. Review audit logs to ensure all actions are properly tracked

## Test Data Requirements

The tests assume the following seed data exists:

- Admin user: admin@smartunderwrite.com / Admin123!
- Underwriter user: underwriter@smartunderwrite.com / Underwriter123!
- Affiliate user: affiliate1@example.com / Affiliate123!
- At least one active affiliate organization
- Basic rule set for application evaluation
