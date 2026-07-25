using Xunit;

namespace Enigma.Icons.UnitTests;

public sealed class SvgIconParserShapeTests
{
    private static IconLayer SingleLayer(string body)
    {
        IconGlyph glyph = SvgIconParser.Parse("<svg viewBox=\"0 0 256 256\">" + body + "</svg>");
        return Assert.Single(glyph.Layers);
    }

    [Fact]
    public void Path_TakesTheDAttributeVerbatim()
    {
        const string D = "M232,104a56.06,56.06,0,0,0-56-56H136a24,24,0,0,0-24,24Z";

        Assert.Equal(D, SingleLayer("<path d=\"" + D + "\" />").PathData);
    }

    [Fact]
    public void Path_WithoutDAttributeProducesNoLayer()
    {
        // An empty <path> paints nothing; that is not an error, but it leaves the document with no
        // paintable element at all.
        Assert.Throws<SvgParseException>(() => SvgIconParser.Parse("<svg><path /></svg>"));
    }

    [Fact]
    public void Rect_PlainProducesFourExplicitLineSegments()
        => Assert.Equal(
            "M 10,20 L 110,20 L 110,70 L 10,70 Z",
            SingleLayer("<rect x=\"10\" y=\"20\" width=\"100\" height=\"50\" />").PathData);

    [Fact]
    public void Rect_DefaultsXAndYToZero()
        => Assert.Equal(
            "M 0,0 L 8,0 L 8,4 L 0,4 Z",
            SingleLayer("<rect width=\"8\" height=\"4\" />").PathData);

    [Fact]
    public void Rect_WithRxOnlyMirrorsTheRadiusToRy()
        => Assert.Equal(
            "M 44,48 L 212,48 A 16,16 0 0 1 228,64 L 228,192 A 16,16 0 0 1 212,208 L 44,208 A 16,16 0 0 1 28,192 L 28,64 A 16,16 0 0 1 44,48 Z",
            SingleLayer("<rect x=\"28\" y=\"48\" width=\"200\" height=\"160\" rx=\"16\" />").PathData);

    [Fact]
    public void Rect_WithRyOnlyMirrorsTheRadiusToRx()
        => Assert.Equal(
            "M 44,48 L 212,48 A 16,16 0 0 1 228,64 L 228,192 A 16,16 0 0 1 212,208 L 44,208 A 16,16 0 0 1 28,192 L 28,64 A 16,16 0 0 1 44,48 Z",
            SingleLayer("<rect x=\"28\" y=\"48\" width=\"200\" height=\"160\" ry=\"16\" />").PathData);

    [Fact]
    public void Rect_WithBothRadiiKeepsThemIndependent()
        => Assert.Equal(
            "M 10,0 L 90,0 A 10,5 0 0 1 100,5 L 100,35 A 10,5 0 0 1 90,40 L 10,40 A 10,5 0 0 1 0,35 L 0,5 A 10,5 0 0 1 10,0 Z",
            SingleLayer("<rect width=\"100\" height=\"40\" rx=\"10\" ry=\"5\" />").PathData);

    [Fact]
    public void Rect_ClampsRadiiToHalfTheSide()
        => Assert.Equal(
            "M 50,0 L 50,0 A 50,20 0 0 1 100,20 L 100,20 A 50,20 0 0 1 50,40 L 50,40 A 50,20 0 0 1 0,20 L 0,20 A 50,20 0 0 1 50,0 Z",
            SingleLayer("<rect width=\"100\" height=\"40\" rx=\"999\" ry=\"999\" />").PathData);

    [Fact]
    public void Rect_TreatsANegativeRadiusAsAbsent()
        => Assert.Equal(
            "M 0,0 L 100,0 L 100,40 L 0,40 Z",
            SingleLayer("<rect width=\"100\" height=\"40\" rx=\"-5\" />").PathData);

    [Theory]
    [InlineData("<rect width=\"0\" height=\"40\" />")]
    [InlineData("<rect width=\"100\" height=\"0\" />")]
    [InlineData("<rect width=\"-1\" height=\"40\" />")]
    [InlineData("<rect height=\"40\" />")]
    public void Rect_NonRenderingProducesNoLayer(string body)
        => Assert.Throws<SvgParseException>(() => SvgIconParser.Parse("<svg>" + body + "</svg>"));

    [Fact]
    public void Rect_UnparseableAttributeIsAnError()
    {
        SvgParseException error = Assert.Throws<SvgParseException>(
            () => SvgIconParser.Parse("<svg><rect width=\"wide\" height=\"40\" /></svg>"));

        Assert.Contains("width", error.Message, System.StringComparison.Ordinal);
        Assert.Contains("<rect>", error.Message, System.StringComparison.Ordinal);
    }

    [Fact]
    public void Circle_ConvertsToTwoSemicircularArcs()
        => Assert.Equal(
            "M 28,128 A 100,100 0 1 1 228,128 A 100,100 0 1 1 28,128 Z",
            SingleLayer("<circle cx=\"128\" cy=\"128\" r=\"100\" />").PathData);

    [Fact]
    public void Circle_DefaultsTheCentreToTheOrigin()
        => Assert.Equal(
            "M -5,0 A 5,5 0 1 1 5,0 A 5,5 0 1 1 -5,0 Z",
            SingleLayer("<circle r=\"5\" />").PathData);

    [Fact]
    public void Circle_WithoutARadiusProducesNoLayer()
        => Assert.Throws<SvgParseException>(() => SvgIconParser.Parse("<svg><circle cx=\"1\" cy=\"1\" /></svg>"));

    [Fact]
    public void Ellipse_ConvertsToTwoSemicircularArcs()
        => Assert.Equal(
            "M 28,128 A 100,60 0 1 1 228,128 A 100,60 0 1 1 28,128 Z",
            SingleLayer("<ellipse cx=\"128\" cy=\"128\" rx=\"100\" ry=\"60\" />").PathData);

    [Fact]
    public void Ellipse_WithoutBothRadiiProducesNoLayer()
        => Assert.Throws<SvgParseException>(() => SvgIconParser.Parse("<svg><ellipse rx=\"10\" /></svg>"));

    [Fact]
    public void Line_ConvertsToAMoveAndALine()
        => Assert.Equal(
            "M 1,2 L 3,4",
            SingleLayer("<line x1=\"1\" y1=\"2\" x2=\"3\" y2=\"4\" />").PathData);

    [Fact]
    public void Line_DefaultsEveryCoordinateToZero()
        => Assert.Equal("M 0,0 L 0,0", SingleLayer("<line />").PathData);

    [Fact]
    public void Polyline_IsOpen()
        => Assert.Equal(
            "M 0,0 L 10,10 L 20,0",
            SingleLayer("<polyline points=\"0,0 10,10 20,0\" />").PathData);

    [Fact]
    public void Polyline_AcceptsWhitespaceSeparatedPoints()
        => Assert.Equal(
            "M 0,0 L 10,10",
            SingleLayer("<polyline points=\"0 0 10 10\" />").PathData);

    [Fact]
    public void Polyline_DropsATrailingOddNumber()
        => Assert.Equal(
            "M 0,0 L 10,10",
            SingleLayer("<polyline points=\"0,0 10,10 20\" />").PathData);

    [Fact]
    public void Polyline_WithFewerThanTwoPairsProducesNoLayer()
        => Assert.Throws<SvgParseException>(() => SvgIconParser.Parse("<svg><polyline points=\"1,2\" /></svg>"));

    [Fact]
    public void Polygon_IsClosed()
        => Assert.Equal(
            "M 0,0 L 10,10 L 20,0 Z",
            SingleLayer("<polygon points=\"0,0 10,10 20,0\" />").PathData);

    [Fact]
    public void DocumentOrder_IsPaintOrder()
    {
        IconGlyph glyph = SvgIconParser.Parse(
            "<svg><path d=\"first\" /><path d=\"second\" /></svg>");

        Assert.Equal(2, glyph.Layers.Count);
        Assert.Equal("first", glyph.Layers[0].PathData);
        Assert.Equal("second", glyph.Layers[1].PathData);
    }

    [Fact]
    public void Shapes_ParseWithoutTheSvgNamespace()
    {
        // Elements are matched on local name, so the namespace is honoured but not required.
        IconGlyph withNamespace = SvgIconParser.Parse(
            "<svg xmlns=\"http://www.w3.org/2000/svg\"><circle r=\"5\" /></svg>");
        IconGlyph withoutNamespace = SvgIconParser.Parse("<svg><circle r=\"5\" /></svg>");

        Assert.Equal(withNamespace.Layers[0].PathData, withoutNamespace.Layers[0].PathData);
    }

    [Fact]
    public void TitleDescAndMetadata_AreSkippedSilently()
    {
        IconGlyph glyph = SvgIconParser.Parse(
            "<svg><title>An icon</title><desc>Described</desc><metadata><x /></metadata><path d=\"M 0,0\" /></svg>");

        Assert.Single(glyph.Layers);
    }
}
