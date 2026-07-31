using System;
using System.Globalization;

namespace Enigma.Icons.Internal;

/// <summary>
/// The shared scanner for SVG's whitespace/comma-separated number syntax, used by path data,
/// transform lists and point lists alike.
/// </summary>
internal static class SvgLexer
{
    /// <summary>Advances past any run of whitespace and commas.</summary>
    /// <param name="text">The text being scanned.</param>
    /// <param name="index">The scan position, advanced in place.</param>
    internal static void SkipSeparators(string text, ref int index)
    {
        while (index < text.Length)
        {
            char c = text[index];
            if (c == ',' || c == ' ' || c == '\t' || c == '\r' || c == '\n' || c == '\f')
            {
                index++;
            }
            else
            {
                return;
            }
        }
    }

    /// <summary>
    /// Reads one SVG number, honouring the mini-language's separator-free forms — <c>.5.5</c> is
    /// two numbers and <c>1-2</c> is <c>1</c> followed by <c>-2</c>.
    /// </summary>
    /// <param name="text">The text being scanned.</param>
    /// <param name="index">The scan position, advanced past the number on success and left untouched on failure.</param>
    /// <param name="value">The parsed, finite number.</param>
    /// <returns><see langword="true"/> when a number was read.</returns>
    internal static bool TryReadNumber(string text, ref int index, out double value)
    {
        value = 0.0;
        SkipSeparators(text, ref index);

        int start = index;
        int i = index;

        if (i < text.Length && (text[i] == '+' || text[i] == '-'))
        {
            i++;
        }

        bool digits = false;
        while (i < text.Length && IsDigit(text[i]))
        {
            i++;
            digits = true;
        }

        if (i < text.Length && text[i] == '.')
        {
            i++;
            while (i < text.Length && IsDigit(text[i]))
            {
                i++;
                digits = true;
            }
        }

        if (!digits)
        {
            index = start;
            return false;
        }

        if (i < text.Length && (text[i] == 'e' || text[i] == 'E'))
        {
            int beforeExponent = i;
            i++;
            if (i < text.Length && (text[i] == '+' || text[i] == '-'))
            {
                i++;
            }

            bool exponentDigits = false;
            while (i < text.Length && IsDigit(text[i]))
            {
                i++;
                exponentDigits = true;
            }

            if (!exponentDigits)
            {
                // Not an exponent after all — e.g. the "e" of a following command letter.
                i = beforeExponent;
            }
        }

        string token = text.Substring(start, i - start);
        if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
            || double.IsNaN(parsed)
            || double.IsInfinity(parsed))
        {
            index = start;
            return false;
        }

        index = i;
        value = parsed;
        return true;
    }

    /// <summary>
    /// Reads one arc flag. SVG allows flags to run together without separators (<c>0 11 200,0</c>),
    /// so a flag is exactly one <c>0</c> or <c>1</c> character — never a general number.
    /// </summary>
    /// <param name="text">The text being scanned.</param>
    /// <param name="index">The scan position, advanced past the flag on success.</param>
    /// <param name="flag">The parsed flag.</param>
    /// <returns><see langword="true"/> when a flag was read.</returns>
    internal static bool TryReadFlag(string text, ref int index, out bool flag)
    {
        SkipSeparators(text, ref index);
        if (index < text.Length && (text[index] == '0' || text[index] == '1'))
        {
            flag = text[index] == '1';
            index++;
            return true;
        }

        flag = false;
        return false;
    }

    /// <summary>True for an ASCII digit. <see cref="char.IsDigit(char)"/> also accepts non-ASCII digits.</summary>
    /// <param name="c">The character to test.</param>
    /// <returns><see langword="true"/> for <c>0</c>–<c>9</c>.</returns>
    internal static bool IsDigit(char c) => c >= '0' && c <= '9';

    /// <summary>True for an ASCII letter.</summary>
    /// <param name="c">The character to test.</param>
    /// <returns><see langword="true"/> for <c>A</c>–<c>Z</c> or <c>a</c>–<c>z</c>.</returns>
    internal static bool IsLetter(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
}
