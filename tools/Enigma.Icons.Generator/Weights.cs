using System;

namespace Enigma.Icons.Generator;

/// <summary>
/// The single source of truth for the six Phosphor weights and their order.
/// </summary>
/// <remarks>
/// The order is SPEC §9's <c>PhosphorWeight</c> enum order — thin, light, regular, bold, fill,
/// duotone — and it drives the order of the summary rows and of the generated <c>.dat</c> files.
/// It is deliberately declared once, here, rather than repeated in each renderer.
/// </remarks>
internal static class Weights
{
    /// <summary>The weight whose upstream filenames carry no <c>-&lt;weight&gt;</c> suffix (SPEC §7.4.7).</summary>
    internal const string Regular = "regular";

    /// <summary>The six weights, in SPEC §9 order.</summary>
    internal static readonly string[] All = ["thin", "light", Regular, "bold", "fill", "duotone"];

    /// <summary>The filename suffix upstream uses for a weight — empty for <see cref="Regular"/>.</summary>
    internal static string FileSuffix(string weight)
        => string.Equals(weight, Regular, StringComparison.Ordinal) ? string.Empty : "-" + weight;
}
