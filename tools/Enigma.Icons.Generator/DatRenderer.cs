using System.Globalization;
using System.Text;

namespace Enigma.Icons.Generator;

/// <summary>
/// Renders one weight to the SPEC §7.2 <c>v1</c> resource format. Produces bytes only — it never
/// touches the filesystem, so generate mode and <c>--check</c> mode consume the very same output.
/// </summary>
internal static class DatRenderer
{
    private const char FieldSeparator = '\t';

    /// <summary>Renders the weight's <c>.dat</c> content.</summary>
    /// <param name="corpus">The weight to render.</param>
    /// <returns>UTF-8 bytes, no BOM, LF line endings, final newline.</returns>
    internal static byte[] Render(WeightCorpus corpus)
    {
        var builder = new StringBuilder(1024 * 1024);

        // Header: v1 <TAB> weight <TAB> viewBox <TAB> count. The count is the machine-readable
        // tripwire a reader uses to detect a corrupt or half-written resource (SPEC §7.2).
        builder.Append("v1")
            .Append(FieldSeparator)
            .Append(corpus.Weight)
            .Append(FieldSeparator)
            .Append(corpus.ViewBox)
            .Append(FieldSeparator)
            .Append(corpus.Icons.Count.ToString(CultureInfo.InvariantCulture))
            .Append('\n');

        foreach (IconRecord icon in corpus.Icons)
        {
            builder.Append(icon.Name);

            foreach (LayerRecord layer in icon.Layers)
            {
                builder.Append(FieldSeparator);

                if (layer.Opacity != 1d)
                {
                    // Parse-then-format, not a copy of the attribute text: SPEC §7.2 mandates the
                    // invariant, shortest round-trippable form, which normalizes a hypothetical
                    // "0.20" to "0.2". On net10.0 the plain ToString overload is that form.
                    builder.Append('@')
                        .Append(layer.Opacity.ToString(CultureInfo.InvariantCulture))
                        .Append(':');
                }

                // Verbatim — no re-flow, no re-rounding, no whitespace normalization, no command
                // re-ordering (SPEC §8.3). Every glyph stays byte-traceable to its source file.
                builder.Append(layer.PathData);
            }

            builder.Append('\n');
        }

        return OutputTarget.ToUtf8(builder.ToString());
    }
}
