using LadyRanias.App.Components.CatImages.Impl;
using System.Buffers.Binary;
using System.Text;

namespace LadyRanias.Tests.ServiceTests.ImageProcessorTests;

[TestClass]
public sealed class ExifDateTests
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Reads_date_time_original_in_both_byte_orders(bool littleEndian)
    {
        var jpeg = JpegWithExif(littleEndian, originalDate: "2026:03:05 10:15:00", changedDate: "2026:04:01 08:00:00");

        Assert.AreEqual(new DateOnly(2026, 3, 5), ExifDate.TryRead(jpeg));
    }

    [TestMethod]
    public void Falls_back_to_date_time_when_original_is_missing()
    {
        var jpeg = JpegWithExif(littleEndian: true, originalDate: null, changedDate: "2026:04:01 08:00:00");

        Assert.AreEqual(new DateOnly(2026, 4, 1), ExifDate.TryRead(jpeg));
    }

    [TestMethod]
    public void Jpeg_without_exif_gives_null()
    {
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xDA, 0x00, 0x02, 0xFF, 0xD9];

        Assert.IsNull(ExifDate.TryRead(jpeg));
    }

    [TestMethod]
    public void Garbled_date_gives_null()
    {
        var jpeg = JpegWithExif(littleEndian: true, originalDate: "0000:00:00 00:00:00", changedDate: null);

        Assert.IsNull(ExifDate.TryRead(jpeg));
    }

    [TestMethod]
    public void Png_gives_null()
    {
        Assert.IsNull(ExifDate.TryRead(TestImages.Png(10, 10)));
    }

    [TestMethod]
    [DataRow(new byte[0])]
    [DataRow(new byte[] { 0xFF, 0xD8 })]
    [DataRow(new byte[] { 0xFF, 0xD8, 0xFF, 0xE1, 0x00, 0x50, (byte)'E', (byte)'x', (byte)'i', (byte)'f', 0, 0, (byte)'I', (byte)'I' })]
    public void Truncated_or_broken_data_gives_null(byte[] data)
    {
        // Sjekker at vi aldri leser forbi slutten og kaster en exception.
        Assert.IsNull(ExifDate.TryRead(data));
    }

    [TestMethod]
    public void Truncated_exif_gives_null_for_every_length()
    {
        var jpeg = JpegWithExif(littleEndian: true, originalDate: "2026:03:05 10:15:00", changedDate: null);

        // Kutt fila på hver eneste posisjon. Ingen av dem skal kaste.
        for (var length = 0; length < jpeg.Length - 2; length++)
        {
            _ = ExifDate.TryRead(jpeg[..length]);
        }
    }

    /// <summary>
    /// En minimal JPEG med ett APP1-segment: TIFF-hode, IFD0 (DateTime og peker til EXIF-IFD),
    /// EXIF-IFD (DateTimeOriginal), og så tekstene. Akkurat det et kamera skriver, uten resten.
    /// </summary>
    private static byte[] JpegWithExif(bool littleEndian, string? originalDate, string? changedDate)
    {
        var tiff = new List<byte>();
        void U16(int value)
        {
            var b = new byte[2];
            if (littleEndian) BinaryPrimitives.WriteUInt16LittleEndian(b, (ushort)value); else BinaryPrimitives.WriteUInt16BigEndian(b, (ushort)value);
            tiff.AddRange(b);
        }
        void U32(int value)
        {
            var b = new byte[4];
            if (littleEndian) BinaryPrimitives.WriteUInt32LittleEndian(b, (uint)value); else BinaryPrimitives.WriteUInt32BigEndian(b, (uint)value);
            tiff.AddRange(b);
        }
        void Entry(int tag, int type, int count, int value) { U16(tag); U16(type); U32(count); U32(value); }

        // Posisjoner i TIFF-strukturen. IFD0 har to oppføringer, EXIF-IFD én.
        const int ifd0 = 8;
        const int ifd0Size = 2 + (2 * 12) + 4;
        const int exifIfd = ifd0 + ifd0Size;
        const int exifIfdSize = 2 + 12 + 4;
        const int changedText = exifIfd + exifIfdSize;
        const int originalText = changedText + 20;

        tiff.AddRange(littleEndian ? "II"u8.ToArray() : "MM"u8.ToArray());
        U16(42);
        U32(ifd0);

        U16(2);
        Entry(0x0132, 2, 20, changedDate == null ? 0 : changedText); // DateTime
        Entry(0x8769, 4, 1, exifIfd);                                  // peker til EXIF-IFD
        U32(0);

        U16(1);
        Entry(originalDate == null ? 0x9999 : 0x9003, 2, 20, originalText); // DateTimeOriginal (eller en annen tag)
        U32(0);

        tiff.AddRange(Encoding.ASCII.GetBytes((changedDate ?? "").PadRight(19, '\0')));
        tiff.Add(0);
        tiff.AddRange(Encoding.ASCII.GetBytes((originalDate ?? "").PadRight(19, '\0')));
        tiff.Add(0);

        var app1 = new List<byte>();
        app1.AddRange("Exif\0\0"u8.ToArray());
        app1.AddRange(tiff);

        var jpeg = new List<byte> { 0xFF, 0xD8, 0xFF, 0xE1 };
        var length = app1.Count + 2;
        jpeg.Add((byte)(length >> 8));
        jpeg.Add((byte)length);
        jpeg.AddRange(app1);
        jpeg.AddRange([0xFF, 0xDA, 0x00, 0x02, 0xFF, 0xD9]);
        return [.. jpeg];
    }
}
