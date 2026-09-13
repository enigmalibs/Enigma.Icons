using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Enigma.Icons.AppIconStudio.Export;

/// <summary>What one export actually wrote.</summary>
/// <remarks>
/// The absolute paths, not a count: the status line quotes the folder and the number, but a caller
/// that wants to say which files landed — or a test that wants to assert it — needs the names.
/// </remarks>
public sealed class ExportResult
{
    /// <summary>Initializes a new result.</summary>
    /// <param name="outputDirectory">The directory everything was written into.</param>
    /// <param name="icoPath">The absolute path of the <c>.ico</c>, or null when none was requested.</param>
    /// <param name="pngPaths">The absolute paths of the standalone PNGs, ascending by size.</param>
    /// <exception cref="ArgumentNullException"><paramref name="outputDirectory"/> or
    /// <paramref name="pngPaths"/> is null.</exception>
    public ExportResult(string outputDirectory, string? icoPath, IReadOnlyList<string> pngPaths)
    {
        if (outputDirectory is null)
        {
            throw new ArgumentNullException(nameof(outputDirectory));
        }

        if (pngPaths is null)
        {
            throw new ArgumentNullException(nameof(pngPaths));
        }

        OutputDirectory = outputDirectory;
        IcoPath = icoPath;

        var png = new string[pngPaths.Count];
        for (int i = 0; i < png.Length; i++)
        {
            png[i] = pngPaths[i];
        }

        PngPaths = Array.AsReadOnly(png);

        var all = new List<string>(png.Length + (icoPath is null ? 0 : 1));
        if (icoPath is not null)
        {
            all.Add(icoPath);
        }

        all.AddRange(png);

        WrittenFiles = new ReadOnlyCollection<string>(all);
    }

    /// <summary>The directory everything was written into.</summary>
    public string OutputDirectory { get; }

    /// <summary>The absolute path of the <c>.ico</c>, or null when none was requested.</summary>
    public string? IcoPath { get; }

    /// <summary>The absolute paths of the standalone PNGs, ascending by size.</summary>
    public IReadOnlyList<string> PngPaths { get; }

    /// <summary>Every file written, the icon first. Never null.</summary>
    public IReadOnlyList<string> WrittenFiles { get; }
}
