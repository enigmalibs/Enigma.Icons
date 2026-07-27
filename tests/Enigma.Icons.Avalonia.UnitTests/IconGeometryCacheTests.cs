using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Enigma.Icons.Avalonia.UnitTests.TestSupport;
using Enigma.Icons.Phosphor;
using Xunit;

namespace Enigma.Icons.Avalonia.UnitTests;

/// <summary>
/// CODE-REVIEW-1FD4 PHASE01 — <c>Icon.Render</c> parses each glyph's path data once, not once per
/// render pass.
/// </summary>
/// <remarks>
/// The cache is deliberately scoped to the render pass. <see cref="IconGlyphExtensions.ToGeometry"/>
/// and <see cref="IconGlyphExtensions.ToDrawing"/> hand their result to the consumer, and
/// <see cref="Geometry.Parse(string)"/> returns a <c>StreamGeometry</c> whose <c>Transform</c> is
/// publicly settable — a shared instance there would let one caller's mutation reach every other.
/// The last two tests pin that decision down.
/// </remarks>
public sealed class IconGeometryCacheTests
{
    /// <summary>An offset no test glyph could produce on its own, so seeing it is proof of reuse.</summary>
    private const double MarkOffset = 1000.0;

    [AvaloniaFact]
    public void GetLayerGeometries_ReturnsTheSameArrayForTheSameGlyph()
    {
        IconGlyph glyph = TestGlyphs.Duotone();

        Geometry[] first = IconGeometryCache.GetLayerGeometries(glyph);
        Geometry[] second = IconGeometryCache.GetLayerGeometries(glyph);

        Assert.Same(first, second);
        Assert.Equal(2, first.Length);
        Assert.Same(first[0], second[0]);
        Assert.Same(first[1], second[1]);
    }

    [AvaloniaFact]
    public void GetLayerGeometries_IsIndexAlignedWithTheLayers_SpacersIncluded()
    {
        // The paint pass skips the fill="none" spacer, but the array must stay index-aligned with
        // IconGlyph.Layers or Icon.DrawLayers would draw the wrong shape for the wrong layer.
        IconGlyph glyph = TestGlyphs.WithSpacer();

        Geometry[] geometries = IconGeometryCache.GetLayerGeometries(glyph);

        Assert.Equal(glyph.Layers.Count, geometries.Length);
        Assert.All(geometries, Assert.NotNull);
        Assert.Equal(new Rect(0, 0, 128, 128), geometries[0].Bounds);
        Assert.Equal(new Rect(0, 0, 64, 64), geometries[1].Bounds);
    }

    [AvaloniaFact]
    public void GetLayerGeometries_KeysOnTheGlyph_NotOnItsPathString()
    {
        // Two distinct glyph instances that happen to carry identical path data are two keys. Keying
        // on the string is the cache the old comment rejected, and rightly so.
        IconGlyph one = TestGlyphs.SingleFilled();
        IconGlyph other = TestGlyphs.SingleFilled();

        Assert.NotSame(
            IconGeometryCache.GetLayerGeometries(one)[0],
            IconGeometryCache.GetLayerGeometries(other)[0]);
    }

    [AvaloniaFact]
    public void GetLayerGeometries_MalformedPathData_ThrowsAndCachesNothing()
    {
        IconGlyph glyph = new IconGlyph(IconViewBox.Default, [new IconLayer("not a path")]);

        Assert.ThrowsAny<Exception>(() => IconGeometryCache.GetLayerGeometries(glyph));

        // A failed parse must not leave a poisoned entry behind — the next call re-attempts it.
        Assert.ThrowsAny<Exception>(() => IconGeometryCache.GetLayerGeometries(glyph));
    }

    [AvaloniaFact]
    public void Cache_DoesNotRetainAGlyphThatBecomesUnreachable()
    {
        // ConditionalWeakTable's weak keys are the whole reason a custom IIconSet can be dropped
        // without leaking its glyphs into a process-lifetime cache.
        WeakReference weak = CacheAGlyphAndForgetIt();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(weak.IsAlive);
    }

    [AvaloniaFact]
    public void Render_TwiceForTheSameGlyph_ReusesTheCachedGeometryPerLayer()
    {
        // The finding this phase exists for: a resize, a theme switch or a scroll re-renders, and the
        // second pass must not re-parse. Duotone, so both layers are covered.
        IconGlyph glyph = TestGlyphs.Duotone();
        var icon = new Icon
        {
            IconSet = new SingleGlyphIconSet(glyph),
            IconName = "only",
            Foreground = Brushes.Black,
        };

        List<Rect> first = LayerBounds(icon, 64, 64);
        MarkEveryCachedLayer(glyph);
        List<Rect> second = LayerBounds(icon, 48, 48);

        // A render pass that called Geometry.Parse again could not possibly see the mark.
        Assert.Equal(2, first.Count);
        Assert.Equal(2, second.Count);
        Assert.All(first, b => Assert.Equal(0.0, b.X));
        Assert.All(second, b => Assert.Equal(MarkOffset, b.X));
    }

    [AvaloniaFact]
    public void Render_TwoIconsSharingOneGlyph_ShareTheCachedGeometry()
    {
        // The gallery's virtualized grid: many controls, one cached glyph, one parse.
        IconGlyph glyph = TestGlyphs.SingleFilled();
        var set = new SingleGlyphIconSet(glyph);

        var a = new Icon { IconSet = set, IconName = "only", Foreground = Brushes.Black };
        var b = new Icon { IconSet = set, IconName = "only", Foreground = Brushes.Red };

        _ = LayerBounds(a, 64, 64);
        MarkEveryCachedLayer(glyph);

        Assert.Equal(MarkOffset, Assert.Single(LayerBounds(b, 32, 32)).X);
    }

    [AvaloniaFact]
    public void Render_MalformedPathData_PaintsNothingRatherThanThrowing()
    {
        var icon = new Icon
        {
            IconSet = new SingleGlyphIconSet(
                new IconGlyph(IconViewBox.Default, [new IconLayer("M0,0 L", opacity: 0.5)])),
            IconName = "only",
            Foreground = Brushes.Black,
        };

        Assert.Empty(RenderRecorder.Leaves(RenderRecorder.Record(icon, 64, 64)));
    }

    [AvaloniaFact]
    public void ToGeometry_StillReturnsAFreshInstance()
    {
        IconGlyph glyph = TestGlyphs.SingleFilled();

        Geometry first = glyph.ToGeometry();
        Geometry second = glyph.ToGeometry();

        Assert.NotSame(first, second);
        Assert.NotSame(IconGeometryCache.GetLayerGeometries(glyph)[0], first);
    }

    [AvaloniaFact]
    public void ToDrawing_StillReturnsFreshGeometry()
    {
        IconGlyph glyph = TestGlyphs.SingleFilled();

        var first = (DrawingGroup)glyph.ToDrawing(Brushes.Black);
        var second = (DrawingGroup)glyph.ToDrawing(Brushes.Black);

        var firstLeaf = (GeometryDrawing)first.Children[0];
        var secondLeaf = (GeometryDrawing)second.Children[0];

        Assert.NotSame(firstLeaf.Geometry, secondLeaf.Geometry);
        Assert.NotSame(IconGeometryCache.GetLayerGeometries(glyph)[0], firstLeaf.Geometry);
    }

    /// <summary>The recorded bounds of each painted layer, in glyph coordinates.</summary>
    /// <remarks>
    /// Bounds rather than instance identity, because <c>DrawingGroup.Open()</c>'s context does not
    /// store the <see cref="Geometry"/> it is handed — it re-wraps the platform impl in its own
    /// internal <c>PlatformGeometry</c>, so the recorded <see cref="GeometryDrawing.Geometry"/> is
    /// never reference-equal to what <c>Icon.Render</c> passed, cached or not. Marking the cached
    /// instance and watching the mark come out the other end proves reuse without depending on
    /// Avalonia internals.
    /// </remarks>
    private static List<Rect> LayerBounds(Icon icon, double width, double height)
    {
        var bounds = new List<Rect>();
        foreach (GeometryDrawing leaf in RenderRecorder.Leaves(RenderRecorder.Record(icon, width, height)))
        {
            Assert.NotNull(leaf.Geometry);
            bounds.Add(leaf.Geometry.Bounds);
        }

        return bounds;
    }

    /// <summary>Translates every cached layer of the glyph, so a later render betrays where it read from.</summary>
    private static void MarkEveryCachedLayer(IconGlyph glyph)
    {
        foreach (Geometry geometry in IconGeometryCache.GetLayerGeometries(glyph))
        {
            geometry.Transform = new TranslateTransform(MarkOffset, 0);
        }
    }

    /// <summary>
    /// Caches a glyph nobody else can reach and returns only a weak handle to it. Not inlined, so the
    /// strong reference dies with this frame rather than lingering in the caller's locals.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CacheAGlyphAndForgetIt()
    {
        IconGlyph glyph = TestGlyphs.SingleFilled();
        Assert.Single(IconGeometryCache.GetLayerGeometries(glyph));
        return new WeakReference(glyph);
    }

    /// <summary>An icon set serving exactly one glyph under the name <c>only</c>.</summary>
    private sealed class SingleGlyphIconSet(IconGlyph glyph) : IIconSet
    {
        public string Name => "Single";

        public IReadOnlyList<string> Variants => [];

        public string? DefaultVariant => null;

        public IEnumerable<string> IconNames => ["only"];

        public bool TryGetGlyph(string icon, string? variant, out IconGlyph? found)
        {
            found = string.Equals(icon, "only", StringComparison.Ordinal) ? glyph : null;
            return found is not null;
        }

        public IconGlyph GetGlyph(string icon, string? variant = null)
            => TryGetGlyph(icon, variant, out IconGlyph? found) && found is not null
                ? found
                : throw new IconNotFoundException(icon, variant, Name);
    }
}
