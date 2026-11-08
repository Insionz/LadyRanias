using LadyRanias.App.Components.Site.Api;
using LadyRanias.App.Components.Site.Impl;
using System.ComponentModel.DataAnnotations;

namespace LadyRanias.Tests.ServiceTests.SiteHandlerTests;

[TestClass]
public sealed class SiteHandlerTests
{
    private readonly SiteHandler _handler = new(new InMemorySiteDatabase());

    [TestMethod]
    public async Task Before_anything_is_saved_the_front_page_looks_like_before()
    {
        var settings = await _handler.GetAsync(CancellationToken.None);

        Assert.AreEqual(SiteHandler.DefaultWelcomeText, settings.WelcomeText);
        Assert.AreEqual(HeroMode.Newest, settings.HeroMode);
        Assert.AreEqual(string.Empty, settings.AboutText);
    }

    [TestMethod]
    public async Task Save_then_get_returns_the_texts_trimmed()
    {
        await _handler.SaveAsync(new SaveSiteSettingsDto { WelcomeText = "  Hei!  ", AboutText = "Om oss\n\nMer", HeroMode = HeroMode.Slideshow }, CancellationToken.None);

        var settings = await _handler.GetAsync(CancellationToken.None);

        Assert.AreEqual("Hei!", settings.WelcomeText);
        Assert.AreEqual("Om oss\n\nMer", settings.AboutText);
        Assert.AreEqual(HeroMode.Slideshow, settings.HeroMode);
    }

    [TestMethod]
    public async Task Fixed_image_must_be_chosen()
    {
        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _handler.SaveAsync(new SaveSiteSettingsDto { HeroMode = HeroMode.Fixed }, CancellationToken.None));
    }

    [TestMethod]
    public async Task Chosen_image_is_only_kept_for_fixed_mode()
    {
        var image = Guid.NewGuid();

        var fixedImage = await _handler.SaveAsync(new SaveSiteSettingsDto { HeroMode = HeroMode.Fixed, HeroImageId = image }, CancellationToken.None);
        var random = await _handler.SaveAsync(new SaveSiteSettingsDto { HeroMode = HeroMode.Random, HeroImageId = image }, CancellationToken.None);

        Assert.AreEqual(image, fixedImage.HeroImageId);
        Assert.IsNull(random.HeroImageId);
    }
}
