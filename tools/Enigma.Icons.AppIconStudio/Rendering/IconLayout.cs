using System;
using Avalonia;

// IconViewBox lives in the enclosing Enigma.Icons namespace and needs no using directive here.
namespace Enigma.Icons.AppIconStudio.Rendering;

/// <summary>
/// The geometry of a composed icon: where the plate is, how big its corners are, where the glyph
/// lands on it, and which way a gradient runs.
/// </summary>
/// <remarks>
/// <para>
/// Pure arithmetic over Avalonia's value types — <see cref="Rect"/>, <see cref="RoundedRect"/>,
/// <see cref="Matrix"/>, <see cref="RelativePoint"/> — none of which needs a rendering platform to
/// construct. That is deliberate: it makes every layout decision unit-testable without a render
/// backend, leaving only the pixels themselves to the rasterizer.
/// </para>
/// <para>
/// All coordinates are in device-independent pixels, and the rasterizer builds its render target at
/// 96 DPI, so one unit here is exactly one output pixel.
/// </para>
/// </remarks>
public static class IconLayout
{
    /// <summary>The smallest canvas edge, in pixels, any of these methods will compute for.</summary>
    public const int MinSizePx = 1;

    /// <summary>The largest canvas edge, in pixels, any of these methods will compute for.</summary>
    /// <remarks>
    /// Well above the 1,024 px the studio offers, and far enough below the point where a square
    /// 32-bpp buffer becomes unreasonable (2,048 px is 16 MiB) to be a sanity bound rather than a
    /// limitation.
    /// </remarks>
    public const int MaxSizePx = 2048;

    /// <summary>The rounded square the plate fills, for a square canvas of <paramref name="sizePx"/>.</summary>
    /// <remarks>
    /// The plate is always edge-to-edge: an app icon's own margin is the platform's business, and
    /// baking one in would shrink the artwork twice on the platforms that add their own.
    /// </remarks>
    /// <param name="sizePx">The canvas edge in pixels.</param>
    /// <param name="cornerRadiusRatio">
    /// Corner radius as a fraction of the edge, 0.0 (a square) to
    /// <see cref="Design.IconDesign.MaxCornerRadiusRatio"/> (a circle).
    /// </param>
    /// <returns>The plate rectangle with a uniform corner radius.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="sizePx"/> is outside
    /// <see cref="MinSizePx"/>–<see cref="MaxSizePx"/>, or <paramref name="cornerRadiusRatio"/> is
    /// outside 0.0–0.5 or is <see cref="double.NaN"/>.</exception>
    public static RoundedRect PlateRect(int sizePx, double cornerRadiusRatio)
    {
        ValidateSize(sizePx);

        // Negated range test so NaN is rejected rather than silently admitted.
        if (!(cornerRadiusRatio >= 0.0 && cornerRadiusRatio <= Design.IconDesign.MaxCornerRadiusRatio))
        {
            throw new ArgumentOutOfRangeException(
                nameof(cornerRadiusRatio),
                cornerRadiusRatio,
                "The corner radius ratio must be between 0.0 and 0.5 inclusive.");
        }

        return new RoundedRect(new Rect(0, 0, sizePx, sizePx), sizePx * cornerRadiusRatio);
    }

    /// <summary>
    /// The transform that maps a glyph's view-box coordinates onto the plate: scaled uniformly to
    /// span <paramref name="glyphScale"/> of the canvas edge, and centred.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is <c>Icon.Render</c>'s <c>Stretch.Uniform</c> arithmetic (SPEC §10.2) with one
    /// difference: the target box is the centred square of side <c>sizePx * glyphScale</c> rather
    /// than the full bounds. Scale is taken from the <b>smaller</b> axis so a non-square view box
    /// still fits, and the view-box origin is subtracted so a box that does not start at 0,0 lands
    /// where it should. Keeping the two derivations visibly the same is the point — if
    /// <c>Icon.Render</c> ever changes how it centres, this is the method that has to follow.
    /// </para>
    /// <para>
    /// Every Phosphor glyph is <c>0 0 256 256</c> (SPEC §7.4), so in practice the two axes agree and
    /// the origin is zero; the general form is here for a custom <see cref="IIconSet"/>, and because
    /// a special case that only works for squares is a trap.
    /// </para>
    /// </remarks>
    /// <param name="viewBox">The glyph's view box.</param>
    /// <param name="sizePx">The canvas edge in pixels.</param>
    /// <param name="glyphScale">The fraction of the edge the glyph spans, 0.2–1.0.</param>
    /// <returns>The scale-then-translate transform to push before drawing the glyph's layers.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="sizePx"/> is outside
    /// <see cref="MinSizePx"/>–<see cref="MaxSizePx"/>, or <paramref name="glyphScale"/> is outside
    /// 0.2–1.0 or is <see cref="double.NaN"/>.</exception>
    public static Matrix GlyphTransform(IconViewBox viewBox, int sizePx, double glyphScale)
    {
        ValidateSize(sizePx);

        if (!(glyphScale >= Design.IconDesign.MinGlyphScale && glyphScale <= Design.IconDesign.MaxGlyphScale))
        {
            throw new ArgumentOutOfRangeException(
                nameof(glyphScale),
                glyphScale,
                "The glyph scale must be between 0.2 and 1.0 inclusive.");
        }

        double target = sizePx * glyphScale;
        double scale = Math.Min(target / viewBox.Width, target / viewBox.Height);

        // Centre in the FULL canvas, not in the target square: the glyph sits in the middle of the
        // plate, and the scale is what the target square governs.
        double tx = (-viewBox.X * scale) + ((sizePx - (viewBox.Width * scale)) / 2);
        double ty = (-viewBox.Y * scale) + ((sizePx - (viewBox.Height * scale)) / 2);

        return Matrix.CreateScale(scale, scale) * Matrix.CreateTranslation(tx, ty);
    }

    /// <summary>The two ends of a linear gradient running at <paramref name="angleDegrees"/>.</summary>
    /// <remarks>
    /// <para>
    /// Angles are measured clockwise from left-to-right, in the screen's y-down coordinate system:
    /// 0° runs left to right, 90° top to bottom, 180° right to left, 270° bottom to top, and 45° —
    /// the default — corner to corner from top-left to bottom-right.
    /// </para>
    /// <para>
    /// The line always passes through the centre of the unit square, and its half-length is
    /// <c>(|cos θ| + |sin θ|) / 2</c>, which is the distance from the centre to the square's boundary
    /// along that direction. That is what makes 45° land exactly on the two opposite corners instead
    /// of stopping short of them, and what keeps the visible colour range constant as the angle
    /// turns.
    /// </para>
    /// </remarks>
    /// <param name="angleDegrees">The direction in degrees. Any finite value; 0–360 is the natural range.</param>
    /// <returns>The gradient's start and end points, in relative (0–1) units.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="angleDegrees"/> is
    /// <see cref="double.NaN"/> or infinite.</exception>
    public static (RelativePoint Start, RelativePoint End) GradientLine(double angleDegrees)
    {
        if (double.IsNaN(angleDegrees) || double.IsInfinity(angleDegrees))
        {
            throw new ArgumentOutOfRangeException(
                nameof(angleDegrees),
                angleDegrees,
                "The gradient angle must be a finite number of degrees.");
        }

        double radians = angleDegrees * Math.PI / 180.0;
        double dx = Math.Cos(radians);
        double dy = Math.Sin(radians);
        double half = (Math.Abs(dx) + Math.Abs(dy)) / 2.0;

        var start = new RelativePoint(0.5 - (dx * half), 0.5 - (dy * half), RelativeUnit.Relative);
        var end = new RelativePoint(0.5 + (dx * half), 0.5 + (dy * half), RelativeUnit.Relative);

        return (start, end);
    }

    private static void ValidateSize(int sizePx)
    {
        if (sizePx < MinSizePx || sizePx > MaxSizePx)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sizePx),
                sizePx,
                "The canvas edge must be between 1 and 2048 pixels inclusive.");
        }
    }
}
