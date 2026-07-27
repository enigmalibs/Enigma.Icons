using System;
using System.IO;
using System.Reflection;
using Enigma.Icons.Phosphor.UnitTests.TestSupport;
using Xunit;

namespace Enigma.Icons.Phosphor.UnitTests;

/// <summary>
/// The load-bearing gate on the packaging. Manifest resource names are synthesized by MSBuild from
/// the root namespace, the folder and the file name, and they are case-sensitive — so a renamed
/// folder, a RootNamespace override, or a glob that matches nothing all produce a silently empty
/// resource set: the build stays green and every lookup throws at runtime. These assertions catch
/// that at test time instead.
/// </summary>
public sealed class ResourceManifestTests
{
    [Fact]
    public void Assembly_ExposesExactlyTheSixSpecifiedResourceNames()
    {
        Assembly assembly = typeof(PhosphorIconSet).Assembly;

        string[] actual = assembly.GetManifestResourceNames();
        Array.Sort(actual, StringComparer.Ordinal);

        var expected = (string[])DatResource.Names.Clone();
        Array.Sort(expected, StringComparer.Ordinal);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EveryResource_OpensAndIsNonEmpty()
    {
        var violations = new ViolationLog();

        foreach (string name in DatResource.Names)
        {
            using Stream? stream = typeof(PhosphorIconSet).Assembly.GetManifestResourceStream(name);

            if (stream is null)
            {
                violations.Add("{0}: no stream.", name);
                continue;
            }

            if (stream.Length == 0)
            {
                violations.Add("{0}: empty stream.", name);
            }
        }

        violations.AssertEmpty("Embedded resources that do not open with content");
    }

    [Fact]
    public void ProductLookup_ReachesEveryEmbeddedResource()
    {
        // The manifest names above are inert data until something looks through them. This proves
        // the names PhosphorIconSet builds internally agree with the ones MSBuild produced: a
        // divergence surfaces as InvalidDataException ("resource not found"), not as a miss.
        var violations = new ViolationLog();

        foreach (PhosphorWeight weight in DatResource.Weights)
        {
            try
            {
                PhosphorIconSet.Instance.GetGlyph(PhosphorIcon.Acorn, weight);
            }
            catch (InvalidDataException ex)
            {
                violations.Add("{0}: {1}", weight, ex.Message);
            }
        }

        violations.AssertEmpty("Weights whose embedded resource could not be reached");
    }
}
