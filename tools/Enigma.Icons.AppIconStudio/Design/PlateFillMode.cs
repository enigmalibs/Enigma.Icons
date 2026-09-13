namespace Enigma.Icons.AppIconStudio.Design;

/// <summary>How the rounded plate behind the glyph is painted.</summary>
/// <remarks>
/// <c>default(PlateFillMode)</c> is <see cref="Solid"/>, which is also the mode a new design starts
/// in — the simplest thing that produces a usable icon.
/// </remarks>
public enum PlateFillMode
{
    /// <summary>One flat colour across the whole plate.</summary>
    Solid,

    /// <summary>A two-stop linear gradient at a configurable angle.</summary>
    LinearGradient,
}
