using System;
using System.Collections.Generic;
using Xunit;

namespace Enigma.Icons.UnitTests;

public sealed class IconGlyphTests
{
    [Fact]
    public void Constructor_RejectsNullLayers()
        => Assert.Throws<ArgumentNullException>(() => new IconGlyph(IconViewBox.Default, null!));

    [Fact]
    public void Constructor_RejectsEmptyLayers()
        => Assert.Throws<ArgumentException>(() => new IconGlyph(IconViewBox.Default, new List<IconLayer>()));

    [Fact]
    public void Constructor_RejectsANullLayerElement()
    {
        var layers = new List<IconLayer> { new IconLayer("M 0,0 L 1,1"), null! };

        ArgumentException error = Assert.Throws<ArgumentException>(() => new IconGlyph(IconViewBox.Default, layers));
        Assert.Contains("index 1", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_CopiesTheLayerListDefensively()
    {
        var layers = new List<IconLayer> { new IconLayer("M 0,0 L 1,1") };
        var glyph = new IconGlyph(IconViewBox.Default, layers);

        layers.Add(new IconLayer("M 2,2 L 3,3"));
        layers[0] = new IconLayer("M 9,9 L 8,8");

        Assert.Single(glyph.Layers);
        Assert.Equal("M 0,0 L 1,1", glyph.Layers[0].PathData);
    }

    [Fact]
    public void Layers_AreNotMutableThroughACast()
    {
        var glyph = new IconGlyph(IconViewBox.Default, new List<IconLayer> { new IconLayer("M 0,0 L 1,1") });

        Assert.IsNotType<IconLayer[]>(glyph.Layers);
        Assert.IsNotType<List<IconLayer>>(glyph.Layers);
    }

    [Fact]
    public void PaintOrder_IsDocumentOrder()
    {
        var glyph = new IconGlyph(
            IconViewBox.Default,
            new List<IconLayer> { new IconLayer("M 0,0 L 1,1"), new IconLayer("M 2,2 L 3,3") });

        Assert.Equal("M 0,0 L 1,1", glyph.Layers[0].PathData);
        Assert.Equal("M 2,2 L 3,3", glyph.Layers[1].PathData);
    }

    [Fact]
    public void IsSingleLayer_IsTrueForOneOpaqueUnstrokedLayer()
    {
        var glyph = new IconGlyph(IconViewBox.Default, new List<IconLayer> { new IconLayer("M 0,0 L 1,1") });

        Assert.True(glyph.IsSingleLayer);
    }

    [Fact]
    public void IsSingleLayer_IsFalseForSeveralLayers()
    {
        var glyph = new IconGlyph(
            IconViewBox.Default,
            new List<IconLayer> { new IconLayer("M 0,0 L 1,1"), new IconLayer("M 2,2 L 3,3") });

        Assert.False(glyph.IsSingleLayer);
    }

    [Fact]
    public void IsSingleLayer_IsFalseForATranslucentLayer()
    {
        var glyph = new IconGlyph(IconViewBox.Default, new List<IconLayer> { new IconLayer("M 0,0 L 1,1", 0.2) });

        Assert.False(glyph.IsSingleLayer);
    }

    [Fact]
    public void IsSingleLayer_IsFalseForAStrokedLayer()
    {
        var glyph = new IconGlyph(
            IconViewBox.Default,
            new List<IconLayer> { new IconLayer("M 0,0 L 1,1", stroke: "#000000") });

        Assert.False(glyph.IsSingleLayer);
    }

    [Fact]
    public void ViewBox_IsKept()
    {
        var box = new IconViewBox(1, 2, 3, 4);
        var glyph = new IconGlyph(box, new List<IconLayer> { new IconLayer("M 0,0 L 1,1") });

        Assert.Equal(box, glyph.ViewBox);
    }
}
