using LadyRanias.App.Components.Auth;
using LadyRanias.App.Components.CatImages;
using LadyRanias.App.Components.Cats;
using LadyRanias.App.Components.Contact;
using LadyRanias.App.Components.Litters;
using LadyRanias.App.Components.Storage;
using Microsoft.AspNetCore.HttpOverrides;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Appen står bak en proxy: Vite under utvikling, og Azure Container Apps i drift.
// Proxyen forteller hvilken adresse brukeren egentlig gikk til (X-Forwarded-Host og -Proto).
// Uten dette bygger appen tilbakeadressen til Entra med feil port eller http i stedet for https.
// Som standard stoles det bare på proxyer på samme maskin (localhost).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto
        | ForwardedHeaders.XForwardedHost;

    if (builder.Configuration.GetValue<bool>("ForwardedHeaders:TrustAllProxies"))
    {
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    }
});

builder.UseAuthComponent();
builder.UseStorage();
builder.UseCatsComponent();
builder.UseCatImagesComponent();
builder.UseLittersComponent();
builder.UseContactComponent();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Først, så alt etterpå ser riktig adresse.
app.UseForwardedHeaders();

app.UseDefaultFiles();     // "/" to index.html
app.UseStaticFiles();      // serverer filene i wwwroot
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();      // etter innlogging, før controllerne ([EnableRateLimiting] på kontaktskjemaet)
app.MapControllers();
// React-ruter som /katter og /admin får index.html, så React kan vise riktig side.
// nonfile: bare adresser UTEN filendelse. Ellers ville også /assets/index-….js fått index.html,
//          fordi rutene kjøres før filene leveres (det var feilen i steg 11).
// regex:   ikke api/... Ukjente API-adresser skal gi 404, ikke en nettside.
app.MapFallbackToFile("{*path:nonfile:regex(^(?!api/).*$)}", "index.html");

app.Run();
