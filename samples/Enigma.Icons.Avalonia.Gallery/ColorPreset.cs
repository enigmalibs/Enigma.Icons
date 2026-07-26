using Avalonia.Media;

namespace Enigma.Icons.Avalonia.Gallery;

/// <summary>A named brush offered by the gallery's colour selector.</summary>
/// <param name="Name">The label shown in the combo box.</param>
/// <param name="Brush">The brush applied to every icon in the grid. Never null.</param>
/// <remarks>
/// <see cref="Brush"/> is deliberately non-null: SPEC §10.2 makes an <c>Icon</c> whose
/// <c>Foreground</c> resolves to null paint nothing, so a null preset would read as a rendering bug
/// rather than as a choice. Foreground <i>inheritance</i> is demonstrated separately, by the two
/// icons in the header that set no <c>Foreground</c> at all.
/// </remarks>
public sealed record ColorPreset(string Name, IBrush Brush);
