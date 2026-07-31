using System.Collections.Generic;

namespace Enigma.Icons.Avalonia.UnitTests.TestSupport;

/// <summary>
/// Synthetic glyphs, so the shape assertions do not depend on the Phosphor corpus. A handful of tests
/// additionally use <c>PhosphorIconSet.Instance</c> for the real <c>Kind</c>/<c>Weight</c> path.
/// </summary>
internal static class TestGlyphs
{
    /// <summary>A 64×64 square in the top-left quadrant of the default view box.</summary>
    public const string SquarePath = "M0,0 L64,0 L64,64 L0,64 Z";

    /// <summary>A 128×128 square, distinguishable from <see cref="SquarePath"/> by its bounds.</summary>
    public const string BigSquarePath = "M0,0 L128,0 L128,128 L0,128 Z";

    /// <summary>The tinted-backing opacity every Phosphor duotone glyph's first layer carries.</summary>
    public const double DuotoneBackingOpacity = 0.2;

    /// <summary>One fully-opaque filled layer — the common case.</summary>
    public static IconGlyph SingleFilled()
        => new IconGlyph(IconViewBox.Default, [new IconLayer(SquarePath)]);

    /// <summary>Two layers shaped like a duotone glyph: a 0.2-opacity backing under an opaque foreground.</summary>
    public static IconGlyph Duotone()
        => new IconGlyph(
            IconViewBox.Default,
            [
                new IconLayer(BigSquarePath, opacity: DuotoneBackingOpacity),
                new IconLayer(SquarePath),
            ]);

    /// <summary>The same two layers as <see cref="Duotone"/>, both fully opaque.</summary>
    public static IconGlyph DuotoneAllOpaque()
        => new IconGlyph(
            IconViewBox.Default,
            [
                new IconLayer(BigSquarePath),
                new IconLayer(SquarePath),
            ]);

    /// <summary>A transparent spacer (<c>fill="none"</c>, no stroke) ahead of a painted layer.</summary>
    public static IconGlyph WithSpacer()
        => new IconGlyph(
            IconViewBox.Default,
            [
                new IconLayer(BigSquarePath, fill: "none"),
                new IconLayer(SquarePath),
            ]);

    /// <summary>One stroked-and-filled layer with a known width, cap and join.</summary>
    public static IconGlyph Stroked()
        => new IconGlyph(
            IconViewBox.Default,
            [
                new IconLayer(
                    SquarePath,
                    stroke: "#FF0000",
                    strokeWidth: 4.0,
                    strokeLineCap: IconLineCap.Round,
                    strokeLineJoin: IconLineJoin.Bevel),
            ]);

    /// <summary>One stroked but unfilled layer — outlined only.</summary>
    public static IconGlyph StrokedOnly()
        => new IconGlyph(
            IconViewBox.Default,
            [new IconLayer(SquarePath, fill: "none", stroke: "#00FF00", strokeWidth: 2.0)]);

    /// <summary>A stroked layer whose stroke paint is unparseable — the silent-fallback case.</summary>
    public static IconGlyph StrokedWithGarbagePaint()
        => new IconGlyph(
            IconViewBox.Default,
            [new IconLayer(SquarePath, stroke: "not-a-colour")]);

    /// <summary>Two layers whose <b>first</b> carries the even-odd fill rule.</summary>
    public static IconGlyph EvenOddFirst()
        => new IconGlyph(
            IconViewBox.Default,
            [
                new IconLayer(BigSquarePath, fillRule: IconFillRule.EvenOdd),
                new IconLayer(SquarePath),
            ]);

    /// <summary>A glyph whose view box does not start at the origin, for the centring maths.</summary>
    public static IconGlyph OffsetViewBox()
        => new IconGlyph(new IconViewBox(10, 20, 100, 50), [new IconLayer(SquarePath)]);

    /// <summary>An in-memory SVG source set holding one icon named <c>logo</c>.</summary>
    public static IEnumerable<KeyValuePair<string, string>> LogoSvgSource()
        =>
        [
            new KeyValuePair<string, string>(
                "logo",
                """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32"><path d="M0,0 L32,0 L32,32 L0,32 Z"/></svg>"""),
        ];
}
