using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LadyRanias.App.Components.Auth.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    [HttpGet]
    [Route("login")]
    public IActionResult Login(string? returnUrl)
    {
        if (!Url.IsLocalUrl(returnUrl)) 
            returnUrl = "/admin";

        return Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<MeDto> Me()
    {
        return Ok(new MeDto(
            Name: User.FindFirst("name")?.Value, 
            Email: User.FindFirst("email")?.Value ?? User.FindFirst("preferred_username")?.Value, 
            IsAdmin: User.IsInRole("Admin")));

    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        return SignOut(new AuthenticationProperties { RedirectUri = "/" }, 
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
    }
}
