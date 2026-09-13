namespace Enigma.Icons.AppIconStudio.Export;

/// <summary>How one image inside an <c>.ico</c> container is encoded.</summary>
/// <remarks>
/// An ICO's directory is format-agnostic: each entry just names a byte range, and the reader decides
/// what it is by looking at the first bytes. Both encodings below are legal in the same file, which
/// is exactly what <see cref="IcoWriter.PngFrameThreshold"/> exploits.
/// </remarks>
public enum IcoFrameFormat
{
    /// <summary>
    /// A 32-bpp BMP device-independent bitmap: a <c>BITMAPINFOHEADER</c>, bottom-up BGRA rows, and a
    /// padded AND mask. Understood by every Windows version ever shipped.
    /// </summary>
    Dib,

    /// <summary>
    /// A complete PNG file, stored verbatim. Read by Windows Vista and later, and by the .NET SDK's
    /// <c>ApplicationIcon</c> path, which copies the payload into the executable's icon resource
    /// untouched.
    /// </summary>
    Png,
}
