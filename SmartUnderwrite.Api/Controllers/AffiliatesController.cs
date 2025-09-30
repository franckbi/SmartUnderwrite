using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartUnderwrite.Api.Constants;
using SmartUnderwrite.Api.Models.Affiliates;
using SmartUnderwrite.Core.Entities;
using SmartUnderwrite.Infrastructure.Data;

namespace SmartUnderwrite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Admin)]
public class AffiliatesController : ControllerBase
{
    private readonly SmartUnderwriteDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<AffiliatesController> _logger;

    public AffiliatesController(
        SmartUnderwriteDbContext context,
        UserManager<User> userManager,
        ILogger<AffiliatesController> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all affiliates with summary information
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AffiliateDto>>> GetAffiliates()
    {
        try
        {
            var affiliates = await _context.Affiliates
                .Include(a => a.Users)
                .Include(a => a.LoanApplications)
                .Select(a => new AffiliateDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    ExternalId = a.ExternalId,
                    IsActive = a.IsActive,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt,
                    UserCount = a.Users.Count,
                    ApplicationCount = a.LoanApplications.Count
                })
                .ToListAsync();

            return Ok(affiliates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving affiliates");
            return StatusCode(500, new { message = "An error occurred while retrieving affiliates" });
        }
    }

    /// <summary>
    /// Gets a specific affiliate by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AffiliateDto>> GetAffiliate(int id)
    {
        try
        {
            var affiliate = await _context.Affiliates
                .Include(a => a.Users)
                .Include(a => a.LoanApplications)
                .Where(a => a.Id == id)
                .Select(a => new AffiliateDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    ExternalId = a.ExternalId,
                    IsActive = a.IsActive,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt,
                    UserCount = a.Users.Count,
                    ApplicationCount = a.LoanApplications.Count
                })
                .FirstOrDefaultAsync();

            if (affiliate == null)
            {
                return NotFound(new { message = "Affiliate not found" });
            }

            return Ok(affiliate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving affiliate {AffiliateId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the affiliate" });
        }
    }

    /// <summary>
    /// Creates a new affiliate
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AffiliateDto>> CreateAffiliate([FromBody] CreateAffiliateRequest request)
    {
        try
        {
            // Check if external ID already exists
            var existingAffiliate = await _context.Affiliates
                .FirstOrDefaultAsync(a => a.ExternalId == request.ExternalId);

            if (existingAffiliate != null)
            {
                return BadRequest(new { message = "An affiliate with this external ID already exists" });
            }

            var affiliate = new Affiliate
            {
                Name = request.Name,
                ExternalId = request.ExternalId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Affiliates.Add(affiliate);
            await _context.SaveChangesAsync();

            var affiliateDto = new AffiliateDto
            {
                Id = affiliate.Id,
                Name = affiliate.Name,
                ExternalId = affiliate.ExternalId,
                IsActive = affiliate.IsActive,
                CreatedAt = affiliate.CreatedAt,
                UpdatedAt = affiliate.UpdatedAt,
                UserCount = 0,
                ApplicationCount = 0
            };

            _logger.LogInformation("Created affiliate {AffiliateId}: {AffiliateName}", affiliate.Id, affiliate.Name);
            return CreatedAtAction(nameof(GetAffiliate), new { id = affiliate.Id }, affiliateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating affiliate");
            return StatusCode(500, new { message = "An error occurred while creating the affiliate" });
        }
    }

    /// <summary>
    /// Updates an existing affiliate
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<AffiliateDto>> UpdateAffiliate(int id, [FromBody] UpdateAffiliateRequest request)
    {
        try
        {
            var affiliate = await _context.Affiliates
                .Include(a => a.Users)
                .Include(a => a.LoanApplications)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (affiliate == null)
            {
                return NotFound(new { message = "Affiliate not found" });
            }

            // Check if external ID already exists (excluding current affiliate)
            var existingAffiliate = await _context.Affiliates
                .FirstOrDefaultAsync(a => a.ExternalId == request.ExternalId && a.Id != id);

            if (existingAffiliate != null)
            {
                return BadRequest(new { message = "An affiliate with this external ID already exists" });
            }

            affiliate.Name = request.Name;
            affiliate.ExternalId = request.ExternalId;
            affiliate.IsActive = request.IsActive;
            affiliate.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var affiliateDto = new AffiliateDto
            {
                Id = affiliate.Id,
                Name = affiliate.Name,
                ExternalId = affiliate.ExternalId,
                IsActive = affiliate.IsActive,
                CreatedAt = affiliate.CreatedAt,
                UpdatedAt = affiliate.UpdatedAt,
                UserCount = affiliate.Users.Count,
                ApplicationCount = affiliate.LoanApplications.Count
            };

            _logger.LogInformation("Updated affiliate {AffiliateId}: {AffiliateName}", affiliate.Id, affiliate.Name);
            return Ok(affiliateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating affiliate {AffiliateId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the affiliate" });
        }
    }

    /// <summary>
    /// Deactivates an affiliate (soft delete)
    /// </summary>
    [HttpPost("{id}/deactivate")]
    public async Task<ActionResult<AffiliateDto>> DeactivateAffiliate(int id)
    {
        try
        {
            var affiliate = await _context.Affiliates
                .Include(a => a.Users)
                .Include(a => a.LoanApplications)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (affiliate == null)
            {
                return NotFound(new { message = "Affiliate not found" });
            }

            affiliate.IsActive = false;
            affiliate.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var affiliateDto = new AffiliateDto
            {
                Id = affiliate.Id,
                Name = affiliate.Name,
                ExternalId = affiliate.ExternalId,
                IsActive = affiliate.IsActive,
                CreatedAt = affiliate.CreatedAt,
                UpdatedAt = affiliate.UpdatedAt,
                UserCount = affiliate.Users.Count,
                ApplicationCount = affiliate.LoanApplications.Count
            };

            _logger.LogInformation("Deactivated affiliate {AffiliateId}: {AffiliateName}", affiliate.Id, affiliate.Name);
            return Ok(affiliateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating affiliate {AffiliateId}", id);
            return StatusCode(500, new { message = "An error occurred while deactivating the affiliate" });
        }
    }

    /// <summary>
    /// Activates an affiliate
    /// </summary>
    [HttpPost("{id}/activate")]
    public async Task<ActionResult<AffiliateDto>> ActivateAffiliate(int id)
    {
        try
        {
            var affiliate = await _context.Affiliates
                .Include(a => a.Users)
                .Include(a => a.LoanApplications)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (affiliate == null)
            {
                return NotFound(new { message = "Affiliate not found" });
            }

            affiliate.IsActive = true;
            affiliate.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var affiliateDto = new AffiliateDto
            {
                Id = affiliate.Id,
                Name = affiliate.Name,
                ExternalId = affiliate.ExternalId,
                IsActive = affiliate.IsActive,
                CreatedAt = affiliate.CreatedAt,
                UpdatedAt = affiliate.UpdatedAt,
                UserCount = affiliate.Users.Count,
                ApplicationCount = affiliate.LoanApplications.Count
            };

            _logger.LogInformation("Activated affiliate {AffiliateId}: {AffiliateName}", affiliate.Id, affiliate.Name);
            return Ok(affiliateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating affiliate {AffiliateId}", id);
            return StatusCode(500, new { message = "An error occurred while activating the affiliate" });
        }
    }

    /// <summary>
    /// Gets all users for a specific affiliate
    /// </summary>
    [HttpGet("{id}/users")]
    public async Task<ActionResult<IEnumerable<AffiliateUserDto>>> GetAffiliateUsers(int id)
    {
        try
        {
            var affiliate = await _context.Affiliates.FindAsync(id);
            if (affiliate == null)
            {
                return NotFound(new { message = "Affiliate not found" });
            }

            var users = await _context.Users
                .Where(u => u.AffiliateId == id)
                .Select(u => new AffiliateUserDto
                {
                    Id = u.Id,
                    Email = u.Email ?? string.Empty,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    CreatedAt = u.CreatedAt,
                    IsActive = !u.LockoutEnd.HasValue || u.LockoutEnd <= DateTimeOffset.UtcNow
                })
                .ToListAsync();

            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users for affiliate {AffiliateId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving affiliate users" });
        }
    }

    /// <summary>
    /// Assigns a user to an affiliate
    /// </summary>
    [HttpPost("{id}/users")]
    public async Task<ActionResult<AffiliateUserDto>> AssignUserToAffiliate(int id, [FromBody] AssignUserToAffiliateRequest request)
    {
        try
        {
            var affiliate = await _context.Affiliates.FindAsync(id);
            if (affiliate == null)
            {
                return NotFound(new { message = "Affiliate not found" });
            }

            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Check if user is already assigned to an affiliate
            if (user.AffiliateId.HasValue)
            {
                return BadRequest(new { message = "User is already assigned to an affiliate" });
            }

            // Check if user has the affiliate role
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains(Roles.Affiliate))
            {
                return BadRequest(new { message = "User must have the Affiliate role to be assigned to an affiliate" });
            }

            user.AffiliateId = id;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(new { message = "Failed to assign user to affiliate", errors });
            }

            var userDto = new AffiliateUserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                CreatedAt = user.CreatedAt,
                IsActive = !user.LockoutEnd.HasValue || user.LockoutEnd <= DateTimeOffset.UtcNow
            };

            _logger.LogInformation("Assigned user {UserId} to affiliate {AffiliateId}", user.Id, id);
            return Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning user {UserId} to affiliate {AffiliateId}", request.UserId, id);
            return StatusCode(500, new { message = "An error occurred while assigning the user to the affiliate" });
        }
    }

    /// <summary>
    /// Removes a user from an affiliate
    /// </summary>
    [HttpDelete("{id}/users/{userId}")]
    public async Task<IActionResult> RemoveUserFromAffiliate(int id, int userId)
    {
        try
        {
            var affiliate = await _context.Affiliates.FindAsync(id);
            if (affiliate == null)
            {
                return NotFound(new { message = "Affiliate not found" });
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (user.AffiliateId != id)
            {
                return BadRequest(new { message = "User is not assigned to this affiliate" });
            }

            user.AffiliateId = null;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(new { message = "Failed to remove user from affiliate", errors });
            }

            _logger.LogInformation("Removed user {UserId} from affiliate {AffiliateId}", userId, id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user {UserId} from affiliate {AffiliateId}", userId, id);
            return StatusCode(500, new { message = "An error occurred while removing the user from the affiliate" });
        }
    }
}