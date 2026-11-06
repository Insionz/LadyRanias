using System.ComponentModel.DataAnnotations;
using SkiaSharp;

namespace LadyRanias.App.Components.CatImages.Impl;

internal sealed class SkiaSharpImageProcessor : IImageProcessor
{
    /// <summary>Lengste side i piksler.</summary>
    internal const int ThumbnailMaxSize = 400;

    /// <summary>Lengste side i piksler.</summary>
    internal const int LargeMaxSize = 1600;

    private const int WebpQuality = 80;

    public async Task<ProcessedImage> ProcessAsync(Stream input, CancellationToken cancellationToken)
    {
        // Leser hele fila inn i minnet (maks 10 MB, sjekket av handleren). Skia trenger en strøm den kan spole i.
        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer, cancellationToken);

        var bytes = buffer.ToArray();
        using var data = SKData.CreateCopy(bytes);

        // Avslører filer som bare later som de er bilder (feil Content-Type): da finner Skia ingen dekoder.
        using var codec = SKCodec.Create(data)
            ?? throw new ValidationException("The file is not a valid image.");

        using var decoded = SKBitmap.Decode(codec)
            ?? throw new ValidationException("The file is not a valid image.");

        // Mobiler lagrer ofte bildet liggende, med en EXIF-merknad om hvilken vei det skal vises.
        // Vi roterer pikslene selv, fordi metadataen (og dermed merknaden) ikke blir med videre.
        using var oriented = ApplyOrientation(decoded, codec.EncodedOrigin);

        // Skia skriver aldri EXIF, XMP eller IPTC når den lagrer et bilde.
        // Ved å tegne og lagre på nytt forsvinner derfor GPS-posisjon, kameramodell og annen metadata.
        var thumbnail = ResizeToWebp(oriented, ThumbnailMaxSize);
        var large = ResizeToWebp(oriented, LargeMaxSize);

        // Datoen leses fra originalen. I de nye filene finnes den ikke lenger.
        return new ProcessedImage(thumbnail, large, "image/webp", ExifDate.TryRead(bytes));
    }

    private static byte[] ResizeToWebp(SKBitmap source, int maxSize)
    {
        var (width, height) = FitWithin(source.Width, source.Height, maxSize);

        // Små bilder forstørres ikke, de blir bare konvertert.
        if (width == source.Width && height == source.Height)
        {
            return Encode(source);
        }

        using var resized = source.Resize(
            new SKImageInfo(width, height, source.ColorType, source.AlphaType),
            new SKSamplingOptions(SKCubicResampler.Mitchell))
            ?? throw new InvalidOperationException("Could not resize image.");

        return Encode(resized);
    }

    private static byte[] Encode(SKBitmap bitmap)
    {
        using var encoded = bitmap.Encode(SKEncodedImageFormat.Webp, WebpQuality)
            ?? throw new InvalidOperationException("Could not encode image as WebP.");

        return encoded.ToArray();
    }

    /// <summary>Største størrelse som får plass innenfor maxSize × maxSize, med samme form som originalen.</summary>
    internal static (int Width, int Height) FitWithin(int width, int height, int maxSize)
    {
        if (width <= maxSize && height <= maxSize)
        {
            return (width, height);
        }

        var scale = Math.Min((double)maxSize / width, (double)maxSize / height);

        return (
            Math.Max(1, (int)Math.Round(width * scale)),
            Math.Max(1, (int)Math.Round(height * scale)));
    }

    /// <summary>Snur pikslene slik EXIF-orienteringen sier, så bildet står riktig uten metadata.</summary>
    private static SKBitmap ApplyOrientation(SKBitmap bitmap, SKEncodedOrigin origin)
    {
        if (origin == SKEncodedOrigin.TopLeft)
        {
            return bitmap.Copy() ?? throw new InvalidOperationException("Could not copy image.");
        }

        float w = bitmap.Width;
        float h = bitmap.Height;

        // Hver matrise flytter et punkt (x, y) i originalen til riktig plass i det ferdige bildet:
        // x' = scaleX·x + skewX·y + transX,  y' = skewY·x + scaleY·y + transY
        var matrix = origin switch
        {
            SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, w, 0, 1, 0, 0, 0, 1),     // speilvendt vannrett
            SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, w, 0, -1, h, 0, 0, 1), // rotert 180°
            SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, h, 0, 0, 1),   // speilvendt loddrett
            SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),       // speilvendt diagonalt
            SKEncodedOrigin.RightTop => new SKMatrix(0, -1, h, 1, 0, 0, 0, 0, 1),     // rotert 90° med klokka
            SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, h, -1, 0, w, 0, 0, 1), // speilvendt og rotert
            SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, w, 0, 0, 1),   // rotert 90° mot klokka
            _ => SKMatrix.Identity,
        };

        // Ved 90° og 270° bytter bredde og høyde plass.
        var swapsSides = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;

        var result = swapsSides
            ? new SKBitmap(bitmap.Height, bitmap.Width, bitmap.ColorType, bitmap.AlphaType)
            : new SKBitmap(bitmap.Width, bitmap.Height, bitmap.ColorType, bitmap.AlphaType);

        using var canvas = new SKCanvas(result);
        using var image = SKImage.FromBitmap(bitmap);
        canvas.SetMatrix(matrix);
        canvas.DrawImage(image, 0, 0, SKSamplingOptions.Default);

        return result;
    }
}
