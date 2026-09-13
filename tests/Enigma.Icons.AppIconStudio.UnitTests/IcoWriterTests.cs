using System;
using System.Collections.Generic;
using Enigma.Icons.AppIconStudio.Export;
using Enigma.Icons.AppIconStudio.UnitTests.TestSupport;
using Xunit;

namespace Enigma.Icons.AppIconStudio.UnitTests;

/// <summary>
/// The container format, to the byte. Pure assembly, so no headless application is involved.
/// </summary>
public sealed class IcoWriterTests
{
    private static readonly byte[] MinimalPng =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x01, 0x02, 0x03,
    ];

    [Theory]
    [InlineData(16, false)]
    [InlineData(128, false)]
    [InlineData(255, false)]
    [InlineData(256, true)]
    public void UsesPngFrame_SwitchesAtTwoFiftySix(int sizePx, bool expected)
        => Assert.Equal(expected, IcoWriter.UsesPngFrame(sizePx));

    [Fact]
    public void Write_EmitsAWellFormedIconDirectory()
    {
        byte[] ico = IcoWriter.Write([Dib(16), Dib(32), Png(256)]);

        IcoReader.Directory directory = IcoReader.Parse(ico);

        Assert.Equal(0, directory.Reserved);
        Assert.Equal(1, directory.Type);
        Assert.Equal(3, directory.Entries.Count);
    }

    [Fact]
    public void Write_OrdersFramesAscendingWhateverTheInputOrder()
    {
        byte[] ico = IcoWriter.Write([Png(256), Dib(16), Dib(64), Dib(32)]);

        IcoReader.Directory directory = IcoReader.Parse(ico);

        Assert.Equal([16, 32, 64, 256], Sizes(directory));
    }

    [Fact]
    public void Write_StoresTwoFiftySixAsAZeroEdgeByte()
    {
        byte[] ico = IcoWriter.Write([Png(256)]);

        IcoReader.Entry entry = IcoReader.Parse(ico).Entries[0];

        Assert.Equal(0, entry.WidthByte);
        Assert.Equal(0, entry.HeightByte);
        Assert.Equal(256, entry.SizePx);
    }

    [Fact]
    public void Write_FillsTheFixedEntryFields()
    {
        byte[] ico = IcoWriter.Write([Dib(48)]);

        IcoReader.Entry entry = IcoReader.Parse(ico).Entries[0];

        Assert.Equal(48, entry.WidthByte);
        Assert.Equal(48, entry.HeightByte);
        Assert.Equal(0, entry.ColorCount);
        Assert.Equal(0, entry.Reserved);
        Assert.Equal(1, entry.Planes);
        Assert.Equal(32, entry.BitCount);
    }

    [Fact]
    public void Write_PointsEveryEntryAtItsOwnPayload()
    {
        byte[] ico = IcoWriter.Write([Dib(16), Dib(32), Png(256)]);

        IcoReader.Directory directory = IcoReader.Parse(ico);

        // The first payload starts right after the directory, and each one abuts the last.
        uint expectedOffset = 6u + (16u * (uint)directory.Entries.Count);
        uint total = expectedOffset;

        foreach (IcoReader.Entry entry in directory.Entries)
        {
            Assert.Equal(expectedOffset, entry.ImageOffset);
            Assert.Equal((uint)entry.Payload.Length, entry.BytesInRes);

            expectedOffset += entry.BytesInRes;
            total += entry.BytesInRes;
        }

        // Nothing before the directory, nothing trailing after the last payload.
        Assert.Equal(total, (uint)ico.Length);
    }

    [Fact]
    public void Write_StoresAPngPayloadVerbatim()
    {
        byte[] ico = IcoWriter.Write([Png(256)]);

        IcoReader.Entry entry = IcoReader.Parse(ico).Entries[0];

        Assert.True(entry.IsPng);
        Assert.Equal(MinimalPng, entry.Payload);
    }

    [Fact]
    public void Write_WrapsPixelsInABitmapInfoHeader()
    {
        byte[] ico = IcoWriter.Write([Dib(32)]);

        IcoReader.Entry entry = IcoReader.Parse(ico).Entries[0];
        (uint headerSize, int width, int height, ushort planes, ushort bitCount, uint compression, uint imageSize) =
            IcoReader.ParseDibHeader(entry.Payload);

        Assert.False(entry.IsPng);
        Assert.Equal(40u, headerSize);
        Assert.Equal(32, width);

        // Twice the edge: the DIB nominally stacks the colour image and the AND mask.
        Assert.Equal(64, height);
        Assert.Equal(1, planes);
        Assert.Equal(32, bitCount);

        // BI_RGB — never compressed.
        Assert.Equal(0u, compression);

        int colourBytes = 32 * 32 * 4;
        int maskBytes = IcoWriter.AndMaskStride(32) * 32;

        Assert.Equal((uint)(colourBytes + maskBytes), imageSize);
        Assert.Equal(40 + colourBytes + maskBytes, entry.Payload.Length);
    }

    [Theory]
    // edge, expected padded AND-mask row length in bytes
    [InlineData(1, 4)]
    [InlineData(16, 4)]
    [InlineData(32, 4)]
    [InlineData(33, 8)]
    [InlineData(48, 8)]
    [InlineData(64, 8)]
    [InlineData(256, 32)]
    public void AndMaskStride_PadsEachRowToFourBytes(int sizePx, int expected)
        => Assert.Equal(expected, IcoWriter.AndMaskStride(sizePx));

    [Fact]
    public void Write_LeavesTheAndMaskZeroed()
    {
        byte[] ico = IcoWriter.Write([Dib(16)]);

        byte[] payload = IcoReader.Parse(ico).Entries[0].Payload;
        int maskStart = 40 + (16 * 16 * 4);

        for (int i = maskStart; i < payload.Length; i++)
        {
            // A 1-bit mask that disagreed with the 32-bpp alpha channel would fringe the rounded
            // corners; zero means "the alpha channel decides".
            Assert.Equal(0, payload[i]);
        }
    }

    [Fact]
    public void Write_FlipsColourRowsBottomUp()
    {
        // A 2x2 frame whose four pixels are distinguishable by their blue channel:
        // top row 0x10 0x11, bottom row 0x20 0x21.
        byte[] pixels = new byte[2 * 2 * 4];
        pixels[0] = 0x10;
        pixels[4] = 0x11;
        pixels[8] = 0x20;
        pixels[12] = 0x21;

        byte[] ico = IcoWriter.Write([IcoFrame.FromPixels(2, pixels)]);

        byte[] payload = IcoReader.Parse(ico).Entries[0].Payload;

        // BMP is bottom-up, so the source's LAST row is written first.
        Assert.Equal(0x20, payload[40]);
        Assert.Equal(0x21, payload[44]);
        Assert.Equal(0x10, payload[48]);
        Assert.Equal(0x11, payload[52]);
    }

    [Fact]
    public void Write_RejectsANullFrameList()
        // null! is the point of the test: it forces the null a nullable-aware caller cannot pass, so
        // the runtime guard is exercised rather than only the compiler's (SPEC §2.3).
        => Assert.Throws<ArgumentNullException>(() => IcoWriter.Write(null!));

    [Fact]
    public void Write_RejectsAnEmptyFrameList()
        => Assert.Throws<ArgumentException>(() => IcoWriter.Write([]));

    [Fact]
    public void Write_RejectsANullFrame()
    {
        var frames = new List<IcoFrame> { Dib(16), null! };

        Assert.Throws<ArgumentNullException>(() => IcoWriter.Write(frames));
    }

    [Fact]
    public void Write_RejectsTwoFramesOfTheSameSize()
        => Assert.Throws<ArgumentException>(() => IcoWriter.Write([Dib(32), Dib(32)]));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(257)]
    public void IcoFrame_RejectsAnOutOfRangeSize(int sizePx)
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => IcoFrame.FromPixels(sizePx, new byte[Math.Max(0, sizePx * sizePx * 4)]));

    [Fact]
    public void IcoFrame_RejectsPixelsOfTheWrongLength()
    {
        Assert.Throws<ArgumentException>(() => IcoFrame.FromPixels(16, new byte[16 * 16 * 4 - 1]));
        Assert.Throws<ArgumentException>(() => IcoFrame.FromPixels(16, new byte[16 * 16 * 4 + 1]));
    }

    [Fact]
    public void IcoFrame_RejectsAPayloadThatIsNotAPng()
    {
        Assert.Throws<ArgumentException>(() => IcoFrame.FromPng(256, [0x00, 0x01, 0x02]));
        Assert.Throws<ArgumentException>(() => IcoFrame.FromPng(256, new byte[64]));
    }

    [Fact]
    public void IcoFrame_RejectsAnUndeclaredFormat()
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => new IcoFrame(16, (IcoFrameFormat)42, new byte[16 * 16 * 4]));

    [Fact]
    public void IcoFrame_RejectsANullPayload()
        // See the null! note above.
        => Assert.Throws<ArgumentNullException>(() => IcoFrame.FromPixels(16, null!));

    private static IcoFrame Dib(int sizePx) => IcoFrame.FromPixels(sizePx, new byte[sizePx * sizePx * 4]);

    private static IcoFrame Png(int sizePx) => IcoFrame.FromPng(sizePx, MinimalPng);

    private static int[] Sizes(IcoReader.Directory directory)
    {
        var sizes = new int[directory.Entries.Count];
        for (int i = 0; i < sizes.Length; i++)
        {
            sizes[i] = directory.Entries[i].SizePx;
        }

        return sizes;
    }
}
