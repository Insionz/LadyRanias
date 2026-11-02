using LadyRanias.App.Components.CatImages.Api;
using LadyRanias.App.Components.CatImages.Impl;
using LadyRanias.App.Components.Cats.Api;
using LadyRanias.App.Components.Cats.Impl;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LadyRanias.Tests.ComponentTests;

[TestClass]
public sealed class CatImageApiTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static WebApplicationFactory<Program> _factory = null!;
    private HttpClient _httpClient = null!;


    [ClassInitialize]
    public static void StartApp(TestContext context)
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton<ICatDatabase, InMemoryCatDatabase>();
                    services.AddSingleton<ICatImageDatabase, InMemoryCatImageDatabase>();
                    services.AddSingleton<IImageStore, InMemoryImageStore>();

                }));
    }

    [ClassCleanup]
    public static void StopApp()
    {
        _factory.Dispose();
    }

    [TestInitialize]
    public void CreateClient()
    {
        _httpClient = _factory.CreateClient();
    }

    [TestMethod]
    public async Task Upload_png_returns_created()
    {
        var cat = await CreateCatAsync();
        using var form = ImageForm("image/png", "kiara.png");

        var response = await _httpClient.PostAsync($"api/admin/cats/{cat.Id}/images", form);
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

        var image = await response.Content.ReadFromJsonAsync<CatImageDto>(Json);
        Assert.IsNotNull(image);
    }

    [TestMethod]
    public async Task Upload_for_unknown_cat_returns_not_found()
    {
        var form = ImageForm("image/png", "kiara.png");

        var response = await _httpClient.PostAsync($"api/admin/cats/{Guid.NewGuid()}/images", form);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Upload_gif_returns_bad_request()
    {
        var cat = await CreateCatAsync();
        using var form = ImageForm("image/gif", "kiara.gif");
        var response = await _httpClient.PostAsync($"api/admin/cats/{cat.Id}/images", form);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<CatDto> CreateCatAsync() 
    {
        var newCat = new CreateCatDto
        {
            Name = "Mons",
            Sex = SexEnum.Male,
            BirthDate = new DateOnly(2024, 6, 1),
            Description = "Testkatt",
        };

        var createResponse = await _httpClient.PostAsJsonAsync("api/admin/cats", newCat, Json);
        Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);

        var cat = await createResponse.Content.ReadFromJsonAsync<CatDto>(Json);
        Assert.IsNotNull(cat);

        return cat;
    }

    private static MultipartFormDataContent ImageForm(string contentType, string fileName)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47]);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "formFile", fileName);
        return form;
    }
}
