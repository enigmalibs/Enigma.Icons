using System;
using Avalonia.Media;
using Enigma.Icons.Phosphor;

namespace Enigma.Icons.AppIconStudio.Design;

/// <summary>
/// Everything needed to compose one app icon: a rounded plate, and a Phosphor glyph centred on it.
/// Immutable — the ViewModel builds a fresh instance whenever a control moves, and the same instance
/// then drives both the preview and the export, which is what keeps the two identical.
/// </summary>
public sealed class IconDesign
{
    /// <summary>Corner radius of a new design, as a fraction of the plate edge.</summary>
    /// <remarks>0.22 is the radius of the existing <c>Enigma.MarkdownEditor</c> plate, measured from
    /// its 256 px artwork.</remarks>
    public const double DefaultCornerRadiusRatio = 0.22;

    /// <summary>The largest accepted <see cref="CornerRadiusRatio"/>. At 0.5 the plate is a circle.</summary>
    public const double MaxCornerRadiusRatio = 0.5;

    /// <summary>Glyph size of a new design, as a fraction of the plate edge.</summary>
    public const double DefaultGlyphScale = 0.60;

    /// <summary>The smallest accepted <see cref="GlyphScale"/>. Below this the glyph is a dot.</summary>
    public const double MinGlyphScale = 0.2;

    /// <summary>The largest accepted <see cref="GlyphScale"/> — the glyph spans the whole plate edge.</summary>
    public const double MaxGlyphScale = 1.0;

    /// <summary>Initializes a new design.</summary>
    /// <param name="icon">The Phosphor glyph drawn on the plate.</param>
    /// <param name="weight">The weight that glyph is drawn in.</param>
    /// <param name="glyphColor">The colour every glyph layer paints with.</param>
    /// <param name="plate">How the plate behind the glyph is painted.</param>
    /// <param name="cornerRadiusRatio">
    /// Corner radius as a fraction of the plate edge, 0.0 (a square) to
    /// <see cref="MaxCornerRadiusRatio"/> (a circle).
    /// </param>
    /// <param name="glyphScale">
    /// The fraction of the plate edge the glyph's view box spans, <see cref="MinGlyphScale"/> to
    /// <see cref="MaxGlyphScale"/>.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="plate"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="cornerRadiusRatio"/> or <paramref name="glyphScale"/> is outside its range, or
    /// is <see cref="double.NaN"/>.
    /// </exception>
    public IconDesign(
        PhosphorIcon icon,
        PhosphorWeight weight,
        Color glyphColor,
        PlateFill plate,
        double cornerRadiusRatio = DefaultCornerRadiusRatio,
        double glyphScale = DefaultGlyphScale)
    {
        if (plate is null)
        {
            throw new ArgumentNullException(nameof(plate));
        }

        // Negated range tests, so NaN is rejected rather than silently admitted — the idiom
        // Enigma.Icons uses in IconViewBox and IconLayer.
        if (!(cornerRadiusRatio >= 0.0 && cornerRadiusRatio <= MaxCornerRadiusRatio))
        {
            throw new ArgumentOutOfRangeException(
                nameof(cornerRadiusRatio),
                cornerRadiusRatio,
                "The corner radius ratio must be between 0.0 and 0.5 inclusive.");
        }

        if (!(glyphScale >= MinGlyphScale && glyphScale <= MaxGlyphScale))
        {
            throw new ArgumentOutOfRangeException(
                nameof(glyphScale),
                glyphScale,
                "The glyph scale must be between 0.2 and 1.0 inclusive.");
        }

        Icon = icon;
        Weight = weight;
        GlyphColor = glyphColor;
        Plate = plate;
        CornerRadiusRatio = cornerRadiusRatio;
        GlyphScale = glyphScale;
    }

    /// <summary>The Phosphor glyph drawn on the plate.</summary>
    public PhosphorIcon Icon { get; }

    /// <summary>The weight that glyph is drawn in.</summary>
    public PhosphorWeight Weight { get; }

    /// <summary>The colour every glyph layer paints with.</summary>
    /// <remarks>
    /// Deliberately a design input, not a hard-coded white: an icon whose plate is pale needs a dark
    /// glyph, and forcing white would make half the plate colours unusable.
    /// </remarks>
    public Color GlyphColor { get; }

    /// <summary>How the plate behind the glyph is painted. Never null.</summary>
    public PlateFill Plate { get; }

    /// <summary>Corner radius as a fraction of the plate edge, 0.0–0.5.</summary>
    public double CornerRadiusRatio { get; }

    /// <summary>The fraction of the plate edge the glyph's view box spans, 0.2–1.0.</summary>
    public double GlyphScale { get; }
}
