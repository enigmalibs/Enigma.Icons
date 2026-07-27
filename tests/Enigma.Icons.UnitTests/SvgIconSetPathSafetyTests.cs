using System;
using System.IO;
using System.Linq;
using Enigma.Icons.Internal;
using Enigma.Icons.UnitTests.TestSupport;
using Xunit;

namespace Enigma.Icons.UnitTests;

public sealed class SvgIconSetPathSafetyTests
{
    private const string SquareSvg =
        "<svg viewBox=\"0 0 256 256\"><path d=\"M 0,0 L 8,0 L 8,8 Z\" /></svg>";

    [Fact]
    public void IsWithin_AcceptsAContainedPath()
    {
        string root = Path.Combine(Path.GetTempPath(), "root");

        Assert.True(PathSafety.IsWithin(root, Path.Combine(root, "icon.svg")));
        Assert.True(PathSafety.IsWithin(root, Path.Combine(root, "bold", "icon.svg")));
    }

    [Fact]
    public void IsWithin_RejectsAnEscapingPath()
    {
        string root = Path.Combine(Path.GetTempPath(), "root");

        Assert.False(PathSafety.IsWithin(root, Path.Combine(Path.GetTempPath(), "elsewhere", "icon.svg")));
        Assert.False(PathSafety.IsWithin(root, Path.GetTempPath()));
    }

    [Fact]
    public void IsWithin_RejectsTheRootItself()
    {
        string root = Path.Combine(Path.GetTempPath(), "root");

        Assert.False(PathSafety.IsWithin(root, root));
    }

    [Fact]
    public void IsWithin_RejectsASiblingWithTheRootAsANamePrefix()
    {
        // "/tmp/root-evil/icon.svg" must not count as inside "/tmp/root".
        string root = Path.Combine(Path.GetTempPath(), "root");
        string sibling = Path.Combine(Path.GetTempPath(), "root-evil", "icon.svg");

        Assert.False(PathSafety.IsWithin(root, sibling));
    }

    [Fact]
    public void IsWithin_RejectsBlankArguments()
    {
        Assert.False(PathSafety.IsWithin(string.Empty, "/tmp/icon.svg"));
        Assert.False(PathSafety.IsWithin("/tmp", string.Empty));
    }

    [Fact]
    public void FromDirectory_DoesNotFollowADirectorySymlinkOutOfTheRoot()
    {
        using var root = new TempDirectory();
        using var outside = new TempDirectory();

        outside.WriteFile("secret.svg", SquareSvg);
        root.WriteFile(Path.Combine("thin", "square.svg"), SquareSvg);

        string link = Path.Combine(root.Path, "escaped");
        try
        {
            Directory.CreateSymbolicLink(link, outside.Path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            // Creating a directory symlink is unprivileged on Linux but may not be elsewhere. The
            // IsWithin tests above cover the containment logic unconditionally.
            Assert.Skip("Creating a directory symlink is not permitted in this environment.");
            return;
        }

        SvgIconSet set = SvgIconSet.FromDirectory(root.Path);

        Assert.Equal(new[] { "square" }, set.IconNames.ToArray());
        Assert.DoesNotContain("secret", set.IconNames);
        Assert.DoesNotContain("escaped", set.Variants);
    }
}
