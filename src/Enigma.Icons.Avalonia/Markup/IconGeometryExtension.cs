using System;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Enigma.Icons.Phosphor;

namespace Enigma.Icons.Avalonia.Markup;

/// <summary>
/// <c>{ei:IconGeometry Acorn, Weight=Bold}</c> — a Phosphor icon's geometry, for <c>Path.Data</c>
/// and anything else taking a <see cref="Geometry"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is evaluated once, at load time.</b> It can never follow a bound brush or a theme switch.
/// Reach for it when you want a static geometry; use <see cref="T:Enigma.Icons.Avalonia.Icon"/>
/// whenever the icon's colour is bound, themed, or inherited.
/// </para>
/// <para>
/// <b>It fails fast, unlike <see cref="T:Enigma.Icons.Avalonia.Icon"/>.</b> An unknown
/// <see cref="Icon"/>/<see cref="Weight"/> pair throws from <c>ProvideValue</c>, because a bad value
/// in XAML is an authoring error that should surface at load time. <c>Icon.Render</c> deliberately
/// does the opposite and paints nothing, to keep the previewer alive. The asymmetry is intentional.
/// </para>
/// <para>
/// Per-layer opacity is lost, as it is for <see cref="IconGlyphExtensions.ToGeometry"/> — a duotone
/// icon rendered through this extension paints both layers at full opacity. Use
/// <see cref="T:Enigma.Icons.Avalonia.Icon"/> for duotone.
/// </para>
/// </remarks>
public sealed class IconGeometryExtension : MarkupExtension
{
    /// <summary>Initializes the extension with <c>default(PhosphorIcon)</c>.</summary>
    public IconGeometryExtension()
    {
    }

    /// <summary>Initializes the extension with a positional icon, as in <c>{ei:IconGeometry Acorn}</c>.</summary>
    /// <param name="icon">The Phosphor icon to provide the geometry of.</param>
    public IconGeometryExtension(PhosphorIcon icon) => Icon = icon;

    /// <summary>The Phosphor icon to provide the geometry of.</summary>
    public PhosphorIcon Icon { get; set; }

    /// <summary>The weight to provide it in. Defaults to <see cref="PhosphorWeight.Regular"/>.</summary>
    public PhosphorWeight Weight { get; set; } = PhosphorWeight.Regular;

    /// <summary>Resolves the icon and returns its geometry.</summary>
    /// <param name="serviceProvider">The XAML service provider. Not used.</param>
    /// <returns>A <see cref="Geometry"/> for <see cref="Icon"/> in <see cref="Weight"/>.</returns>
    /// <exception cref="IconNotFoundException"><see cref="Icon"/> or <see cref="Weight"/> is not in the Phosphor set.</exception>
    public override object ProvideValue(IServiceProvider serviceProvider)
        => PhosphorIconSet.Instance.GetGlyph(Icon, Weight).ToGeometry();
}
