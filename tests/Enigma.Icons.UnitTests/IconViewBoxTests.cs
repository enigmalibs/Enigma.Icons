using System;
using Xunit;

namespace Enigma.Icons.UnitTests;

public sealed class IconViewBoxTests
{
    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    public void Constructor_RejectsNonPositiveWidth(double width)
        => Assert.Throws<ArgumentOutOfRangeException>(() => new IconViewBox(0, 0, width, 10));

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    public void Constructor_RejectsNonPositiveHeight(double height)
        => Assert.Throws<ArgumentOutOfRangeException>(() => new IconViewBox(0, 0, 10, height));

    [Fact]
    public void Constructor_KeepsAllFourComponents()
    {
        var box = new IconViewBox(1, 2, 3, 4);

        Assert.Equal(1, box.X);
        Assert.Equal(2, box.Y);
        Assert.Equal(3, box.Width);
        Assert.Equal(4, box.Height);
    }

    [Fact]
    public void Default_IsThePhosphorViewBox()
    {
        IconViewBox box = IconViewBox.Default;

        Assert.Equal(0, box.X);
        Assert.Equal(0, box.Y);
        Assert.Equal(256, box.Width);
        Assert.Equal(256, box.Height);
    }

    [Fact]
    public void Equality_ComparesByValue()
    {
        var a = new IconViewBox(0, 0, 256, 256);
        var b = new IconViewBox(0, 0, 256, 256);
        var c = new IconViewBox(0, 0, 256, 128);

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.NotEqual(a, c);
        Assert.True(a != c);
        Assert.True(a.Equals((object)b));
        Assert.False(a.Equals("not a view box"));
    }

    [Fact]
    public void GetHashCode_AgreesWithEquality()
    {
        var a = new IconViewBox(1, 2, 3, 4);
        var b = new IconViewBox(1, 2, 3, 4);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ToString_EmitsTheDatHeaderShape()
        => Assert.Equal("0 0 256 256", IconViewBox.Default.ToString());
}
