using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Enigma.Icons.Generator;

/// <summary>
/// Enumerates the six weight directories, derives icon names, validates every file, and asserts the
/// cross-weight invariants (SPEC §8.3 steps 1-4).
/// </summary>
internal static class CorpusLoader
{
    private const int MaxListedDifferences = 25;

    /// <summary>
    /// Loads the whole corpus. Every failure is a <see cref="GeneratorException"/> naming the
    /// offending weight or file — never an unhandled exception.
    /// </summary>
    /// <param name="inputDirectory">The directory holding the six weight subdirectories.</param>
    /// <param name="vocabulary">Receives the unexpected-vocabulary tally.</param>
    /// <returns>One corpus per weight, in <see cref="Weights.All"/> order, icons sorted ordinal by name.</returns>
    internal static IReadOnlyList<WeightCorpus> Load(string inputDirectory, Vocabulary vocabulary)
    {
        var corpora = new List<WeightCorpus>(Weights.All.Length);

        foreach (string weight in Weights.All)
        {
            corpora.Add(LoadWeight(inputDirectory, weight, vocabulary));
        }

        AssertNameSetsMatch(corpora);
        return corpora;
    }

    private static WeightCorpus LoadWeight(string inputDirectory, string weight, Vocabulary vocabulary)
    {
        string weightDirectory = Path.Combine(inputDirectory, weight);
        if (!Directory.Exists(weightDirectory))
        {
            throw new GeneratorException("the --input directory has no \"" + weight + "\" subdirectory.");
        }

        string[] files = Directory.GetFiles(weightDirectory, "*.svg", SearchOption.TopDirectoryOnly);
        for (int i = 0; i < files.Length; i++)
        {
            files[i] = Path.GetFileName(files[i]);
        }

        // Ordinal everywhere, never a culture-sensitive comparer, so enumeration is identical on
        // every machine and in every locale.
        Array.Sort(files, StringComparer.Ordinal);

        if (files.Length == 0)
        {
            throw new GeneratorException("the \"" + weight + "\" subdirectory contains no .svg file.");
        }

        string suffix = Weights.FileSuffix(weight) + ".svg";
        var icons = new List<IconRecord>(files.Length);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        string? viewBox = null;
        string? viewBoxSource = null;

        foreach (string file in files)
        {
            string displayName = weight + "/" + file;

            if (!file.EndsWith(suffix, StringComparison.Ordinal) || file.Length == suffix.Length)
            {
                throw new GeneratorException(
                    displayName + ": the filename does not end with \"" + suffix + "\", so no icon name can be derived.");
            }

            string name = file.Substring(0, file.Length - suffix.Length);

            // All 1,512 upstream names match ^[a-z-]+$, which is exactly what makes SPEC §8.4's
            // kebab <-> Pascal mapping mechanical and lossless. A name outside that alphabet would
            // silently produce a colliding or invalid enum member, so it is fatal here instead.
            if (!NameMapper.IsMappableKebabName(name))
            {
                throw new GeneratorException(
                    displayName + ": the icon name \"" + name + "\" is not lower-case kebab-case, so the SPEC §8.4 name mapping is not lossless for it.");
            }

            if (!seen.Add(name))
            {
                throw new GeneratorException(displayName + ": the icon name \"" + name + "\" is not unique within the weight.");
            }

            List<LayerRecord> layers = SvgPathReader.Read(
                Path.Combine(weightDirectory, file),
                displayName,
                vocabulary,
                out string fileViewBox);

            // SPEC §7.2's header carries ONE view box for the whole weight file, so the format
            // cannot represent a weight whose icons disagree. Read it rather than hard-coding
            // "0 0 256 256" (SPEC §7.4.3 guarantees agreement today), and refuse to pick a winner.
            if (viewBox is null)
            {
                viewBox = fileViewBox;
                viewBoxSource = displayName;
            }
            else if (!string.Equals(viewBox, fileViewBox, StringComparison.Ordinal))
            {
                throw new GeneratorException(
                    displayName + ": its viewBox \"" + fileViewBox + "\" differs from \"" + viewBox + "\" in " + viewBoxSource
                    + "; the v1 .dat format carries one view box per weight.");
            }

            icons.Add(new IconRecord(name, layers));
        }

        // The .dat line order and therefore the enum ordinals follow the NAME, not the filename:
        // for the five suffixed weights the two orders can differ (compare "a-bold.svg" against
        // "a-b-bold.svg" with the names "a" and "a-b").
        icons.Sort(static (left, right) => string.CompareOrdinal(left.Name, right.Name));

        return new WeightCorpus(weight, viewBox ?? string.Empty, icons);
    }

    private static void AssertNameSetsMatch(IReadOnlyList<WeightCorpus> corpora)
    {
        WeightCorpus reference = corpora[0];
        var referenceNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (IconRecord icon in reference.Icons)
        {
            referenceNames.Add(icon.Name);
        }

        var report = new StringBuilder();

        for (int i = 1; i < corpora.Count; i++)
        {
            WeightCorpus current = corpora[i];
            var currentNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (IconRecord icon in current.Icons)
            {
                currentNames.Add(icon.Name);
            }

            var missing = new List<string>();
            foreach (IconRecord icon in reference.Icons)
            {
                if (!currentNames.Contains(icon.Name))
                {
                    missing.Add(icon.Name);
                }
            }

            var extra = new List<string>();
            foreach (IconRecord icon in current.Icons)
            {
                if (!referenceNames.Contains(icon.Name))
                {
                    extra.Add(icon.Name);
                }
            }

            if (missing.Count == 0 && extra.Count == 0)
            {
                continue;
            }

            report.Append(CultureInfo.InvariantCulture, $"\n  {current.Weight}: {missing.Count} missing, {extra.Count} extra");
            AppendNames(report, "missing from " + current.Weight, missing);
            AppendNames(report, "extra in " + current.Weight, extra);
        }

        if (report.Length > 0)
        {
            throw new GeneratorException(
                "the six weights do not carry identical icon name sets (SPEC §7.4.6), measured against \""
                + reference.Weight + "\":" + report);
        }
    }

    private static void AppendNames(StringBuilder report, string label, List<string> names)
    {
        if (names.Count == 0)
        {
            return;
        }

        report.Append("\n    ").Append(label).Append(": ");

        int listed = Math.Min(names.Count, MaxListedDifferences);
        for (int i = 0; i < listed; i++)
        {
            if (i > 0)
            {
                report.Append(", ");
            }

            report.Append(names[i]);
        }

        if (names.Count > listed)
        {
            report.Append(CultureInfo.InvariantCulture, $" (+{names.Count - listed} more)");
        }
    }
}

/// <summary>One <c>&lt;path&gt;</c>: its verbatim <c>d</c> and its effective opacity.</summary>
/// <param name="PathData">The <c>d</c> attribute value, byte-identical to upstream.</param>
/// <param name="Opacity">The <c>opacity</c> attribute value, or 1 when the attribute is absent.</param>
internal sealed record LayerRecord(string PathData, double Opacity);

/// <summary>One icon of one weight: its kebab-case name and its layers in paint order.</summary>
/// <param name="Name">The upstream kebab-case name, with the weight suffix removed.</param>
/// <param name="Layers">The layers, index 0 being the bottom-most.</param>
internal sealed record IconRecord(string Name, IReadOnlyList<LayerRecord> Layers);

/// <summary>One whole weight: everything a <c>.dat</c> file needs.</summary>
/// <param name="Weight">The weight name, e.g. <c>duotone</c>.</param>
/// <param name="ViewBox">The view box shared by every file of the weight, verbatim.</param>
/// <param name="Icons">The icons, ordinal-sorted by name.</param>
internal sealed record WeightCorpus(string Weight, string ViewBox, IReadOnlyList<IconRecord> Icons);

/// <summary>
/// A fatal input or validation failure. Carries a message that names the offending weight or file
/// and is reported as exit code 2 — no unhandled exception ever reaches the console.
/// </summary>
internal sealed class GeneratorException : Exception
{
    /// <summary>Creates the exception with a diagnostic message.</summary>
    internal GeneratorException(string message)
        : base(message)
    {
    }
}
