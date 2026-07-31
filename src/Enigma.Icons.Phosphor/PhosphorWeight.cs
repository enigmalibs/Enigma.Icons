namespace Enigma.Icons.Phosphor;

/// <summary>The six Phosphor icon weights.</summary>
/// <remarks>
/// <para>
/// <b>The declaration order is load-bearing.</b> A member's numeric value indexes the weight-name
/// table inside <see cref="PhosphorIconSet"/>, which builds the embedded-resource name and the
/// <see cref="Enigma.Icons.IIconSet.Variants"/> order from it. Reordering these members is a
/// breaking change to the resource lookup, not a cosmetic edit — and it would silently resolve a
/// weight against another weight's artwork.
/// </para>
/// <para>
/// Note that <c>default(PhosphorWeight)</c> is <see cref="Thin"/>, not <see cref="Regular"/>: every
/// API in this package that defaults a weight states <see cref="Regular"/> explicitly.
/// </para>
/// </remarks>
public enum PhosphorWeight
{
    /// <summary>The thinnest stroked weight.</summary>
    Thin,

    /// <summary>A light stroked weight.</summary>
    Light,

    /// <summary>The default stroked weight.</summary>
    Regular,

    /// <summary>A heavy stroked weight.</summary>
    Bold,

    /// <summary>The solid, filled weight.</summary>
    Fill,

    /// <summary>A two-layer weight: a tinted backing shape at 20 % opacity behind the foreground shape.</summary>
    Duotone,
}
