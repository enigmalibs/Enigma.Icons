using System;
using System.Globalization;

namespace Enigma.Icons;

/// <summary>
/// The coordinate rectangle a glyph's path data is expressed in. Renderers use it to scale a glyph
/// into the target bounds.
/// </summary>
public readonly struct IconViewBox : IEquatable<IconViewBox>
{
    private static readonly IconViewBox _default = new IconViewBox(0, 0, 256, 256);

    /// <summary>Initializes a new view box.</summary>
    /// <param name="x">Left edge of the rectangle.</param>
    /// <param name="y">Top edge of the rectangle.</param>
    /// <param name="width">Width of the rectangle. Must be greater than zero.</param>
    /// <param name="height">Height of the rectangle. Must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="width"/> or <paramref name="height"/> is not greater than zero, or is
    /// <see cref="double.NaN"/>.
    /// </exception>
    public IconViewBox(double x, double y, double width, double height)
    {
        // Phrased as a negated "> 0" rather than "<= 0" on purpose: it also rejects NaN, which a
        // "<= 0" test would silently admit.
        if (!(width > 0))
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be greater than zero.");
        }

        if (!(height > 0))
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be greater than zero.");
        }

        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>Left edge of the rectangle.</summary>
    public double X { get; }

    /// <summary>Top edge of the rectangle.</summary>
    public double Y { get; }

    /// <summary>Width of the rectangle. Always greater than zero.</summary>
    public double Width { get; }

    /// <summary>Height of the rectangle. Always greater than zero.</summary>
    public double Height { get; }

    /// <summary>The 0 0 256 256 view box used by every Phosphor icon.</summary>
    public static IconViewBox Default => _default;

    /// <summary>Determines whether this view box equals <paramref name="other"/>.</summary>
    /// <param name="other">The view box to compare with.</param>
    /// <returns><see langword="true"/> when all four components are equal.</returns>
    public bool Equals(IconViewBox other)
        => X.Equals(other.X) && Y.Equals(other.Y) && Width.Equals(other.Width) && Height.Equals(other.Height);

    /// <summary>Determines whether this view box equals <paramref name="obj"/>.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns><see langword="true"/> when <paramref name="obj"/> is an equal view box.</returns>
    public override bool Equals(object? obj) => obj is IconViewBox other && Equals(other);

    /// <summary>Returns a hash code for this view box.</summary>
    /// <returns>A hash code derived from all four components.</returns>
    public override int GetHashCode()
    {
        // Hand-rolled: System.HashCode is .NET Standard 2.1+, and netstandard2.0 is in the TFM set.
        // One implementation for every TFM — no #if.
        unchecked
        {
            int hash = 17;
            hash = (hash * 31) + X.GetHashCode();
            hash = (hash * 31) + Y.GetHashCode();
            hash = (hash * 31) + Width.GetHashCode();
            hash = (hash * 31) + Height.GetHashCode();
            return hash;
        }
    }

    /// <summary>Returns the view box as <c>"X Y Width Height"</c> in the invariant culture.</summary>
    /// <returns>The four components separated by single spaces.</returns>
    public override string ToString()
        => string.Format(
            CultureInfo.InvariantCulture,
            "{0} {1} {2} {3}",
            X,
            Y,
            Width,
            Height);

    /// <summary>Determines whether two view boxes are equal.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> when the two view boxes are equal.</returns>
    public static bool operator ==(IconViewBox left, IconViewBox right) => left.Equals(right);

    /// <summary>Determines whether two view boxes differ.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> when the two view boxes are not equal.</returns>
    public static bool operator !=(IconViewBox left, IconViewBox right) => !left.Equals(right);
}
