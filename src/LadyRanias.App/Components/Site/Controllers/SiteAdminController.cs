using LadyRanias.App.Components.Site.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace LadyRanias.App.Components.Site.Controllers;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/site")]
public sealed class SiteAdminController(ISiteHandler handler) : ControllerBase
{
    [HttpPut]
    public async Task<ActionResult<SiteSettingsDto>> Save(SaveSiteSettingsDto settings, CancellationToken ct)
    {
        try
        {
            return await handler.SaveAsync(settings, ct);
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(detail: ex.Message);
        }
    }
}
