using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartUnderwrite.Core.Entities;
using SmartUnderwrite.Core.Enums;
using SmartUnderwrite.Core.ValueObjects;

namespace SmartUnderwrite.Infrastructure.Data;

public static class SeedData
{
    public static async Task SeedAsync(SmartUnderwriteDbContext context, UserManager<User> userManager, RoleManager<Role> roleManager)
    {
        // Seed Roles
        await SeedRolesAsync(roleManager);
        
        // Seed Affiliates
        await SeedAffiliatesAsync(context);
        
        // Seed Users
        await SeedUsersAsync(context, userManager);
        
        // Seed Rules
        await SeedRulesAsync(context);
        
        // Seed Applicants and Applications
        await SeedApplicationsAsync(context);
        
        await context.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(RoleManager<Role> roleManager)
    {
        var roles = new[] { "Admin", "Underwriter", "Affiliate" };
        
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new Role(roleName));
            }
        }
    }

    private static async Task SeedAffiliatesAsync(SmartUnderwriteDbContext context)
    {
        if (await context.Affiliates.AnyAsync())
            return;

        var affiliates = new[]
        {
            new Affiliate
            {
                Name = "Premier Financial Partners",
                ExternalId = "PFP001",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Affiliate
            {
                Name = "Coastal Credit Solutions",
                ExternalId = "CCS002",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Affiliate
            {
                Name = "Mountain View Lending",
                ExternalId = "MVL003",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Affiliates.AddRange(affiliates);
        await context.SaveChangesAsync();
    }

    private static async Task SeedUsersAsync(SmartUnderwriteDbContext context, UserManager<User> userManager)
    {
        if (await context.Users.AnyAsync())
            return;

        var affiliates = await context.Affiliates.ToListAsync();

        // Admin User
        var adminUser = new User
        {
            UserName = "admin@smartunderwrite.com",
            Email = "admin@smartunderwrite.com",
            FirstName = "System",
            LastName = "Administrator",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(adminUser, "Admin123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        // Underwriter User
        var underwriterUser = new User
        {
            UserName = "underwriter@smartunderwrite.com",
            Email = "underwriter@smartunderwrite.com",
            FirstName = "John",
            LastName = "Underwriter",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        result = await userManager.CreateAsync(underwriterUser, "Under123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(underwriterUser, "Underwriter");
        }

        // Affiliate Users
        for (int i = 0; i < affiliates.Count; i++)
        {
            var affiliate = affiliates[i];
            var affiliateUser = new User
            {
                UserName = $"affiliate{i + 1}@{affiliate.ExternalId.ToLower()}.com",
                Email = $"affiliate{i + 1}@{affiliate.ExternalId.ToLower()}.com",
                FirstName = $"Affiliate",
                LastName = $"User {i + 1}",
                AffiliateId = affiliate.Id,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            result = await userManager.CreateAsync(affiliateUser, "Affiliate123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(affiliateUser, "Affiliate");
            }
        }
    }

    private static async Task SeedRulesAsync(SmartUnderwriteDbContext context)
    {
        if (await context.Rules.AnyAsync())
            return;

        var rules = new[]
        {
            new Rule
            {
                Name = "Basic Credit & DTI",
                Description = "Basic underwriting rules for credit score and debt-to-income ratio",
                Priority = 10,
                IsActive = true,
                RuleDefinition = """
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
                """,
                CreatedAt = DateTime.UtcNow
            },
            new Rule
            {
                Name = "Employment Verification",
                Description = "Rules for employment type verification",
                Priority = 20,
                IsActive = true,
                RuleDefinition = """
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
                """,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Rules.AddRange(rules);
        await context.SaveChangesAsync();
    }

    private static async Task SeedApplicationsAsync(SmartUnderwriteDbContext context)
    {
        if (await context.LoanApplications.AnyAsync())
            return;

        var affiliates = await context.Affiliates.ToListAsync();
        var random = new Random(42); // Fixed seed for consistent test data

        var applicants = new List<Applicant>();
        var applications = new List<LoanApplication>();

        // Create 30 sample applications with varied risk profiles
        // Distribution: 40% low risk, 35% medium risk, 25% high risk
        for (int i = 1; i <= 30; i++)
        {
            var riskProfile = GetRiskProfile(i);
            
            var applicant = new Applicant
            {
                FirstName = GetFirstName(i, random),
                LastName = GetLastName(i, random),
                SsnHash = $"hash_{i:D3}_{random.Next(100000, 999999)}",
                DateOfBirth = DateTime.UtcNow.AddYears(-random.Next(25, 65)),
                Phone = $"555-{random.Next(100, 999):D3}-{random.Next(1000, 9999):D4}",
                Email = $"{GetFirstName(i, random).ToLower()}.{GetLastName(i, random).ToLower()}{i}@email.com",
                Address = new Address(
                    $"{random.Next(100, 9999)} {GetRandomStreetName(random)}",
                    GetRandomCity(random),
                    GetRandomState(random),
                    $"{random.Next(10000, 99999):D5}"
                ),
                CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 90))
            };

            applicants.Add(applicant);

            var application = new LoanApplication
            {
                AffiliateId = affiliates[random.Next(affiliates.Count)].Id,
                Applicant = applicant,
                ProductType = GetRandomProductType(random),
                Amount = GetLoanAmountByRisk(riskProfile, random),
                IncomeMonthly = GetIncomeByRisk(riskProfile, random),
                EmploymentType = GetEmploymentByRisk(riskProfile, random),
                CreditScore = GetCreditScoreByRisk(riskProfile, random),
                Status = GetStatusByRisk(riskProfile, random),
                CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 90))
            };

            applications.Add(application);
        }

        context.Applicants.AddRange(applicants);
        context.LoanApplications.AddRange(applications);
        await context.SaveChangesAsync();
    }

    private static RiskProfile GetRiskProfile(int index)
    {
        // First 12 applications (40%) - Low Risk
        if (index <= 12) return RiskProfile.Low;
        // Next 10 applications (35%) - Medium Risk  
        if (index <= 22) return RiskProfile.Medium;
        // Last 8 applications (25%) - High Risk
        return RiskProfile.High;
    }

    private enum RiskProfile
    {
        Low,
        Medium,
        High
    }

    private static string GetFirstName(int index, Random random)
    {
        var maleNames = new[] { "James", "John", "Robert", "Michael", "William", "David", "Richard", "Joseph", "Thomas", "Christopher" };
        var femaleNames = new[] { "Mary", "Patricia", "Jennifer", "Linda", "Elizabeth", "Barbara", "Susan", "Jessica", "Sarah", "Karen" };
        var allNames = maleNames.Concat(femaleNames).ToArray();
        return allNames[index % allNames.Length];
    }

    private static string GetLastName(int index, Random random)
    {
        var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez", 
                               "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin" };
        return lastNames[index % lastNames.Length];
    }

    private static string GetRandomStreetName(Random random)
    {
        var streetNames = new[] { "Main St", "Oak Ave", "Pine St", "Maple Dr", "Cedar Ln", "Elm St", "Park Ave", "First St", "Second St", "Broadway" };
        return streetNames[random.Next(streetNames.Length)];
    }

    private static decimal GetLoanAmountByRisk(RiskProfile risk, Random random)
    {
        return risk switch
        {
            RiskProfile.Low => new[] { 5000m, 10000m, 15000m, 25000m }[random.Next(4)],
            RiskProfile.Medium => new[] { 25000m, 35000m, 50000m }[random.Next(3)],
            RiskProfile.High => new[] { 50000m, 75000m, 100000m }[random.Next(3)],
            _ => 25000m
        };
    }

    private static decimal GetIncomeByRisk(RiskProfile risk, Random random)
    {
        return risk switch
        {
            RiskProfile.Low => random.Next(6000, 12000),
            RiskProfile.Medium => random.Next(4000, 8000),
            RiskProfile.High => random.Next(2000, 5000),
            _ => 5000
        };
    }

    private static string GetEmploymentByRisk(RiskProfile risk, Random random)
    {
        return risk switch
        {
            RiskProfile.Low => new[] { "Full-Time", "Full-Time", "Full-Time", "Contract" }[random.Next(4)],
            RiskProfile.Medium => new[] { "Full-Time", "Part-Time", "Self-Employed" }[random.Next(3)],
            RiskProfile.High => new[] { "Part-Time", "Self-Employed", "Contract", "Unemployed" }[random.Next(4)],
            _ => "Full-Time"
        };
    }

    private static int? GetCreditScoreByRisk(RiskProfile risk, Random random)
    {
        return risk switch
        {
            RiskProfile.Low => random.Next(700, 850),
            RiskProfile.Medium => random.Next(600, 720),
            RiskProfile.High => random.Next(450, 620),
            _ => 650
        };
    }

    private static ApplicationStatus GetStatusByRisk(RiskProfile risk, Random random)
    {
        return risk switch
        {
            RiskProfile.Low => new[] { ApplicationStatus.Approved, ApplicationStatus.Evaluated, ApplicationStatus.InReview }[random.Next(3)],
            RiskProfile.Medium => new[] { ApplicationStatus.ManualReview, ApplicationStatus.InReview, ApplicationStatus.Evaluated }[random.Next(3)],
            RiskProfile.High => new[] { ApplicationStatus.Rejected, ApplicationStatus.ManualReview, ApplicationStatus.Submitted }[random.Next(3)],
            _ => ApplicationStatus.Submitted
        };
    }

    private static string GetRandomCity(Random random)
    {
        var cities = new[] { "New York", "Los Angeles", "Chicago", "Houston", "Phoenix", "Philadelphia", "San Antonio", "San Diego", "Dallas", "San Jose" };
        return cities[random.Next(cities.Length)];
    }

    private static string GetRandomState(Random random)
    {
        var states = new[] { "NY", "CA", "IL", "TX", "AZ", "PA", "FL", "OH", "GA", "NC" };
        return states[random.Next(states.Length)];
    }

    private static string GetRandomProductType(Random random)
    {
        var products = new[] { "Personal Loan", "Auto Loan", "Home Improvement", "Debt Consolidation", "Business Loan" };
        return products[random.Next(products.Length)];
    }

    private static decimal GetRandomLoanAmount(Random random)
    {
        var amounts = new[] { 5000m, 10000m, 15000m, 25000m, 35000m, 50000m, 75000m, 100000m };
        return amounts[random.Next(amounts.Length)];
    }

    private static decimal GetRandomIncome(Random random)
    {
        return random.Next(2000, 15000);
    }

    private static string GetRandomEmploymentType(Random random)
    {
        var types = new[] { "Full-Time", "Part-Time", "Self-Employed", "Contract", "Unemployed" };
        return types[random.Next(types.Length)];
    }

    private static int? GetRandomCreditScore(Random random)
    {
        // 10% chance of null credit score
        if (random.Next(10) == 0)
            return null;
        
        return random.Next(450, 850);
    }

    private static ApplicationStatus GetRandomStatus(Random random)
    {
        var statuses = new[] { ApplicationStatus.Submitted, ApplicationStatus.InReview, ApplicationStatus.Evaluated, ApplicationStatus.Approved, ApplicationStatus.Rejected, ApplicationStatus.ManualReview };
        return statuses[random.Next(statuses.Length)];
    }
}