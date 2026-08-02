using HomeDiary_api.Models;
using HomeDiary_api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HomeDiary_api.Controllers;

[ApiController]
[Route("api/contacts")]
public class ContactController(IContactRepository repo) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Contact>>> GetAll()
    {
        var contacts = await repo.GetAllAsync();
        return Ok(contacts);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Contact>> GetById(int id)
    {
        var contact = await repo.GetByIdAsync(id);
        return contact is null ? NotFound() : Ok(contact);
    }

    [HttpPost]
    public async Task<ActionResult<Contact>> Create(Contact contact)
    {
        var created = await repo.CreateAsync(contact);
        return CreatedAtAction(nameof(GetById), new { id = created.ContactId }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Contact contact)
    {
        if (id != contact.ContactId) return BadRequest("Route id does not match body ContactId.");
        var updated = await repo.UpdateAsync(contact);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await repo.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
