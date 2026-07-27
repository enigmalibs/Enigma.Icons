using System.Text;

namespace Enigma.Icons.Generator;

/// <summary>
/// SPEC §8.4's kebab &lt;-&gt; Pascal mapping: mechanical, lossless, and with no exception list.
/// </summary>
/// <remarks>
/// The generator only needs the kebab -&gt; Pascal direction. The reverse direction runs at runtime
/// inside <c>PhosphorIconNames.TryParse</c>, so its source is kept here too — beside the forward
/// mapping it has to stay consistent with — and emitted verbatim by <see cref="NamesRenderer"/>.
/// </remarks>
internal static class NameMapper
{
    /// <summary>
    /// True when the mechanical mapping is lossless for this name: lower-case letters and single
    /// interior hyphens only. All 1,512 upstream names satisfy it (SPEC §8.4).
    /// </summary>
    internal static bool IsMappableKebabName(string name)
    {
        if (name.Length == 0 || name[0] == '-' || name[name.Length - 1] == '-')
        {
            return false;
        }

        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (c == '-')
            {
                if (name[i - 1] == '-')
                {
                    return false;
                }

                continue;
            }

            if (c is < 'a' or > 'z')
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// kebab -&gt; Pascal: split on <c>-</c>, upper-case each segment's first character, concatenate.
    /// No special cases, no acronym "fixing" — SPEC §8.4.
    /// </summary>
    internal static string ToPascalCase(string kebab)
    {
        var builder = new StringBuilder(kebab.Length);
        bool atSegmentStart = true;

        foreach (char c in kebab)
        {
            if (c == '-')
            {
                atSegmentStart = true;
                continue;
            }

            if (atSegmentStart)
            {
                // ASCII-only by construction (IsMappableKebabName), so this needs no culture and
                // cannot be affected by the Turkish-i or any other locale rule.
                builder.Append((char)(c - ('a' - 'A')));
                atSegmentStart = false;
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// The runtime Pascal/snake -&gt; kebab normalizer, emitted verbatim into
    /// <c>PhosphorIconNames.g.cs</c>. Indented for a class body.
    /// </summary>
    internal const string RuntimeNormalizer = """
            private static string Normalize(string name)
            {
                var builder = new StringBuilder(name.Length + 8);

                for (int i = 0; i < name.Length; i++)
                {
                    char c = name[i];

                    if (c == '_')
                    {
                        builder.Append('-');
                        continue;
                    }

                    // Only the separators have to be inserted: the lookup's OrdinalIgnoreCase
                    // comparer already covers the casing, which is why "AddressBookTabs",
                    // "address_book_tabs" and "ADDRESS-BOOK-TABS" all resolve to the same icon.
                    if (i > 0 && c >= 'A' && c <= 'Z' && builder.Length > 0 && builder[builder.Length - 1] != '-')
                    {
                        builder.Append('-');
                    }

                    builder.Append(c);
                }

                return builder.ToString();
            }
        """;
}
