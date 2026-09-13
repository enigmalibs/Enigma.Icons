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
/// The Reset command: what it restores, what it deliberately leaves alone, and that it does so
/// without waiting for the preview debounce.
/// </summary>
/// <remarks>
/// <c>[AvaloniaFact]</c> throughout, for the same reason as <see cref="MainWindowViewModelTests"/>:
/// the ViewModel owns a <c>DispatcherTimer</c> and rasterizes through Skia. Every assertion here runs
/// straight after the call — <c>Reset</c> stops the debounce and renders synchronously, which is
/// exactly what makes it testable without advancing a timer.
/// </remarks>
public sealed class ResetTests
{
    [AvaloniaFact]
    public void Reset_RestoresEveryDesignValue()
    {
        MainWindowViewModel vm = Create();

        vm.SelectedIcon = vm.FilteredIcons.First(e => e.Name == "rocket");
        vm.SelectedWeight = vm.Weights.First(w => w.Weight == PhosphorWeight.Thin);
        vm.SelectedFillMode = vm.FillModes.First(m => m.Mode == PlateFillMode.LinearGradient);
        vm.PrimaryColor = Colors.Crimson;
        vm.SecondaryColor = Colors.Goldenrod;
        vm.GlyphColor = Colors.Black;
        vm.GradientAngle = 210;
        vm.CornerRadiusRatio = 0.5;
        vm.GlyphScale = 0.95;

        vm.Reset();

        Assert.Equal(StudioDefaults.Icon, vm.ActiveIcon.Kind);
        Assert.Equal(StudioDefaults.Weight, vm.SelectedWeight.Weight);
        Assert.Equal(StudioDefaults.FillMode, vm.SelectedFillMode.Mode);
        Assert.Equal(StudioDefaults.PrimaryColor, vm.PrimaryColor);
        Assert.Equal(StudioDefaults.SecondaryColor, vm.SecondaryColor);
        Assert.Equal(StudioDefaults.GlyphColor, vm.GlyphColor);
        Assert.Equal(StudioDefaults.GradientAngleDegrees, vm.GradientAngle);
        Assert.Equal(StudioDefaults.CornerRadiusRatio, vm.CornerRadiusRatio);
        Assert.Equal(StudioDefaults.GlyphScale, vm.GlyphScale);
        Assert.False(vm.IsGradient);
    }

    [AvaloniaFact]
    public void Reset_RebuildsTheDesignAndThePreviewWithoutWaitingForTheDebounce()
    {
        MainWindowViewModel vm = Create();

        vm.GlyphScale = 0.95;
        vm.SelectedFillMode = vm.FillModes.First(m => m.Mode == PlateFillMode.LinearGradient);

        // The debounce is still pending here, which is the whole point: CurrentDesign has not caught
        // up with the controls yet.
        Assert.NotEqual(0.95, vm.CurrentDesign.GlyphScale);

        vm.Reset();

        Assert.Equal(StudioDefaults.GlyphScale, vm.CurrentDesign.GlyphScale);
        Assert.Equal(StudioDefaults.CornerRadiusRatio, vm.CurrentDesign.CornerRadiusRatio);
        Assert.Equal(PlateFillMode.Solid, vm.CurrentDesign.Plate.Mode);
        Assert.Equal(StudioDefaults.Icon, vm.CurrentDesign.Icon);

        Assert.NotNull(vm.PreviewImage);
        Assert.Equal(
            new PixelSize(MainWindowViewModel.PreviewSizePx, MainWindowViewModel.PreviewSizePx),
            vm.PreviewImage!.PixelSize);
        Assert.Equal([64, 48, 32, 16], vm.Thumbnails.Select(t => t.SizePx));
    }

    [AvaloniaFact]
    public void Reset_DescribesTheRestoredDesignInTheStatusLine()
    {
        MainWindowViewModel vm = Create();

        vm.SelectedIcon = vm.FilteredIcons.First(e => e.Name == "rocket");
        vm.SelectedWeight = vm.Weights.First(w => w.Weight == PhosphorWeight.Thin);

        vm.Reset();

        Assert.Contains("markdown-logo", vm.StatusText, StringComparison.Ordinal);
        Assert.Contains("Fill", vm.StatusText, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void Reset_ClearsTheSearchAndRestoresTheHighlight()
    {
        MainWindowViewModel vm = Create();

        vm.SelectedIcon = vm.FilteredIcons.First(e => e.Name == "rocket");
        vm.SearchText = "acorn";

        // The filter has hidden the chosen row, so the list highlights nothing.
        Assert.Null(vm.SelectedIcon);

        vm.Reset();

        Assert.Equal(string.Empty, vm.SearchText);
        Assert.Equal(PhosphorIconNames.All.Count, vm.FilteredIcons.Count);
        Assert.True(vm.HasResults);
        Assert.NotNull(vm.SelectedIcon);
        Assert.Same(vm.ActiveIcon, vm.SelectedIcon);
    }

    [AvaloniaFact]
    public void Reset_RestoresTheHighlightEvenWhenTheSearchBoxIsAlreadyEmpty()
    {
        MainWindowViewModel vm = Create();

        // No filter in play: the SearchText setter short-circuits, so the highlight can only come
        // back from Reset's own unconditional re-filter.
        vm.SelectedIcon = vm.FilteredIcons.First(e => e.Name == "rocket");

        vm.Reset();

        Assert.NotNull(vm.SelectedIcon);
        Assert.Equal(StudioDefaults.Icon, vm.SelectedIcon!.Kind);
        Assert.Same(vm.ActiveIcon, vm.SelectedIcon);
    }

    [AvaloniaFact]
    public void Reset_RestoresTheBaseNameAndBothSizeSelections()
    {
        MainWindowViewModel vm = Create();

        vm.BaseName = "enigma-markdown-editor";

        // One default size unticked, and one non-default ticked, so a reset that only re-ticked
        // everything would fail this.
        vm.IcoSizes.First(s => s.SizePx == 32).IsSelected = false;
        vm.PngSizes.First(s => s.SizePx == 16).IsSelected = true;
        vm.PngSizes.First(s => s.SizePx == 512).IsSelected = false;

        vm.Reset();

        Assert.Equal(StudioDefaults.BaseName, vm.BaseName);
        Assert.All(vm.IcoSizes, size => Assert.True(size.IsSelected));
        Assert.Equal([256, 512, 1024], vm.PngSizes.Where(s => s.IsSelected).Select(s => s.SizePx));

        // The invariant behind both assertions above, stated directly.
        Assert.All(vm.IcoSizes, size => Assert.Equal(size.IsSelectedByDefault, size.IsSelected));
        Assert.All(vm.PngSizes, size => Assert.Equal(size.IsSelectedByDefault, size.IsSelected));
    }

    [AvaloniaFact]
    public void Reset_KeepsTheOutputFolder()
    {
        using var temp = new TempDirectory();
        MainWindowViewModel vm = Create();
        vm.OutputDirectory = temp.Path;

        vm.Reset();

        Assert.Equal(temp.Path, vm.OutputDirectory);
    }

    [AvaloniaFact]
    public void Reset_ReEnablesGenerateWhenTheBlockerWasTheBaseNameOrTheSizes()
    {
        using var temp = new TempDirectory();
        MainWindowViewModel vm = Create();
        vm.OutputDirectory = temp.Path;

        vm.BaseName = "sub/app";

        foreach (SizeOption size in vm.IcoSizes.Concat(vm.PngSizes))
        {
            size.IsSelected = false;
        }

        Assert.False(vm.GenerateCommand.CanExecute(null));

        vm.Reset();

        Assert.True(vm.GenerateCommand.CanExecute(null));
        Assert.Equal(string.Empty, vm.GenerateHint);
    }

    [AvaloniaFact]
    public void Reset_LeavesGenerateBlockedWhenNoFolderIsChosen()
    {
        MainWindowViewModel vm = Create();

        vm.Reset();

        // The one blocker a reset cannot clear, because it deliberately does not touch the folder.
        Assert.False(vm.GenerateCommand.CanExecute(null));
        Assert.Equal("Choose an output folder.", vm.GenerateHint);
    }

    [AvaloniaFact]
    public void Reset_OnAFreshViewModelChangesNothing()
    {
        MainWindowViewModel vm = Create();

        vm.Reset();

        Assert.Equal(StudioDefaults.Icon, vm.ActiveIcon.Kind);
        Assert.Equal(StudioDefaults.Weight, vm.SelectedWeight.Weight);
        Assert.Equal(StudioDefaults.FillMode, vm.SelectedFillMode.Mode);
        Assert.Equal(StudioDefaults.PrimaryColor, vm.PrimaryColor);
        Assert.Equal(StudioDefaults.SecondaryColor, vm.SecondaryColor);
        Assert.Equal(StudioDefaults.GlyphColor, vm.GlyphColor);
        Assert.Equal(StudioDefaults.GradientAngleDegrees, vm.GradientAngle);
        Assert.Equal(StudioDefaults.CornerRadiusRatio, vm.CornerRadiusRatio);
        Assert.Equal(StudioDefaults.GlyphScale, vm.GlyphScale);
        Assert.Equal(StudioDefaults.BaseName, vm.BaseName);
        Assert.Equal(string.Empty, vm.SearchText);
        Assert.Equal(string.Empty, vm.OutputDirectory);
        Assert.Equal(PhosphorIconNames.All.Count, vm.FilteredIcons.Count);
    }

    [AvaloniaFact]
    public void ResetCommand_RunsResetAndIsAlwaysExecutable()
    {
        MainWindowViewModel vm = Create();

        vm.GlyphScale = 0.95;

        Assert.True(vm.ResetCommand.CanExecute(null));

        vm.ResetCommand.Execute(null);

        Assert.Equal(StudioDefaults.GlyphScale, vm.GlyphScale);
        Assert.True(vm.ResetCommand.CanExecute(null));
    }

    [AvaloniaFact]
    public void StudioDefaults_AreTheValuesTheWindowOpensWith()
    {
        MainWindowViewModel vm = Create();

        // The constructor and Reset must read the same source. This is the assertion that fails if
        // one of them ever grows a literal of its own.
        Assert.Equal(StudioDefaults.Icon, vm.ActiveIcon.Kind);
        Assert.Equal(StudioDefaults.Weight, vm.SelectedWeight.Weight);
        Assert.Equal(StudioDefaults.FillMode, vm.SelectedFillMode.Mode);
        Assert.Equal(StudioDefaults.CornerRadiusRatio, vm.CurrentDesign.CornerRadiusRatio);
        Assert.Equal(StudioDefaults.GlyphScale, vm.CurrentDesign.GlyphScale);
        Assert.Equal(StudioDefaults.BaseName, vm.BaseName);

        // Forwarded, not copied: the design model stays the authority on its own ranges.
        Assert.Equal(IconDesign.DefaultCornerRadiusRatio, StudioDefaults.CornerRadiusRatio);
        Assert.Equal(IconDesign.DefaultGlyphScale, StudioDefaults.GlyphScale);
        Assert.Equal(PlateFill.DefaultAngleDegrees, StudioDefaults.GradientAngleDegrees);
    }

    private static MainWindowViewModel Create()
    {
        var rasterizer = new AvaloniaIconRasterizer();

        return new MainWindowViewModel(rasterizer, new IconExporter(rasterizer), new FakeFolderPicker());
    }
}
