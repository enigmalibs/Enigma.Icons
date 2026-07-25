using System;
using System.Globalization;
using System.Text;

namespace Enigma.Icons.Internal;

/// <summary>
/// Bakes a <see cref="Matrix2D"/> into SVG path data, so an <see cref="IconLayer"/> never has to
/// carry a transform.
/// </summary>
internal static class SvgPathTransformer
{
    /// <summary>Applies <paramref name="matrix"/> to every coordinate in <paramref name="pathData"/>.</summary>
    /// <param name="pathData">The source path data.</param>
    /// <param name="matrix">The transform to bake in.</param>
    /// <param name="origin">A short description of where the path data came from, used in error messages.</param>
    /// <returns>
    /// The transformed path data — or <paramref name="pathData"/> itself, byte for byte, when
    /// <paramref name="matrix"/> is the identity.
    /// </returns>
    /// <exception cref="SvgParseException">The path data is malformed and a transform had to be applied.</exception>
    internal static string Bake(string pathData, in Matrix2D matrix, string origin)
    {
        // The overwhelmingly common case. No tokenization and no validation: this is what keeps
        // <path d> byte-identical to the source, exactly as SPEC §5.1 requires — and it means
        // malformed d with no transform passes straight through to the renderer.
        if (matrix.IsIdentity)
        {
            return pathData;
        }

        var writer = new PathWriter();
        var reader = new PathReader(pathData, origin);

        double currentX = 0.0;
        double currentY = 0.0;
        double startX = 0.0;
        double startY = 0.0;
        char command = '\0';
        bool anyCommand = false;

        while (true)
        {
            char? next = reader.TryReadCommand();
            if (next is null)
            {
                if (!reader.AtEnd)
                {
                    // Not a command letter and not the end: an implicit repetition of the previous
                    // command, which is only legal once a command has been seen.
                    if (!anyCommand)
                    {
                        throw reader.Malformed("path data must begin with a command letter");
                    }

                    // M/m repeats as L/l (SVG's implicit-lineto rule).
                    command = command switch
                    {
                        'M' => 'L',
                        'm' => 'l',
                        _ => command,
                    };

                    // Z takes no operands, so it can never repeat implicitly — treating it as a
                    // repetition would spin forever on trailing junk.
                    if (command is 'Z' or 'z')
                    {
                        throw reader.Malformed("a closepath command cannot take operands");
                    }
                }
                else
                {
                    break;
                }
            }
            else
            {
                command = next.Value;
                anyCommand = true;
            }

            bool relative = char.IsLower(command);
            switch (char.ToUpperInvariant(command))
            {
                case 'M':
                {
                    double x = reader.ReadNumber(command);
                    double y = reader.ReadNumber(command);
                    Resolve(relative, currentX, currentY, ref x, ref y);
                    currentX = x;
                    currentY = y;
                    startX = x;
                    startY = y;
                    writer.Command('M');
                    writer.Point(matrix, x, y);
                    break;
                }

                case 'L':
                {
                    double x = reader.ReadNumber(command);
                    double y = reader.ReadNumber(command);
                    Resolve(relative, currentX, currentY, ref x, ref y);
                    currentX = x;
                    currentY = y;
                    writer.Command('L');
                    writer.Point(matrix, x, y);
                    break;
                }

                case 'H':
                {
                    // Expanded into L: a horizontal segment is no longer horizontal after a
                    // rotation or a skew.
                    double x = reader.ReadNumber(command);
                    if (relative)
                    {
                        x += currentX;
                    }

                    currentX = x;
                    writer.Command('L');
                    writer.Point(matrix, currentX, currentY);
                    break;
                }

                case 'V':
                {
                    double y = reader.ReadNumber(command);
                    if (relative)
                    {
                        y += currentY;
                    }

                    currentY = y;
                    writer.Command('L');
                    writer.Point(matrix, currentX, currentY);
                    break;
                }

                case 'C':
                {
                    double x1 = reader.ReadNumber(command);
                    double y1 = reader.ReadNumber(command);
                    double x2 = reader.ReadNumber(command);
                    double y2 = reader.ReadNumber(command);
                    double x = reader.ReadNumber(command);
                    double y = reader.ReadNumber(command);
                    Resolve(relative, currentX, currentY, ref x1, ref y1);
                    Resolve(relative, currentX, currentY, ref x2, ref y2);
                    Resolve(relative, currentX, currentY, ref x, ref y);
                    writer.Command('C');
                    writer.Point(matrix, x1, y1);
                    writer.Separator();
                    writer.Point(matrix, x2, y2);
                    writer.Separator();
                    writer.Point(matrix, x, y);
                    currentX = x;
                    currentY = y;
                    break;
                }

                case 'S':
                {
                    // S and Q/T need no special handling beyond transforming their explicit points:
                    // the implied control point is the reflection 2·P_current − P_prevControl, and
                    // an affine map preserves that relation exactly —
                    // M(2P − Q) + t = 2(MP + t) − (MQ + t).
                    double x2 = reader.ReadNumber(command);
                    double y2 = reader.ReadNumber(command);
                    double x = reader.ReadNumber(command);
                    double y = reader.ReadNumber(command);
                    Resolve(relative, currentX, currentY, ref x2, ref y2);
                    Resolve(relative, currentX, currentY, ref x, ref y);
                    writer.Command('S');
                    writer.Point(matrix, x2, y2);
                    writer.Separator();
                    writer.Point(matrix, x, y);
                    currentX = x;
                    currentY = y;
                    break;
                }

                case 'Q':
                {
                    double x1 = reader.ReadNumber(command);
                    double y1 = reader.ReadNumber(command);
                    double x = reader.ReadNumber(command);
                    double y = reader.ReadNumber(command);
                    Resolve(relative, currentX, currentY, ref x1, ref y1);
                    Resolve(relative, currentX, currentY, ref x, ref y);
                    writer.Command('Q');
                    writer.Point(matrix, x1, y1);
                    writer.Separator();
                    writer.Point(matrix, x, y);
                    currentX = x;
                    currentY = y;
                    break;
                }

                case 'T':
                {
                    double x = reader.ReadNumber(command);
                    double y = reader.ReadNumber(command);
                    Resolve(relative, currentX, currentY, ref x, ref y);
                    writer.Command('T');
                    writer.Point(matrix, x, y);
                    currentX = x;
                    currentY = y;
                    break;
                }

                case 'A':
                {
                    double rx = reader.ReadNumber(command);
                    double ry = reader.ReadNumber(command);
                    double rotation = reader.ReadNumber(command);
                    bool largeArc = reader.ReadFlag(command);
                    bool sweep = reader.ReadFlag(command);
                    double x = reader.ReadNumber(command);
                    double y = reader.ReadNumber(command);
                    Resolve(relative, currentX, currentY, ref x, ref y);
                    WriteArc(writer, matrix, rx, ry, rotation, largeArc, sweep, x, y);
                    currentX = x;
                    currentY = y;
                    break;
                }

                case 'Z':
                {
                    writer.Command('Z');
                    currentX = startX;
                    currentY = startY;
                    break;
                }

                default:
                    throw reader.Malformed(
                        string.Format(CultureInfo.InvariantCulture, "unknown command '{0}'", command));
            }
        }

        return writer.ToString();
    }

    private static void Resolve(bool relative, double currentX, double currentY, ref double x, ref double y)
    {
        if (relative)
        {
            x += currentX;
            y += currentY;
        }
    }

    /// <summary>
    /// Writes an elliptical arc through the transform. The endpoint maps normally; the ellipse
    /// itself has to be recomputed.
    /// </summary>
    private static void WriteArc(
        PathWriter writer,
        in Matrix2D matrix,
        double rx,
        double ry,
        double rotationDegrees,
        bool largeArc,
        bool sweep,
        double endX,
        double endY)
    {
        rx = Math.Abs(rx);
        ry = Math.Abs(ry);

        // Per the SVG specification a zero radius degenerates the arc into a straight line.
        if (rx == 0.0 || ry == 0.0)
        {
            writer.Command('L');
            writer.Point(matrix, endX, endY);
            return;
        }

        double newRx;
        double newRy;
        double newRotation;

        if (matrix.IsTranslationOnly)
        {
            // A pure translation leaves the ellipse untouched — keep the original radii and
            // rotation rather than re-deriving an equivalent-but-different representation.
            newRx = rx;
            newRy = ry;
            newRotation = rotationDegrees;
        }
        else
        {
            // Represent the arc's ellipse by its shape matrix E = R(phi) · diag(rx, ry). Under the
            // linear part L of the transform it becomes E' = L · E; recovering the transformed
            // radii and rotation from E' is a closed-form eigen-decomposition of S = E' · E'ᵀ,
            // whose eigenvalues are rx'² and ry'² and whose eigenvectors give the new rotation.
            double phi = rotationDegrees * Math.PI / 180.0;
            double cos = Math.Cos(phi);
            double sin = Math.Sin(phi);

            double e11 = cos * rx;
            double e21 = sin * rx;
            double e12 = -sin * ry;
            double e22 = cos * ry;

            double t11 = (matrix.A * e11) + (matrix.C * e21);
            double t21 = (matrix.B * e11) + (matrix.D * e21);
            double t12 = (matrix.A * e12) + (matrix.C * e22);
            double t22 = (matrix.B * e12) + (matrix.D * e22);

            double s11 = (t11 * t11) + (t12 * t12);
            double s12 = (t11 * t21) + (t12 * t22);
            double s22 = (t21 * t21) + (t22 * t22);

            double mean = (s11 + s22) / 2.0;
            double half = (s11 - s22) / 2.0;
            double spread = Math.Sqrt((half * half) + (s12 * s12));

            double major = mean + spread;
            double minor = mean - spread;
            newRx = Math.Sqrt(major > 0.0 ? major : 0.0);
            newRy = Math.Sqrt(minor > 0.0 ? minor : 0.0);

            // The principal-axis angle of a symmetric 2x2 matrix; this branch of atan2 picks the
            // axis belonging to the larger eigenvalue, which is the one newRx was assigned.
            double angle = 0.5 * Math.Atan2(2.0 * s12, s11 - s22);
            newRotation = angle * 180.0 / Math.PI;

            if (newRy == 0.0)
            {
                // The transform collapsed the ellipse onto a line.
                writer.Command('L');
                writer.Point(matrix, endX, endY);
                return;
            }
        }

        // large-arc-flag is unchanged; sweep-flag flips exactly when the transform mirrors,
        // because a reflection reverses the angular direction.
        bool newSweep = matrix.Determinant < 0 ? !sweep : sweep;

        writer.Command('A');
        writer.RawPoint(newRx, newRy);
        writer.Separator();
        writer.Raw(SvgNumber.Format(newRotation));
        writer.Separator();
        writer.Raw(largeArc ? "1" : "0");
        writer.Separator();
        writer.Raw(newSweep ? "1" : "0");
        writer.Separator();
        writer.Point(matrix, endX, endY);
    }

    /// <summary>Emits normalized, absolute path data with a single deterministic layout.</summary>
    private sealed class PathWriter
    {
        private readonly StringBuilder _builder = new StringBuilder();

        internal void Command(char command)
        {
            if (_builder.Length > 0)
            {
                _builder.Append(' ');
            }

            _builder.Append(command);
            if (command != 'Z')
            {
                _builder.Append(' ');
            }
        }

        internal void Separator() => _builder.Append(' ');

        internal void Raw(string text) => _builder.Append(text);

        internal void RawPoint(double x, double y)
        {
            _builder.Append(SvgNumber.Format(x));
            _builder.Append(',');
            _builder.Append(SvgNumber.Format(y));
        }

        internal void Point(in Matrix2D matrix, double x, double y)
        {
            matrix.Transform(x, y, out double tx, out double ty);
            RawPoint(tx, ty);
        }

        public override string ToString() => _builder.ToString();
    }

    /// <summary>Tokenizes the SVG path mini-language.</summary>
    private sealed class PathReader
    {
        private readonly string _text;
        private readonly string _origin;
        private int _index;

        internal PathReader(string text, string origin)
        {
            _text = text;
            _origin = origin;
        }

        internal bool AtEnd
        {
            get
            {
                SvgLexer.SkipSeparators(_text, ref _index);
                return _index >= _text.Length;
            }
        }

        internal char? TryReadCommand()
        {
            SvgLexer.SkipSeparators(_text, ref _index);
            if (_index >= _text.Length || !SvgLexer.IsLetter(_text[_index]))
            {
                return null;
            }

            return _text[_index++];
        }

        internal double ReadNumber(char command)
        {
            if (!SvgLexer.TryReadNumber(_text, ref _index, out double value))
            {
                throw Malformed(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "command '{0}' is missing an operand at offset {1}",
                        command,
                        _index));
            }

            return value;
        }

        internal bool ReadFlag(char command)
        {
            if (!SvgLexer.TryReadFlag(_text, ref _index, out bool flag))
            {
                throw Malformed(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "command '{0}' expects a 0 or 1 arc flag at offset {1}",
                        command,
                        _index));
            }

            return flag;
        }

        internal SvgParseException Malformed(string detail)
            => new SvgParseException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Malformed path data in {0}: {1}. Path data: \"{2}\".",
                    _origin,
                    detail,
                    _text));
    }
}
