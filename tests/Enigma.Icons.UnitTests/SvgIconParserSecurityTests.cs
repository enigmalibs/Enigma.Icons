using System;
using System.Globalization;
using System.IO;
using Xunit;

namespace Enigma.Icons.UnitTests;

public sealed class SvgIconParserSecurityTests
{
    [Fact]
    public void ExternalEntity_IsNeverResolved()
    {
        string sentinel = "ENIGMA-XXE-SENTINEL-" + Guid.NewGuid().ToString("N");
        string secret = Path.Combine(Path.GetTempPath(), "enigma-xxe-" + Guid.NewGuid().ToString("N") + ".txt");
        File.WriteAllText(secret, sentinel);

        try
        {
            string svg = string.Format(
                CultureInfo.InvariantCulture,
                "<?xml version=\"1.0\"?><!DOCTYPE svg [<!ENTITY xxe SYSTEM \"file://{0}\">]><svg><path d=\"&xxe;\" /></svg>",
                secret.Replace('\\', '/'));

            SvgParseException error = Assert.Throws<SvgParseException>(() => SvgIconParser.Parse(svg));

            // DtdProcessing.Prohibit rejects the DTD outright, so the entity is never even DEFINED.
            // The part that actually proves non-resolution: the file's content appears nowhere.
            Assert.DoesNotContain(sentinel, error.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(secret);
        }
    }

    [Fact]
    public void ExternalEntity_NegativeControlNeverReachesALayer()
    {
        // The variant that would succeed under a permissive reader: a valid path plus the entity.
        string sentinel = "ENIGMA-XXE-SENTINEL-" + Guid.NewGuid().ToString("N");
        string secret = Path.Combine(Path.GetTempPath(), "enigma-xxe-" + Guid.NewGuid().ToString("N") + ".txt");
        File.WriteAllText(secret, sentinel);

        try
        {
            string svg = string.Format(
                CultureInfo.InvariantCulture,
                "<?xml version=\"1.0\"?><!DOCTYPE svg [<!ENTITY xxe SYSTEM \"file://{0}\">]><svg><path d=\"M 0,0\" /><path d=\"&xxe;\" /></svg>",
                secret.Replace('\\', '/'));

            IconGlyph? glyph = null;
            try
            {
                glyph = SvgIconParser.Parse(svg);
            }
            catch (SvgParseException error)
            {
                Assert.DoesNotContain(sentinel, error.ToString(), StringComparison.Ordinal);
            }

            if (glyph is not null)
            {
                foreach (IconLayer layer in glyph.Layers)
                {
                    Assert.DoesNotContain(sentinel, layer.PathData, StringComparison.Ordinal);
                }
            }
        }
        finally
        {
            File.Delete(secret);
        }
    }

    [Fact]
    public void BillionLaughs_FailsFastWithoutExpanding()
    {
        // No timing assertion (SPEC §15 forbids them) — the proof is structural: DTD processing is
        // prohibited, so the entities are never defined and there is nothing to expand. The
        // document is deliberately tiny; a permissive reader would blow up on it.
        const string Svg =
            "<?xml version=\"1.0\"?>" +
            "<!DOCTYPE lolz [" +
            "<!ENTITY lol \"lol\">" +
            "<!ENTITY lol1 \"&lol;&lol;&lol;&lol;&lol;&lol;&lol;&lol;&lol;&lol;\">" +
            "<!ENTITY lol2 \"&lol1;&lol1;&lol1;&lol1;&lol1;&lol1;&lol1;&lol1;&lol1;&lol1;\">" +
            "<!ENTITY lol3 \"&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;\">" +
            "<!ENTITY lol4 \"&lol3;&lol3;&lol3;&lol3;&lol3;&lol3;&lol3;&lol3;&lol3;&lol3;\">" +
            "<!ENTITY lol5 \"&lol4;&lol4;&lol4;&lol4;&lol4;&lol4;&lol4;&lol4;&lol4;&lol4;\">" +
            "<!ENTITY lol6 \"&lol5;&lol5;&lol5;&lol5;&lol5;&lol5;&lol5;&lol5;&lol5;&lol5;\">" +
            "<!ENTITY lol7 \"&lol6;&lol6;&lol6;&lol6;&lol6;&lol6;&lol6;&lol6;&lol6;&lol6;\">" +
            "<!ENTITY lol8 \"&lol7;&lol7;&lol7;&lol7;&lol7;&lol7;&lol7;&lol7;&lol7;&lol7;\">" +
            "<!ENTITY lol9 \"&lol8;&lol8;&lol8;&lol8;&lol8;&lol8;&lol8;&lol8;&lol8;&lol8;\">" +
            "]>" +
            "<svg><path d=\"&lol9;\" /></svg>";

        SvgParseException error = Assert.Throws<SvgParseException>(() => SvgIconParser.Parse(Svg));

        Assert.DoesNotContain("lollol", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ADocumentTypeDeclaration_IsRejectedEvenWithoutEntities()
        => Assert.Throws<SvgParseException>(
            () => SvgIconParser.Parse(
                "<?xml version=\"1.0\"?><!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 1.1//EN\" "
                + "\"http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd\"><svg><path d=\"M 0,0\" /></svg>"));
}
