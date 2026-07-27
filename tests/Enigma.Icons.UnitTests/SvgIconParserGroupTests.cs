using Xunit;

namespace Enigma.Icons.UnitTests;

public sealed class SvgIconParserGroupTests
{
    private static IconLayer SingleLayer(string svg) => Assert.Single(SvgIconParser.Parse(svg).Layers);

    [Fact]
    public void Group_InheritsItsPaintToDescendants()
    {
        IconLayer layer = SingleLayer("<svg><g fill=\"red\" stroke=\"blue\"><path d=\"M 0,0\" /></g></svg>");

        Assert.Equal("red", layer.Fill);
        Assert.Equal("blue", layer.Stroke);
    }

    [Fact]
    public void Child_OverridesTheInheritedPaint()
    {
        IconLayer layer = SingleLayer("<svg><g fill=\"red\"><path fill=\"green\" d=\"M 0,0\" /></g></svg>");

        Assert.Equal("green", layer.Fill);
    }

    [Fact]
    public void Inheritance_NestsToAnyDepth()
    {
        IconLayer layer = SingleLayer(
            "<svg><g fill=\"red\"><g stroke-width=\"3\"><g stroke-linecap=\"round\"><path d=\"M 0,0\" /></g></g></g></svg>");

        Assert.Equal("red", layer.Fill);
        Assert.Equal(3.0, layer.StrokeWidth);
        Assert.Equal(IconLineCap.Round, layer.StrokeLineCap);
    }

    [Fact]
    public void RootSvgElement_ParticipatesInInheritance()
    {
        // Upstream Phosphor artwork carries fill="currentColor" on <svg> itself, which normalizes
        // to null — "inherit the renderer's brush".
        IconLayer layer = SingleLayer("<svg fill=\"currentColor\"><path d=\"M 0,0\" /></svg>");

        Assert.Null(layer.Fill);
        Assert.True(layer.IsFilled);
    }

    [Fact]
    public void RootSvgElement_InheritsAConcretePaintToo()
        => Assert.Equal("#abcdef", SingleLayer("<svg fill=\"#abcdef\"><path d=\"M 0,0\" /></svg>").Fill);

    [Fact]
    public void Opacity_IsMultipliedDownTheGroupChain()
    {
        IconLayer layer = SingleLayer(
            "<svg><g opacity=\"0.5\"><path opacity=\"0.4\" d=\"M 0,0\" /></g></svg>");

        Assert.Equal(0.2, layer.Opacity, 10);
    }

    [Fact]
    public void Opacity_IsMultipliedAcrossSeveralGroups()
    {
        IconLayer layer = SingleLayer(
            "<svg><g opacity=\"0.5\"><g opacity=\"0.5\"><path opacity=\"0.5\" d=\"M 0,0\" /></g></g></svg>");

        Assert.Equal(0.125, layer.Opacity, 10);
    }

    [Fact]
    public void SiblingGroups_DoNotLeakStateIntoEachOther()
    {
        IconGlyph glyph = SvgIconParser.Parse(
            "<svg><g fill=\"red\"><path d=\"M 0,0\" /></g><g><path d=\"M 1,1\" /></g></svg>");

        Assert.Equal(2, glyph.Layers.Count);
        Assert.Equal("red", glyph.Layers[0].Fill);
        Assert.Null(glyph.Layers[1].Fill);
    }

    [Fact]
    public void EmptyGroup_DoesNotUnbalanceTheStack()
    {
        IconGlyph glyph = SvgIconParser.Parse(
            "<svg><g fill=\"red\" /><g fill=\"blue\"><path d=\"M 0,0\" /></g></svg>");

        Assert.Equal("blue", Assert.Single(glyph.Layers).Fill);
    }

    [Fact]
    public void InheritKeyword_LetsTheParentValueFlowThrough()
        => Assert.Equal("red", SingleLayer("<svg><g fill=\"red\"><path fill=\"inherit\" d=\"M 0,0\" /></g></svg>").Fill);

    [Fact]
    public void DeeplyNestedGroups_DoNotOverflowTheStack()
    {
        // The walk is iterative, so nesting is bounded by MaxDocumentBytes, not by the CLR stack.
        var builder = new System.Text.StringBuilder("<svg>");
        const int Depth = 2000;
        for (int i = 0; i < Depth; i++)
        {
            builder.Append("<g>");
        }

        builder.Append("<path d=\"M 0,0\" />");
        for (int i = 0; i < Depth; i++)
        {
            builder.Append("</g>");
        }

        builder.Append("</svg>");

        Assert.Single(SvgIconParser.Parse(builder.ToString()).Layers);
    }
}
