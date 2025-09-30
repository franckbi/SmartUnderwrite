# SmartUnderwrite MVP

A cloud-native credit underwriting system with configurable rules engine, built on .NET 8 with React frontend. The system automates loan application processing through business rules, provides comprehensive audit trails, and supports role-based access for affiliates, underwriters, and administrators.

## 🏗️ Architecture Overview

SmartUnderwrite follows Clean Architecture principles with clear separation of concerns:

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   React SPA     │    │  .NET 8 Web API │    │   PostgreSQL    │
│   (Frontend)    │◄──►│   (Backend)     │◄──►│   (Database)    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                                │
                                ▼
                       ┌─────────────────┐
                       │   MinIO/S3      │
                       │  (Documents)    │
                       └─────────────────┘
```

### Key Components

- **Frontend**: React TypeScript SPA with Material-UI
- **Backend**: .NET 8 Web API with Entity Framework Core
- **Database**: PostgreSQL with comprehensive audit logging
- **Storage**: MinIO/S3 for document management
- **Authentication**: JWT with ASP.NET Identity
- **Rules Engine**: JSON-configurable business rules with expression compilation

## 🚀 Quick Start

### Prerequisites

- **Docker & Docker Compose** (recommended)
- **.NET 8 SDK** (for local development)
- **Node.js 18+** (for frontend development)
- **PostgreSQL 15+** (if running without Docker)

### Option 1: Docker Compose (Recommended)

```bash
# Clone the repository
git clone <repository-url>
cd SmartUnderwrite

# Start all services
make up

# Seed the database with test data
make seed

# Verify everything is working
make verify
```

### Option 2: Local Development

```bash
# Start dependencies only
docker-compose up -d postgres minio

# Run database migrations
make migrate

# Seed test data
make seed

# Start the API
dotnet run --project SmartUnderwrite.Api --urls="http://localhost:8080"

# Start the frontend (in another terminal)
cd SmartUnderwrite.Frontend
npm install
npm run dev
```

### Access Points

- **Frontend**: http://localhost:3000
- **API**: http://localhost:8080
- **Swagger UI**: http://localhost:8080/swagger
- **MinIO Console**: http://localhost:9001 (minioadmin/minioadmin123)

### Test Credentials

| Role        | Email                           | Password      |
| ----------- | ------------------------------- | ------------- |
| Admin       | admin@smartunderwrite.com       | Admin123!     |
| Underwriter | underwriter@smartunderwrite.com | Under123!     |
| Affiliate 1 | affiliate1@pfp001.com           | Affiliate123! |
| Affiliate 2 | affiliate2@pfp002.com           | Affiliate123! |

## 📋 Features

### ✅ Completed Features

- **User Management**: Role-based authentication (Admin, Underwriter, Affiliate)
- **Application Processing**: Complete loan application lifecycle
- **Rules Engine**: JSON-configurable business rules with real-time evaluation
- **Decision Management**: Automated and manual decision workflows
- **Document Management**: Secure file upload/download with S3 integration
- **Audit Logging**: Comprehensive audit trails for compliance
- **API Documentation**: OpenAPI/Swagger with contract testing
- **Frontend Portal**: React SPA with role-based interfaces
- **Infrastructure**: Docker containerization and AWS deployment ready

### 🎯 Key Capabilities

1. **Automated Underwriting**: Configurable rules evaluate applications instantly
2. **Manual Override**: Underwriters can review and override automated decisions
3. **Data Segregation**: Affiliates can only access their own applications
4. **Comprehensive Auditing**: All actions logged for compliance and investigation
5. **Document Management**: Secure storage and retrieval of supporting documents
6. **Performance**: Rules engine handles concurrent evaluations efficiently
7. **Security**: JWT authentication, role-based authorization, input validation

## 🛠️ Development

### Project Structure

```
SmartUnderwrite/
├── SmartUnderwrite.Api/              # Web API (.NET 8)
│   ├── Controllers/                  # API endpoints
│   ├── Services/                     # Business logic services
│   ├── Models/                       # DTOs and request/response models
│   └── Middleware/                   # Cross-cutting concerns
├── SmartUnderwrite.Core/             # Domain layer
│   ├── Entities/                     # Domain entities
│   ├── RulesEngine/                  # Business rules engine
│   └── ValueObjects/                 # Value objects
├── SmartUnderwrite.Infrastructure/   # Data access layer
│   ├── Data/                         # Entity Framework DbContext
│   └── Repositories/                 # Data repositories
├── SmartUnderwrite.Frontend/         # React SPA
│   ├── src/components/               # React components
│   ├── src/services/                 # API client services
│   └── src/types/                    # TypeScript type definitions
├── SmartUnderwrite.Tests/            # Unit tests
├── SmartUnderwrite.IntegrationTests/ # Integration tests
├── api-tests/                        # API contract tests (Bruno)
├── scripts/                          # Development and deployment scripts
└── terraform/                       # Infrastructure as Code
```

### Available Commands

```bash
# Development
make dev-up          # Start development environment with hot reload
make dev-down        # Stop development environment
make logs            # View service logs
make reset           # Reset database and reseed

# Testing
make test            # Run unit and integration tests
make validate-all    # Run complete validation suite
make validate-e2e    # Run end-to-end workflow tests
make validate-security # Run security validation tests
make validate-audit  # Run audit logging tests
make validate-load   # Run rules engine load tests

# Database
make migrate         # Run database migrations
make seed            # Seed database with test data

# Utilities
make verify          # Verify system setup
make clean           # Clean up Docker resources
```

### Running Tests

```bash
# Unit tests
dotnet test SmartUnderwrite.Tests/

# Integration tests
dotnet test SmartUnderwrite.IntegrationTests/

# API contract tests
./run-api-tests.sh

# Complete validation suite
make validate-all
```

## 🔧 Configuration

### Environment Variables

| Variable                               | Description                          | Default              |
| -------------------------------------- | ------------------------------------ | -------------------- |
| `ASPNETCORE_ENVIRONMENT`               | Environment (Development/Production) | Development          |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string         | See appsettings.json |
| `JwtSettings__SecretKey`               | JWT signing key                      | Generated            |
| `JwtSettings__Issuer`                  | JWT issuer                           | SmartUnderwrite      |
| `MinioSettings__Endpoint`              | MinIO/S3 endpoint                    | localhost:9000       |
| `MinioSettings__AccessKey`             | MinIO/S3 access key                  | minioadmin           |
| `MinioSettings__SecretKey`             | MinIO/S3 secret key                  | minioadmin123        |

### Database Configuration

The system uses PostgreSQL with Entity Framework Core. Connection strings are configured in:

- **Development**: `appsettings.Development.json`
- **Production**: Environment variables or AWS Parameter Store

### Rules Engine Configuration

Business rules are stored in the database as JSON documents. Example rule:

```json
{
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
}
```

## 🚀 Deployment

### Local Development

Use Docker Compose for the complete development environment:

```bash
make dev-up    # Start with hot reload
make verify    # Verify everything is working
```

### AWS Production Deployment

The system includes Terraform configurations for AWS deployment:

```bash
cd terraform
terraform init
terraform plan
terraform apply
```

**AWS Resources Created:**

- **App Runner**: API hosting with auto-scaling
- **RDS PostgreSQL**: Managed database
- **S3**: Document storage
- **CloudWatch**: Logging and monitoring
- **IAM**: Security roles and policies
- **SSM Parameter Store**: Secrets management

### CI/CD Pipeline

GitHub Actions workflow handles:

- Automated testing on pull requests
- Docker image building and pushing
- Infrastructure provisioning with Terraform
- Automated deployment with approval gates

## 📊 Monitoring and Observability

### Health Checks

- **Health**: `GET /api/health/healthz` - Basic health status
- **Readiness**: `GET /api/health/readyz` - Ready to serve traffic

### Logging

Structured logging with Serilog includes:

- **Correlation IDs**: Request tracing across services
- **Audit Events**: All business actions logged
- **Performance Metrics**: Response times and throughput
- **Error Tracking**: Detailed error information

### Metrics

Key metrics monitored:

- API response times and error rates
- Rules engine evaluation performance
- Database query performance
- Authentication success/failure rates

## 🔒 Security

### Authentication & Authorization

- **JWT Tokens**: Stateless authentication with role-based claims
- **Role-Based Access**: Admin, Underwriter, Affiliate roles
- **Data Segregation**: Affiliates can only access their own data
- **Token Expiration**: Short-lived tokens with refresh capability

### Security Features

- **Input Validation**: Comprehensive validation on all endpoints
- **SQL Injection Protection**: Parameterized queries with EF Core
- **XSS Protection**: Input sanitization and output encoding
- **CORS Configuration**: Restricted cross-origin requests
- **Rate Limiting**: Protection against abuse
- **Security Headers**: OWASP recommended headers

### Compliance

- **Audit Trails**: Complete audit logging for regulatory compliance
- **PII Protection**: Sensitive data hashing and masking
- **Data Retention**: Configurable retention policies
- **Access Logging**: All data access logged and monitored

## 📚 API Documentation

### OpenAPI Specification

Complete API documentation available at:

- **Swagger UI**: http://localhost:8080/swagger
- **OpenAPI JSON**: http://localhost:8080/swagger/v1/swagger.json

### Key Endpoints

| Endpoint                          | Method   | Description            | Auth Required |
| --------------------------------- | -------- | ---------------------- | ------------- |
| `/api/auth/login`                 | POST     | User authentication    | No            |
| `/api/applications`               | GET/POST | Application management | Yes           |
| `/api/applications/{id}/evaluate` | POST     | Evaluate application   | Underwriter   |
| `/api/applications/{id}/decision` | POST     | Manual decision        | Underwriter   |
| `/api/rules`                      | GET/POST | Rule management        | Admin         |
| `/api/audit`                      | GET      | Audit logs             | Admin         |

### Authentication

All protected endpoints require JWT token in Authorization header:

```bash
curl -H "Authorization: Bearer <token>" \
     http://localhost:8080/api/applications
```

## 🤝 Contributing

### Development Workflow

1. **Setup**: `make dev-up && make seed`
2. **Develop**: Make changes with hot reload
3. **Test**: `make test && make validate-all`
4. **Commit**: Follow conventional commit messages
5. **Deploy**: CI/CD handles deployment

### Code Quality

- **Unit Tests**: Minimum 80% coverage on business logic
- **Integration Tests**: End-to-end workflow testing
- **Code Analysis**: Static analysis with SonarQube
- **Security Scanning**: Automated vulnerability scanning

## 📞 Support

### Troubleshooting

Common issues and solutions:

1. **Services not starting**: Check Docker is running and ports are available
2. **Database connection errors**: Verify PostgreSQL is running and accessible
3. **Authentication failures**: Check JWT configuration and user seeding
4. **API errors**: Check logs with `make logs`

### Getting Help

- **Documentation**: Check the `/docs` directory for detailed guides
- **Issues**: Create GitHub issues for bugs and feature requests
- **Discussions**: Use GitHub Discussions for questions

### Useful Resources

- [API Contract Tests](API-CONTRACT-TESTS.md)
- [Docker Setup](DOCKER.md)
- [Deployment Guide](DEPLOYMENT.md)
- [Development Data](DEVELOPMENT-DATA.md)

---

**SmartUnderwrite MVP** - Built with ❤️ for modern credit underwriting
