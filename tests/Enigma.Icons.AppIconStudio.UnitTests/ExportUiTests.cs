using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using Enigma.Icons.AppIconStudio.Design;
using Enigma.Icons.AppIconStudio.Export;
using Enigma.Icons.AppIconStudio.Rendering;
using Enigma.Icons.AppIconStudio.UnitTests.TestSupport;
using Enigma.Icons.Phosphor;
using Xunit;

namespace Enigma.Icons.AppIconStudio.UnitTests;

/// <summary>
/// The output panel: what the size lists offer, what request the ViewModel builds from them, and when
/// the Generate button is allowed to run.
/// </summary>
public sealed class ExportUiTests
{
    [AvaloniaFact]
    public void IcoSizes_OfferWindowsRecommendedSetAllTicked()
    {
        MainWindowViewModel vm = Create(out _, out _);

        Assert.Equal([16, 24, 32, 48, 64, 128, 256], vm.IcoSizes.Select(s => s.SizePx));
        Assert.All(vm.IcoSizes, size => Assert.True(size.IsSelected));

        // 256 is the ICO format's ceiling — the directory stores the edge in one byte.
        Assert.DoesNotContain(vm.IcoSizes, size => size.SizePx > IcoWriter.MaxFrameSizePx);
    }

    [AvaloniaFact]
    public void PngSizes_OfferTheSplashAssetsTickedAndTheRestNot()
    {
        MainWindowViewModel vm = Create(out _, out _);

        Assert.Equal([16, 32, 64, 128, 256, 512, 1024], vm.PngSizes.Select(s => s.SizePx));
        Assert.Equal([256, 512, 1024], vm.PngSizes.Where(s => s.IsSelected).Select(s => s.SizePx));
    }

    [AvaloniaFact]
    public void BuildExportRequest_TakesTheTickedSizesAscending()
    {
        MainWindowViewModel vm = Create(out _, out _);
        vm.OutputDirectory = "/out";

        foreach (SizeOption size in vm.IcoSizes)
        {
            size.IsSelected = size.SizePx is 16 or 48;
        }

        ExportRequest request = vm.BuildExportRequest();

        Assert.Equal([16, 48], request.IcoSizes);
        Assert.Equal([256, 512, 1024], request.PngSizes);
    }

    [AvaloniaFact]
    public void BuildExportRequest_TrimsTheFolderAndTheBaseName()
    {
        MainWindowViewModel vm = Create(out _, out _);
        vm.OutputDirectory = "  /out  ";
        vm.BaseName = "  enigma-app  ";

        ExportRequest request = vm.BuildExportRequest();

        Assert.Equal("/out", request.OutputDirectory);
        Assert.Equal("enigma-app", request.BaseName);
    }

    [AvaloniaFact]
    public void BuildExportRequest_UsesTheLiveControlValuesNotTheDebouncedPreview()
    {
        MainWindowViewModel vm = Create(out _, out _);
        vm.OutputDirectory = "/out";

        // The preview is debounced, so CurrentDesign is deliberately still the old one here. What the
        // user pressed Generate on is what must be written.
        vm.GlyphScale = 0.9;
        vm.SelectedFillMode = vm.FillModes.First(m => m.Mode == PlateFillMode.LinearGradient);

        ExportRequest request = vm.BuildExportRequest();

        Assert.Equal(0.9, request.Design.GlyphScale);
        Assert.Equal(PlateFillMode.LinearGradient, request.Design.Plate.Mode);
        Assert.NotEqual(0.9, vm.CurrentDesign.GlyphScale);
    }

    [AvaloniaFact]
    public void Generate_IsBlockedUntilAFolderIsChosen()
    {
        MainWindowViewModel vm = Create(out _, out _);

        Assert.False(vm.GenerateCommand.CanExecute(null));
        Assert.Equal("Choose an output folder.", vm.GenerateHint);
    }

    [AvaloniaFact]
    public void Generate_IsBlockedWhenTheFolderDoesNotExist()
    {
        MainWindowViewModel vm = Create(out _, out _);

        vm.OutputDirectory = Path.Combine(Path.GetTempPath(), "enigma-iconstudio-missing-" + Guid.NewGuid().ToString("N"));

        Assert.False(vm.GenerateCommand.CanExecute(null));
        Assert.Equal("That folder does not exist.", vm.GenerateHint);
    }

    [AvaloniaFact]
    public void Generate_IsBlockedByABaseNameThatIsNotAPlainFileName()
    {
        using var temp = new TempDirectory();
        MainWindowViewModel vm = Create(out _, out _);
        vm.OutputDirectory = temp.Path;

        Assert.True(vm.GenerateCommand.CanExecute(null));

        vm.BaseName = "sub/app";

        Assert.False(vm.GenerateCommand.CanExecute(null));
        Assert.Equal("The base name must be a plain file name.", vm.GenerateHint);
    }

    [AvaloniaFact]
    public void Generate_IsBlockedWhenEverySizeIsUnticked()
    {
        using var temp = new TempDirectory();
        MainWindowViewModel vm = Create(out _, out _);
        vm.OutputDirectory = temp.Path;

        foreach (SizeOption size in vm.IcoSizes)
        {
            size.IsSelected = false;
        }

        Assert.True(vm.GenerateCommand.CanExecute(null));

        foreach (SizeOption size in vm.PngSizes)
        {
            size.IsSelected = false;
        }

        Assert.False(vm.GenerateCommand.CanExecute(null));
        Assert.Equal("Select at least one size.", vm.GenerateHint);
    }

    [AvaloniaFact]
    public void Generate_ClearsTheHintOnceEverythingIsReady()
    {
        using var temp = new TempDirectory();
        MainWindowViewModel vm = Create(out _, out _);

        vm.OutputDirectory = temp.Path;

        Assert.True(vm.GenerateCommand.CanExecute(null));
        Assert.Equal(string.Empty, vm.GenerateHint);
    }

    [AvaloniaFact]
    public async Task Generate_WritesTheAssetsAndReportsThem()
    {
        using var temp = new TempDirectory();
        MainWindowViewModel vm = Create(out _, out _);
        vm.OutputDirectory = temp.Path;
        vm.BaseName = "app";

        // Keep the run small: the defaults would render eleven frames, three of them 512 px or more.
        foreach (SizeOption size in vm.IcoSizes)
        {
            size.IsSelected = size.SizePx is 16 or 32;
        }

        foreach (SizeOption size in vm.PngSizes)
        {
            size.IsSelected = size.SizePx == 256;
        }

        await vm.GenerateCommand.ExecuteAsync(null);

        Assert.True(File.Exists(Path.Combine(temp.Path, "app.ico")));
        Assert.True(File.Exists(Path.Combine(temp.Path, "app-256.png")));
        Assert.Contains("Wrote 2 files", vm.StatusText, StringComparison.Ordinal);
        Assert.Contains(temp.Path, vm.StatusText, StringComparison.Ordinal);
        Assert.False(vm.IsExporting);
    }

    [AvaloniaFact]
    public async Task Generate_ReportsAFailureInsteadOfThrowing()
    {
        using var temp = new TempDirectory();
        MainWindowViewModel vm = Create(out _, out _);
        vm.OutputDirectory = temp.Path;

        foreach (SizeOption size in vm.IcoSizes)
        {
            size.IsSelected = size.SizePx == 16;
        }

        foreach (SizeOption size in vm.PngSizes)
        {
            size.IsSelected = false;
        }

        // The folder vanishes between the button lighting up and the command running — the race a
        // CanExecute check can never close.
        Directory.Delete(temp.Path, recursive: true);

        await vm.GenerateCommand.ExecuteAsync(null);

        Assert.StartsWith("Export failed:", vm.StatusText, StringComparison.Ordinal);
        Assert.False(vm.IsExporting);
    }

    [AvaloniaFact]
    public async Task Browse_AdoptsTheChosenFolder()
    {
        MainWindowViewModel vm = Create(out _, out FakeFolderPicker picker);
        picker.Result = "/chosen";

        await vm.BrowseCommand.ExecuteAsync(null);

        Assert.Equal("/chosen", vm.OutputDirectory);
        Assert.Equal(1, picker.CallCount);
    }

    [AvaloniaFact]
    public async Task Browse_LeavesTheFolderAloneWhenNothingIsChosen()
    {
        using var temp = new TempDirectory();
        MainWindowViewModel vm = Create(out _, out FakeFolderPicker picker);
        vm.OutputDirectory = temp.Path;
        picker.Result = null;

        await vm.BrowseCommand.ExecuteAsync(null);

        Assert.Equal(temp.Path, vm.OutputDirectory);
    }

    [AvaloniaFact]
    public async Task Browse_OpensWhereTheCurrentFolderPointsTo()
    {
        using var temp = new TempDirectory();
        MainWindowViewModel vm = Create(out _, out FakeFolderPicker picker);
        vm.OutputDirectory = "  " + temp.Path + "  ";

        await vm.BrowseCommand.ExecuteAsync(null);

        Assert.Equal(temp.Path, picker.LastSuggestedStartLocation);
    }

    [AvaloniaFact]
    public async Task Browse_PassesNoStartLocationWhenTheFolderIsBlank()
    {
        MainWindowViewModel vm = Create(out _, out FakeFolderPicker picker);

        await vm.BrowseCommand.ExecuteAsync(null);

        Assert.Null(picker.LastSuggestedStartLocation);
    }

    private static MainWindowViewModel Create(out IIconRasterizer rasterizer, out FakeFolderPicker picker)
    {
        rasterizer = new AvaloniaIconRasterizer();
        picker = new FakeFolderPicker();

        return new MainWindowViewModel(rasterizer, new IconExporter(rasterizer), picker);
    }
}
