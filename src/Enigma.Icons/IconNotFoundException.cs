using System;
using System.Globalization;

namespace Enigma.Icons;

/// <summary>
/// Thrown by <see cref="IIconSet.GetGlyph"/> when the requested icon — or the requested variant of
/// it — is not in the set.
/// </summary>
/// <remarks>
/// This is the <i>miss</i> half of <see cref="IIconSet"/>'s precedence rule. A source that is in the
/// set but cannot be read or parsed raises <see cref="SvgParseException"/> instead, from both
/// lookup methods.
/// <para>
/// There is deliberately no <c>(SerializationInfo, StreamingContext)</c> constructor — see
/// <see cref="SvgParseException"/>.
/// </para>
/// </remarks>
public sealed class IconNotFoundException : Exception
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="iconName">The icon name that was asked for, as the caller wrote it.</param>
    /// <param name="variant">The variant that was asked for, or <see langword="null"/> when the caller asked for the default variant.</param>
    /// <param name="setName">The display name of the icon set that was searched.</param>
    /// <exception cref="ArgumentNullException"><paramref name="iconName"/> or <paramref name="setName"/> is null.</exception>
    public IconNotFoundException(string iconName, string? variant, string setName)
        : base(BuildMessage(iconName, variant, setName))
    {
        IconName = iconName;
        Variant = variant;
        SetName = setName;
    }

    /// <summary>The icon name that was asked for, as the caller wrote it.</summary>
    public string IconName { get; }

    /// <summary>The variant that was asked for, or null when the caller asked for the default variant.</summary>
    public string? Variant { get; }

    /// <summary>The display name of the icon set that was searched.</summary>
    public string SetName { get; }

    private static string BuildMessage(string iconName, string? variant, string setName)
    {
        if (iconName is null)
        {
            throw new ArgumentNullException(nameof(iconName));
        }

        if (setName is null)
        {
            throw new ArgumentNullException(nameof(setName));
        }

        return variant is null
            ? string.Format(
                CultureInfo.InvariantCulture,
                "Icon '{0}' was not found in icon set '{1}'.",
                iconName,
                setName)
            : string.Format(
                CultureInfo.InvariantCulture,
                "Icon '{0}' (variant '{1}') was not found in icon set '{2}'.",
                iconName,
                variant,
                setName);
    }
}
