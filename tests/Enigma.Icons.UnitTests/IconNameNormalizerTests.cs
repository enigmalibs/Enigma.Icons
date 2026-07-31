using System;
using Enigma.Icons.Internal;
using Xunit;

namespace Enigma.Icons.UnitTests;

public sealed class IconNameNormalizerTests
{
    [Theory]
    [InlineData("acorn", "acorn")]
    [InlineData("address-book", "address-book")]
    [InlineData("address_book", "address-book")]
    [InlineData("AddressBook", "address-book")]
    [InlineData("addressBook", "address-book")]
    [InlineData("ADDRESSBOOK", "addressbook")]
    [InlineData("HTTPServer", "http-server")]
    [InlineData("Address Book", "address-book")]
    [InlineData("  acorn  ", "acorn")]
    [InlineData("ACORN", "acorn")]
    [InlineData("cell-signal-none", "cell-signal-none")]
    [InlineData("CellSignalNone", "cell-signal-none")]
    [InlineData("cell__signal", "cell-signal")]
    [InlineData("--acorn--", "acorn")]
    [InlineData("number2Icon", "number2-icon")]
    public void TryNormalize_ProducesKebabCase(string input, string expected)
        => Assert.Equal(expected, IconNameNormalizer.TryNormalize(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("---")]
    public void TryNormalize_ReturnsNullWhenNothingUsableIsLeft(string? input)
        => Assert.Null(IconNameNormalizer.TryNormalize(input));

    [Fact]
    public void Normalize_ThrowsOnANullName()
        => Assert.Throws<ArgumentNullException>(() => IconNameNormalizer.Normalize(null!, "icon"));

    [Fact]
    public void Normalize_ThrowsWhenNothingUsableIsLeft()
        => Assert.Throws<ArgumentException>(() => IconNameNormalizer.Normalize("---", "icon"));

    [Theory]
    [InlineData("acorn-bold", "bold", "acorn")]
    [InlineData("acorn", "bold", "acorn")]
    [InlineData("acorn-bold-bold", "bold", "acorn-bold")]
    [InlineData("bold", "bold", "bold")]
    [InlineData("acorn-bold", "thin", "acorn-bold")]
    [InlineData("acorn", "", "acorn")]
    public void StripVariantSuffix_RemovesOnlyAFullTrailingVariant(string name, string variant, string expected)
        => Assert.Equal(expected, IconNameNormalizer.StripVariantSuffix(name, variant));
}
