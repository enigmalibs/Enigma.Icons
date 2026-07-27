using System.Runtime.CompilerServices;
using Avalonia.Media;

namespace Enigma.Icons.Avalonia;

/// <summary>
/// One parsed <see cref="Geometry"/> per layer, per glyph, so a repeated render pass costs no
/// <see cref="Geometry.Parse(string)"/> at all.
/// </summary>
/// <remarks>
/// <para>
/// The key is the <see cref="IconGlyph"/> itself, <b>not</b> its path string. That is what makes the
/// cache free of an invalidation surface: <see cref="IconGlyph"/> is immutable, and both
/// <c>PhosphorIconSet</c> and <c>SvgIconSet</c> hand out a cached, reference-equal instance per
/// (icon, variant) — so a given key can never come to mean different geometry.
/// </para>
/// <para>
/// The table's keys are weak, so a custom <see cref="IIconSet"/> that goes out of scope takes its
/// glyphs — and their geometry — with it. Nothing here roots anything.
/// </para>
/// <para>
/// Layers are parsed <b>eagerly, all of them</b>, on the first request for a glyph: the array is
/// index-aligned with <see cref="IconGlyph.Layers"/>, including layers a paint pass skips (a
/// <c>fill="none"</c> spacer). One consequence is deliberate — a glyph with one malformed layer now
/// paints nothing rather than painting the layers that precede the bad one. Half a broken glyph was
/// never the intended output, and <see cref="Icon.Render"/> still swallows the failure.
/// </para>
/// </remarks>
internal static class IconGeometryCache
{
    private static readonly ConditionalWeakTable<IconGlyph, Geometry[]> Entries = new ConditionalWeakTable<IconGlyph, Geometry[]>();

    // Held in a field rather than passed as a method group: the conversion would allocate a delegate
    // on every call, on the very path this cache exists to keep allocation-free.
    private static readonly ConditionalWeakTable<IconGlyph, Geometry[]>.CreateValueCallback Factory = ParseLayers;

    /// <summary>
    /// The glyph's layers as geometry, in layer order — parsed on the first call, returned
    /// reference-equal on every call after it.
    /// </summary>
    /// <param name="glyph">The glyph to resolve. Must not be null.</param>
    /// <returns>One <see cref="Geometry"/> per entry of <see cref="IconGlyph.Layers"/>, same order.</returns>
    /// <remarks>
    /// Propagates whatever <see cref="Geometry.Parse(string)"/> throws on malformed path data; the
    /// failed glyph is simply not added to the table, so a later call re-attempts the parse.
    /// </remarks>
    internal static Geometry[] GetLayerGeometries(IconGlyph glyph)
        => Entries.GetValue(glyph, Factory);

    private static Geometry[] ParseLayers(IconGlyph glyph)
    {
        var geometries = new Geometry[glyph.Layers.Count];
        for (int i = 0; i < geometries.Length; i++)
        {
            geometries[i] = Geometry.Parse(glyph.Layers[i].PathData);
        }

        return geometries;
    }
}
