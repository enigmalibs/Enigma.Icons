using System;
using Avalonia.Media;
using Enigma.Icons.AppIconStudio.Design;
using Xunit;

namespace Enigma.Icons.AppIconStudio.UnitTests;

public sealed class PlateFillTests
{
    [Fact]
    public void Solid_KeepsTheColourAndSeedsTheSecondStop()
    {
        Color blue = Color.FromRgb(0x3B, 0x72, 0xF0);

        PlateFill fill = PlateFill.Solid(blue);

        Assert.Equal(PlateFillMode.Solid, fill.Mode);
        Assert.Equal(blue, fill.PrimaryColor);

        // Seeded rather than left default: toggling to gradient in the UI must not start from
        // transparent black.
        Assert.Equal(blue, fill.SecondaryColor);
    }

    [Fact]
    public void LinearGradient_DefaultsToTheTopLeftToBottomRightAngle()
    {
        PlateFill fill = PlateFill.LinearGradient(Colors.White, Colors.Black);

        Assert.Equal(PlateFillMode.LinearGradient, fill.Mode);
        Assert.Equal(PlateFill.DefaultAngleDegrees, fill.AngleDegrees);
    }

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(45.0, 45.0)]
    [InlineData(359.5, 359.5)]
    [InlineData(360.0, 0.0)]
    [InlineData(450.0, 90.0)]
    [InlineData(-90.0, 270.0)]
    [InlineData(-450.0, 270.0)]
    public void AngleDegrees_IsWrappedIntoZeroToThreeSixty(double given, double expected)
    {
        PlateFill fill = PlateFill.LinearGradient(Colors.White, Colors.Black, given);

        Assert.Equal(expected, fill.AngleDegrees, 10);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void AngleDegrees_RejectsNonFiniteValues(double given)
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => PlateFill.LinearGradient(Colors.White, Colors.Black, given));

    [Fact]
    public void Mode_RejectsAnUndeclaredMember()
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => new PlateFill((PlateFillMode)42, Colors.White, Colors.Black, 0.0));

    [Fact]
    public void ToString_DescribesTheModeWithoutThrowing()
    {
        Assert.Contains("Solid", PlateFill.Solid(Colors.White).ToString(), StringComparison.Ordinal);
        Assert.Contains(
            "Gradient",
            PlateFill.LinearGradient(Colors.White, Colors.Black, 90).ToString(),
            StringComparison.Ordinal);
    }
}
