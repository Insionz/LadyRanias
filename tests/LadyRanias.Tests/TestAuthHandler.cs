using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace LadyRanias.Tests;

/// <summary>
/// «Innlogging» som bare finnes i testene. I stedet for å snakke med Entra leser den to headere:
///   X-Test-User:  hvem som er innlogget (mangler den, er ingen innlogget)
///   X-Test-Roles: kommaseparerte roller, for eksempel «Admin»
/// Claims-ene får samme navn som fra Entra («name» og «roles»), så appen ikke merker forskjell.
/// </summary>
internal sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string UserHeader = "X-Test-User";
    public const string RolesHeader = "X-Test-Roles";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var user = Request.Headers[UserHeader].ToString();

        // NoResult = «ikke innlogget». Da svarer [Authorize] med 401 (challenge).
        if (string.IsNullOrEmpty(user))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim> { new("name", user) };

        var roles = Request.Headers[RolesHeader].ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        claims.AddRange(roles.Select(role => new Claim("roles", role)));

        // roleType: "roles" gjør at User.IsInRole("Admin") og RequireRole("Admin") ser etter claimet «roles»,
        // slik Microsoft.Identity.Web gjør for ekte tokens.
        var identity = new ClaimsIdentity(claims, SchemeName, nameType: "name", roleType: "roles");
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

internal static class TestAuth
{
    /// <summary>
    /// Bytter ut Entra med TestAuthHandler. Alle fire standardene settes, fordi Microsoft.Identity.Web har satt
    /// sine egne. Ellers ville testene prøvd å sende oss til Entra.
    /// </summary>
    public static IServiceCollection AddTestAuth(this IServiceCollection services)
    {
        services.AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

        services.Configure<AuthenticationOptions>(options =>
        {
            options.DefaultScheme = TestAuthHandler.SchemeName;
            options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
            options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            options.DefaultForbidScheme = TestAuthHandler.SchemeName;
        });

        return services;
    }

    /// <summary>Innlogget med gitte roller. Ingen roller = innlogget, men uten tilgang til admin.</summary>
    public static HttpClient As(this HttpClient client, string user, params string[] roles)
    {
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, user);
        if (roles.Length > 0)
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, string.Join(',', roles));
        }
        return client;
    }

    public static HttpClient AsAdmin(this HttpClient client) => client.As("mamma@example.com", "Admin");
}
