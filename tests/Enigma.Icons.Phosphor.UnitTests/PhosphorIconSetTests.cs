using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Enigma.Icons.Phosphor.UnitTests;

/// <summary>
/// SPEC §9 and §12.2 — the public surface of <see cref="PhosphorIconSet"/>, both the strongly-typed
/// and the string-keyed one.
/// </summary>
public sealed class PhosphorIconSetTests
{
    private static PhosphorIconSet Set => PhosphorIconSet.Instance;

    [Fact]
    public void Name_IsPhosphor()
    {
        Assert.Equal("Phosphor", Set.Name);
    }

    [Fact]
    public void Variants_AreTheSixWeightNamesInDeclarationOrder()
    {
        Assert.Equal(
            new[] { "thin", "light", "regular", "bold", "fill", "duotone" },
            Set.Variants);
    }

    [Fact]
    public void Variants_AreNotMutableThroughACast()
    {
        Assert.IsNotType<string[]>(Set.Variants);
    }

    [Fact]
    public void DefaultVariant_IsRegular()
    {
        Assert.Equal("regular", Set.DefaultVariant);
    }

    [Fact]
    public void IconNames_AreTheFullOrdinallySortedCorpus()
    {
        string[] names = Set.IconNames.ToArray();

        Assert.Equal(1512, names.Length);
        Assert.Equal(names.OrderBy(n => n, StringComparer.Ordinal).ToArray(), names);
    }

    [Fact]
    public void Instance_IsASingleton()
    {
        Assert.Same(PhosphorIconSet.Instance, PhosphorIconSet.Instance);
    }

    [Fact]
    public void GetGlyph_DefaultsToRegular()
    {
        Assert.Same(Set.GetGlyph(PhosphorIcon.Acorn), Set.GetGlyph(PhosphorIcon.Acorn, PhosphorWeight.Regular));
        Assert.Same(Set.GetGlyph("acorn"), Set.GetGlyph(PhosphorIcon.Acorn, PhosphorWeight.Regular));
    }

    [Fact]
    public void TypedAndStringSurfaces_ReturnTheSameInstance()
    {
        // One lookup core behind both surfaces, so they cannot resolve different rows.
        Assert.Same(
            Set.GetGlyph(PhosphorIcon.AddressBook, PhosphorWeight.Duotone),
            Set.GetGlyph("address-book", "duotone"));
    }

    [Fact]
    public void RepeatedLookups_AreReferenceEqual()
    {
        Assert.Same(
            Set.GetGlyph(PhosphorIcon.Acorn, PhosphorWeight.Bold),
            Set.GetGlyph(PhosphorIcon.Acorn, PhosphorWeight.Bold));
    }

    [Theory]
    [InlineData("acorn")]
    [InlineData("ACORN")]
    [InlineData("Acorn")]
    public void NameLookup_IsCaseInsensitive(string name)
    {
        Assert.Same(Set.GetGlyph(PhosphorIcon.Acorn), Set.GetGlyph(name));
    }

    [Theory]
    [InlineData("address-book")]
    [InlineData("ADDRESS-BOOK")]
    [InlineData("address_book")]
    [InlineData("Address_Book")]
    [InlineData("AddressBook")]
    public void NameLookup_AcceptsKebabSnakeAndPascalCase(string name)
    {
        // A multi-word icon on purpose: every name matches ^[a-z-]+$ with no digits, so a one-word
        // name like "acorn" has no distinct snake or Pascal form to test.
        Assert.Same(Set.GetGlyph(PhosphorIcon.AddressBook), Set.GetGlyph(name));
    }

    [Fact]
    public void NameLookup_MissesOnARunOfUpperCaseLetters()
    {
        // The known normalization gap of EnumNameTests, seen from the IIconSet surface: SvgIconSet
        // resolves "ADDRESS_BOOK", this set misses it. Reported in docs/done/FEATURE-3950.md; pinned
        // here so the divergence is visible rather than a surprise.
        Assert.False(Set.TryGetGlyph("ADDRESS_BOOK", null, out IconGlyph? glyph));
        Assert.Null(glyph);
    }

    [Theory]
    [InlineData("thin")]
    [InlineData("THIN")]
    [InlineData("Thin")]
    public void VariantLookup_IsCaseInsensitive(string variant)
    {
        Assert.Same(Set.GetGlyph(PhosphorIcon.Acorn, PhosphorWeight.Thin), Set.GetGlyph("acorn", variant));
    }

    [Theory]
    [InlineData(" duotone")]
    [InlineData("duotone ")]
    [InlineData(" Duotone ")]
    [InlineData("\tDUOTONE\t")]
    public void VariantLookup_NormalizesTheSameWayAnIconNameDoes(string variant)
    {
        // CODE-REVIEW-1FD4 PHASE02: IIconSet documents one normalization rule for names AND
        // variants. Before this phase the variant was a bare dictionary probe on the raw string, so
        // every spelling below missed here and hit on the equivalent SvgIconSet.
        Assert.Same(Set.GetGlyph(PhosphorIcon.Acorn, PhosphorWeight.Duotone), Set.GetGlyph("acorn", variant));
    }

    [Theory]
    [InlineData("heavy")]
    [InlineData("outline")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("duo-tone")]
    [InlineData("DuoTone")]
    public void AVariantTheSetLacks_IsAMissAndNeverFallsBackToRegular(string variant)
    {
        Assert.False(Set.TryGetGlyph("acorn", variant, out IconGlyph? glyph));
        Assert.Null(glyph);

        IconNotFoundException ex = Assert.Throws<IconNotFoundException>(() => Set.GetGlyph("acorn", variant));

        Assert.Equal("acorn", ex.IconName);
        Assert.Equal(variant, ex.Variant);
        Assert.Equal("Phosphor", ex.SetName);
        Assert.Equal($"Icon 'acorn' (variant '{variant}') was not found in icon set 'Phosphor'.", ex.Message);
    }

    [Fact]
    public void AnUnknownIconName_IsAMiss()
    {
        Assert.False(Set.TryGetGlyph("not-an-icon", null, out IconGlyph? glyph));
        Assert.Null(glyph);

        IconNotFoundException ex = Assert.Throws<IconNotFoundException>(() => Set.GetGlyph("not-an-icon"));

        Assert.Equal("not-an-icon", ex.IconName);
        Assert.Null(ex.Variant);
        Assert.Equal("Phosphor", ex.SetName);
        Assert.Equal("Icon 'not-an-icon' was not found in icon set 'Phosphor'.", ex.Message);
    }

    [Fact]
    public void ANullIconName_IsAMissForTryGetGlyphAndThrowsFromGetGlyph()
    {
        // The rule FEATURE-24DD established for SvgIconSet, matched exactly: the two IIconSet
        // implementations must not disagree on null handling. The null-forgiving operators are
        // deliberate — passing null into a non-nullable parameter is the behaviour under test.
        Assert.False(Set.TryGetGlyph(null!, null, out IconGlyph? glyph));
        Assert.Null(glyph);

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => Set.GetGlyph(null!));
        Assert.Equal("icon", ex.ParamName);
    }

    [Fact]
    public void ANullVariant_MeansTheDefaultVariant()
    {
        Assert.True(Set.TryGetGlyph("acorn", null, out IconGlyph? glyph));
        Assert.Same(Set.GetGlyph(PhosphorIcon.Acorn, PhosphorWeight.Regular), glyph);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(99)]
    [InlineData(-1)]
    public void AnUndefinedWeight_ThrowsArgumentOutOfRange(int value)
    {
        var weight = (PhosphorWeight)value;

        ArgumentOutOfRangeException fromGet =
            Assert.Throws<ArgumentOutOfRangeException>(() => Set.GetGlyph(PhosphorIcon.Acorn, weight));
        Assert.Equal("weight", fromGet.ParamName);

        ArgumentOutOfRangeException fromTry =
            Assert.Throws<ArgumentOutOfRangeException>(() => Set.TryGetGlyph(PhosphorIcon.Acorn, weight, out IconGlyph? _));
        Assert.Equal("weight", fromTry.ParamName);
    }

    [Theory]
    [InlineData(1512)]
    [InlineData(999999)]
    public void AnUndefinedIcon_ThrowsArgumentOutOfRange(int value)
    {
        var icon = (PhosphorIcon)value;

        Assert.Throws<ArgumentOutOfRangeException>(() => Set.GetGlyph(icon));
        Assert.Throws<ArgumentOutOfRangeException>(() => Set.TryGetGlyph(icon, PhosphorWeight.Regular, out IconGlyph? _));
    }

    [Fact]
    public void TryGetGlyph_ReportsSuccessWithANonNullGlyph()
    {
        Assert.True(Set.TryGetGlyph(PhosphorIcon.Acorn, PhosphorWeight.Fill, out IconGlyph? glyph));
        Assert.NotNull(glyph);
    }

    [Fact]
    public void DuotoneGlyphs_KeepTheirPerLayerOpacity()
    {
        // The package carries duotone's tint into IconLayer.Opacity rather than flattening it; the
        // caveat about collapsing it belongs to the Avalonia renderer, not here.
        IconGlyph glyph = Set.GetGlyph(PhosphorIcon.Acorn, PhosphorWeight.Duotone);

        Assert.Equal(2, glyph.Layers.Count);
        Assert.Equal(0.2, glyph.Layers[0].Opacity, tolerance: 1e-9);
        Assert.False(glyph.IsSingleLayer);
    }

    [Fact]
    public void AsAnIconSet_ExposesTheSameBehaviourThroughTheAbstraction()
    {
        IIconSet set = PhosphorIconSet.Instance;

        Assert.Equal("Phosphor", set.Name);
        Assert.Equal("regular", set.DefaultVariant);
        Assert.Equal(6, set.Variants.Count);
        Assert.Equal(1512, set.IconNames.Count());
        Assert.Same(Set.GetGlyph(PhosphorIcon.Acorn), set.GetGlyph("acorn"));
    }

    [Fact]
    public void EveryVariantName_ResolvesToItsWeight()
    {
        var violations = new List<string>();

        for (int i = 0; i < Set.Variants.Count; i++)
        {
            IconGlyph byName = Set.GetGlyph("acorn", Set.Variants[i]);
            IconGlyph byWeight = Set.GetGlyph(PhosphorIcon.Acorn, (PhosphorWeight)i);

            if (!ReferenceEquals(byName, byWeight))
            {
                violations.Add(Set.Variants[i]);
            }
        }

        Assert.Empty(violations);
    }
}
