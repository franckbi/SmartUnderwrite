using SmartUnderwrite.Api.Models.Document;
using System.Security.Claims;

namespace SmartUnderwrite.Api.Services;

public interface IDocumentService
{
    Task<DocumentUploadResponse> UploadDocumentAsync(DocumentUploadRequest request, ClaimsPrincipal user);
    Task<DocumentDownloadResponse?> GetDocumentAsync(int documentId, ClaimsPrincipal user);
    Task<bool> DeleteDocumentAsync(int documentId, ClaimsPrincipal user);
    Task<List<DocumentUploadResponse>> GetApplicationDocumentsAsync(int applicationId, ClaimsPrincipal user);
}