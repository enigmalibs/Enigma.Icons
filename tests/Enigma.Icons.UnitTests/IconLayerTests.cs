using System;
using Xunit;

namespace Enigma.Icons.UnitTests;

public sealed class IconLayerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsBlankPathData(string? pathData)
        => Assert.Throws<ArgumentException>(() => new IconLayer(pathData!));

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void Constructor_RejectsOpacityOutsideTheUnitRange(double opacity)
        => Assert.Throws<ArgumentOutOfRangeException>(() => new IconLayer("M 0,0 L 1,1", opacity));

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.2)]
    [InlineData(1.0)]
    public void Constructor_AcceptsOpacityInsideTheUnitRange(double opacity)
        => Assert.Equal(opacity, new IconLayer("M 0,0 L 1,1", opacity).Opacity);

    [Fact]
    public void Constructor_KeepsEveryValue()
    {
        var layer = new IconLayer(
            "M 0,0 L 1,1",
            0.5,
            IconFillRule.EvenOdd,
            "#ff0000",
            "#00ff00",
            2.5,
            IconLineCap.Round,
            IconLineJoin.Bevel);

        Assert.Equal("M 0,0 L 1,1", layer.PathData);
        Assert.Equal(0.5, layer.Opacity);
        Assert.Equal(IconFillRule.EvenOdd, layer.FillRule);
        Assert.Equal("#ff0000", layer.Fill);
        Assert.Equal("#00ff00", layer.Stroke);
        Assert.Equal(2.5, layer.StrokeWidth);
        Assert.Equal(IconLineCap.Round, layer.StrokeLineCap);
        Assert.Equal(IconLineJoin.Bevel, layer.StrokeLineJoin);
    }

    [Theory]
    // fill, expected IsFilled — null means "inherit the renderer's brush", so it IS filled.
    [InlineData(null, true)]
    [InlineData("none", false)]
    [InlineData("#123456", true)]
    [InlineData("NONE", true)]
    public void IsFilled_IsFalseOnlyForTheLiteralNone(string? fill, bool expected)
        => Assert.Equal(expected, new IconLayer("M 0,0 L 1,1", fill: fill).IsFilled);

    [Theory]
    [InlineData(null, false)]
    [InlineData("none", false)]
    [InlineData("#123456", true)]
    public void IsStroked_RequiresANonNoneStroke(string? stroke, bool expected)
        => Assert.Equal(expected, new IconLayer("M 0,0 L 1,1", stroke: stroke).IsStroked);

    [Fact]
    public void Constructor_DoesNotNormalizeCurrentColor()
    {
        // Normalization is the parser's job (SPEC §4.2/§5.1), so a hand-built layer keeps the value
        // it was given.
        var layer = new IconLayer("M 0,0 L 1,1", fill: "currentColor");

        Assert.Equal("currentColor", layer.Fill);
    }
}
