using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Enigma.Icons.AppIconStudio.Design;
using Enigma.Icons.Avalonia;
using Enigma.Icons.Phosphor;

// IconGlyph lives in the enclosing Enigma.Icons namespace and needs no using directive here.
namespace Enigma.Icons.AppIconStudio.Rendering;

/// <summary>
/// Composes an icon with Avalonia's own renderer: a rounded plate, then the Phosphor glyph on top,
/// drawn into an off-screen <see cref="RenderTargetBitmap"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why Avalonia and not an imaging library.</b> Avalonia is already a dependency, its Skia
/// backend anti-aliases and handles gradients and rounded rectangles, and — the deciding reason —
/// the glyph reaches the bitmap through <see cref="IconGlyphExtensions.ToDrawing"/>, the *same*
/// conversion the shipped <c>Icon</c> control paints with. Per-layer opacity, duotone's tinted
/// backing shape, stroked layers and <c>fill="none"</c> all behave here exactly as they do on screen,
/// with no second implementation to keep in step. Adding SkiaSharp or ImageSharp would buy a
/// third-party dependency and a rendering path that could disagree with the library's.
/// </para>
/// <para>
/// <b>Each size is rendered natively</b>, not downsampled from a master. A 16 px frame is drawn at
/// 16 px, so its corner radius and stroke weights are resolved by the rasterizer rather than blurred
/// by a resample.
/// </para>
/// <para>
/// The render target is created at the default 96 DPI, so one drawing unit is exactly one output
/// pixel and <see cref="IconLayout"/>'s arithmetic needs no scale factor.
/// </para>
/// </remarks>
public sealed class AvaloniaIconRasterizer : IIconRasterizer
{
    /// <summary>Bytes per pixel in the BGRA buffers this rasterizer produces.</summary>
    public const int BytesPerPixel = 4;

    /// <inheritdoc />
    public byte[] RenderPng(IconDesign design, int sizePx)
    {
        using RenderTargetBitmap bitmap = Render(design, sizePx);
        using var stream = new MemoryStream();

        // The Save(Stream, int?) overload is obsolete in Avalonia 12.1 and would fail the
        // zero-warning build (CS0618). PngBitmapEncoderOptions is the current surface; its defaults
        // are what we want, so nothing is set on it.
        bitmap.Save(stream, new PngBitmapEncoderOptions());

        return stream.ToArray();
    }

    /// <inheritdoc />
    public byte[] RenderBgra(IconDesign design, int sizePx)
    {
        using RenderTargetBitmap bitmap = Render(design, sizePx);

        // The render target is Bgra8888 with PREMULTIPLIED alpha. A BMP frame inside an .ico wants
        // straight alpha, so the copy goes through a WriteableBitmap declared Unpremul and Skia
        // performs the conversion during CopyPixels. Reading the render target's own pixels directly
        // would hand back premultiplied values and darken every part-transparent edge pixel.
        using var target = new WriteableBitmap(
            new PixelSize(sizePx, sizePx),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Unpremul);

        var pixels = new byte[sizePx * sizePx * BytesPerPixel];
        int stride = sizePx * BytesPerPixel;

        using (ILockedFramebuffer framebuffer = target.Lock())
        {
            bitmap.CopyPixels(framebuffer);

            // Row by row rather than one block copy: the framebuffer's stride is the platform's to
            // choose and need not equal sizePx * 4.
            for (int y = 0; y < sizePx; y++)
            {
                Marshal.Copy(
                    IntPtr.Add(framebuffer.Address, y * framebuffer.RowBytes),
                    pixels,
                    y * stride,
                    stride);
            }
        }

        return pixels;
    }

    private static RenderTargetBitmap Render(IconDesign design, int sizePx)
    {
        ArgumentNullException.ThrowIfNull(design);

        if (sizePx < IconLayout.MinSizePx || sizePx > IconLayout.MaxSizePx)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sizePx),
                sizePx,
                "The canvas edge must be between 1 and 2048 pixels inclusive.");
        }

        // Resolved before the bitmap exists, so a bad icon/weight pair cannot leak a render target.
        IconGlyph glyph = PhosphorIconSet.Instance.GetGlyph(design.Icon, design.Weight);
        IBrush plateBrush = CreatePlateBrush(design.Plate);
        Drawing glyphDrawing = glyph.ToDrawing(new SolidColorBrush(design.GlyphColor));
        RoundedRect plate = IconLayout.PlateRect(sizePx, design.CornerRadiusRatio);
        Matrix glyphTransform = IconLayout.GlyphTransform(glyph.ViewBox, sizePx, design.GlyphScale);

        var bitmap = new RenderTargetBitmap(new PixelSize(sizePx, sizePx));
        try
        {
            using (DrawingContext context = bitmap.CreateDrawingContext())
            {
                context.DrawRectangle(plateBrush, null, plate);

                using (context.PushTransform(glyphTransform))
                {
                    glyphDrawing.Draw(context);
                }
            }
        }
        catch
        {
            // A render target holds a native surface; a throw between construction and the caller's
            // `using` would leak it.
            bitmap.Dispose();
            throw;
        }

        return bitmap;
    }

    private static IBrush CreatePlateBrush(PlateFill fill)
    {
        if (fill.Mode == PlateFillMode.Solid)
        {
            return new SolidColorBrush(fill.PrimaryColor);
        }

        (RelativePoint start, RelativePoint end) = IconLayout.GradientLine(fill.AngleDegrees);

        var brush = new LinearGradientBrush { StartPoint = start, EndPoint = end };
        brush.GradientStops.Add(new GradientStop(fill.PrimaryColor, 0.0));
        brush.GradientStops.Add(new GradientStop(fill.SecondaryColor, 1.0));

        return brush;
    }
}
