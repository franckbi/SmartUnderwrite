# SmartUnderwrite MVP

A modular credit underwriting system with configurable rules engine built on .NET 8.

## Project Structure

```
SmartUnderwrite/
├── SmartUnderwrite.Api/              # Web API project
│   ├── Controllers/                  # API controllers
│   ├── Program.cs                    # Application entry point
│   └── appsettings.json             # Configuration
├── SmartUnderwrite.Core/             # Domain models and business logic
│   ├── Entities/                     # Domain entities
│   ├── Enums/                        # Enumerations
│   └── ValueObjects/                 # Value objects
├── SmartUnderwrite.Infrastructure/   # Data access and external services
│   └── Data/                         # Entity Framework DbContext
├── SmartUnderwrite.Tests/            # Unit tests
└── SmartUnderwrite.IntegrationTests/ # Integration tests
```

## Technology Stack

- **.NET 8** - Web API framework
- **Entity Framework Core** - ORM with PostgreSQL provider
- **ASP.NET Identity** - Authentication and authorization
- **Serilog** - Structured logging with correlation IDs
- **xUnit** - Testing framework
- **FluentAssertions** - Test assertions
- **Moq** - Mocking framework

## Features Implemented

### Task 1: Project Structure and Core Infrastructure ✅

- [x] Solution structure with API, Core, Infrastructure, and Test projects
- [x] .NET 8 Web API project with essential NuGet packages
- [x] Entity Framework Core with PostgreSQL provider
- [x] Serilog structured logging with correlation IDs
- [x] ASP.NET Identity configuration
- [x] Basic domain entities and value objects
- [x] Health check endpoints (/healthz, /readyz)

## Getting Started

### Prerequisites

- .NET 8 SDK
- PostgreSQL database

### Configuration

Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=smartunderwrite;Username=postgres;Password=postgres"
  }
}
```

### Running the Application

```bash
# Build the solution
dotnet build

# Run the API
dotnet run --project SmartUnderwrite.Api

# Run tests
dotnet test
```

### Health Checks

- **Health Check**: `GET /api/health/healthz`
- **Readiness Check**: `GET /api/health/readyz`

## Logging

The application uses Serilog for structured logging with:

- Console output for development
- File logging with daily rolling
- Correlation IDs for request tracing
- Configurable log levels

Logs are written to the `logs/` directory with the pattern `smartunderwrite-{date}.log`.

## Next Steps

This completes Task 1 of the implementation plan. The next tasks will involve:

1. Implementing core domain models and database schema
2. Setting up authentication and authorization
3. Building application services and business logic
4. Creating the rules engine
5. Implementing the REST API controllers

## Architecture

The solution follows Clean Architecture principles:

- **Core**: Contains domain entities, value objects, and business rules
- **Infrastructure**: Handles data persistence, external services, and cross-cutting concerns
- **API**: Provides HTTP endpoints and handles request/response mapping

The project is designed for testability, maintainability, and scalability while keeping the MVP scope focused and deliverable.
