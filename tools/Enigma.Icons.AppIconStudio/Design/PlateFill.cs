using System;
using System.Globalization;
using Avalonia.Media;

namespace Enigma.Icons.AppIconStudio.Design;

/// <summary>
/// How the rounded plate is painted: one flat colour, or a two-stop linear gradient at an angle.
/// Immutable, and safe to share between the preview and an export.
/// </summary>
/// <remarks>
/// <para>
/// Two stops, not an arbitrary stop list. Every plate look the studio is for — flat, or a shaded
/// square reading darker towards one corner — is expressible with two, and a stop editor would be
/// UI surface for a case that has not come up.
/// </para>
/// <para>
/// <see cref="Avalonia.Media.Color"/> rather than a colour type of this project's own: it is a plain
/// struct in <c>Avalonia.Base</c> that needs no platform to construct, it is what
/// <c>ColorPicker.Color</c> binds to, and this project already depends on Avalonia.
/// </para>
/// </remarks>
public sealed class PlateFill
{
    /// <summary>The angle a new gradient starts at — top-left to bottom-right.</summary>
    public const double DefaultAngleDegrees = 45.0;

    /// <summary>Initializes a new plate fill.</summary>
    /// <param name="mode">Flat colour or two-stop gradient.</param>
    /// <param name="primaryColor">The flat colour, or gradient stop 0.</param>
    /// <param name="secondaryColor">Gradient stop 1. Kept, but unused, when <paramref name="mode"/>
    /// is <see cref="PlateFillMode.Solid"/> — so toggling the mode back and forth in the UI does not
    /// lose the second colour.</param>
    /// <param name="angleDegrees">
    /// The gradient direction in degrees, measured clockwise from left-to-right: 0 is left to right,
    /// 90 is top to bottom. Any finite value is accepted and wrapped into the range 0 (inclusive) to
    /// 360 (exclusive), because an angle slider that stops dead at its ends is worse than one that
    /// wraps.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> is not a declared member,
    /// or <paramref name="angleDegrees"/> is <see cref="double.NaN"/> or infinite.</exception>
    public PlateFill(PlateFillMode mode, Color primaryColor, Color secondaryColor, double angleDegrees)
    {
        // An explicit pair test, not Enum.IsDefined: the enum has two members and this runs on the
        // preview path (SPEC §2.11).
        if (mode != PlateFillMode.Solid && mode != PlateFillMode.LinearGradient)
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown plate fill mode.");
        }

        // Phrased as a negated "is finite" so NaN is rejected rather than silently admitted, the same
        // idiom Enigma.Icons uses in IconViewBox.
        if (double.IsNaN(angleDegrees) || double.IsInfinity(angleDegrees))
        {
            throw new ArgumentOutOfRangeException(
                nameof(angleDegrees),
                angleDegrees,
                "The gradient angle must be a finite number of degrees.");
        }

        Mode = mode;
        PrimaryColor = primaryColor;
        SecondaryColor = secondaryColor;
        AngleDegrees = Wrap(angleDegrees);
    }

    /// <summary>Flat colour or two-stop gradient.</summary>
    public PlateFillMode Mode { get; }

    /// <summary>The flat colour, or gradient stop 0.</summary>
    public Color PrimaryColor { get; }

    /// <summary>Gradient stop 1. Unused when <see cref="Mode"/> is <see cref="PlateFillMode.Solid"/>.</summary>
    public Color SecondaryColor { get; }

    /// <summary>The gradient direction in degrees, always in the range 0 (inclusive) to 360 (exclusive).</summary>
    public double AngleDegrees { get; }

    /// <summary>A flat plate.</summary>
    /// <param name="color">The colour of the whole plate.</param>
    /// <returns>A solid fill; the second colour is seeded to the same value.</returns>
    public static PlateFill Solid(Color color)
        => new PlateFill(PlateFillMode.Solid, color, color, DefaultAngleDegrees);

    /// <summary>A two-stop gradient plate.</summary>
    /// <param name="from">Gradient stop 0.</param>
    /// <param name="to">Gradient stop 1.</param>
    /// <param name="angleDegrees">The direction, clockwise from left-to-right; wrapped into 0–360.</param>
    /// <returns>A gradient fill.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="angleDegrees"/> is
    /// <see cref="double.NaN"/> or infinite.</exception>
    public static PlateFill LinearGradient(Color from, Color to, double angleDegrees = DefaultAngleDegrees)
        => new PlateFill(PlateFillMode.LinearGradient, from, to, angleDegrees);

    /// <summary>Returns a short, culture-invariant description, used in status text and test failures.</summary>
    /// <returns>The mode and the colours involved.</returns>
    public override string ToString()
        => Mode == PlateFillMode.Solid
            ? string.Format(CultureInfo.InvariantCulture, "Solid {0}", PrimaryColor)
            : string.Format(
                CultureInfo.InvariantCulture,
                "Gradient {0} -> {1} at {2}°",
                PrimaryColor,
                SecondaryColor,
                AngleDegrees);

    private static double Wrap(double angleDegrees)
    {
        double wrapped = angleDegrees % 360.0;

        // The % operator keeps the sign of the dividend, so -90 comes back as -90, not 270.
        return wrapped < 0 ? wrapped + 360.0 : wrapped;
    }
}
