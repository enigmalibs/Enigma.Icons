using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Enigma.Icons.Generator;

/// <summary>
/// Renders <c>PhosphorIcon.g.cs</c> — the enum, one member per icon, with the per-member
/// <c>&lt;summary&gt;</c> that keeps CS1591 satisfied under SPEC §2.8.
/// </summary>
internal static class EnumRenderer
{
    /// <summary>Renders the enum file.</summary>
    /// <param name="names">The icon names in <c>.dat</c> order — ordinal by kebab name.</param>
    /// <returns>UTF-8 bytes, no BOM, LF line endings, final newline.</returns>
    internal static byte[] Render(IReadOnlyList<string> names)
    {
        var builder = new StringBuilder(128 * 1024);

        builder.Append(OutputTarget.GeneratedFileHeader);

        // No using directives at all: the file needs none, and SPEC §2.2 forbids implicit ones.
        builder.Append("""

            namespace Enigma.Icons.Phosphor;


            """);

        // The counts come from the data, never from a literal, so the sentence cannot drift from
        // what was actually generated.
        builder.Append("/// <summary>The Phosphor icon set. ")
            .Append(names.Count.ToString("N0", CultureInfo.InvariantCulture))
            .Append(" icons, ")
            .Append(Weights.All.Length.ToString(CultureInfo.InvariantCulture))
            .Append(" weights.</summary>\n");

        // SPEC §8.4: the ordinals are positional, not an ABI. The caveat lives in the type's own
        // documentation because that is where a caller about to persist (int)icon will see it.
        builder.Append("""
            /// <remarks>
            /// <para>
            /// The numeric value of a member is <b>positional</b> - it is the member's index in the upstream,
            /// ordinal-sorted icon-name list - so it is not a stable ABI. An artwork refresh that adds or
            /// removes a single icon renumbers every member after it.
            /// </para>
            /// <para>
            /// Never persist or transmit the numeric value of a <see cref="PhosphorIcon"/>. Persist
            /// <see cref="PhosphorIconNames.ToKebabCase(PhosphorIcon)"/> and read it back with
            /// <see cref="PhosphorIconNames.TryParse(string, out PhosphorIcon)"/>: the names are the
            /// contract, the ordinals are not.
            /// </para>
            /// </remarks>
            public enum PhosphorIcon
            {

            """);

        foreach (string name in names)
        {
            builder.Append("    /// <summary>The \"").Append(name).Append("\" icon.</summary>\n")
                .Append("    ").Append(NameMapper.ToPascalCase(name)).Append(",\n");
        }

        builder.Append("}\n");

        return OutputTarget.ToUtf8(builder.ToString());
    }
}
