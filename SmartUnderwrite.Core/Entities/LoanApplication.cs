using SmartUnderwrite.Core.Enums;

namespace SmartUnderwrite.Core.Entities;

public class LoanApplication
{
    public int Id { get; set; }
    public int AffiliateId { get; set; }
    public int ApplicantId { get; set; }
    public string ProductType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal IncomeMonthly { get; set; }
    public string EmploymentType { get; set; } = string.Empty;
    public int? CreditScore { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Submitted;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Affiliate Affiliate { get; set; } = null!;
    public Applicant Applicant { get; set; } = null!;
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<Decision> Decisions { get; set; } = new List<Decision>();
}