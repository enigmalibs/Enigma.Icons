using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using Enigma.Icons.Internal;

namespace Enigma.Icons;

/// <summary>
/// Parses the "shapes, groups and transforms" subset of SVG into an <see cref="IconGlyph"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Supported:</b> <c>&lt;path&gt;</c> (whose <c>d</c> is taken verbatim), <c>&lt;rect&gt;</c>,
/// <c>&lt;circle&gt;</c>, <c>&lt;ellipse&gt;</c>, <c>&lt;line&gt;</c>, <c>&lt;polyline&gt;</c>,
/// <c>&lt;polygon&gt;</c>, <c>&lt;g&gt;</c> nesting with presentation-attribute inheritance,
/// <c>transform</c> lists, <c>opacity</c>, <c>fill</c>, <c>fill-rule</c>, <c>stroke</c> and its
/// three companions, and <c>viewBox</c>.
/// </para>
/// <para>
/// <b>Not supported</b>, and documented as such: gradients, <c>clipPath</c>, <c>mask</c>,
/// <c>&lt;defs&gt;</c>/<c>&lt;use&gt;</c>/<c>&lt;symbol&gt;</c>, <c>style="…"</c> attributes and
/// <c>&lt;style&gt;</c> CSS blocks, <c>&lt;text&gt;</c>, <c>&lt;image&gt;</c>, filters, animation,
/// and <c>fill-opacity</c>/<c>stroke-opacity</c>. A document whose only paintable content sits
/// inside an unsupported construct raises <see cref="SvgParseException"/> naming that construct.
/// For arbitrary artwork use a full SVG renderer such as <c>Avalonia.Svg</c> or <c>Svg.Skia</c> —
/// this library is about icons.
/// </para>
/// <para>
/// <b>Security.</b> Input is treated as untrusted: DTDs are prohibited outright (so neither XXE nor
/// entity-expansion attacks are possible), no external resource is ever resolved, and input larger
/// than <see cref="MaxDocumentBytes"/> is rejected before parsing.
/// </para>
/// </remarks>
public static class SvgIconParser
{
    private const int DefaultMaxDocumentBytes = 1024 * 1024;

    private static volatile int _maxDocumentBytes = DefaultMaxDocumentBytes;

    /// <summary>Maximum accepted document size in bytes. Default 1 MiB.</summary>
    /// <remarks>
    /// This is <b>process-wide</b> mutable configuration: changing it affects every caller on every
    /// thread. Tests that lower it must restore it.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is not greater than zero.</exception>
    public static int MaxDocumentBytes
    {
        get => _maxDocumentBytes;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "The maximum document size must be greater than zero.");
            }

            _maxDocumentBytes = value;
        }
    }

    /// <summary>Parses an SVG document into a glyph.</summary>
    /// <param name="svg">The SVG document text.</param>
    /// <returns>The parsed glyph.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="svg"/> is null.</exception>
    /// <exception cref="SvgParseException">The document is malformed, exceeds <see cref="MaxDocumentBytes"/>, or contains no
    /// paintable element.</exception>
    public static IconGlyph Parse(string svg)
    {
        if (svg is null)
        {
            throw new ArgumentNullException(nameof(svg));
        }

        if (svg.Trim().Length == 0)
        {
            throw new SvgParseException("The SVG document is empty.");
        }

        int limit = MaxDocumentBytes;

        // A UTF-8 byte count is always >= the UTF-16 code-unit count, so a string longer than the
        // limit is over it without measuring; otherwise take the exact count.
        int size = svg.Length > limit ? svg.Length : Encoding.UTF8.GetByteCount(svg);
        if (size > limit)
        {
            throw TooLarge(size, limit);
        }

        using var textReader = new StringReader(svg);
        using XmlReader reader = XmlReader.Create(textReader, CreateSettings());
        return ParseCore(reader);
    }

    /// <summary>Parses an SVG document from a stream. The stream is read to the end but
    /// not disposed.</summary>
    /// <param name="stream">The stream to read the SVG document from.</param>
    /// <returns>The parsed glyph.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
    /// <exception cref="SvgParseException">The document is malformed, exceeds <see cref="MaxDocumentBytes"/>, or contains no
    /// paintable element.</exception>
    public static IconGlyph Parse(Stream stream)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        int limit = MaxDocumentBytes;
        using MemoryStream buffer = ReadBounded(stream, limit);

        // The buffered copy — never the caller's stream — is handed to XmlReader, so the XML
        // declaration's encoding and any byte-order mark are honoured.
        using XmlReader reader = XmlReader.Create(buffer, CreateSettings());
        return ParseCore(reader);
    }

    private static MemoryStream ReadBounded(Stream stream, int limit)
    {
        // Never trust stream.Length: the stream may be non-seekable. Read at most limit + 1 bytes;
        // if the extra byte materializes the document is over the limit.
        var buffer = new MemoryStream();
        byte[] chunk = new byte[8192];
        int total = 0;

        while (true)
        {
            int wanted = Math.Min(chunk.Length, (limit + 1) - total);
            if (wanted <= 0)
            {
                break;
            }

            int read;
            try
            {
                read = stream.Read(chunk, 0, wanted);
            }
            catch (IOException ex)
            {
                buffer.Dispose();
                throw new SvgParseException("The SVG document could not be read from its stream.", ex);
            }

            if (read <= 0)
            {
                break;
            }

            buffer.Write(chunk, 0, read);
            total += read;
        }

        if (total > limit)
        {
            buffer.Dispose();
            throw TooLarge(total, limit);
        }

        if (total == 0)
        {
            buffer.Dispose();
            throw new SvgParseException("The SVG document is empty.");
        }

        buffer.Position = 0;
        return buffer;
    }

    private static SvgParseException TooLarge(int size, int limit)
        => new SvgParseException(
            string.Format(
                CultureInfo.InvariantCulture,
                "The SVG document is {0} bytes, which exceeds the {1}-byte limit set by SvgIconParser.MaxDocumentBytes.",
                size,
                limit));

    private static XmlReaderSettings CreateSettings()
    {
        // MANDATORY, not cosmetic — do not "simplify" this.
        //
        // On netstandard2.0 / .NET Framework, XmlDocument's and XmlTextReader's defaults resolve
        // DTDs and external entities: XmlDocument.LoadXml there will happily dereference
        // <!ENTITY x SYSTEM "file:///etc/passwd"> (XXE — arbitrary local-file disclosure) and will
        // expand a nested-entity bomb ("billion laughs") until memory is exhausted. This parser's
        // input is arbitrary user SVG, so those defaults are unacceptable.
        //
        // DtdProcessing.Prohibit closes both holes at once: the DTD is rejected outright as an
        // XmlException, so no entity is ever DEFINED, let alone resolved or expanded.
        // XmlResolver = null is belt-and-braces for any other external reference.
        return new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = true,
            CloseInput = false,
        };
    }

    private static IconGlyph ParseCore(XmlReader reader)
    {
        try
        {
            return Walk(reader);
        }
        catch (XmlException ex)
        {
            // The typed contract for malformed SVG, with the XmlException preserved as the cause.
            throw new SvgParseException("The SVG document is not well-formed XML.", ex);
        }
    }

    private static IconGlyph Walk(XmlReader reader)
    {
        var layers = new List<IconLayer>();

        // The walk is ITERATIVE over an explicit stack, never recursive, so a pathologically nested
        // document cannot overflow the stack; nesting memory is bounded by MaxDocumentBytes.
        var stack = new Stack<StyleFrame>();

        IconViewBox viewBox = IconViewBox.Default;
        string? firstUnsupported = null;
        bool rootSeen = false;
        bool read = reader.Read();

        while (read)
        {
            bool alreadyAdvanced = false;

            if (reader.NodeType == XmlNodeType.EndElement)
            {
                PopTo(stack, reader.Depth);
            }
            else if (reader.NodeType == XmlNodeType.Element)
            {
                // Empty elements produce no EndElement, so unwind before looking at this one.
                PopTo(stack, reader.Depth);

                // Matching on LocalName honours the SVG namespace without requiring it; SPEC §5.2.
                string name = reader.LocalName;

                if (!rootSeen)
                {
                    if (!string.Equals(name, "svg", StringComparison.Ordinal))
                    {
                        throw new SvgParseException(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "The document's root element is not <svg> but <{0}>.",
                                name));
                    }

                    rootSeen = true;
                    viewBox = ReadViewBox(reader);

                    // The root participates in inheritance like any <g> — upstream Phosphor artwork
                    // carries fill="currentColor" on <svg> itself.
                    SvgStyleContext rootContext = SvgStyleContext.Root.Merge(ReadPresentation(reader));
                    if (!reader.IsEmptyElement)
                    {
                        stack.Push(new StyleFrame(rootContext, reader.Depth));
                    }
                }
                else
                {
                    SvgStyleContext parent = stack.Count > 0 ? stack.Peek().Context : SvgStyleContext.Root;

                    switch (name)
                    {
                        case "g":
                        {
                            SvgStyleContext context = parent.Merge(ReadPresentation(reader));
                            if (!reader.IsEmptyElement)
                            {
                                stack.Push(new StyleFrame(context, reader.Depth));
                            }

                            break;
                        }

                        case "path":
                        case "rect":
                        case "circle":
                        case "ellipse":
                        case "line":
                        case "polyline":
                        case "polygon":
                        {
                            if (reader.GetAttribute("style") is not null)
                            {
                                // The geometry is still usable; only the CSS-expressed paint is
                                // lost, so this records the gap without suppressing the layer.
                                firstUnsupported ??= "style attribute";
                            }

                            IconLayer? layer = BuildLayer(reader, name, parent);
                            if (layer is not null)
                            {
                                // Document order is paint order; index 0 is bottom-most.
                                layers.Add(layer);
                            }

                            break;
                        }

                        case "title":
                        case "desc":
                        case "metadata":
                            // Non-paintable, not "unsupported" — never named in an error message.
                            reader.Skip();
                            alreadyAdvanced = true;
                            break;

                        default:
                            firstUnsupported ??= "<" + name + ">";
                            reader.Skip();
                            alreadyAdvanced = true;
                            break;
                    }
                }
            }

            read = alreadyAdvanced
                ? reader.ReadState == ReadState.Interactive
                : reader.Read();
        }

        if (!rootSeen)
        {
            throw new SvgParseException("The SVG document contains no elements.");
        }

        if (layers.Count == 0)
        {
            throw firstUnsupported is null
                ? new SvgParseException("The SVG document contains no paintable element.")
                : new SvgParseException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The SVG document contains no supported paintable element; its only paintable content is inside an unsupported {0} construct. Enigma.Icons supports shapes, groups and transforms only — see the package README.",
                        firstUnsupported));
        }

        return new IconGlyph(viewBox, layers);
    }

    private static void PopTo(Stack<StyleFrame> stack, int depth)
    {
        // Depths are tracked alongside the pushed contexts so the stack cannot desynchronize.
        while (stack.Count > 0 && stack.Peek().Depth >= depth)
        {
            stack.Pop();
        }
    }

    private static IconLayer? BuildLayer(XmlReader reader, string element, in SvgStyleContext parent)
    {
        SvgStyleContext context = parent.Merge(ReadPresentation(reader));

        string? pathData;
        if (string.Equals(element, "path", StringComparison.Ordinal))
        {
            // Verbatim, per SPEC §5.1 — not trimmed, not rewritten.
            pathData = reader.GetAttribute("d");

            // An empty <path> paints nothing; that is not an error.
            if (string.IsNullOrWhiteSpace(pathData))
            {
                return null;
            }
        }
        else
        {
            pathData = SvgShapeConverter.Convert(element, reader);
            if (pathData is null)
            {
                return null;
            }
        }

        string baked = SvgPathTransformer.Bake(pathData!, context.Transform, "<" + element + ">");

        return new IconLayer(
            baked,
            context.Opacity,
            context.FillRule,
            context.Fill,
            context.Stroke,
            context.StrokeWidth,
            context.StrokeLineCap,
            context.StrokeLineJoin);
    }

    private static SvgPresentationAttributes ReadPresentation(XmlReader reader)
        => new SvgPresentationAttributes(
            reader.GetAttribute("transform"),
            reader.GetAttribute("opacity"),
            reader.GetAttribute("fill"),
            reader.GetAttribute("stroke"),
            reader.GetAttribute("stroke-width"),
            reader.GetAttribute("stroke-linecap"),
            reader.GetAttribute("stroke-linejoin"),
            reader.GetAttribute("fill-rule"));

    private static IconViewBox ReadViewBox(XmlReader reader)
    {
        string? raw = reader.GetAttribute("viewBox");
        if (raw is not null)
        {
            // The fallback ladder is for an ABSENT viewBox, not a broken one.
            if (!SvgValueParser.TryParseViewBox(raw, out IconViewBox parsed))
            {
                throw new SvgParseException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Attribute 'viewBox' of <svg> is not four numbers with a positive width and height: \"{0}\".",
                        raw));
            }

            return parsed;
        }

        // A percentage width — common on a root <svg> — is "not usable", so the ladder falls
        // through to the default rather than throwing.
        if (SvgValueParser.TryParseLength(reader.GetAttribute("width"), out double width)
            && SvgValueParser.TryParseLength(reader.GetAttribute("height"), out double height)
            && width > 0
            && height > 0)
        {
            return new IconViewBox(0, 0, width, height);
        }

        return IconViewBox.Default;
    }

    private readonly struct StyleFrame
    {
        internal StyleFrame(SvgStyleContext context, int depth)
        {
            Context = context;
            Depth = depth;
        }

        internal SvgStyleContext Context { get; }

        internal int Depth { get; }
    }
}
