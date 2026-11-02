using System.ComponentModel.DataAnnotations;
using LadyRanias.App.Components.CatImages.Impl;
using SkiaSharp;

namespace LadyRanias.Tests.ServiceTests.ImageProcessorTests;

[TestClass]
public sealed class SkiaSharpImageProcessorTests
{
    private readonly SkiaSharpImageProcessor _processor = new();

    [TestMethod]
    public async Task Large_landscape_image_is_scaled_to_max_sizes()
    {
        using var input = new MemoryStream(TestImages.Jpeg(3000, 2000));

        var result = await _processor.ProcessAsync(input, CancellationToken.None);

        using var thumbnail = Decode(result.Thumbnail);
        using var large = Decode(result.Large);
        Assert.AreEqual(400, thumbnail.Width);
        Assert.AreEqual(267, thumbnail.Height);
        Assert.AreEqual(1600, large.Width);
        Assert.AreEqual(1067, large.Height);
    }

    [TestMethod]
    public async Task Small_image_is_not_upscaled()
    {
        using var input = new MemoryStream(TestImages.Png(200, 100));

        var result = await _processor.ProcessAsync(input, CancellationToken.None);

        using var thumbnail = Decode(result.Thumbnail);
        using var large = Decode(result.Large);
        Assert.AreEqual(200, thumbnail.Width);
        Assert.AreEqual(200, large.Width);
    }

    [TestMethod]
    public async Task Output_is_webp()
    {
        using var input = new MemoryStream(TestImages.Png(50, 50));

        var result = await _processor.ProcessAsync(input, CancellationToken.None);

        Assert.AreEqual("image/webp", result.ContentType);
        using var data = SKData.CreateCopy(result.Large);
        using var codec = SKCodec.Create(data);
        Assert.IsNotNull(codec);
        Assert.AreEqual(SKEncodedImageFormat.Webp, codec.EncodedFormat);
    }

    [TestMethod]
    public async Task Exif_metadata_is_removed()
    {
        var original = TestImages.JpegWithExif(800, 600, orientation: 1, make: "TestPhone");
        Assert.IsTrue(TestImages.ContainsText(original, "TestPhone"), "Testbildet skal ha EXIF før behandling");
        using var input = new MemoryStream(original);

        var result = await _processor.ProcessAsync(input, CancellationToken.None);

        Assert.IsFalse(TestImages.ContainsText(result.Thumbnail, "TestPhone"));
        Assert.IsFalse(TestImages.ContainsText(result.Large, "TestPhone"));
        Assert.IsFalse(TestImages.ContainsText(result.Large, "Exif"));
    }

    [TestMethod]
    public async Task Image_is_rotated_according_to_exif_orientation()
    {
        // Liggende piksler, men EXIF sier "roter 90 grader med klokka" (6). Slik lagrer mange mobiler stående bilder.
        using var input = new MemoryStream(TestImages.JpegWithExif(300, 100, orientation: 6, make: "TestPhone"));

        var result = await _processor.ProcessAsync(input, CancellationToken.None);

        using var large = Decode(result.Large);
        Assert.AreEqual(100, large.Width);
        Assert.AreEqual(300, large.Height);
    }

    [TestMethod]
    public async Task File_that_is_not_an_image_is_rejected()
    {
        using var input = new MemoryStream("dette er ikke et bilde"u8.ToArray());

        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _processor.ProcessAsync(input, CancellationToken.None));
    }

    [TestMethod]
    [DataRow(3000, 2000, 400, 400, 267)]
    [DataRow(2000, 3000, 400, 267, 400)]
    [DataRow(400, 400, 400, 400, 400)]
    [DataRow(100, 50, 400, 100, 50)]
    public void FitWithin_keeps_shape_and_never_upscales(int width, int height, int max, int expectedWidth, int expectedHeight)
    {
        var (w, h) = SkiaSharpImageProcessor.FitWithin(width, height, max);

        Assert.AreEqual(expectedWidth, w);
        Assert.AreEqual(expectedHeight, h);
    }

    private static SKBitmap Decode(byte[] bytes) =>
        SKBitmap.Decode(bytes) ?? throw new AssertFailedException("Resultatet kunne ikke leses som et bilde.");
}
