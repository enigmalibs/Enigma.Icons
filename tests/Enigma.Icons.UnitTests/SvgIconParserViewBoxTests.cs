using Xunit;

namespace Enigma.Icons.UnitTests;

public sealed class SvgIconParserViewBoxTests
{
    private static IconViewBox ViewBoxOf(string rootAttributes)
        => SvgIconParser.Parse("<svg " + rootAttributes + "><path d=\"M 0,0\" /></svg>").ViewBox;

    [Fact]
    public void ViewBoxAttribute_IsUsedWhenPresent()
    {
        IconViewBox box = ViewBoxOf("viewBox=\"1 2 48 24\"");

        Assert.Equal(1, box.X);
        Assert.Equal(2, box.Y);
        Assert.Equal(48, box.Width);
        Assert.Equal(24, box.Height);
    }

    [Fact]
    public void ViewBoxAttribute_AcceptsCommaSeparatedNumbers()
        => Assert.Equal(new IconViewBox(0, 0, 256, 256), ViewBoxOf("viewBox=\"0,0,256,256\""));

    [Fact]
    public void ViewBoxAttribute_WinsOverWidthAndHeight()
        => Assert.Equal(new IconViewBox(0, 0, 48, 48), ViewBoxOf("viewBox=\"0 0 48 48\" width=\"999\" height=\"999\""));

    [Fact]
    public void AbsentViewBox_IsDerivedFromWidthAndHeight()
        => Assert.Equal(new IconViewBox(0, 0, 32, 24), ViewBoxOf("width=\"32\" height=\"24\""));

    [Fact]
    public void AbsentViewBox_AcceptsPixelUnitsOnWidthAndHeight()
        => Assert.Equal(new IconViewBox(0, 0, 32, 24), ViewBoxOf("width=\"32px\" height=\"24px\""));

    [Fact]
    public void AbsentViewBox_FallsBackToTheDefaultWhenOnlyOneDimensionIsGiven()
        => Assert.Equal(IconViewBox.Default, ViewBoxOf("width=\"32\""));

    [Fact]
    public void AbsentViewBox_FallsBackToTheDefaultForPercentageDimensions()
    {
        // width="100%" is common on a root <svg>; it is "not usable", not an error.
        Assert.Equal(IconViewBox.Default, ViewBoxOf("width=\"100%\" height=\"100%\""));
    }

    [Fact]
    public void AbsentViewBox_FallsBackToTheDefaultForNonPositiveDimensions()
        => Assert.Equal(IconViewBox.Default, ViewBoxOf("width=\"0\" height=\"10\""));

    [Fact]
    public void NoViewBoxAndNoDimensions_YieldsTheDefault()
        => Assert.Equal(IconViewBox.Default, ViewBoxOf(string.Empty));

    [Theory]
    [InlineData("viewBox=\"0 0 48\"")]
    [InlineData("viewBox=\"0 0 48 24 96\"")]
    [InlineData("viewBox=\"0 0 0 24\"")]
    [InlineData("viewBox=\"0 0 -48 24\"")]
    [InlineData("viewBox=\"nonsense\"")]
    public void MalformedViewBox_IsAnError(string rootAttributes)
    {
        // The fallback ladder is for an ABSENT viewBox, not a broken one.
        SvgParseException error = Assert.Throws<SvgParseException>(() => ViewBoxOf(rootAttributes));

        Assert.Contains("viewBox", error.Message, System.StringComparison.Ordinal);
    }
}
