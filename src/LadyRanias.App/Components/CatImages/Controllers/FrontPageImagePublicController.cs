using LadyRanias.App.Components.CatImages.Api;
using Microsoft.AspNetCore.Mvc;

namespace LadyRanias.App.Components.CatImages.Controllers;

[ApiController]
[Route("api/public/site/{ownerId:guid}/images")]
public sealed class FrontPageImagePublicController(ICatImageHandler handler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CatImageDto>>> GetAll(Guid ownerId, CancellationToken cancellationToken)
    {
        if (ownerId != FrontPageImages.OwnerId) return NotFound();

        return Ok(await handler.GetAllForFrontPageAsync(cancellationToken));
    }

    [HttpGet("{imageId:guid}")]
    public async Task<IActionResult> Get(
        Guid ownerId,
        Guid imageId,
        [FromQuery] CatImageSize size = CatImageSize.Large,
        CancellationToken cancellationToken = default)
    {
        if (ownerId != FrontPageImages.OwnerId) return NotFound();

        var file = await handler.OpenAsync(ownerId, imageId, size, cancellationToken);
        if (file == null) return NotFound();

        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return File(file.Content, file.ContentType);
    }
}
