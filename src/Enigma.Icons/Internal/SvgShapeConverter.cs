using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml;

namespace Enigma.Icons.Internal;

/// <summary>
/// Converts the basic SVG shapes into path data. One converter per supported shape element.
/// </summary>
/// <remarks>
/// A shape whose dimensions make it non-rendering per the SVG specification — a zero or negative
/// <c>width</c>, <c>height</c>, <c>r</c>, <c>rx</c> or <c>ry</c> — produces <b>no</b> layer rather
/// than an exception. An attribute that is present but unparseable is an error.
/// </remarks>
internal static class SvgShapeConverter
{
    /// <summary>Converts the element the reader is positioned on into path data.</summary>
    /// <param name="element">The element's local name.</param>
    /// <param name="reader">The reader, positioned on the element.</param>
    /// <returns>The path data, or null when the shape paints nothing.</returns>
    /// <exception cref="SvgParseException">An attribute is present but unparseable.</exception>
    internal static string? Convert(string element, XmlReader reader)
    {
        switch (element)
        {
            case "rect":
                return ConvertRect(reader);
            case "circle":
                return ConvertCircle(reader);
            case "ellipse":
                return ConvertEllipse(reader);
            case "line":
                return ConvertLine(reader);
            case "polyline":
                return ConvertPolyline(reader, closed: false);
            case "polygon":
                return ConvertPolyline(reader, closed: true);
            default:
                return null;
        }
    }

    private static string? ConvertRect(XmlReader reader)
    {
        double x = ReadNumber(reader, "rect", "x", 0.0);
        double y = ReadNumber(reader, "rect", "y", 0.0);
        double width = ReadNumber(reader, "rect", "width", 0.0);
        double height = ReadNumber(reader, "rect", "height", 0.0);

        if (!(width > 0) || !(height > 0))
        {
            return null;
        }

        // SVG's rx/ry mirroring rule: one supplied radius mirrors to the other axis; a negative
        // value counts as absent.
        double? rx = ReadOptionalNumber(reader, "rect", "rx");
        double? ry = ReadOptionalNumber(reader, "rect", "ry");
        if (rx < 0)
        {
            rx = null;
        }

        if (ry < 0)
        {
            ry = null;
        }

        rx ??= ry;
        ry ??= rx;

        var builder = new StringBuilder();
        if (rx is null || ry is null || !(rx > 0) || !(ry > 0))
        {
            // Sharp corners. Explicit L rather than H/V: the transformer has to expand H/V anyway,
            // because a horizontal segment is no longer horizontal after a rotation.
            builder.Append("M ");
            AppendPoint(builder, x, y);
            builder.Append(" L ");
            AppendPoint(builder, x + width, y);
            builder.Append(" L ");
            AppendPoint(builder, x + width, y + height);
            builder.Append(" L ");
            AppendPoint(builder, x, y + height);
            builder.Append(" Z");
            return builder.ToString();
        }

        double radiusX = Math.Min(rx.Value, width / 2.0);
        double radiusY = Math.Min(ry.Value, height / 2.0);

        // Corner arcs use sweep-flag 1: SVG's y-axis points down, so the positive angular direction
        // is the on-screen clockwise direction the top→right→bottom→left corner sequence follows.
        // large-arc-flag is 0 — a corner is a quarter arc.
        builder.Append("M ");
        AppendPoint(builder, x + radiusX, y);
        builder.Append(" L ");
        AppendPoint(builder, x + width - radiusX, y);
        AppendCornerArc(builder, radiusX, radiusY, x + width, y + radiusY);
        builder.Append(" L ");
        AppendPoint(builder, x + width, y + height - radiusY);
        AppendCornerArc(builder, radiusX, radiusY, x + width - radiusX, y + height);
        builder.Append(" L ");
        AppendPoint(builder, x + radiusX, y + height);
        AppendCornerArc(builder, radiusX, radiusY, x, y + height - radiusY);
        builder.Append(" L ");
        AppendPoint(builder, x, y + radiusY);
        AppendCornerArc(builder, radiusX, radiusY, x + radiusX, y);
        builder.Append(" Z");
        return builder.ToString();
    }

    private static string? ConvertCircle(XmlReader reader)
    {
        double cx = ReadNumber(reader, "circle", "cx", 0.0);
        double cy = ReadNumber(reader, "circle", "cy", 0.0);
        double r = ReadNumber(reader, "circle", "r", 0.0);

        if (!(r > 0))
        {
            return null;
        }

        return TwoArcEllipse(cx, cy, r, r);
    }

    private static string? ConvertEllipse(XmlReader reader)
    {
        double cx = ReadNumber(reader, "ellipse", "cx", 0.0);
        double cy = ReadNumber(reader, "ellipse", "cy", 0.0);
        double rx = ReadNumber(reader, "ellipse", "rx", 0.0);
        double ry = ReadNumber(reader, "ellipse", "ry", 0.0);

        if (!(rx > 0) || !(ry > 0))
        {
            return null;
        }

        return TwoArcEllipse(cx, cy, rx, ry);
    }

    private static string ConvertLine(XmlReader reader)
    {
        double x1 = ReadNumber(reader, "line", "x1", 0.0);
        double y1 = ReadNumber(reader, "line", "y1", 0.0);
        double x2 = ReadNumber(reader, "line", "x2", 0.0);
        double y2 = ReadNumber(reader, "line", "y2", 0.0);

        var builder = new StringBuilder();
        builder.Append("M ");
        AppendPoint(builder, x1, y1);
        builder.Append(" L ");
        AppendPoint(builder, x2, y2);

        // Emitted even though it is invisible unless stroked — renderers skip non-filled,
        // non-stroked layers (SPEC §5.1).
        return builder.ToString();
    }

    private static string? ConvertPolyline(XmlReader reader, bool closed)
    {
        string? raw = reader.GetAttribute("points");
        if (raw is null)
        {
            return null;
        }

        List<double> numbers = SvgValueParser.ParseNumberList(raw);

        // SVG error handling: a trailing odd number is dropped — render up to the last complete pair.
        int pairs = numbers.Count / 2;
        if (pairs < 2)
        {
            return null;
        }

        var builder = new StringBuilder();
        builder.Append("M ");
        AppendPoint(builder, numbers[0], numbers[1]);
        for (int i = 1; i < pairs; i++)
        {
            builder.Append(" L ");
            AppendPoint(builder, numbers[i * 2], numbers[(i * 2) + 1]);
        }

        if (closed)
        {
            builder.Append(" Z");
        }

        return builder.ToString();
    }

    private static string TwoArcEllipse(double cx, double cy, double rx, double ry)
    {
        var builder = new StringBuilder();
        builder.Append("M ");
        AppendPoint(builder, cx - rx, cy);
        AppendSemicircularArc(builder, rx, ry, cx + rx, cy);
        AppendSemicircularArc(builder, rx, ry, cx - rx, cy);
        builder.Append(" Z");
        return builder.ToString();
    }

    private static void AppendSemicircularArc(StringBuilder builder, double rx, double ry, double endX, double endY)
    {
        builder.Append(" A ");
        AppendPoint(builder, rx, ry);
        builder.Append(" 0 1 1 ");
        AppendPoint(builder, endX, endY);
    }

    private static void AppendCornerArc(StringBuilder builder, double rx, double ry, double endX, double endY)
    {
        builder.Append(" A ");
        AppendPoint(builder, rx, ry);
        builder.Append(" 0 0 1 ");
        AppendPoint(builder, endX, endY);
    }

    private static void AppendPoint(StringBuilder builder, double x, double y)
    {
        builder.Append(SvgNumber.Format(x));
        builder.Append(',');
        builder.Append(SvgNumber.Format(y));
    }

    private static double ReadNumber(XmlReader reader, string element, string attribute, double fallback)
        => ReadOptionalNumber(reader, element, attribute) ?? fallback;

    private static double? ReadOptionalNumber(XmlReader reader, string element, string attribute)
    {
        string? raw = reader.GetAttribute(attribute);
        if (raw is null || raw.Trim().Length == 0)
        {
            return null;
        }

        if (!SvgValueParser.TryParseNumber(raw, out double value))
        {
            throw new SvgParseException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Attribute '{0}' of <{1}> is not a valid number: \"{2}\".",
                    attribute,
                    element,
                    raw));
        }

        return value;
    }
}
