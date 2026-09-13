using System;
using System.Globalization;
using System.IO;

namespace Enigma.Icons.AppIconStudio.UnitTests.TestSupport;

/// <summary>A uniquely named temporary directory that deletes itself, and its contents, on dispose.</summary>
/// <remarks>
/// The same helper the other test projects carry. Duplicated rather than shared: a test-support type
/// is not worth a fourth assembly, and the copies have no reason to move together.
/// </remarks>
public sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            string.Format(CultureInfo.InvariantCulture, "enigma-iconstudio-{0:N}", Guid.NewGuid()));

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
