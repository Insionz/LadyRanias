using LadyRanias.App.Components.Contact.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LadyRanias.App.Components.Contact.Controllers;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/contact")]
public sealed class ContactAdminController(IContactHandler handler) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ContactMessageDto>> GetAll(CancellationToken ct)
    {
        return handler.GetAllAsync(ct);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<ContactMessageDto>> ChangeStatus(Guid id, ChangeContactStatusDto change, CancellationToken ct)
    {
        var message = await handler.ChangeStatusAsync(id, change.Status, ct);

        if (message == null)
        {
            return NotFound();
        }

        return message;
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        return await handler.DeleteAsync(id, ct) ? NoContent() : NotFound();
    }
}
