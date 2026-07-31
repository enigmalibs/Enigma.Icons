using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Enigma.Icons.Phosphor.UnitTests.TestSupport;
using Xunit;
using Xunit.Sdk;

namespace Enigma.Icons.Phosphor.UnitTests;

/// <summary>
/// SPEC §15 — the concurrency row: every member is safe for concurrent use from any thread, and two
/// threads never observe different <see cref="IconGlyph"/> instances for the same pair.
/// </summary>
public sealed class ConcurrencyTests
{
    private const int IconCount = 1512;

    [Fact]
    public async Task ThreadsRacingTheFirstTouchOfAWeight_AllSeeOneGlyphInstance()
    {
        // The interesting race is the *first* touch, when the weight table and the glyph are built
        // at once. Every thread is held at the gate so they start together.
        PhosphorIconSet set = NewUncachedSet();
        CancellationToken token = TestContext.Current.CancellationToken;
        const int Threads = 16;

        using var gate = new ManualResetEventSlim(false);
        var tasks = new Task<IconGlyph>[Threads];

        for (int i = 0; i < Threads; i++)
        {
            tasks[i] = Task.Run(
                () =>
                {
                    gate.Wait(token);
                    return set.GetGlyph(PhosphorIcon.Acorn, PhosphorWeight.Duotone);
                },
                token);
        }

        gate.Set();
        IconGlyph[] glyphs = await Task.WhenAll(tasks);

        foreach (IconGlyph glyph in glyphs)
        {
            Assert.Same(glyphs[0], glyph);
        }
    }

    [Fact]
    public async Task ThreadsRacingTheFirstTouchOfEveryWeight_NeverThrow()
    {
        PhosphorIconSet set = NewUncachedSet();
        CancellationToken token = TestContext.Current.CancellationToken;
        const int ThreadsPerWeight = 4;

        using var gate = new ManualResetEventSlim(false);
        var tasks = new List<Task>();

        foreach (PhosphorWeight weight in DatResource.Weights)
        {
            for (int i = 0; i < ThreadsPerWeight; i++)
            {
                PhosphorWeight captured = weight;
                tasks.Add(Task.Run(
                    () =>
                    {
                        gate.Wait(token);
                        set.GetGlyph(PhosphorIcon.Acorn, captured);
                    },
                    token));
            }
        }

        gate.Set();

        // A faulted task surfaces here, which is exactly the failure this asserts against.
        await Task.WhenAll(tasks);
    }

    [Fact]
    public void ManyThreadsHammeringManyPairs_NeverThrowAndAlwaysAgree()
    {
        PhosphorIconSet set = NewUncachedSet();
        const int Threads = 8;
        const int IconsPerThread = 96;

        var failures = new ConcurrentQueue<string>();
        var observed = new ConcurrentDictionary<(PhosphorWeight Weight, string Name), IconGlyph>();

        Parallel.For(0, Threads, thread =>
        {
            try
            {
                foreach (PhosphorWeight weight in DatResource.Weights)
                {
                    for (int i = 0; i < IconsPerThread; i++)
                    {
                        // Deliberately overlapping strides, so the threads collide on the same pairs
                        // instead of each working a private slice.
                        var icon = (PhosphorIcon)(((i * 13) + (thread * 5)) % IconCount);
                        string name = PhosphorIconNames.ToKebabCase(icon);

                        IconGlyph typed = set.GetGlyph(icon, weight);
                        IconGlyph byName = set.GetGlyph(name, set.Variants[(int)weight]);

                        if (!ReferenceEquals(typed, byName))
                        {
                            failures.Enqueue($"{weight}/{name}: the typed and string surfaces returned different instances.");
                        }

                        IconGlyph first = observed.GetOrAdd((weight, name), typed);
                        if (!ReferenceEquals(first, typed))
                        {
                            failures.Enqueue($"{weight}/{name}: two different instances were observed.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                failures.Enqueue($"thread {thread} threw: {ex}");
            }
        });

        Assert.Empty(failures);
    }

    /// <summary>
    /// A set with empty caches. <see cref="PhosphorIconSet.Instance"/> has almost certainly loaded
    /// every weight by the time this class runs, so a first-touch race is unreachable through it.
    /// </summary>
    /// <remarks>
    /// Reflection over a private constructor is confined to the test assembly — the trim- and
    /// AOT-clean constraint of SPEC §10.4 applies to the package, which contains no reflection at
    /// all.
    /// </remarks>
    private static PhosphorIconSet NewUncachedSet()
        => Activator.CreateInstance(typeof(PhosphorIconSet), nonPublic: true) as PhosphorIconSet
           ?? throw new XunitException("Could not construct a PhosphorIconSet through its private constructor.");
}
