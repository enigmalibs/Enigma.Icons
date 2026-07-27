using System;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Enigma.Icons.Avalonia.UnitTests.TestSupport;
using Enigma.Icons.Phosphor;
using Xunit;

namespace Enigma.Icons.Avalonia.UnitTests;

/// <summary>
/// SPEC §12.3, <c>ToGeometry</c> bullet.
/// </summary>
/// <remarks>
/// Every test carries <c>[AvaloniaFact]</c>, not <c>[Fact]</c>: <c>Geometry.Parse</c> needs the
/// platform render interface, and without the headless fixture it fails with an obscure
/// "Unable to locate IPlatformRenderInterface" that reads like a bad path string.
/// </remarks>
public sealed class IconGlyphExtensionsGeometryTests
{
    [AvaloniaFact]
    public void ToGeometry_SingleLayer_IsParseableWithNonEmptyBounds()
    {
        Geometry geometry = TestGlyphs.SingleFilled().ToGeometry();

        Assert.IsNotType<GeometryGroup>(geometry);
        Assert.True(geometry.Bounds.Width > 0, "the parsed geometry has no width");
        Assert.True(geometry.Bounds.Height > 0, "the parsed geometry has no height");
    }

    [AvaloniaFact]
    public void ToGeometry_SingleStrokedLayer_StillCollapsesToABareGeometry()
    {
        IconGlyph glyph = TestGlyphs.Stroked();

        // The discriminator is the layer COUNT, not IsSingleLayer — which is false here because the
        // layer is stroked. A one-layer glyph must never be boxed in a one-child group.
        Assert.False(glyph.IsSingleLayer);
        Assert.IsNotType<GeometryGroup>(glyph.ToGeometry());
    }

    [AvaloniaFact]
    public void ToGeometry_MultiLayer_ReturnsOneChildPerLayer()
    {
        var group = Assert.IsType<GeometryGroup>(TestGlyphs.Duotone().ToGeometry());

        Assert.Equal(2, group.Children.Count);
    }

    [AvaloniaFact]
    public void ToGeometry_MultiLayer_TakesFillRuleFromTheFirstLayer()
    {
        var evenOdd = Assert.IsType<GeometryGroup>(TestGlyphs.EvenOddFirst().ToGeometry());
        Assert.Equal(FillRule.EvenOdd, evenOdd.FillRule);

        var nonZero = Assert.IsType<GeometryGroup>(TestGlyphs.Duotone().ToGeometry());
        Assert.Equal(FillRule.NonZero, nonZero.FillRule);
    }

    [AvaloniaFact]
    public void ToGeometry_Duotone_LosesPerLayerOpacity()
    {
        var translucent = Assert.IsType<GeometryGroup>(TestGlyphs.Duotone().ToGeometry());
        var opaque = Assert.IsType<GeometryGroup>(TestGlyphs.DuotoneAllOpaque().ToGeometry());

        // Documented, deliberate, and asserted so it stays deliberate: a GeometryGroup has no
        // per-child opacity, so the 0.2 backing layer comes out indistinguishable from an opaque one.
        // ToDrawing is the duotone-correct path and its own test asserts the 0.2 survives there.
        Assert.Equal(opaque.Children.Count, translucent.Children.Count);
        Assert.Equal(opaque.FillRule, translucent.FillRule);
        for (int i = 0; i < translucent.Children.Count; i++)
        {
            Assert.Equal(opaque.Children[i].Bounds, translucent.Children[i].Bounds);
        }
    }

    [AvaloniaFact]
    public void ToGeometry_RealPhosphorDuotone_ReturnsATwoChildGroup()
    {
        IconGlyph glyph = PhosphorIconSet.Instance.GetGlyph(PhosphorIcon.Acorn, PhosphorWeight.Duotone);

        var group = Assert.IsType<GeometryGroup>(glyph.ToGeometry());

        Assert.Equal(glyph.Layers.Count, group.Children.Count);
        Assert.Equal(2, group.Children.Count);
    }

    [AvaloniaFact]
    public void ToGeometry_NullGlyph_Throws()
        => Assert.Throws<ArgumentNullException>(() => IconGlyphExtensions.ToGeometry(null!));
}
