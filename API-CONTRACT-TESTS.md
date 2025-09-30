# SmartUnderwrite API Contract Tests

This document describes the comprehensive API contract testing suite for the SmartUnderwrite system.

## Overview

The API contract tests validate that the SmartUnderwrite API:

- Conforms to the OpenAPI specification
- Handles happy path scenarios correctly
- Properly handles error conditions
- Enforces authentication and authorization
- Maintains data validation and business rules
- Provides proper audit trails

## Test Coverage

### 1. Health and Monitoring (2 tests)

- **Health Check** - Verifies system health endpoint
- **Readiness Check** - Verifies system readiness for traffic

### 2. Authentication and Authorization (4 tests)

- **Admin Login** - Successful admin authentication
- **Underwriter Login** - Successful underwriter authentication
- **Affiliate Login** - Successful affiliate authentication
- **Invalid Credentials** - Failed authentication handling

### 3. Application Management (8 tests)

- **Create Application** - Successful application submission
- **Get Applications** - List applications with pagination
- **Get Application by ID** - Retrieve specific application
- **Evaluate Application** - Automated decision processing
- **Manual Decision** - Underwriter decision override
- **Validation Errors** - Invalid application data handling
- **Unauthorized Access** - Missing authentication
- **Cross-Affiliate Access** - Data segregation enforcement

### 4. Rules Management (3 tests)

- **Get All Rules** - List business rules
- **Create Rule** - Add new business rule
- **Invalid Rule JSON** - Rule validation error handling

### 5. Audit and Compliance (1 test)

- **Get Audit Logs** - Compliance audit trail retrieval

## Test Execution

### Prerequisites

1. **SmartUnderwrite API** must be running on `http://localhost:5000`
2. **Database** must be seeded with test data
3. **Bruno CLI** must be installed: `npm install -g @usebruno/cli`

### Running Tests

#### Option 1: Full Test Suite

```bash
./run-api-tests.sh
```

#### Option 2: Individual Test Categories

```bash
npm run test:health
npm run test:auth
npm run test:applications
npm run test:rules
npm run test:audit
```

#### Option 3: Schema Validation Only

```bash
npm run validate:schema
```

### Test Environment

The tests use the `local` environment configuration:

- **Base URL**: `http://localhost:5000`
- **Test Users**: Seeded admin, underwriter, and affiliate accounts
- **Test Data**: Sample applications and rules

## OpenAPI Schema Validation

The test suite includes automated validation against the OpenAPI specification:

### Validated Endpoints

- ✅ `GET /api/Health/healthz` - Health check
- ✅ `GET /api/Health/readyz` - Readiness check
- ✅ `POST /api/Auth/login` - User authentication
- ✅ `GET /api/Applications` - List applications
- ✅ `POST /api/Applications` - Create application
- ✅ `GET /api/Applications/{id}` - Get application
- ✅ `POST /api/Applications/{id}/evaluate` - Evaluate application
- ✅ `POST /api/Applications/{id}/decision` - Manual decision
- ✅ `GET /api/Rules` - List rules
- ✅ `POST /api/Rules` - Create rule
- ✅ `GET /api/Audit` - Get audit logs

### Schema Compliance

- **Response Structure**: All responses match OpenAPI schema definitions
- **Status Codes**: HTTP status codes match documented responses
- **Data Types**: Response fields have correct data types
- **Required Fields**: All required fields are present in responses

## Test Scenarios

### Happy Path Scenarios

1. **Complete Application Workflow**

   - Affiliate logs in successfully
   - Creates new loan application
   - Application is evaluated automatically
   - Underwriter reviews and makes manual decision
   - All actions are logged in audit trail

2. **Rule Management Workflow**

   - Admin logs in successfully
   - Creates new business rule
   - Rule is validated and stored
   - Rule becomes available for evaluation

3. **System Health Monitoring**
   - Health check returns healthy status
   - Readiness check confirms system readiness
   - All dependencies are operational

### Error Scenarios

1. **Authentication Failures**

   - Invalid credentials are rejected
   - Missing authentication returns 401
   - Expired tokens are handled properly

2. **Authorization Violations**

   - Cross-affiliate data access is denied
   - Role-based permissions are enforced
   - Unauthorized operations return 403

3. **Data Validation Errors**

   - Invalid application data is rejected
   - Malformed rule definitions are rejected
   - Validation errors provide clear messages

4. **Business Rule Violations**
   - Applications below credit thresholds are rejected
   - Missing required fields trigger validation
   - Edge cases are handled gracefully

## Test Data Requirements

### Seeded Users

- **Admin**: `admin@smartunderwrite.com` / `Admin123!`
- **Underwriter**: `underwriter@smartunderwrite.com` / `Underwriter123!`
- **Affiliate**: `affiliate1@example.com` / `Affiliate123!`

### Test Applications

- Various risk profiles (low, medium, high)
- Different loan amounts and credit scores
- Complete applicant information
- Supporting documents

### Business Rules

- Credit score thresholds
- Income verification rules
- Loan amount limits
- Risk assessment criteria

## Continuous Integration

The API contract tests are designed to run in CI/CD pipelines:

### GitHub Actions Integration

```yaml
- name: Run API Contract Tests
  run: |
    npm install -g @usebruno/cli
    ./run-api-tests.sh
```

### Test Results

- **JSON Reports**: Detailed test results in JSON format
- **Exit Codes**: Non-zero exit code on test failures
- **Coverage Metrics**: Schema validation coverage reporting

## Troubleshooting

### Common Issues

1. **API Not Running**

   - Ensure SmartUnderwrite API is started
   - Check port 5000 is available
   - Verify database connection

2. **Authentication Failures**

   - Confirm test users are seeded
   - Check password requirements
   - Verify JWT configuration

3. **Test Data Issues**

   - Run database migrations
   - Execute seed data scripts
   - Check affiliate configurations

4. **Schema Validation Failures**
   - Regenerate OpenAPI specification
   - Update test expectations
   - Check controller annotations

### Debug Mode

Set environment variable for detailed logging:

```bash
export BRUNO_DEBUG=true
./run-api-tests.sh
```

## Maintenance

### Updating Tests

1. **New Endpoints**: Add corresponding Bruno test files
2. **Schema Changes**: Update OpenAPI validation script
3. **Test Data**: Modify seed data as needed
4. **Environment**: Update environment variables

### Best Practices

- Keep tests independent and idempotent
- Use descriptive test names and assertions
- Validate both success and error responses
- Include edge cases and boundary conditions
- Maintain test data consistency

## Reporting

Test results include:

- **Pass/Fail Status**: Individual test outcomes
- **Response Validation**: Schema compliance verification
- **Performance Metrics**: Response time measurements
- **Coverage Analysis**: API endpoint coverage
- **Audit Trail**: Test execution logging

The comprehensive test suite ensures the SmartUnderwrite API maintains high quality, reliability, and compliance with its contract specifications.
