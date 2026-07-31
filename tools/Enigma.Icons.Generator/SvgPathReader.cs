using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

namespace Enigma.Icons.Generator;

/// <summary>
/// Extracts the root <c>viewBox</c> and the ordered <c>(d, opacity)</c> list from a single upstream
/// SVG file (SPEC §8.3 step 4).
/// </summary>
internal static class SvgPathReader
{
    // MANDATORY, not cosmetic — SPEC §5.2. The pinned snapshot is trusted, but the hardened posture
    // costs nothing and keeps exactly one XML idiom in the solution: DtdProcessing.Prohibit closes
    // XXE and entity-expansion outright, XmlResolver = null blocks every other external reference.
    private static readonly XmlReaderSettings ReaderSettings = new XmlReaderSettings
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        IgnoreComments = true,
        IgnoreProcessingInstructions = true,
        IgnoreWhitespace = true,
        CloseInput = false,
    };

    /// <summary>
    /// Reads one file. Elements and attributes outside <c>&lt;svg&gt;</c>/<c>&lt;path&gt;</c> and
    /// <c>d</c>/<c>opacity</c> are ignored per SPEC §8.3 step 4, but tallied into
    /// <paramref name="vocabulary"/> so that dropping artwork information can never be silent.
    /// </summary>
    /// <param name="filePath">The file to read.</param>
    /// <param name="displayName">The weight-relative name used in diagnostics.</param>
    /// <param name="vocabulary">Receives the unexpected-vocabulary tally.</param>
    /// <param name="viewBox">The root element's <c>viewBox</c>, verbatim.</param>
    /// <returns>Every <c>&lt;path&gt;</c> of the file, in document (paint) order.</returns>
    internal static List<LayerRecord> Read(
        string filePath,
        string displayName,
        Vocabulary vocabulary,
        out string viewBox)
    {
        var layers = new List<LayerRecord>();
        string? rawViewBox = null;
        bool rootSeen = false;

        using (FileStream stream = File.OpenRead(filePath))
        using (XmlReader reader = XmlReader.Create(stream, ReaderSettings))
        {
            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    continue;
                }

                // Matching on LocalName honours the SVG namespace without requiring it — SPEC §5.2.
                string name = reader.LocalName;

                if (!rootSeen)
                {
                    if (!string.Equals(name, "svg", StringComparison.Ordinal))
                    {
                        throw new GeneratorException(
                            Message(displayName, "its root element is <" + name + ">, not <svg>."));
                    }

                    rootSeen = true;

                    // No root attribute is required: SPEC §7.4.4's two duotone files carry no
                    // fill="currentColor", and the generator never looks at fill at all.
                    rawViewBox = reader.GetAttribute("viewBox");
                    continue;
                }

                if (string.Equals(name, "path", StringComparison.Ordinal))
                {
                    layers.Add(ReadPath(reader, displayName, vocabulary));
                }
                else
                {
                    vocabulary.AddElement(name);
                }
            }
        }

        if (!rootSeen)
        {
            throw new GeneratorException(Message(displayName, "it contains no elements."));
        }

        if (layers.Count == 0)
        {
            throw new GeneratorException(Message(displayName, "it contains no <path> element."));
        }

        viewBox = ValidateViewBox(rawViewBox, displayName);
        return layers;
    }

    private static LayerRecord ReadPath(XmlReader reader, string displayName, Vocabulary vocabulary)
    {
        if (reader.HasAttributes)
        {
            reader.MoveToFirstAttribute();
            do
            {
                string attribute = reader.Name;
                if (!string.Equals(attribute, "d", StringComparison.Ordinal)
                    && !string.Equals(attribute, "opacity", StringComparison.Ordinal))
                {
                    vocabulary.AddPathAttribute(attribute);
                }
            }
            while (reader.MoveToNextAttribute());

            reader.MoveToElement();
        }

        string? pathData = reader.GetAttribute("d");
        if (string.IsNullOrWhiteSpace(pathData))
        {
            throw new GeneratorException(
                Message(displayName, "a <path> has a missing or empty 'd' attribute."));
        }

        // The v1 line format is TAB-delimited, one line per icon, with '@' introducing the opacity
        // prefix — so a 'd' carrying any of those cannot be represented. Rewriting it would breach
        // SPEC §8.3's verbatim rule, so the only safe answer is to abort. No upstream 'd' does this.
        if (pathData.IndexOf('\t') >= 0 || pathData.IndexOf('\r') >= 0 || pathData.IndexOf('\n') >= 0)
        {
            throw new GeneratorException(
                Message(displayName, "a <path>'s 'd' attribute contains a TAB or newline, which the v1 .dat line format cannot represent."));
        }

        if (pathData[0] == '@')
        {
            throw new GeneratorException(
                Message(displayName, "a <path>'s 'd' attribute begins with '@', which the v1 .dat layer field reserves for the opacity prefix."));
        }

        double opacity = 1d;
        string? rawOpacity = reader.GetAttribute("opacity");
        if (rawOpacity is not null)
        {
            if (!double.TryParse(rawOpacity, NumberStyles.Float, CultureInfo.InvariantCulture, out opacity)
                || double.IsNaN(opacity)
                || opacity < 0d
                || opacity > 1d)
            {
                throw new GeneratorException(
                    Message(displayName, "a <path> has an 'opacity' attribute that is not a number in [0,1]: \"" + rawOpacity + "\"."));
            }
        }

        return new LayerRecord(pathData, opacity);
    }

    private static string ValidateViewBox(string? rawViewBox, string displayName)
    {
        if (rawViewBox is null)
        {
            throw new GeneratorException(Message(displayName, "its <svg> element has no 'viewBox' attribute."));
        }

        // Split on space and comma only — never on general whitespace — so a viewBox carrying a TAB
        // or a newline is rejected rather than smuggled into the TAB-delimited header line.
        string[] parts = rawViewBox.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
        bool valid = parts.Length == 4;
        if (valid)
        {
            foreach (string part in parts)
            {
                if (!double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out double _))
                {
                    valid = false;
                    break;
                }
            }
        }

        if (!valid || rawViewBox.IndexOf('\t') >= 0)
        {
            throw new GeneratorException(
                Message(displayName, "its 'viewBox' is not four numbers: \"" + rawViewBox + "\"."));
        }

        return rawViewBox;
    }

    private static string Message(string displayName, string problem)
        => displayName + ": " + problem;
}

/// <summary>
/// Counts the elements and <c>&lt;path&gt;</c> attributes the generator ignored, so that a future
/// upstream refresh introducing artwork information the v1 format cannot carry is reported instead
/// of silently dropped (FEATURE-2DDE design step 4.8).
/// </summary>
internal sealed class Vocabulary
{
    private readonly SortedDictionary<string, int> _elements = new SortedDictionary<string, int>(StringComparer.Ordinal);
    private readonly SortedDictionary<string, int> _pathAttributes = new SortedDictionary<string, int>(StringComparer.Ordinal);

    /// <summary>True when nothing unexpected was seen — the state SPEC §7.4.1-2 guarantees today.</summary>
    internal bool IsEmpty => _elements.Count == 0 && _pathAttributes.Count == 0;

    /// <summary>Unexpected element names with their occurrence counts, ordinal-sorted.</summary>
    internal IEnumerable<KeyValuePair<string, int>> Elements => _elements;

    /// <summary>Unexpected <c>&lt;path&gt;</c> attribute names with their occurrence counts, ordinal-sorted.</summary>
    internal IEnumerable<KeyValuePair<string, int>> PathAttributes => _pathAttributes;

    /// <summary>Records one occurrence of an element other than <c>&lt;svg&gt;</c> or <c>&lt;path&gt;</c>.</summary>
    internal void AddElement(string name) => Increment(_elements, name);

    /// <summary>Records one occurrence of a <c>&lt;path&gt;</c> attribute other than <c>d</c> or <c>opacity</c>.</summary>
    internal void AddPathAttribute(string name) => Increment(_pathAttributes, name);

    private static void Increment(SortedDictionary<string, int> counts, string name)
    {
        counts.TryGetValue(name, out int count);
        counts[name] = count + 1;
    }
}
