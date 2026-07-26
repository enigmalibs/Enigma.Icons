using System;
using System.Collections.Generic;
using Enigma.Icons.Phosphor.UnitTests.TestSupport;
using Xunit;

namespace Enigma.Icons.Phosphor.UnitTests;

/// <summary>
/// SPEC §12.2 — the full-corpus integrity pass. Everything downstream trusts 3.77 MiB of committed
/// generated data that no human reviews line by line; these assertions are what make a bad
/// regeneration impossible to ship silently.
/// </summary>
/// <remarks>
/// Resolving 9,072 glyphs costs a second or two. That is accepted (SPEC §15 forbids timing-based
/// tests) — the corpus is not sampled.
/// </remarks>
public sealed class CorpusIntegrityTests
{
    private const int IconCount = 1512;

    [Fact]
    public void EveryIconInEveryWeight_ResolvesToANonEmptyGlyph()
    {
        var violations = new ViolationLog();
        int resolved = 0;

        foreach (PhosphorWeight weight in DatResource.Weights)
        {
            for (int i = 0; i < IconCount; i++)
            {
                var icon = (PhosphorIcon)i;
                string name = PhosphorIconNames.ToKebabCase(icon);

                if (!PhosphorIconSet.Instance.TryGetGlyph(icon, weight, out IconGlyph? glyph) || glyph is null)
                {
                    violations.Add("{0}/{1}: did not resolve.", weight, name);
                    continue;
                }

                resolved++;

                if (glyph.Layers.Count == 0)
                {
                    violations.Add("{0}/{1}: no layers.", weight, name);
                    continue;
                }

                for (int layer = 0; layer < glyph.Layers.Count; layer++)
                {
                    if (string.IsNullOrWhiteSpace(glyph.Layers[layer].PathData))
                    {
                        violations.Add("{0}/{1} layer {2}: empty path data.", weight, name, layer);
                    }
                }

                if (glyph.ViewBox != IconViewBox.Default)
                {
                    violations.Add("{0}/{1}: view box {2}, expected {3}.", weight, name, glyph.ViewBox, IconViewBox.Default);
                }
            }
        }

        violations.AssertEmpty("Corpus glyph resolution");
        Assert.Equal(IconCount * DatResource.Weights.Length, resolved);
    }

    [Fact]
    public void EveryWeightTable_HoldsExactlyTheEnumsIconNames()
    {
        // Read the resources directly rather than through the set: resolving by enum member could
        // only ever prove the enum agrees with itself. This is what catches a .dat and a generated
        // enum that have drifted apart.
        var expected = new HashSet<string>(PhosphorIconNames.All, StringComparer.Ordinal);
        var violations = new ViolationLog();

        foreach (string resource in DatResource.Names)
        {
            List<string> names = DatResource.IconNamesOf(resource);

            if (names.Count != IconCount)
            {
                violations.Add("{0}: {1} icons, expected {2}.", resource, names.Count, IconCount);
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string name in names)
            {
                if (!seen.Add(name))
                {
                    violations.Add("{0}: duplicate icon name '{1}'.", resource, name);
                }

                if (!expected.Contains(name))
                {
                    violations.Add("{0}: '{1}' is not a PhosphorIcon member.", resource, name);
                }
            }

            foreach (string name in expected)
            {
                if (!seen.Contains(name))
                {
                    violations.Add("{0}: '{1}' is a PhosphorIcon member but is missing from the table.", resource, name);
                }
            }
        }

        violations.AssertEmpty("Weight tables versus the generated enum");
    }

    [Fact]
    public void EveryWeightTable_IsSortedOrdinally()
    {
        // The sort order is what makes an artwork refresh a reviewable diff (SPEC §7.2).
        var violations = new ViolationLog();

        foreach (string resource in DatResource.Names)
        {
            List<string> names = DatResource.IconNamesOf(resource);

            for (int i = 1; i < names.Count; i++)
            {
                if (string.CompareOrdinal(names[i - 1], names[i]) >= 0)
                {
                    violations.Add("{0}: '{1}' precedes '{2}'.", resource, names[i - 1], names[i]);
                }
            }
        }

        violations.AssertEmpty("Resource sort order");
    }
}
