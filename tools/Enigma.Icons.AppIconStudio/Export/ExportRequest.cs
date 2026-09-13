using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using Enigma.Icons.AppIconStudio.Design;
using Enigma.Icons.AppIconStudio.Rendering;

namespace Enigma.Icons.AppIconStudio.Export;

/// <summary>One "Generate" press: what to compose, where to put it, and in which sizes.</summary>
/// <remarks>
/// Validation lives here rather than in <see cref="IconExporter"/> so that a request that exists is
/// always a request that can be run. The size lists are normalized on the way in — de-duplicated and
/// sorted ascending — so the exporter never has to think about the order a check-list happened to be
/// clicked in.
/// </remarks>
public sealed class ExportRequest
{
    /// <summary>The largest PNG this request will accept, matching the rasterizer's own ceiling.</summary>
    public const int MaxPngSizePx = IconLayout.MaxSizePx;

    /// <summary>Initializes a new export request.</summary>
    /// <param name="design">The design to compose.</param>
    /// <param name="outputDirectory">An existing directory to write into.</param>
    /// <param name="baseName">
    /// The file-name stem: <c>app</c> produces <c>app.ico</c> and <c>app-256.png</c>. Trimmed, and
    /// required to be a plain file name — see <see cref="IsValidBaseName"/>.
    /// </param>
    /// <param name="icoSizes">The frame sizes of the <c>.ico</c>, each 1–256. Empty writes no icon.</param>
    /// <param name="pngSizes">The standalone PNG sizes, each 1–2048. Empty writes no PNGs.</param>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="outputDirectory"/> is blank, <paramref name="baseName"/> is not a valid plain
    /// file name, both size lists are empty, or a size is outside its range.
    /// </exception>
    public ExportRequest(
        IconDesign design,
        string outputDirectory,
        string baseName,
        IReadOnlyList<int> icoSizes,
        IReadOnlyList<int> pngSizes)
    {
        if (design is null)
        {
            throw new ArgumentNullException(nameof(design));
        }

        if (outputDirectory is null)
        {
            throw new ArgumentNullException(nameof(outputDirectory));
        }

        if (baseName is null)
        {
            throw new ArgumentNullException(nameof(baseName));
        }

        if (icoSizes is null)
        {
            throw new ArgumentNullException(nameof(icoSizes));
        }

        if (pngSizes is null)
        {
            throw new ArgumentNullException(nameof(pngSizes));
        }

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException("An output directory is required.", nameof(outputDirectory));
        }

        string trimmedName = baseName.Trim();
        if (!IsValidBaseName(trimmedName))
        {
            throw new ArgumentException(
                "The base name must be a plain file name — no directory separators, no invalid characters, and not '.' or '..'.",
                nameof(baseName));
        }

        ReadOnlyCollection<int> ico = Normalize(icoSizes, IcoWriter.MinFrameSizePx, IcoWriter.MaxFrameSizePx, nameof(icoSizes));
        ReadOnlyCollection<int> png = Normalize(pngSizes, IconLayout.MinSizePx, MaxPngSizePx, nameof(pngSizes));

        if (ico.Count == 0 && png.Count == 0)
        {
            throw new ArgumentException(
                "Nothing to export — select at least one ICO frame size or one PNG size.",
                nameof(icoSizes));
        }

        Design = design;
        OutputDirectory = outputDirectory;
        BaseName = trimmedName;
        IcoSizes = ico;
        PngSizes = png;
    }

    /// <summary>The design to compose.</summary>
    public IconDesign Design { get; }

    /// <summary>The directory to write into.</summary>
    public string OutputDirectory { get; }

    /// <summary>The trimmed file-name stem.</summary>
    public string BaseName { get; }

    /// <summary>The <c>.ico</c> frame sizes, ascending and distinct. Empty writes no icon.</summary>
    public IReadOnlyList<int> IcoSizes { get; }

    /// <summary>The standalone PNG sizes, ascending and distinct. Empty writes no PNGs.</summary>
    public IReadOnlyList<int> PngSizes { get; }

    /// <summary>Whether a string is usable as the file-name stem.</summary>
    /// <remarks>
    /// Public so the UI can disable its Generate button on the same rule the constructor enforces,
    /// rather than discovering the answer by catching an exception. Separators are rejected
    /// explicitly as well as through <see cref="Path.GetInvalidFileNameChars"/>, because
    /// <c>'\'</c> is a perfectly legal file-name character on Linux and would otherwise slip through.
    /// </remarks>
    /// <param name="baseName">The candidate stem, already trimmed.</param>
    /// <returns><see langword="true"/> when it names a file and nothing else.</returns>
    public static bool IsValidBaseName(string? baseName)
    {
        if (string.IsNullOrWhiteSpace(baseName))
        {
            return false;
        }

        if (baseName != baseName.Trim())
        {
            return false;
        }

        if (baseName is "." or "..")
        {
            return false;
        }

        if (baseName.IndexOf('/') >= 0 || baseName.IndexOf('\\') >= 0)
        {
            return false;
        }

        return baseName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
    }

    private static ReadOnlyCollection<int> Normalize(
        IReadOnlyList<int> sizes,
        int min,
        int max,
        string parameterName)
    {
        var distinct = new SortedSet<int>();

        for (int i = 0; i < sizes.Count; i++)
        {
            int size = sizes[i];
            if (size < min || size > max)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} px is outside the allowed range {1}-{2}.",
                        size,
                        min,
                        max),
                    parameterName);
            }

            distinct.Add(size);
        }

        var ordered = new int[distinct.Count];
        distinct.CopyTo(ordered);

        return Array.AsReadOnly(ordered);
    }
}
