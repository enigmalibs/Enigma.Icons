using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Enigma.Icons.UnitTests.TestSupport;
using Xunit;

namespace Enigma.Icons.UnitTests;

public sealed class SvgIconSetTests
{
    private const string ResourcePrefix = "Enigma.Icons.UnitTests.TestAssets.";

    private const string SquareSvg =
        "<svg viewBox=\"0 0 256 256\" fill=\"currentColor\"><path d=\"M 0,0 L 8,0 L 8,8 Z\" /></svg>";

    private static string AssetRoot => Path.Combine(AppContext.BaseDirectory, "TestAssets");

    [Fact]
    public void FromDirectory_DiscoversVariantsFromSubfolders()
    {
        SvgIconSet set = SvgIconSet.FromDirectory(AssetRoot);

        Assert.Equal(new[] { "bold", "thin" }, set.Variants);
        Assert.Equal("TestAssets", set.Name);
    }

    [Fact]
    public void FromDirectory_ListsEveryIconNameOnceOrdinallySorted()
    {
        SvgIconSet set = SvgIconSet.FromDirectory(AssetRoot);

        Assert.Equal(
            new[] { "acorn", "circle-badge", "ellipse-badge", "rounded-card" },
            set.IconNames.ToArray());
    }

    [Fact]
    public void FromDirectory_StripsTheVariantSuffixFromFileNames()
    {
        // bold/acorn-bold.svg and thin/acorn.svg both yield the icon "acorn".
        SvgIconSet set = SvgIconSet.FromDirectory(AssetRoot);

        Assert.True(set.TryGetGlyph("acorn", "bold", out IconGlyph? bold));
        Assert.True(set.TryGetGlyph("acorn", "thin", out IconGlyph? thin));
        Assert.NotNull(bold);
        Assert.NotNull(thin);
        Assert.NotEqual(bold.Layers[0].PathData, thin.Layers[0].PathData);
    }

    [Fact]
    public void FromDirectory_DefaultsToRegularWhenPresent()
    {
        using var temp = new TempDirectory();
        temp.WriteFile(Path.Combine("bold", "acorn.svg"), SquareSvg);
        temp.WriteFile(Path.Combine("regular", "acorn.svg"), SquareSvg);

        SvgIconSet set = SvgIconSet.FromDirectory(temp.Path);

        Assert.Equal("regular", set.DefaultVariant);
    }

    [Fact]
    public void FromDirectory_DefaultsToTheFirstVariantOrdinallyWhenRegularIsAbsent()
        => Assert.Equal("bold", SvgIconSet.FromDirectory(AssetRoot).DefaultVariant);

    [Fact]
    public void FromDirectory_HonoursAnExplicitDefaultVariant()
        => Assert.Equal("thin", SvgIconSet.FromDirectory(AssetRoot, defaultVariant: "thin").DefaultVariant);

    [Fact]
    public void FromDirectory_RejectsADefaultVariantThatWasNotDiscovered()
        => Assert.Throws<ArgumentException>(() => SvgIconSet.FromDirectory(AssetRoot, defaultVariant: "duotone"));

    [Fact]
    public void FromDirectory_WithoutSubfoldersHasNoVariants()
    {
        using var temp = new TempDirectory();
        temp.WriteFile("acorn.svg", SquareSvg);
        temp.WriteFile("leaf.svg", SquareSvg);

        SvgIconSet set = SvgIconSet.FromDirectory(temp.Path, variantsFromSubfolders: false);

        Assert.Empty(set.Variants);
        Assert.Null(set.DefaultVariant);
        Assert.Equal(new[] { "acorn", "leaf" }, set.IconNames.ToArray());
        Assert.NotNull(set.GetGlyph("acorn"));
    }

    [Fact]
    public void FromDirectory_WithoutSubfoldersRejectsADefaultVariant()
    {
        using var temp = new TempDirectory();
        temp.WriteFile("acorn.svg", SquareSvg);

        Assert.Throws<ArgumentException>(
            () => SvgIconSet.FromDirectory(temp.Path, variantsFromSubfolders: false, defaultVariant: "regular"));
    }

    [Fact]
    public void FromDirectory_WithoutSubfoldersDoesNotStripAVariantSuffix()
    {
        using var temp = new TempDirectory();
        temp.WriteFile("acorn-bold.svg", SquareSvg);

        SvgIconSet set = SvgIconSet.FromDirectory(temp.Path, variantsFromSubfolders: false);

        Assert.Equal(new[] { "acorn-bold" }, set.IconNames.ToArray());
    }

    [Fact]
    public void FromDirectory_DoesNotRecurseBelowAVariantFolder()
    {
        using var temp = new TempDirectory();
        temp.WriteFile(Path.Combine("bold", "acorn.svg"), SquareSvg);
        temp.WriteFile(Path.Combine("bold", "nested", "hidden.svg"), SquareSvg);

        SvgIconSet set = SvgIconSet.FromDirectory(temp.Path);

        Assert.Equal(new[] { "acorn" }, set.IconNames.ToArray());
    }

    [Fact]
    public void FromDirectory_ThrowsAtConstructionForAMissingDirectory()
        => Assert.Throws<DirectoryNotFoundException>(
            () => SvgIconSet.FromDirectory(Path.Combine(Path.GetTempPath(), "enigma-icons-does-not-exist-" + Guid.NewGuid().ToString("N"))));

    [Fact]
    public void FromDirectory_RejectsANullPath()
        => Assert.Throws<ArgumentNullException>(() => SvgIconSet.FromDirectory(null!));

    [Fact]
    public void FromDirectory_HonoursAnExplicitName()
        => Assert.Equal("My Icons", SvgIconSet.FromDirectory(AssetRoot, name: "My Icons").Name);

    [Fact]
    public void FromAssembly_DiscoversVariantsFromTheResourceSegments()
    {
        SvgIconSet set = SvgIconSet.FromAssembly(typeof(SvgIconSetTests).Assembly, ResourcePrefix);

        Assert.Equal(new[] { "bold", "thin" }, set.Variants);
        Assert.Equal(
            new[] { "acorn", "circle-badge", "ellipse-badge", "rounded-card" },
            set.IconNames.ToArray());
        Assert.NotNull(set.GetGlyph("acorn", "bold"));
    }

    [Fact]
    public void FromAssembly_DefaultsTheNameToTheAssemblySimpleName()
        => Assert.Equal(
            "Enigma.Icons.UnitTests",
            SvgIconSet.FromAssembly(typeof(SvgIconSetTests).Assembly, ResourcePrefix).Name);

    [Fact]
    public void FromAssembly_RejectsAPrefixThatMatchesNothing()
    {
        Assembly assembly = typeof(SvgIconSetTests).Assembly;

        ArgumentException error = Assert.Throws<ArgumentException>(
            () => SvgIconSet.FromAssembly(assembly, "No.Such.Prefix."));

        Assert.Contains("No.Such.Prefix.", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FromAssembly_RejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => SvgIconSet.FromAssembly(null!, ResourcePrefix));
        Assert.Throws<ArgumentNullException>(() => SvgIconSet.FromAssembly(typeof(SvgIconSetTests).Assembly, null!));
    }

    [Fact]
    public void FromFiles_UsesEachFileNameAndHasNoVariants()
    {
        SvgIconSet set = SvgIconSet.FromFiles(
            new[]
            {
                Path.Combine(AssetRoot, "thin", "acorn.svg"),
                Path.Combine(AssetRoot, "thin", "circle-badge.svg"),
            });

        Assert.Empty(set.Variants);
        Assert.Null(set.DefaultVariant);
        Assert.Equal(new[] { "acorn", "circle-badge" }, set.IconNames.ToArray());
        Assert.Equal("SvgIcons", set.Name);
    }

    [Fact]
    public void FromFiles_ThrowsAtConstructionForAMissingFile()
        => Assert.Throws<FileNotFoundException>(
            () => SvgIconSet.FromFiles(new[] { Path.Combine(AssetRoot, "thin", "nope.svg") }));

    [Fact]
    public void FromFiles_RejectsANullSequence()
        => Assert.Throws<ArgumentNullException>(() => SvgIconSet.FromFiles(null!));

    [Fact]
    public void FromSvgSources_UsesTheKeyAsTheIconName()
    {
        SvgIconSet set = SvgIconSet.FromSvgSources(
            new[]
            {
                new KeyValuePair<string, string>("My_Square", SquareSvg),
            },
            name: "Memory");

        Assert.Equal("Memory", set.Name);
        Assert.Equal(new[] { "my-square" }, set.IconNames.ToArray());
        Assert.Equal("M 0,0 L 8,0 L 8,8 Z", set.GetGlyph("my-square").Layers[0].PathData);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromSvgSources_RejectsABlankValue(string? svg)
        => Assert.Throws<ArgumentException>(
            () => SvgIconSet.FromSvgSources(new[] { new KeyValuePair<string, string>("icon", svg!) }));

    [Fact]
    public void FromSvgSources_RejectsANullSequence()
        => Assert.Throws<ArgumentNullException>(() => SvgIconSet.FromSvgSources(null!));

    [Theory]
    [InlineData("circle-badge")]
    [InlineData("Circle-Badge")]
    [InlineData("CIRCLE-BADGE")]
    [InlineData("circle_badge")]
    [InlineData("CircleBadge")]
    [InlineData("  circle-badge  ")]
    public void Lookup_IsCaseInsensitiveAndAcceptsEveryNamingStyle(string icon)
    {
        SvgIconSet set = SvgIconSet.FromDirectory(AssetRoot);

        Assert.True(set.TryGetGlyph(icon, "thin", out IconGlyph? glyph));
        Assert.NotNull(glyph);
    }

    [Theory]
    [InlineData("THIN")]
    [InlineData("Thin")]
    public void VariantLookup_IsCaseInsensitive(string variant)
    {
        SvgIconSet set = SvgIconSet.FromDirectory(AssetRoot);

        Assert.True(set.TryGetGlyph("acorn", variant, out IconGlyph? glyph));
        Assert.NotNull(glyph);
    }

    [Fact]
    public void NullVariant_ResolvesToTheDefaultVariant()
    {
        SvgIconSet set = SvgIconSet.FromDirectory(AssetRoot, defaultVariant: "thin");

        Assert.True(set.TryGetGlyph("acorn", null, out IconGlyph? viaDefault));
        Assert.True(set.TryGetGlyph("acorn", "thin", out IconGlyph? explicitly));
        Assert.Same(explicitly, viaDefault);
    }

    [Fact]
    public void AVariantTheSetLacks_IsAMissNotAFallback()
    {
        // A caller asking for "duotone" must not silently get the default variant back.
        SvgIconSet set = SvgIconSet.FromDirectory(AssetRoot);

        Assert.False(set.TryGetGlyph("acorn", "duotone", out IconGlyph? glyph));
        Assert.Null(glyph);

        IconNotFoundException error = Assert.Throws<IconNotFoundException>(() => set.GetGlyph("acorn", "duotone"));
        Assert.Equal("duotone", error.Variant);
    }

    [Fact]
    public void AVariantOnASetWithoutVariants_IsAMiss()
    {
        SvgIconSet set = SvgIconSet.FromSvgSources(
            new[] { new KeyValuePair<string, string>("square", SquareSvg) });

        Assert.False(set.TryGetGlyph("square", "bold", out IconGlyph? glyph));
        Assert.Null(glyph);
        Assert.True(set.TryGetGlyph("square", null, out IconGlyph? viaDefault));
        Assert.NotNull(viaDefault);
    }

    [Fact]
    public void AMissingIcon_IsFalseFromTryGetGlyphAndThrowsFromGetGlyph()
    {
        SvgIconSet set = SvgIconSet.FromDirectory(AssetRoot, name: "Phosphor");

        Assert.False(set.TryGetGlyph("no-such-icon", "thin", out IconGlyph? glyph));
        Assert.Null(glyph);

        IconNotFoundException error = Assert.Throws<IconNotFoundException>(() => set.GetGlyph("no-such-icon", "thin"));
        Assert.Equal("no-such-icon", error.IconName);
        Assert.Equal("thin", error.Variant);
        Assert.Equal("Phosphor", error.SetName);
        Assert.Equal("Icon 'no-such-icon' (variant 'thin') was not found in icon set 'Phosphor'.", error.Message);
    }

    [Fact]
    public void AMissingIconWithoutAVariant_DropsTheParentheticalFromTheMessage()
    {
        SvgIconSet set = SvgIconSet.FromSvgSources(
            new[] { new KeyValuePair<string, string>("square", SquareSvg) },
            name: "Memory");

        IconNotFoundException error = Assert.Throws<IconNotFoundException>(() => set.GetGlyph("nope"));

        Assert.Null(error.Variant);
        Assert.Equal("Icon 'nope' was not found in icon set 'Memory'.", error.Message);
    }

    [Fact]
    public void ANullIconName_IsAMissForTryGetGlyphAndThrowsFromGetGlyph()
    {
        // The precedent for every IIconSet implementation.
        SvgIconSet set = SvgIconSet.FromDirectory(AssetRoot);

        Assert.False(set.TryGetGlyph(null!, "thin", out IconGlyph? glyph));
        Assert.Null(glyph);
        Assert.Throws<ArgumentNullException>(() => set.GetGlyph(null!, "thin"));
    }

    [Fact]
    public void ABlankIconName_IsAMiss()
    {
        SvgIconSet set = SvgIconSet.FromDirectory(AssetRoot);

        Assert.False(set.TryGetGlyph("   ", "thin", out IconGlyph? glyph));
        Assert.Null(glyph);
    }

    [Fact]
    public void RepeatedLookups_ReturnAReferenceEqualGlyph()
    {
        SvgIconSet set = SvgIconSet.FromDirectory(AssetRoot);

        IconGlyph first = set.GetGlyph("acorn", "thin");
        IconGlyph second = set.GetGlyph("Acorn", "THIN");

        Assert.Same(first, second);
    }

    [Fact]
    public async Task ConcurrentLookups_StayReferenceConsistent()
    {
        SvgIconSet set = SvgIconSet.FromDirectory(AssetRoot);

        var tasks = new Task<IconGlyph>[32];
        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(() => set.GetGlyph("circle-badge", "thin"), TestContext.Current.CancellationToken);
        }

        IconGlyph[] glyphs = await Task.WhenAll(tasks);

        foreach (IconGlyph glyph in glyphs)
        {
            Assert.Same(glyphs[0], glyph);
        }
    }

    [Fact]
    public void ParsingIsLazyButDiscoveryIsEager()
    {
        using var temp = new TempDirectory();
        temp.WriteFile(Path.Combine("thin", "good.svg"), SquareSvg);
        temp.WriteFile(Path.Combine("thin", "broken.svg"), "<svg><not-a-shape /></svg>");

        // Construction succeeds even though one source cannot be parsed...
        SvgIconSet set = SvgIconSet.FromDirectory(temp.Path);
        Assert.Equal(new[] { "broken", "good" }, set.IconNames.ToArray());

        // ...and the good icon resolves regardless.
        Assert.NotNull(set.GetGlyph("good", "thin"));
    }

    [Fact]
    public void CollidingNamesInOneVariant_ResolveToTheFirstInOrdinalOrder()
    {
        using var temp = new TempDirectory();
        temp.WriteFile(Path.Combine("thin", "My_Icon.svg"), SquareSvg);
        temp.WriteFile(
            Path.Combine("thin", "my-icon.svg"),
            "<svg viewBox=\"0 0 256 256\"><path d=\"M 1,1\" /></svg>");

        SvgIconSet set = SvgIconSet.FromDirectory(temp.Path);

        // "My_Icon.svg" sorts before "my-icon.svg" ordinally, so it wins.
        Assert.Equal(new[] { "my-icon" }, set.IconNames.ToArray());
        Assert.Equal("M 0,0 L 8,0 L 8,8 Z", set.GetGlyph("my-icon", "thin").Layers[0].PathData);
    }

    [Fact]
    public void ShapePrimitives_RoundTripThroughAnIconSet()
    {
        // The arc-flag conversions exercised through SvgIconSet, not only through SvgIconParser.
        SvgIconSet set = SvgIconSet.FromDirectory(AssetRoot);

        Assert.Equal(
            "M 28,128 A 100,100 0 1 1 228,128 A 100,100 0 1 1 28,128 Z",
            set.GetGlyph("circle-badge", "thin").Layers[0].PathData);
        Assert.Equal(
            "M 28,128 A 100,60 0 1 1 228,128 A 100,60 0 1 1 28,128 Z",
            set.GetGlyph("ellipse-badge", "thin").Layers[0].PathData);
        Assert.Equal(
            "M 44,48 L 212,48 A 16,16 0 0 1 228,64 L 228,192 A 16,16 0 0 1 212,208 L 44,208 A 16,16 0 0 1 28,192 L 28,64 A 16,16 0 0 1 44,48 Z",
            set.GetGlyph("rounded-card", "thin").Layers[0].PathData);
    }

    [Fact]
    public void ShapePrimitives_RoundTripThroughEmbeddedResources()
    {
        SvgIconSet set = SvgIconSet.FromAssembly(typeof(SvgIconSetTests).Assembly, ResourcePrefix);

        Assert.Equal(
            "M 28,128 A 100,100 0 1 1 228,128 A 100,100 0 1 1 28,128 Z",
            set.GetGlyph("circle-badge", "thin").Layers[0].PathData);
    }

    [Fact]
    public void ParsedGlyphs_CarryTheRootCurrentColorAsNull()
    {
        SvgIconSet set = SvgIconSet.FromDirectory(AssetRoot);

        Assert.Null(set.GetGlyph("acorn", "thin").Layers[0].Fill);
    }
}
