namespace SmartUnderwrite.Core.RulesEngine.Models;

public class EvaluationContext
{
    public decimal Amount { get; set; }
    public decimal IncomeMonthly { get; set; }
    public int? CreditScore { get; set; }
    public string EmploymentType { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public DateTime ApplicationDate { get; set; }
    
    // Additional context properties can be added here
    public Dictionary<string, object> AdditionalProperties { get; set; } = new();
}