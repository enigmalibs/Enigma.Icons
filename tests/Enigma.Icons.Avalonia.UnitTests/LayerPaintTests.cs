using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Enigma.Icons.Avalonia.UnitTests.TestSupport;
using Xunit;

namespace Enigma.Icons.Avalonia.UnitTests;

/// <summary>
/// The <c>internal</c> per-layer paint decision that <c>ToDrawing</c> and <c>Icon.Render</c> share,
/// asserted directly — it is the single point where the two could drift apart.
/// </summary>
/// <remarks>Reached through the project's <c>InternalsVisibleTo</c>.</remarks>
public sealed class LayerPaintTests
{
    [AvaloniaFact]
    public void UnfilledUnstrokedLayer_IsSkipped()
    {
        var layer = new IconLayer(TestGlyphs.SquarePath, fill: "none");

        bool painted = IconGlyphExtensions.TryGetLayerPaint(layer, Brushes.Black, out IBrush? fill, out IPen? pen);

        Assert.False(painted);
        Assert.Null(fill);
        Assert.Null(pen);
    }

    [AvaloniaFact]
    public void FilledLayer_TakesTheSuppliedBrushAndNoPen()
    {
        var layer = new IconLayer(TestGlyphs.SquarePath);

        bool painted = IconGlyphExtensions.TryGetLayerPaint(layer, Brushes.Black, out IBrush? fill, out IPen? pen);

        Assert.True(painted);
        Assert.Same(Brushes.Black, fill);
        Assert.Null(pen);
    }

    [AvaloniaFact]
    public void StrokedOnlyLayer_TakesAPenAndNoFill()
    {
        var layer = new IconLayer(TestGlyphs.SquarePath, fill: "none", stroke: "#00FF00", strokeWidth: 2.0);

        bool painted = IconGlyphExtensions.TryGetLayerPaint(layer, Brushes.Black, out IBrush? fill, out IPen? pen);

        Assert.True(painted);
        Assert.Null(fill);
        Assert.Equal(2.0, Assert.IsType<Pen>(pen).Thickness);
    }

    [AvaloniaFact]
    public void UnspecifiedStrokeWidthCapAndJoin_TakeTheSvgDefaults()
    {
        var layer = new IconLayer(TestGlyphs.SquarePath, stroke: "#000000");

        IconGlyphExtensions.TryGetLayerPaint(layer, Brushes.Black, out _, out IPen? pen);

        var stroke = Assert.IsType<Pen>(pen);
        Assert.Equal(1.0, stroke.Thickness);                  // SVG's default stroke-width
        Assert.Equal(PenLineCap.Flat, stroke.LineCap);        // SVG's butt cap
        Assert.Equal(PenLineJoin.Miter, stroke.LineJoin);     // SVG's default join
    }
}
