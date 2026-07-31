using System;
using System.Collections.Generic;
using System.Globalization;

namespace Enigma.Icons.Internal;

/// <summary>
/// Parses a <c>transform="…"</c> attribute into a single composed <see cref="Matrix2D"/>.
/// </summary>
/// <remarks>
/// <b>Composition order.</b> SVG applies a transform list as nested coordinate systems, so
/// <c>transform="A B C"</c> yields <c>M = A × B × C</c> and a point maps as <c>A × (B × (C × p))</c>.
/// The accumulator is therefore multiplied on the <b>right</b> as the list is read left to right,
/// and the effect is that the <b>last-listed</b> primitive applies to the geometry <b>first</b>.
/// An inherited transform composes the same way, with the ancestor on the left:
/// <c>M_effective = M_ancestor × M_own</c>.
/// </remarks>
internal static class SvgTransformParser
{
    /// <summary>Parses a transform attribute.</summary>
    /// <param name="value">The raw attribute value.</param>
    /// <returns>The composed transform, or the identity when the value holds no primitives.</returns>
    /// <exception cref="SvgParseException">A function name is unknown, an argument count is wrong, or the syntax is broken.</exception>
    internal static Matrix2D Parse(string value)
    {
        Matrix2D result = Matrix2D.Identity;
        int index = 0;

        while (true)
        {
            SvgLexer.SkipSeparators(value, ref index);
            if (index >= value.Length)
            {
                return result;
            }

            int nameStart = index;
            while (index < value.Length && SvgLexer.IsLetter(value[index]))
            {
                index++;
            }

            if (index == nameStart)
            {
                throw new SvgParseException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Malformed transform attribute: expected a transform function at offset {0} of \"{1}\".",
                        nameStart,
                        value));
            }

            string name = value.Substring(nameStart, index - nameStart);

            SvgLexer.SkipSeparators(value, ref index);
            if (index >= value.Length || value[index] != '(')
            {
                throw new SvgParseException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Malformed transform attribute: transform function '{0}' is missing its argument list.",
                        name));
            }

            index++;

            var arguments = new List<double>();
            while (SvgLexer.TryReadNumber(value, ref index, out double argument))
            {
                arguments.Add(argument);
            }

            SvgLexer.SkipSeparators(value, ref index);
            if (index >= value.Length || value[index] != ')')
            {
                throw new SvgParseException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Malformed transform attribute: transform function '{0}' is missing its closing parenthesis.",
                        name));
            }

            index++;

            result = result.Multiply(Build(name, arguments, value));
        }
    }

    private static Matrix2D Build(string name, List<double> arguments, string attribute)
    {
        switch (name)
        {
            case "translate":
                RequireArgumentCount(name, arguments, attribute, "1 or 2", arguments.Count is 1 or 2);
                return new Matrix2D(1, 0, 0, 1, arguments[0], arguments.Count > 1 ? arguments[1] : 0.0);

            case "scale":
                RequireArgumentCount(name, arguments, attribute, "1 or 2", arguments.Count is 1 or 2);
                // A single argument scales both axes uniformly.
                return new Matrix2D(arguments[0], 0, 0, arguments.Count > 1 ? arguments[1] : arguments[0], 0, 0);

            case "rotate":
            {
                RequireArgumentCount(name, arguments, attribute, "1 or 3", arguments.Count is 1 or 3);
                Matrix2D rotation = Rotation(arguments[0]);
                if (arguments.Count == 1)
                {
                    return rotation;
                }

                double cx = arguments[1];
                double cy = arguments[2];
                return new Matrix2D(1, 0, 0, 1, cx, cy)
                    .Multiply(rotation)
                    .Multiply(new Matrix2D(1, 0, 0, 1, -cx, -cy));
            }

            case "matrix":
                RequireArgumentCount(name, arguments, attribute, "6", arguments.Count == 6);
                return new Matrix2D(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5]);

            case "skewX":
                RequireArgumentCount(name, arguments, attribute, "1", arguments.Count == 1);
                return new Matrix2D(1, 0, Math.Tan(ToRadians(arguments[0])), 1, 0, 0);

            case "skewY":
                RequireArgumentCount(name, arguments, attribute, "1", arguments.Count == 1);
                return new Matrix2D(1, Math.Tan(ToRadians(arguments[0])), 0, 1, 0, 0);

            default:
                throw new SvgParseException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Unsupported transform function '{0}' in transform=\"{1}\".",
                        name,
                        attribute));
        }
    }

    private static Matrix2D Rotation(double degrees)
    {
        double radians = ToRadians(degrees);
        double cos = Math.Cos(radians);
        double sin = Math.Sin(radians);
        return new Matrix2D(cos, sin, -sin, cos, 0, 0);
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

    private static void RequireArgumentCount(string name, List<double> arguments, string attribute, string expected, bool satisfied)
    {
        if (!satisfied)
        {
            throw new SvgParseException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Transform function '{0}' in transform=\"{1}\" takes {2} argument(s) but {3} were given.",
                    name,
                    attribute,
                    expected,
                    arguments.Count));
        }
    }
}
