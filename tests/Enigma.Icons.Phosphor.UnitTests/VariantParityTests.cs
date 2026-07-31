using System.Collections.Generic;
using Enigma.Icons.Phosphor.UnitTests.TestSupport;
using Xunit;

namespace Enigma.Icons.Phosphor.UnitTests;

/// <summary>
/// CODE-REVIEW-1FD4 PHASE02 — the parity this phase exists to establish: the two in-house
/// <see cref="IIconSet"/> implementations must agree on every variant spelling, because
/// <see cref="IIconSet"/> documents <b>one</b> normalization rule and applies it to names and
/// variants alike.
/// </summary>
/// <remarks>
/// The reference implementation is <see cref="SvgIconSet"/>, which has always routed its variants
/// through <c>IconNameNormalizer</c>. <see cref="PhosphorIconSet"/> probed the raw string until this
/// phase, so <c>" Duotone "</c> missed here and hit there.
/// </remarks>
public sealed class VariantParityTests
{
    private const string SquareSvg =
        "<svg viewBox=\"0 0 256 256\" fill=\"currentColor\"><path d=\"M 0,0 L 8,0 L 8,8 Z\" /></svg>";

    /// <summary>The six Phosphor weights, as an equivalent on-disk <see cref="SvgIconSet"/>.</summary>
    private static SvgIconSet BuildEquivalentSvgSet(TempDirectory directory)
    {
        foreach (string weight in PhosphorIconSet.Instance.Variants)
        {
            directory.WriteFile(weight + "/acorn.svg", SquareSvg);
        }

        return SvgIconSet.FromDirectory(directory.Path, defaultVariant: "regular", name: "Phosphor");
    }

    [Theory]
    // Spellings both sets must resolve.
    [InlineData(null)]
    [InlineData("regular")]
    [InlineData("REGULAR")]
    [InlineData("Regular")]
    [InlineData("duotone")]
    [InlineData("DUOTONE")]
    [InlineData(" duotone")]
    [InlineData("duotone ")]
    [InlineData(" Duotone ")]
    [InlineData("\tduotone\t")]
    [InlineData("thin")]
    [InlineData("Light")]
    [InlineData("BOLD")]
    [InlineData(" fill ")]
    // Spellings both sets must miss.
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("-")]
    [InlineData("heavy")]
    [InlineData("outline")]
    [InlineData("duo-tone")]
    [InlineData("DuoTone")]
    [InlineData("duo tone")]
    public void BothSetsAgreeOnAVariantSpelling(string? variant)
    {
        using var directory = new TempDirectory();
        SvgIconSet reference = BuildEquivalentSvgSet(directory);

        bool phosphorHit = PhosphorIconSet.Instance.TryGetGlyph("acorn", variant, out IconGlyph? phosphorGlyph);
        bool referenceHit = reference.TryGetGlyph("acorn", variant, out IconGlyph? referenceGlyph);

        Assert.Equal(referenceHit, phosphorHit);

        if (!phosphorHit)
        {
            Assert.Null(phosphorGlyph);
            Assert.Null(referenceGlyph);
            return;
        }

        Assert.NotNull(phosphorGlyph);
        Assert.NotNull(referenceGlyph);
    }

    [Fact]
    public void EverySpellingOfAWeightResolvesToTheSameGlyphInstance()
    {
        IconGlyph canonical = PhosphorIconSet.Instance.GetGlyph(PhosphorIcon.Acorn, PhosphorWeight.Duotone);

        var spellings = new List<string> { "duotone", "Duotone", "DUOTONE", " duotone ", "\tDuotone\n" };

        foreach (string spelling in spellings)
        {
            // Reference equality, not just a hit: normalization must land on the same cached row.
            Assert.Same(canonical, PhosphorIconSet.Instance.GetGlyph("acorn", spelling));
        }
    }

    [Fact]
    public void ANormalizedVariantStillDoesNotFallBackToRegular()
    {
        // The one rule normalization must not soften: a weight the set lacks is a miss, however it
        // is spelled.
        Assert.False(PhosphorIconSet.Instance.TryGetGlyph("acorn", " Heavy ", out IconGlyph? glyph));
        Assert.Null(glyph);

        IconNotFoundException ex =
            Assert.Throws<IconNotFoundException>(() => PhosphorIconSet.Instance.GetGlyph("acorn", " Heavy "));

        // The exception echoes the caller's original string, not the normalized one.
        Assert.Equal(" Heavy ", ex.Variant);
    }
}
