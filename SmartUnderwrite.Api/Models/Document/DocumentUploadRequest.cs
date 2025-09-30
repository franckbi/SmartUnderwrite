using System.ComponentModel.DataAnnotations;

namespace SmartUnderwrite.Api.Models.Document;

public class DocumentUploadRequest
{
    [Required]
    public int LoanApplicationId { get; set; }

    [Required]
    public IFormFile File { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }
}

public class DocumentUploadResponse
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? Description { get; set; }
}

public class DocumentDownloadResponse
{
    public Stream FileStream { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
}