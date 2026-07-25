using System;
using System.Globalization;
using System.IO;

namespace Enigma.Icons.UnitTests.TestSupport;

/// <summary>A uniquely named temporary directory that deletes itself, and its contents, on dispose.</summary>
public sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            string.Format(CultureInfo.InvariantCulture, "enigma-icons-{0:N}", Guid.NewGuid()));

        Directory.CreateDirectory(Path);
    }

    /// <summary>The absolute path of the directory.</summary>
    public string Path { get; }

    /// <summary>Creates a subdirectory and returns its absolute path.</summary>
    public string CreateSubdirectory(string name)
    {
        string full = System.IO.Path.Combine(Path, name);
        Directory.CreateDirectory(full);
        return full;
    }

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
