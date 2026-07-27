using System;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Enigma.Icons.Phosphor;

namespace Enigma.Icons.Avalonia;

/// <summary>
/// Draws an icon. Built-in Phosphor artwork via <see cref="Kind"/> and <see cref="Weight"/>, or any
/// other <see cref="IIconSet"/> via <see cref="IconSet"/>, <see cref="IconName"/> and
/// <see cref="Variant"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>No <c>StyleInclude</c> is required.</b> This is a plain <see cref="Control"/> that overrides
/// <see cref="MeasureOverride"/> and <see cref="Render"/> — not a <see cref="TemplatedControl"/>.
/// The package therefore ships no XAML and declares no theme resources, so nothing goes into
/// <c>App.axaml</c> and there is no resource key to get wrong.
/// </para>
/// <para>
/// <b><see cref="Foreground"/> inherits, and that is the point.</b> It is registered with
/// <c>TextElement.ForegroundProperty.AddOwner&lt;Icon&gt;()</c>, and
/// <c>TextElement.Foreground</c> is an inheriting property — so an <see cref="Icon"/> inside a
/// <c>Button</c>, <c>MenuItem</c> or any other text scope picks up that scope's foreground and
/// follows a theme switch with <i>no binding written by the consumer</i>. This is exactly what a
/// markup extension cannot do: it is evaluated once at load time and can never follow a bound brush.
/// When <see cref="Foreground"/> resolves to null the control paints nothing — it does not fall back
/// to black.
/// </para>
/// <para>
/// <b><see cref="Render"/> never throws.</b> A missing glyph, an unresolvable name, or a third-party
/// icon set that misbehaves all paint nothing. A throwing render pass takes down the Avalonia XAML
/// previewer for the whole window, not just the icon.
/// </para>
/// </remarks>
public sealed class Icon : Control
{
    /// <summary>Identifies the <see cref="Kind"/> property.</summary>
    public static readonly StyledProperty<PhosphorIcon> KindProperty =
        AvaloniaProperty.Register<Icon, PhosphorIcon>(nameof(Kind));

    /// <summary>Identifies the <see cref="Weight"/> property.</summary>
    public static readonly StyledProperty<PhosphorWeight> WeightProperty =
        AvaloniaProperty.Register<Icon, PhosphorWeight>(nameof(Weight), PhosphorWeight.Regular);

    /// <summary>Identifies the <see cref="IconSet"/> property.</summary>
    public static readonly StyledProperty<IIconSet?> IconSetProperty =
        AvaloniaProperty.Register<Icon, IIconSet?>(nameof(IconSet));

    /// <summary>Identifies the <see cref="IconName"/> property.</summary>
    public static readonly StyledProperty<string?> IconNameProperty =
        AvaloniaProperty.Register<Icon, string?>(nameof(IconName));

    /// <summary>Identifies the <see cref="Variant"/> property.</summary>
    public static readonly StyledProperty<string?> VariantProperty =
        AvaloniaProperty.Register<Icon, string?>(nameof(Variant));

    /// <summary>
    /// Identifies the <see cref="Foreground"/> property — <c>TextElement.ForegroundProperty</c>
    /// re-owned, so the value inherits from the enclosing text scope.
    /// </summary>
    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner<Icon>();

    /// <summary>Identifies the <see cref="Size"/> property.</summary>
    public static readonly StyledProperty<double> SizeProperty =
        AvaloniaProperty.Register<Icon, double>(nameof(Size), 16.0);

    /// <summary>Identifies the <see cref="Stretch"/> property.</summary>
    public static readonly StyledProperty<Stretch> StretchProperty =
        AvaloniaProperty.Register<Icon, Stretch>(nameof(Stretch), Stretch.Uniform);

    static Icon()
    {
        // Every registration for Icon lives here, in Icon's own static state. Field initializers —
        // including ForegroundProperty's AddOwner — have already run by the time this body executes.
        // Registering an Icon property from some other type's static initializer would make
        // correctness depend on which type happens to be touched first.
        AffectsRender<Icon>(
            KindProperty,
            WeightProperty,
            IconSetProperty,
            IconNameProperty,
            VariantProperty,
            ForegroundProperty,
            SizeProperty,
            StretchProperty);

        AffectsMeasure<Icon>(SizeProperty, StretchProperty);

        FocusableProperty.OverrideDefaultValue<Icon>(false);
    }

    /// <summary>
    /// The built-in Phosphor icon to draw. Ignored when <see cref="IconSet"/> is set.
    /// </summary>
    /// <remarks>
    /// The default is <c>default(PhosphorIcon)</c>, i.e. enum value 0 — so a bare <c>&lt;ei:Icon/&gt;</c>
    /// draws an acorn. That is the unavoidable consequence of a value-type default, not an oversight.
    /// </remarks>
    public PhosphorIcon Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    /// <summary>The Phosphor weight to draw <see cref="Kind"/> in. Defaults to
    /// <see cref="PhosphorWeight.Regular"/>.</summary>
    public PhosphorWeight Weight
    {
        get => GetValue(WeightProperty);
        set => SetValue(WeightProperty, value);
    }

    /// <summary>
    /// A custom icon set to resolve <see cref="IconName"/> against. Defaults to null; when it is
    /// non-null it <b>wins over</b> <see cref="Kind"/> and <see cref="Weight"/>.
    /// </summary>
    public IIconSet? IconSet
    {
        get => GetValue(IconSetProperty);
        set => SetValue(IconSetProperty, value);
    }

    /// <summary>The icon name to resolve against <see cref="IconSet"/>. Defaults to null.</summary>
    public string? IconName
    {
        get => GetValue(IconNameProperty);
        set => SetValue(IconNameProperty, value);
    }

    /// <summary>The variant of <see cref="IconName"/> to resolve, or null for the set's default
    /// variant. Defaults to null.</summary>
    public string? Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    /// <summary>
    /// The brush every layer paints with. Inherited from the enclosing <c>TextElement</c> scope when
    /// not set locally; when it resolves to null the control paints nothing.
    /// </summary>
    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    /// <summary>
    /// The measured size on both axes, in device-independent pixels. Defaults to 16.
    /// </summary>
    /// <remarks>
    /// An explicit <see cref="Layoutable.Width"/> or <see cref="Layoutable.Height"/> wins over this
    /// value. That is not implemented here and must not be: <c>Layoutable.MeasureCore</c> already
    /// coerces the measured result with the explicit <c>Width</c>/<c>Height</c>/<c>Min*</c>/<c>Max*</c>
    /// values, so a second precedence rule in <see cref="MeasureOverride"/> would only conflict with
    /// the framework's.
    /// </remarks>
    public double Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>How the glyph's view box is scaled into the control's bounds. Defaults to
    /// <see cref="Stretch.Uniform"/>.</summary>
    public Stretch Stretch
    {
        get => GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    /// <summary>Measures the control as <see cref="Size"/> square.</summary>
    /// <param name="availableSize">The space offered by the parent. Not used — an icon's size is its own.</param>
    /// <returns>A square of <see cref="Size"/>, or an empty size when <see cref="Size"/> is not a usable number.</returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        double size = Size;

        // Negated range test so NaN falls through to 0 rather than being admitted.
        if (!(size > 0) || double.IsInfinity(size))
        {
            return default;
        }

        return new Size(size, size);
    }

    /// <summary>Draws the resolved glyph, scaled from its view box into the control's bounds.</summary>
    /// <param name="context">The drawing context to paint into.</param>
    /// <remarks>
    /// Public, not protected: <c>Visual.Render</c> is declared public in Avalonia 12 and an override
    /// may not narrow it. SPEC §10.2's signature table predates that change.
    /// </remarks>
    public override void Render(DrawingContext context)
    {
        // Deliberately swallowing everything (SPEC §10.2, §15): an IIconSet is arbitrary third-party
        // code, Geometry.Parse on third-party path data can fail, and a throwing Render kills the
        // XAML previewer's surface for the whole window rather than just this icon. No logging —
        // observability is deliberately none in this solution.
        try
        {
            IBrush? foreground = Foreground;
            if (foreground is null)
            {
                return;
            }

            IconGlyph? glyph = ResolveGlyph();
            if (glyph is null)
            {
                return;
            }

            var bounds = new Rect(Bounds.Size);
            if (!(bounds.Width > 0) || !(bounds.Height > 0) ||
                double.IsInfinity(bounds.Width) || double.IsInfinity(bounds.Height))
            {
                return;
            }

            IconViewBox viewBox = glyph.ViewBox;
            double sx = bounds.Width / viewBox.Width;
            double sy = bounds.Height / viewBox.Height;

            Stretch stretch = Stretch;
            double scaleX;
            double scaleY;
            switch (stretch)
            {
                case Stretch.None:
                    scaleX = 1.0;
                    scaleY = 1.0;
                    break;
                case Stretch.Fill:
                    scaleX = sx;
                    scaleY = sy;
                    break;
                case Stretch.UniformToFill:
                    scaleX = Math.Max(sx, sy);
                    scaleY = scaleX;
                    break;
                default:
                    scaleX = Math.Min(sx, sy);
                    scaleY = scaleX;
                    break;
            }

            if (!IsUsableScale(scaleX) || !IsUsableScale(scaleY))
            {
                return;
            }

            // Centre the scaled content in the bounds, then subtract the scaled view-box origin so a
            // view box that does not start at 0,0 still lands where it should.
            double tx = (-viewBox.X * scaleX) + ((bounds.Width - (viewBox.Width * scaleX)) / 2);
            double ty = (-viewBox.Y * scaleY) + ((bounds.Height - (viewBox.Height * scaleY)) / 2);
            Matrix transform = Matrix.CreateScale(scaleX, scaleY) * Matrix.CreateTranslation(tx, ty);

            if (stretch == Stretch.UniformToFill)
            {
                // UniformToFill is the only mode whose content can exceed the bounds; clip it, as
                // Image does, so an overflowing icon cannot paint over its neighbours.
                using (context.PushClip(bounds))
                {
                    DrawLayers(context, glyph, foreground, transform);
                }
            }
            else
            {
                DrawLayers(context, glyph, foreground, transform);
            }
        }
        catch (Exception)
        {
            // Intentionally silent — see the comment above.
        }
    }

    /// <summary>Creates the automation peer reporting this control as an image.</summary>
    /// <returns>A peer that stays out of the content view until the consumer names the icon.</returns>
    protected override AutomationPeer OnCreateAutomationPeer() => new IconAutomationPeer(this);

    private static void DrawLayers(DrawingContext context, IconGlyph glyph, IBrush brush, Matrix transform)
    {
        using (context.PushTransform(transform))
        {
            for (int i = 0; i < glyph.Layers.Count; i++)
            {
                IconLayer layer = glyph.Layers[i];

                // The SAME helper ToDrawing uses, so the control and the extension methods cannot
                // drift apart on what a layer paints.
                if (!IconGlyphExtensions.TryGetLayerPaint(layer, brush, out IBrush? fill, out IPen? pen))
                {
                    continue;
                }

                // No geometry cache: glyph instances are already cached and reference-equal, and a
                // cache keyed by path string would only add an invalidation surface.
                Geometry geometry = Geometry.Parse(layer.PathData);

                if (layer.Opacity < 1.0)
                {
                    using (context.PushOpacity(layer.Opacity))
                    {
                        context.DrawGeometry(fill, pen, geometry);
                    }
                }
                else
                {
                    context.DrawGeometry(fill, pen, geometry);
                }
            }
        }
    }

    private static bool IsUsableScale(double scale)
        => scale > 0 && !double.IsInfinity(scale);

    private IconGlyph? ResolveGlyph()
    {
        IIconSet? set = IconSet;
        IconGlyph? glyph;

        if (set is not null)
        {
            // IconSet wins over Kind/Weight (SPEC §10.2), but only with a name to resolve.
            string? name = IconName;
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            return set.TryGetGlyph(name, Variant, out glyph) ? glyph : null;
        }

        return PhosphorIconSet.Instance.TryGetGlyph(Kind, Weight, out glyph) ? glyph : null;
    }

    /// <summary>
    /// Reports the icon as an image, and as decoration a screen reader skips until the consumer gives
    /// it an <c>AutomationProperties.Name</c>.
    /// </summary>
    private sealed class IconAutomationPeer : ControlAutomationPeer
    {
        public IconAutomationPeer(Control owner)
            : base(owner)
        {
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
            => AutomationControlType.Image;

        protected override bool IsContentElementCore()
            => !string.IsNullOrEmpty(AutomationProperties.GetName(Owner));
    }
}
