using System;
using System.Globalization;

namespace Enigma.Icons.AppIconStudio.Export;

/// <summary>One square image destined for an <c>.ico</c> container, in the encoding it will be
/// stored in.</summary>
/// <remarks>
/// The payload is held in its <i>input</i> form, not its final one:
/// <see cref="IcoFrameFormat.Dib"/> carries straight BGRA in <b>top-down</b> row order — what
/// <see cref="Rendering.IIconRasterizer.RenderBgra"/> hands back — and
/// <see cref="IcoWriter"/> flips it and builds the BMP header. That keeps the rasterizer unaware of
/// the container format and the container writer unaware of Avalonia.
/// </remarks>
public sealed class IcoFrame
{
    /// <summary>Bytes per pixel in a <see cref="IcoFrameFormat.Dib"/> payload.</summary>
    public const int BytesPerPixel = 4;

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>Initializes a new frame.</summary>
    /// <param name="sizePx">The square edge in pixels, 1–256.</param>
    /// <param name="format">How <paramref name="payload"/> is encoded.</param>
    /// <param name="payload">
    /// Straight BGRA, top-down, exactly <c>sizePx * sizePx * 4</c> bytes for
    /// <see cref="IcoFrameFormat.Dib"/>; a complete PNG file for <see cref="IcoFrameFormat.Png"/>.
    /// Stored by reference — the caller must not mutate it afterwards.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="payload"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="sizePx"/> is outside 1–256, or
    /// <paramref name="format"/> is not a declared member.</exception>
    /// <exception cref="ArgumentException">The payload does not match the format — a DIB payload of
    /// the wrong length, or a PNG payload that does not start with the PNG signature.</exception>
    public IcoFrame(int sizePx, IcoFrameFormat format, byte[] payload)
    {
        if (payload is null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        if (sizePx < IcoWriter.MinFrameSizePx || sizePx > IcoWriter.MaxFrameSizePx)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sizePx),
                sizePx,
                "An ICO frame must be between 1 and 256 pixels square — the format stores the edge in a single byte.");
        }

        switch (format)
        {
            case IcoFrameFormat.Dib:
                int expected = sizePx * sizePx * BytesPerPixel;
                if (payload.Length != expected)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "A {0}x{0} BGRA payload must be exactly {1} bytes, but {2} were given.",
                            sizePx,
                            expected,
                            payload.Length),
                        nameof(payload));
                }

                break;

            case IcoFrameFormat.Png:
                if (!StartsWithPngSignature(payload))
                {
                    throw new ArgumentException(
                        "A PNG frame payload must be a complete PNG file, starting with the PNG signature.",
                        nameof(payload));
                }

                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown ICO frame format.");
        }

        SizePx = sizePx;
        Format = format;
        Payload = payload;
    }

    /// <summary>The square edge in pixels, 1–256.</summary>
    public int SizePx { get; }

    /// <summary>How <see cref="Payload"/> is encoded.</summary>
    public IcoFrameFormat Format { get; }

    /// <summary>The frame's bytes, in input form. Never null, never empty.</summary>
    public byte[] Payload { get; }

    /// <summary>A frame from raw pixels, to be stored as a BMP DIB.</summary>
    /// <param name="sizePx">The square edge in pixels, 1–256.</param>
    /// <param name="bgraTopDown">Straight BGRA, top row first, <c>sizePx * sizePx * 4</c> bytes.</param>
    /// <returns>The frame.</returns>
    public static IcoFrame FromPixels(int sizePx, byte[] bgraTopDown)
        => new IcoFrame(sizePx, IcoFrameFormat.Dib, bgraTopDown);

    /// <summary>A frame from an encoded PNG file, to be stored verbatim.</summary>
    /// <param name="sizePx">The square edge in pixels, 1–256. Must match the PNG's own dimensions.</param>
    /// <param name="png">The complete bytes of a PNG file.</param>
    /// <returns>The frame.</returns>
    public static IcoFrame FromPng(int sizePx, byte[] png)
        => new IcoFrame(sizePx, IcoFrameFormat.Png, png);

    private static bool StartsWithPngSignature(byte[] payload)
    {
        if (payload.Length < PngSignature.Length)
        {
            return false;
        }

        for (int i = 0; i < PngSignature.Length; i++)
        {
            if (payload[i] != PngSignature[i])
            {
                return false;
            }
        }

        return true;
    }
}
