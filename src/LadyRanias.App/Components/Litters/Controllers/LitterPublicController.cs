using LadyRanias.App.Components.Litters.Api;
using Microsoft.AspNetCore.Mvc;

namespace LadyRanias.App.Components.Litters.Controllers;

[ApiController]
[Route("api/public/litters")]
public sealed class LitterPublicController(ILitterHandler handler) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<LitterDto>> GetAll(CancellationToken ct)
    {
        return handler.GetAllAsync(ct);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LitterDto>> Get(Guid id, CancellationToken ct)
    {
        var litter = await handler.GetAsync(id, ct);

        return litter == null ? NotFound() : litter;
    }
}
