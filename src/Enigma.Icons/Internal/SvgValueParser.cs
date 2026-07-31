using System;
using System.Collections.Generic;
using System.Globalization;

namespace Enigma.Icons.Internal;

/// <summary>
/// Attribute-value parsing: numbers, lengths, point lists, view boxes, opacity, and the
/// presentation keywords.
/// </summary>
internal static class SvgValueParser
{
    /// <summary>Parses a bare SVG number in the invariant culture.</summary>
    /// <param name="value">The raw attribute value, or null.</param>
    /// <param name="result">The parsed, finite value.</param>
    /// <returns><see langword="true"/> when the whole value is one finite number.</returns>
    internal static bool TryParseNumber(string? value, out double result)
    {
        result = 0.0;
        if (value is null)
        {
            return false;
        }

        string trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return false;
        }

        if (!double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
        {
            return false;
        }

        // "NaN" and "Infinity" parse successfully on modern runtimes — never accept them as a
        // coordinate.
        if (double.IsNaN(parsed) || double.IsInfinity(parsed))
        {
            return false;
        }

        result = parsed;
        return true;
    }

    /// <summary>Parses a <c>width</c>/<c>height</c> length: a bare number, or one with a <c>px</c> suffix.</summary>
    /// <param name="value">The raw attribute value, or null.</param>
    /// <param name="result">The parsed length in user units.</param>
    /// <returns><see langword="true"/> when the value is a usable length. A percentage or any other unit returns <see langword="false"/>.</returns>
    internal static bool TryParseLength(string? value, out double result)
    {
        result = 0.0;
        if (value is null)
        {
            return false;
        }

        string trimmed = value.Trim();
        if (trimmed.EndsWith("px", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed.Substring(0, trimmed.Length - 2);
        }

        return TryParseNumber(trimmed, out result);
    }

    /// <summary>Parses a <c>viewBox</c> attribute: exactly four whitespace- or comma-separated numbers.</summary>
    /// <param name="value">The raw attribute value, or null.</param>
    /// <param name="result">The parsed view box.</param>
    /// <returns><see langword="true"/> when the value is four numbers with a positive width and height.</returns>
    internal static bool TryParseViewBox(string? value, out IconViewBox result)
    {
        result = IconViewBox.Default;
        if (value is null)
        {
            return false;
        }

        List<double> numbers = ParseNumberList(value);
        if (numbers.Count != 4)
        {
            return false;
        }

        if (!(numbers[2] > 0) || !(numbers[3] > 0))
        {
            return false;
        }

        result = new IconViewBox(numbers[0], numbers[1], numbers[2], numbers[3]);
        return true;
    }

    /// <summary>Parses a whitespace- and/or comma-separated list of numbers.</summary>
    /// <param name="value">The raw attribute value.</param>
    /// <returns>The numbers, in order. Stops at the first token that is not a number.</returns>
    internal static List<double> ParseNumberList(string value)
    {
        var numbers = new List<double>();
        int index = 0;
        while (SvgLexer.TryReadNumber(value, ref index, out double number))
        {
            numbers.Add(number);
        }

        return numbers;
    }

    /// <summary>Parses an <c>opacity</c> value, clamped into the closed range 0.0–1.0.</summary>
    /// <param name="value">The raw attribute value, or null.</param>
    /// <param name="result">The clamped opacity.</param>
    /// <returns><see langword="true"/> when the value is a number. An unparseable value is treated as unspecified.</returns>
    internal static bool TryParseOpacity(string? value, out double result)
    {
        if (!TryParseNumber(value, out double parsed))
        {
            result = 1.0;
            return false;
        }

        // Math.Clamp is .NET Standard 2.1+.
        result = parsed < 0.0 ? 0.0 : parsed > 1.0 ? 1.0 : parsed;
        return true;
    }

    /// <summary>Maps a <c>fill-rule</c> keyword. An unrecognized keyword falls back to the CSS initial value.</summary>
    /// <param name="value">The raw attribute value.</param>
    /// <returns>The mapped fill rule.</returns>
    internal static IconFillRule ParseFillRule(string value)
        => string.Equals(value, "evenodd", StringComparison.OrdinalIgnoreCase)
            ? IconFillRule.EvenOdd
            : IconFillRule.NonZero;

    /// <summary>Maps a <c>stroke-linecap</c> keyword. An unrecognized keyword falls back to the CSS initial value.</summary>
    /// <param name="value">The raw attribute value.</param>
    /// <returns>The mapped line cap.</returns>
    internal static IconLineCap ParseLineCap(string value)
    {
        if (string.Equals(value, "round", StringComparison.OrdinalIgnoreCase))
        {
            return IconLineCap.Round;
        }

        if (string.Equals(value, "square", StringComparison.OrdinalIgnoreCase))
        {
            return IconLineCap.Square;
        }

        return IconLineCap.Flat;
    }

    /// <summary>Maps a <c>stroke-linejoin</c> keyword. An unrecognized keyword falls back to the CSS initial value.</summary>
    /// <param name="value">The raw attribute value.</param>
    /// <returns>The mapped line join.</returns>
    internal static IconLineJoin ParseLineJoin(string value)
    {
        if (string.Equals(value, "round", StringComparison.OrdinalIgnoreCase))
        {
            return IconLineJoin.Round;
        }

        if (string.Equals(value, "bevel", StringComparison.OrdinalIgnoreCase))
        {
            return IconLineJoin.Bevel;
        }

        return IconLineJoin.Miter;
    }
}
