using System;
using System.Globalization;

namespace Enigma.Icons.Internal;

/// <summary>
/// The single deterministic number-to-string formatter used by every path-data emitter.
/// </summary>
/// <remarks>
/// The default <see cref="double.ToString()"/> is shortest-round-trippable on .NET Core 3.0+ and
/// <c>G15</c> on .NET Framework — so the same input SVG would emit different path data depending on
/// which runtime loaded the <c>netstandard2.0</c> assembly. Every emitter goes through
/// <see cref="Format"/> instead, which pins the rounding, the format string and the culture.
/// </remarks>
internal static class SvgNumber
{
    private const int Decimals = 6;

    /// <summary>Formats a coordinate for emission into path data.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The value rounded to six decimal places, in the invariant culture, with <c>-0</c> normalized to <c>0</c>.</returns>
    /// <exception cref="SvgParseException">The value is NaN or infinite.</exception>
    internal static string Format(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new SvgParseException(
                "A transform or attribute produced a coordinate that is not a finite number.");
        }

        double rounded = Math.Round(value, Decimals, MidpointRounding.AwayFromZero);

        // Normalizes both -0.0 and +0.0 to "0" (they compare equal).
        if (rounded == 0.0)
        {
            return "0";
        }

        return rounded.ToString("0.######", CultureInfo.InvariantCulture);
    }
}
