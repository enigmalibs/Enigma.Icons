using System;
using System.Globalization;
using System.IO;

namespace Enigma.Icons.Phosphor.UnitTests.TestSupport;

/// <summary>
/// A uniquely named temporary directory that deletes itself, and its contents, on dispose.
/// </summary>
/// <remarks>
/// A deliberate twin of <c>Enigma.Icons.UnitTests.TestSupport.TempDirectory</c>. The two test
/// projects share no assembly and neither is packable, so a copy is cheaper than a fourth project
/// created only to hold this class.
/// </remarks>
public sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            string.Format(CultureInfo.InvariantCulture, "enigma-icons-phosphor-{0:N}", Guid.NewGuid()));

        Directory.CreateDirectory(Path);
    }

    /// <summary>The absolute path of the directory.</summary>
    public string Path { get; }

    /// <summary>Writes a file relative to the directory root and returns its absolute path.</summary>
    public string WriteFile(string relativePath, string content)
    {
        string full = System.IO.Path.Combine(Path, relativePath);
        string? parent = System.IO.Path.GetDirectoryName(full);
        if (parent is not null)
        {
            Directory.CreateDirectory(parent);
        }

        File.WriteAllText(full, content);
        return full;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch (IOException)
        {
            // A leftover temp directory is not worth failing a test over.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
