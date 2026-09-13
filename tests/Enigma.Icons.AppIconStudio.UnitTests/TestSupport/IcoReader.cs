using System;
using System.Collections.Generic;
using System.IO;

namespace Enigma.Icons.AppIconStudio.UnitTests.TestSupport;

/// <summary>
/// Reads back an <c>.ico</c> produced by <c>IcoWriter</c>, so the tests assert against a parsed
/// structure rather than against magic byte offsets scattered through every assertion.
/// </summary>
/// <remarks>
/// Deliberately a second, independent implementation of the format: a test that reused the writer's
/// own field order would agree with it whatever that order was.
/// </remarks>
public static class IcoReader
{
    /// <summary>One parsed directory entry, with the payload it points at.</summary>
    /// <param name="WidthByte">The raw <c>bWidth</c> byte — 0 means 256.</param>
    /// <param name="HeightByte">The raw <c>bHeight</c> byte — 0 means 256.</param>
    /// <param name="ColorCount">The raw <c>bColorCount</c> byte.</param>
    /// <param name="Reserved">The raw <c>bReserved</c> byte.</param>
    /// <param name="Planes">The <c>wPlanes</c> field.</param>
    /// <param name="BitCount">The <c>wBitCount</c> field.</param>
    /// <param name="BytesInRes">The declared payload length.</param>
    /// <param name="ImageOffset">The declared payload offset.</param>
    /// <param name="Payload">The bytes the entry points at.</param>
    public sealed record Entry(
        byte WidthByte,
        byte HeightByte,
        byte ColorCount,
        byte Reserved,
        ushort Planes,
        ushort BitCount,
        uint BytesInRes,
        uint ImageOffset,
        byte[] Payload)
    {
        /// <summary>The frame's edge in pixels, resolving the 0-means-256 encoding.</summary>
        public int SizePx => WidthByte == 0 ? 256 : WidthByte;

        /// <summary>True when the payload is a PNG file rather than a BMP DIB.</summary>
        public bool IsPng
            => Payload.Length >= 8
                && Payload[0] == 0x89
                && Payload[1] == 0x50
                && Payload[2] == 0x4E
                && Payload[3] == 0x47;
    }

    /// <summary>The whole parsed directory.</summary>
    /// <param name="Reserved">The <c>reserved</c> field; must be 0.</param>
    /// <param name="Type">The <c>type</c> field; 1 for an icon.</param>
    /// <param name="Entries">The entries, in file order.</param>
    public sealed record Directory(ushort Reserved, ushort Type, IReadOnlyList<Entry> Entries);

    /// <summary>Parses an <c>.ico</c>.</summary>
    /// <param name="bytes">The complete file.</param>
    /// <returns>The parsed directory.</returns>
    public static Directory Parse(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var reader = new BinaryReader(stream);

        ushort reserved = reader.ReadUInt16();
        ushort type = reader.ReadUInt16();
        ushort count = reader.ReadUInt16();

        var entries = new List<Entry>(count);
        var headers = new (byte W, byte H, byte C, byte R, ushort P, ushort B, uint Len, uint Off)[count];

        for (int i = 0; i < count; i++)
        {
            headers[i] = (
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadUInt16(),
                reader.ReadUInt16(),
                reader.ReadUInt32(),
                reader.ReadUInt32());
        }

        for (int i = 0; i < count; i++)
        {
            (byte w, byte h, byte c, byte r, ushort p, ushort b, uint len, uint off) = headers[i];

            var payload = new byte[len];
            Array.Copy(bytes, (int)off, payload, 0, (int)len);

            entries.Add(new Entry(w, h, c, r, p, b, len, off, payload));
        }

        return new Directory(reserved, type, entries);
    }

    /// <summary>Reads the <c>BITMAPINFOHEADER</c> at the start of a DIB payload.</summary>
    /// <param name="payload">A <c>IcoFrameFormat.Dib</c> payload.</param>
    /// <returns>The header's size, width, height, plane count, bit depth, compression and image size.</returns>
    public static (uint HeaderSize, int Width, int Height, ushort Planes, ushort BitCount, uint Compression, uint ImageSize)
        ParseDibHeader(byte[] payload)
    {
        using var stream = new MemoryStream(payload, writable: false);
        using var reader = new BinaryReader(stream);

        return (
            reader.ReadUInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadUInt16(),
            reader.ReadUInt16(),
            reader.ReadUInt32(),
            reader.ReadUInt32());
    }
}
