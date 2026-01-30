namespace Xplore.Domain.Entities;

/// <summary>
/// Represents a document uploaded for a museum (PDF guides, brochures, etc).
/// </summary>
public class Document
{
    public Guid Id { get; set; }
    public Guid MuseumId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    
    // Navigation property
    public Museum? Museum { get; set; }
}

/// <summary>
/// Status of document processing.
/// </summary>
public enum DocumentStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}
