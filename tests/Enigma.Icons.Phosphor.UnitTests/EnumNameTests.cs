using System;
using System.Text.RegularExpressions;
using Enigma.Icons.Phosphor.UnitTests.TestSupport;
using Xunit;

namespace Enigma.Icons.Phosphor.UnitTests;

/// <summary>
/// SPEC §12.2 — the full-corpus enum ↔ name round trip. The generated tables are the only bridge
/// between a <see cref="PhosphorIcon"/> member and the artwork it names, and they replace
/// <c>Enum.ToString</c>/<c>Enum.Parse</c> everywhere (SPEC §2.11), so they are asserted over all
/// 1,512 members rather than spot-checked.
/// </summary>
public sealed class EnumNameTests
{
    private const int IconCount = 1512;

    private static readonly Regex KebabOnly = new Regex("^[a-z-]+$", RegexOptions.CultureInvariant);

    [Fact]
    public void All_HasOneNamePerEnumMemberInEnumOrder()
    {
        Assert.Equal(IconCount, PhosphorIconNames.All.Count);

        var violations = new ViolationLog();

        for (int i = 0; i < IconCount; i++)
        {
            string fromTable = PhosphorIconNames.All[i];
            string fromMember = PhosphorIconNames.ToKebabCase((PhosphorIcon)i);

            if (!string.Equals(fromTable, fromMember, StringComparison.Ordinal))
            {
                violations.Add("index {0}: All is '{1}' but ToKebabCase is '{2}'.", i, fromTable, fromMember);
            }
        }

        violations.AssertEmpty("All versus ToKebabCase");
    }

    [Fact]
    public void EveryName_RoundTripsThroughTryParse()
    {
        var violations = new ViolationLog();

        for (int i = 0; i < IconCount; i++)
        {
            var expected = (PhosphorIcon)i;
            string name = PhosphorIconNames.ToKebabCase(expected);

            if (!PhosphorIconNames.TryParse(name, out PhosphorIcon actual))
            {
                violations.Add("'{0}': TryParse returned false.", name);
                continue;
            }

            if (actual != expected)
            {
                violations.Add("'{0}': parsed to {1}, expected {2}.", name, (int)actual, i);
            }
        }

        violations.AssertEmpty("Enum-to-name round trip");
    }

    [Fact]
    public void EveryName_IsLowerCaseKebabAndOrdinallySorted()
    {
        // SPEC §7.4/§8.4: the whole kebab→Pascal mapping is lossless only because every name matches
        // this alphabet — no digits, no underscores, no upper case.
        var violations = new ViolationLog();

        for (int i = 0; i < IconCount; i++)
        {
            string name = PhosphorIconNames.All[i];

            if (!KebabOnly.IsMatch(name))
            {
                violations.Add("'{0}': does not match ^[a-z-]+$.", name);
            }

            if (i > 0 && string.CompareOrdinal(PhosphorIconNames.All[i - 1], name) >= 0)
            {
                violations.Add("'{0}' precedes '{1}'.", PhosphorIconNames.All[i - 1], name);
            }
        }

        violations.AssertEmpty("Icon-name alphabet and order");
    }

    [Theory]
    [InlineData("address-book")]
    [InlineData("ADDRESS-BOOK")]
    [InlineData("Address-Book")]
    [InlineData("address_book")]
    [InlineData("Address_Book")]
    [InlineData("AddressBook")]
    [InlineData("addressBook")]
    public void TryParse_AcceptsKebabSnakeAndPascalCaseInsensitively(string input)
    {
        Assert.True(PhosphorIconNames.TryParse(input, out PhosphorIcon icon));
        Assert.Equal(PhosphorIcon.AddressBook, icon);
    }

    [Theory]
    [InlineData("ADDRESS_BOOK")]
    [InlineData("ADDRESSBOOK")]
    public void TryParse_DoesNotAcceptARunOfUpperCaseLetters(string input)
    {
        // KNOWN GAP, pinned deliberately rather than patched here — see docs/done/FEATURE-3950.md.
        // The generated normalizer (FEATURE-2DDE, SPEC §8.4) inserts a separator before *every*
        // upper-case letter, so two upper-case letters in a row never reconstruct the kebab name.
        // Enigma.Icons' own IconNameNormalizer uses a word-boundary rule and does accept
        // "ADDRESS_BOOK", so the two IIconSet implementations differ on this one input class.
        // Fixing it means changing the generator's emitted template and regenerating, which is
        // FEATURE-2DDE's to own. This test fails the day it is fixed — update it then.
        Assert.False(PhosphorIconNames.TryParse(input, out PhosphorIcon _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-icon")]
    [InlineData("acorn ")]
    [InlineData(" acorn")]
    [InlineData("acorn-")]
    public void TryParse_RejectsGarbage(string input)
    {
        Assert.False(PhosphorIconNames.TryParse(input, out PhosphorIcon icon));
        Assert.Equal(default, icon);
    }

    [Fact]
    public void TryParse_RejectsNullWithoutThrowing()
    {
        // Null is a miss, not an exception — the rule IIconSet fixes for every lookup surface.
        // The null-forgiving operator is deliberate: the parameter is non-nullable, and passing null
        // anyway is exactly the runtime behaviour under test.
        Assert.False(PhosphorIconNames.TryParse(null!, out PhosphorIcon _));
    }

    [Theory]
    [InlineData(999999)]
    [InlineData(IconCount)]
    [InlineData(-1)]
    public void ToKebabCase_ThrowsForAnUndefinedMember(int value)
    {
        ArgumentOutOfRangeException ex =
            Assert.Throws<ArgumentOutOfRangeException>(() => PhosphorIconNames.ToKebabCase((PhosphorIcon)value));

        Assert.Equal("icon", ex.ParamName);
    }

    [Fact]
    public void All_IsNotMutableThroughACast()
    {
        Assert.IsNotType<string[]>(PhosphorIconNames.All);
    }
}
