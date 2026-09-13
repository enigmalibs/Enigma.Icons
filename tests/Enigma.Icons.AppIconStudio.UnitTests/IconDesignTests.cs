using System;
using Avalonia.Media;
using Enigma.Icons.AppIconStudio.Design;
using Enigma.Icons.Phosphor;
using Xunit;

namespace Enigma.Icons.AppIconStudio.UnitTests;

public sealed class IconDesignTests
{
    private static readonly PlateFill Blue = PlateFill.Solid(Color.FromRgb(0x3B, 0x72, 0xF0));

    [Fact]
    public void Defaults_MatchTheReferencePlate()
    {
        var design = new IconDesign(PhosphorIcon.Acorn, PhosphorWeight.Regular, Colors.White, Blue);

        Assert.Equal(IconDesign.DefaultCornerRadiusRatio, design.CornerRadiusRatio);
        Assert.Equal(IconDesign.DefaultGlyphScale, design.GlyphScale);
        Assert.Equal(PhosphorIcon.Acorn, design.Icon);
        Assert.Equal(PhosphorWeight.Regular, design.Weight);
        Assert.Equal(Colors.White, design.GlyphColor);
        Assert.Same(Blue, design.Plate);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.22)]
    [InlineData(0.5)]
    public void CornerRadiusRatio_AcceptsItsWholeRange(double ratio)
    {
        var design = new IconDesign(
            PhosphorIcon.Acorn,
            PhosphorWeight.Regular,
            Colors.White,
            Blue,
            cornerRadiusRatio: ratio);

        Assert.Equal(ratio, design.CornerRadiusRatio);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(0.51)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void CornerRadiusRatio_RejectsOutOfRangeValues(double ratio)
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => new IconDesign(
                PhosphorIcon.Acorn,
                PhosphorWeight.Regular,
                Colors.White,
                Blue,
                cornerRadiusRatio: ratio));

    [Theory]
    [InlineData(0.2)]
    [InlineData(0.6)]
    [InlineData(1.0)]
    public void GlyphScale_AcceptsItsWholeRange(double scale)
    {
        var design = new IconDesign(
            PhosphorIcon.Acorn,
            PhosphorWeight.Regular,
            Colors.White,
            Blue,
            glyphScale: scale);

        Assert.Equal(scale, design.GlyphScale);
    }

    [Theory]
    [InlineData(0.19)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public void GlyphScale_RejectsOutOfRangeValues(double scale)
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => new IconDesign(
                PhosphorIcon.Acorn,
                PhosphorWeight.Regular,
                Colors.White,
                Blue,
                glyphScale: scale));

    [Fact]
    public void Plate_IsRequired()
        // null! is the point of the test: it forces the null a nullable-aware caller cannot pass, so
        // the runtime guard is exercised rather than only the compiler's (SPEC §2.3).
        => Assert.Throws<ArgumentNullException>(
            () => new IconDesign(PhosphorIcon.Acorn, PhosphorWeight.Regular, Colors.White, null!));
}
