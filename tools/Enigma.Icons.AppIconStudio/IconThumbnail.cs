using Avalonia.Media.Imaging;

namespace Enigma.Icons.AppIconStudio;

/// <summary>One entry of the small-size preview strip beside the main preview.</summary>
/// <param name="SizePx">The edge this thumbnail was rendered at, in pixels.</param>
/// <param name="Image">The decoded render, shown at exactly <paramref name="SizePx"/> on screen.</param>
/// <remarks>
/// The strip exists because a glyph scale that looks generous at 256 px can be an unreadable smudge
/// at 16, and finding that out after shipping the icon is the expensive way. Every thumbnail comes
/// from the same rasterizer as the export, at the size it will actually be written, so what the strip
/// shows is what the <c>.ico</c> will contain.
/// </remarks>
public sealed record IconThumbnail(int SizePx, Bitmap Image);
