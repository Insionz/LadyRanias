using System.Buffers.Binary;
using System.Text;
using SkiaSharp;

namespace LadyRanias.Tests;

/// <summary>Lager ekte bildefiler i minnet til testene.</summary>
internal static class TestImages
{
    public static byte[] Png(int width, int height) => Encode(width, height, SKEncodedImageFormat.Png);

    public static byte[] Jpeg(int width, int height) => Encode(width, height, SKEncodedImageFormat.Jpeg);

    /// <summary>
    /// En JPEG med EXIF-metadata, slik et mobilkamera lager den: orientering og kameramerke.
    /// Skia skriver ikke EXIF selv, så vi legger inn et EXIF-segment (APP1) for hånd rett etter starten av fila.
    /// </summary>
    public static byte[] JpegWithExif(int width, int height, ushort orientation, string make)
    {
        var jpeg = Jpeg(width, height);
        var exif = BuildExif(orientation, make);

        var app1 = new byte[4 + exif.Length];
        app1[0] = 0xFF;
        app1[1] = 0xE1;                                                         // APP1-markør
        BinaryPrimitives.WriteUInt16BigEndian(app1.AsSpan(2), (ushort)(2 + exif.Length)); // lengde, inkludert seg selv
        exif.CopyTo(app1, 4);

        // FF D8 (start av bilde), så EXIF, så resten av den opprinnelige fila.
        return [.. jpeg.AsSpan(0, 2), .. app1, .. jpeg.AsSpan(2)];
    }

    /// <summary>True hvis <paramref name="text"/> finnes som bytes et sted i fila.</summary>
    public static bool ContainsText(byte[] file, string text) =>
        file.AsSpan().IndexOf(Encoding.ASCII.GetBytes(text)) >= 0;

    private static byte[] Encode(int width, int height, SKEncodedImageFormat format)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Orange);

        using var data = bitmap.Encode(format, 90)
            ?? throw new InvalidOperationException($"Could not encode test image as {format}.");

        return data.ToArray();
    }

    /// <summary>"Exif\0\0" + en liten TIFF-struktur (little-endian) med to felt: Make og Orientation.</summary>
    private static byte[] BuildExif(ushort orientation, string make)
    {
        var makeBytes = Encoding.ASCII.GetBytes(make + "\0");

        const int entryCount = 2;
        const int ifdOffset = 8;
        var dataOffset = ifdOffset + 2 + (entryCount * 12) + 4;   // rett etter IFD-en

        var tiff = new byte[dataOffset + makeBytes.Length];
        var span = tiff.AsSpan();

        "II"u8.CopyTo(span);                                       // little-endian
        BinaryPrimitives.WriteUInt16LittleEndian(span[2..], 42);
        BinaryPrimitives.WriteUInt32LittleEndian(span[4..], ifdOffset);
        BinaryPrimitives.WriteUInt16LittleEndian(span[8..], entryCount);

        // Felt 1: Make (0x010F), ASCII, peker til teksten etter IFD-en. Feltene må stå sortert på tag.
        var entry = span[10..];
        BinaryPrimitives.WriteUInt16LittleEndian(entry, 0x010F);
        BinaryPrimitives.WriteUInt16LittleEndian(entry[2..], 2);
        BinaryPrimitives.WriteUInt32LittleEndian(entry[4..], (uint)makeBytes.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(entry[8..], (uint)dataOffset);

        // Felt 2: Orientation (0x0112), SHORT, verdien ligger rett i feltet.
        entry = span[22..];
        BinaryPrimitives.WriteUInt16LittleEndian(entry, 0x0112);
        BinaryPrimitives.WriteUInt16LittleEndian(entry[2..], 3);
        BinaryPrimitives.WriteUInt32LittleEndian(entry[4..], 1);
        BinaryPrimitives.WriteUInt16LittleEndian(entry[8..], orientation);

        // Neste IFD = 0 (ingen flere), allerede 0 i arrayen.
        makeBytes.CopyTo(span[dataOffset..]);

        return [.. "Exif\0\0"u8, .. tiff];
    }
}
