using HomeDiary_api.Models;
using HomeDiary_api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HomeDiary_api.Controllers;

[ApiController]
[Route("api/event-images")]
public class EventImageController(
    IEventImageRepository repo,
    IApplicationParameterRepository applicationParameters) : ControllerBase
{
    private const long AbsoluteMaxRequestSize = 25 * 1024 * 1024;
    private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
    };

    [HttpGet("by-event/{eventId:int}")]
    public async Task<ActionResult<IEnumerable<EventImage>>> GetForEvent(int eventId)
    {
        return Ok(await repo.GetForEventAsync(eventId));
    }

    [HttpGet("{id:int}/content")]
    public async Task<IActionResult> Content(int id)
    {
        var image = await repo.GetFileAsync(id);
        return image is null
            ? NotFound()
            : File(image.ImageData, image.ContentType);
    }

    [HttpPost("by-event/{eventId:int}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(AbsoluteMaxRequestSize)]
    public async Task<ActionResult<EventImage>> Upload(int eventId, IFormFile file)
    {
        if (!await repo.EventExistsAsync(eventId)) return NotFound("Event not found.");
        var settings = await applicationParameters.GetApplicationSettingsAsync();
        var maxFileSize = settings.MaximumImageUploadMegabytes * 1024L * 1024L;
        if (file.Length is <= 0 || file.Length > maxFileSize)
            return BadRequest(
                $"Images must be between 1 byte and {settings.MaximumImageUploadMegabytes} MB.");

        var fileName = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(fileName);
        if (!AllowedTypes.TryGetValue(extension, out var contentType))
            return BadRequest("Supported image types are JPEG, PNG, GIF, and WebP.");

        await using var memory = new MemoryStream();
        await file.CopyToAsync(memory);
        var data = memory.ToArray();
        if (!HasValidSignature(extension, data))
            return BadRequest("The uploaded file does not contain a valid supported image.");

        var image = await repo.CreateAsync(new EventImage
        {
            EventId = eventId,
            FileName = fileName,
            ContentType = contentType,
            FileSize = file.Length,
            ImageData = data,
            CreatedById = CurrentUserId(),
        });
        return CreatedAtAction(nameof(Content), new { id = image.EventImageId }, image);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        return await repo.DeleteAsync(id) ? NoContent() : NotFound();
    }

    private static bool HasValidSignature(string extension, byte[] data)
    {
        if (data.Length < 12) return false;
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => data[0] == 0xff && data[1] == 0xd8 && data[2] == 0xff,
            ".png" => data.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }),
            ".gif" => data.AsSpan(0, 6).SequenceEqual("GIF87a"u8) || data.AsSpan(0, 6).SequenceEqual("GIF89a"u8),
            ".webp" => data.AsSpan(0, 4).SequenceEqual("RIFF"u8) && data.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            _ => false,
        };
    }

    private int CurrentUserId()
    {
        return int.TryParse(User.FindFirst("homediary_user_id")?.Value, out var userId)
            ? userId
            : throw new InvalidOperationException("The authenticated HomeDiary user ID is missing.");
    }
}
