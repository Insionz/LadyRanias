using LadyRanias.App.Components.Site.Api;
using Microsoft.AspNetCore.Mvc;

namespace LadyRanias.App.Components.Site.Controllers;

[ApiController]
[Route("api/public/site")]
public sealed class SitePublicController(ISiteHandler handler) : ControllerBase
{
    [HttpGet]
    public Task<SiteSettingsDto> Get(CancellationToken ct) => handler.GetAsync(ct);
}
