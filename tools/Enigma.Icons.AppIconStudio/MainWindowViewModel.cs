using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Enigma.Icons.AppIconStudio.Design;
using Enigma.Icons.Phosphor;

namespace Enigma.Icons.AppIconStudio;

/// <summary>Drives the studio window.</summary>
/// <remarks>
/// Explicit CommunityToolkit.Mvvm style — <c>field</c>-backed properties with <c>SetProperty</c> and
/// get-only command properties initialized in the constructor. No <c>[ObservableProperty]</c> /
/// <c>[RelayCommand]</c> source generators, so the class does not need to be <c>partial</c>. This is
/// the house convention the gallery already follows; see <c>docs/done/FEATURE-469B.md</c>.
/// </remarks>
public sealed class MainWindowViewModel : ObservableObject
{
    /// <summary>
    /// The design a freshly opened studio starts from: the blue rounded plate and white glyph of the
    /// existing <c>Enigma.MarkdownEditor</c> icon, so the reference look is one click away.
    /// </summary>
    public static IconDesign DefaultDesign { get; } = new IconDesign(
        PhosphorIcon.PencilSimple,
        PhosphorWeight.Fill,
        Colors.White,
        PlateFill.Solid(Color.FromRgb(0x3B, 0x72, 0xF0)));

    /// <summary>The status line at the bottom of the window.</summary>
    public string StatusText { get; } =
        "App icon studio — composition and export arrive in the following phases.";
}
