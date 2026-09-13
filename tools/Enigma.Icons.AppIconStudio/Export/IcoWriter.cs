using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Enigma.Icons.AppIconStudio.Export;

/// <summary>Assembles frames into the bytes of a Windows <c>.ico</c> file.</summary>
/// <remarks>
/// <para>
/// Pure byte assembly: no Avalonia, no file system, no rendering. Hand it frames, get a byte array —
/// which is what makes the container format testable to the offset without a render backend.
/// </para>
/// <para><b>The format, as written here.</b></para>
/// <code>
/// ICONDIR        reserved u16 = 0 · type u16 = 1 · count u16 = N
/// ICONDIRENTRY   bWidth u8 (0 means 256) · bHeight u8 (0 means 256) · bColorCount u8 = 0 ·
///     x N        bReserved u8 = 0 · wPlanes u16 = 1 · wBitCount u16 = 32 ·
///                dwBytesInRes u32 · dwImageOffset u32
/// payloads       in the same order as the entries
/// </code>
/// <para>
/// All multi-byte fields are little-endian. The 256 px special case is not a quirk to work around:
/// the edge really is stored in one byte, which is why <see cref="MaxFrameSizePx"/> is 256 and why
/// 256 is written as 0.
/// </para>
/// <para>
/// <b>Why two encodings in one file (<see cref="PngFrameThreshold"/>).</b> One uncompressed 256 px
/// frame is 256 KB on its own, so an all-BMP icon of the studio's seven sizes runs to about 285 KB;
/// storing just that frame as a PNG brings the same seven to about 105 KB — measured, not estimated.
/// Below 256 the frames stay BMP, because those are the sizes the oldest rendering paths reach for
/// and BMP is what they have always read. That the .NET SDK copies a PNG frame into an executable's
/// icon resource untouched was verified against this toolchain before the format was chosen.
/// </para>
/// <para>
/// <b>The one known cost.</b> GDI+ — <c>System.Drawing.Icon</c>, Windows-only and legacy — cannot
/// decode a PNG frame and falls back to the largest BMP one, so such a caller sees 128 px as the
/// icon's top size. The Windows shell, WIC, Skia (and therefore Avalonia) and the SDK's resource
/// embedder all read the 256 px frame normally, and an app wanting a large bitmap should use the
/// studio's standalone PNGs anyway. Moving <see cref="PngFrameThreshold"/> is the single lever if
/// that trade ever needs to go the other way.
/// </para>
/// </remarks>
public static class IcoWriter
{
    /// <summary>The smallest frame an ICO can hold.</summary>
    public const int MinFrameSizePx = 1;

    /// <summary>The largest frame an ICO can hold — the directory stores the edge in one byte.</summary>
    public const int MaxFrameSizePx = 256;

    /// <summary>At and above this edge a frame is stored as a PNG file rather than a BMP DIB.</summary>
    public const int PngFrameThreshold = 256;

    private const int IconDirBytes = 6;
    private const int IconDirEntryBytes = 16;
    private const int BitmapInfoHeaderBytes = 40;

    /// <summary>Whether a frame of this size should be stored as a PNG rather than a BMP DIB.</summary>
    /// <param name="sizePx">The frame's square edge in pixels.</param>
    /// <returns><see langword="true"/> for 256 px and above.</returns>
    public static bool UsesPngFrame(int sizePx) => sizePx >= PngFrameThreshold;

    /// <summary>Assembles an <c>.ico</c> file from its frames.</summary>
    /// <remarks>
    /// Frames are written in ascending size order regardless of the order given, because that is how
    /// icon editors present them and it makes a hex dump of the result readable. Order carries no
    /// meaning to a reader — every entry names its own offset.
    /// </remarks>
    /// <param name="frames">The frames. At least one, at most 65,535, with no two of the same size.</param>
    /// <returns>The complete bytes of an <c>.ico</c> file.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="frames"/> is null, or contains a null
    /// element.</exception>
    /// <exception cref="ArgumentException"><paramref name="frames"/> is empty, holds more than 65,535
    /// entries, or holds two frames of the same size.</exception>
    public static byte[] Write(IReadOnlyList<IcoFrame> frames)
    {
        if (frames is null)
        {
            throw new ArgumentNullException(nameof(frames));
        }

        if (frames.Count == 0)
        {
            throw new ArgumentException("An icon must hold at least one frame.", nameof(frames));
        }

        if (frames.Count > ushort.MaxValue)
        {
            throw new ArgumentException(
                "An icon directory counts its entries in a 16-bit field, so it cannot hold more than 65,535 frames.",
                nameof(frames));
        }

        // Materialize once: the argument may be a lazy view, and it is read twice below.
        var ordered = new List<IcoFrame>(frames.Count);
        var seen = new HashSet<int>();
        for (int i = 0; i < frames.Count; i++)
        {
            IcoFrame frame = frames[i];
            if (frame is null)
            {
                throw new ArgumentNullException(
                    nameof(frames),
                    string.Format(CultureInfo.InvariantCulture, "The frame at index {0} is null.", i));
            }

            if (!seen.Add(frame.SizePx))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Two frames are {0} pixels square; an icon holds at most one frame per size.",
                        frame.SizePx),
                    nameof(frames));
            }

            ordered.Add(frame);
        }

        ordered.Sort(static (left, right) => left.SizePx.CompareTo(right.SizePx));

        // Encode before writing anything: the directory has to know every payload's length up front.
        var payloads = new byte[ordered.Count][];
        for (int i = 0; i < ordered.Count; i++)
        {
            IcoFrame frame = ordered[i];
            payloads[i] = frame.Format == IcoFrameFormat.Png
                ? frame.Payload
                : BuildDib(frame.SizePx, frame.Payload);
        }

        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            // ICONDIR. BinaryWriter is little-endian, which is what the format wants.
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)ordered.Count);

            int offset = IconDirBytes + (IconDirEntryBytes * ordered.Count);
            for (int i = 0; i < ordered.Count; i++)
            {
                IcoFrame frame = ordered[i];
                byte edge = frame.SizePx == MaxFrameSizePx ? (byte)0 : (byte)frame.SizePx;

                writer.Write(edge);
                writer.Write(edge);
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((ushort)1);
                writer.Write((ushort)32);
                writer.Write((uint)payloads[i].Length);
                writer.Write((uint)offset);

                offset += payloads[i].Length;
            }

            for (int i = 0; i < payloads.Length; i++)
            {
                writer.Write(payloads[i]);
            }
        }

        return stream.ToArray();
    }

    /// <summary>The number of bytes one row of an AND mask occupies, padded to a 4-byte boundary.</summary>
    /// <param name="sizePx">The frame's square edge in pixels.</param>
    /// <returns>The padded row length in bytes.</returns>
    public static int AndMaskStride(int sizePx) => ((sizePx + 31) / 32) * 4;

    /// <summary>
    /// Wraps straight, top-down BGRA pixels in the BMP device-independent bitmap an ICO frame
    /// expects.
    /// </summary>
    /// <remarks>
    /// Three things are easy to get wrong here and are all deliberate. <c>biHeight</c> is
    /// <b>twice</b> the edge, because the DIB nominally holds the colour image and the AND mask
    /// stacked. The colour rows are written <b>bottom-up</b>, which is BMP's native order and the
    /// opposite of the rasterizer's. And the AND mask is written as <b>all zeros</b> — with 32 bits
    /// per pixel the alpha channel is the authority on transparency, and a redundant 1-bit mask that
    /// disagreed with it would show as hard-edged fringing on exactly the rounded corners this tool
    /// exists to draw.
    /// </remarks>
    /// <param name="sizePx">The frame's square edge in pixels.</param>
    /// <param name="bgraTopDown">Straight BGRA, top row first, <c>sizePx * sizePx * 4</c> bytes.</param>
    /// <returns>The DIB payload.</returns>
    private static byte[] BuildDib(int sizePx, byte[] bgraTopDown)
    {
        int colourStride = sizePx * IcoFrame.BytesPerPixel;
        int maskStride = AndMaskStride(sizePx);
        int colourBytes = colourStride * sizePx;
        int maskBytes = maskStride * sizePx;

        var dib = new byte[BitmapInfoHeaderBytes + colourBytes + maskBytes];

        using (var stream = new MemoryStream(dib))
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            // BITMAPINFOHEADER
            writer.Write((uint)BitmapInfoHeaderBytes);
            writer.Write(sizePx);
            writer.Write(sizePx * 2);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write((uint)0);
            writer.Write((uint)(colourBytes + maskBytes));
            writer.Write(0);
            writer.Write(0);
            writer.Write((uint)0);
            writer.Write((uint)0);

            for (int y = sizePx - 1; y >= 0; y--)
            {
                writer.Write(bgraTopDown, y * colourStride, colourStride);
            }

            // The AND mask stays zero-filled: `dib` is already all zeros here, so there is nothing to
            // write — the array's own length reserves the space.
        }

        return dib;
    }
}
