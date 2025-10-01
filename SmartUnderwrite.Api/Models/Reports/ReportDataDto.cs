namespace SmartUnderwrite.Api.Models.Reports;

public class ReportDataDto
{
    public int TotalApplications { get; set; }
    public int ApprovedApplications { get; set; }
    public int RejectedApplications { get; set; }
    public int PendingApplications { get; set; }
    public double AverageProcessingTime { get; set; }
    public double ApprovalRate { get; set; }
    public decimal TotalLoanAmount { get; set; }
    public decimal AverageLoanAmount { get; set; }
    public List<TopAffiliateDto> TopAffiliates { get; set; } = new();
    public List<DailyStatDto> DailyStats { get; set; } = new();
}

public class TopAffiliateDto
{
    public int AffiliateId { get; set; }
    public string AffiliateName { get; set; } = string.Empty;
    public int ApplicationCount { get; set; }
    public double ApprovalRate { get; set; }
}

public class DailyStatDto
{
    public string Date { get; set; } = string.Empty;
    public int Applications { get; set; }
    public int Approvals { get; set; }
    public int Rejections { get; set; }
}