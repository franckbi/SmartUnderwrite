using SmartUnderwrite.Api.Models.Reports;
using System.Security.Claims;

namespace SmartUnderwrite.Api.Services;

public interface IReportsService
{
    /// <summary>
    /// Gets dashboard report data for the specified date range
    /// </summary>
    /// <param name="fromDate">Start date for the report</param>
    /// <param name="toDate">End date for the report</param>
    /// <param name="user">The requesting user</param>
    /// <returns>Dashboard report data</returns>
    Task<ReportDataDto> GetDashboardReportAsync(DateTime fromDate, DateTime toDate, ClaimsPrincipal user);
}