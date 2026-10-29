using LadyRanias.App.Components.Cats.Api;
using Microsoft.AspNetCore.Mvc;

namespace LadyRanias.App.Components.Cats.Controllers;

[ApiController]
[Route("api/public/cats")]
public sealed class CatPublicController(ICatHandler handler) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<CatDto>> GetAll(CancellationToken ct)
    {
        var allCats = handler.GetAllAsync(ct);
        return allCats;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CatDto>> Get(Guid id, CancellationToken ct)
    {
        var cat = await handler.GetAsync(id, ct);
        
        if (cat == null)
        {
            return NotFound();
        }

        return cat;
    }
}
