# SmartUnderwrite API Documentation

Complete API reference for the SmartUnderwrite system with authentication requirements, request/response examples, and error handling.

## Table of Contents

- [Authentication](#authentication)
- [Error Handling](#error-handling)
- [Rate Limiting](#rate-limiting)
- [Endpoints](#endpoints)
  - [Authentication](#authentication-endpoints)
  - [Applications](#application-endpoints)
  - [Decisions](#decision-endpoints)
  - [Rules](#rule-endpoints)
  - [Audit](#audit-endpoints)
  - [Health](#health-endpoints)

## Authentication

SmartUnderwrite uses JWT (JSON Web Token) authentication. All protected endpoints require a valid JWT token in the Authorization header.

### Getting a Token

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}
```

**Response:**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh_token_here",
  "expiresAt": "2024-01-01T12:00:00Z",
  "user": {
    "id": 1,
    "email": "user@example.com",
    "roles": ["Affiliate"],
    "affiliateId": 1
  }
}
```

### Using the Token

Include the token in the Authorization header for all protected requests:

```http
GET /api/applications
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Token Refresh

```http
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "refresh_token_here"
}
```

### User Roles

| Role            | Description          | Permissions                                              |
| --------------- | -------------------- | -------------------------------------------------------- |
| **Admin**       | System administrator | Full system access, user management, rule management     |
| **Underwriter** | Loan underwriter     | View all applications, make decisions, access audit logs |
| **Affiliate**   | Partner organization | Create applications, view own applications only          |

## Error Handling

The API uses standard HTTP status codes and returns consistent error responses.

### Error Response Format

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "One or more validation errors occurred",
    "details": {
      "Email": ["The Email field is required"],
      "Amount": ["Amount must be greater than 0"]
    },
    "correlationId": "12345678-1234-1234-1234-123456789012"
  }
}
```

### HTTP Status Codes

| Code | Description           | When Used                                           |
| ---- | --------------------- | --------------------------------------------------- |
| 200  | OK                    | Successful GET, PUT requests                        |
| 201  | Created               | Successful POST requests                            |
| 400  | Bad Request           | Invalid request data, validation errors             |
| 401  | Unauthorized          | Missing or invalid authentication token             |
| 403  | Forbidden             | Insufficient permissions for the requested resource |
| 404  | Not Found             | Resource not found or access denied                 |
| 409  | Conflict              | Resource conflict (e.g., duplicate email)           |
| 429  | Too Many Requests     | Rate limit exceeded                                 |
| 500  | Internal Server Error | Unexpected server error                             |

### Common Error Codes

| Error Code                | Description                            |
| ------------------------- | -------------------------------------- |
| `VALIDATION_ERROR`        | Request validation failed              |
| `UNAUTHORIZED_ACCESS`     | User lacks permission for the resource |
| `RESOURCE_NOT_FOUND`      | Requested resource does not exist      |
| `BUSINESS_RULE_VIOLATION` | Business rule validation failed        |
| `RATE_LIMIT_EXCEEDED`     | Too many requests from client          |

## Rate Limiting

The API implements rate limiting to prevent abuse:

- **Authenticated requests**: 1000 requests per hour per user
- **Unauthenticated requests**: 100 requests per hour per IP
- **Login attempts**: 5 attempts per 15 minutes per IP

Rate limit headers are included in responses:

```http
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1640995200
```

## Endpoints

### Authentication Endpoints

#### Login

Authenticate a user and receive a JWT token.

```http
POST /api/auth/login
```

**Request Body:**

```json
{
  "email": "affiliate1@pfp001.com",
  "password": "Affiliate123!"
}
```

**Response (200 OK):**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh_token_here",
  "expiresAt": "2024-01-01T12:00:00Z",
  "user": {
    "id": 3,
    "email": "affiliate1@pfp001.com",
    "firstName": "Affiliate",
    "lastName": "User",
    "roles": ["Affiliate"],
    "affiliateId": 1
  }
}
```

**Error Response (401 Unauthorized):**

```json
{
  "error": {
    "code": "INVALID_CREDENTIALS",
    "message": "Invalid email or password",
    "correlationId": "12345678-1234-1234-1234-123456789012"
  }
}
```

#### Refresh Token

Refresh an expired JWT token.

```http
POST /api/auth/refresh
```

**Request Body:**

```json
{
  "refreshToken": "refresh_token_here"
}
```

**Response (200 OK):**

```json
{
  "token": "new_jwt_token_here",
  "refreshToken": "new_refresh_token_here",
  "expiresAt": "2024-01-01T13:00:00Z"
}
```

#### Register (Admin Only)

Register a new user in the system.

```http
POST /api/auth/register
Authorization: Bearer <admin_token>
```

**Request Body:**

```json
{
  "email": "newuser@example.com",
  "password": "SecurePassword123!",
  "firstName": "John",
  "lastName": "Doe",
  "role": "Affiliate",
  "affiliateId": 1
}
```

### Application Endpoints

#### List Applications

Get a paginated list of loan applications. Affiliates see only their applications, underwriters and admins see all.

```http
GET /api/applications?page=1&pageSize=10&status=Submitted&affiliateId=1
Authorization: Bearer <token>
```

**Query Parameters:**

- `page` (optional): Page number (default: 1)
- `pageSize` (optional): Items per page (default: 10, max: 100)
- `status` (optional): Filter by status (Submitted, Evaluated, Approved, Rejected)
- `affiliateId` (optional): Filter by affiliate (admin/underwriter only)
- `fromDate` (optional): Filter applications from date (ISO 8601)
- `toDate` (optional): Filter applications to date (ISO 8601)

**Response (200 OK):**

```json
{
  "items": [
    {
      "id": 1,
      "applicant": {
        "firstName": "John",
        "lastName": "Doe",
        "email": "john.doe@example.com",
        "phone": "555-0123",
        "dateOfBirth": "1985-06-15T00:00:00Z"
      },
      "productType": "Personal Loan",
      "amount": 25000,
      "incomeMonthly": 5000,
      "employmentType": "Full-time",
      "creditScore": 720,
      "status": "Submitted",
      "createdAt": "2024-01-01T10:00:00Z",
      "affiliate": {
        "id": 1,
        "name": "Premier Financial Partners",
        "externalId": "PFP001"
      }
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

#### Get Application

Get a specific loan application by ID.

```http
GET /api/applications/{id}
Authorization: Bearer <token>
```

**Response (200 OK):**

```json
{
  "id": 1,
  "applicant": {
    "id": 1,
    "firstName": "John",
    "lastName": "Doe",
    "email": "john.doe@example.com",
    "phone": "555-0123",
    "dateOfBirth": "1985-06-15T00:00:00Z",
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
  "creditScore": 720,
  "status": "Evaluated",
  "createdAt": "2024-01-01T10:00:00Z",
  "evaluatedAt": "2024-01-01T10:05:00Z",
  "affiliate": {
    "id": 1,
    "name": "Premier Financial Partners",
    "externalId": "PFP001"
  },
  "documents": [
    {
      "id": 1,
      "fileName": "income_statement.pdf",
      "fileSize": 1024000,
      "contentType": "application/pdf",
      "uploadedAt": "2024-01-01T10:02:00Z"
    }
  ],
  "decisions": [
    {
      "id": 1,
      "outcome": "Approve",
      "score": 750,
      "reasons": ["Good credit score", "Stable income"],
      "decidedAt": "2024-01-01T10:05:00Z",
      "decidedBy": "system"
    }
  ]
}
```

#### Create Application

Create a new loan application.

```http
POST /api/applications
Authorization: Bearer <affiliate_token>
Content-Type: application/json
```

**Request Body:**

```json
{
  "applicant": {
    "firstName": "Jane",
    "lastName": "Smith",
    "email": "jane.smith@example.com",
    "phone": "555-0124",
    "dateOfBirth": "1990-03-20T00:00:00Z",
    "ssn": "987654321",
    "address": {
      "street": "456 Oak Ave",
      "city": "Somewhere",
      "state": "NY",
      "zipCode": "54321"
    }
  },
  "productType": "Auto Loan",
  "amount": 35000,
  "incomeMonthly": 6500,
  "employmentType": "Full-time",
  "creditScore": 680
}
```

**Response (201 Created):**

```json
{
  "id": 2,
  "applicant": {
    "id": 2,
    "firstName": "Jane",
    "lastName": "Smith",
    "email": "jane.smith@example.com",
    "phone": "555-0124",
    "dateOfBirth": "1990-03-20T00:00:00Z",
    "address": {
      "street": "456 Oak Ave",
      "city": "Somewhere",
      "state": "NY",
      "zipCode": "54321"
    }
  },
  "productType": "Auto Loan",
  "amount": 35000,
  "incomeMonthly": 6500,
  "employmentType": "Full-time",
  "creditScore": 680,
  "status": "Submitted",
  "createdAt": "2024-01-01T11:00:00Z",
  "affiliate": {
    "id": 1,
    "name": "Premier Financial Partners",
    "externalId": "PFP001"
  }
}
```

#### Upload Document

Upload a supporting document for an application.

```http
POST /api/applications/{id}/documents
Authorization: Bearer <token>
Content-Type: multipart/form-data

file: <binary_file_data>
```

**Response (201 Created):**

```json
{
  "id": 2,
  "fileName": "pay_stub.pdf",
  "fileSize": 512000,
  "contentType": "application/pdf",
  "uploadedAt": "2024-01-01T11:05:00Z",
  "downloadUrl": "/api/applications/2/documents/2"
}
```

#### Download Document

Download a supporting document.

```http
GET /api/applications/{applicationId}/documents/{documentId}
Authorization: Bearer <token>
```

**Response (200 OK):**

- Content-Type: application/pdf (or appropriate MIME type)
- Content-Disposition: attachment; filename="document.pdf"
- Binary file content

### Decision Endpoints

#### Evaluate Application

Evaluate an application using the rules engine.

```http
POST /api/applications/{id}/evaluate
Authorization: Bearer <underwriter_token>
```

**Response (200 OK):**

```json
{
  "id": 3,
  "outcome": "ManualReview",
  "score": 650,
  "reasons": [
    "High loan amount requires manual review",
    "Credit score below threshold for auto-approval"
  ],
  "decidedAt": "2024-01-01T12:00:00Z",
  "decidedBy": "system",
  "rulesApplied": [
    {
      "ruleId": 1,
      "ruleName": "High Amount Review",
      "result": "MANUAL"
    },
    {
      "ruleId": 2,
      "ruleName": "Credit Score Check",
      "result": "MANUAL"
    }
  ]
}
```

#### Manual Decision

Make a manual decision on an application.

```http
POST /api/applications/{id}/decision
Authorization: Bearer <underwriter_token>
Content-Type: application/json
```

**Request Body:**

```json
{
  "outcome": "Approve",
  "reasons": ["Applicant provided additional income verification"],
  "justification": "After reviewing additional documentation, applicant meets all criteria for approval."
}
```

**Response (201 Created):**

```json
{
  "id": 4,
  "outcome": "Approve",
  "score": 650,
  "reasons": ["Applicant provided additional income verification"],
  "justification": "After reviewing additional documentation, applicant meets all criteria for approval.",
  "decidedAt": "2024-01-01T12:30:00Z",
  "decidedBy": {
    "id": 2,
    "email": "underwriter@smartunderwrite.com",
    "firstName": "Under",
    "lastName": "Writer"
  }
}
```

### Rule Endpoints

#### List Rules

Get all business rules (Admin only).

```http
GET /api/rules?isActive=true
Authorization: Bearer <admin_token>
```

**Query Parameters:**

- `isActive` (optional): Filter by active status (true/false)

**Response (200 OK):**

```json
{
  "items": [
    {
      "id": 1,
      "name": "Basic Credit Check",
      "description": "Basic credit score and income validation",
      "definition": {
        "name": "Basic Credit Check",
        "priority": 10,
        "clauses": [
          {
            "if": "CreditScore < 550",
            "then": "REJECT",
            "reason": "Low credit score"
          },
          {
            "if": "IncomeMonthly <= 0",
            "then": "MANUAL",
            "reason": "No income provided"
          }
        ]
      },
      "isActive": true,
      "createdAt": "2024-01-01T09:00:00Z",
      "updatedAt": "2024-01-01T09:00:00Z"
    }
  ]
}
```

#### Create Rule

Create a new business rule (Admin only).

```http
POST /api/rules
Authorization: Bearer <admin_token>
Content-Type: application/json
```

**Request Body:**

```json
{
  "name": "High Amount Check",
  "description": "Additional validation for high loan amounts",
  "definition": {
    "name": "High Amount Check",
    "priority": 20,
    "clauses": [
      {
        "if": "Amount > 50000 && CreditScore < 700",
        "then": "MANUAL",
        "reason": "High amount with moderate credit score"
      }
    ]
  },
  "isActive": true
}
```

**Response (201 Created):**

```json
{
  "id": 3,
  "name": "High Amount Check",
  "description": "Additional validation for high loan amounts",
  "definition": {
    "name": "High Amount Check",
    "priority": 20,
    "clauses": [
      {
        "if": "Amount > 50000 && CreditScore < 700",
        "then": "MANUAL",
        "reason": "High amount with moderate credit score"
      }
    ]
  },
  "isActive": true,
  "createdAt": "2024-01-01T13:00:00Z",
  "updatedAt": "2024-01-01T13:00:00Z"
}
```

#### Update Rule

Update an existing business rule (Admin only).

```http
PUT /api/rules/{id}
Authorization: Bearer <admin_token>
Content-Type: application/json
```

**Request Body:**

```json
{
  "name": "Updated High Amount Check",
  "description": "Updated validation for high loan amounts",
  "definition": {
    "name": "Updated High Amount Check",
    "priority": 25,
    "clauses": [
      {
        "if": "Amount > 40000 && CreditScore < 680",
        "then": "MANUAL",
        "reason": "High amount with moderate credit score - updated threshold"
      }
    ]
  },
  "isActive": true
}
```

#### Activate/Deactivate Rule

Toggle rule active status (Admin only).

```http
PATCH /api/rules/{id}/status
Authorization: Bearer <admin_token>
Content-Type: application/json
```

**Request Body:**

```json
{
  "isActive": false
}
```

### Audit Endpoints

#### Get Audit Logs

Retrieve audit logs for compliance and investigation (Admin only).

```http
GET /api/audit?entityType=LoanApplication&action=Created&fromDate=2024-01-01&toDate=2024-01-02&page=1&pageSize=50
Authorization: Bearer <admin_token>
```

**Query Parameters:**

- `entityType` (optional): Filter by entity type (LoanApplication, Decision, Rule, User)
- `entityId` (optional): Filter by specific entity ID
- `action` (optional): Filter by action (Created, Updated, Deleted, Login, etc.)
- `userId` (optional): Filter by user who performed the action
- `fromDate` (optional): Filter from date (ISO 8601)
- `toDate` (optional): Filter to date (ISO 8601)
- `page` (optional): Page number (default: 1)
- `pageSize` (optional): Items per page (default: 50, max: 100)

**Response (200 OK):**

```json
{
  "items": [
    {
      "id": 1,
      "entityType": "LoanApplication",
      "entityId": "1",
      "action": "Created",
      "userId": 3,
      "userName": "affiliate1@pfp001.com",
      "timestamp": "2024-01-01T10:00:00Z",
      "correlationId": "12345678-1234-1234-1234-123456789012",
      "changes": {
        "Amount": "25000",
        "Status": "Submitted"
      },
      "ipAddress": "192.168.1.100",
      "userAgent": "Mozilla/5.0..."
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 50,
  "totalPages": 1
}
```

### Health Endpoints

#### Health Check

Basic health check endpoint.

```http
GET /api/health/healthz
```

**Response (200 OK):**

```json
{
  "status": "Healthy",
  "timestamp": "2024-01-01T12:00:00Z",
  "version": "1.0.0"
}
```

#### Readiness Check

Readiness check including dependencies.

```http
GET /api/health/readyz
```

**Response (200 OK):**

```json
{
  "status": "Ready",
  "timestamp": "2024-01-01T12:00:00Z",
  "checks": {
    "database": "Healthy",
    "storage": "Healthy"
  }
}
```

## SDK Examples

### JavaScript/TypeScript

```typescript
// API Client setup
class SmartUnderwriteClient {
  private baseUrl: string;
  private token: string | null = null;

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl;
  }

  async login(email: string, password: string) {
    const response = await fetch(`${this.baseUrl}/api/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email, password }),
    });

    if (response.ok) {
      const data = await response.json();
      this.token = data.token;
      return data;
    }
    throw new Error("Login failed");
  }

  async createApplication(applicationData: any) {
    const response = await fetch(`${this.baseUrl}/api/applications`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${this.token}`,
      },
      body: JSON.stringify(applicationData),
    });

    if (response.ok) {
      return await response.json();
    }
    throw new Error("Application creation failed");
  }
}

// Usage
const client = new SmartUnderwriteClient("http://localhost:8080");
await client.login("affiliate1@pfp001.com", "Affiliate123!");
const application = await client.createApplication({
  applicant: {
    firstName: "John",
    lastName: "Doe",
    // ... other fields
  },
  // ... other application data
});
```

### C#

```csharp
// API Client setup
public class SmartUnderwriteClient
{
    private readonly HttpClient _httpClient;
    private string? _token;

    public SmartUnderwriteClient(string baseUrl)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public async Task<LoginResponse> LoginAsync(string email, string password)
    {
        var request = new LoginRequest { Email = email, Password = password };
        var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);

        if (response.IsSuccessStatusCode)
        {
            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
            _token = loginResponse.Token;
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
            return loginResponse;
        }

        throw new Exception("Login failed");
    }

    public async Task<LoanApplicationDto> CreateApplicationAsync(CreateApplicationRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/applications", request);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<LoanApplicationDto>();
        }

        throw new Exception("Application creation failed");
    }
}
```

## Webhooks (Future Enhancement)

The API is designed to support webhooks for real-time notifications:

```json
{
  "event": "application.evaluated",
  "timestamp": "2024-01-01T12:00:00Z",
  "data": {
    "applicationId": 1,
    "outcome": "Approve",
    "score": 750
  }
}
```

## Versioning

The API uses URL versioning:

- Current version: `/api/v1/`
- Future versions: `/api/v2/`, etc.

Version headers are also supported:

```http
Accept: application/vnd.smartunderwrite.v1+json
```

---

For more information, see the [OpenAPI specification](http://localhost:8080/swagger) or contact the development team.
