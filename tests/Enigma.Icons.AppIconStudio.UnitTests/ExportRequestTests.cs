using System;
using Avalonia.Media;
using Enigma.Icons.AppIconStudio.Design;
using Enigma.Icons.AppIconStudio.Export;
using Enigma.Icons.Phosphor;
using Xunit;

namespace Enigma.Icons.AppIconStudio.UnitTests;

public sealed class ExportRequestTests
{
    private static readonly IconDesign Design = new IconDesign(
        PhosphorIcon.Square,
        PhosphorWeight.Fill,
        Colors.White,
        PlateFill.Solid(Colors.DodgerBlue));

    [Fact]
    public void Sizes_AreDeduplicatedAndSortedAscending()
    {
        var request = new ExportRequest(Design, "/out", "app", [64, 16, 64, 32], [512, 256, 256]);

        Assert.Equal([16, 32, 64], request.IcoSizes);
        Assert.Equal([256, 512], request.PngSizes);
    }

    [Fact]
    public void BaseName_IsTrimmed()
    {
        var request = new ExportRequest(Design, "/out", "  app  ", [32], []);

        Assert.Equal("app", request.BaseName);
    }

    [Fact]
    public void EitherSizeListMayBeEmpty()
    {
        Assert.Empty(new ExportRequest(Design, "/out", "app", [], [256]).IcoSizes);
        Assert.Empty(new ExportRequest(Design, "/out", "app", [32], []).PngSizes);
    }

    [Fact]
    public void BothSizeListsEmptyIsRejected()
        => Assert.Throws<ArgumentException>(() => new ExportRequest(Design, "/out", "app", [], []));

    [Theory]
    [InlineData(0)]
    [InlineData(-16)]
    [InlineData(257)]
    public void IcoSizes_AreCappedAtTwoFiftySix(int sizePx)
        => Assert.Throws<ArgumentException>(() => new ExportRequest(Design, "/out", "app", [sizePx], []));

    [Theory]
    [InlineData(0)]
    [InlineData(ExportRequest.MaxPngSizePx + 1)]
    public void PngSizes_AreCappedAtTheRasterizerCeiling(int sizePx)
        => Assert.Throws<ArgumentException>(() => new ExportRequest(Design, "/out", "app", [], [sizePx]));

    [Fact]
    public void PngSizes_MayExceedTheIcoLimit()
    {
        // A 1,024 px splash asset is one of the reasons the PNG list is separate from the ICO list.
        var request = new ExportRequest(Design, "/out", "app", [], [1024]);

        Assert.Equal([1024], request.PngSizes);
    }

    [Theory]
    [InlineData("app")]
    [InlineData("enigma-markdown-editor")]
    [InlineData("App_Icon.v2")]
    public void IsValidBaseName_AcceptsAPlainFileName(string baseName)
        => Assert.True(ExportRequest.IsValidBaseName(baseName));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("sub/app")]
    [InlineData("sub\\app")]
    [InlineData("../app")]
    [InlineData("..\\app")]
    [InlineData("/etc/app")]
    [InlineData(" app")]
    [InlineData("app ")]
    public void IsValidBaseName_RejectsAnythingThatIsNotOne(string? baseName)
        => Assert.False(ExportRequest.IsValidBaseName(baseName));

    [Theory]
    [InlineData("sub/app")]
    [InlineData("../app")]
    [InlineData("..")]
    [InlineData("")]
    public void BaseName_IsRejectedByTheConstructorToo(string baseName)
        => Assert.Throws<ArgumentException>(() => new ExportRequest(Design, "/out", baseName, [32], []));

    [Fact]
    public void OutputDirectory_IsRequired()
        => Assert.Throws<ArgumentException>(() => new ExportRequest(Design, "   ", "app", [32], []));

    [Fact]
    public void Arguments_AreNullChecked()
    {
        // null! is the point of these: they force the nulls a nullable-aware caller cannot pass, so
        // the runtime guards are exercised rather than only the compiler's (SPEC §2.3).
        Assert.Throws<ArgumentNullException>(() => new ExportRequest(null!, "/out", "app", [32], []));
        Assert.Throws<ArgumentNullException>(() => new ExportRequest(Design, null!, "app", [32], []));
        Assert.Throws<ArgumentNullException>(() => new ExportRequest(Design, "/out", null!, [32], []));
        Assert.Throws<ArgumentNullException>(() => new ExportRequest(Design, "/out", "app", null!, []));
        Assert.Throws<ArgumentNullException>(() => new ExportRequest(Design, "/out", "app", [32], null!));
    }
}
