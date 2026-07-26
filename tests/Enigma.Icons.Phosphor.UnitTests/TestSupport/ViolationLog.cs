using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xunit.Sdk;

namespace Enigma.Icons.Phosphor.UnitTests.TestSupport;

/// <summary>
/// Collects every violation found while sweeping the corpus, then fails once with a readable
/// summary.
/// </summary>
/// <remarks>
/// SPEC §12.2 wants the whole 9,072-pair corpus covered. A <c>[Theory]</c> with 9,072 cases would
/// inflate the run and bury the signal, so each rule is one <c>[Fact]</c> that loops the corpus,
/// records what it finds here, and asserts the log is empty — reporting the first
/// <see cref="MaxReported"/> offenders plus a total, which is what makes a failure diagnosable.
/// </remarks>
internal sealed class ViolationLog
{
    private const int MaxReported = 10;

    private readonly List<string> _violations = new List<string>();

    /// <summary>How many violations have been recorded.</summary>
    internal int Count => _violations.Count;

    /// <summary>Records one violation.</summary>
    internal void Add(string message) => _violations.Add(message);

    /// <summary>Records one violation, formatted in the invariant culture.</summary>
    internal void Add(string format, params object[] args)
        => _violations.Add(string.Format(CultureInfo.InvariantCulture, format, args));

    /// <summary>Fails the test when anything was recorded.</summary>
    /// <param name="what">What was being checked, named in the failure message.</param>
    internal void AssertEmpty(string what)
    {
        if (_violations.Count == 0)
        {
            return;
        }

        var message = new StringBuilder();
        message.Append(CultureInfo.InvariantCulture, $"{what}: {_violations.Count} violation(s).");

        int reported = Math.Min(MaxReported, _violations.Count);
        for (int i = 0; i < reported; i++)
        {
            message.Append(Environment.NewLine).Append("  ").Append(_violations[i]);
        }

        if (_violations.Count > reported)
        {
            message
                .Append(Environment.NewLine)
                .Append(CultureInfo.InvariantCulture, $"  … and {_violations.Count - reported} more.");
        }

        throw new XunitException(message.ToString());
    }
}
