using System;
using System.Collections.Generic;
using Enigma.Icons.Phosphor.UnitTests.TestSupport;
using Xunit;

namespace Enigma.Icons.Phosphor.UnitTests;

/// <summary>
/// SPEC §7.4 — the upstream ground truth, encoded here as the <i>expected</i> value so that an
/// artwork refresh which changes the shape of the corpus fails loudly instead of drifting in.
/// </summary>
/// <remarks>
/// Every expectation below is a literal written in this file. None of it is derived from the asset
/// under test — a test that reads its expectation out of the data it is checking asserts nothing.
/// </remarks>
public sealed class LayerShapeTests
{
    private const int IconCount = 1512;

    /// <summary>SPEC §7.4 fact 9: the whole corpus holds exactly this many layers.</summary>
    private const int TotalLayerCount = 10592;

    private const double Tolerance = 1e-9;

    /// <summary>SPEC §7.4 fact 8: the only fill icons with more than one layer, and their counts.</summary>
    private static readonly Dictionary<string, int> FillOutliers = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        ["bookmarks-simple"] = 2,
        ["crane-tower"] = 2,
        ["hard-drives"] = 2,
        ["lasso"] = 2,
        ["music-notes-minus"] = 3,
        ["speaker-simple-x"] = 2,
        ["stack"] = 3,
        ["stack-simple"] = 2,
    };

    /// <summary>SPEC §7.4 fact 8: the only duotone icons without a tinted backing layer.</summary>
    private static readonly HashSet<string> DuotoneOutliers = new HashSet<string>(StringComparer.Ordinal)
    {
        "cell-signal-none",
        "wifi-none",
    };

    [Theory]
    [InlineData(PhosphorWeight.Thin)]
    [InlineData(PhosphorWeight.Light)]
    [InlineData(PhosphorWeight.Regular)]
    [InlineData(PhosphorWeight.Bold)]
    public void StrokedWeights_HaveExactlyOneFullyOpaqueLayerPerIcon(PhosphorWeight weight)
    {
        var violations = new ViolationLog();

        for (int i = 0; i < IconCount; i++)
        {
            string name = PhosphorIconNames.All[i];
            IconGlyph glyph = PhosphorIconSet.Instance.GetGlyph((PhosphorIcon)i, weight);

            if (glyph.Layers.Count != 1)
            {
                violations.Add("{0}: {1} layers, expected 1.", name, glyph.Layers.Count);
                continue;
            }

            if (Math.Abs(glyph.Layers[0].Opacity - 1.0) > Tolerance)
            {
                violations.Add("{0}: opacity {1}, expected 1.", name, glyph.Layers[0].Opacity);
            }
        }

        violations.AssertEmpty($"Layer shape of weight {weight}");
    }

    [Fact]
    public void Fill_HasOneLayerPerIconExceptTheEightNamedOutliers()
    {
        var violations = new ViolationLog();
        var seenOutliers = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < IconCount; i++)
        {
            string name = PhosphorIconNames.All[i];
            IconGlyph glyph = PhosphorIconSet.Instance.GetGlyph((PhosphorIcon)i, PhosphorWeight.Fill);

            int expected = 1;
            if (FillOutliers.TryGetValue(name, out int outlierCount))
            {
                expected = outlierCount;
                seenOutliers.Add(name);
            }

            if (glyph.Layers.Count != expected)
            {
                violations.Add("{0}: {1} layers, expected {2}.", name, glyph.Layers.Count, expected);
            }

            foreach (IconLayer layer in glyph.Layers)
            {
                // No fill layer is translucent — the tinting is duotone's job, not fill's.
                if (layer.Opacity < 1.0 - Tolerance)
                {
                    violations.Add("{0}: a layer carries opacity {1}, expected 1.", name, layer.Opacity);
                }
            }
        }

        foreach (string name in FillOutliers.Keys)
        {
            if (!seenOutliers.Contains(name))
            {
                violations.Add("{0}: the named multi-layer fill icon is missing from the corpus.", name);
            }
        }

        violations.AssertEmpty("Layer shape of weight Fill");
    }

    [Fact]
    public void Duotone_HasTwoLayersWithATintedBackingExceptTheTwoNamedOutliers()
    {
        var violations = new ViolationLog();
        var seenOutliers = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < IconCount; i++)
        {
            string name = PhosphorIconNames.All[i];
            IconGlyph glyph = PhosphorIconSet.Instance.GetGlyph((PhosphorIcon)i, PhosphorWeight.Duotone);

            if (DuotoneOutliers.Contains(name))
            {
                seenOutliers.Add(name);

                if (glyph.Layers.Count != 1)
                {
                    violations.Add("{0}: {1} layers, expected 1.", name, glyph.Layers.Count);
                }
                else if (Math.Abs(glyph.Layers[0].Opacity - 1.0) > Tolerance)
                {
                    violations.Add("{0}: opacity {1}, expected 1.", name, glyph.Layers[0].Opacity);
                }

                continue;
            }

            if (glyph.Layers.Count != 2)
            {
                violations.Add("{0}: {1} layers, expected 2.", name, glyph.Layers.Count);
                continue;
            }

            // Index 0 is the bottom-most layer: duotone's tinted backing shape.
            if (Math.Abs(glyph.Layers[0].Opacity - 0.2) > Tolerance)
            {
                violations.Add("{0}: backing-layer opacity {1}, expected 0.2.", name, glyph.Layers[0].Opacity);
            }

            if (Math.Abs(glyph.Layers[1].Opacity - 1.0) > Tolerance)
            {
                violations.Add("{0}: foreground-layer opacity {1}, expected 1.", name, glyph.Layers[1].Opacity);
            }
        }

        foreach (string name in DuotoneOutliers)
        {
            if (!seenOutliers.Contains(name))
            {
                violations.Add("{0}: the named single-layer duotone icon is missing from the corpus.", name);
            }
        }

        violations.AssertEmpty("Layer shape of weight Duotone");
    }

    [Fact]
    public void WholeCorpus_HoldsExactlyTheSpecifiedNumberOfLayers()
    {
        // One arithmetic assertion over all six weights, catching any drift the per-weight rules
        // miss: 9,072 one-per-icon + 10 extra in fill + 1,512 extra in duotone − 2 missing in
        // duotone = 10,592.
        int total = 0;

        foreach (PhosphorWeight weight in DatResource.Weights)
        {
            for (int i = 0; i < IconCount; i++)
            {
                total += PhosphorIconSet.Instance.GetGlyph((PhosphorIcon)i, weight).Layers.Count;
            }
        }

        Assert.Equal(TotalLayerCount, total);
    }
}
