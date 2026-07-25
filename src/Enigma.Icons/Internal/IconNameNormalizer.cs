using System;
using System.Text;

namespace Enigma.Icons.Internal;

/// <summary>
/// Normalizes icon and variant names to lower-case kebab-case, so a set can be probed with
/// kebab-case, <c>snake_case</c> or PascalCase input.
/// </summary>
internal static class IconNameNormalizer
{
    /// <summary>Normalizes a name, throwing when nothing usable is left.</summary>
    /// <param name="value">The raw name.</param>
    /// <param name="parameterName">The caller's parameter name, used in the exception.</param>
    /// <returns>The normalized kebab-case name.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">The value normalizes to nothing.</exception>
    internal static string Normalize(string value, string parameterName)
    {
        if (value is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        string? normalized = TryNormalize(value);
        if (normalized is null)
        {
            throw new ArgumentException("The name contains no usable characters.", parameterName);
        }

        return normalized;
    }

    /// <summary>Normalizes a name, returning null when nothing usable is left.</summary>
    /// <param name="value">The raw name, which may be null.</param>
    /// <returns>The normalized kebab-case name, or null.</returns>
    internal static string? TryNormalize(string? value)
    {
        if (value is null)
        {
            return null;
        }

        string trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        var builder = new StringBuilder(trimmed.Length + 4);
        for (int i = 0; i < trimmed.Length; i++)
        {
            char c = trimmed[i];

            if (c == '_' || c == ' ' || c == '\t')
            {
                builder.Append('-');
                continue;
            }

            if (char.IsUpper(c))
            {
                char previous = i > 0 ? trimmed[i - 1] : '\0';
                char next = i + 1 < trimmed.Length ? trimmed[i + 1] : '\0';

                // Split PascalCase, and split the tail of an acronym off the word that follows it
                // so HTTPServer becomes http-server rather than httpserver.
                bool boundary = char.IsLower(previous)
                    || char.IsDigit(previous)
                    || (char.IsUpper(previous) && char.IsLower(next));

                if (boundary)
                {
                    builder.Append('-');
                }

                builder.Append(char.ToLowerInvariant(c));
                continue;
            }

            builder.Append(char.ToLowerInvariant(c));
        }

        return Collapse(builder);
    }

    /// <summary>
    /// Removes a trailing <c>-&lt;variant&gt;</c> suffix, which is what makes an extracted Phosphor
    /// tree work with <see cref="SvgIconSet.FromDirectory"/> out of the box.
    /// </summary>
    /// <param name="name">The already-normalized icon name.</param>
    /// <param name="variant">The already-normalized variant name.</param>
    /// <returns>The name without the suffix, or the name unchanged.</returns>
    internal static string StripVariantSuffix(string name, string variant)
    {
        if (variant.Length == 0)
        {
            return name;
        }

        string suffix = "-" + variant;
        return name.Length > suffix.Length && name.EndsWith(suffix, StringComparison.Ordinal)
            ? name.Substring(0, name.Length - suffix.Length)
            : name;
    }

    /// <summary>Collapses runs of hyphens and trims them off both ends.</summary>
    private static string? Collapse(StringBuilder builder)
    {
        var result = new StringBuilder(builder.Length);
        bool pendingHyphen = false;

        for (int i = 0; i < builder.Length; i++)
        {
            char c = builder[i];
            if (c == '-')
            {
                pendingHyphen = result.Length > 0;
                continue;
            }

            if (pendingHyphen)
            {
                result.Append('-');
                pendingHyphen = false;
            }

            result.Append(c);
        }

        return result.Length == 0 ? null : result.ToString();
    }
}
