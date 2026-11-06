using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace LadyRanias.App.Components.CatImages.Impl;

/// <summary>
/// Leser datoen bildet ble tatt (EXIF «DateTimeOriginal») fra en JPEG, før metadataen fjernes.
/// Mobilkameraer lagrer den, så mamma slipper å skrive inn datoen selv.
/// </summary>
/// <remarks>
/// Vi trenger bare én verdi, så vi leser den selv i stedet for å ta inn et helt bibliotek.
/// Oppbygningen, kort fortalt:
///   JPEG = segmenter (FF xx + lengde). EXIF ligger i segmentet APP1 (FF E1) og begynner med «Exif\0\0».
///   Etter det kommer en TIFF-struktur: byte-rekkefølge («II» = little endian, «MM» = big endian),
///   og lister med felt (IFD). Hver oppføring er 12 bytes: tag, type, antall, verdi eller posisjon.
///   IFD0 peker til en egen EXIF-liste (tag 0x8769), som har DateTimeOriginal (tag 0x9003).
///   Datoen er tekst: «2026:03:05 10:15:00».
/// Alt som ser rart ut gir null. Da brukes opplastingsdagen i stedet.
/// </remarks>
internal static class ExifDate
{
    private const ushort ExifIfdPointer = 0x8769;
    private const ushort DateTimeOriginal = 0x9003;
    private const ushort DateTimeChanged = 0x0132; // når bildet sist ble endret, brukes hvis DateTimeOriginal mangler

    public static DateOnly? TryRead(byte[] data)
    {
        try
        {
            return Read(data);
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or IndexOutOfRangeException or FormatException)
        {
            return null;
        }
    }

    private static DateOnly? Read(byte[] data)
    {
        // JPEG begynner alltid med FF D8. PNG og WebP har sjelden EXIF, så dem hopper vi over.
        if (data.Length < 4 || data[0] != 0xFF || data[1] != 0xD8)
        {
            return null;
        }

        var position = 2;
        while (position + 4 <= data.Length && data[position] == 0xFF)
        {
            var marker = data[position + 1];
            var length = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(position + 2, 2)); // lengden teller seg selv

            // FF DA: selve bildedataene begynner. Metadata kommer alltid før.
            if (marker == 0xDA || length < 2)
            {
                return null;
            }

            var segmentStart = position + 4;
            var segmentLength = length - 2;

            if (marker == 0xE1 && segmentLength > 6 && data.AsSpan(segmentStart, 6).SequenceEqual("Exif\0\0"u8))
            {
                return ReadTiff(data.AsSpan(segmentStart + 6, segmentLength - 6));
            }

            position = segmentStart + segmentLength;
        }

        return null;
    }

    private static DateOnly? ReadTiff(ReadOnlySpan<byte> tiff)
    {
        var littleEndian = tiff[0] == (byte)'I' && tiff[1] == (byte)'I';
        var bigEndian = tiff[0] == (byte)'M' && tiff[1] == (byte)'M';
        if (!littleEndian && !bigEndian)
        {
            return null;
        }

        ushort U16(ReadOnlySpan<byte> s, int at) =>
            littleEndian ? BinaryPrimitives.ReadUInt16LittleEndian(s.Slice(at, 2)) : BinaryPrimitives.ReadUInt16BigEndian(s.Slice(at, 2));
        uint U32(ReadOnlySpan<byte> s, int at) =>
            littleEndian ? BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(at, 4)) : BinaryPrimitives.ReadUInt32BigEndian(s.Slice(at, 4));

        var ifd0 = (int)U32(tiff, 4);

        var exifIfd = FindValue(tiff, ifd0, ExifIfdPointer, U16, U32);
        if (exifIfd != null)
        {
            var original = FindValue(tiff, (int)exifIfd.Value, DateTimeOriginal, U16, U32);
            if (original != null)
            {
                return ParseDate(tiff, (int)original.Value);
            }
        }

        var changed = FindValue(tiff, ifd0, DateTimeChanged, U16, U32);
        return changed == null ? null : ParseDate(tiff, (int)changed.Value);
    }

    private delegate ushort ReadU16(ReadOnlySpan<byte> span, int at);
    private delegate uint ReadU32(ReadOnlySpan<byte> span, int at);

    /// <summary>Verdien (eller posisjonen) til en tag i en IFD, eller null hvis den ikke finnes.</summary>
    private static uint? FindValue(ReadOnlySpan<byte> tiff, int ifd, ushort tag, ReadU16 u16, ReadU32 u32)
    {
        var count = u16(tiff, ifd);
        for (var i = 0; i < count; i++)
        {
            var entry = ifd + 2 + (i * 12);
            if (u16(tiff, entry) == tag)
            {
                return u32(tiff, entry + 8);
            }
        }

        return null;
    }

    /// <summary>«2026:03:05 10:15:00» → 2026-03-05. Bare datoen, klokkeslettet trenger vi ikke.</summary>
    private static DateOnly? ParseDate(ReadOnlySpan<byte> tiff, int at)
    {
        var text = Encoding.ASCII.GetString(tiff.Slice(at, 10));

        return DateOnly.TryParseExact(text, "yyyy:MM:dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }
}
