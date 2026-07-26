using System;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Enigma.Icons.Avalonia.Markup;
using Enigma.Icons.Phosphor;
using Xunit;

namespace Enigma.Icons.Avalonia.UnitTests;

/// <summary>SPEC §12.3, the markup-extension bullet.</summary>
/// <remarks>
/// <c>ProvideValue</c>'s <c>serviceProvider</c> is unused by both extensions, so the tests pass null
/// deliberately rather than standing up a XAML service provider.
/// </remarks>
public sealed class MarkupExtensionTests
{
    [AvaloniaFact]
    public void IconGeometry_ProvideValue_ReturnsAGeometry()
    {
        var extension = new IconGeometryExtension(PhosphorIcon.Acorn);

        Assert.IsAssignableFrom<Geometry>(extension.ProvideValue(null!));
    }

    [AvaloniaFact]
    public void IconGeometry_PositionalConstructor_SetsIcon()
    {
        var extension = new IconGeometryExtension(PhosphorIcon.AddressBook);

        Assert.Equal(PhosphorIcon.AddressBook, extension.Icon);
    }

    [AvaloniaFact]
    public void IconGeometry_WeightDefaultsToRegular()
        => Assert.Equal(PhosphorWeight.Regular, new IconGeometryExtension().Weight);

    [AvaloniaFact]
    public void IconGeometry_HonoursWeight()
    {
        var thin = new IconGeometryExtension(PhosphorIcon.Acorn) { Weight = PhosphorWeight.Thin };
        var duotone = new IconGeometryExtension(PhosphorIcon.Acorn) { Weight = PhosphorWeight.Duotone };

        Assert.IsNotType<GeometryGroup>(thin.ProvideValue(null!));

        // Duotone is the two-layer weight, so it is the one that must come back as a group.
        Assert.IsType<GeometryGroup>(duotone.ProvideValue(null!));
    }

    [AvaloniaFact]
    public void IconImage_ProvideValue_ReturnsADrawingImage()
    {
        var extension = new IconImageExtension(PhosphorIcon.Acorn);

        Assert.IsType<DrawingImage>(extension.ProvideValue(null!));
    }

    [AvaloniaFact]
    public void IconImage_PositionalConstructor_SetsIcon()
    {
        var extension = new IconImageExtension(PhosphorIcon.AddressBook);

        Assert.Equal(PhosphorIcon.AddressBook, extension.Icon);
    }

    [AvaloniaFact]
    public void IconImage_WeightDefaultsToRegular()
        => Assert.Equal(PhosphorWeight.Regular, new IconImageExtension().Weight);

    [AvaloniaFact]
    public void IconImage_BrushDefaultsToBlack()
        => Assert.Same(Brushes.Black, new IconImageExtension().Brush);

    [AvaloniaFact]
    public void IconImage_PaintsWithTheSuppliedBrush()
    {
        var extension = new IconImageExtension(PhosphorIcon.Acorn) { Brush = Brushes.Red };

        var image = Assert.IsType<DrawingImage>(extension.ProvideValue(null!));
        var root = Assert.IsType<DrawingGroup>(image.Drawing);
        var drawing = Assert.IsType<GeometryDrawing>(Assert.Single(root.Children));

        Assert.Same(Brushes.Red, drawing.Brush);
    }

    [AvaloniaFact]
    public void MarkupExtensions_FailFastOnAnUnknownIcon()
    {
        // The deliberate asymmetry with Icon.Render, which paints nothing: a bad value in XAML is an
        // authoring error and should surface at load time.
        var geometry = new IconGeometryExtension((PhosphorIcon)999999);
        var image = new IconImageExtension((PhosphorIcon)999999);

        Assert.Throws<ArgumentOutOfRangeException>(() => geometry.ProvideValue(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => image.ProvideValue(null!));
    }
}
