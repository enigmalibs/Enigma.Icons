using Xunit;

namespace Enigma.Icons.UnitTests;

public sealed class SvgIconParserPaintTests
{
    private static IconLayer SingleLayer(string attributes)
        => Assert.Single(SvgIconParser.Parse("<svg><path " + attributes + " d=\"M 0,0\" /></svg>").Layers);

    [Fact]
    public void FillNone_IsKeptVerbatimAndIsNotFilled()
    {
        IconLayer layer = SingleLayer("fill=\"none\"");

        Assert.Equal("none", layer.Fill);
        Assert.False(layer.IsFilled);
    }

    [Fact]
    public void FillCurrentColor_NormalizesToNull()
    {
        IconLayer layer = SingleLayer("fill=\"currentColor\"");

        Assert.Null(layer.Fill);
        Assert.True(layer.IsFilled);
    }

    [Fact]
    public void FillCurrentColor_OverridesAnInheritedPaint()
    {
        IconGlyph glyph = SvgIconParser.Parse(
            "<svg><g fill=\"red\"><path fill=\"currentColor\" d=\"M 0,0\" /></g></svg>");

        Assert.Null(glyph.Layers[0].Fill);
    }

    [Fact]
    public void StrokeCurrentColor_NormalizesToNull()
    {
        IconLayer layer = SingleLayer("stroke=\"currentColor\"");

        Assert.Null(layer.Stroke);
        Assert.False(layer.IsStroked);
    }

    [Fact]
    public void FillRuleEvenOdd_IsCaptured()
        => Assert.Equal(IconFillRule.EvenOdd, SingleLayer("fill-rule=\"evenodd\"").FillRule);

    [Fact]
    public void FillRuleNonZero_IsTheDefault()
    {
        Assert.Equal(IconFillRule.NonZero, SingleLayer(string.Empty).FillRule);
        Assert.Equal(IconFillRule.NonZero, SingleLayer("fill-rule=\"nonzero\"").FillRule);
    }

    [Fact]
    public void UnrecognizedFillRule_FallsBackToTheCssInitialValue()
        => Assert.Equal(IconFillRule.NonZero, SingleLayer("fill-rule=\"sideways\"").FillRule);

    [Fact]
    public void StrokeQuartet_IsCapturedOnTheLayer()
    {
        IconLayer layer = SingleLayer(
            "stroke=\"#112233\" stroke-width=\"6.5\" stroke-linecap=\"square\" stroke-linejoin=\"bevel\"");

        Assert.Equal("#112233", layer.Stroke);
        Assert.Equal(6.5, layer.StrokeWidth);
        Assert.Equal(IconLineCap.Square, layer.StrokeLineCap);
        Assert.Equal(IconLineJoin.Bevel, layer.StrokeLineJoin);
        Assert.True(layer.IsStroked);
    }

    [Theory]
    [InlineData("butt", IconLineCap.Flat)]
    [InlineData("round", IconLineCap.Round)]
    [InlineData("square", IconLineCap.Square)]
    [InlineData("nonsense", IconLineCap.Flat)]
    public void StrokeLineCap_MapsTheSvgKeywords(string keyword, IconLineCap expected)
        => Assert.Equal(expected, SingleLayer("stroke-linecap=\"" + keyword + "\"").StrokeLineCap);

    [Theory]
    [InlineData("miter", IconLineJoin.Miter)]
    [InlineData("round", IconLineJoin.Round)]
    [InlineData("bevel", IconLineJoin.Bevel)]
    [InlineData("nonsense", IconLineJoin.Miter)]
    public void StrokeLineJoin_MapsTheSvgKeywords(string keyword, IconLineJoin expected)
        => Assert.Equal(expected, SingleLayer("stroke-linejoin=\"" + keyword + "\"").StrokeLineJoin);

    [Fact]
    public void UnspecifiedPaint_StaysNull()
    {
        IconLayer layer = SingleLayer(string.Empty);

        Assert.Null(layer.Fill);
        Assert.Null(layer.Stroke);
        Assert.Null(layer.StrokeWidth);
        Assert.Null(layer.StrokeLineCap);
        Assert.Null(layer.StrokeLineJoin);
        Assert.Equal(1.0, layer.Opacity);
    }

    [Fact]
    public void FillOpacityAndStrokeOpacity_AreIgnored()
    {
        // Documented exclusion: only `opacity` is folded into IconLayer.Opacity.
        IconLayer layer = SingleLayer("fill-opacity=\"0.3\" stroke-opacity=\"0.4\"");

        Assert.Equal(1.0, layer.Opacity);
    }

    [Fact]
    public void OpacityOutsideTheUnitRange_IsClamped()
    {
        Assert.Equal(1.0, SingleLayer("opacity=\"5\"").Opacity);
        Assert.Equal(0.0, SingleLayer("opacity=\"-2\"").Opacity);
    }

    [Fact]
    public void ATransparentSpacerRect_IsKeptAsANormalLayer()
    {
        // A full-view-box rect with fill="none" is a legitimate spacer; renderers skip non-filled,
        // non-stroked layers themselves.
        IconGlyph glyph = SvgIconParser.Parse(
            "<svg viewBox=\"0 0 256 256\"><rect width=\"256\" height=\"256\" fill=\"none\" /><path d=\"M 0,0\" /></svg>");

        Assert.Equal(2, glyph.Layers.Count);
        Assert.False(glyph.Layers[0].IsFilled);
        Assert.False(glyph.Layers[0].IsStroked);
    }
}
