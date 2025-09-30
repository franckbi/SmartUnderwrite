using SmartUnderwrite.Api.Models.Application;
using System.Security.Claims;

namespace SmartUnderwrite.Api.Services;

public interface IApplicationService
{
    Task<LoanApplicationDto> CreateApplicationAsync(CreateApplicationRequest request, int affiliateId);
    Task<LoanApplicationDto?> GetApplicationAsync(int id, ClaimsPrincipal user);
    Task<PagedResult<LoanApplicationDto>> GetApplicationsAsync(ApplicationFilter filter, ClaimsPrincipal user);
    Task<LoanApplicationDto?> UpdateApplicationStatusAsync(int id, Core.Enums.ApplicationStatus status, ClaimsPrincipal user);
}