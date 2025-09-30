using System.ComponentModel.DataAnnotations;

namespace SmartUnderwrite.Api.Models.Audit;

public class AuditLogFilterRequest
{
    /// <summary>
    /// Filter by entity type (e.g., LoanApplication, Rule, User)
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Filter by specific entity ID
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// Filter by start date (inclusive)
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Filter by end date (inclusive)
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Validate date range
    /// </summary>
    /// <returns>Validation results</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FromDate.HasValue && ToDate.HasValue && FromDate > ToDate)
        {
            yield return new ValidationResult(
                "FromDate must be less than or equal to ToDate",
                new[] { nameof(FromDate), nameof(ToDate) });
        }

        if (FromDate.HasValue && FromDate > DateTime.UtcNow)
        {
            yield return new ValidationResult(
                "FromDate cannot be in the future",
                new[] { nameof(FromDate) });
        }

        if (ToDate.HasValue && ToDate > DateTime.UtcNow)
        {
            yield return new ValidationResult(
                "ToDate cannot be in the future",
                new[] { nameof(ToDate) });
        }
    }
}