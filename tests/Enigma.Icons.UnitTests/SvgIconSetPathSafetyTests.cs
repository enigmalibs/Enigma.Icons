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

    [Fact]
    public void FromDirectory_DoesNotFollowAFileSymlinkOutOfTheRoot()
    {
        using var root = new TempDirectory();
        using var outside = new TempDirectory();

        string secret = outside.WriteFile("secret.svg", SquareSvg);
        root.WriteFile(Path.Combine("thin", "square.svg"), SquareSvg);

        // Path.GetFullPath does not resolve symlinks, so this path is literally inside the root and
        // passes IsWithin — only the ReparsePoint test stops it.
        if (!TryCreateFileSymlink(Path.Combine(root.Path, "thin", "escaped.svg"), secret))
        {
            Assert.Skip("Creating a file symlink is not permitted in this environment.");
            return;
        }

        SvgIconSet set = SvgIconSet.FromDirectory(root.Path);

        Assert.Equal(new[] { "square" }, set.IconNames.ToArray());
        Assert.False(set.TryGetGlyph("escaped", "thin", out IconGlyph? glyph));
        Assert.Null(glyph);
    }

    [Fact]
    public void FromDirectory_DoesNotFollowAFileSymlinkWhenVariantsAreOff()
    {
        using var root = new TempDirectory();
        using var outside = new TempDirectory();

        string secret = outside.WriteFile("secret.svg", SquareSvg);
        root.WriteFile("square.svg", SquareSvg);

        if (!TryCreateFileSymlink(Path.Combine(root.Path, "escaped.svg"), secret))
        {
            Assert.Skip("Creating a file symlink is not permitted in this environment.");
            return;
        }

        SvgIconSet set = SvgIconSet.FromDirectory(root.Path, variantsFromSubfolders: false);

        Assert.Equal(new[] { "square" }, set.IconNames.ToArray());
        Assert.False(set.TryGetGlyph("escaped", null, out _));
    }

    [Fact]
    public void FromDirectory_DoesNotFollowAFileSymlinkToANonSvgTarget()
    {
        using var root = new TempDirectory();
        using var outside = new TempDirectory();

        // The pre-fix behaviour read the target and surfaced it as an SvgParseException on first
        // lookup; now the entry never enters the index at all.
        string plain = outside.WriteFile("hosts", "127.0.0.1 localhost\n");
        root.WriteFile("square.svg", SquareSvg);

        if (!TryCreateFileSymlink(Path.Combine(root.Path, "escaped.svg"), plain))
        {
            Assert.Skip("Creating a file symlink is not permitted in this environment.");
            return;
        }

        SvgIconSet set = SvgIconSet.FromDirectory(root.Path, variantsFromSubfolders: false);

        Assert.DoesNotContain("escaped", set.IconNames);
        Assert.False(set.TryGetGlyph("escaped", null, out _));
    }

    [Fact]
    public void FromDirectory_SkipsASymlinkedFileThatPointsInsideTheRoot()
    {
        using var root = new TempDirectory();

        string square = root.WriteFile("square.svg", SquareSvg);

        // A symlink is skipped on the attribute, not on where it leads: resolving the target to
        // decide would need FileSystemInfo.LinkTarget, which is .NET 6+ and netstandard2.0 is in
        // the TFM set.
        if (!TryCreateFileSymlink(Path.Combine(root.Path, "alias.svg"), square))
        {
            Assert.Skip("Creating a file symlink is not permitted in this environment.");
            return;
        }

        SvgIconSet set = SvgIconSet.FromDirectory(root.Path, variantsFromSubfolders: false);

        Assert.Equal(new[] { "square" }, set.IconNames.ToArray());
        Assert.NotNull(set.GetGlyph("square"));
    }

    /// <summary>
    /// Creates a file symlink, reporting false instead of throwing where the platform does not allow
    /// it — on Windows this needs elevation or Developer Mode.
    /// </summary>
    private static bool TryCreateFileSymlink(string link, string target)
    {
        try
        {
            File.CreateSymbolicLink(link, target);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            return false;
        }
    }
}
