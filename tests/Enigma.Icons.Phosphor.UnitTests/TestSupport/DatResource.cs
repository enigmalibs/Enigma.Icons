using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit.Sdk;

// PhosphorIconSet and PhosphorWeight live in the enclosing Enigma.Icons.Phosphor namespace and need
// no using directive here.
namespace Enigma.Icons.Phosphor.UnitTests.TestSupport;

/// <summary>
/// Reads the six embedded <c>.dat</c> resources straight out of the assembly, bypassing
/// <see cref="PhosphorIconSet"/> — so a test can assert what the artwork actually contains rather
/// than what the reader reports about it.
/// </summary>
internal static class DatResource
{
    /// <summary>
    /// The six manifest resource names of SPEC §7.1, in <c>PhosphorWeight</c> order, written as
    /// literals: they are the expectation, never derived from the assembly under test.
    /// </summary>
    internal static readonly string[] Names =
    {
        "Enigma.Icons.Phosphor.Assets.phosphor.thin.dat",
        "Enigma.Icons.Phosphor.Assets.phosphor.light.dat",
        "Enigma.Icons.Phosphor.Assets.phosphor.regular.dat",
        "Enigma.Icons.Phosphor.Assets.phosphor.bold.dat",
        "Enigma.Icons.Phosphor.Assets.phosphor.fill.dat",
        "Enigma.Icons.Phosphor.Assets.phosphor.duotone.dat",
    };

    /// <summary>The six weight names of SPEC §9, in the same order as <see cref="Names"/>.</summary>
    internal static readonly string[] WeightNames =
    {
        "thin", "light", "regular", "bold", "fill", "duotone",
    };

    /// <summary>The six weights, in declaration order.</summary>
    internal static readonly PhosphorWeight[] Weights =
    {
        PhosphorWeight.Thin,
        PhosphorWeight.Light,
        PhosphorWeight.Regular,
        PhosphorWeight.Bold,
        PhosphorWeight.Fill,
        PhosphorWeight.Duotone,
    };

    /// <summary>The resource name for one weight.</summary>
    internal static string NameOf(PhosphorWeight weight) => Names[(int)weight];

    /// <summary>Opens one resource, failing the test when it is absent.</summary>
    internal static Stream Open(string resourceName)
        => typeof(PhosphorIconSet).Assembly.GetManifestResourceStream(resourceName)
           ?? throw new XunitException($"The embedded resource '{resourceName}' is missing from the assembly.");

    /// <summary>Reads one resource's raw bytes.</summary>
    internal static byte[] ReadAllBytes(string resourceName)
    {
        using Stream stream = Open(resourceName);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    /// <summary>
    /// Reads one resource as LF-separated lines. Line 0 is the header. The trailing empty string the
    /// final newline leaves behind is dropped — but only that one, so an interior blank line stays
    /// visible as an empty entry rather than being silently swallowed.
    /// </summary>
    internal static string[] ReadLines(string resourceName)
    {
        string text = new UTF8Encoding(false).GetString(ReadAllBytes(resourceName));
        string[] parts = text.Split('\n');

        if (parts.Length < 2 || parts[parts.Length - 1].Length != 0)
        {
            throw new XunitException($"The embedded resource '{resourceName}' does not end with a newline.");
        }

        var lines = new string[parts.Length - 1];
        Array.Copy(parts, lines, lines.Length);
        return lines;
    }

    /// <summary>The icon names of one weight, in file order.</summary>
    internal static List<string> IconNamesOf(string resourceName)
    {
        string[] lines = ReadLines(resourceName);
        var names = new List<string>(lines.Length);

        for (int i = 1; i < lines.Length; i++)
        {
            int tab = lines[i].IndexOf('\t');
            names.Add(tab < 0 ? lines[i] : lines[i].Substring(0, tab));
        }

        return names;
    }
}
