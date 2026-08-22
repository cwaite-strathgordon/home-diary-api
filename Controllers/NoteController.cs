using HomeDiary_api.Models;
using HomeDiary_api.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace HomeDiary_api.Controllers;

[ApiController]
[Route("api/notes")]
public class NoteController(INoteRepository repo) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Note>>> GetAll(
        [FromQuery] int? linkObjectTypeId,
        [FromQuery] int? linkObjectId)
    {
        if (linkObjectId.HasValue && !linkObjectTypeId.HasValue)
            return BadRequest("linkObjectTypeId is required when filtering by linkObjectId.");

        return Ok(await repo.GetAllAsync(linkObjectTypeId, linkObjectId));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Note>> GetById(int id)
    {
        var note = await repo.GetByIdAsync(id);
        return note is null ? NotFound() : Ok(note);
    }

    [HttpPost]
    public async Task<ActionResult<Note>> Create(CreateNoteRequest request)
    {
        if (!await repo.LinkTargetExistsAsync(request.LinkObjectTypeId, request.LinkObjectId))
            return BadRequest("The specified Contact or Event does not exist.");

        var note = new Note
        {
            LinkObjectTypeId = request.LinkObjectTypeId,
            LinkObjectId = request.LinkObjectId,
            Subject = request.Subject.Trim(),
            NoteText = request.NoteText?.Trim() ?? string.Empty,
            CreatedById = CurrentUserId()
        };

        var created = await repo.CreateAsync(note);
        return CreatedAtAction(nameof(GetById), new { id = created.NoteId }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateNoteRequest request)
    {
        var note = await repo.GetByIdAsync(id);
        if (note is null) return NotFound();

        if (!await repo.LinkTargetExistsAsync(request.LinkObjectTypeId, request.LinkObjectId))
            return BadRequest("The specified Contact or Event does not exist.");

        note.LinkObjectTypeId = request.LinkObjectTypeId;
        note.LinkObjectId = request.LinkObjectId;
        note.Subject = request.Subject.Trim();
        note.NoteText = request.NoteText?.Trim() ?? string.Empty;
        note.UpdatedById = CurrentUserId();

        return await repo.UpdateAsync(note) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        return await repo.DeleteAsync(id) ? NoContent() : NotFound();
    }

    private int CurrentUserId()
    {
        return int.TryParse(User.FindFirst("homediary_user_id")?.Value, out var userId)
            ? userId
            : throw new InvalidOperationException("The authenticated HomeDiary user ID is missing.");
    }
}

public class CreateNoteRequest
{
    [Range(1, int.MaxValue)] public int LinkObjectTypeId { get; set; }
    [Range(1, int.MaxValue)] public int LinkObjectId { get; set; }
    [Required, StringLength(255)] public string Subject { get; set; } = string.Empty;
    [StringLength(10000)] public string? NoteText { get; set; }
}

public class UpdateNoteRequest : CreateNoteRequest { }
