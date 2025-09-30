namespace SmartUnderwrite.Api.Models.Audit;

public class AuditSummaryDto
{
    public int TotalEntries { get; set; }
    public DateRangeDto DateRange { get; set; } = new();
    public Dictionary<string, int> ActionCounts { get; set; } = new();
    public Dictionary<string, int> EntityTypeCounts { get; set; } = new();
    public Dictionary<string, int> UserActivityCounts { get; set; } = new();
}

public class DateRangeDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}