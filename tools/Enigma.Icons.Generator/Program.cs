using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

namespace Enigma.Icons.Generator;

/// <summary>
/// Entry point of the deterministic Phosphor asset generator (SPEC §8): same input, byte-identical
/// output, so <c>git diff</c> is the review surface for every icon refresh.
/// </summary>
internal static class Program
{
    private const int ExitSuccess = 0;
    private const int ExitUsage = 1;
    private const int ExitInput = 2;
    private const int ExitStale = 3;

    /// <summary>Runs the tool.</summary>
    /// <param name="args">The command line — see <see cref="CommandLine.Usage"/>.</param>
    /// <returns>0 success, 1 usage error, 2 input/validation failure, 3 <c>--check</c> found a difference.</returns>
    internal static int Main(string[] args)
    {
        CommandLine? options = CommandLine.Parse(args, out string? error);
        if (options is null)
        {
            Console.Error.WriteLine("error: " + error);
            Console.Error.WriteLine(CommandLine.Usage);
            return ExitUsage;
        }

        // Every foreseeable failure below is a diagnostic, never a stack trace on the console.
        try
        {
            return Run(options);
        }
        catch (GeneratorException ex)
        {
            return Fail(ex.Message);
        }
        catch (XmlException ex)
        {
            return Fail("an input file is not well-formed XML: " + ex.Message);
        }
        catch (IOException ex)
        {
            return Fail(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Fail(ex.Message);
        }
    }

    private static int Run(CommandLine options)
    {
        if (!Directory.Exists(options.InputDirectory))
        {
            throw new GeneratorException("the --input directory does not exist: " + options.InputDirectory);
        }

        if (options.CheckOnly && !Directory.Exists(options.OutputDirectory))
        {
            throw new GeneratorException("the --output directory does not exist: " + options.OutputDirectory);
        }

        var vocabulary = new Vocabulary();
        IReadOnlyList<WeightCorpus> corpora = CorpusLoader.Load(options.InputDirectory, vocabulary);

        var target = new OutputTarget(options.OutputDirectory, options.CheckOnly);
        var rows = new List<string>(corpora.Count + 3);
        rows.Add(Row("weight", "icons", "layers", "multi-layer", "bytes"));

        int totalIcons = 0;
        int totalLayers = 0;
        int totalMultiLayer = 0;
        long totalBytes = 0;

        foreach (WeightCorpus corpus in corpora)
        {
            byte[] content = DatRenderer.Render(corpus);
            target.Emit("Assets/phosphor." + corpus.Weight + ".dat", content);

            int layers = 0;
            int multiLayer = 0;
            foreach (IconRecord icon in corpus.Icons)
            {
                layers += icon.Layers.Count;
                if (icon.Layers.Count > 1)
                {
                    multiLayer++;
                }
            }

            rows.Add(Row(corpus.Weight, corpus.Icons.Count, layers, multiLayer, content.Length));

            totalIcons += corpus.Icons.Count;
            totalLayers += layers;
            totalMultiLayer += multiLayer;
            totalBytes += content.Length;
        }

        rows.Add(Row("total", totalIcons, totalLayers, totalMultiLayer, totalBytes));

        // Every weight carries the same 1,512 names (asserted in CorpusLoader), so the first weight
        // is as good as any as the source of the enum order.
        var names = new List<string>(corpora[0].Icons.Count);
        foreach (IconRecord icon in corpora[0].Icons)
        {
            names.Add(icon.Name);
        }

        byte[] enumFile = EnumRenderer.Render(names);
        target.Emit("PhosphorIcon.g.cs", enumFile);

        byte[] namesFile = NamesRenderer.Render(names);
        target.Emit("PhosphorIconNames.g.cs", namesFile);

        foreach (string row in rows)
        {
            Console.Out.WriteLine(row);
        }

        Console.Out.WriteLine();
        Console.Out.WriteLine(Artifact("PhosphorIcon.g.cs", enumFile.Length));
        Console.Out.WriteLine(Artifact("PhosphorIconNames.g.cs", namesFile.Length));

        ReportVocabulary(vocabulary);

        if (!options.CheckOnly)
        {
            Console.Out.WriteLine();
            Console.Out.WriteLine(Count(target.Emitted, "artifacts written."));
            return ExitSuccess;
        }

        if (target.Differences.Count > 0)
        {
            Console.Error.WriteLine("error: the committed assets differ from what the generator produces:");
            foreach (string difference in target.Differences)
            {
                Console.Error.WriteLine("  " + difference);
            }

            Console.Error.WriteLine("re-run the generator without --check and review the diff.");
            return ExitStale;
        }

        Console.Out.WriteLine();
        Console.Out.WriteLine(Count(target.Emitted, "artifacts match the committed bytes."));
        return ExitSuccess;
    }

    private static void ReportVocabulary(Vocabulary vocabulary)
    {
        if (vocabulary.IsEmpty)
        {
            return;
        }

        // SPEC §8.3 step 4 says to ignore this vocabulary, and the generator does — but the v1 .dat
        // format can carry only path data and opacity, so anything listed here is artwork
        // information being dropped on the floor. Not fatal; loud.
        Console.Error.WriteLine("warning: input carries vocabulary the v1 .dat format cannot represent, and it was ignored:");

        foreach (KeyValuePair<string, int> element in vocabulary.Elements)
        {
            Console.Error.WriteLine("  element <" + element.Key + ">: " + element.Value.ToString(CultureInfo.InvariantCulture));
        }

        foreach (KeyValuePair<string, int> attribute in vocabulary.PathAttributes)
        {
            Console.Error.WriteLine("  <path> attribute " + attribute.Key + ": " + attribute.Value.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine("error: " + message);
        return ExitInput;
    }

    private static string Count(int value, string suffix)
        => value.ToString(CultureInfo.InvariantCulture) + " " + suffix;

    private static string Artifact(string name, int bytes)
        => string.Format(CultureInfo.InvariantCulture, "{0,-24}{1,11}", name, bytes);

    private static string Row(string weight, int icons, int layers, int multiLayer, long bytes)
        => Row(
            weight,
            icons.ToString(CultureInfo.InvariantCulture),
            layers.ToString(CultureInfo.InvariantCulture),
            multiLayer.ToString(CultureInfo.InvariantCulture),
            bytes.ToString(CultureInfo.InvariantCulture));

    private static string Row(string weight, string icons, string layers, string multiLayer, string bytes)
        => string.Format(CultureInfo.InvariantCulture, "{0,-10}{1,7}{2,8}{3,13}{4,11}", weight, icons, layers, multiLayer, bytes);
}
