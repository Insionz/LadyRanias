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
                    services.AddTestAuth();

                }));

        // Start appen nå, én gang. Ellers kan to tester som kjører samtidig starte hver sin app
        // (med hver sin database i minnet) første gang de ber om en klient.
        _ = _factory.Server;
    }

    [ClassCleanup]
    public static void StopApp()
    {
        _factory.Dispose();
    }

    [TestInitialize]
    public void CreateClient()
    {
        _httpClient = _factory.CreateClient().AsAdmin();
    }

    [TestMethod]
    public async Task Upload_png_returns_created()
    {
        var cat = await CreateCatAsync();
        using var form = ImageForm(CreatePng(), "image/png", "kiara.png");

        var response = await _httpClient.PostAsync($"api/admin/cats/{cat.Id}/images", form);

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var image = await response.Content.ReadFromJsonAsync<CatImageDto>(Json);
        Assert.IsNotNull(image);
    }

    [TestMethod]
    public async Task Upload_for_unknown_cat_returns_not_found()
    {
        using var form = ImageForm(CreatePng(), "image/png", "kiara.png");

        var response = await _httpClient.PostAsync($"api/admin/cats/{Guid.NewGuid()}/images", form);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Upload_gif_returns_bad_request()
    {
        var cat = await CreateCatAsync();
        using var form = ImageForm(CreatePng(), "image/gif", "kiara.gif");

        var response = await _httpClient.PostAsync($"api/admin/cats/{cat.Id}/images", form);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Upload_file_that_is_not_an_image_returns_bad_request()
    {
        // Later som om det er en PNG, men innholdet er tekst.
        var cat = await CreateCatAsync();
        using var form = ImageForm("ikke et bilde"u8.ToArray(), "image/png", "kiara.png");

        var response = await _httpClient.PostAsync($"api/admin/cats/{cat.Id}/images", form);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Uploaded_image_is_listed_and_can_be_downloaded()
    {
        var cat = await CreateCatAsync();
        var uploaded = await UploadAsync(cat.Id);

        var images = await _httpClient.GetFromJsonAsync<List<CatImageDto>>($"api/public/cats/{cat.Id}/images", Json);
        Assert.IsNotNull(images);
        Assert.HasCount(1, images);
        Assert.AreEqual(uploaded.ImageId, images[0].ImageId);

        var response = await _httpClient.GetAsync($"api/public/cats/{cat.Id}/images/{uploaded.ImageId}?size=thumb");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("image/webp", response.Content.Headers.ContentType?.MediaType);
        Assert.IsNotNull(response.Headers.CacheControl?.MaxAge, "Bildet skal kunne caches av nettleseren");
    }

    [TestMethod]
    public async Task Images_for_unknown_cat_returns_not_found()
    {
        var response = await _httpClient.GetAsync($"api/public/cats/{Guid.NewGuid()}/images");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Deleted_image_is_gone()
    {
        var cat = await CreateCatAsync();
        var uploaded = await UploadAsync(cat.Id);

        var deleteResponse = await _httpClient.DeleteAsync($"api/admin/cats/{cat.Id}/images/{uploaded.ImageId}");
        Assert.AreEqual(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _httpClient.GetAsync($"api/public/cats/{cat.Id}/images/{uploaded.ImageId}");
        Assert.AreEqual(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [TestMethod]
    public async Task Deleting_cat_also_deletes_its_image_files()
    {
        var cat = await CreateCatAsync();
        var uploaded = await UploadAsync(cat.Id);

        var deleteResponse = await _httpClient.DeleteAsync($"api/admin/cats/{cat.Id}");
        Assert.AreEqual(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Kikker rett i lageret appen bruker, for å bevise at filene faktisk er borte.
        var store = _factory.Services.GetRequiredService<IImageStore>();
        var thumbnail = CatImageHandler.BlobName(cat.Id, uploaded.ImageId, CatImageHandler.ThumbnailSuffix);
        var large = CatImageHandler.BlobName(cat.Id, uploaded.ImageId, CatImageHandler.LargeSuffix);
        Assert.IsNull(await store.OpenAsync(thumbnail, CancellationToken.None));
        Assert.IsNull(await store.OpenAsync(large, CancellationToken.None));
    }

    private async Task<CatImageDto> UploadAsync(Guid catId)
    {
        using var form = ImageForm(CreatePng(), "image/png", "kiara.png");
        var response = await _httpClient.PostAsync($"api/admin/cats/{catId}/images", form);
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

        var image = await response.Content.ReadFromJsonAsync<CatImageDto>(Json);
        Assert.IsNotNull(image);
        return image;
    }

    private async Task<CatDto> CreateCatAsync()
    {
        var newCat = new CreateCatDto
        {
            Breed = Breed.MaineCoon,
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

    /// <summary>Et ekte, lite PNG-bilde laget i minnet.</summary>
    private static byte[] CreatePng() => TestImages.Png(20, 10);

    private static MultipartFormDataContent ImageForm(byte[] bytes, string contentType, string fileName)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "formFile", fileName);
        return form;
    }
}
