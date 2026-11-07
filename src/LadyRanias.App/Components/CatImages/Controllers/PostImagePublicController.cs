using LadyRanias.App.Components.CatImages.Api;
using Microsoft.AspNetCore.Mvc;

namespace LadyRanias.App.Components.CatImages.Controllers;

[ApiController]
[Route("api/public/posts/{postId:guid}/images")]
public sealed class PostImagePublicController(ICatImageHandler handler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CatImageDto>>> GetAll(Guid postId, CancellationToken cancellationToken)
    {
        var images = await handler.GetAllForPostAsync(postId, cancellationToken);

        return images == null ? NotFound() : Ok(images);
    }

    [HttpGet("{imageId:guid}")]
    public async Task<IActionResult> Get(
        Guid postId,
        Guid imageId,
        [FromQuery] CatImageSize size = CatImageSize.Large,
        CancellationToken cancellationToken = default)
    {
        var file = await handler.OpenAsync(postId, imageId, size, cancellationToken);

        if (file == null) return NotFound();

        // Samme som for katter: et bilde endres aldri, så nettleseren kan beholde det.
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return File(file.Content, file.ContentType);
    }
}
