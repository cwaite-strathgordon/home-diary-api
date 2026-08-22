using HomeDiary_api.Models;
using HomeDiary_api.Repositories;
using HomeDiary_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace HomeDiary_api.Controllers;

[ApiController]
[Route("api/event-documents")]
public class EventDocumentController(
    IEventDocumentRepository repo,
    DocumentTextExtractor textExtractor) : ControllerBase
{
    private const long MaxFileSize = 20 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions =
        [".pdf", ".docx", ".txt", ".md", ".csv"];

    [HttpGet("by-event/{eventId:int}")]
    public async Task<ActionResult<IEnumerable<EventDocument>>> GetForEvent(int eventId)
    {
        return Ok(await repo.GetForEventAsync(eventId));
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<EventDocument>>> Search(
        [FromQuery] string query, [FromQuery] int? eventId)
    {
        if (string.IsNullOrWhiteSpace(query)) return BadRequest("A search query is required.");
        return Ok(await repo.SearchAsync(query.Trim(), eventId));
    }

    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var document = await repo.GetFileAsync(id);
        return document is null
            ? NotFound()
            : File(document.FileData, document.ContentType, document.FileName);
    }

    [HttpPost("by-event/{eventId:int}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<ActionResult<EventDocument>> Upload(int eventId, IFormFile file)
    {
        if (!await repo.EventExistsAsync(eventId)) return NotFound("Event not found.");
        if (file.Length is <= 0 or > MaxFileSize)
            return BadRequest("Files must be between 1 byte and 20 MB.");
        var fileName = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return BadRequest("Supported file types are PDF, DOCX, TXT, Markdown, and CSV.");

        await using var memory = new MemoryStream();
        await file.CopyToAsync(memory);
        var data = memory.ToArray();
        string extractedText;
        try { extractedText = textExtractor.Extract(fileName, data); }
        catch { extractedText = string.Empty; }

        var document = await repo.CreateAsync(new EventDocument
        {
            EventId = eventId,
            FileName = fileName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream" : file.ContentType,
            FileSize = file.Length,
            FileData = data,
            ExtractedText = extractedText,
            CreatedById = CurrentUserId()
        });
        return CreatedAtAction(nameof(Download), new { id = document.EventDocumentId }, document);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        return await repo.DeleteAsync(id) ? NoContent() : NotFound();
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventDocument>>> GetAll()
    {
        return Ok(await repo.GetAllAsync());
    }

    [HttpGet("count")]
    public async Task<ActionResult<int>> GetCount()
    {
        return Ok(await repo.GetCountAsync());
    }

    private int CurrentUserId()
    {
        return int.TryParse(User.FindFirst("homediary_user_id")?.Value, out var userId)
            ? userId
            : throw new InvalidOperationException("The authenticated HomeDiary user ID is missing.");
    }
}
