using Xunit;

namespace Enigma.Icons.UnitTests;

public sealed class SvgIconParserTransformTests
{
    private static string Baked(string transform, string pathData)
    {
        IconGlyph glyph = SvgIconParser.Parse(
            "<svg><path transform=\"" + transform + "\" d=\"" + pathData + "\" /></svg>");

        return Assert.Single(glyph.Layers).PathData;
    }

    [Fact]
    public void NoTransform_LeavesTheDAttributeByteIdentical()
    {
        // The identity fast path: no tokenization, no re-emission.
        const string D = "M232,104a56.06,56.06,0,0,0-56-56H136a24,24,0,0,0-24,24Z";

        IconGlyph glyph = SvgIconParser.Parse("<svg><path d=\"" + D + "\" /></svg>");

        Assert.Equal(D, glyph.Layers[0].PathData);
    }

    [Fact]
    public void IdentityTransform_LeavesTheDAttributeByteIdentical()
    {
        const string D = "M232,104a56.06,56.06,0,0,0-56-56H136Z";

        Assert.Equal(D, Baked("translate(0,0)", D));
    }

    [Fact]
    public void Translate_ShiftsEveryCoordinate()
        => Assert.Equal("M 10,20 L 20,20", Baked("translate(10,20)", "M 0,0 L 10,0"));

    [Fact]
    public void Translate_DefaultsTheYOffsetToZero()
        => Assert.Equal("M 10,0", Baked("translate(10)", "M 0,0"));

    [Fact]
    public void Scale_WithOneArgumentScalesBothAxes()
        => Assert.Equal("M 2,2 L 4,4", Baked("scale(2)", "M 1,1 L 2,2"));

    [Fact]
    public void Scale_WithTwoArgumentsScalesEachAxis()
        => Assert.Equal("M 2,3 L 4,6", Baked("scale(2,3)", "M 1,1 L 2,2"));

    [Fact]
    public void Rotate_AboutTheOrigin()
        => Assert.Equal("M 0,10", Baked("rotate(90)", "M 10,0"));

    [Fact]
    public void Rotate_AboutAnExplicitCentre()
        => Assert.Equal("M 10,10", Baked("rotate(90 10 10)", "M 10,10"));

    [Fact]
    public void Rotate_AboutAnExplicitCentreMovesOtherPoints()
        => Assert.Equal("M 0,10", Baked("rotate(90 5 5)", "M 10,10"));

    [Fact]
    public void Matrix_IsUsedDirectly()
        => Assert.Equal("M 7,9", Baked("matrix(1 0 0 1 5 5)", "M 2,4"));

    [Fact]
    public void SkewX_ShearsAlongX()
        => Assert.Equal("M 10,10", Baked("skewX(45)", "M 0,10"));

    [Fact]
    public void SkewY_ShearsAlongY()
        => Assert.Equal("M 10,10", Baked("skewY(45)", "M 10,0"));

    [Fact]
    public void TransformList_AppliesTheLastPrimitiveFirst()
        => Assert.Equal("M 12,2", Baked("translate(10,0) scale(2)", "M 1,1"));

    [Fact]
    public void TransformList_IsNotCommutative()
    {
        // The regression test for composition order: swapping the pair must change the result.
        string translateThenRotate = Baked("translate(10,0) rotate(90)", "M 0,0");
        string rotateThenTranslate = Baked("rotate(90) translate(10,0)", "M 0,0");

        Assert.Equal("M 10,0", translateThenRotate);
        Assert.Equal("M 0,10", rotateThenTranslate);
        Assert.NotEqual(translateThenRotate, rotateThenTranslate);
    }

    [Fact]
    public void AncestorTransform_ComposesOnTheLeftOfTheElementsOwn()
    {
        IconGlyph glyph = SvgIconParser.Parse(
            "<svg><g transform=\"translate(10,0)\"><path transform=\"translate(0,20)\" d=\"M 0,0\" /></g></svg>");

        Assert.Equal("M 10,20", glyph.Layers[0].PathData);
    }

    [Fact]
    public void NestedGroupTransforms_Accumulate()
    {
        IconGlyph glyph = SvgIconParser.Parse(
            "<svg><g transform=\"translate(10,0)\"><g transform=\"scale(2)\"><path d=\"M 1,1\" /></g></g></svg>");

        Assert.Equal("M 12,2", glyph.Layers[0].PathData);
    }

    [Fact]
    public void RelativeCommands_AreConvertedToAbsolute()
        => Assert.Equal("M 1,1 L 3,3", Baked("translate(1,1)", "M 0,0 l 2,2"));

    [Fact]
    public void HorizontalAndVerticalCommands_AreExpandedToLines()
    {
        // H and V cannot survive a rotation, so they are always expanded.
        Assert.Equal("M 1,0 L 11,0 L 11,5", Baked("translate(1,0)", "M 0,0 H 10 V 5"));
    }

    [Fact]
    public void CurveCommands_TransformEveryControlPoint()
        => Assert.Equal(
            "M 1,1 C 2,1 3,1 4,1",
            Baked("translate(1,1)", "M 0,0 C 1,0 2,0 3,0"));

    [Fact]
    public void SmoothAndQuadraticCommands_KeepTheirShorthandForm()
        => Assert.Equal(
            "M 1,1 Q 2,1 3,1 T 5,1 S 6,1 7,1",
            Baked("translate(1,1)", "M 0,0 Q 1,0 2,0 T 4,0 S 5,0 6,0"));

    [Fact]
    public void ClosePath_PassesThroughAndResetsTheCurrentPoint()
        => Assert.Equal(
            "M 1,1 L 3,1 Z L 3,3",
            Baked("translate(1,1)", "M 0,0 L 2,0 Z l 2,2"));

    [Fact]
    public void ImplicitRepetition_OfMoveToBecomesLineTo()
        => Assert.Equal("M 1,1 L 2,1 L 3,1", Baked("translate(1,1)", "M 0,0 1,0 2,0"));

    [Fact]
    public void Arc_UnderAPureTranslationKeepsItsRadiiAndRotation()
        => Assert.Equal(
            "M 1,1 A 50,100 0 1 0 11,1",
            Baked("translate(1,1)", "M 0,0 A 50,100 0 1 0 10,0"));

    [Fact]
    public void Arc_UnderANonUniformScaleIsRecomputed()
        => Assert.Equal(
            "M 0,0 A 200,50 0 0 1 20,0",
            Baked("scale(2,1)", "M 0,0 A 100,50 0 0 1 10,0"));

    [Fact]
    public void Arc_UnderARotationTurnsWithTheEllipse()
        => Assert.Equal(
            "M 0,0 A 100,50 90 0 1 0,10",
            Baked("rotate(90)", "M 0,0 A 100,50 0 0 1 10,0"));

    [Fact]
    public void Arc_UnderAMirrorFlipsTheSweepFlag()
    {
        // A reflection reverses the angular direction, so sweep flips while large-arc does not.
        Assert.Equal(
            "M 0,0 A 100,50 0 1 0 -10,0",
            Baked("scale(-1,1)", "M 0,0 A 100,50 0 1 1 10,0"));
    }

    [Fact]
    public void Arc_WithAZeroRadiusDegeneratesToALine()
        => Assert.Equal("M 1,1 L 11,1", Baked("translate(1,1)", "M 0,0 A 0,50 0 1 1 10,0"));

    [Fact]
    public void Arc_FlagsMayRunTogetherWithoutSeparators()
        => Assert.Equal("M 1,1 A 5,5 0 1 1 11,1", Baked("translate(1,1)", "M 0,0 a5 5 0 11 10 0"));

    [Fact]
    public void UnknownTransformFunction_IsAnError()
    {
        SvgParseException error = Assert.Throws<SvgParseException>(
            () => SvgIconParser.Parse("<svg><path transform=\"warp(2)\" d=\"M 0,0\" /></svg>"));

        Assert.Contains("warp", error.Message, System.StringComparison.Ordinal);
    }

    [Fact]
    public void WrongTransformArgumentCount_IsAnError()
    {
        SvgParseException error = Assert.Throws<SvgParseException>(
            () => SvgIconParser.Parse("<svg><path transform=\"rotate(1,2)\" d=\"M 0,0\" /></svg>"));

        Assert.Contains("rotate", error.Message, System.StringComparison.Ordinal);
    }

    [Fact]
    public void MalformedPathData_IsAnErrorOnceATransformMustBeApplied()
    {
        SvgParseException error = Assert.Throws<SvgParseException>(
            () => SvgIconParser.Parse("<svg><path transform=\"translate(1,1)\" d=\"M 0,0 W 3\" /></svg>"));

        Assert.Contains("'W'", error.Message, System.StringComparison.Ordinal);
    }

    [Fact]
    public void MalformedPathData_PassesThroughWhenThereIsNoTransform()
    {
        // Deliberate: with no transform the d attribute is never tokenized, so it reaches the
        // renderer exactly as written.
        IconGlyph glyph = SvgIconParser.Parse("<svg><path d=\"M 0,0 W 3\" /></svg>");

        Assert.Equal("M 0,0 W 3", glyph.Layers[0].PathData);
    }
}
