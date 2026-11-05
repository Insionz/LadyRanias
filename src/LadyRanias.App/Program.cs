using LadyRanias.App.Components.Auth;
using LadyRanias.App.Components.CatImages;
using LadyRanias.App.Components.Cats;
using LadyRanias.App.Components.Litters;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.UseAuthComponent();
builder.UseCatsComponent();
builder.UseCatImagesComponent();
builder.UseLittersComponent();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseDefaultFiles();     // "/" to index.html
app.UseStaticFiles();      // serverer filene i wwwroot
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("{*path:regex(^(?!api/).*$)}", "index.html");

app.Run();
