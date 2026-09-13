using Avalonia.Media;
using Enigma.Icons.AppIconStudio.Design;
using Enigma.Icons.Phosphor;

namespace Enigma.Icons.AppIconStudio;

/// <summary>
/// Every value the studio window opens with, in one place — the state <c>Reset</c> restores.
/// </summary>
/// <remarks>
/// <para>
/// <b>Read twice, written once.</b> <see cref="MainWindowViewModel"/>'s property initializers and its
/// <see cref="MainWindowViewModel.Reset"/> both take their values from here, so the two can never
/// disagree. Re-stating these literals inside a reset is how a reset button quietly stops restoring
/// the actual defaults the first time one of them is tuned.
/// </para>
/// <para>
/// The size check-lists are deliberately absent: which sizes start ticked travels with the
/// <see cref="SizeOption"/> instances themselves, through
/// <see cref="SizeOption.IsSelectedByDefault"/>, so the offered set and its initial ticks stay
/// expressed in exactly one place too.
/// </para>
/// <para>
/// So is the output folder. It starts empty, but a reset keeps whatever the user picked: a folder is
/// a session destination, not a design value, and clearing it would re-disable <i>Generate</i> and
/// send them back through the file dialog — an undo that costs more than it saves.
/// </para>
/// </remarks>
public static class StudioDefaults
{
    /// <summary>
    /// The glyph a new design starts from — the icon of the existing <c>Enigma.MarkdownEditor</c>
    /// artwork, so the reference look is where the studio starts rather than something to rebuild by
    /// hand.
    /// </summary>
    public const PhosphorIcon Icon = PhosphorIcon.MarkdownLogo;

    /// <summary>
    /// The weight that glyph is drawn in. Solid, because that is what still reads at 16 px — the size
    /// an app icon is judged at most often.
    /// </summary>
    public const PhosphorWeight Weight = PhosphorWeight.Fill;

    /// <summary>The plate starts flat: the simplest thing that produces a usable icon.</summary>
    public const PlateFillMode FillMode = PlateFillMode.Solid;

    /// <summary>The gradient direction, forwarded from the fill model so the two cannot drift.</summary>
    public const double GradientAngleDegrees = PlateFill.DefaultAngleDegrees;

    /// <summary>The corner radius, forwarded from the design model so the two cannot drift.</summary>
    public const double CornerRadiusRatio = IconDesign.DefaultCornerRadiusRatio;

    /// <summary>The glyph size, forwarded from the design model so the two cannot drift.</summary>
    public const double GlyphScale = IconDesign.DefaultGlyphScale;

    /// <summary>The file-name stem an export writes under.</summary>
    public const string BaseName = "app";

    /// <summary>
    /// The search box starts empty, listing the whole catalog. Spelled <c>""</c> rather than
    /// <see cref="string.Empty"/> because only the former is a compile-time constant.
    /// </summary>
    public const string SearchText = "";

    /// <summary>The flat plate colour, or gradient stop 0 — the blue of the reference plate.</summary>
    public static Color PrimaryColor { get; } = Color.FromRgb(0x3B, 0x72, 0xF0);

    /// <summary>Gradient stop 1 — the darker blue the reference plate shades towards.</summary>
    public static Color SecondaryColor { get; } = Color.FromRgb(0x1E, 0x3A, 0x8A);

    /// <summary>The colour every glyph layer paints with.</summary>
    public static Color GlyphColor { get; } = Colors.White;

    // Properties rather than consts for the three colours: Color is a struct, and a struct cannot be
    // a C# compile-time constant. Get-only statics are the closest equivalent, and a property
    // initializer can still read them.
}
