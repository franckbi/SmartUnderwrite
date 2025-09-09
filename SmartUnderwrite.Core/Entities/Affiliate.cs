namespace SmartUnderwrite.Core.Entities;

public class Affiliate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<LoanApplication> LoanApplications { get; set; } = new List<LoanApplication>();
}