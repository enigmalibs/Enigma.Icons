using System;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Enigma.Icons.Phosphor;

namespace Enigma.Icons.Avalonia.Markup;

/// <summary>
/// <c>{ei:IconImage Acorn, Weight=Fill, Brush=Red}</c> — a Phosphor icon as a
/// <see cref="DrawingImage"/>, for <c>Image.Source</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why <c>IconImage</c> and not <c>IconSource</c>.</b> It returns a <see cref="DrawingImage"/>,
/// and Avalonia has its own, unrelated <c>IconSource</c> concept. The retired
/// <c>PhosphorIconsAvalonia</c> package called this <c>IconSourceExtension</c> and actively misled
/// people into expecting an <c>IconSource</c>; the rename is the most visible break from that API and
/// is deliberate.
/// </para>
/// <para>
/// <b>This is evaluated once, at load time.</b> Its <see cref="Brush"/> is a fixed value that can
/// never follow a theme switch. Use <see cref="T:Enigma.Icons.Avalonia.Icon"/> whenever the colour is
/// bound, themed, or inherited.
/// </para>
/// <para>
/// <b>It fails fast, unlike <see cref="T:Enigma.Icons.Avalonia.Icon"/>.</b> An unknown
/// <see cref="Icon"/>/<see cref="Weight"/> pair throws from <c>ProvideValue</c>, because a bad value
/// in XAML is an authoring error that should surface at load time; <c>Icon.Render</c> deliberately
/// paints nothing instead, to keep the previewer alive.
/// </para>
/// </remarks>
public sealed class IconImageExtension : MarkupExtension
{
    /// <summary>Initializes the extension with <c>default(PhosphorIcon)</c>.</summary>
    public IconImageExtension()
    {
    }

    /// <summary>Initializes the extension with a positional icon, as in <c>{ei:IconImage Acorn}</c>.</summary>
    /// <param name="icon">The Phosphor icon to provide an image of.</param>
    public IconImageExtension(PhosphorIcon icon) => Icon = icon;

    /// <summary>The Phosphor icon to provide an image of.</summary>
    public PhosphorIcon Icon { get; set; }

    /// <summary>The weight to provide it in. Defaults to <see cref="PhosphorWeight.Regular"/>.</summary>
    public PhosphorWeight Weight { get; set; } = PhosphorWeight.Regular;

    /// <summary>
    /// The brush the icon paints with. Defaults to <see cref="Brushes.Black"/> — deliberately
    /// preserving the retired package's behaviour.
    /// </summary>
    public IBrush Brush { get; set; } = Brushes.Black;

    /// <summary>Resolves the icon and returns it as a drawing image.</summary>
    /// <param name="serviceProvider">The XAML service provider. Not used.</param>
    /// <returns>A <see cref="DrawingImage"/> for <see cref="Icon"/> in <see cref="Weight"/>, painted with <see cref="Brush"/>.</returns>
    /// <exception cref="IconNotFoundException"><see cref="Icon"/> or <see cref="Weight"/> is not in the Phosphor set.</exception>
    public override object ProvideValue(IServiceProvider serviceProvider)
        => PhosphorIconSet.Instance.GetGlyph(Icon, Weight).ToDrawingImage(Brush);
}
