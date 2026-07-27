using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using Enigma.Icons.Internal;

namespace Enigma.Icons;

/// <summary>
/// An <see cref="IIconSet"/> over your own <c>.svg</c> files — a directory tree, an explicit file
/// list, an assembly's embedded resources, or in-memory SVG text.
/// </summary>
/// <remarks>
/// <para>
/// <b>Discovery is eager, parsing is lazy.</b> Every factory enumerates its sources at
/// construction, so <see cref="IconNames"/> is complete immediately and a
/// <see cref="TryGetGlyph"/> miss is a dictionary miss rather than an I/O probe. A source is parsed
/// on first use and the resulting <see cref="IconGlyph"/> is cached, so repeated lookups return the
/// same instance.
/// </para>
/// <para>See <see cref="IIconSet"/> for the miss-versus-broken-source precedence rule.</para>
/// </remarks>
public sealed class SvgIconSet : IIconSet
{
    private const string FallbackSetName = "SvgIcons";

    private static readonly ReadOnlyCollection<string> _noVariants =
        new ReadOnlyCollection<string>(Array.Empty<string>());

    private readonly Dictionary<string, Dictionary<string, SvgSource>> _index;
    private readonly ConcurrentDictionary<(string Variant, string Name), Lazy<IconGlyph>> _cache =
        new ConcurrentDictionary<(string Variant, string Name), Lazy<IconGlyph>>();

    private SvgIconSet(
        string name,
        Dictionary<string, Dictionary<string, SvgSource>> index,
        List<string> variants,
        string? defaultVariant)
    {
        _index = index;
        Name = name;
        Variants = variants.Count == 0 ? _noVariants : new ReadOnlyCollection<string>(variants);
        DefaultVariant = defaultVariant;

        var names = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, Dictionary<string, SvgSource>> bucket in index)
        {
            foreach (string icon in bucket.Value.Keys)
            {
                if (seen.Add(icon))
                {
                    names.Add(icon);
                }
            }
        }

        names.Sort(StringComparer.Ordinal);
        IconNames = new ReadOnlyCollection<string>(names);
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public IReadOnlyList<string> Variants { get; }

    /// <inheritdoc/>
    public string? DefaultVariant { get; }

    /// <inheritdoc/>
    /// <remarks>
    /// The <b>union</b> of the icon names of every variant. A listed name is therefore not
    /// guaranteed to resolve in every variant — including <see cref="DefaultVariant"/>: a tree
    /// missing one icon in one variant still lists that name, and
    /// <see cref="TryGetGlyph(string, string?, out IconGlyph?)"/> returns false for the variants
    /// that do not have it. The property answers "what icons does this set know about", not "what
    /// resolves in the default variant".
    /// </remarks>
    public IEnumerable<string> IconNames { get; }

    /// <summary>Icons from a directory of .svg files.</summary>
    /// <param name="path">The directory to read. Resolved with <see cref="Path.GetFullPath(string)"/>.</param>
    /// <param name="variantsFromSubfolders">When true (default) each immediate subdirectory is a
    /// variant and its .svg files are its icons; when false the directory's own .svg files are
    /// the icons and the set has no variants.</param>
    /// <param name="defaultVariant">
    /// The variant used when a caller passes null. When omitted, <c>regular</c> is used if it was
    /// discovered, otherwise the first variant in ordinal order.
    /// </param>
    /// <param name="name">The set's display name. Defaults to the directory's own leaf name.</param>
    /// <returns>The constructed set.</returns>
    /// <remarks>
    /// <para>
    /// A file name ending in <c>-&lt;variant&gt;</c> has that suffix stripped, so
    /// <c>bold/acorn-bold.svg</c> and <c>bold/acorn.svg</c> both yield the icon <c>acorn</c> in
    /// variant <c>bold</c> — which is what makes an extracted Phosphor tree work unchanged. Two
    /// files in one variant that normalize to the same name collide, and the first in ordinal
    /// file-name order wins.
    /// </para>
    /// <para>
    /// <b>Path safety.</b> Enumeration is top-directory-only: only <c>.svg</c> files directly inside
    /// the root (or, with variants, directly inside one variant subdirectory) are read — never
    /// recursively. Symlinked variant subdirectories <i>and</i> symlinked <c>.svg</c> files are
    /// skipped, so no entry can point the set at content outside the root; every path that is read is
    /// additionally constrained to the root. A skipped entry is dropped silently, so one hostile entry
    /// cannot deny service on an otherwise valid directory.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="defaultVariant"/> is not one of the discovered variants, or was supplied for a set with no variants.</exception>
    /// <exception cref="DirectoryNotFoundException">The directory does not exist.</exception>
    public static SvgIconSet FromDirectory(
        string path,
        bool variantsFromSubfolders = true,
        string? defaultVariant = null,
        string? name = null)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        string root = Path.GetFullPath(path);
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException(
                string.Format(CultureInfo.InvariantCulture, "The icon directory '{0}' does not exist.", root));
        }

        var index = NewIndex();
        var variants = new List<string>();

        if (variantsFromSubfolders)
        {
            var directories = new List<string>(Directory.EnumerateDirectories(root));
            directories.Sort(StringComparer.Ordinal);

            foreach (string directory in directories)
            {
                var info = new DirectoryInfo(directory);

                // FileAttributes.ReparsePoint is the portable "this is a symlink" test —
                // FileSystemInfo.LinkTarget is .NET 6+ and netstandard2.0 is in the TFM set.
                if ((info.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                {
                    continue;
                }

                string? variant = IconNameNormalizer.TryNormalize(info.Name);
                if (variant is null)
                {
                    continue;
                }

                AddDirectoryFiles(GetBucket(index, variant, variants), root, directory, variant);
            }
        }
        else
        {
            if (defaultVariant is not null)
            {
                throw new ArgumentException(
                    "A default variant cannot be supplied when variantsFromSubfolders is false, because the set has no variants.",
                    nameof(defaultVariant));
            }

            AddDirectoryFiles(GetBucket(index, string.Empty, null), root, root, string.Empty);
        }

        variants.Sort(StringComparer.Ordinal);
        string? resolvedDefault = ResolveDefaultVariant(defaultVariant, variants);
        return new SvgIconSet(name ?? new DirectoryInfo(root).Name, index, variants, resolvedDefault);
    }

    /// <summary>Icons from an assembly's embedded resources whose names start with
    /// <paramref name="resourcePrefix"/>.</summary>
    /// <param name="assembly">The assembly to read manifest resources from.</param>
    /// <param name="resourcePrefix">The manifest-resource name prefix to match, compared ordinally.</param>
    /// <param name="defaultVariant">
    /// The variant used when a caller passes null. When omitted, <c>regular</c> is used if it was
    /// discovered, otherwise the first variant in ordinal order.
    /// </param>
    /// <param name="name">The set's display name. Defaults to the assembly's simple name.</param>
    /// <returns>The constructed set.</returns>
    /// <remarks>
    /// Manifest resource names are dot-flattened, so after the prefix and the <c>.svg</c> suffix are
    /// removed the <b>last</b> dot-separated segment is the icon name and the segment before it, if
    /// any, is the variant; anything further left is ignored. A uniform layout is expected — if some
    /// resources carry a variant segment and others do not, the ones without are grouped under a
    /// nameless variant that is not listed in <see cref="Variants"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="assembly"/> or <paramref name="resourcePrefix"/> is null.</exception>
    /// <exception cref="ArgumentException">The prefix matches no <c>.svg</c> resource, or <paramref name="defaultVariant"/> is not one of the discovered variants.</exception>
    public static SvgIconSet FromAssembly(
        Assembly assembly,
        string resourcePrefix,
        string? defaultVariant = null,
        string? name = null)
    {
        if (assembly is null)
        {
            throw new ArgumentNullException(nameof(assembly));
        }

        if (resourcePrefix is null)
        {
            throw new ArgumentNullException(nameof(resourcePrefix));
        }

        var matches = new List<string>();
        foreach (string resource in assembly.GetManifestResourceNames())
        {
            if (resource.StartsWith(resourcePrefix, StringComparison.Ordinal)
                && resource.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(resource);
            }
        }

        if (matches.Count == 0)
        {
            // A mistyped prefix silently yielding an empty icon set is a nasty debugging trap.
            throw new ArgumentException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "No embedded resource of assembly '{0}' starts with '{1}' and ends with '.svg'.",
                    assembly.GetName().Name,
                    resourcePrefix),
                nameof(resourcePrefix));
        }

        matches.Sort(StringComparer.Ordinal);

        var index = NewIndex();
        var variants = new List<string>();

        foreach (string resource in matches)
        {
            int take = resource.Length - resourcePrefix.Length - 4;
            if (take <= 0)
            {
                continue;
            }

            string[] segments = resource.Substring(resourcePrefix.Length, take).Split('.');
            string? iconName = IconNameNormalizer.TryNormalize(segments[segments.Length - 1]);
            if (iconName is null)
            {
                continue;
            }

            string? variant = segments.Length >= 2
                ? IconNameNormalizer.TryNormalize(segments[segments.Length - 2])
                : null;

            if (variant is not null)
            {
                iconName = IconNameNormalizer.StripVariantSuffix(iconName, variant);
            }

            Dictionary<string, SvgSource> bucket = variant is null
                ? GetBucket(index, string.Empty, null)
                : GetBucket(index, variant, variants);

            if (!bucket.ContainsKey(iconName))
            {
                bucket.Add(iconName, SvgSource.ForResource(assembly, resource));
            }
        }

        variants.Sort(StringComparer.Ordinal);
        string? resolvedDefault = ResolveDefaultVariant(defaultVariant, variants);
        return new SvgIconSet(name ?? assembly.GetName().Name ?? FallbackSetName, index, variants, resolvedDefault);
    }

    /// <summary>Icons from an explicit list of .svg file paths.</summary>
    /// <param name="files">The file paths. Each file's name supplies the icon name; the set has no variants.</param>
    /// <param name="name">The set's display name. Defaults to <c>SvgIcons</c>.</param>
    /// <returns>The constructed set.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="files"/> is null.</exception>
    /// <exception cref="ArgumentException">The sequence contains a null path.</exception>
    /// <exception cref="FileNotFoundException">A listed file does not exist. Discovery is eager, so this surfaces at construction.</exception>
    public static SvgIconSet FromFiles(
        IEnumerable<string> files,
        string? name = null)
    {
        if (files is null)
        {
            throw new ArgumentNullException(nameof(files));
        }

        var index = NewIndex();
        Dictionary<string, SvgSource> bucket = GetBucket(index, string.Empty, null);

        foreach (string file in files)
        {
            if (file is null)
            {
                throw new ArgumentException("The file list contains a null path.", nameof(files));
            }

            string full = Path.GetFullPath(file);
            if (!File.Exists(full))
            {
                throw new FileNotFoundException("The icon file does not exist.", full);
            }

            string? iconName = IconNameNormalizer.TryNormalize(Path.GetFileNameWithoutExtension(full));
            if (iconName is null || bucket.ContainsKey(iconName))
            {
                continue;
            }

            bucket.Add(iconName, SvgSource.ForFile(full));
        }

        return new SvgIconSet(name ?? FallbackSetName, index, new List<string>(), defaultVariant: null);
    }

    /// <summary>Icons from name/SVG-text pairs — the in-memory escape hatch.</summary>
    /// <param name="sources">The pairs. The key supplies the icon name, the value is the SVG document text; the set has no variants.</param>
    /// <param name="name">The set's display name. Defaults to <c>SvgIcons</c>.</param>
    /// <returns>The constructed set.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sources"/> is null.</exception>
    /// <exception cref="ArgumentException">A pair has a null or blank key or value.</exception>
    public static SvgIconSet FromSvgSources(
        IEnumerable<KeyValuePair<string, string>> sources,
        string? name = null)
    {
        if (sources is null)
        {
            throw new ArgumentNullException(nameof(sources));
        }

        var index = NewIndex();
        Dictionary<string, SvgSource> bucket = GetBucket(index, string.Empty, null);

        foreach (KeyValuePair<string, string> source in sources)
        {
            if (string.IsNullOrWhiteSpace(source.Value))
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, "The SVG text for '{0}' is null, empty or whitespace.", source.Key),
                    nameof(sources));
            }

            string? iconName = IconNameNormalizer.TryNormalize(source.Key);
            if (iconName is null)
            {
                throw new ArgumentException("A source key contains no usable characters.", nameof(sources));
            }

            if (bucket.ContainsKey(iconName))
            {
                continue;
            }

            bucket.Add(iconName, SvgSource.ForText(iconName, source.Value));
        }

        return new SvgIconSet(name ?? FallbackSetName, index, new List<string>(), defaultVariant: null);
    }

    /// <inheritdoc/>
    public bool TryGetGlyph(string icon, string? variant, out IconGlyph? glyph)
    {
        glyph = null;

        // The null guard sits ahead of normalization, so a null never reaches it. A null name is a
        // miss here and an ArgumentNullException from GetGlyph — see IIconSet.
        string? iconKey = IconNameNormalizer.TryNormalize(icon);
        if (iconKey is null)
        {
            return false;
        }

        string bucketKey;
        if (variant is null)
        {
            bucketKey = DefaultVariant ?? string.Empty;
        }
        else
        {
            string? variantKey = IconNameNormalizer.TryNormalize(variant);
            if (variantKey is null)
            {
                return false;
            }

            bucketKey = variantKey;
        }

        // A variant the set does not have is a miss, never a fallback to the default.
        if (!_index.TryGetValue(bucketKey, out Dictionary<string, SvgSource>? bucket)
            || !bucket.TryGetValue(iconKey, out SvgSource? source))
        {
            return false;
        }

        // Deliberately NOT wrapped in a catch: a broken source propagates from TryGetGlyph too.
        glyph = Resolve(bucketKey, iconKey, source);
        return true;
    }

    /// <inheritdoc/>
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
        throw new IconNotFoundException(icon, variant, Name);
    }

    private IconGlyph Resolve(string variant, string icon, SvgSource source)
    {
        var key = (variant, icon);

        // Lazy with ExecutionAndPublication — not a bare GetOrAdd factory — is what guarantees the
        // "cached, reference-equal" and "parsed exactly once" contract even under a race:
        // GetOrAdd's factory can run more than once and hand different instances to concurrent
        // callers.
        Lazy<IconGlyph> lazy = _cache.GetOrAdd(
            key,
            _ => new Lazy<IconGlyph>(source.Parse, LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            return lazy.Value;
        }
        catch
        {
            // A faulted Lazy caches its exception forever, so a transient I/O failure would
            // otherwise poison the entry for the life of the process.
            _cache.TryRemove(key, out _);
            throw;
        }
    }

    private static Dictionary<string, Dictionary<string, SvgSource>> NewIndex()
        => new Dictionary<string, Dictionary<string, SvgSource>>(StringComparer.Ordinal);

    /// <summary>
    /// Fetches or creates one variant bucket. Keys are already normalized at construction, so the
    /// dictionaries use an ordinal comparer and lookups normalize the <i>input</i> instead of paying
    /// for a case-insensitive comparison on every probe.
    /// </summary>
    private static Dictionary<string, SvgSource> GetBucket(
        Dictionary<string, Dictionary<string, SvgSource>> index,
        string variant,
        List<string>? variants)
    {
        if (index.TryGetValue(variant, out Dictionary<string, SvgSource>? bucket))
        {
            return bucket;
        }

        bucket = new Dictionary<string, SvgSource>(StringComparer.Ordinal);
        index.Add(variant, bucket);
        variants?.Add(variant);
        return bucket;
    }

    private static void AddDirectoryFiles(
        Dictionary<string, SvgSource> bucket,
        string root,
        string directory,
        string variant)
    {
        var files = new List<string>();
        foreach (string file in Directory.EnumerateFiles(directory, "*.svg", SearchOption.TopDirectoryOnly))
        {
            // The "*.svg" pattern is not exact on every platform; re-check the extension.
            if (file.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            {
                files.Add(file);
            }
        }

        files.Sort(StringComparer.Ordinal);

        foreach (string file in files)
        {
            string full = Path.GetFullPath(file);

            // Path.GetFullPath does not resolve symlinks, so root/evil.svg -> /etc/passwd yields a
            // path that is literally inside the root. The same portable ReparsePoint test the
            // directory loop uses is therefore the mechanism that stops a symlinked file, and it
            // runs before IsWithin. Skipped silently, like a non-contained path below, so one
            // hostile entry cannot deny service on an otherwise valid directory.
            if ((new FileInfo(full).Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
            {
                continue;
            }

            // Defence-in-depth only: every candidate comes from an enumerator rooted at the root and
            // enumeration is top-directory-only, so this cannot currently reject anything. It guards
            // a future change to how files are discovered. A non-contained path is dropped silently
            // rather than thrown, for the same reason as above.
            if (!PathSafety.IsWithin(root, full))
            {
                continue;
            }

            string? iconName = IconNameNormalizer.TryNormalize(Path.GetFileNameWithoutExtension(full));
            if (iconName is null)
            {
                continue;
            }

            iconName = IconNameNormalizer.StripVariantSuffix(iconName, variant);
            if (bucket.ContainsKey(iconName))
            {
                continue;
            }

            bucket.Add(iconName, SvgSource.ForFile(full));
        }
    }

    private static string? ResolveDefaultVariant(string? requested, List<string> variants)
    {
        if (variants.Count == 0)
        {
            if (requested is not null)
            {
                throw new ArgumentException(
                    "A default variant was supplied but no variants were discovered.",
                    nameof(requested));
            }

            return null;
        }

        if (requested is not null)
        {
            string normalized = IconNameNormalizer.Normalize(requested, nameof(requested));
            if (!variants.Contains(normalized))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The default variant '{0}' is not one of the discovered variants: {1}.",
                        normalized,
                        string.Join(", ", variants.ToArray())),
                    nameof(requested));
            }

            return normalized;
        }

        return variants.Contains("regular") ? "regular" : variants[0];
    }

    /// <summary>One indexed icon source, parsed on demand.</summary>
    private sealed class SvgSource
    {
        private readonly Func<IconGlyph> _parse;

        private SvgSource(string description, Func<IconGlyph> parse)
        {
            Description = description;
            _parse = parse;
        }

        /// <summary>Where the SVG comes from, used in error messages.</summary>
        internal string Description { get; }

        internal static SvgSource ForFile(string path)
            => new SvgSource(
                string.Format(CultureInfo.InvariantCulture, "file '{0}'", path),
                () =>
                {
                    // The Stream overload, so byte-order-mark and encoding detection are the
                    // XmlReader's job.
                    using FileStream stream = File.OpenRead(path);
                    return SvgIconParser.Parse(stream);
                });

        internal static SvgSource ForResource(Assembly assembly, string resourceName)
            => new SvgSource(
                string.Format(CultureInfo.InvariantCulture, "embedded resource '{0}'", resourceName),
                () =>
                {
                    using Stream? stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream is null)
                    {
                        throw new SvgParseException(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "The embedded resource '{0}' could not be opened.",
                                resourceName));
                    }

                    return SvgIconParser.Parse(stream);
                });

        internal static SvgSource ForText(string iconName, string svg)
            => new SvgSource(
                string.Format(CultureInfo.InvariantCulture, "in-memory source '{0}'", iconName),
                () => SvgIconParser.Parse(svg));

        /// <summary>
        /// Parses the source, wrapping an I/O failure — including a file that disappeared between
        /// construction and first parse — in <see cref="SvgParseException"/>.
        /// </summary>
        internal IconGlyph Parse()
        {
            try
            {
                return _parse();
            }
            catch (IOException ex)
            {
                throw new SvgParseException(
                    string.Format(CultureInfo.InvariantCulture, "The icon {0} could not be read.", Description),
                    ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new SvgParseException(
                    string.Format(CultureInfo.InvariantCulture, "The icon {0} could not be read.", Description),
                    ex);
            }
        }
    }
}
