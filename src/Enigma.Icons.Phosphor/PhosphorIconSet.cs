using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using Enigma.Icons.Internal;

// The model types (IconGlyph, IconLayer, IconViewBox, IIconSet, IconNotFoundException) live in the
// enclosing Enigma.Icons namespace and need no using directive here.
namespace Enigma.Icons.Phosphor;

/// <summary>
/// The Phosphor icon set — 1,512 icons in 6 weights, served from six embedded resources.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="Instance"/>; the type has no public constructor. Nothing is read at construction:
/// a weight's table is loaded on first use of that weight and then kept for the life of the process,
/// and every resolved <see cref="IconGlyph"/> is cached, so repeated lookups return the same
/// instance.
/// </para>
/// <para>
/// Two surfaces resolve the same rows: the strongly-typed <see cref="PhosphorIcon"/> /
/// <see cref="PhosphorWeight"/> pair, and the string-keyed <see cref="IIconSet"/> one. The typed
/// surface is the ergonomic path; the string surface is what makes the set interchangeable with any
/// other <see cref="IIconSet"/>.
/// </para>
/// <para>
/// <b>Thread safety.</b> Every member is safe for concurrent use from any thread: both caches are
/// <see cref="ConcurrentDictionary{TKey, TValue}"/> and the model types are immutable.
/// </para>
/// <para>
/// <b>No XML is parsed at runtime.</b> The embedded resources hold path data, not SVG, so
/// <see cref="SvgIconParser"/> is not on this code path at all — it exists for user-supplied SVG and
/// for <see cref="SvgIconSet"/>.
/// </para>
/// </remarks>
public sealed class PhosphorIconSet : IIconSet
{
    private const string ResourcePrefix = "Enigma.Icons.Phosphor.Assets.phosphor.";
    private const string ResourceSuffix = ".dat";
    private const string ExpectedVersion = "v1";
    private const string ExpectedViewBox = "0 0 256 256";
    private const string SetName = "Phosphor";

    /// <summary>
    /// The weight names, indexed by <see cref="PhosphorWeight"/>. This table — never
    /// <c>Enum.GetName</c>, <c>Enum.ToString</c> or <c>Enum.Parse</c> — is what builds a resource
    /// name, so the lookup is a compile-time literal plus an array index and trimming cannot break
    /// it (SPEC §7.1, §10.4).
    /// </summary>
    private static readonly string[] WeightNames = { "thin", "light", "regular", "bold", "fill", "duotone" };

    // Array.AsReadOnly, not the bare array: a string[] handed out as IReadOnlyList<string> is
    // castable straight back to a mutable string[].
    private static readonly ReadOnlyCollection<string> VariantNames = Array.AsReadOnly(WeightNames);

    private static readonly Dictionary<string, PhosphorWeight> WeightsByName = BuildWeightLookup();

    private static readonly PhosphorIconSet Singleton = new PhosphorIconSet();

    // Level 1 (SPEC §9.1): canonical icon name -> the raw remainder of its .dat line, i.e. the
    // TAB-joined layer fields. Keys are already canonical, so the inner comparer is ordinal.
    private readonly ConcurrentDictionary<PhosphorWeight, Dictionary<string, string>> _tables =
        new ConcurrentDictionary<PhosphorWeight, Dictionary<string, string>>();

    // Level 2: the materialized glyphs, so repeated lookups are reference-equal.
    private readonly ConcurrentDictionary<(PhosphorWeight Weight, string Name), IconGlyph> _glyphs =
        new ConcurrentDictionary<(PhosphorWeight Weight, string Name), IconGlyph>();

    private PhosphorIconSet()
    {
        // Deliberately empty: no I/O here, so touching Instance costs nothing.
    }

    /// <summary>The shared, thread-safe instance. Resources are loaded lazily per weight.</summary>
    public static PhosphorIconSet Instance => Singleton;

    /// <summary>Display name of the set, used in exception messages. Always <c>Phosphor</c>.</summary>
    public string Name => SetName;

    /// <summary>
    /// The six weight names — <c>thin</c>, <c>light</c>, <c>regular</c>, <c>bold</c>, <c>fill</c>,
    /// <c>duotone</c> — in <see cref="PhosphorWeight"/> declaration order.
    /// </summary>
    public IReadOnlyList<string> Variants => VariantNames;

    /// <summary>The variant used when a caller passes null: <c>regular</c>.</summary>
    public string? DefaultVariant => WeightNames[(int)PhosphorWeight.Regular];

    /// <summary>
    /// All 1,512 icon names, lower-case kebab-case, in ordinal order. Complete without reading any
    /// embedded resource.
    /// </summary>
    public IEnumerable<string> IconNames => PhosphorIconNames.All;

    /// <summary>Resolves an icon in a weight.</summary>
    /// <param name="icon">The icon to resolve.</param>
    /// <param name="weight">The weight to resolve it in. Defaults to <see cref="PhosphorWeight.Regular"/>.</param>
    /// <returns>The resolved glyph. Repeated calls for the same pair return the same instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="icon"/> or <paramref name="weight"/> is not a defined member of its enum — a cast integer rather than a real value.</exception>
    /// <exception cref="IconNotFoundException">The icon is not in this weight's table. Unreachable in a correctly generated package: it would mean the artwork and the generated enum disagree.</exception>
    /// <exception cref="InvalidDataException">The weight's embedded resource is missing or corrupt — a packaging defect, not a miss.</exception>
    public IconGlyph GetGlyph(PhosphorIcon icon, PhosphorWeight weight = PhosphorWeight.Regular)
    {
        ValidateWeight(weight);

        string name = PhosphorIconNames.ToKebabCase(icon);
        if (TryGetGlyphCore(weight, name, out IconGlyph? glyph) && glyph is not null)
        {
            return glyph;
        }

        throw new IconNotFoundException(name, WeightNames[(int)weight], SetName);
    }

    /// <summary>Resolves an icon in a weight without throwing on a miss.</summary>
    /// <param name="icon">The icon to resolve.</param>
    /// <param name="weight">The weight to resolve it in.</param>
    /// <param name="glyph">The resolved glyph, or null on a miss.</param>
    /// <returns><see langword="true"/> when the icon was found.</returns>
    /// <remarks>
    /// Non-throwing <i>for a miss</i> only. An undefined enum value is a programming error, not an
    /// absence, and a corrupt or missing resource is a broken source — both throw from here exactly
    /// as they do from <see cref="GetGlyph(PhosphorIcon, PhosphorWeight)"/>.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="icon"/> or <paramref name="weight"/> is not a defined member of its enum.</exception>
    /// <exception cref="InvalidDataException">The weight's embedded resource is missing or corrupt.</exception>
    public bool TryGetGlyph(PhosphorIcon icon, PhosphorWeight weight, out IconGlyph? glyph)
    {
        ValidateWeight(weight);

        return TryGetGlyphCore(weight, PhosphorIconNames.ToKebabCase(icon), out glyph);
    }

    /// <summary>Non-throwing lookup by name.</summary>
    /// <param name="icon">The icon name, in kebab-case, <c>snake_case</c> or PascalCase, case-insensitively.</param>
    /// <param name="variant">One of the six weight names, normalized exactly as an icon name is — case-insensitively, with surrounding whitespace trimmed and <c>_</c>, space and tab read as <c>-</c> — or null for <see cref="DefaultVariant"/>.</param>
    /// <param name="glyph">The resolved glyph, or null on a miss.</param>
    /// <returns><see langword="true"/> when the icon was found.</returns>
    /// <remarks>
    /// <para>
    /// <b>A variant this set does not have is a miss, never a silent fallback</b> — a caller asking
    /// for <c>heavy</c> gets <see langword="false"/>, not <c>regular</c>. A null
    /// <paramref name="icon"/> is likewise a miss.
    /// </para>
    /// <para>
    /// Non-throwing <i>for a miss</i> only: a missing or corrupt embedded resource propagates its
    /// <see cref="InvalidDataException"/> from here exactly as it does from
    /// <see cref="GetGlyph(string, string)"/> — see the precedence table on <see cref="IIconSet"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidDataException">The weight's embedded resource is missing or corrupt.</exception>
    public bool TryGetGlyph(string icon, string? variant, out IconGlyph? glyph)
    {
        glyph = null;

        // A variant the set does not have is a miss, never a fallback to the default.
        if (!TryResolveWeight(variant, out PhosphorWeight weight))
        {
            return false;
        }

        // TryParse treats null and empty as a miss, so the null guard sits ahead of normalization
        // and a null never reaches it — the IIconSet rule SvgIconSet already establishes.
        if (!PhosphorIconNames.TryParse(icon, out PhosphorIcon parsed))
        {
            return false;
        }

        return TryGetGlyphCore(weight, PhosphorIconNames.ToKebabCase(parsed), out glyph);
    }

    /// <summary>Throwing lookup by name.</summary>
    /// <param name="icon">The icon name, in kebab-case, <c>snake_case</c> or PascalCase, case-insensitively.</param>
    /// <param name="variant">One of the six weight names, normalized exactly as an icon name is — case-insensitively, with surrounding whitespace trimmed and <c>_</c>, space and tab read as <c>-</c> — or null for <see cref="DefaultVariant"/>.</param>
    /// <returns>The resolved glyph. Repeated calls for the same pair return the same instance.</returns>
    /// <remarks>
    /// <b>A variant this set does not have is a miss, never a silent fallback</b> — asking for
    /// <c>heavy</c> throws rather than returning <c>regular</c>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="icon"/> is null.</exception>
    /// <exception cref="IconNotFoundException">The icon, or the requested variant of it, is not in the set.</exception>
    /// <exception cref="InvalidDataException">The weight's embedded resource is missing or corrupt — a broken source, not a miss.</exception>
    public IconGlyph GetGlyph(string icon, string? variant = null)
    {
        if (icon is null)
        {
            throw new ArgumentNullException(nameof(icon));
        }

        if (TryGetGlyph(icon, variant, out IconGlyph? glyph) && glyph is not null)
        {
            return glyph;
        }

        // The caller's original strings, so the exception echoes what was asked for.
        throw new IconNotFoundException(icon, variant, SetName);
    }

    /// <summary>
    /// The single lookup core both surfaces funnel through, so the typed and string paths cannot
    /// diverge.
    /// </summary>
    private bool TryGetGlyphCore(PhosphorWeight weight, string canonicalName, out IconGlyph? glyph)
    {
        // Deliberately NOT wrapped in a catch: a broken resource propagates from TryGetGlyph too.
        Dictionary<string, string> table = _tables.GetOrAdd(weight, LoadWeight);

        if (!table.TryGetValue(canonicalName, out string? remainder))
        {
            glyph = null;
            return false;
        }

        // GetOrAdd may run the factory twice under a race, but it returns the value that is actually
        // in the dictionary — so every caller observes one instance, which is the reference-equality
        // guarantee of IIconSet.
        glyph = _glyphs.GetOrAdd((weight, canonicalName), key => BuildGlyph(remainder, key.Name, ResourceNameFor(key.Weight)));
        return true;
    }

    private static string ResourceNameFor(PhosphorWeight weight)
        => ResourcePrefix + WeightNames[(int)weight] + ResourceSuffix;

    private static void ValidateWeight(PhosphorWeight weight)
    {
        // One unsigned comparison covers both ends of the range — no Enum.IsDefined, no reflection.
        if ((uint)(int)weight >= (uint)WeightNames.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(weight),
                weight,
                "The value is not a defined PhosphorWeight member.");
        }
    }

    private static bool TryResolveWeight(string? variant, out PhosphorWeight weight)
    {
        if (variant is null)
        {
            weight = PhosphorWeight.Regular;
            return true;
        }

        // The same normalizer SvgIconSet runs its variants through, reached over InternalsVisibleTo:
        // IIconSet documents one normalization rule for names AND variants, so a bare dictionary
        // probe on the raw string would leave the two in-house implementations disagreeing about
        // " Duotone " and "duo_tone". Normalizing first, then probing the still-OrdinalIgnoreCase
        // table, keeps a weight the set does not have a miss rather than a fallback.
        string? normalized = IconNameNormalizer.TryNormalize(variant);
        if (normalized is null)
        {
            weight = PhosphorWeight.Regular;
            return false;
        }

        return WeightsByName.TryGetValue(normalized, out weight);
    }

    private static Dictionary<string, PhosphorWeight> BuildWeightLookup()
    {
        var lookup = new Dictionary<string, PhosphorWeight>(WeightNames.Length, StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < WeightNames.Length; i++)
        {
            lookup[WeightNames[i]] = (PhosphorWeight)i;
        }

        return lookup;
    }

    /// <summary>Reads one weight's embedded resource into its name → line-remainder table.</summary>
    private static Dictionary<string, string> LoadWeight(PhosphorWeight weight)
    {
        string resourceName = ResourceNameFor(weight);

        using Stream? stream = typeof(PhosphorIconSet).Assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            // A packaging defect, and per SPEC §6.1 a broken source rather than a miss. Unreachable
            // in a correctly built package — ResourceManifestTests is what guarantees that.
            throw new InvalidDataException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The embedded resource '{0}' was not found in assembly '{1}'.",
                    resourceName,
                    typeof(PhosphorIconSet).Assembly.GetName().Name));
        }

        return ReadTable(stream, WeightNames[(int)weight], resourceName);
    }

    /// <summary>
    /// Parses the SPEC §7.2 <c>v1</c> format in a single pass: header validation, then one line per
    /// icon split at the first TAB.
    /// </summary>
    /// <param name="stream">The resource stream. UTF-8 without BOM, LF, final newline.</param>
    /// <param name="expectedWeightName">The weight name the header must declare.</param>
    /// <param name="resourceName">The resource's name, for error messages.</param>
    /// <returns>Canonical icon name → the raw remainder of its line (the TAB-joined layer fields).</returns>
    /// <exception cref="InvalidDataException">The resource is not in the expected format.</exception>
    /// <remarks>
    /// Internal rather than private so the unit tests can drive it over synthetic streams — a corrupt
    /// header cannot be reproduced through the six real, valid resources.
    /// </remarks>
    internal static Dictionary<string, string> ReadTable(Stream stream, string expectedWeightName, string resourceName)
    {
        // The encoding is fixed by SPEC §7.2, so nothing is sniffed: no BOM detection, no fallback.
        using var reader = new StreamReader(stream, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: false);

        string? header = reader.ReadLine();
        if (header is null)
        {
            throw Corrupt(resourceName, "header", "4 tab-separated fields", "an empty resource");
        }

        string[] headerFields = header.Split('\t');
        if (headerFields.Length != 4)
        {
            throw Corrupt(
                resourceName,
                "header",
                "4 tab-separated fields",
                headerFields.Length.ToString(CultureInfo.InvariantCulture));
        }

        if (!string.Equals(headerFields[0], ExpectedVersion, StringComparison.Ordinal))
        {
            throw Corrupt(resourceName, "format version", Quote(ExpectedVersion), Quote(headerFields[0]));
        }

        // Catches a .dat embedded under the wrong name — the failure that would otherwise serve one
        // weight's artwork for another.
        if (!string.Equals(headerFields[1], expectedWeightName, StringComparison.Ordinal))
        {
            throw Corrupt(resourceName, "weight name", Quote(expectedWeightName), Quote(headerFields[1]));
        }

        // The view box is a literal comparison, not a number parse: SPEC §7.4 fact 3 fixes it at
        // 0 0 256 256 for every Phosphor file, which is exactly IconViewBox.Default.
        if (!string.Equals(headerFields[2], ExpectedViewBox, StringComparison.Ordinal))
        {
            throw Corrupt(resourceName, "view box", Quote(ExpectedViewBox), Quote(headerFields[2]));
        }

        // NumberStyles.None: digits only, so a sign or surrounding whitespace is rejected too.
        if (!int.TryParse(headerFields[3], NumberStyles.None, CultureInfo.InvariantCulture, out int declaredCount))
        {
            throw Corrupt(resourceName, "icon count", "a non-negative integer", Quote(headerFields[3]));
        }

        var table = new Dictionary<string, string>(declaredCount, StringComparer.Ordinal);

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Length == 0)
            {
                // The resource ends with a newline, which ReadLine does not turn into an extra line —
                // so a blank line is tolerated only as the very last one.
                if (reader.ReadLine() is not null)
                {
                    throw Corrupt(resourceName, "body", "one icon per line", "a blank line inside the table");
                }

                break;
            }

            int tab = line.IndexOf('\t');
            if (tab <= 0 || tab == line.Length - 1)
            {
                throw Corrupt(
                    resourceName,
                    "icon line",
                    "a name and at least one layer field, separated by a TAB",
                    Quote(Truncate(line)));
            }

            string name = line.Substring(0, tab);

            // The "no duplicate names within a weight" guarantee is enforced at load, not merely
            // asserted by a test. ContainsKey + Add rather than TryAdd: TryAdd is netstandard2.1+,
            // and one implementation for every TFM beats an #if.
            if (table.ContainsKey(name))
            {
                throw Corrupt(resourceName, "icon name", "no duplicate names within a weight", Quote(name));
            }

            table.Add(name, line.Substring(tab + 1));
        }

        // Together with the version check, this is the tripwire for a truncated or half-written
        // resource.
        if (table.Count != declaredCount)
        {
            throw Corrupt(
                resourceName,
                "icon count",
                declaredCount.ToString(CultureInfo.InvariantCulture) + " icons, as the header declares",
                table.Count.ToString(CultureInfo.InvariantCulture));
        }

        return table;
    }

    /// <summary>Materializes one icon line's layer fields into a glyph.</summary>
    /// <param name="remainder">The raw remainder of the icon's line: the TAB-joined layer fields.</param>
    /// <param name="iconName">The icon's canonical name, for error messages.</param>
    /// <param name="resourceName">The resource's name, for error messages.</param>
    /// <returns>The materialized glyph.</returns>
    /// <exception cref="InvalidDataException">A layer field is malformed.</exception>
    /// <remarks>
    /// Internal rather than private for the same reason as <see cref="ReadTable"/>: the six real
    /// resources carry no malformed layer field, so the rejection paths are only reachable from a
    /// test over synthetic data.
    /// </remarks>
    internal static IconGlyph BuildGlyph(string remainder, string iconName, string resourceName)
    {
        // Paint order: index 0 is the bottom-most layer (SPEC §7.2).
        string[] fields = remainder.Split('\t');
        var layers = new IconLayer[fields.Length];

        for (int i = 0; i < fields.Length; i++)
        {
            string field = fields[i];
            double opacity = 1.0;
            string pathData = field;

            if (field.Length > 0 && field[0] == '@')
            {
                int colon = field.IndexOf(':');
                if (colon < 2)
                {
                    throw CorruptIcon(resourceName, iconName, "an opacity prefix of the form @<opacity>:", Quote(Truncate(field)));
                }

                string text = field.Substring(1, colon - 1);
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out opacity)
                    || !(opacity >= 0.0 && opacity <= 1.0))
                {
                    throw CorruptIcon(resourceName, iconName, "an opacity in [0,1], invariant culture", Quote(text));
                }

                pathData = field.Substring(colon + 1);
            }

            if (pathData.Length == 0)
            {
                throw CorruptIcon(resourceName, iconName, "non-empty path data on every layer", "an empty layer field");
            }

            // Every other IconLayer argument stays at its default: the .dat carries no paint
            // information, and upstream's fill="currentColor" normalizes to null (SPEC §4.2, §5.1),
            // so the layer inherits the renderer's brush.
            layers[i] = new IconLayer(pathData, opacity);
        }

        return new IconGlyph(IconViewBox.Default, layers);
    }

    private static InvalidDataException Corrupt(string resourceName, string field, string expected, string actual)
        => new InvalidDataException(
            string.Format(
                CultureInfo.InvariantCulture,
                "The embedded resource '{0}' is not a valid v1 icon table: its {1} is wrong — expected {2}, found {3}.",
                resourceName,
                field,
                expected,
                actual));

    private static InvalidDataException CorruptIcon(string resourceName, string iconName, string expected, string actual)
        => new InvalidDataException(
            string.Format(
                CultureInfo.InvariantCulture,
                "The embedded resource '{0}' is not a valid v1 icon table: icon '{1}' — expected {2}, found {3}.",
                resourceName,
                iconName,
                expected,
                actual));

    private static string Quote(string value)
        => "\"" + value + "\"";

    private static string Truncate(string value)
        => value.Length <= 40 ? value : value.Substring(0, 40) + "…";
}
