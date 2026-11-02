using LadyRanias.App.Components.CatImages.Api;
using Microsoft.AspNetCore.Mvc;

namespace LadyRanias.App.Components.CatImages.Controllers;

[ApiController]
[Route("api/public/cats/{catId:guid}/images")]
public sealed class CatImagePublicController(ICatImageHandler handler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CatImageDto>>> GetAll(Guid catId, CancellationToken cancellationToken)
    {
        var images = await handler.GetAllAsync(catId, cancellationToken);

        if (images == null) return NotFound();

        // Ok(...) og ikke bare "return images": C# kan ikke konvertere automatisk fra et interface (IReadOnlyList)
        // til ActionResult<T>, slik det kan med en vanlig klasse som CatDto.
        return Ok(images);
    }

    [HttpGet("{imageId:guid}")]
    public async Task<IActionResult> Get(
        Guid catId,
        Guid imageId,
        [FromQuery] CatImageSize size = CatImageSize.Large,
        CancellationToken cancellationToken = default)
    {
        var file = await handler.OpenAsync(catId, imageId, size, cancellationToken);

        if (file == null) return NotFound();

        // Et bilde får ny id ved hver opplasting og endres aldri,
        // så nettleseren kan trygt beholde det i et år uten å spørre på nytt.
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";

        // File(...) sender strømmen og lukker den når svaret er ferdig.
        return File(file.Content, file.ContentType);
    }
}
