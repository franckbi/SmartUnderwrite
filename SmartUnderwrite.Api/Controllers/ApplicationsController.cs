using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartUnderwrite.Api.Constants;
using SmartUnderwrite.Api.Models.Application;
using SmartUnderwrite.Api.Models.Document;
using SmartUnderwrite.Api.Services;
using SmartUnderwrite.Core.Enums;
using System.Security.Claims;

namespace SmartUnderwrite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationService _applicationService;
    private readonly IDecisionService _decisionService;
    private readonly IDocumentService _documentService;
    private readonly ILogger<ApplicationsController> _logger;

    public ApplicationsController(
        IApplicationService applicationService,
        IDecisionService decisionService,
        IDocumentService documentService,
        ILogger<ApplicationsController> logger)
    {
        _applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
        _decisionService = decisionService ?? throw new ArgumentNullException(nameof(decisionService));
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new loan application
    /// </summary>
    [HttpPost]
    [Authorize(Roles = Roles.Affiliate)]
    public async Task<ActionResult<LoanApplicationDto>> CreateApplication([FromBody] CreateApplicationRequest request)
    {
        try
        {
            // Get affiliate ID from user claims
            var affiliateIdClaim = User.FindFirst("AffiliateId");
            if (affiliateIdClaim == null || !int.TryParse(affiliateIdClaim.Value, out var affiliateId))
            {
                _logger.LogWarning("Affiliate user {UserId} does not have valid AffiliateId claim", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return BadRequest(new { message = "Invalid affiliate user" });
            }

            var application = await _applicationService.CreateApplicationAsync(request, affiliateId);
            
            _logger.LogInformation("Created application {ApplicationId} for affiliate {AffiliateId}", application.Id, affiliateId);
            return CreatedAtAction(nameof(GetApplication), new { id = application.Id }, application);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for creating application");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating application");
            return StatusCode(500, new { message = "An error occurred while creating the application" });
        }
    }

    /// <summary>
    /// Gets a specific loan application by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<LoanApplicationDto>> GetApplication(int id)
    {
        try
        {
            var application = await _applicationService.GetApplicationAsync(id, User);
            if (application == null)
            {
                return NotFound(new { message = "Application not found" });
            }

            return Ok(application);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to application {ApplicationId} by user {UserId}", id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving application {ApplicationId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the application" });
        }
    }

    /// <summary>
    /// Gets a paginated list of loan applications with optional filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<LoanApplicationDto>>> GetApplications([FromQuery] ApplicationFilter filter)
    {
        try
        {
            var applications = await _applicationService.GetApplicationsAsync(filter, User);
            return Ok(applications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving applications");
            return StatusCode(500, new { message = "An error occurred while retrieving applications" });
        }
    }

    /// <summary>
    /// Evaluates a loan application using the rules engine
    /// </summary>
    [HttpPost("{id}/evaluate")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Underwriter}")]
    public async Task<ActionResult<DecisionDto>> EvaluateApplication(int id)
    {
        try
        {
            // First check if application exists and user has access
            var application = await _applicationService.GetApplicationAsync(id, User);
            if (application == null)
            {
                return NotFound(new { message = "Application not found" });
            }

            // Check if application is in a state that can be evaluated
            if (application.Status != ApplicationStatus.Submitted)
            {
                return BadRequest(new { message = "Application cannot be evaluated in its current status" });
            }

            var decision = await _decisionService.EvaluateApplicationAsync(id);
            
            _logger.LogInformation("Evaluated application {ApplicationId} with outcome {Outcome}", id, decision.Outcome);
            return Ok(decision);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to evaluate application {ApplicationId} by user {UserId}", id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for evaluating application {ApplicationId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating application {ApplicationId}", id);
            return StatusCode(500, new { message = "An error occurred while evaluating the application" });
        }
    }

    /// <summary>
    /// Makes a manual decision on a loan application
    /// </summary>
    [HttpPost("{id}/decision")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Underwriter}")]
    public async Task<ActionResult<DecisionDto>> MakeManualDecision(int id, [FromBody] ManualDecisionRequest request)
    {
        try
        {
            // First check if application exists and user has access
            var application = await _applicationService.GetApplicationAsync(id, User);
            if (application == null)
            {
                return NotFound(new { message = "Application not found" });
            }

            // Get user ID from claims
            var decision = await _decisionService.MakeManualDecisionAsync(id, request, User);
            
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userId = userIdClaim?.Value ?? "Unknown";
            
            _logger.LogInformation("Manual decision made on application {ApplicationId} by user {UserId} with outcome {Outcome}", 
                id, userId, decision.Outcome);
            return Ok(decision);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to make decision on application {ApplicationId} by user {UserId}", 
                id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for making decision on application {ApplicationId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making decision on application {ApplicationId}", id);
            return StatusCode(500, new { message = "An error occurred while making the decision" });
        }
    }

    /// <summary>
    /// Uploads a document for a loan application
    /// </summary>
    [HttpPost("{id}/documents")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Underwriter},{Roles.Affiliate}")]
    public async Task<ActionResult<DocumentUploadResponse>> UploadDocument(int id, [FromForm] IFormFile file, [FromForm] string? description = null)
    {
        try
        {
            // First check if application exists and user has access
            var application = await _applicationService.GetApplicationAsync(id, User);
            if (application == null)
            {
                return NotFound(new { message = "Application not found" });
            }

            // Validate file
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file provided" });
            }

            // Check file size (10MB limit)
            if (file.Length > 10 * 1024 * 1024)
            {
                return BadRequest(new { message = "File size cannot exceed 10MB" });
            }

            // Check file type
            var allowedTypes = new[] { "application/pdf", "image/jpeg", "image/png", "image/gif", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };
            if (!allowedTypes.Contains(file.ContentType))
            {
                return BadRequest(new { message = "File type not allowed. Allowed types: PDF, JPEG, PNG, GIF, DOC, DOCX" });
            }

            var request = new DocumentUploadRequest
            {
                LoanApplicationId = id,
                File = file,
                Description = description
            };

            var document = await _documentService.UploadDocumentAsync(request, User);
            
            _logger.LogInformation("Uploaded document {DocumentId} for application {ApplicationId}", document.Id, id);
            return CreatedAtAction(nameof(GetDocument), new { applicationId = id, documentId = document.Id }, document);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to upload document for application {ApplicationId} by user {UserId}", 
                id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for uploading document to application {ApplicationId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document for application {ApplicationId}", id);
            return StatusCode(500, new { message = "An error occurred while uploading the document" });
        }
    }

    /// <summary>
    /// Gets all documents for a loan application
    /// </summary>
    [HttpGet("{applicationId}/documents")]
    public async Task<ActionResult<List<DocumentUploadResponse>>> GetApplicationDocuments(int applicationId)
    {
        try
        {
            // First check if application exists and user has access
            var application = await _applicationService.GetApplicationAsync(applicationId, User);
            if (application == null)
            {
                return NotFound(new { message = "Application not found" });
            }

            var documents = await _documentService.GetApplicationDocumentsAsync(applicationId, User);
            return Ok(documents);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to documents for application {ApplicationId} by user {UserId}", 
                applicationId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents for application {ApplicationId}", applicationId);
            return StatusCode(500, new { message = "An error occurred while retrieving the documents" });
        }
    }

    /// <summary>
    /// Gets a specific document for a loan application
    /// </summary>
    [HttpGet("{applicationId}/documents/{documentId}")]
    public async Task<ActionResult<DocumentUploadResponse>> GetDocument(int applicationId, int documentId)
    {
        try
        {
            // First check if application exists and user has access
            var application = await _applicationService.GetApplicationAsync(applicationId, User);
            if (application == null)
            {
                return NotFound(new { message = "Application not found" });
            }

            var document = await _documentService.GetDocumentAsync(documentId, User);
            if (document == null)
            {
                return NotFound(new { message = "Document not found" });
            }

            // Convert DocumentDownloadResponse to DocumentUploadResponse for consistency
            var response = new DocumentUploadResponse
            {
                Id = documentId,
                FileName = document.FileName,
                ContentType = document.ContentType,
                FileSize = document.FileSize,
                UploadedAt = DateTime.UtcNow // This should come from the database
            };

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to document {DocumentId} by user {UserId}", 
                documentId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document {DocumentId}", documentId);
            return StatusCode(500, new { message = "An error occurred while retrieving the document" });
        }
    }

    /// <summary>
    /// Downloads a document file
    /// </summary>
    [HttpGet("{applicationId}/documents/{documentId}/download")]
    public async Task<IActionResult> DownloadDocument(int applicationId, int documentId)
    {
        try
        {
            // First check if application exists and user has access
            var application = await _applicationService.GetApplicationAsync(applicationId, User);
            if (application == null)
            {
                return NotFound(new { message = "Application not found" });
            }

            var document = await _documentService.GetDocumentAsync(documentId, User);
            if (document == null)
            {
                return NotFound(new { message = "Document not found" });
            }

            _logger.LogInformation("Downloaded document {DocumentId} for application {ApplicationId}", documentId, applicationId);
            return File(document.FileStream, document.ContentType, document.FileName);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to download document {DocumentId} by user {UserId}", 
                documentId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document {DocumentId}", documentId);
            return StatusCode(500, new { message = "An error occurred while downloading the document" });
        }
    }

    /// <summary>
    /// Deletes a document from a loan application
    /// </summary>
    [HttpDelete("{applicationId}/documents/{documentId}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Underwriter}")]
    public async Task<IActionResult> DeleteDocument(int applicationId, int documentId)
    {
        try
        {
            // First check if application exists and user has access
            var application = await _applicationService.GetApplicationAsync(applicationId, User);
            if (application == null)
            {
                return NotFound(new { message = "Application not found" });
            }

            var deleted = await _documentService.DeleteDocumentAsync(documentId, User);
            if (!deleted)
            {
                return NotFound(new { message = "Document not found" });
            }

            _logger.LogInformation("Deleted document {DocumentId} from application {ApplicationId}", documentId, applicationId);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to delete document {DocumentId} by user {UserId}", 
                documentId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
            return StatusCode(500, new { message = "An error occurred while deleting the document" });
        }
    }
}