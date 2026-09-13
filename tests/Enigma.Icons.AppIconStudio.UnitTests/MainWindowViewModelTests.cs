using System;
using System.Linq;
using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Enigma.Icons.AppIconStudio.Design;
using Enigma.Icons.AppIconStudio.Export;
using Enigma.Icons.AppIconStudio.Rendering;
using Enigma.Icons.AppIconStudio.UnitTests.TestSupport;
using Enigma.Icons.Phosphor;
using Xunit;

namespace Enigma.Icons.AppIconStudio.UnitTests;

/// <summary>
/// The ViewModel's synchronous behaviour — the catalog filter, the selection rules, and the state the
/// constructor leaves behind.
/// </summary>
/// <remarks>
/// <c>[AvaloniaFact]</c> throughout: the ViewModel owns a <c>DispatcherTimer</c> and renders its first
/// preview in the constructor, so it needs both a dispatcher and the Skia backend. Re-renders are
/// debounced through that timer and are deliberately not asserted here — a dispatcher timer cannot be
/// advanced from a test without sleeping, and what the debounce coalesces is already covered by the
/// rasterizer's own tests.
/// </remarks>
public sealed class MainWindowViewModelTests
{
    [AvaloniaFact]
    public void Constructor_StartsFromTheReferenceDesign()
    {
        MainWindowViewModel vm = Create();

        Assert.Equal("markdown-logo", vm.ActiveIcon.Name);
        Assert.Equal(PhosphorWeight.Fill, vm.SelectedWeight.Weight);
        Assert.Equal(PlateFillMode.Solid, vm.SelectedFillMode.Mode);
        Assert.Equal(Color.FromRgb(0x3B, 0x72, 0xF0), vm.PrimaryColor);
        Assert.Equal(Colors.White, vm.GlyphColor);
        Assert.Equal(IconDesign.DefaultCornerRadiusRatio, vm.CurrentDesign.CornerRadiusRatio);
        Assert.Equal(IconDesign.DefaultGlyphScale, vm.CurrentDesign.GlyphScale);
    }

    [AvaloniaFact]
    public void Constructor_RendersThePreviewAndTheThumbnailStrip()
    {
        MainWindowViewModel vm = Create();

        Assert.NotNull(vm.PreviewImage);
        Assert.Equal(
            new PixelSize(MainWindowViewModel.PreviewSizePx, MainWindowViewModel.PreviewSizePx),
            vm.PreviewImage!.PixelSize);

        Assert.Equal([64, 48, 32, 16], vm.Thumbnails.Select(t => t.SizePx));

        foreach (IconThumbnail thumbnail in vm.Thumbnails)
        {
            Assert.Equal(new PixelSize(thumbnail.SizePx, thumbnail.SizePx), thumbnail.Image.PixelSize);
        }
    }

    [AvaloniaFact]
    public void Constructor_ListsTheWholeCatalogAndDescribesTheDesign()
    {
        MainWindowViewModel vm = Create();

        Assert.Equal(PhosphorIconNames.All.Count, vm.FilteredIcons.Count);
        Assert.True(vm.HasResults);
        Assert.Contains("markdown-logo", vm.StatusText, StringComparison.Ordinal);
        Assert.Contains("Fill", vm.StatusText, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void SearchText_FiltersTheCatalogCaseInsensitively()
    {
        MainWindowViewModel vm = Create();

        vm.SearchText = "MARKDOWN";

        // NotEmpty first: Assert.All passes vacuously on an empty sequence, and this solution is on
        // xunit.v3 3.2.2, which has no `throwIfEmpty` overload yet.
        Assert.NotEmpty(vm.FilteredIcons);
        Assert.All(
            vm.FilteredIcons,
            entry => Assert.Contains("markdown", entry.Name, StringComparison.OrdinalIgnoreCase));
    }

    [AvaloniaFact]
    public void SearchText_ReportsAnEmptyResult()
    {
        MainWindowViewModel vm = Create();

        vm.SearchText = "no-such-icon-anywhere";

        Assert.Empty(vm.FilteredIcons);
        Assert.False(vm.HasResults);
        Assert.Contains("no-such-icon-anywhere", vm.EmptyMessage, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void AFilterThatHidesTheChosenIconDoesNotChangeTheDesign()
    {
        MainWindowViewModel vm = Create();
        IconEntry chosen = vm.ActiveIcon;

        vm.SearchText = "acorn";

        // The list has no row to highlight, but the design keeps the icon the user picked.
        Assert.Null(vm.SelectedIcon);
        Assert.Same(chosen, vm.ActiveIcon);
    }

    [AvaloniaFact]
    public void ClearingTheSearchRestoresTheHighlight()
    {
        MainWindowViewModel vm = Create();
        IconEntry chosen = vm.ActiveIcon;

        vm.SearchText = "acorn";
        vm.SearchText = string.Empty;

        Assert.Same(chosen, vm.SelectedIcon);
        Assert.Same(chosen, vm.ActiveIcon);
    }

    [AvaloniaFact]
    public void SelectingARowChangesTheActiveIcon()
    {
        MainWindowViewModel vm = Create();
        IconEntry rocket = vm.FilteredIcons.First(e => e.Name == "rocket");

        vm.SelectedIcon = rocket;

        Assert.Same(rocket, vm.ActiveIcon);
    }

    [AvaloniaFact]
    public void ANullSelectionIsIgnoredByTheDesign()
    {
        MainWindowViewModel vm = Create();
        IconEntry chosen = vm.ActiveIcon;

        vm.SelectedIcon = null;

        Assert.Null(vm.SelectedIcon);
        Assert.Same(chosen, vm.ActiveIcon);
    }

    [AvaloniaFact]
    public void IsGradient_FollowsTheFillMode()
    {
        MainWindowViewModel vm = Create();

        Assert.False(vm.IsGradient);

        vm.SelectedFillMode = vm.FillModes.First(m => m.Mode == PlateFillMode.LinearGradient);

        Assert.True(vm.IsGradient);
    }

    [AvaloniaFact]
    public void Weights_CoverAllSixInDeclarationOrder()
    {
        MainWindowViewModel vm = Create();

        Assert.Equal(
            [
                PhosphorWeight.Thin,
                PhosphorWeight.Light,
                PhosphorWeight.Regular,
                PhosphorWeight.Bold,
                PhosphorWeight.Fill,
                PhosphorWeight.Duotone,
            ],
            vm.Weights.Select(w => w.Weight));
    }

    [AvaloniaFact]
    public void Constructor_RequiresEveryDependency()
    {
        var rasterizer = new AvaloniaIconRasterizer();
        var exporter = new IconExporter(rasterizer);
        var picker = new FakeFolderPicker();

        // null! is the point of these: they force the nulls a nullable-aware caller cannot pass, so
        // the runtime guards are exercised rather than only the compiler's (SPEC §2.3).
        Assert.Throws<ArgumentNullException>(() => new MainWindowViewModel(null!, exporter, picker));
        Assert.Throws<ArgumentNullException>(() => new MainWindowViewModel(rasterizer, null!, picker));
        Assert.Throws<ArgumentNullException>(() => new MainWindowViewModel(rasterizer, exporter, null!));
    }

    private static MainWindowViewModel Create()
    {
        var rasterizer = new AvaloniaIconRasterizer();

        return new MainWindowViewModel(rasterizer, new IconExporter(rasterizer), new FakeFolderPicker());
    }
}
