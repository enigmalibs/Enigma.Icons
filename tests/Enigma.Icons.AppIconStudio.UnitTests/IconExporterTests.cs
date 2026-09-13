using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Enigma.Icons.AppIconStudio.Design;
using Enigma.Icons.AppIconStudio.Export;
using Enigma.Icons.AppIconStudio.Rendering;
using Enigma.Icons.AppIconStudio.UnitTests.TestSupport;
using Enigma.Icons.Phosphor;
using Xunit;

namespace Enigma.Icons.AppIconStudio.UnitTests;

public sealed class IconExporterTests
{
    private static readonly IconDesign Design = new IconDesign(
        PhosphorIcon.MarkdownLogo,
        PhosphorWeight.Fill,
        Colors.White,
        PlateFill.Solid(Color.FromRgb(0x3B, 0x72, 0xF0)));

    [Fact]
    public async Task ExportAsync_WritesTheIconAndEveryPngUnderTheBaseName()
    {
        using var temp = new TempDirectory();
        var exporter = new IconExporter(new FakeRasterizer());

        ExportResult result = await exporter.ExportAsync(
            new ExportRequest(Design, temp.Path, "app", [16, 32, 256], [256, 512]),
            TestContext.Current.CancellationToken);

        Assert.True(File.Exists(Path.Combine(temp.Path, "app.ico")));
        Assert.True(File.Exists(Path.Combine(temp.Path, "app-256.png")));
        Assert.True(File.Exists(Path.Combine(temp.Path, "app-512.png")));

        Assert.Equal(3, result.WrittenFiles.Count);
        Assert.Equal(Path.Combine(temp.Path, "app.ico"), result.IcoPath);
        Assert.Equal(2, result.PngPaths.Count);

        // Exactly those three — no stray files.
        Assert.Equal(3, Directory.GetFiles(temp.Path).Length);
    }

    [Fact]
    public async Task ExportAsync_ChoosesTheFrameEncodingByTheThreshold()
    {
        using var temp = new TempDirectory();
        var rasterizer = new FakeRasterizer();

        await new IconExporter(rasterizer).ExportAsync(
            new ExportRequest(Design, temp.Path, "app", [16, 32, 48, 256], []),
            TestContext.Current.CancellationToken);

        // Below 256 the frames come from pixels; 256 comes from the PNG encoder.
        Assert.Equal([16, 32, 48], rasterizer.BgraCalls);
        Assert.Equal([256], rasterizer.PngCalls);
    }

    [Fact]
    public async Task ExportAsync_ProducesAnIcoWhoseDirectoryMatchesTheRequestedSizes()
    {
        using var temp = new TempDirectory();

        await new IconExporter(new FakeRasterizer()).ExportAsync(
            new ExportRequest(Design, temp.Path, "app", [16, 24, 32, 48, 64, 128, 256], []),
            TestContext.Current.CancellationToken);

        byte[] ico = await File.ReadAllBytesAsync(
            Path.Combine(temp.Path, "app.ico"),
            TestContext.Current.CancellationToken);

        IcoReader.Directory directory = IcoReader.Parse(ico);

        Assert.Equal(7, directory.Entries.Count);

        int[] expected = [16, 24, 32, 48, 64, 128, 256];
        for (int i = 0; i < expected.Length; i++)
        {
            IcoReader.Entry entry = directory.Entries[i];

            Assert.Equal(expected[i], entry.SizePx);
            Assert.Equal(entry.SizePx >= 256, entry.IsPng);

            // Every declared range addresses real bytes inside the file.
            Assert.True(entry.ImageOffset + entry.BytesInRes <= (uint)ico.Length);
            Assert.Equal((int)entry.BytesInRes, entry.Payload.Length);
        }
    }

    [Fact]
    public async Task ExportAsync_WritesNoIconWhenNoFrameSizeIsSelected()
    {
        using var temp = new TempDirectory();

        ExportResult result = await new IconExporter(new FakeRasterizer()).ExportAsync(
            new ExportRequest(Design, temp.Path, "app", [], [256]),
            TestContext.Current.CancellationToken);

        Assert.Null(result.IcoPath);
        Assert.False(File.Exists(Path.Combine(temp.Path, "app.ico")));
        Assert.Single(result.WrittenFiles);
    }

    [Fact]
    public async Task ExportAsync_OverwritesAnExistingFile()
    {
        using var temp = new TempDirectory();
        string target = Path.Combine(temp.Path, "app-256.png");
        await File.WriteAllTextAsync(target, "stale", TestContext.Current.CancellationToken);

        await new IconExporter(new FakeRasterizer()).ExportAsync(
            new ExportRequest(Design, temp.Path, "app", [], [256]),
            TestContext.Current.CancellationToken);

        byte[] written = await File.ReadAllBytesAsync(target, TestContext.Current.CancellationToken);

        // The PNG signature, not the five bytes of "stale".
        Assert.Equal(0x89, written[0]);
        Assert.Equal(0x50, written[1]);
    }

    [Fact]
    public async Task ExportAsync_RejectsADirectoryThatDoesNotExist()
    {
        using var temp = new TempDirectory();
        string missing = Path.Combine(temp.Path, "not-created");

        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => new IconExporter(new FakeRasterizer()).ExportAsync(
                new ExportRequest(Design, missing, "app", [32], []),
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ExportAsync_WritesNothingWhenTheDirectoryIsMissing()
    {
        using var temp = new TempDirectory();
        string missing = Path.Combine(temp.Path, "not-created");

        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => new IconExporter(new FakeRasterizer()).ExportAsync(
                new ExportRequest(Design, missing, "app", [32], [256]),
                TestContext.Current.CancellationToken));

        Assert.Empty(Directory.GetFiles(temp.Path));
    }

    [Fact]
    public async Task ExportAsync_HonoursCancellation()
    {
        using var temp = new TempDirectory();
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new IconExporter(new FakeRasterizer()).ExportAsync(
                new ExportRequest(Design, temp.Path, "app", [32], [256]),
                cancelled.Token));

        Assert.Empty(Directory.GetFiles(temp.Path));
    }

    [Fact]
    public void Constructor_RequiresARasterizer()
        // null! is the point of the test: it forces the null a nullable-aware caller cannot pass, so
        // the runtime guard is exercised rather than only the compiler's (SPEC §2.3).
        => Assert.Throws<ArgumentNullException>(() => new IconExporter(null!));

    [Fact]
    public async Task ExportAsync_RequiresARequest()
        => await Assert.ThrowsAsync<ArgumentNullException>(
            () => new IconExporter(new FakeRasterizer()).ExportAsync(null!, TestContext.Current.CancellationToken));

    [AvaloniaFact]
    public async Task ExportAsync_ProducesRealAssetsWithTheAvaloniaRasterizer()
    {
        using var temp = new TempDirectory();

        ExportResult result = await new IconExporter(new AvaloniaIconRasterizer()).ExportAsync(
            new ExportRequest(Design, temp.Path, "app", [16, 32, 48, 256], [256, 512]),
            TestContext.Current.CancellationToken);

        Assert.Equal(3, result.WrittenFiles.Count);

        // Every PNG decodes at the size its name claims.
        foreach (string path in result.PngPaths)
        {
            using FileStream file = File.OpenRead(path);
            using var bitmap = new Bitmap(file);

            int expected = int.Parse(
                Path.GetFileNameWithoutExtension(path).Split('-')[^1],
                System.Globalization.CultureInfo.InvariantCulture);

            Assert.Equal(new PixelSize(expected, expected), bitmap.PixelSize);
        }

        // The icon's 256 px frame is a real PNG that decodes on its own.
        byte[] ico = await File.ReadAllBytesAsync(result.IcoPath!, TestContext.Current.CancellationToken);
        IcoReader.Directory directory = IcoReader.Parse(ico);

        Assert.Equal([16, 32, 48, 256], SizesOf(directory));

        IcoReader.Entry largest = directory.Entries[^1];
        Assert.True(largest.IsPng);

        using var frame = new MemoryStream(largest.Payload);
        using var decodedFrame = new Bitmap(frame);
        Assert.Equal(new PixelSize(256, 256), decodedFrame.PixelSize);

        // The hybrid layout is the whole point: this frame set stored all-BMP would be ~280 KB,
        // almost all of it the one 256 px frame.
        int allBmp = 0;
        foreach (int size in new[] { 16, 32, 48, 256 })
        {
            allBmp += 40 + (size * size * 4) + (IcoWriter.AndMaskStride(size) * size);
        }

        Assert.True(allBmp > 270_000, $"sanity: the all-BMP equivalent should be large, got {allBmp}");
        Assert.True(ico.Length < allBmp / 4, $"expected a compact icon, got {ico.Length} of {allBmp} bytes");
    }

    private static int[] SizesOf(IcoReader.Directory directory)
    {
        var sizes = new int[directory.Entries.Count];
        for (int i = 0; i < sizes.Length; i++)
        {
            sizes[i] = directory.Entries[i].SizePx;
        }

        return sizes;
    }
}
