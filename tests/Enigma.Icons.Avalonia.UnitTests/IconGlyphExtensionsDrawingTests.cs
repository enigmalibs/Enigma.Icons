using System;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Enigma.Icons.Avalonia.UnitTests.TestSupport;
using Xunit;

namespace Enigma.Icons.Avalonia.UnitTests;

/// <summary>SPEC §12.3, the <c>ToDrawing</c> and <c>ToDrawingImage</c> bullets.</summary>
public sealed class IconGlyphExtensionsDrawingTests
{
    private const int OpacityPrecision = 9;

    [AvaloniaFact]
    public void ToDrawing_SingleLayer_IsOneUnwrappedGeometryDrawing()
    {
        var root = Assert.IsType<DrawingGroup>(TestGlyphs.SingleFilled().ToDrawing(Brushes.Black));

        Drawing only = Assert.Single(root.Children);
        var drawing = Assert.IsType<GeometryDrawing>(only);
        Assert.Same(Brushes.Black, drawing.Brush);
        Assert.Null(drawing.Pen);
    }

    [AvaloniaFact]
    public void ToDrawing_Duotone_WrapsOnlyTheTranslucentLayer()
    {
        var root = Assert.IsType<DrawingGroup>(TestGlyphs.Duotone().ToDrawing(Brushes.Black));

        Assert.Equal(2, root.Children.Count);

        // Layer 0 is at 0.2, so it gets its own DrawingGroup — GeometryDrawing has no opacity.
        var wrapper = Assert.IsType<DrawingGroup>(root.Children[0]);
        Assert.Equal(TestGlyphs.DuotoneBackingOpacity, wrapper.Opacity, OpacityPrecision);
        Assert.IsType<GeometryDrawing>(Assert.Single(wrapper.Children));

        // Layer 1 is opaque, so it is added to the root directly — no wrapper.
        Assert.IsType<GeometryDrawing>(root.Children[1]);
    }

    [AvaloniaFact]
    public void ToDrawing_UnfilledUnstrokedLayer_IsSkippedEntirely()
    {
        var root = Assert.IsType<DrawingGroup>(TestGlyphs.WithSpacer().ToDrawing(Brushes.Black));

        // Nothing is emitted for the spacer — not an empty group, not a null-brushed drawing.
        Assert.IsType<GeometryDrawing>(Assert.Single(root.Children));
    }

    [AvaloniaFact]
    public void ToDrawing_StrokedLayer_BuildsAPenWithTheLayersWidthCapAndJoin()
    {
        var root = Assert.IsType<DrawingGroup>(TestGlyphs.Stroked().ToDrawing(Brushes.Black));

        var drawing = Assert.IsType<GeometryDrawing>(Assert.Single(root.Children));
        var pen = Assert.IsType<Pen>(drawing.Pen);

        Assert.Equal(4.0, pen.Thickness);
        Assert.Equal(PenLineCap.Round, pen.LineCap);
        Assert.Equal(PenLineJoin.Bevel, pen.LineJoin);

        // Filled as well as stroked: the fill takes the supplied brush.
        Assert.Same(Brushes.Black, drawing.Brush);
    }

    [AvaloniaFact]
    public void ToDrawing_StrokedLayer_HonoursItsOwnStrokePaint()
    {
        var root = Assert.IsType<DrawingGroup>(TestGlyphs.Stroked().ToDrawing(Brushes.Black));

        var pen = Assert.IsType<Pen>(Assert.IsType<GeometryDrawing>(root.Children[0]).Pen);
        var stroke = Assert.IsAssignableFrom<ISolidColorBrush>(pen.Brush);

        Assert.Equal(Colors.Red, stroke.Color);
    }

    [AvaloniaFact]
    public void ToDrawing_UnparseableStrokePaint_FallsBackToTheSuppliedBrush()
    {
        var root = Assert.IsType<DrawingGroup>(TestGlyphs.StrokedWithGarbagePaint().ToDrawing(Brushes.Black));

        var pen = Assert.IsType<Pen>(Assert.IsType<GeometryDrawing>(root.Children[0]).Pen);

        Assert.Same(Brushes.Black, pen.Brush);
    }

    [AvaloniaFact]
    public void ToDrawing_StrokedButUnfilledLayer_IsOutlinedOnly()
    {
        var root = Assert.IsType<DrawingGroup>(TestGlyphs.StrokedOnly().ToDrawing(Brushes.Black));

        var drawing = Assert.IsType<GeometryDrawing>(Assert.Single(root.Children));

        Assert.Null(drawing.Brush);
        Assert.NotNull(drawing.Pen);
    }

    [AvaloniaFact]
    public void ToDrawingImage_WrapsToDrawing()
    {
        IconGlyph glyph = TestGlyphs.Duotone();

        DrawingImage image = glyph.ToDrawingImage(Brushes.Black);

        var wrapped = Assert.IsType<DrawingGroup>(image.Drawing);
        var reference = Assert.IsType<DrawingGroup>(glyph.ToDrawing(Brushes.Black));

        Assert.Equal(reference.Children.Count, wrapped.Children.Count);
        Assert.Equal(
            Assert.IsType<DrawingGroup>(reference.Children[0]).Opacity,
            Assert.IsType<DrawingGroup>(wrapped.Children[0]).Opacity,
            OpacityPrecision);
    }

    [AvaloniaFact]
    public void ToDrawing_NullGlyph_Throws()
        => Assert.Throws<ArgumentNullException>(() => IconGlyphExtensions.ToDrawing(null!, Brushes.Black));

    [AvaloniaFact]
    public void ToDrawing_NullBrush_Throws()
        => Assert.Throws<ArgumentNullException>(() => TestGlyphs.SingleFilled().ToDrawing(null!));

    [AvaloniaFact]
    public void ToDrawingImage_NullGlyph_Throws()
        => Assert.Throws<ArgumentNullException>(() => IconGlyphExtensions.ToDrawingImage(null!, Brushes.Black));

    [AvaloniaFact]
    public void ToDrawingImage_NullBrush_Throws()
        => Assert.Throws<ArgumentNullException>(() => TestGlyphs.SingleFilled().ToDrawingImage(null!));
}
