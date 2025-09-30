using Microsoft.EntityFrameworkCore;
using SmartUnderwrite.Api.Constants;
using SmartUnderwrite.Api.Models.Application;
using SmartUnderwrite.Api.Services;
using SmartUnderwrite.Core.Entities;
using SmartUnderwrite.Core.Enums;
using SmartUnderwrite.Core.ValueObjects;
using SmartUnderwrite.Infrastructure.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SmartUnderwrite.Api.Services;

public class ApplicationService : IApplicationService
{
    private readonly SmartUnderwriteDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ApplicationService> _logger;

    public ApplicationService(
        SmartUnderwriteDbContext context,
        ICurrentUserService currentUserService,
        ILogger<ApplicationService> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<LoanApplicationDto> CreateApplicationAsync(CreateApplicationRequest request, int affiliateId)
    {
        _logger.LogInformation("Creating loan application for affiliate {AffiliateId}", affiliateId);

        // Validate affiliate exists and is active
        var affiliate = await _context.Affiliates
            .FirstOrDefaultAsync(a => a.Id == affiliateId && a.IsActive);
        
        if (affiliate == null)
        {
            throw new ArgumentException($"Affiliate with ID {affiliateId} not found or inactive");
        }

        // Hash SSN for storage
        var ssnHash = HashSsn(request.Ssn);

        // Create applicant
        var applicant = new Applicant
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            SsnHash = ssnHash,
            DateOfBirth = request.DateOfBirth,
            Phone = request.Phone.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Address = new Address
            {
                Street = request.Address.Street.Trim(),
                City = request.Address.City.Trim(),
                State = request.Address.State.Trim(),
                ZipCode = request.Address.ZipCode.Trim()
            }
        };

        // Create loan application
        var loanApplication = new LoanApplication
        {
            AffiliateId = affiliateId,
            Applicant = applicant,
            ProductType = request.ProductType.Trim(),
            Amount = request.Amount,
            IncomeMonthly = request.IncomeMonthly,
            EmploymentType = request.EmploymentType.Trim(),
            CreditScore = request.CreditScore,
            Status = ApplicationStatus.Submitted
        };

        _context.LoanApplications.Add(loanApplication);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created loan application {ApplicationId} for affiliate {AffiliateId}", 
            loanApplication.Id, affiliateId);

        return await MapToDto(loanApplication);
    }

    public async Task<LoanApplicationDto?> GetApplicationAsync(int id, ClaimsPrincipal user)
    {
        _logger.LogDebug("Getting application {ApplicationId} for user {UserId}", id, _currentUserService.GetUserId());

        var query = _context.LoanApplications
            .Include(la => la.Affiliate)
            .Include(la => la.Applicant)
            .Include(la => la.Documents)
            .Include(la => la.Decisions)
                .ThenInclude(d => d.DecidedByUser)
            .AsQueryable();

        // Apply role-based filtering
        query = ApplyRoleBasedFiltering(query, user);

        var application = await query.FirstOrDefaultAsync(la => la.Id == id);
        
        if (application == null)
        {
            return null;
        }

        return await MapToDto(application);
    }

    public async Task<PagedResult<LoanApplicationDto>> GetApplicationsAsync(ApplicationFilter filter, ClaimsPrincipal user)
    {
        _logger.LogDebug("Getting applications with filter for user {UserId}", _currentUserService.GetUserId());

        var query = _context.LoanApplications
            .Include(la => la.Affiliate)
            .Include(la => la.Applicant)
            .Include(la => la.Documents)
            .Include(la => la.Decisions)
                .ThenInclude(d => d.DecidedByUser)
            .AsQueryable();

        // Apply role-based filtering
        query = ApplyRoleBasedFiltering(query, user);

        // Apply filters
        if (filter.Status.HasValue)
        {
            query = query.Where(la => la.Status == filter.Status.Value);
        }

        if (filter.AffiliateId.HasValue)
        {
            query = query.Where(la => la.AffiliateId == filter.AffiliateId.Value);
        }

        if (filter.CreatedAfter.HasValue)
        {
            query = query.Where(la => la.CreatedAt >= filter.CreatedAfter.Value);
        }

        if (filter.CreatedBefore.HasValue)
        {
            query = query.Where(la => la.CreatedAt <= filter.CreatedBefore.Value);
        }

        if (filter.MinAmount.HasValue)
        {
            query = query.Where(la => la.Amount >= filter.MinAmount.Value);
        }

        if (filter.MaxAmount.HasValue)
        {
            query = query.Where(la => la.Amount <= filter.MaxAmount.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.ProductType))
        {
            query = query.Where(la => la.ProductType.Contains(filter.ProductType));
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply pagination
        var applications = await query
            .OrderByDescending(la => la.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var dtos = new List<LoanApplicationDto>();
        foreach (var app in applications)
        {
            dtos.Add(await MapToDto(app));
        }

        return new PagedResult<LoanApplicationDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<LoanApplicationDto?> UpdateApplicationStatusAsync(int id, ApplicationStatus status, ClaimsPrincipal user)
    {
        _logger.LogInformation("Updating application {ApplicationId} status to {Status}", id, status);

        var query = _context.LoanApplications
            .Include(la => la.Affiliate)
            .Include(la => la.Applicant)
            .Include(la => la.Documents)
            .Include(la => la.Decisions)
                .ThenInclude(d => d.DecidedByUser)
            .AsQueryable();

        // Apply role-based filtering
        query = ApplyRoleBasedFiltering(query, user);

        var application = await query.FirstOrDefaultAsync(la => la.Id == id);
        
        if (application == null)
        {
            return null;
        }

        application.Status = status;
        application.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated application {ApplicationId} status to {Status}", id, status);

        return await MapToDto(application);
    }

    private IQueryable<LoanApplication> ApplyRoleBasedFiltering(IQueryable<LoanApplication> query, ClaimsPrincipal user)
    {
        var userRoles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        // Admins and Underwriters can see all applications
        if (userRoles.Contains(Roles.Admin) || userRoles.Contains(Roles.Underwriter))
        {
            return query;
        }

        // Affiliates can only see their own applications
        if (userRoles.Contains(Roles.Affiliate))
        {
            var userAffiliateId = _currentUserService.GetAffiliateId();
            if (userAffiliateId.HasValue)
            {
                return query.Where(la => la.AffiliateId == userAffiliateId.Value);
            }
        }

        // If no valid role or affiliate, return empty query
        return query.Where(la => false);
    }

    private Task<LoanApplicationDto> MapToDto(LoanApplication application)
    {
        return Task.FromResult(new LoanApplicationDto
        {
            Id = application.Id,
            AffiliateId = application.AffiliateId,
            AffiliateName = application.Affiliate?.Name ?? "Unknown",
            Applicant = new ApplicantDto
            {
                Id = application.Applicant.Id,
                FirstName = application.Applicant.FirstName,
                LastName = application.Applicant.LastName,
                DateOfBirth = application.Applicant.DateOfBirth,
                Phone = application.Applicant.Phone,
                Email = application.Applicant.Email,
                Address = new AddressDto
                {
                    Street = application.Applicant.Address.Street,
                    City = application.Applicant.Address.City,
                    State = application.Applicant.Address.State,
                    ZipCode = application.Applicant.Address.ZipCode
                }
            },
            ProductType = application.ProductType,
            Amount = application.Amount,
            IncomeMonthly = application.IncomeMonthly,
            EmploymentType = application.EmploymentType,
            CreditScore = application.CreditScore,
            Status = application.Status,
            CreatedAt = application.CreatedAt,
            UpdatedAt = application.UpdatedAt,
            Documents = application.Documents.Select(d => new DocumentDto
            {
                Id = d.Id,
                FileName = d.FileName,
                ContentType = d.ContentType,
                FileSize = d.FileSize,
                UploadedAt = d.CreatedAt
            }).ToList(),
            Decisions = application.Decisions.Select(d => new DecisionDto
            {
                Id = d.Id,
                LoanApplicationId = d.LoanApplicationId,
                Outcome = d.Outcome,
                Score = d.Score,
                Reasons = d.Reasons,
                DecidedByUserId = d.DecidedByUserId,
                DecidedByUserName = d.DecidedByUser != null ? $"{d.DecidedByUser.FirstName} {d.DecidedByUser.LastName}" : null,
                DecidedAt = d.DecidedAt
            }).OrderByDescending(d => d.DecidedAt).ToList()
        });
    }

    private static string HashSsn(string ssn)
    {
        // Remove any formatting from SSN
        var cleanSsn = ssn.Replace("-", "").Replace(" ", "");
        
        // Use SHA256 with a salt for hashing
        var salt = "SmartUnderwrite_SSN_Salt_2024"; // In production, this should be from configuration
        var saltedSsn = cleanSsn + salt;
        
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedSsn));
        return Convert.ToBase64String(hashedBytes);
    }
}