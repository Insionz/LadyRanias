using LadyRanias.App.Components.Posts.Api;
using Microsoft.AspNetCore.Mvc;

namespace LadyRanias.App.Components.Posts.Controllers;

[ApiController]
[Route("api/public/posts")]
public sealed class PostPublicController(IPostHandler handler) : ControllerBase
{
    /// <summary>Publiserte innlegg. ?catId=… gir bare de som handler om den katten.</summary>
    [HttpGet]
    public Task<IReadOnlyList<PostDto>> GetAll([FromQuery] Guid? catId, CancellationToken ct) =>
        handler.GetPublishedAsync(catId, ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PostDto>> Get(Guid id, CancellationToken ct)
    {
        // Et utkast finnes ikke for besøkende.
        var post = await handler.GetAsync(id, includeDrafts: false, ct);
        return post == null ? NotFound() : post;
    }
}
