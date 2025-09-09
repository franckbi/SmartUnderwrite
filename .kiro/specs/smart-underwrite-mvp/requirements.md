# Requirements Document

## Introduction

SmartUnderwrite is a modular credit underwriting MVP designed to automate loan application processing through a configurable rules engine. The system ingests loan applications, evaluates them against business rules, issues decisions (approve/reject/manual review), maintains comprehensive audit trails, and provides an administrative portal for management. The solution is optimized for clarity, testability, and cloud deployment while serving affiliates, underwriters, and administrators through role-based access controls.

## Requirements

### Requirement 1

**User Story:** As an affiliate, I want to submit loan applications with applicant information and supporting documents, so that I can initiate the underwriting process for my customers.

#### Acceptance Criteria

1. WHEN an affiliate submits a loan application THEN the system SHALL capture applicant personal information (name, SSN hash, DOB, address, phone, email)
2. WHEN an affiliate submits a loan application THEN the system SHALL capture loan details (product type, amount, monthly income, employment type, credit score)
3. WHEN an affiliate uploads supporting documents THEN the system SHALL store them securely in S3/MinIO with proper metadata
4. WHEN a loan application is submitted THEN the system SHALL set the status to "Submitted" and create an audit log entry
5. IF required fields are missing THEN the system SHALL reject the submission with validation errors

### Requirement 2

**User Story:** As an underwriter, I want the system to automatically evaluate loan applications against configurable business rules, so that I can focus on edge cases requiring manual review.

#### Acceptance Criteria

1. WHEN a loan application is submitted for evaluation THEN the system SHALL execute all active rules in priority order
2. WHEN rules are evaluated THEN the system SHALL generate a decision outcome (Approve/Reject/ManualReview) with calculated score and reasons
3. WHEN evaluation is complete THEN the system SHALL persist the decision and update application status
4. IF credit score is below 550 THEN the system SHALL automatically reject with reason "Low credit score"
5. IF monthly income is zero or negative THEN the system SHALL flag for manual review with reason "No income provided"
6. IF loan amount exceeds $50,000 AND credit score is below 680 THEN the system SHALL flag for manual review with reason "High amount risk"

### Requirement 3

**User Story:** As an underwriter, I want to manually override automated decisions with justification, so that I can handle complex cases that require human judgment.

#### Acceptance Criteria

1. WHEN an underwriter reviews an application THEN the system SHALL display the automated decision with score and reasons
2. WHEN an underwriter makes a manual decision THEN the system SHALL require outcome selection (Approve/Reject/ManualReview) and justification reasons
3. WHEN a manual decision is submitted THEN the system SHALL update the application status and create a new decision record
4. WHEN a manual decision is made THEN the system SHALL log the action with underwriter ID and timestamp
5. IF an application is already in final status THEN the system SHALL prevent further decision changes

### Requirement 4

**User Story:** As an administrator, I want to manage business rules through a configuration interface, so that I can adapt underwriting criteria without code changes.

#### Acceptance Criteria

1. WHEN an administrator creates a rule THEN the system SHALL validate the JSON definition syntax
2. WHEN an administrator updates a rule THEN the system SHALL version the changes and maintain rule history
3. WHEN rules are modified THEN the system SHALL allow activation/deactivation without deletion
4. WHEN multiple rules exist THEN the system SHALL execute them in priority order
5. IF rule JSON is malformed THEN the system SHALL reject the update with specific validation errors

### Requirement 5

**User Story:** As an administrator, I want to manage affiliate organizations and their users, so that I can control access and maintain data segregation.

#### Acceptance Criteria

1. WHEN an administrator creates an affiliate THEN the system SHALL assign a unique identifier and external ID
2. WHEN affiliate users are created THEN the system SHALL associate them with their affiliate organization
3. WHEN affiliates access applications THEN the system SHALL restrict visibility to only their own submissions
4. WHEN an affiliate is deactivated THEN the system SHALL prevent new applications but preserve historical data
5. IF an affiliate user attempts to access another affiliate's data THEN the system SHALL deny access with authorization error

### Requirement 6

**User Story:** As a compliance officer, I want comprehensive audit trails of all system actions, so that I can demonstrate regulatory compliance and investigate issues.

#### Acceptance Criteria

1. WHEN any entity is created, updated, or deleted THEN the system SHALL create an audit log entry
2. WHEN decisions are made THEN the system SHALL log the actor (system or user), timestamp, and decision details
3. WHEN sensitive data is accessed THEN the system SHALL log the access without exposing PII
4. WHEN audit logs are queried THEN the system SHALL support filtering by entity type, entity ID, and date range
5. IF PII is logged THEN the system SHALL hash or mask sensitive information like SSN

### Requirement 7

**User Story:** As a system administrator, I want role-based authentication and authorization, so that users can only access features appropriate to their responsibilities.

#### Acceptance Criteria

1. WHEN users log in THEN the system SHALL authenticate against ASP.NET Identity and issue JWT tokens
2. WHEN API requests are made THEN the system SHALL validate JWT tokens and extract user roles
3. WHEN affiliates make requests THEN the system SHALL restrict access to their own organization's data
4. WHEN underwriters make requests THEN the system SHALL allow access to all applications for review
5. WHEN administrators make requests THEN the system SHALL allow full system access including user and rule management

### Requirement 8

**User Story:** As a developer, I want comprehensive API documentation and testing capabilities, so that I can integrate with and maintain the system effectively.

#### Acceptance Criteria

1. WHEN the API is deployed THEN the system SHALL expose OpenAPI/Swagger documentation at /swagger
2. WHEN endpoints are called THEN the system SHALL return consistent error responses with appropriate HTTP status codes
3. WHEN the system starts THEN the system SHALL provide health check endpoints (/healthz, /readyz)
4. WHEN requests are processed THEN the system SHALL include correlation IDs for tracing
5. IF the system is unhealthy THEN the health endpoints SHALL return appropriate failure status

### Requirement 9

**User Story:** As an operations team member, I want the system to be deployable via Infrastructure as Code, so that I can maintain consistent environments and enable CI/CD.

#### Acceptance Criteria

1. WHEN infrastructure is provisioned THEN Terraform SHALL create all required AWS resources (RDS, S3, App Runner/ECS, IAM)
2. WHEN the application is deployed THEN the system SHALL connect to managed PostgreSQL and S3 services
3. WHEN secrets are needed THEN the system SHALL retrieve them from AWS SSM Parameter Store
4. WHEN code is pushed to main branch THEN GitHub Actions SHALL build, test, and deploy automatically
5. IF deployment fails THEN the system SHALL maintain the previous working version

### Requirement 10

**User Story:** As a developer, I want comprehensive test coverage and local development capabilities, so that I can develop and debug efficiently.

#### Acceptance Criteria

1. WHEN tests are run THEN the system SHALL achieve at least 80% code coverage on rules engine components
2. WHEN integration tests execute THEN the system SHALL test complete workflows from application submission to decision
3. WHEN local development starts THEN Docker Compose SHALL provide all dependencies (PostgreSQL, MinIO)
4. WHEN the development environment runs THEN the system SHALL support hot reload for both API and frontend
5. IF tests fail THEN the CI pipeline SHALL prevent deployment to production
