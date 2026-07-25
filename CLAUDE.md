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

The test runner is Microsoft.Testing.Platform, selected by `global.json` (SPEC §3.1) — there is no
`Microsoft.NET.Test.Sdk` or VSTest in this solution.

> The solution is being built incrementally. Until the owning work item has run, some of these
> commands have nothing to act on — `dotnet build` on a project-less solution succeeds and does
> nothing, and `dotnet test` finds no tests.

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

1. **Zero-warning builds.** `TreatWarningsAsErrors` and `EnforceCodeStyleInBuild` are on solution-wide — a warning is a build failure.
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
