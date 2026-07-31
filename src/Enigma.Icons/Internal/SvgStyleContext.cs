using System;

namespace Enigma.Icons.Internal;

/// <summary>
/// The inheritable presentation state pushed and popped per <c>&lt;g&gt;</c>: the composed
/// transform, the multiplied opacity, and the overridable paint properties.
/// </summary>
internal readonly struct SvgStyleContext
{
    private static readonly SvgStyleContext _root = new SvgStyleContext(
        Matrix2D.Identity,
        1.0,
        fill: null,
        stroke: null,
        strokeWidth: null,
        strokeLineCap: null,
        strokeLineJoin: null,
        IconFillRule.NonZero);

    private SvgStyleContext(
        Matrix2D transform,
        double opacity,
        string? fill,
        string? stroke,
        double? strokeWidth,
        IconLineCap? strokeLineCap,
        IconLineJoin? strokeLineJoin,
        IconFillRule fillRule)
    {
        Transform = transform;
        Opacity = opacity;
        Fill = fill;
        Stroke = stroke;
        StrokeWidth = strokeWidth;
        StrokeLineCap = strokeLineCap;
        StrokeLineJoin = strokeLineJoin;
        FillRule = fillRule;
    }

    /// <summary>The transform composed from every ancestor and this element.</summary>
    internal Matrix2D Transform { get; }

    /// <summary>The opacity multiplied down the whole inheritance chain.</summary>
    internal double Opacity { get; }

    /// <summary>The inherited fill paint, or null for "inherit the renderer's brush".</summary>
    internal string? Fill { get; }

    /// <summary>The inherited stroke paint, or null when unspecified.</summary>
    internal string? Stroke { get; }

    /// <summary>The inherited stroke width, or null when unspecified.</summary>
    internal double? StrokeWidth { get; }

    /// <summary>The inherited stroke line cap, or null when unspecified.</summary>
    internal IconLineCap? StrokeLineCap { get; }

    /// <summary>The inherited stroke line join, or null when unspecified.</summary>
    internal IconLineJoin? StrokeLineJoin { get; }

    /// <summary>The inherited fill rule.</summary>
    internal IconFillRule FillRule { get; }

    /// <summary>
    /// The starting context: identity transform, full opacity, no paint values, non-zero fill rule.
    /// </summary>
    /// <remarks>
    /// The null paint defaults are a deliberate divergence from CSS, whose initial <c>fill</c> is
    /// black. Here "unspecified" means <i>inherit the consumer's brush</i>, which is the behaviour
    /// an icon library wants.
    /// </remarks>
    internal static SvgStyleContext Root => _root;

    /// <summary>
    /// Produces the child context from this one plus a single element's presentation attributes.
    /// The element's own value always wins; anything it does not specify is inherited unchanged.
    /// </summary>
    /// <param name="attributes">The element's raw presentation attribute values.</param>
    /// <returns>The merged context.</returns>
    /// <exception cref="SvgParseException">A <c>transform</c> attribute is malformed.</exception>
    internal SvgStyleContext Merge(in SvgPresentationAttributes attributes)
    {
        Matrix2D transform = attributes.Transform is null
            ? Transform
            : Transform.Multiply(SvgTransformParser.Parse(attributes.Transform));

        // Opacity is the one property that is MULTIPLIED down the chain rather than overridden:
        // a <g opacity="0.5"> containing an opacity="0.4" shape yields 0.2.
        double opacity = Opacity;
        if (SvgValueParser.TryParseOpacity(attributes.Opacity, out double own))
        {
            opacity *= own;
        }

        string? fill = TryResolvePaint(attributes.Fill, out string? fillValue) ? fillValue : Fill;
        string? stroke = TryResolvePaint(attributes.Stroke, out string? strokeValue) ? strokeValue : Stroke;

        double? strokeWidth = StrokeWidth;
        if (IsSpecified(attributes.StrokeWidth) && SvgValueParser.TryParseNumber(attributes.StrokeWidth, out double width))
        {
            strokeWidth = width;
        }

        IconLineCap? lineCap = StrokeLineCap;
        if (IsSpecified(attributes.StrokeLineCap))
        {
            lineCap = SvgValueParser.ParseLineCap(attributes.StrokeLineCap!.Trim());
        }

        IconLineJoin? lineJoin = StrokeLineJoin;
        if (IsSpecified(attributes.StrokeLineJoin))
        {
            lineJoin = SvgValueParser.ParseLineJoin(attributes.StrokeLineJoin!.Trim());
        }

        IconFillRule fillRule = FillRule;
        if (IsSpecified(attributes.FillRule))
        {
            fillRule = SvgValueParser.ParseFillRule(attributes.FillRule!.Trim());
        }

        return new SvgStyleContext(transform, opacity, fill, stroke, strokeWidth, lineCap, lineJoin, fillRule);
    }

    /// <summary>
    /// True when an attribute carries a usable value. An absent, blank or <c>inherit</c> value means
    /// "not specified", so the parent's value flows through untouched.
    /// </summary>
    private static bool IsSpecified(string? raw)
    {
        if (raw is null)
        {
            return false;
        }

        string trimmed = raw.Trim();
        return trimmed.Length != 0
            && !string.Equals(trimmed, "inherit", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Resolves a paint attribute. <c>"currentColor"</c> normalizes to <see langword="null"/> —
    /// an explicit "inherit the renderer's brush" that still overrides an ancestor's paint —
    /// while <c>"none"</c> is kept verbatim, because <see cref="IconLayer.IsFilled"/> and
    /// <see cref="IconLayer.IsStroked"/> are defined against it.
    /// </summary>
    private static bool TryResolvePaint(string? raw, out string? value)
    {
        value = null;
        if (!IsSpecified(raw))
        {
            return false;
        }

        string trimmed = raw!.Trim();
        if (string.Equals(trimmed, "currentColor", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        value = trimmed;
        return true;
    }
}

/// <summary>
/// The raw presentation attribute values read off one element, before any inheritance is applied.
/// </summary>
internal readonly struct SvgPresentationAttributes
{
    internal SvgPresentationAttributes(
        string? transform,
        string? opacity,
        string? fill,
        string? stroke,
        string? strokeWidth,
        string? strokeLineCap,
        string? strokeLineJoin,
        string? fillRule)
    {
        Transform = transform;
        Opacity = opacity;
        Fill = fill;
        Stroke = stroke;
        StrokeWidth = strokeWidth;
        StrokeLineCap = strokeLineCap;
        StrokeLineJoin = strokeLineJoin;
        FillRule = fillRule;
    }

    /// <summary>The raw <c>transform</c> attribute, or null.</summary>
    internal string? Transform { get; }

    /// <summary>The raw <c>opacity</c> attribute, or null.</summary>
    internal string? Opacity { get; }

    /// <summary>The raw <c>fill</c> attribute, or null.</summary>
    internal string? Fill { get; }

    /// <summary>The raw <c>stroke</c> attribute, or null.</summary>
    internal string? Stroke { get; }

    /// <summary>The raw <c>stroke-width</c> attribute, or null.</summary>
    internal string? StrokeWidth { get; }

    /// <summary>The raw <c>stroke-linecap</c> attribute, or null.</summary>
    internal string? StrokeLineCap { get; }

    /// <summary>The raw <c>stroke-linejoin</c> attribute, or null.</summary>
    internal string? StrokeLineJoin { get; }

    /// <summary>The raw <c>fill-rule</c> attribute, or null.</summary>
    internal string? FillRule { get; }
}
