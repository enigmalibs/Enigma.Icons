using System.Collections.Generic;
using System.Text;

namespace Enigma.Icons.Generator;

/// <summary>
/// Renders <c>PhosphorIconNames.g.cs</c> — SPEC §8.4's <c>ToKebabCase</c> / <c>TryParse</c> /
/// <c>All</c> surface, and the reason no <c>Enum.ToString</c> or <c>Enum.Parse</c> is ever called
/// (SPEC §2.11).
/// </summary>
/// <remarks>
/// The file is a fixed template plus one generated data block. Everything in the template is
/// <c>netstandard2.0</c>-clean, because FEATURE-3950 compiles it for
/// <c>netstandard2.0;net8.0;net10.0</c>.
/// </remarks>
internal static class NamesRenderer
{
    /// <summary>Renders the name-lookup file.</summary>
    /// <param name="names">The icon names in enum order — ordinal by kebab name.</param>
    /// <returns>UTF-8 bytes, no BOM, LF line endings, final newline.</returns>
    internal static byte[] Render(IReadOnlyList<string> names)
    {
        var builder = new StringBuilder(64 * 1024);

        builder.Append(OutputTarget.GeneratedFileHeader);

        builder.Append("""

            using System;
            using System.Collections.Generic;
            using System.Collections.ObjectModel;
            using System.Text;

            namespace Enigma.Icons.Phosphor;

            /// <summary>
            /// The upstream kebab-case names of the <see cref="PhosphorIcon"/> members, and the lookups
            /// that map between name and member without <c>Enum.ToString</c>, <c>Enum.Parse</c>,
            /// <c>Enum.IsDefined</c> or reflection - which is what keeps the package trim- and
            /// AOT-clean.
            /// </summary>
            /// <remarks>
            /// Every table is built once, in a static initializer, and never mutated afterwards, so
            /// concurrent reads from any number of threads are safe.
            /// </remarks>
            public static class PhosphorIconNames
            {
                // Declared first: static field initializers run in declaration order, and the two
                // fields below are built from this one. One name per line, so an artwork refresh
                // diffs line-per-icon.
                private static readonly string[] Names =
                [

            """);

        foreach (string name in names)
        {
            builder.Append("        \"").Append(name).Append("\",\n");
        }

        builder.Append("""
                ];

                // Array.AsReadOnly, not the bare array: a string[] would satisfy IReadOnlyList<string>
                // while handing every caller a mutable view of the table.
                private static readonly ReadOnlyCollection<string> AllNames = Array.AsReadOnly(Names);

                private static readonly Dictionary<string, PhosphorIcon> Lookup = BuildLookup();

                /// <summary>Every icon name, in enum order.</summary>
                public static IReadOnlyList<string> All => AllNames;

                /// <summary>The upstream kebab-case name of an icon, e.g. "address-book-tabs".</summary>
                /// <param name="icon">The icon to name.</param>
                /// <returns>The icon's upstream kebab-case name.</returns>
                /// <exception cref="ArgumentOutOfRangeException"><paramref name="icon"/> is not a defined
                /// <see cref="PhosphorIcon"/> member.</exception>
                public static string ToKebabCase(PhosphorIcon icon)
                {
                    // One unsigned comparison covers both ends of the range, and it is a plain array
                    // index afterwards - no Enum.IsDefined, no reflection.
                    uint index = (uint)(int)icon;
                    if (index >= (uint)Names.Length)
                    {
                        throw new ArgumentOutOfRangeException(
                            nameof(icon),
                            icon,
                            "The value is not a defined PhosphorIcon member.");
                    }

                    return Names[index];
                }

                /// <summary>
                /// Looks an icon up by name. Accepts kebab-case, snake_case, or PascalCase,
                /// case-insensitively. Never throws.
                /// </summary>
                /// <param name="name">The name to look up.</param>
                /// <param name="icon">The matching icon, or the default value on a miss.</param>
                /// <returns><see langword="true"/> when <paramref name="name"/> matched an icon.</returns>
                public static bool TryParse(string name, out PhosphorIcon icon)
                {
                    if (string.IsNullOrEmpty(name))
                    {
                        icon = default;
                        return false;
                    }

                    // Fast path: the raw input hits for every kebab-case name and every casing of one,
                    // with no allocation. Only a separator-less or underscored name reaches Normalize.
                    if (Lookup.TryGetValue(name, out icon))
                    {
                        return true;
                    }

                    return Lookup.TryGetValue(Normalize(name), out icon);
                }

                private static Dictionary<string, PhosphorIcon> BuildLookup()
                {
                    var lookup = new Dictionary<string, PhosphorIcon>(Names.Length, StringComparer.OrdinalIgnoreCase);

                    for (int i = 0; i < Names.Length; i++)
                    {
                        lookup[Names[i]] = (PhosphorIcon)i;
                    }

                    return lookup;
                }


            """);

        builder.Append(NameMapper.RuntimeNormalizer).Append('\n');
        builder.Append("}\n");

        return OutputTarget.ToUtf8(builder.ToString());
    }
}
