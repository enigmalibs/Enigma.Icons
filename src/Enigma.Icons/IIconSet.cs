using System.Collections.Generic;

namespace Enigma.Icons;

/// <summary>
/// A named collection of icons, optionally in several style variants, that resolves a name to an
/// <see cref="IconGlyph"/>.
/// </summary>
/// <remarks>
/// <para><b>Contract for implementers.</b></para>
/// <list type="bullet">
/// <item><description>
/// Name lookup is <b>case-insensitive</b> and accepts kebab-case, <c>snake_case</c> or PascalCase
/// input, normalized internally to kebab-case. The same applies to variants.
/// </description></item>
/// <item><description>
/// <c>variant == null</c> means <see cref="DefaultVariant"/>. A variant the set does not have is a
/// miss, <b>never</b> a silent fallback to the default — a caller asking for <c>bold</c> must not
/// get <c>regular</c> back.
/// </description></item>
/// <item><description>
/// Repeated calls for the same <c>(icon, variant)</c> return a <b>cached, reference-equal</b>
/// <see cref="IconGlyph"/>.
/// </description></item>
/// <item><description>All members are safe for concurrent use from any thread.</description></item>
/// <item><description>
/// A <see langword="null"/> <c>icon</c> is a <b>miss</b> for <see cref="TryGetGlyph"/> — it returns
/// <see langword="false"/> without throwing — while <see cref="GetGlyph"/> fails fast with
/// <see cref="System.ArgumentNullException"/>. The null guard sits ahead of name normalization, so a
/// null never reaches it. Every implementation must behave this way.
/// </description></item>
/// </list>
/// <para>
/// <b>What "<see cref="TryGetGlyph"/> never throws" means — precedence.</b> The <c>Try</c> prefix
/// governs <i>absence</i>, not <i>corruption</i>:
/// </para>
/// <list type="table">
/// <listheader>
/// <term>Situation</term>
/// <description>Behaviour</description>
/// </listheader>
/// <item>
/// <term>The icon or variant is not in the set — an ordinary <b>miss</b></term>
/// <description><see cref="TryGetGlyph"/> returns <see langword="false"/> with a null glyph;
/// <see cref="GetGlyph"/> throws <see cref="IconNotFoundException"/>.</description>
/// </item>
/// <item>
/// <term>The source is present but <b>broken</b> — malformed SVG, an unreadable or vanished file, a
/// missing embedded-resource stream, a corrupt resource</term>
/// <description><b>Both</b> methods throw — <see cref="SvgParseException"/>,
/// <see cref="System.IO.InvalidDataException"/>, or the underlying
/// <see cref="System.IO.IOException"/>.</description>
/// </item>
/// </list>
/// <para>
/// Rationale: a miss is a normal outcome a caller should branch on; a broken source is a defect, and
/// silently reporting it as "icon not found" would turn a fixable bug into an invisible blank space.
/// <see cref="TryGetGlyph"/> is not a general-purpose exception swallow.
/// </para>
/// <para>
/// <b>No default interface members.</b> <see cref="GetGlyph"/> could be supplied once as a default
/// interface member on the modern target frameworks, but <c>netstandard2.0</c> is in this library's
/// target set and does not support them — so every set implements it explicitly. Implementations
/// must also repeat the <c>variant = null</c> default argument, or callers holding a class-typed
/// reference lose it.
/// </para>
/// </remarks>
public interface IIconSet
{
    /// <summary>Display name of the set, used in exception messages. E.g. "Phosphor".</summary>
    string Name { get; }

    /// <summary>The variant names this set understands, lower-case. Empty for a set with
    /// a single style.</summary>
    IReadOnlyList<string> Variants { get; }

    /// <summary>The variant used when the caller passes null. Null when the set has no
    /// variants.</summary>
    string? DefaultVariant { get; }

    /// <summary>Every icon name in the set, lower-case kebab-case, ordered.</summary>
    IEnumerable<string> IconNames { get; }

    /// <summary>Non-throwing lookup.</summary>
    /// <remarks>
    /// Non-throwing <i>for a miss</i>. A source that is in the set but cannot be read or parsed
    /// propagates its exception from here exactly as it does from <see cref="GetGlyph"/> — see the
    /// precedence table on <see cref="IIconSet"/>. A null <paramref name="icon"/> is a miss.
    /// </remarks>
    /// <param name="icon">The icon name, in kebab-case, <c>snake_case</c> or PascalCase.</param>
    /// <param name="variant">The variant name, or null for <see cref="DefaultVariant"/>.</param>
    /// <param name="glyph">The resolved glyph, or null on a miss.</param>
    /// <returns><see langword="true"/> when the icon was found.</returns>
    /// <exception cref="SvgParseException">The icon is in the set but its source is broken.</exception>
    bool TryGetGlyph(string icon, string? variant, out IconGlyph? glyph);

    /// <summary>Throwing lookup.</summary>
    /// <remarks>
    /// A miss throws <see cref="IconNotFoundException"/>; a broken source propagates its own
    /// exception — see the precedence table on <see cref="IIconSet"/>.
    /// </remarks>
    /// <param name="icon">The icon name, in kebab-case, <c>snake_case</c> or PascalCase.</param>
    /// <param name="variant">The variant name, or null for <see cref="DefaultVariant"/>.</param>
    /// <returns>The resolved glyph.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="icon"/> is null.</exception>
    /// <exception cref="IconNotFoundException">The icon, or the requested variant of it, is not in the set.</exception>
    /// <exception cref="SvgParseException">The icon is in the set but its source is broken.</exception>
    IconGlyph GetGlyph(string icon, string? variant = null);
}
