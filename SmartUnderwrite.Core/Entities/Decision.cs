using SmartUnderwrite.Core.Enums;

namespace SmartUnderwrite.Core.Entities;

public class Decision
{
    public int Id { get; set; }
    public int LoanApplicationId { get; set; }
    public DecisionOutcome Outcome { get; set; }
    public int Score { get; set; }
    public string[] Reasons { get; set; } = Array.Empty<string>();
    public int? DecidedByUserId { get; set; }
    public DateTime DecidedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public LoanApplication LoanApplication { get; set; } = null!;
    public User? DecidedByUser { get; set; }
}