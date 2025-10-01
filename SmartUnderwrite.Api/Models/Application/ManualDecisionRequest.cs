using SmartUnderwrite.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace SmartUnderwrite.Api.Models.Application;

public class ManualDecisionRequest
{
    [Required]
    public DecisionOutcome Outcome { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one reason must be provided")]
    public string[] Reasons { get; set; } = Array.Empty<string>();

    [MaxLength(1000)]
    public string? Justification { get; set; }
}

public class DecisionRequest
{
    [Required]
    public int ApplicationId { get; set; }

    [Required]
    public DecisionOutcome Outcome { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one reason must be provided")]
    public string[] Reasons { get; set; } = Array.Empty<string>();

    [MaxLength(1000)]
    public string? Notes { get; set; }
}