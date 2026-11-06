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
        // Bare våre egne katter. Kattungene vises i kullet, og eksterne hannkatter hører ikke hjemme her.
        return handler.GetOwnAsync(ct);
    }

    /// <summary>Minnesiden: våre katter som er døde.</summary>
    [HttpGet("memorial")]
    public Task<IReadOnlyList<CatDto>> GetMemorial(CancellationToken ct)
    {
        return handler.GetInMemoryAsync(ct);
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
