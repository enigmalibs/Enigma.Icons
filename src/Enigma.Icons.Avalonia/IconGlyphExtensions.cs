using System;
using Avalonia.Media;

namespace Enigma.Icons.Avalonia;

/// <summary>
/// Converts an <see cref="IconGlyph"/> — from <i>any</i> <see cref="IIconSet"/>, Phosphor or a
/// <see cref="SvgIconSet"/> of your own SVGs — into Avalonia drawing primitives.
/// </summary>
/// <remarks>
/// <para>
/// The input side is deliberately framework-agnostic: these methods know nothing about Phosphor.
/// A glyph parsed from a consumer's own <c>.svg</c> files renders exactly like a built-in one.
/// </para>
/// <para>
/// A <see cref="Geometry.Parse(string)"/> failure is allowed to propagate. Malformed path data in a
/// committed asset is a bug, not a runtime condition — the one place that rule is relaxed is
/// <see cref="Icon"/>, whose render pass must keep the XAML previewer alive.
/// </para>
/// </remarks>
public static class IconGlyphExtensions
{
    /// <summary>The glyph as a single geometry.</summary>
    /// <remarks>
    /// <para>
    /// A one-layer glyph becomes the parsed path itself; a multi-layer glyph becomes a
    /// <see cref="GeometryGroup"/> whose children are the parsed layers in paint order, with the
    /// <see cref="GeometryGroup.FillRule"/> taken from the <b>first</b> layer.
    /// </para>
    /// <para>
    /// <b>Per-layer opacity is LOST.</b> A <see cref="GeometryGroup"/> has no per-child opacity and
    /// one shared fill rule, so a duotone glyph collapsed this way paints its tinted backing layer at
    /// full opacity — visually wrong, and deliberately allowed because a single
    /// <see cref="Geometry"/> is what <c>Path.Data</c> needs. For a duotone-correct result use
    /// <see cref="ToDrawing"/> (or the <see cref="Icon"/> control, which walks the layers itself).
    /// </para>
    /// </remarks>
    /// <param name="glyph">The glyph to convert.</param>
    /// <returns>The glyph's geometry.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glyph"/> is null.</exception>
    public static Geometry ToGeometry(this IconGlyph glyph)
    {
        ArgumentNullException.ThrowIfNull(glyph);

        // Discriminated on the layer COUNT, not on IconGlyph.IsSingleLayer: the latter additionally
        // requires opacity 1 and no stroke, and a one-layer stroked or translucent glyph must still
        // collapse to a bare geometry rather than be boxed in a pointless one-child group.
        if (glyph.Layers.Count == 1)
        {
            return Geometry.Parse(glyph.Layers[0].PathData);
        }

        var group = new GeometryGroup { FillRule = ToFillRule(glyph.Layers[0].FillRule) };
        for (int i = 0; i < glyph.Layers.Count; i++)
        {
            group.Children.Add(Geometry.Parse(glyph.Layers[i].PathData));
        }

        return group;
    }

    /// <summary>The glyph as a <see cref="Drawing"/>, preserving per-layer opacity.</summary>
    /// <remarks>
    /// The result is a root <see cref="DrawingGroup"/> holding one <see cref="GeometryDrawing"/> per
    /// painted layer, in paint order. A layer whose <see cref="IconLayer.Opacity"/> is below 1 is
    /// wrapped in its own <see cref="DrawingGroup"/> carrying that opacity, because
    /// <see cref="GeometryDrawing"/> has none of its own; fully opaque layers are added to the root
    /// unwrapped. A layer that is neither filled nor stroked — the legitimate transparent spacer of a
    /// hand-authored SVG — contributes nothing at all.
    /// </remarks>
    /// <param name="glyph">The glyph to convert.</param>
    /// <param name="brush">The brush every layer paints with, unless the layer names its own stroke paint.</param>
    /// <returns>The glyph's drawing.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glyph"/> or <paramref name="brush"/> is null.</exception>
    public static Drawing ToDrawing(this IconGlyph glyph, IBrush brush)
    {
        ArgumentNullException.ThrowIfNull(glyph);
        ArgumentNullException.ThrowIfNull(brush);

        var root = new DrawingGroup();

        for (int i = 0; i < glyph.Layers.Count; i++)
        {
            IconLayer layer = glyph.Layers[i];
            if (!TryGetLayerPaint(layer, brush, out IBrush? fill, out IPen? pen))
            {
                continue;
            }

            var drawing = new GeometryDrawing
            {
                Geometry = Geometry.Parse(layer.PathData),
                Brush = fill,
                Pen = pen,
            };

            if (layer.Opacity < 1.0)
            {
                var wrapper = new DrawingGroup { Opacity = layer.Opacity };
                wrapper.Children.Add(drawing);
                root.Children.Add(wrapper);
            }
            else
            {
                root.Children.Add(drawing);
            }
        }

        return root;
    }

    /// <summary>The glyph as a <see cref="DrawingImage"/>, suitable for <c>Image.Source</c>.</summary>
    /// <param name="glyph">The glyph to convert.</param>
    /// <param name="brush">The brush every layer paints with, unless the layer names its own stroke paint.</param>
    /// <returns>A <see cref="DrawingImage"/> wrapping <see cref="ToDrawing"/>'s result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glyph"/> or <paramref name="brush"/> is null.</exception>
    public static DrawingImage ToDrawingImage(this IconGlyph glyph, IBrush brush)
        => new DrawingImage { Drawing = glyph.ToDrawing(brush) };

    /// <summary>
    /// The single per-layer paint decision, shared by <see cref="ToDrawing"/> and
    /// <see cref="Icon"/>'s render pass so the two cannot drift apart. Only the emission differs —
    /// an object graph there, <see cref="DrawingContext"/> calls here.
    /// </summary>
    /// <param name="layer">The layer to inspect.</param>
    /// <param name="brush">The brush supplied by the caller or the control.</param>
    /// <param name="fill">The fill brush, or null when the layer is stroke-only.</param>
    /// <param name="pen">The pen, or null when the layer is fill-only.</param>
    /// <returns><see langword="false"/> when the layer paints nothing and must be skipped entirely.</returns>
    internal static bool TryGetLayerPaint(IconLayer layer, IBrush brush, out IBrush? fill, out IPen? pen)
    {
        fill = null;
        pen = null;

        // The transparent spacer case: fill="none" with no stroke. Emit nothing for it — not an
        // empty group, not a null-brushed drawing.
        if (!layer.IsFilled && !layer.IsStroked)
        {
            return false;
        }

        if (layer.IsFilled)
        {
            // The consumer's brush always wins over layer.Fill. The raw paint string is deliberately
            // ignored for fills: "currentColor" is already normalized to null by the parser, and an
            // icon that refuses to take the caller's colour is the bug this avoids.
            fill = brush;
        }

        if (layer.IsStroked)
        {
            pen = new Pen(
                ResolveStrokeBrush(layer, brush),
                layer.StrokeWidth ?? 1.0,
                null,
                ToPenLineCap(layer.StrokeLineCap),
                ToPenLineJoin(layer.StrokeLineJoin));
        }

        return true;
    }

    /// <summary>
    /// Resolves a layer's stroke paint. A named paint is honoured; anything unparseable — and a null
    /// <see cref="IconLayer.Stroke"/> — falls back to the supplied brush.
    /// </summary>
    /// <remarks>
    /// This is the deliberate asymmetry with fills, where the supplied brush always wins: a
    /// hand-authored SVG that says <c>stroke="red"</c> means it, whereas an icon's fill is the thing
    /// the consumer is expected to recolour. The fallback is silent by design — a bad paint string
    /// must never take down a render pass.
    /// </remarks>
    private static IBrush ResolveStrokeBrush(IconLayer layer, IBrush brush)
    {
        // Defensive rather than dead: IconLayer.IsStroked already implies a non-null Stroke today,
        // but the SPEC states the null case explicitly and this keeps the two in agreement.
        if (layer.Stroke is null)
        {
            return brush;
        }

        try
        {
            return Brush.Parse(layer.Stroke);
        }
        catch (FormatException)
        {
            return brush;
        }
        catch (ArgumentException)
        {
            return brush;
        }
    }

    private static FillRule ToFillRule(IconFillRule fillRule)
        => fillRule == IconFillRule.EvenOdd ? FillRule.EvenOdd : FillRule.NonZero;

    private static PenLineCap ToPenLineCap(IconLineCap? lineCap)
        => lineCap switch
        {
            IconLineCap.Round => PenLineCap.Round,
            IconLineCap.Square => PenLineCap.Square,
            // SVG's default cap is butt, which Avalonia calls Flat.
            _ => PenLineCap.Flat,
        };

    private static PenLineJoin ToPenLineJoin(IconLineJoin? lineJoin)
        => lineJoin switch
        {
            IconLineJoin.Round => PenLineJoin.Round,
            IconLineJoin.Bevel => PenLineJoin.Bevel,
            // SVG's default join is miter.
            _ => PenLineJoin.Miter,
        };
}
