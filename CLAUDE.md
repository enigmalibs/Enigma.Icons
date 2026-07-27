# CLAUDE.md

Agent instructions for the `Enigma.Icons` solution.

## Commands

```bash
# Build the whole solution
dotnet build Enigma.Icons.slnx

# Run the whole test suite (a solution path must be passed via --solution on the .NET 10 SDK;
# a bare `dotnet test` from the repo root works too)
dotnet test --solution Enigma.Icons.slnx

# Pack one package (Release)
dotnet pack src/<Project>/<Project>.csproj -c Release
```

Regenerate the Phosphor assets — extract the pinned snapshot first, then run the generator
(SPEC §8.1):

```bash
mkdir -p /tmp/phosphor-flat
tar -xzf docs/reference/phosphor/phosphor-2.1.1-svgs-flat.tar.gz -C /tmp/phosphor-flat

dotnet run --project tools/Enigma.Icons.Generator -- \
    --input  /tmp/phosphor-flat \
    --output src/Enigma.Icons.Phosphor
```

Add `--check` to the generator invocation for a staleness gate: it regenerates into memory, writes
nothing, and exits non-zero if the committed assets differ from what it would produce (SPEC §8.5).
Never extract the snapshot inside the working tree — `.gitignore` has no rule for it, so 9,072 stray
`.svg` files would land in the next commit.

The generator's exit codes are the failure surface — never treat non-zero as "just rerun it":

| Code | Meaning |
|---|---|
| 0 | success (a non-fatal `warning:` block on stderr still exits 0) |
| 1 | usage error — unknown/repeated/missing argument; usage is printed to stderr |
| 2 | input or validation failure — missing weight directory, cross-weight name-set mismatch, zero paths, empty `d`, a `viewBox` that is absent, malformed or inconsistent within a weight, or an out-of-range `opacity` |
| 3 | `--check` found a difference; the per-file report names each artifact and the first differing offset |

The test runner is Microsoft.Testing.Platform, selected by `global.json` (SPEC §3.1) — there is no
`Microsoft.NET.Test.Sdk` or VSTest in this solution.

> As of FEATURE-469B the slnx holds **all eight** end-state projects (SPEC §3.4) — every path above
> exists. `dotnet pack` applies to the three packable ones: `Enigma.Icons`, `Enigma.Icons.Phosphor`
> and `Enigma.Icons.Avalonia`. FEATURE-718F has since written the four READMEs to their shipped
> state, and **FEATURE-74DC has completed 1.0.0 release preparation** — all three packages carry
> `<Version>1.0.0</Version>` and `<PackageReleaseNotes>`, `RELEASENOTES.md` is filled, and the
> pack/tag/push runbook lives in **`docs/RELEASE.md`**. The 1.0.0 line is therefore feature-complete;
> the only remaining roadmap item, FEATURE-6FA1 (the WPF sibling), is **deferred post-1.0 and must
> not be built**.

**Releasing is the user's job, never the agent's.** `docs/RELEASE.md` is a runbook to *print and
follow*, not to execute: the merge to `main`, `git tag`, the publish `dotnet pack` and
`dotnet nuget push` are all outward-facing and belong to the user. The NuGet API key is a secret —
never store, commit, or echo it. Note the repo currently has **no `git remote`**, so the publish path
cannot run until one is added.

Run the gallery — the sample app, and the only way to verify rendering by eye:

```bash
dotnet run --project samples/Enigma.Icons.Avalonia.Gallery
```

It needs a real desktop session (`DISPLAY`/`WAYLAND_DISPLAY`); a headless shell cannot show its window.

## Architecture

```
Enigma.Icons              model + SVG parser + IIconSet        (zero dependencies)
    ↑
Enigma.Icons.Phosphor     the Phosphor artwork pack            (references Enigma.Icons only)
    ↑
Enigma.Icons.Avalonia     Geometry/Drawing conversion, markup extensions, the Icon control
```

- `tools/Enigma.Icons.Generator` — non-packable console app; writes the committed `.dat` resources
  and the generated `PhosphorIcon` enum into `src/Enigma.Icons.Phosphor/`.
- `samples/Enigma.Icons.Avalonia.Gallery` — non-packable Avalonia desktop app; the visual
  verification unit tests cannot provide.

Neither is packable, and neither is referenced by any packable project.

## Hard rules (SPEC §2)

1. **Zero-warning builds.** `TreatWarningsAsErrors` and `EnforceCodeStyleInBuild` are on solution-wide — a warning is a build failure. **Exception to watch:** Avalonia's XAML compiler logs its `AVLN*` diagnostics from an MSBuild task, and `TreatWarningsAsErrors` does **not** promote those — the build reports `Build succeeded` with warnings. Read the warning count on any project that compiles `.axaml`; do not trust the exit code alone.
2. **`ImplicitUsings` is `disable`** in every csproj; every file declares its own `using` directives.
3. **`Nullable` enable** everywhere; no `!` without a commented justification.
4. **`LangVersion 14`.**
5. **Central Package Management** — versions live only in `Directory.Packages.props`; a `<PackageReference>` never carries `Version=`.
6. **`.slnx`, never `.sln`.**
7. **Every text file is LF with a final newline**, generated files included.
8. **XML doc comments on every public member** — with `GenerateDocumentationFile`, CS1591 is a build error.
9. **Never commit.** The user owns all commits, tags, and pushes; a build stages the tree and prints a suggested commit message.
10. **Zero third-party runtime dependencies** in `Enigma.Icons` (none at all) and `Enigma.Icons.Phosphor` (the sibling `Enigma.Icons` only — a family reference is not third-party).
11. **No `Enum.ToString()` / `Enum.Parse` on hot paths** — use the generated lookup tables instead.

## Where the truth lives

- **`docs/SPEC.md`** is the authoritative build specification. Where a plan file and the SPEC
  disagree, the SPEC wins — fix the plan.
- **`docs/roadmap.md`** is the work-item registry; full plans are in `docs/plan/<ID>.md` and
  completion records in `docs/done/<ID>.md`.
