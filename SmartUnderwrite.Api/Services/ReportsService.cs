using Microsoft.EntityFrameworkCore;
using SmartUnderwrite.Api.Models.Reports;
using SmartUnderwrite.Core.Enums;
using SmartUnderwrite.Infrastructure.Data;
using System.Security.Claims;

namespace SmartUnderwrite.Api.Services;

public class ReportsService : IReportsService
{
    private readonly SmartUnderwriteDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ReportsService> _logger;

    public ReportsService(
        SmartUnderwriteDbContext context,
        ICurrentUserService currentUserService,
        ILogger<ReportsService> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ReportDataDto> GetDashboardReportAsync(DateTime fromDate, DateTime toDate, ClaimsPrincipal user)
    {
        _logger.LogDebug("Generating dashboard report from {FromDate} to {ToDate}", fromDate, toDate);

        var userRoles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var applicationsQuery = _context.LoanApplications
            .Include(la => la.Affiliate)
            .Include(la => la.Decisions)
            .Where(la => la.CreatedAt >= fromDate && la.CreatedAt <= toDate)
            .AsQueryable();

        // Apply role-based filtering
        if (userRoles.Contains("Affiliate"))
        {
            var userId = _currentUserService.GetUserId();
            var userAffiliate = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.AffiliateId)
                .FirstOrDefaultAsync();

            if (userAffiliate.HasValue)
            {
                applicationsQuery = applicationsQuery.Where(la => la.AffiliateId == userAffiliate.Value);
            }
            else
            {
                // Affiliate user without affiliate assignment - return empty report
                return new ReportDataDto();
            }
        }
        // Admins and underwriters can see all data

        var applications = await applicationsQuery.ToListAsync();

        // Calculate basic metrics
        var totalApplications = applications.Count;
        var approvedApplications = applications.Count(a => a.Status == ApplicationStatus.Approved);
        var rejectedApplications = applications.Count(a => a.Status == ApplicationStatus.Rejected);
        var pendingApplications = applications.Count(a => 
            a.Status == ApplicationStatus.Submitted || 
            a.Status == ApplicationStatus.InReview);

        var approvalRate = totalApplications > 0 ? (double)approvedApplications / totalApplications : 0;
        var totalLoanAmount = applications.Sum(a => a.Amount);
        var averageLoanAmount = totalApplications > 0 ? totalLoanAmount / totalApplications : 0;

        // Calculate average processing time (in hours)
        var processedApplications = applications.Where(a => 
            a.Status == ApplicationStatus.Approved || 
            a.Status == ApplicationStatus.Rejected).ToList();
        
        var averageProcessingTime = 0.0;
        if (processedApplications.Any())
        {
            var totalProcessingTime = processedApplications
                .Where(a => a.UpdatedAt.HasValue)
                .Sum(a => (a.UpdatedAt!.Value - a.CreatedAt).TotalHours);
            averageProcessingTime = totalProcessingTime / processedApplications.Count;
        }

        // Get top affiliates
        var topAffiliates = applications
            .GroupBy(a => new { a.AffiliateId, a.Affiliate.Name })
            .Select(g => new TopAffiliateDto
            {
                AffiliateId = g.Key.AffiliateId,
                AffiliateName = g.Key.Name,
                ApplicationCount = g.Count(),
                ApprovalRate = g.Count() > 0 ? (double)g.Count(a => a.Status == ApplicationStatus.Approved) / g.Count() : 0
            })
            .OrderByDescending(a => a.ApplicationCount)
            .Take(10)
            .ToList();

        // Get daily statistics
        var dailyStats = applications
            .GroupBy(a => a.CreatedAt.Date)
            .Select(g => new DailyStatDto
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                Applications = g.Count(),
                Approvals = g.Count(a => a.Status == ApplicationStatus.Approved),
                Rejections = g.Count(a => a.Status == ApplicationStatus.Rejected)
            })
            .OrderBy(d => d.Date)
            .ToList();

        return new ReportDataDto
        {
            TotalApplications = totalApplications,
            ApprovedApplications = approvedApplications,
            RejectedApplications = rejectedApplications,
            PendingApplications = pendingApplications,
            AverageProcessingTime = averageProcessingTime,
            ApprovalRate = approvalRate,
            TotalLoanAmount = totalLoanAmount,
            AverageLoanAmount = averageLoanAmount,
            TopAffiliates = topAffiliates,
            DailyStats = dailyStats
        };
    }
}