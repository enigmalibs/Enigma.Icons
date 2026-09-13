using System;
using Avalonia;
using Enigma.Icons.AppIconStudio.Rendering;
using Xunit;

// IconViewBox lives in the enclosing Enigma.Icons namespace and needs no using directive here.

namespace Enigma.Icons.AppIconStudio.UnitTests;

/// <summary>
/// Layout maths only — every type involved is an Avalonia value type, so these are plain
/// <c>[Fact]</c>s with no headless application behind them.
/// </summary>
public sealed class IconLayoutTests
{
    private const double Tolerance = 1e-9;

    [Theory]
    [InlineData(256, 0.0, 0.0)]
    [InlineData(256, 0.22, 56.32)]
    [InlineData(256, 0.5, 128.0)]
    [InlineData(16, 0.22, 3.52)]
    public void PlateRect_ScalesTheCornerRadiusWithTheEdge(int sizePx, double ratio, double expectedRadius)
    {
        RoundedRect plate = IconLayout.PlateRect(sizePx, ratio);

        Assert.Equal(new Rect(0, 0, sizePx, sizePx), plate.Rect);
        Assert.Equal(expectedRadius, plate.RadiiTopLeft.X, Tolerance);
        Assert.Equal(expectedRadius, plate.RadiiBottomRight.Y, Tolerance);
    }

    [Fact]
    public void PlateRect_AtHalfTheEdgeIsACircle()
    {
        RoundedRect plate = IconLayout.PlateRect(256, 0.5);

        // Every radius at half the edge means the four quarter-arcs meet — a circle, not a square.
        Assert.Equal(128.0, plate.RadiiTopLeft.X, Tolerance);
        Assert.Equal(128.0, plate.RadiiTopRight.X, Tolerance);
        Assert.Equal(128.0, plate.RadiiBottomLeft.X, Tolerance);
        Assert.Equal(128.0, plate.RadiiBottomRight.X, Tolerance);
        Assert.True(plate.IsUniform);
    }

    [Fact]
    public void PlateRect_FillsTheCanvasEdgeToEdge()
    {
        RoundedRect plate = IconLayout.PlateRect(64, 0.22);

        Assert.Equal(0.0, plate.Rect.X, Tolerance);
        Assert.Equal(0.0, plate.Rect.Y, Tolerance);
        Assert.Equal(64.0, plate.Rect.Right, Tolerance);
        Assert.Equal(64.0, plate.Rect.Bottom, Tolerance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(IconLayout.MaxSizePx + 1)]
    public void PlateRect_RejectsAnOutOfRangeSize(int sizePx)
        => Assert.Throws<ArgumentOutOfRangeException>(() => IconLayout.PlateRect(sizePx, 0.22));

    [Theory]
    [InlineData(-0.01)]
    [InlineData(0.51)]
    [InlineData(double.NaN)]
    public void PlateRect_RejectsAnOutOfRangeRatio(double ratio)
        => Assert.Throws<ArgumentOutOfRangeException>(() => IconLayout.PlateRect(256, ratio));

    [Theory]
    [InlineData(256, 1.0, 1.0)]
    [InlineData(256, 0.6, 0.6)]
    [InlineData(512, 0.6, 1.2)]
    [InlineData(16, 0.5, 0.03125)]
    public void GlyphTransform_ScalesThePhosphorViewBoxByTheRequestedFraction(
        int sizePx,
        double glyphScale,
        double expectedScale)
    {
        Matrix transform = IconLayout.GlyphTransform(IconViewBox.Default, sizePx, glyphScale);

        Assert.Equal(expectedScale, transform.M11, Tolerance);
        Assert.Equal(expectedScale, transform.M22, Tolerance);
    }

    [Theory]
    [InlineData(256, 0.6)]
    [InlineData(1024, 0.2)]
    [InlineData(16, 1.0)]
    public void GlyphTransform_CentresTheGlyphOnThePlate(int sizePx, double glyphScale)
    {
        Matrix transform = IconLayout.GlyphTransform(IconViewBox.Default, sizePx, glyphScale);

        Point topLeft = new Point(0, 0).Transform(transform);
        Point bottomRight = new Point(IconViewBox.Default.Width, IconViewBox.Default.Height).Transform(transform);

        // Equal margins on both axes is what "centred" means, and the painted span is exactly the
        // requested fraction of the edge.
        Assert.Equal(sizePx - bottomRight.X, topLeft.X, Tolerance);
        Assert.Equal(sizePx - bottomRight.Y, topLeft.Y, Tolerance);
        Assert.Equal(sizePx * glyphScale, bottomRight.X - topLeft.X, Tolerance);
        Assert.Equal(sizePx * glyphScale, bottomRight.Y - topLeft.Y, Tolerance);
    }

    [Fact]
    public void GlyphTransform_HonoursAViewBoxOriginThatIsNotZero()
    {
        var offset = new IconViewBox(-50, 20, 100, 100);

        Matrix transform = IconLayout.GlyphTransform(offset, 200, 1.0);

        Point topLeft = new Point(offset.X, offset.Y).Transform(transform);

        // The view box's own origin is subtracted, so its top-left lands on the canvas origin when
        // the glyph spans the whole edge.
        Assert.Equal(0.0, topLeft.X, Tolerance);
        Assert.Equal(0.0, topLeft.Y, Tolerance);
    }

    [Fact]
    public void GlyphTransform_UsesTheSmallerAxisForANonSquareViewBox()
    {
        var wide = new IconViewBox(0, 0, 200, 100);

        Matrix transform = IconLayout.GlyphTransform(wide, 100, 1.0);

        // Uniform scale from the tighter axis — 100/200 — so the wide box still fits.
        Assert.Equal(0.5, transform.M11, Tolerance);
        Assert.Equal(0.5, transform.M22, Tolerance);

        Point topLeft = new Point(0, 0).Transform(transform);
        Point bottomRight = new Point(wide.Width, wide.Height).Transform(transform);

        Assert.Equal(0.0, topLeft.X, Tolerance);
        Assert.Equal(100.0, bottomRight.X, Tolerance);

        // Vertically centred: 50 units tall in a 100 unit canvas leaves 25 above and below.
        Assert.Equal(25.0, topLeft.Y, Tolerance);
        Assert.Equal(75.0, bottomRight.Y, Tolerance);
    }

    [Theory]
    [InlineData(0.19)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    public void GlyphTransform_RejectsAnOutOfRangeScale(double glyphScale)
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => IconLayout.GlyphTransform(IconViewBox.Default, 256, glyphScale));

    [Theory]
    [InlineData(0)]
    [InlineData(IconLayout.MaxSizePx + 1)]
    public void GlyphTransform_RejectsAnOutOfRangeSize(int sizePx)
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => IconLayout.GlyphTransform(IconViewBox.Default, sizePx, 0.6));

    [Theory]
    // angle, startX, startY, endX, endY — clockwise from left-to-right, y down.
    [InlineData(0.0, 0.0, 0.5, 1.0, 0.5)]
    [InlineData(45.0, 0.0, 0.0, 1.0, 1.0)]
    [InlineData(90.0, 0.5, 0.0, 0.5, 1.0)]
    [InlineData(135.0, 1.0, 0.0, 0.0, 1.0)]
    [InlineData(180.0, 1.0, 0.5, 0.0, 0.5)]
    [InlineData(225.0, 1.0, 1.0, 0.0, 0.0)]
    [InlineData(270.0, 0.5, 1.0, 0.5, 0.0)]
    [InlineData(315.0, 0.0, 1.0, 1.0, 0.0)]
    [InlineData(360.0, 0.0, 0.5, 1.0, 0.5)]
    public void GradientLine_RunsThroughTheExpectedEndpoints(
        double angleDegrees,
        double startX,
        double startY,
        double endX,
        double endY)
    {
        (RelativePoint start, RelativePoint end) = IconLayout.GradientLine(angleDegrees);

        Assert.Equal(RelativeUnit.Relative, start.Unit);
        Assert.Equal(RelativeUnit.Relative, end.Unit);

        // 1e-12 rather than exact: cos(90°) is 6.1e-17, not 0.
        Assert.Equal(startX, start.Point.X, 12);
        Assert.Equal(startY, start.Point.Y, 12);
        Assert.Equal(endX, end.Point.X, 12);
        Assert.Equal(endY, end.Point.Y, 12);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(37.0)]
    [InlineData(123.4)]
    [InlineData(270.0)]
    public void GradientLine_IsAlwaysCentredOnThePlate(double angleDegrees)
    {
        (RelativePoint start, RelativePoint end) = IconLayout.GradientLine(angleDegrees);

        Assert.Equal(1.0, start.Point.X + end.Point.X, 12);
        Assert.Equal(1.0, start.Point.Y + end.Point.Y, 12);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void GradientLine_RejectsANonFiniteAngle(double angleDegrees)
        => Assert.Throws<ArgumentOutOfRangeException>(() => IconLayout.GradientLine(angleDegrees));
}
