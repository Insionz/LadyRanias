using LadyRanias.App.Components.Posts.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace LadyRanias.App.Components.Posts.Controllers;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/posts")]
public sealed class PostAdminController(IPostHandler handler) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<PostDto>> GetAll(CancellationToken ct) => handler.GetAllAsync(ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PostDto>> Get(Guid id, CancellationToken ct)
    {
        var post = await handler.GetAsync(id, includeDrafts: true, ct);
        return post == null ? NotFound() : post;
    }

    [HttpPost]
    public async Task<ActionResult<PostDto>> Create(SavePostDto post, CancellationToken ct)
    {
        try
        {
            var created = await handler.CreateAsync(post, ct);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(detail: ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PostDto>> Update(Guid id, SavePostDto post, CancellationToken ct)
    {
        try
        {
            var updated = await handler.UpdateAsync(id, post, ct);
            return updated == null ? NotFound() : updated;
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(detail: ex.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        await handler.DeleteAsync(id, ct) ? NoContent() : NotFound();
}
