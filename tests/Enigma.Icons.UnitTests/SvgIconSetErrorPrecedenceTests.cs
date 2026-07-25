using System;
using System.Collections.Generic;
using System.IO;
using Enigma.Icons.UnitTests.TestSupport;
using Xunit;

namespace Enigma.Icons.UnitTests;

/// <summary>
/// Both halves of the miss-versus-broken-source precedence rule: the <c>Try</c> prefix governs
/// absence, not corruption.
/// </summary>
public sealed class SvgIconSetErrorPrecedenceTests
{
    private const string SquareSvg =
        "<svg viewBox=\"0 0 256 256\"><path d=\"M 0,0 L 8,0 L 8,8 Z\" /></svg>";

    [Fact]
    public void AMiss_ReturnsFalseAndThrowsNothing()
    {
        SvgIconSet set = SvgIconSet.FromSvgSources(
            new[] { new KeyValuePair<string, string>("square", SquareSvg) });

        Assert.False(set.TryGetGlyph("no-such-icon", null, out IconGlyph? glyph));
        Assert.Null(glyph);
    }

    [Fact]
    public void AMissingVariant_ReturnsFalseAndThrowsNothing()
    {
        using var temp = new TempDirectory();
        temp.WriteFile(Path.Combine("thin", "square.svg"), SquareSvg);

        SvgIconSet set = SvgIconSet.FromDirectory(temp.Path);

        Assert.False(set.TryGetGlyph("square", "bold", out IconGlyph? glyph));
        Assert.Null(glyph);
    }

    [Fact]
    public void ABrokenInMemorySource_ThrowsFromTryGetGlyphToo()
    {
        SvgIconSet set = SvgIconSet.FromSvgSources(
            new[] { new KeyValuePair<string, string>("broken", "this is not svg at all") });

        // Reporting a malformed source as "icon not found" would hide a real defect behind a blank
        // space, so TryGetGlyph does NOT swallow it.
        Assert.Throws<SvgParseException>(() => set.TryGetGlyph("broken", null, out _));
        Assert.Throws<SvgParseException>(() => set.GetGlyph("broken"));
    }

    [Fact]
    public void AFileThatVanishesAfterConstruction_ThrowsWrappingTheIoError()
    {
        using var temp = new TempDirectory();
        string file = temp.WriteFile(Path.Combine("thin", "square.svg"), SquareSvg);

        SvgIconSet set = SvgIconSet.FromDirectory(temp.Path);
        File.Delete(file);

        SvgParseException error = Assert.Throws<SvgParseException>(
            () => set.TryGetGlyph("square", "thin", out _));

        Assert.IsAssignableFrom<IOException>(error.InnerException);
        Assert.Throws<SvgParseException>(() => set.GetGlyph("square", "thin"));
    }

    [Fact]
    public void AFaultedSource_IsNotCachedForever()
    {
        using var temp = new TempDirectory();
        string file = temp.WriteFile(Path.Combine("thin", "square.svg"), SquareSvg);

        SvgIconSet set = SvgIconSet.FromDirectory(temp.Path);
        File.Delete(file);

        Assert.Throws<SvgParseException>(() => set.GetGlyph("square", "thin"));

        // The transient failure must not poison the entry for the life of the process.
        File.WriteAllText(file, SquareSvg);

        IconGlyph glyph = set.GetGlyph("square", "thin");
        Assert.Equal("M 0,0 L 8,0 L 8,8 Z", glyph.Layers[0].PathData);
        Assert.Same(glyph, set.GetGlyph("square", "thin"));
    }

    [Fact]
    public void ABrokenFileSource_ThrowsFromBothMethods()
    {
        using var temp = new TempDirectory();
        temp.WriteFile(Path.Combine("thin", "square.svg"), "<svg><linearGradient /></svg>");

        SvgIconSet set = SvgIconSet.FromDirectory(temp.Path);

        SvgParseException fromTry = Assert.Throws<SvgParseException>(
            () => set.TryGetGlyph("square", "thin", out _));
        Assert.Contains("<linearGradient>", fromTry.Message, StringComparison.Ordinal);

        Assert.Throws<SvgParseException>(() => set.GetGlyph("square", "thin"));
    }
}
