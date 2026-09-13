using System;
using System.Collections.Generic;
using Enigma.Icons.AppIconStudio.Design;
using Enigma.Icons.AppIconStudio.Rendering;

namespace Enigma.Icons.AppIconStudio.UnitTests.TestSupport;

/// <summary>
/// A rasterizer that returns plausible bytes without touching a render backend, and records what it
/// was asked for.
/// </summary>
/// <remarks>
/// The exporter's job is orchestration — which sizes, which encodings, which file names, in which
/// order — and none of that needs real pixels. Using this instead of the Avalonia rasterizer keeps
/// those tests platform-free and makes "was 256 rendered as a PNG rather than as pixels?" directly
/// observable. One separate end-to-end test does use the real rasterizer.
/// </remarks>
public sealed class FakeRasterizer : IIconRasterizer
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>The sizes <see cref="RenderPng"/> was called with, in call order.</summary>
    public List<int> PngCalls { get; } = [];

    /// <summary>The sizes <see cref="RenderBgra"/> was called with, in call order.</summary>
    public List<int> BgraCalls { get; } = [];

    /// <inheritdoc />
    public byte[] RenderPng(IconDesign design, int sizePx)
    {
        ArgumentNullException.ThrowIfNull(design);
        PngCalls.Add(sizePx);

        // Signature plus the size, so a test can tell one frame's payload from another's.
        var png = new byte[PngSignature.Length + 4];
        PngSignature.CopyTo(png, 0);
        BitConverter.GetBytes(sizePx).CopyTo(png, PngSignature.Length);

        return png;
    }

    /// <inheritdoc />
    public byte[] RenderBgra(IconDesign design, int sizePx)
    {
        ArgumentNullException.ThrowIfNull(design);
        BgraCalls.Add(sizePx);

        return new byte[sizePx * sizePx * 4];
    }
}
