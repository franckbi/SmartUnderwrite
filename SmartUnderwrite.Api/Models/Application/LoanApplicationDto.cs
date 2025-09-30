using SmartUnderwrite.Core.Enums;

namespace SmartUnderwrite.Api.Models.Application;

public class LoanApplicationDto
{
    public int Id { get; set; }
    public int AffiliateId { get; set; }
    public string AffiliateName { get; set; } = string.Empty;
    public ApplicantDto Applicant { get; set; } = new();
    public string ProductType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal IncomeMonthly { get; set; }
    public string EmploymentType { get; set; } = string.Empty;
    public int? CreditScore { get; set; }
    public ApplicationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<DocumentDto> Documents { get; set; } = new();
    public List<DecisionDto> Decisions { get; set; } = new();
}

public class ApplicantDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public DateTime DateOfBirth { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public AddressDto Address { get; set; } = new();
}

public class DocumentDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class DecisionDto
{
    public int Id { get; set; }
    public int LoanApplicationId { get; set; }
    public DecisionOutcome Outcome { get; set; }
    public int Score { get; set; }
    public string[] Reasons { get; set; } = Array.Empty<string>();
    public int? DecidedByUserId { get; set; }
    public string? DecidedByUserName { get; set; }
    public DateTime DecidedAt { get; set; }
    public bool IsAutomated => DecidedByUserId == null;
}