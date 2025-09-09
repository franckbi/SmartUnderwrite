namespace SmartUnderwrite.Core.Entities;

public class Document
{
    public int Id { get; set; }
    public int LoanApplicationId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public LoanApplication LoanApplication { get; set; } = null!;
}