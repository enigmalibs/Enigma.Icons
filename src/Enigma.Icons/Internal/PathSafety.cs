using System;
using System.IO;

namespace Enigma.Icons.Internal;

/// <summary>
/// The path-containment check that keeps <see cref="SvgIconSet.FromDirectory"/> inside its root.
/// </summary>
internal static class PathSafety
{
    /// <summary>Determines whether a resolved path lies inside a resolved root directory.</summary>
    /// <param name="root">The already-resolved root directory.</param>
    /// <param name="candidate">The already-resolved candidate path.</param>
    /// <returns><see langword="true"/> when <paramref name="candidate"/> is under <paramref name="root"/>.</returns>
    internal static bool IsWithin(string root, string candidate)
    {
        if (string.IsNullOrEmpty(root) || string.IsNullOrEmpty(candidate))
        {
            return false;
        }

        string prefix = root;
        if (prefix[prefix.Length - 1] != Path.DirectorySeparatorChar)
        {
            prefix += Path.DirectorySeparatorChar;
        }

        // Windows paths are case-insensitive; POSIX paths are not.
        StringComparison comparison = Path.DirectorySeparatorChar == '\\'
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return candidate.StartsWith(prefix, comparison);
    }
}
