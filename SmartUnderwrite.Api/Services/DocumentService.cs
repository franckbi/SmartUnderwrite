using Microsoft.EntityFrameworkCore;
using SmartUnderwrite.Api.Constants;
using SmartUnderwrite.Api.Models.Document;
using SmartUnderwrite.Core.Entities;
using SmartUnderwrite.Infrastructure.Data;
using System.Security.Claims;

namespace SmartUnderwrite.Api.Services;

public class DocumentService : IDocumentService
{
    private readonly SmartUnderwriteDbContext _context;
    private readonly IStorageService _storageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DocumentService> _logger;

    private static readonly string[] AllowedContentTypes = {
        "application/pdf",
        "image/jpeg",
        "image/jpg", 
        "image/png",
        "image/gif",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "text/plain"
    };

    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

    public DocumentService(
        SmartUnderwriteDbContext context,
        IStorageService storageService,
        ICurrentUserService currentUserService,
        ILogger<DocumentService> logger)
    {
        _context = context;
        _storageService = storageService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<DocumentUploadResponse> UploadDocumentAsync(DocumentUploadRequest request, ClaimsPrincipal user)
    {
        _logger.LogInformation("Uploading document for application {ApplicationId}", request.LoanApplicationId);

        // Validate file
        ValidateFile(request.File);

        // Check if user can access the application
        var application = await GetApplicationWithAccessCheck(request.LoanApplicationId, user);
        if (application == null)
        {
            throw new UnauthorizedAccessException("Access denied to loan application");
        }

        try
        {
            // Upload file to storage
            var storagePath = await _storageService.UploadFileAsync(
                request.File.OpenReadStream(),
                request.File.FileName,
                request.File.ContentType,
                $"applications/{request.LoanApplicationId}");

            // Create document record
            var document = new Document
            {
                LoanApplicationId = request.LoanApplicationId,
                FileName = request.File.FileName,
                ContentType = request.File.ContentType,
                StoragePath = storagePath,
                FileSize = request.File.Length
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully uploaded document {DocumentId} for application {ApplicationId}", 
                document.Id, request.LoanApplicationId);

            return new DocumentUploadResponse
            {
                Id = document.Id,
                FileName = document.FileName,
                ContentType = document.ContentType,
                FileSize = document.FileSize,
                UploadedAt = document.CreatedAt,
                Description = request.Description
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload document for application {ApplicationId}", request.LoanApplicationId);
            throw;
        }
    }

    public async Task<DocumentDownloadResponse?> GetDocumentAsync(int documentId, ClaimsPrincipal user)
    {
        _logger.LogDebug("Getting document {DocumentId}", documentId);

        var document = await _context.Documents
            .Include(d => d.LoanApplication)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
        {
            return null;
        }

        // Check if user can access the application
        var hasAccess = await CanAccessApplication(document.LoanApplicationId, user);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Access denied to document");
        }

        try
        {
            var fileStream = await _storageService.GetFileAsync(document.StoragePath);

            return new DocumentDownloadResponse
            {
                FileStream = fileStream,
                FileName = document.FileName,
                ContentType = document.ContentType,
                FileSize = document.FileSize
            };
        }
        catch (StorageException ex)
        {
            _logger.LogError(ex, "Failed to retrieve document {DocumentId} from storage", documentId);
            throw;
        }
    }

    public async Task<bool> DeleteDocumentAsync(int documentId, ClaimsPrincipal user)
    {
        _logger.LogInformation("Deleting document {DocumentId}", documentId);

        var document = await _context.Documents
            .Include(d => d.LoanApplication)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
        {
            return false;
        }

        // Check if user can access the application
        var hasAccess = await CanAccessApplication(document.LoanApplicationId, user);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Access denied to document");
        }

        try
        {
            // Delete from storage
            await _storageService.DeleteFileAsync(document.StoragePath);

            // Delete from database
            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully deleted document {DocumentId}", documentId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete document {DocumentId}", documentId);
            return false;
        }
    }

    public async Task<List<DocumentUploadResponse>> GetApplicationDocumentsAsync(int applicationId, ClaimsPrincipal user)
    {
        _logger.LogDebug("Getting documents for application {ApplicationId}", applicationId);

        // Check if user can access the application
        var hasAccess = await CanAccessApplication(applicationId, user);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Access denied to loan application");
        }

        var documents = await _context.Documents
            .Where(d => d.LoanApplicationId == applicationId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return documents.Select(d => new DocumentUploadResponse
        {
            Id = d.Id,
            FileName = d.FileName,
            ContentType = d.ContentType,
            FileSize = d.FileSize,
            UploadedAt = d.CreatedAt
        }).ToList();
    }

    private static void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is required and cannot be empty");
        }

        if (file.Length > MaxFileSize)
        {
            throw new ArgumentException($"File size cannot exceed {MaxFileSize / (1024 * 1024)}MB");
        }

        if (!AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            throw new ArgumentException($"File type '{file.ContentType}' is not allowed");
        }

        // Additional validation for file extension
        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".doc", ".docx", ".xls", ".xlsx", ".txt" };
        
        if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
        {
            throw new ArgumentException($"File extension '{extension}' is not allowed");
        }
    }

    private async Task<LoanApplication?> GetApplicationWithAccessCheck(int applicationId, ClaimsPrincipal user)
    {
        var query = _context.LoanApplications.AsQueryable();

        // Apply role-based filtering
        var userRoles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        if (userRoles.Contains(Roles.Admin) || userRoles.Contains(Roles.Underwriter))
        {
            // Admins and Underwriters can access all applications
            return await query.FirstOrDefaultAsync(la => la.Id == applicationId);
        }

        if (userRoles.Contains(Roles.Affiliate))
        {
            // Affiliates can only access their own applications
            var userAffiliateId = _currentUserService.GetAffiliateId();
            if (userAffiliateId.HasValue)
            {
                return await query.FirstOrDefaultAsync(la => la.Id == applicationId && la.AffiliateId == userAffiliateId.Value);
            }
        }

        return null;
    }

    private async Task<bool> CanAccessApplication(int applicationId, ClaimsPrincipal user)
    {
        var application = await GetApplicationWithAccessCheck(applicationId, user);
        return application != null;
    }
}