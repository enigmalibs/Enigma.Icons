using System;
using System.Collections.Generic;

namespace Enigma.Icons.Generator;

/// <summary>
/// The tool's complete argument surface — <c>--input</c>, <c>--output</c>, <c>--check</c>, and
/// nothing else (SPEC §8.1, §8.5).
/// </summary>
internal sealed class CommandLine
{
    /// <summary>The one-line usage text, printed to stderr on any argument error.</summary>
    internal const string Usage =
        "usage: Enigma.Icons.Generator --input <dir> --output <dir> [--check]";

    private CommandLine(string inputDirectory, string outputDirectory, bool checkOnly)
    {
        InputDirectory = inputDirectory;
        OutputDirectory = outputDirectory;
        CheckOnly = checkOnly;
    }

    /// <summary>The directory holding the six weight subdirectories.</summary>
    internal string InputDirectory { get; }

    /// <summary>The <c>Enigma.Icons.Phosphor</c> project directory the artifacts are written to.</summary>
    internal string OutputDirectory { get; }

    /// <summary>When set, regenerate into memory and compare instead of writing (SPEC §8.5).</summary>
    internal bool CheckOnly { get; }

    /// <summary>
    /// Parses the command line. Returns <see langword="null"/> and sets <paramref name="error"/> on
    /// any usage problem: an unknown argument, a missing value, a repeated flag, or a missing
    /// required option.
    /// </summary>
    internal static CommandLine? Parse(IReadOnlyList<string> args, out string? error)
    {
        string? input = null;
        string? output = null;
        bool check = false;

        for (int i = 0; i < args.Count; i++)
        {
            string arg = args[i];

            switch (arg)
            {
                case "--input":
                case "--output":
                {
                    bool isInput = string.Equals(arg, "--input", StringComparison.Ordinal);
                    if ((isInput ? input : output) is not null)
                    {
                        error = arg + " was given more than once.";
                        return null;
                    }

                    if (i + 1 >= args.Count)
                    {
                        error = arg + " requires a directory.";
                        return null;
                    }

                    string value = args[++i];
                    if (value.Length == 0)
                    {
                        error = arg + " requires a non-empty directory.";
                        return null;
                    }

                    if (isInput)
                    {
                        input = value;
                    }
                    else
                    {
                        output = value;
                    }

                    break;
                }

                case "--check":
                {
                    if (check)
                    {
                        error = "--check was given more than once.";
                        return null;
                    }

                    check = true;
                    break;
                }

                default:
                {
                    error = "unknown argument \"" + arg + "\".";
                    return null;
                }
            }
        }

        if (input is null)
        {
            error = "--input is required.";
            return null;
        }

        if (output is null)
        {
            error = "--output is required.";
            return null;
        }

        error = null;
        return new CommandLine(input, output, check);
    }
}
