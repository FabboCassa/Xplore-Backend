namespace Xplore.API.Controllers;

using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Xplore.Contracts;

/// <summary>
/// Controller for document upload and management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IPublishEndpoint publishEndpoint,
        IWebHostEnvironment environment,
        ILogger<DocumentsController> logger)
    {
        _publishEndpoint = publishEndpoint;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Uploads a document (PDF) for processing and indexing.
    /// </summary>
    /// <param name="museumId">The museum this document belongs to.</param>
    /// <param name="file">The PDF file to upload.</param>
    [HttpPost("{museumId:guid}")]
    [ProducesResponseType(typeof(DocumentUploadResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadDocument(Guid museumId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file provided.");
        }

        if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Only PDF files are supported.");
        }

        // Generate unique document ID and save path
        var documentId = Guid.NewGuid();
        var uploadsFolder = Path.Combine(_environment.ContentRootPath, "uploads");
        Directory.CreateDirectory(uploadsFolder);
        
        var fileName = $"{documentId}_{file.FileName}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        // Save file to disk
        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        _logger.LogInformation("Document {DocumentId} uploaded: {FileName}", documentId, fileName);

        // Publish event for async processing
        await _publishEndpoint.Publish(new DocumentUploadedEvent(
            documentId,
            museumId,
            file.FileName,
            filePath));

        return Accepted(new DocumentUploadResponse(
            documentId,
            "Document uploaded successfully. Processing will begin shortly."));
    }
}

/// <summary>
/// Response returned after successful document upload.
/// </summary>
public record DocumentUploadResponse(Guid DocumentId, string Message);
