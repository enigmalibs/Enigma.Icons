using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Enigma.Icons.AppIconStudio.Rendering;

namespace Enigma.Icons.AppIconStudio.Export;

/// <summary>Rasterizes a design and writes the icon and PNG files it asks for.</summary>
/// <remarks>
/// <para>
/// <b>Rasterize first, then write.</b> Every frame is rendered and every payload assembled before a
/// single file is opened, so a rendering failure leaves the output directory untouched rather than
/// half-written. The rendering half is synchronous and runs on the caller's thread — the UI thread,
/// per <see cref="IIconRasterizer"/>'s contract — and only the file writes are asynchronous.
/// </para>
/// <para>
/// <b>Names.</b> <c>&lt;base&gt;.ico</c> and <c>&lt;base&gt;-&lt;size&gt;.png</c>: the shape an
/// Avalonia app consumes directly, with the <c>.ico</c> going to <c>&lt;ApplicationIcon&gt;</c> and
/// the 256 px PNG to an About or splash view. Existing files are overwritten — this is a generator,
/// and a run that refused to replace its own previous output would be useless.
/// </para>
/// </remarks>
public sealed class IconExporter
{
    private readonly IIconRasterizer _rasterizer;

    /// <summary>Initializes a new exporter.</summary>
    /// <param name="rasterizer">The rasterizer that turns the design into pixels.</param>
    /// <exception cref="ArgumentNullException"><paramref name="rasterizer"/> is null.</exception>
    public IconExporter(IIconRasterizer rasterizer)
        => _rasterizer = rasterizer ?? throw new ArgumentNullException(nameof(rasterizer));

    /// <summary>Runs the export.</summary>
    /// <param name="request">What to compose, where, and in which sizes.</param>
    /// <param name="cancellationToken">Cancels between frames and between writes.</param>
    /// <returns>The files written.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is null.</exception>
    /// <exception cref="DirectoryNotFoundException"><see cref="ExportRequest.OutputDirectory"/> does
    /// not exist. It is the caller's to create — silently making a directory the user mistyped is how
    /// output ends up somewhere nobody looks.</exception>
    /// <exception cref="IOException">A file could not be written.</exception>
    /// <exception cref="UnauthorizedAccessException">The directory or a file is not writable.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was signalled.</exception>
    public async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        string root = Path.GetFullPath(request.OutputDirectory);
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException(
                string.Format(CultureInfo.InvariantCulture, "The output directory '{0}' does not exist.", root));
        }

        byte[]? ico = null;
        if (request.IcoSizes.Count > 0)
        {
            var frames = new List<IcoFrame>(request.IcoSizes.Count);
            for (int i = 0; i < request.IcoSizes.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int size = request.IcoSizes[i];
                frames.Add(IcoWriter.UsesPngFrame(size)
                    ? IcoFrame.FromPng(size, _rasterizer.RenderPng(request.Design, size))
                    : IcoFrame.FromPixels(size, _rasterizer.RenderBgra(request.Design, size)));
            }

            ico = IcoWriter.Write(frames);
        }

        var pngs = new List<byte[]>(request.PngSizes.Count);
        for (int i = 0; i < request.PngSizes.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            pngs.Add(_rasterizer.RenderPng(request.Design, request.PngSizes[i]));
        }

        string? icoPath = null;
        if (ico is not null)
        {
            icoPath = ResolvePath(root, request.BaseName + ".ico");
            await File.WriteAllBytesAsync(icoPath, ico, cancellationToken).ConfigureAwait(true);
        }

        var pngPaths = new List<string>(pngs.Count);
        for (int i = 0; i < pngs.Count; i++)
        {
            string name = string.Format(
                CultureInfo.InvariantCulture,
                "{0}-{1}.png",
                request.BaseName,
                request.PngSizes[i]);

            string path = ResolvePath(root, name);
            await File.WriteAllBytesAsync(path, pngs[i], cancellationToken).ConfigureAwait(true);
            pngPaths.Add(path);
        }

        return new ExportResult(root, icoPath, pngPaths);
    }

    /// <summary>
    /// Combines the directory and a file name, and proves the result is still inside the directory.
    /// </summary>
    /// <remarks>
    /// Belt and braces: <see cref="ExportRequest.IsValidBaseName"/> has already rejected separators,
    /// so nothing should ever escape. The check stays because the cost is one string comparison and
    /// the failure mode it guards — a generator writing outside the folder the user picked — is the
    /// kind that is only noticed afterwards.
    /// </remarks>
    private static string ResolvePath(string root, string fileName)
    {
        string full = Path.GetFullPath(Path.Combine(root, fileName));

        string prefix = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;

        if (!full.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The file name '{0}' resolves outside the output directory.",
                    fileName));
        }

        return full;
    }
}
