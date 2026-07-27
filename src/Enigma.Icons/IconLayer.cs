using System;

namespace Enigma.Icons;

/// <summary>
/// One paintable element of a glyph. Duotone's tinted backing shape and its foreground shape are
/// two layers; a stroked user SVG contributes layers carrying stroke metadata.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why the paint values are raw strings and not a colour type.</b> <c>Enigma.Icons</c> has zero
/// dependencies and therefore no framework colour type. Each renderer interprets
/// <see cref="Fill"/> and <see cref="Stroke"/> in its own terms and, in the normal icon case,
/// ignores them entirely in favour of the consumer's brush. <see langword="null"/> — the normalized
/// form of <c>"currentColor"</c> — is what makes "inherit the renderer's brush" the default.
/// </para>
/// <para>
/// A layer never carries a transform: <see cref="SvgIconParser"/> bakes every SVG transform into
/// <see cref="PathData"/>.
/// </para>
/// </remarks>
public sealed class IconLayer
{
    /// <summary>Initializes a new layer.</summary>
    /// <param name="pathData">SVG path mini-language geometry. Must not be null, empty or whitespace.</param>
    /// <param name="opacity">Layer opacity in the closed range 0.0–1.0.</param>
    /// <param name="fillRule">The fill rule used when the layer is filled.</param>
    /// <param name="fill">
    /// Raw SVG paint value, or <see langword="null"/> when the layer inherits the renderer's brush.
    /// Stored verbatim: <c>"currentColor"</c> is normalized to <see langword="null"/> by the
    /// <see cref="SvgIconParser"/>, not by this constructor, so a hand-built layer keeps exactly the
    /// value it was given.
    /// </param>
    /// <param name="stroke">Raw SVG stroke paint value, or <see langword="null"/>. Stored verbatim, as <paramref name="fill"/> is.</param>
    /// <param name="strokeWidth">Stroke width in view-box units, or <see langword="null"/> when unspecified.</param>
    /// <param name="strokeLineCap">Stroke line cap, or <see langword="null"/> when unspecified.</param>
    /// <param name="strokeLineJoin">Stroke line join, or <see langword="null"/> when unspecified.</param>
    /// <exception cref="ArgumentException"><paramref name="pathData"/> is null, empty or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="opacity"/> is outside 0.0–1.0, or is <see cref="double.NaN"/>.</exception>
    public IconLayer(
        string pathData,
        double opacity = 1.0,
        IconFillRule fillRule = IconFillRule.NonZero,
        string? fill = null,
        string? stroke = null,
        double? strokeWidth = null,
        IconLineCap? strokeLineCap = null,
        IconLineJoin? strokeLineJoin = null)
    {
        if (string.IsNullOrWhiteSpace(pathData))
        {
            throw new ArgumentException("Path data must not be null, empty or whitespace.", nameof(pathData));
        }

        // Negated range test so NaN is rejected rather than silently admitted.
        if (!(opacity >= 0.0 && opacity <= 1.0))
        {
            throw new ArgumentOutOfRangeException(nameof(opacity), opacity, "Opacity must be between 0.0 and 1.0 inclusive.");
        }

        PathData = pathData;
        Opacity = opacity;
        FillRule = fillRule;
        Fill = fill;
        Stroke = stroke;
        StrokeWidth = strokeWidth;
        StrokeLineCap = strokeLineCap;
        StrokeLineJoin = strokeLineJoin;

        // Computed once: the type is immutable.
        IsStroked = stroke is not null && !string.Equals(stroke, "none", StringComparison.Ordinal);
        IsFilled = !string.Equals(fill, "none", StringComparison.Ordinal);
    }

    /// <summary>SVG path mini-language geometry. Never null, never empty.</summary>
    public string PathData { get; }

    /// <summary>0.0–1.0. 1.0 for a fully opaque layer.</summary>
    public double Opacity { get; }

    /// <summary>The fill rule used when the layer is filled.</summary>
    public IconFillRule FillRule { get; }

    /// <summary>
    /// Raw SVG paint value, or null when the layer inherits the renderer's brush.
    /// "none" means "do not fill". "currentColor" is normalized to null.
    /// </summary>
    public string? Fill { get; }

    /// <summary>Raw SVG stroke paint value, or null when the layer inherits the renderer's brush.</summary>
    public string? Stroke { get; }

    /// <summary>Stroke width in view-box units, or null when unspecified.</summary>
    public double? StrokeWidth { get; }

    /// <summary>Stroke line cap, or null when unspecified.</summary>
    public IconLineCap? StrokeLineCap { get; }

    /// <summary>Stroke line join, or null when unspecified.</summary>
    public IconLineJoin? StrokeLineJoin { get; }

    /// <summary>True when this layer should be stroked rather than (or as well as) filled.</summary>
    public bool IsStroked { get; }

    /// <summary>
    /// True when this layer should be filled. A <see langword="null"/> <see cref="Fill"/> is filled —
    /// null means "inherit the renderer's brush", only the literal <c>"none"</c> disables filling.
    /// </summary>
    public bool IsFilled { get; }
}

/// <summary>How a filled path decides which regions are inside it.</summary>
public enum IconFillRule
{
    /// <summary>SVG's <c>nonzero</c> fill rule — the default.</summary>
    NonZero,

    /// <summary>SVG's <c>evenodd</c> fill rule.</summary>
    EvenOdd,
}

/// <summary>The shape drawn at the open ends of a stroked subpath.</summary>
public enum IconLineCap
{
    /// <summary>SVG's <c>butt</c> cap — the stroke ends flush with the end point.</summary>
    Flat,

    /// <summary>SVG's <c>round</c> cap.</summary>
    Round,

    /// <summary>SVG's <c>square</c> cap.</summary>
    Square,
}

/// <summary>The shape drawn where two stroked segments meet.</summary>
public enum IconLineJoin
{
    /// <summary>SVG's <c>miter</c> join — the default.</summary>
    Miter,

    /// <summary>SVG's <c>round</c> join.</summary>
    Round,

    /// <summary>SVG's <c>bevel</c> join.</summary>
    Bevel,
}
