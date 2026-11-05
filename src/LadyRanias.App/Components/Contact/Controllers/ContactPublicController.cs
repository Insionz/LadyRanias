using LadyRanias.App.Components.Contact.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LadyRanias.App.Components.Contact.Controllers;

[ApiController]
[Route("api/public/contact")]
public sealed class ContactPublicController(IContactHandler handler) : ControllerBase
{
    /// <summary>
    /// 202 Accepted, ikke 201 Created: meldingen er tatt imot, men varselet er ikke sendt ennå.
    /// Ugyldige felt gir 400 automatisk, takket være [ApiController] og attributtene på DTO-en.
    /// For mange meldinger fra samme IP gir 429, takket være [EnableRateLimiting].
    /// </summary>
    [HttpPost]
    [EnableRateLimiting(ContactComponent.RateLimitPolicy)]
    public async Task<IActionResult> Send(CreateContactMessageDto message, CancellationToken ct)
    {
        await handler.CreateAsync(message, ct);
        return Accepted();
    }
}
