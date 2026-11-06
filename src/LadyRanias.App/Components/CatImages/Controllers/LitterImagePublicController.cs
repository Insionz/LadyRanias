using LadyRanias.App.Components.CatImages.Api;
using Microsoft.AspNetCore.Mvc;

namespace LadyRanias.App.Components.CatImages.Controllers;

[ApiController]
[Route("api/public/litters/{litterId:guid}/images")]
public sealed class LitterImagePublicController(ICatImageHandler handler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CatImageDto>>> GetAll(Guid litterId, CancellationToken cancellationToken)
    {
        var images = await handler.GetAllForLitterAsync(litterId, cancellationToken);

        return images == null ? NotFound() : Ok(images);
    }

    [HttpGet("{imageId:guid}")]
    public async Task<IActionResult> Get(
        Guid litterId,
        Guid imageId,
        [FromQuery] CatImageSize size = CatImageSize.Large,
        CancellationToken cancellationToken = default)
    {
        var file = await handler.OpenAsync(litterId, imageId, size, cancellationToken);

        if (file == null) return NotFound();

        // Samme som for katter: et bilde endres aldri, så nettleseren kan beholde det.
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return File(file.Content, file.ContentType);
    }
}
