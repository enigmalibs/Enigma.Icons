using System;
using Enigma.Icons.AppIconStudio.Design;

namespace Enigma.Icons.AppIconStudio.Rendering;

/// <summary>Turns an <see cref="IconDesign"/> into pixels at a requested size.</summary>
/// <remarks>
/// <para>
/// Two outputs, because the two consumers want different things: a PNG file is what lands on disk
/// and what the preview decodes, while raw BGRA is what a BMP frame inside an <c>.ico</c> is built
/// from. Each call renders once — a frame needs one form or the other, never both.
/// </para>
/// <para>
/// <b>Implementations run on the UI thread.</b> The Avalonia implementation draws through a
/// <c>RenderTargetBitmap</c> and parses path data with <c>Geometry.Parse</c>; neither is documented
/// as thread-safe, and an export is a handful of frames, so nothing here is worth moving off the
/// dispatcher. Only the file writes that follow are asynchronous.
/// </para>
/// </remarks>
public interface IIconRasterizer
{
    /// <summary>Renders the design and encodes it as a PNG file.</summary>
    /// <param name="design">The design to compose.</param>
    /// <param name="sizePx">The square canvas edge in pixels.</param>
    /// <returns>The complete bytes of a PNG file, starting with the PNG signature.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="design"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="sizePx"/> is outside
    /// <see cref="IconLayout.MinSizePx"/>–<see cref="IconLayout.MaxSizePx"/>.</exception>
    byte[] RenderPng(IconDesign design, int sizePx);

    /// <summary>Renders the design and returns its raw pixels.</summary>
    /// <param name="design">The design to compose.</param>
    /// <param name="sizePx">The square canvas edge in pixels.</param>
    /// <returns>
    /// <c>sizePx * sizePx * 4</c> bytes of BGRA, top row first, with <b>straight</b> (not
    /// premultiplied) alpha and a stride of exactly <c>sizePx * 4</c> — the layout an ICO BMP frame
    /// needs, modulo its bottom-up row order.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="design"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="sizePx"/> is outside
    /// <see cref="IconLayout.MinSizePx"/>–<see cref="IconLayout.MaxSizePx"/>.</exception>
    byte[] RenderBgra(IconDesign design, int sizePx);
}
