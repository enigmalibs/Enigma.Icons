using System;
using System.IO;
using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Enigma.Icons.AppIconStudio.Design;
using Enigma.Icons.AppIconStudio.Rendering;
using Enigma.Icons.Phosphor;
using Xunit;

namespace Enigma.Icons.AppIconStudio.UnitTests;

/// <summary>
/// The rasterizer against a real Skia backend. <c>TestAppBuilder</c> turns the headless platform's
/// stub drawing off precisely so these produce inspectable pixels.
/// </summary>
public sealed class AvaloniaIconRasterizerTests
{
    private const int Size = 128;

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static readonly Color Plate = Color.FromRgb(0x3B, 0x72, 0xF0);
    private static readonly Color Glyph = Color.FromRgb(0xFF, 0xEE, 0x11);

    [AvaloniaFact]
    public void RenderPng_ProducesADecodablePngOfTheRequestedSize()
    {
        byte[] png = new AvaloniaIconRasterizer().RenderPng(SolidDesign(), Size);

        Assert.True(png.Length > PngSignature.Length);
        Assert.Equal(PngSignature, png[..PngSignature.Length]);

        using var stream = new MemoryStream(png);
        using var decoded = new Bitmap(stream);

        Assert.Equal(new PixelSize(Size, Size), decoded.PixelSize);
    }

    [AvaloniaFact]
    public void RenderPng_IsSmallerThanTheRawPixels()
    {
        // Not a compression benchmark — a guard that the PNG encoder actually ran rather than the
        // call handing back something raw.
        byte[] png = new AvaloniaIconRasterizer().RenderPng(SolidDesign(), Size);

        Assert.True(png.Length < Size * Size * AvaloniaIconRasterizer.BytesPerPixel);
    }

    [AvaloniaFact]
    public void RenderBgra_HasFourBytesPerPixelAndNoStridePadding()
    {
        byte[] pixels = new AvaloniaIconRasterizer().RenderBgra(SolidDesign(), Size);

        Assert.Equal(Size * Size * AvaloniaIconRasterizer.BytesPerPixel, pixels.Length);
    }

    [AvaloniaFact]
    public void RenderBgra_LeavesTheRoundedCornerTransparent()
    {
        byte[] pixels = new AvaloniaIconRasterizer().RenderBgra(SolidDesign(), Size);

        // 0,0 sits well outside a 22 %-radius corner arc.
        Assert.Equal(0, Alpha(pixels, 0, 0));
        Assert.Equal(0, Alpha(pixels, Size - 1, 0));
        Assert.Equal(0, Alpha(pixels, 0, Size - 1));
        Assert.Equal(0, Alpha(pixels, Size - 1, Size - 1));
    }

    [AvaloniaFact]
    public void RenderBgra_FillsTheCornerWhenTheRadiusIsZero()
    {
        IconDesign square = new IconDesign(
            PhosphorIcon.Square,
            PhosphorWeight.Fill,
            Glyph,
            PlateFill.Solid(Plate),
            cornerRadiusRatio: 0.0);

        byte[] pixels = new AvaloniaIconRasterizer().RenderBgra(square, Size);

        // The same pixel that was transparent above — proof the corner radius is what clears it.
        Assert.Equal(255, Alpha(pixels, 0, 0));
        AssertColor(Plate, pixels, 0, 0);
    }

    [AvaloniaFact]
    public void RenderBgra_PaintsASolidPlateInItsPrimaryColour()
    {
        byte[] pixels = new AvaloniaIconRasterizer().RenderBgra(SolidDesign(), Size);

        // Two pixels inside the plate but outside the centred 60 % the glyph occupies.
        AssertColor(Plate, pixels, Size / 2, 2);
        AssertColor(Plate, pixels, 2, Size / 2);
    }

    [AvaloniaFact]
    public void RenderBgra_PaintsTheGlyphInItsOwnColour()
    {
        // A filled square glyph covers the centre outright, so the middle pixel is the glyph's and
        // nothing else — no anti-aliased edge to reason about.
        byte[] pixels = new AvaloniaIconRasterizer().RenderBgra(SolidDesign(), Size);

        AssertColor(Glyph, pixels, Size / 2, Size / 2);
    }

    [AvaloniaFact]
    public void RenderBgra_HonoursTheGlyphColourChoice()
    {
        var rasterizer = new AvaloniaIconRasterizer();

        byte[] white = rasterizer.RenderBgra(WithGlyphColour(Colors.White), Size);
        byte[] black = rasterizer.RenderBgra(WithGlyphColour(Colors.Black), Size);

        AssertColor(Colors.White, white, Size / 2, Size / 2);
        AssertColor(Colors.Black, black, Size / 2, Size / 2);
    }

    [AvaloniaFact]
    public void RenderBgra_RunsAGradientAlongItsAngle()
    {
        var topToBottom = new IconDesign(
            PhosphorIcon.Square,
            PhosphorWeight.Fill,
            Glyph,
            PlateFill.LinearGradient(Colors.Black, Colors.White, 90.0));

        byte[] pixels = new AvaloniaIconRasterizer().RenderBgra(topToBottom, Size);

        byte top = Red(pixels, Size / 2, 2);
        byte bottom = Red(pixels, Size / 2, Size - 3);

        // 90° is top to bottom: black at the top, white at the bottom, and a wide gap between.
        Assert.True(bottom > top + 200, $"expected a strong top-to-bottom ramp, got {top} -> {bottom}");
    }

    [AvaloniaFact]
    public void RenderBgra_LeavesAGradientFlatAcrossItsAngle()
    {
        var topToBottom = new IconDesign(
            PhosphorIcon.Square,
            PhosphorWeight.Fill,
            Glyph,
            PlateFill.LinearGradient(Colors.Black, Colors.White, 90.0));

        byte[] pixels = new AvaloniaIconRasterizer().RenderBgra(topToBottom, Size);

        // Perpendicular to a 90° gradient nothing should change. Sampled at mid-height, where the
        // plate spans the full width and the rounded corners are out of the way, and outside the
        // centred 60 % the glyph occupies.
        Assert.Equal(Red(pixels, 2, Size / 2), Red(pixels, Size - 3, Size / 2));
    }

    [AvaloniaFact]
    public void RenderBgra_ReturnsStraightNotPremultipliedAlpha()
    {
        // A 50 %-alpha plate: premultiplied storage would scale the colour channels down by half,
        // so seeing the full-strength colour beside alpha 128 is the proof of the conversion.
        var translucent = new IconDesign(
            PhosphorIcon.Square,
            PhosphorWeight.Fill,
            Glyph,
            PlateFill.Solid(Color.FromArgb(0x80, 0xFF, 0x00, 0x00)),
            cornerRadiusRatio: 0.0);

        byte[] pixels = new AvaloniaIconRasterizer().RenderBgra(translucent, Size);

        Assert.Equal(0x80, Alpha(pixels, 2, 2));

        // Skia's premultiply/unpremultiply round trip is lossy by a unit or two; 0xFF must not come
        // back near 0x80.
        Assert.True(Red(pixels, 2, 2) > 0xF0, $"expected a near-full red channel, got {Red(pixels, 2, 2)}");
    }

    [AvaloniaFact]
    public void Render_WorksAtEveryIcoFrameSize()
    {
        var rasterizer = new AvaloniaIconRasterizer();
        IconDesign design = SolidDesign();

        foreach (int size in new[] { 16, 24, 32, 48, 64, 128, 256 })
        {
            byte[] pixels = rasterizer.RenderBgra(design, size);
            Assert.Equal(size * size * AvaloniaIconRasterizer.BytesPerPixel, pixels.Length);

            using var stream = new MemoryStream(rasterizer.RenderPng(design, size));
            using var decoded = new Bitmap(stream);
            Assert.Equal(new PixelSize(size, size), decoded.PixelSize);
        }
    }

    [AvaloniaTheory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(IconLayout.MaxSizePx + 1)]
    public void Render_RejectsAnOutOfRangeSize(int sizePx)
    {
        var rasterizer = new AvaloniaIconRasterizer();

        Assert.Throws<ArgumentOutOfRangeException>(() => rasterizer.RenderPng(SolidDesign(), sizePx));
        Assert.Throws<ArgumentOutOfRangeException>(() => rasterizer.RenderBgra(SolidDesign(), sizePx));
    }

    [AvaloniaFact]
    public void Render_RejectsANullDesign()
    {
        var rasterizer = new AvaloniaIconRasterizer();

        // null! is the point of the test: it forces the null a nullable-aware caller cannot pass, so
        // the runtime guard is exercised rather than only the compiler's (SPEC §2.3).
        Assert.Throws<ArgumentNullException>(() => rasterizer.RenderPng(null!, Size));
        Assert.Throws<ArgumentNullException>(() => rasterizer.RenderBgra(null!, Size));
    }

    private static IconDesign SolidDesign()
        => new IconDesign(PhosphorIcon.Square, PhosphorWeight.Fill, Glyph, PlateFill.Solid(Plate));

    private static IconDesign WithGlyphColour(Color glyphColor)
        => new IconDesign(PhosphorIcon.Square, PhosphorWeight.Fill, glyphColor, PlateFill.Solid(Plate));

    private static int Offset(int x, int y) => ((y * Size) + x) * AvaloniaIconRasterizer.BytesPerPixel;

    private static byte Alpha(byte[] bgra, int x, int y) => bgra[Offset(x, y) + 3];

    private static byte Red(byte[] bgra, int x, int y) => bgra[Offset(x, y) + 2];

    private static void AssertColor(Color expected, byte[] bgra, int x, int y)
    {
        int offset = Offset(x, y);

        Assert.Equal(expected.B, bgra[offset]);
        Assert.Equal(expected.G, bgra[offset + 1]);
        Assert.Equal(expected.R, bgra[offset + 2]);
        Assert.Equal(expected.A, bgra[offset + 3]);
    }
}
