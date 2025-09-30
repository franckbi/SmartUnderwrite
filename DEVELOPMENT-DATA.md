# SmartUnderwrite Development Data

This document describes the test data seeded into the development database for SmartUnderwrite.

## Overview

The development database is automatically seeded with realistic test data to facilitate development and testing. The data includes:

- **3 Affiliates** with different configurations
- **5 Test Users** across all roles
- **30 Loan Applications** with varied risk profiles
- **2 Default Rules** covering basic underwriting scenarios

## Test Users

### Administrator

- **Email**: `admin@smartunderwrite.com`
- **Password**: `Admin123!`
- **Role**: Admin
- **Permissions**: Full system access, user management, rule management, audit logs

### Underwriter

- **Email**: `underwriter@smartunderwrite.com`
- **Password**: `Under123!`
- **Role**: Underwriter
- **Permissions**: Review all applications, make manual decisions, view audit logs

### Affiliate Users

#### Affiliate 1 - Premier Financial Partners

- **Email**: `affiliate1@pfp001.com`
- **Password**: `Affiliate123!`
- **Role**: Affiliate
- **Affiliate**: Premier Financial Partners (PFP001)
- **Permissions**: Submit applications, view own applications only

#### Affiliate 2 - Coastal Credit Solutions

- **Email**: `affiliate2@ccs002.com`
- **Password**: `Affiliate123!`
- **Role**: Affiliate
- **Affiliate**: Coastal Credit Solutions (CCS002)
- **Permissions**: Submit applications, view own applications only

#### Affiliate 3 - Mountain View Lending

- **Email**: `affiliate3@mvl003.com`
- **Password**: `Affiliate123!`
- **Role**: Affiliate
- **Affiliate**: Mountain View Lending (MVL003)
- **Permissions**: Submit applications, view own applications only

## Affiliates

### 1. Premier Financial Partners

- **External ID**: PFP001
- **Status**: Active
- **Focus**: High-volume personal loans
- **Typical Applications**: 10-12 applications in seed data

### 2. Coastal Credit Solutions

- **External ID**: CCS002
- **Status**: Active
- **Focus**: Auto loans and debt consolidation
- **Typical Applications**: 9-10 applications in seed data

### 3. Mountain View Lending

- **External ID**: MVL003
- **Status**: Active
- **Focus**: Home improvement and business loans
- **Typical Applications**: 8-9 applications in seed data

## Loan Applications

The system seeds 30 loan applications with the following distribution:

### Risk Profile Distribution

- **Low Risk (40% - 12 applications)**

  - Credit Score: 700-850
  - Income: $6,000-$12,000/month
  - Employment: Primarily Full-Time
  - Loan Amount: $5,000-$25,000
  - Status: Mostly Approved/Evaluated

- **Medium Risk (35% - 10 applications)**

  - Credit Score: 600-720
  - Income: $4,000-$8,000/month
  - Employment: Mix of Full-Time, Part-Time, Self-Employed
  - Loan Amount: $25,000-$50,000
  - Status: Manual Review/In Review

- **High Risk (25% - 8 applications)**
  - Credit Score: 450-620
  - Income: $2,000-$5,000/month
  - Employment: Part-Time, Self-Employed, Unemployed
  - Loan Amount: $50,000-$100,000
  - Status: Rejected/Manual Review

### Product Types

- Personal Loan
- Auto Loan
- Home Improvement
- Debt Consolidation
- Business Loan

### Application Statuses

- **Submitted**: New applications awaiting evaluation
- **In Review**: Applications being processed
- **Evaluated**: Completed automatic evaluation
- **Manual Review**: Flagged for underwriter review
- **Approved**: Approved applications
- **Rejected**: Rejected applications

## Business Rules

### 1. Basic Credit & DTI (Priority 10)

```json
{
  "name": "Basic Credit & DTI",
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
    },
    {
      "if": "Amount > 50000 && CreditScore < 680",
      "then": "MANUAL",
      "reason": "High amount risk"
    }
  ],
  "score": {
    "base": 600,
    "add": [
      {
        "when": "CreditScore >= 720",
        "points": 50
      },
      {
        "when": "CreditScore >= 650 && CreditScore < 720",
        "points": 25
      }
    ],
    "subtract": [
      {
        "when": "CreditScore < 600",
        "points": 100
      }
    ]
  }
}
```

### 2. Employment Verification (Priority 20)

```json
{
  "name": "Employment Verification",
  "priority": 20,
  "clauses": [
    {
      "if": "EmploymentType == 'Unemployed'",
      "then": "REJECT",
      "reason": "Unemployed applicant"
    },
    {
      "if": "EmploymentType == 'Self-Employed' && Amount > 25000",
      "then": "MANUAL",
      "reason": "Self-employed high amount"
    }
  ],
  "score": {
    "base": 0,
    "add": [
      {
        "when": "EmploymentType == 'Full-Time'",
        "points": 30
      },
      {
        "when": "EmploymentType == 'Part-Time'",
        "points": 15
      }
    ]
  }
}
```

## Sample Applicant Data

The seeded applicants include realistic personal information:

### Names

- Mix of common first and last names
- Gender-neutral distribution
- Realistic email addresses based on names

### Addresses

- Addresses across 10 major US cities
- Realistic street names and ZIP codes
- Distributed across 10 states

### Demographics

- Ages: 25-65 years old
- Phone numbers: Realistic US format
- SSN: Hashed for security (not real SSNs)

## Database Scripts

### Seeding Scripts

- `scripts/seed-database.sh` - Seed database with test data
- `scripts/reset-database.sh` - Reset and reseed database
- `scripts/migrate-database.sh` - Run database migrations only

### Usage Examples

```bash
# Seed database with test data
./scripts/seed-database.sh

# Reset database completely
./scripts/reset-database.sh

# Run migrations only
./scripts/migrate-database.sh

# Using Make commands
make seed      # Seed database
make migrate   # Run migrations
```

## Testing Scenarios

The seeded data supports various testing scenarios:

### Authentication Testing

- Test login with different user roles
- Verify role-based access control
- Test affiliate data segregation

### Application Processing

- Submit new applications as affiliates
- Evaluate applications with rules engine
- Make manual decisions as underwriter

### Rules Engine Testing

- Test automatic approval (low-risk applications)
- Test automatic rejection (very low credit scores)
- Test manual review triggers (high amounts, self-employed)

### Audit Trail Testing

- View audit logs as admin
- Track decision history
- Monitor user actions

### API Testing

- Use seeded data for API contract tests
- Test pagination with 30 applications
- Test filtering by affiliate, status, etc.

## Data Consistency

All seeded data uses:

- **Fixed Random Seed (42)**: Ensures consistent data across environments
- **UTC Timestamps**: All dates in UTC timezone
- **Realistic Relationships**: Proper foreign key relationships
- **Valid Constraints**: All data passes validation rules

## Customization

To customize the seeded data:

1. **Modify `SeedData.cs`**: Update the seeding logic
2. **Adjust Risk Profiles**: Change the distribution in `GetRiskProfile()`
3. **Add More Applications**: Increase the loop count in `SeedApplicationsAsync()`
4. **Update Rules**: Modify the JSON rule definitions

## Security Notes

- **No Real PII**: All personal information is fictional
- **Hashed SSNs**: SSN values are hashed, not real numbers
- **Test Passwords**: Use strong passwords even for test data
- **Development Only**: This data is for development/testing only

## Troubleshooting

### Common Issues

1. **Seeding Fails**: Check database connection and migrations
2. **Duplicate Data**: Reset database if seeding runs multiple times
3. **Login Issues**: Verify user credentials match documentation
4. **Missing Data**: Ensure all services are running before seeding

### Verification Queries

```sql
-- Check seeded counts
SELECT 'Affiliates' as entity, COUNT(*) as count FROM "Affiliates"
UNION ALL
SELECT 'Users', COUNT(*) FROM "AspNetUsers"
UNION ALL
SELECT 'Applications', COUNT(*) FROM "LoanApplications"
UNION ALL
SELECT 'Rules', COUNT(*) FROM "Rules";

-- Check application distribution by affiliate
SELECT a."Name", COUNT(la."Id") as application_count
FROM "Affiliates" a
LEFT JOIN "LoanApplications" la ON a."Id" = la."AffiliateId"
GROUP BY a."Name";

-- Check application status distribution
SELECT "Status", COUNT(*) as count
FROM "LoanApplications"
GROUP BY "Status";
```
