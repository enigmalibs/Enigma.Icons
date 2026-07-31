using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using Xunit;

namespace Enigma.Icons.UnitTests;

public sealed class SvgIconParserErrorTests
{
    [Fact]
    public void Parse_RejectsANullString()
        => Assert.Throws<ArgumentNullException>(() => SvgIconParser.Parse((string)null!));

    [Fact]
    public void Parse_RejectsANullStream()
        => Assert.Throws<ArgumentNullException>(() => SvgIconParser.Parse((Stream)null!));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\t ")]
    public void Parse_RejectsAnEmptyDocument(string svg)
        => Assert.Throws<SvgParseException>(() => SvgIconParser.Parse(svg));

    [Fact]
    public void Parse_RejectsAnEmptyStream()
    {
        using var stream = new MemoryStream(Array.Empty<byte>());

        Assert.Throws<SvgParseException>(() => SvgIconParser.Parse(stream));
    }

    [Fact]
    public void Parse_WrapsAnXmlExceptionForInputThatIsNotXml()
    {
        SvgParseException error = Assert.Throws<SvgParseException>(
            () => SvgIconParser.Parse("this is definitely not xml"));

        Assert.IsType<XmlException>(error.InnerException);
    }

    [Fact]
    public void Parse_RejectsUnclosedMarkup()
        => Assert.Throws<SvgParseException>(() => SvgIconParser.Parse("<svg><path d=\"M 0,0\" ></svg>"));

    [Fact]
    public void Parse_RejectsARootThatIsNotSvg()
    {
        SvgParseException error = Assert.Throws<SvgParseException>(
            () => SvgIconParser.Parse("<html><path d=\"M 0,0\" /></html>"));

        Assert.Contains("<html>", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_RejectsAnSvgWithNoPaintableChild()
    {
        SvgParseException error = Assert.Throws<SvgParseException>(() => SvgIconParser.Parse("<svg></svg>"));

        Assert.Contains("no paintable element", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("linearGradient")]
    [InlineData("radialGradient")]
    [InlineData("clipPath")]
    [InlineData("mask")]
    [InlineData("defs")]
    [InlineData("use")]
    [InlineData("symbol")]
    [InlineData("text")]
    [InlineData("image")]
    [InlineData("filter")]
    public void Parse_NamesTheUnsupportedConstructThatHoldsTheOnlyContent(string element)
    {
        SvgParseException error = Assert.Throws<SvgParseException>(
            () => SvgIconParser.Parse(
                "<svg><" + element + "><path d=\"M 0,0\" /></" + element + "></svg>"));

        Assert.Contains("<" + element + ">", error.Message, StringComparison.Ordinal);
        Assert.Contains("README", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_DoesNotNameTitleOrDescAsUnsupported()
    {
        SvgParseException error = Assert.Throws<SvgParseException>(
            () => SvgIconParser.Parse("<svg><title>Nothing here</title><desc>Really</desc></svg>"));

        Assert.DoesNotContain("title", error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("desc", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_KeepsTheGeometryOfAShapeCarryingAStyleAttribute()
    {
        // The CSS-expressed paint is lost, but the geometry is still usable, so the layer stands.
        IconGlyph glyph = SvgIconParser.Parse("<svg><path style=\"fill:red\" d=\"M 0,0\" /></svg>");

        Assert.Single(glyph.Layers);
    }

    [Fact]
    public void Parse_RejectsAStringLargerThanTheLimit()
    {
        int original = SvgIconParser.MaxDocumentBytes;
        try
        {
            SvgIconParser.MaxDocumentBytes = 64;

            SvgParseException error = Assert.Throws<SvgParseException>(
                () => SvgIconParser.Parse("<svg><path d=\"" + new string('0', 200) + "\" /></svg>"));

            Assert.Contains("MaxDocumentBytes", error.Message, StringComparison.Ordinal);
        }
        finally
        {
            SvgIconParser.MaxDocumentBytes = original;
        }
    }

    [Fact]
    public void Parse_ReportsTheTrueUtf8ByteCountOfAnOverLimitString()
    {
        // Every 'é' is one UTF-16 code unit but two UTF-8 bytes, so a code-unit count would
        // understate the size the message claims to state.
        string svg = "<svg><path d=\"" + new string('é', 200) + "\" /></svg>";
        int bytes = Encoding.UTF8.GetByteCount(svg);
        int original = SvgIconParser.MaxDocumentBytes;
        try
        {
            SvgIconParser.MaxDocumentBytes = 64;

            SvgParseException error = Assert.Throws<SvgParseException>(() => SvgIconParser.Parse(svg));

            Assert.Contains(bytes.ToString(CultureInfo.InvariantCulture), error.Message, StringComparison.Ordinal);
            Assert.DoesNotContain(svg.Length.ToString(CultureInfo.InvariantCulture), error.Message, StringComparison.Ordinal);
        }
        finally
        {
            SvgIconParser.MaxDocumentBytes = original;
        }
    }

    [Fact]
    public void Parse_RejectsAStreamLargerThanTheLimit()
    {
        int original = SvgIconParser.MaxDocumentBytes;
        try
        {
            SvgIconParser.MaxDocumentBytes = 64;
            byte[] bytes = Encoding.UTF8.GetBytes("<svg><path d=\"" + new string('0', 200) + "\" /></svg>");
            using var stream = new MemoryStream(bytes);

            SvgParseException error = Assert.Throws<SvgParseException>(() => SvgIconParser.Parse(stream));

            Assert.Contains("MaxDocumentBytes", error.Message, StringComparison.Ordinal);
        }
        finally
        {
            SvgIconParser.MaxDocumentBytes = original;
        }
    }

    [Fact]
    public void Parse_AcceptsADocumentExactlyAtTheLimit()
    {
        const string Svg = "<svg><path d=\"M 0,0\" /></svg>";
        int original = SvgIconParser.MaxDocumentBytes;
        try
        {
            SvgIconParser.MaxDocumentBytes = Encoding.UTF8.GetByteCount(Svg);

            Assert.Single(SvgIconParser.Parse(Svg).Layers);
        }
        finally
        {
            SvgIconParser.MaxDocumentBytes = original;
        }
    }

    [Fact]
    public void MaxDocumentBytes_RejectsANonPositiveLimit()
    {
        int original = SvgIconParser.MaxDocumentBytes;
        try
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => SvgIconParser.MaxDocumentBytes = 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => SvgIconParser.MaxDocumentBytes = -1);
            Assert.Equal(original, SvgIconParser.MaxDocumentBytes);
        }
        finally
        {
            SvgIconParser.MaxDocumentBytes = original;
        }
    }

    [Fact]
    public void MaxDocumentBytes_DefaultsToOneMebibyte()
        => Assert.Equal(1024 * 1024, SvgIconParser.MaxDocumentBytes);

    [Fact]
    public void Parse_ReadsTheStreamButDoesNotDisposeIt()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("<svg><path d=\"M 0,0\" /></svg>");
        using var stream = new MemoryStream(bytes);

        Assert.Single(SvgIconParser.Parse(stream).Layers);

        // Still usable: reading a disposed MemoryStream would throw.
        stream.Position = 0;
        Assert.Equal(bytes.Length, stream.Length);
    }

    [Fact]
    public void Parse_HonoursAByteOrderMarkOnAStream()
    {
        byte[] bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)
            .GetBytes("<svg><path d=\"M 0,0\" /></svg>");
        byte[] withPreamble = new byte[3 + bytes.Length];
        Encoding.UTF8.GetPreamble().CopyTo(withPreamble, 0);
        bytes.CopyTo(withPreamble, 3);

        using var stream = new MemoryStream(withPreamble);

        Assert.Single(SvgIconParser.Parse(stream).Layers);
    }
}
