namespace Xplore.Contracts;

/// <summary>
/// Event published when a document is uploaded for processing.
/// </summary>
/// <param name="DocumentId">Unique identifier for the document.</param>
/// <param name="MuseumId">The museum this document belongs to.</param>
/// <param name="FileName">Original file name.</param>
/// <param name="FilePath">Path where the file is stored.</param>
public record DocumentUploadedEvent(
    Guid DocumentId,
    Guid MuseumId,
    string FileName,
    string FilePath);
