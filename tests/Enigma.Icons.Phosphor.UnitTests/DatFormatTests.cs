using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Enigma.Icons.Phosphor.UnitTests.TestSupport;
using Xunit;

namespace Enigma.Icons.Phosphor.UnitTests;

/// <summary>
/// SPEC §7.2 — the <c>v1</c> resource format, asserted against the six committed resources read
/// directly, plus the reader's negative cases over synthetic streams (a corrupt header cannot be
/// reproduced through the six real, valid resources).
/// </summary>
public sealed class DatFormatTests
{
    private const string ValidHeader = "v1\tregular\t0 0 256 256\t1\n";

    private const string ResourceName = "Enigma.Icons.Phosphor.Assets.phosphor.regular.dat";

    [Fact]
    public void EveryResource_DeclaresTheSpecifiedHeader()
    {
        var violations = new ViolationLog();

        foreach (PhosphorWeight weight in DatResource.Weights)
        {
            string name = DatResource.NameOf(weight);
            string[] fields = DatResource.ReadLines(name)[0].Split('\t');

            if (fields.Length != 4)
            {
                violations.Add("{0}: header has {1} fields, expected 4.", name, fields.Length);
                continue;
            }

            if (fields[0] != "v1")
            {
                violations.Add("{0}: version '{1}', expected 'v1'.", name, fields[0]);
            }

            if (fields[1] != DatResource.WeightNames[(int)weight])
            {
                violations.Add("{0}: weight '{1}', expected '{2}'.", name, fields[1], DatResource.WeightNames[(int)weight]);
            }

            if (fields[2] != "0 0 256 256")
            {
                violations.Add("{0}: view box '{1}', expected '0 0 256 256'.", name, fields[2]);
            }

            if (fields[3] != "1512")
            {
                violations.Add("{0}: icon count '{1}', expected '1512'.", name, fields[3]);
            }
        }

        violations.AssertEmpty("Resource headers");
    }

    [Fact]
    public void EveryResource_HasAsManyDataLinesAsItsHeaderDeclares()
    {
        var violations = new ViolationLog();

        foreach (string name in DatResource.Names)
        {
            string[] lines = DatResource.ReadLines(name);
            string[] fields = lines[0].Split('\t');
            int declared = int.Parse(fields[3], CultureInfo.InvariantCulture);

            if (lines.Length - 1 != declared)
            {
                violations.Add("{0}: {1} data lines, header declares {2}.", name, lines.Length - 1, declared);
            }
        }

        violations.AssertEmpty("Declared-versus-actual icon counts");
    }

    [Fact]
    public void EveryResource_IsUtf8WithoutBomAndEndsWithASingleNewline()
    {
        var violations = new ViolationLog();

        foreach (string name in DatResource.Names)
        {
            byte[] bytes = DatResource.ReadAllBytes(name);

            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                violations.Add("{0}: starts with a UTF-8 BOM.", name);
            }

            if (bytes.Length == 0 || bytes[bytes.Length - 1] != (byte)'\n')
            {
                violations.Add("{0}: does not end with a newline.", name);
            }

            if (bytes.Length >= 2 && bytes[bytes.Length - 2] == (byte)'\n')
            {
                violations.Add("{0}: ends with a blank line.", name);
            }

            int carriageReturns = 0;
            foreach (byte b in bytes)
            {
                if (b == (byte)'\r')
                {
                    carriageReturns++;
                }
            }

            if (carriageReturns != 0)
            {
                violations.Add("{0}: contains {1} CR byte(s); the format is LF-only.", name, carriageReturns);
            }
        }

        violations.AssertEmpty("Resource encoding and line endings");
    }

    [Fact]
    public void EveryResource_HasNoBlankOrMalformedDataLine()
    {
        var violations = new ViolationLog();

        foreach (string name in DatResource.Names)
        {
            string[] lines = DatResource.ReadLines(name);

            for (int i = 1; i < lines.Length; i++)
            {
                int tab = lines[i].IndexOf('\t');

                if (tab <= 0 || tab == lines[i].Length - 1)
                {
                    violations.Add("{0} line {1}: no name/layer split at a TAB.", name, i + 1);
                }
            }
        }

        violations.AssertEmpty("Malformed data lines");
    }

    [Fact]
    public void Reader_AcceptsAWellFormedTable()
    {
        Dictionary<string, string> table = Read(ValidHeader + "acorn\tM0,0Z\n");

        Assert.Single(table);
        Assert.Equal("M0,0Z", table["acorn"]);
    }

    [Fact]
    public void Reader_AcceptsMultipleLayerFields()
    {
        Dictionary<string, string> table = Read(
            "v1\tregular\t0 0 256 256\t2\nacorn\t@0.2:M0,0Z\tM1,1Z\naddress-book\tM2,2Z\n");

        Assert.Equal(2, table.Count);
        Assert.Equal("@0.2:M0,0Z\tM1,1Z", table["acorn"]);
    }

    [Theory]
    // An unrecognized format version — the primary tripwire.
    [InlineData("v2\tregular\t0 0 256 256\t1\nacorn\tM0,0Z\n", "version")]
    // A .dat embedded under the wrong name: this one is bold artwork served as regular.
    [InlineData("v1\tbold\t0 0 256 256\t1\nacorn\tM0,0Z\n", "weight name")]
    [InlineData("v1\tregular\t0 0 24 24\t1\nacorn\tM0,0Z\n", "view box")]
    [InlineData("v1\tregular\t0 0 256 256\tmany\nacorn\tM0,0Z\n", "icon count")]
    [InlineData("v1\tregular\t0 0 256 256\t-1\nacorn\tM0,0Z\n", "icon count")]
    // Truncated or half-written: the header promises more icons than the body carries.
    [InlineData("v1\tregular\t0 0 256 256\t2\nacorn\tM0,0Z\n", "icon count")]
    [InlineData("v1\tregular\t0 0 256 256\n", "header")]
    [InlineData("", "header")]
    [InlineData("v1\tregular\t0 0 256 256\t1\nacorn\n", "icon line")]
    [InlineData("v1\tregular\t0 0 256 256\t1\n\tM0,0Z\n", "icon line")]
    [InlineData("v1\tregular\t0 0 256 256\t1\nacorn\t\n", "icon line")]
    [InlineData("v1\tregular\t0 0 256 256\t2\nacorn\tM0,0Z\nacorn\tM1,1Z\n", "icon name")]
    [InlineData("v1\tregular\t0 0 256 256\t2\nacorn\tM0,0Z\n\naddress-book\tM1,1Z\n", "body")]
    public void Reader_RejectsACorruptTable(string content, string expectedFieldInMessage)
    {
        InvalidDataException ex = Assert.Throws<InvalidDataException>(() => Read(content));

        Assert.Contains("phosphor.regular.dat", ex.Message, StringComparison.Ordinal);
        Assert.Contains(expectedFieldInMessage, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Reader_ToleratesASingleTrailingBlankLine()
    {
        Dictionary<string, string> table = Read(ValidHeader + "acorn\tM0,0Z\n\n");

        Assert.Single(table);
    }

    [Fact]
    public void GlyphMaterialization_ReadsPathDataAndOpacity()
    {
        IconGlyph glyph = PhosphorIconSet.BuildGlyph("@0.2:M0,0Z\tM1,1Z", "acorn", ResourceName);

        Assert.Equal(2, glyph.Layers.Count);
        Assert.Equal("M0,0Z", glyph.Layers[0].PathData);
        Assert.Equal(0.2, glyph.Layers[0].Opacity, tolerance: 1e-9);
        Assert.Equal("M1,1Z", glyph.Layers[1].PathData);
        Assert.Equal(1.0, glyph.Layers[1].Opacity, tolerance: 1e-9);

        // The .dat carries no paint information, so a layer inherits the renderer's brush.
        Assert.Null(glyph.Layers[0].Fill);
        Assert.Null(glyph.Layers[0].Stroke);
        Assert.Equal(IconFillRule.NonZero, glyph.Layers[0].FillRule);
        Assert.Equal(IconViewBox.Default, glyph.ViewBox);
    }

    [Theory]
    // A layer field whose opacity prefix is unparseable, out of range, or has no path data after it.
    [InlineData("@:M0,0Z")]
    [InlineData("@abc:M0,0Z")]
    [InlineData("@1.5:M0,0Z")]
    [InlineData("@0,2:M0,0Z")]
    [InlineData("@0.2:")]
    [InlineData("@0.2")]
    [InlineData("M0,0Z\t\tM1,1Z")]
    public void GlyphMaterialization_RejectsAMalformedLayerField(string remainder)
    {
        InvalidDataException ex = Assert.Throws<InvalidDataException>(
            () => PhosphorIconSet.BuildGlyph(remainder, "acorn", ResourceName));

        Assert.Contains("acorn", ex.Message, StringComparison.Ordinal);
        Assert.Contains("phosphor.regular.dat", ex.Message, StringComparison.Ordinal);
    }

    private static Dictionary<string, string> Read(string content)
    {
        using var stream = new MemoryStream(new UTF8Encoding(false).GetBytes(content));

        return PhosphorIconSet.ReadTable(stream, "regular", ResourceName);
    }
}
