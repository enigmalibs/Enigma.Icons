namespace Enigma.Icons.Internal;

/// <summary>
/// SVG's 2×3 affine transform, laid out as <c>[a c e; b d f; 0 0 1]</c>.
/// </summary>
internal readonly struct Matrix2D
{
    private static readonly Matrix2D _identity = new Matrix2D(1, 0, 0, 1, 0, 0);

    internal Matrix2D(double a, double b, double c, double d, double e, double f)
    {
        A = a;
        B = b;
        C = c;
        D = d;
        E = e;
        F = f;
    }

    /// <summary>Row 0, column 0 — the x scale.</summary>
    internal double A { get; }

    /// <summary>Row 1, column 0 — the y shear.</summary>
    internal double B { get; }

    /// <summary>Row 0, column 1 — the x shear.</summary>
    internal double C { get; }

    /// <summary>Row 1, column 1 — the y scale.</summary>
    internal double D { get; }

    /// <summary>Row 0, column 2 — the x translation.</summary>
    internal double E { get; }

    /// <summary>Row 1, column 2 — the y translation.</summary>
    internal double F { get; }

    /// <summary>The identity transform.</summary>
    internal static Matrix2D Identity => _identity;

    /// <summary>
    /// True when this is exactly the identity. Compared exactly on purpose: the identity always
    /// arrives as a literal (an absent <c>transform</c> attribute), never as a rounding artefact,
    /// and the "emit <c>d</c> verbatim" fast path depends on the test being exact.
    /// </summary>
    internal bool IsIdentity => A == 1.0 && B == 0.0 && C == 0.0 && D == 1.0 && E == 0.0 && F == 0.0;

    /// <summary>True when the linear part is the identity, i.e. this is a pure translation.</summary>
    internal bool IsTranslationOnly => A == 1.0 && B == 0.0 && C == 0.0 && D == 1.0;

    /// <summary>The determinant of the linear part. Negative for a transform that mirrors.</summary>
    internal double Determinant => (A * D) - (B * C);

    /// <summary>Returns <c>this × other</c>.</summary>
    /// <param name="other">The right-hand operand — applied to the geometry first.</param>
    /// <returns>The composed transform.</returns>
    internal Matrix2D Multiply(in Matrix2D other)
        => new Matrix2D(
            (A * other.A) + (C * other.B),
            (B * other.A) + (D * other.B),
            (A * other.C) + (C * other.D),
            (B * other.C) + (D * other.D),
            (A * other.E) + (C * other.F) + E,
            (B * other.E) + (D * other.F) + F);

    /// <summary>Maps a point through this transform.</summary>
    /// <param name="x">The source x coordinate.</param>
    /// <param name="y">The source y coordinate.</param>
    /// <param name="resultX">The mapped x coordinate.</param>
    /// <param name="resultY">The mapped y coordinate.</param>
    internal void Transform(double x, double y, out double resultX, out double resultY)
    {
        resultX = (A * x) + (C * y) + E;
        resultY = (B * x) + (D * y) + F;
    }
}
