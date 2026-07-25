**Status:** TODO · Single-phase · Suggested build branch `feature/feature-2dde-asset-generator`

# FEATURE-2DDE — Asset generator tool + generated Phosphor resources and enum

## Objective

Build the deterministic asset generator (`tools/Enigma.Icons.Generator`, SPEC §8) **and** run it to
produce the eight committed artifacts it owns: the six `Assets/phosphor.<weight>.dat` resources
(SPEC §7.2) plus `PhosphorIcon.g.cs` and `PhosphorIconNames.g.cs` (SPEC §8.4), all written into
`src/Enigma.Icons.Phosphor/`. Two deliverables, one item: **the tool, and the data it produces**.
Success means the tool builds with zero warnings, its output satisfies every SPEC §7.4 ground-truth
count, and `--check` (SPEC §8.5) passes twice against the committed bytes — i.e. the artwork is
reproducible and `git diff` is a trustworthy review surface for every future icon refresh.

## Context

**This item writes into `src/Enigma.Icons.Phosphor/` before that project has a csproj — that is
intentional, not an ordering mistake.** SPEC §16.2's dependency note on 2DDE → 3950 is explicit: 2DDE
owns *producing* the assets and proving they are reproducible; FEATURE-3950 owns *packaging* them.
Until 3950 adds `Enigma.Icons.Phosphor.csproj`, the `.dat` files and the two `.g.cs` files are
**inert committed data** — nothing compiles them, nothing embeds them, no project in the slnx points
at that directory. Consequently this item appends **only** `tools/Enigma.Icons.Generator` to the
slnx (SPEC §3.4 incremental-growth table). A builder who "helpfully" adds the Phosphor csproj here
is doing 3950's work and breaks the item boundary.

The generator's input is the pinned snapshot `docs/reference/phosphor/phosphor-2.1.1-svgs-flat.tar.gz`
(9,072 flat SVGs, provenance and sha256 in `docs/reference/phosphor/README.md`). It is extracted to a
**temp directory** and read from there; it is never extracted into the working tree.

The committed `.dat` files **are** the Phosphor artwork, so SPEC §14.2's attribution obligation
attaches to the repository at the moment this item lands. `THIRD-PARTY-NOTICES.md` was created by
FEATURE-21C4; this item verifies it is present and carries the verbatim Phosphor MIT text before
committing artwork-derived data.

## Scope

### In scope
- `tools/Enigma.Icons.Generator/` — the complete console tool per SPEC §8 (`--input`, `--output`,
  `--check`).
- Appending `tools/Enigma.Icons.Generator` to the slnx `/tools/` folder (SPEC §3.4).
- Extracting the pinned snapshot to a temp directory and running the tool.
- The eight generated, committed artifacts under `src/Enigma.Icons.Phosphor/`:
  `Assets/phosphor.{thin,light,regular,bold,fill,duotone}.dat`, `PhosphorIcon.g.cs`,
  `PhosphorIconNames.g.cs`.
- Verifying the output against every SPEC §7.4 ground-truth fact and the SPEC §7.3 size budget, and
  proving reproducibility with two `--check` runs.

### Out of scope
- **`src/Enigma.Icons.Phosphor/Enigma.Icons.Phosphor.csproj`**, `PhosphorWeight.cs`,
  `PhosphorIconSet.cs`, the `<EmbeddedResource>` wiring, the `.dat` **reader**, and the package's
  `README.md` — all FEATURE-3950 (SPEC §9, §7.1).
- **Any test project.** `Enigma.Icons.Phosphor.UnitTests` (SPEC §12.2) is FEATURE-3950's, and it is
  the right owner: those tests assert the corpus *through the reading API*, which does not exist yet.
  See "Acceptance criteria" for how DoD criterion 2 is met here instead.
- Adding a `ProjectReference` from the tool to `Enigma.Icons` (see Design step 1) or to any other
  project; the tool is referenced by nothing and references nothing.
- `IsTrimmable`/`IsAotCompatible` (SPEC §10.4 — packages only, not the tool).
- Touching `.gitattributes` (FEATURE-21C4 already added `*.tar.gz binary`, SPEC §3.6), the root docs,
  or `Directory.Packages.props` (the tool takes **no** NuGet packages).
- Any change to `Enigma.Icons` itself.

## Design

Every text file this item creates is LF with a final newline; the build is zero-warning with
`ImplicitUsings` disabled and explicit usings per file, and no `PackageReference` carries `Version=`
(SPEC §2). All generator types are `internal`, so `GenerateDocumentationFile` is not set on the tool
and CS1591 never applies to *its own* code — only to what it emits (step 6).

### 1. `tools/Enigma.Icons.Generator/Enigma.Icons.Generator.csproj`

```xml
<TargetFramework>net10.0</TargetFramework>
<OutputType>Exe</OutputType>
<ImplicitUsings>disable</ImplicitUsings>
<IsPackable>false</IsPackable>
```
Nothing else: `LangVersion 14`, `Nullable enable`, `TreatWarningsAsErrors`, and
`EnforceCodeStyleInBuild` come from `Directory.Build.props` (SPEC §3.2). No `PackageReference` at all.

**Decision: the tool does NOT reference `Enigma.Icons`.** Justification — the generator is a *text
emitter*. It reads `d`/`opacity` strings out of XML and writes bytes; it never constructs an
`IconGlyph`, `IconLayer`, or `IconViewBox`, so the library would buy it exactly nothing while adding
a build coupling: a compile break or an in-flight refactor in `Enigma.Icons` would then block asset
regeneration, and the tool would silently inherit the library's `netstandard2.0`-driven API
constraints. Keeping the tool dependency-free also means the artwork can be regenerated from the
snapshot at any commit, independently of the library's state. The FEATURE-24DD dependency is
therefore **conceptual, not a reference**: the `.dat` format mirrors SPEC §4.2's layer semantics
(paint order, per-layer opacity, verbatim path data) so that 3950's reader can hydrate `IconLayer`
directly. A `ProjectReference` to `Enigma.Icons.Phosphor` is not even possible here — it has no
csproj yet (SPEC §16.2).

Source files, all `internal`, under `tools/Enigma.Icons.Generator/`:

| File | Responsibility |
|---|---|
| `Program.cs` | Entry point; orchestration; exit code; summary output. |
| `CommandLine.cs` | `--input`/`--output`/`--check` parsing + the usage text. |
| `Weights.cs` | The six weights in SPEC §9 enum order with their filename suffixes — the single source of weight order. |
| `SvgPathReader.cs` | `XmlReader`-based extraction of the root `viewBox` and the ordered `(d, opacity)` list from one file. |
| `CorpusLoader.cs` | Enumeration, name derivation, the cross-weight name-set assertion, per-file validation. |
| `DatRenderer.cs` | One `WeightCorpus` → `byte[]` in the SPEC §7.2 format. |
| `EnumRenderer.cs` | `PhosphorIcon.g.cs` → `byte[]`. |
| `NamesRenderer.cs` | `PhosphorIconNames.g.cs` → `byte[]`. |
| `NameMapper.cs` | kebab → Pascal (generator-side); also emits the runtime kebab-normalizer as template text. |
| `OutputTarget.cs` | The single write-or-compare path shared by generate and `--check` mode. |

Record shapes (they pin the contract between loader and renderers):

```csharp
internal sealed record LayerRecord(string PathData, double Opacity);
internal sealed record IconRecord(string Name, IReadOnlyList<LayerRecord> Layers);
internal sealed record WeightCorpus(string Weight, string ViewBox, IReadOnlyList<IconRecord> Icons);
```

### 2. Extracting the pinned snapshot (SPEC §8.1)

Before extracting, verify the archive against the sha256 recorded in
`docs/reference/phosphor/README.md`. Then use the SPEC §8.1 command verbatim:

```bash
mkdir -p /tmp/phosphor-flat
tar -xzf docs/reference/phosphor/phosphor-2.1.1-svgs-flat.tar.gz -C /tmp/phosphor-flat
```

The extraction target is a **temp directory** (`/tmp/phosphor-flat`, or `mktemp -d`) — **never a path
inside the repository**, under any name. `.gitignore` (SPEC §3.5) has no rule for it, so 9,072 stray
`.svg` files in the working tree would land in the next commit. Expect the six weight directories at
the archive root, 1,512 `.svg` each; sanity-check with a file count before running the tool.

### 3. Argument handling (SPEC §8.1, §8.5) — `CommandLine.cs`, `Program.cs`

- `--input <dir>` (required): directory containing the six weight subdirectories.
- `--output <dir>` (required): the `src/Enigma.Icons.Phosphor` directory.
- `--check` (optional flag): modifier on the same invocation — it still needs `--input` and
  `--output`, because it regenerates from input and compares against output.
- **No other arguments.** An unknown argument, a missing value, a repeated flag, or a missing
  required option prints the one-line usage to `stderr` and exits non-zero. `--input` that is not an
  existing directory, or a `--output` that is missing a weight's `Assets` file *in `--check` mode*,
  is a diagnostic, not a crash — no unhandled exception ever reaches the console.
- Exit codes: `0` success; `1` usage error; `2` input/validation failure (missing weight directory,
  name-set mismatch, zero paths, empty `d`, bad `viewBox`, bad `opacity`); `3` `--check` found a
  difference. SPEC §8.1 only requires "0 / non-zero"; the split codes are a refinement that makes
  the build script's failure mode legible.
- Diagnostics go to `stderr`, the SPEC §8.3 step 7 summary to `stdout`. Wording is deterministic and
  contains **no timestamps, no absolute host paths, no machine names** — the summary is quoted in the
  completion doc, and paths would make it non-reproducible.

### 4. The algorithm (SPEC §8.3) — `CorpusLoader.cs`, `SvgPathReader.cs`

Follow SPEC §8.3 steps 1–7. The parts that need pinning:

1. **Enumeration.** For each weight in `Weights.cs` order (thin, light, regular, bold, fill,
   duotone), enumerate `*.svg` in `<input>/<weight>`, sort by file name with `StringComparer.Ordinal`.
   A missing weight directory is a fatal diagnostic. Ordinal everywhere — never a culture-sensitive
   comparer — so the sort, and therefore the `.dat` line order and the enum member ordinals, are
   identical on every machine and every locale.
2. **Name derivation.** Drop `.svg`; then for the five suffixed weights drop a trailing
   `-<weight>` (`acorn-bold` → `acorn`); `regular` carries no suffix (SPEC §7.4.7). A non-`regular`
   file *without* the expected suffix is a fatal diagnostic, not a silently-kept name.
3. **Cross-weight equality assertion (SPEC §8.3 step 3).** Build the six ordinal-sorted name sets and
   compare each against the first. On any difference, abort (exit 2) with a **diff listing**: per
   weight, the names missing from it and the names extra in it, capped at the first 25 of each with a
   `(+N more)` tail. This is the tripwire for SPEC §7.4.6 (1,512 identical names in all six weights)
   and it must fail loudly — a silent intersection would quietly shrink the enum.
4. **Path extraction.** Read each file through `XmlReader` configured as in SPEC §5.2
   (`DtdProcessing.Prohibit`, `XmlResolver = null`, ignore comments/PIs/whitespace). The input is
   pinned and trusted, but the hardened posture costs nothing and keeps one XML idiom in the
   solution. Match elements on **local name** — SPEC §5.2 — so namespace presence is irrelevant.
   Capture, in document order, every `<path>`'s `d` and `opacity`. Read the root `<svg>`'s `viewBox`.
   **No root attribute is required**: SPEC §7.4.4's two files (`duotone/cricket-duotone.svg`,
   `duotone/signature-duotone.svg`) carry no `fill` on `<svg>`, and the generator never looks at
   `fill` at all, so they must produce complete, ordinary lines.
5. **Per-file validation** (all fatal, exit 2, message naming the file):
   zero `<path>` elements; a `d` that is missing, empty, or whitespace; a `viewBox` that is not
   exactly four invariant-parseable numbers; an `opacity` that does not parse invariantly or falls
   outside `[0,1]`. Additionally, because SPEC §7.2's line format is TAB-delimited and one line per
   icon: a `d` containing TAB, CR, or LF, or beginning with `@`, is fatal — the generator **never
   rewrites path data to make it fit** (see step 5 and Notes).
6. **`viewBox` per weight.** SPEC §7.2's header carries one view box for the whole weight file. Take
   the value read from the files, require every file in the weight to agree, and emit that — do not
   hard-code `0 0 256 256`, and do not pick a winner if they disagree (fatal). SPEC §7.4.3 guarantees
   agreement today; the assertion is what makes a future violation visible.
7. **Path data is copied VERBATIM.** No re-flow, no re-rounding, no whitespace normalization, no
   re-ordering of commands — SPEC §8.3's closing rule. The `d` string that goes into the `.dat` file
   is byte-identical to the `d` attribute value upstream, so any glyph can be traced back to its
   source file and a refresh diff shows only genuinely changed artwork.
8. **Unexpected vocabulary.** SPEC §8.3 step 4 says to *ignore* elements and attributes other than
   `<path>`/`d`/`opacity`, and the generator does. But it **counts** them and prints a warning block
   listing each unexpected element or `<path>` attribute name with its occurrence count (exit code
   still 0). Under SPEC §7.4.1–2 that block must be empty today; if it is ever non-empty, artwork
   information is being dropped on the floor and someone must decide what to do (see OPEN QUESTION 1).

### 5. Emitting the six `.dat` files (SPEC §7.2) — `DatRenderer.cs`, `OutputTarget.cs`

`DatRenderer` produces a `byte[]` — it does not touch the filesystem. Content:

- **Header line:** `v1<TAB><weight><TAB><viewBox><TAB><count>`, e.g.
  `v1<TAB>regular<TAB>0 0 256 256<TAB>1512`. `count` is the number of icon lines that follow, written
  with `CultureInfo.InvariantCulture` and no thousands separator (it is the machine-readable tripwire
  SPEC §7.2 describes).
- **Icon lines:** `name` then one TAB-separated field per layer in paint order (index 0 =
  bottom-most). Lines sorted by `name` with `StringComparer.Ordinal` (SPEC §7.2).
- **Layer field:** the verbatim path data, prefixed with `@<opacity>:` **only** when opacity ≠ 1.
  The number is formatted with `CultureInfo.InvariantCulture` in shortest round-trippable form —
  on `net10.0` that is plain `double.ToString(CultureInfo.InvariantCulture)`, so upstream's
  `opacity="0.2"` renders as `@0.2:`. Parse-then-format (rather than copying the attribute text) is
  what SPEC §7.2 mandates, and it normalizes a hypothetical `0.20`; today it is a no-op on all
  1,510 duotone values. There is no `netstandard2.0` shortest-round-trip caveat here: the *generator*
  is `net10.0`; only 3950's *reader* is multi-targeted, and parsing `0.2` is version-independent.
- **Encoding:** UTF-8 with **no BOM** (`new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)`),
  `\n` line separators only (never `Environment.NewLine`), and a final newline after the last icon
  line (SPEC §7.2, SPEC §2).

`OutputTarget` is the **single** write-or-compare path: every artifact is rendered to `byte[]` first,
then either written with `File.WriteAllBytes` (creating `<output>/Assets` if needed) or compared
against the file on disk in `--check` mode. One renderer, two consumers — that is what makes
`--check` a real proof rather than a parallel reimplementation that can drift. In `--check` mode
nothing is created, not even the `Assets` directory.

### 6. Emitting `PhosphorIcon.g.cs` and `PhosphorIconNames.g.cs` (SPEC §8.4)

Both files: `// <auto-generated/>` first line, then a short provenance comment (produced by
`tools/Enigma.Icons.Generator`; artwork © Phosphor Icons, MIT — see `THIRD-PARTY-NOTICES.md`; "do not
edit by hand, run the generator"), then `#nullable enable`, then a file-scoped
`namespace Enigma.Icons.Phosphor;`. The provenance comment carries **no date, no version, no host
path** — anything that changes between runs would break byte-determinism, and the snapshot version
already lives in `docs/reference/phosphor/README.md`. Explicit `using` directives only where needed
(`PhosphorIcon.g.cs` needs none; `PhosphorIconNames.g.cs` needs `System` and
`System.Collections.Generic`) — SPEC §2.2.

**`PhosphorIcon.g.cs`.** One `public enum PhosphorIcon` with 1,512 members in `.dat` order (ordinal
by kebab name), **no explicit numeric values** — SPEC §8.4 forbids `= N` initializers, and the
implicit contiguous 0..1511 numbering is what lets `PhosphorIconNames.ToKebabCase` index an array by
`(int)icon`. That numbering is **positional within a build, not a stable ABI across artwork
refreshes** (SPEC §8.4), so the type-level summary carries both the SPEC §8.4 text
(`The Phosphor icon set. 1,512 icons, 6 weights.`) — with the count formatted `"N0"` invariant from
the actual data rather than hard-coded — **and** the ordinal-instability caveat as a `<remarks>`:
numeric values shift when the artwork is refreshed, so persist and transmit
`PhosphorIconNames.ToKebabCase(icon)` and read it back with `TryParse`, never the numeric value.
**Every member carries a one-line `/// <summary>The "<kebab-name>" icon.</summary>`.** This is not
cosmetic: SPEC §2.8 —
`GenerateDocumentationFile=true` plus `TreatWarningsAsErrors=true` in FEATURE-3950 turns CS1591 into
an **error**, so an undocumented enum would fail the build with **1,512 errors** the moment 3950's
csproj appears. The summary doubles as the IDE tooltip that shows the upstream kebab name.

**kebab ↔ Pascal mapping — mechanical, no exception list** (SPEC §8.4). kebab → Pascal: split on `-`,
upper-case each segment's first character, concatenate. Pascal → kebab: insert `-` before every
upper-case character except the first, then lower-case. All 1,512 upstream names match `^[a-z-]+$`
(no digits, no leading numeral), which is why this is lossless. Do **not** add a special-case table,
and do not "fix" acronyms.

**`PhosphorIconNames.g.cs`.** Public surface exactly as SPEC §8.4 specifies (`ToKebabCase`,
`TryParse`, `All`) with an XML summary on the class and on each member. The file is a fixed template
plus one generated data block:

- `private static readonly string[] Names = [ … ];` — 1,512 entries, **one per line**, in enum order,
  so a refresh diff is line-per-icon. Declared **first**, because static field initializers run in
  declaration order and the other two fields consume it.
- `private static readonly ReadOnlyCollection<string> AllNames = Array.AsReadOnly(Names);` backing
  `All` — a bare `string[]` would satisfy `IReadOnlyList<string>` but hand callers a mutable array.
- `private static readonly Dictionary<string, PhosphorIcon> Lookup` — built once from `Names` with
  capacity 1,512 and `StringComparer.OrdinalIgnoreCase`. Never mutated after initialization, so
  concurrent `TryParse` is safe (SPEC §15) — which is what lets 3950's concurrency test pass.
- `ToKebabCase(PhosphorIcon icon)`: bounds-check `(uint)(int)icon` against `Names.Length` and throw
  `ArgumentOutOfRangeException` (SPEC §8.4), else index `Names`. **No `Enum.ToString()`, no
  `Enum.IsDefined`, no reflection** — SPEC §2.11 and §10.4's trim/AOT cleanliness depend on it.
- `TryParse(string name, out PhosphorIcon icon)`: `null`/empty → `false` (never throws). Fast path:
  probe `Lookup` with the raw input first, which hits for every kebab and every case variant with no
  allocation. Miss → normalize in one pass (`_` → `-`; before an upper-case char at index > 0 whose
  predecessor is not `-`, insert `-`), then probe again. The `OrdinalIgnoreCase` comparer covers the
  casing, so the normalizer only has to insert separators — that is what makes `AddressBookTabs`,
  `address_book_tabs`, and `ADDRESS-BOOK-TABS` all resolve.

### 7. `--check` mode and the reproducibility proof (SPEC §8.5)

`--check` re-renders all eight artifacts in memory and byte-compares them with what is on disk,
writing nothing and exiting `3` on any difference, with a per-file report (missing / N bytes vs M
bytes / first differing offset). This item uses it as its own proof, in three runs after the
generate pass:

1. `--check` against the just-written files → exit 0 (renderer/writer agreement).
2. `--check` again → exit 0 (nothing drifted; the run has no hidden state).
3. Re-extract the archive to a **second, differently-named** temp directory and `--check` from there
   → exit 0. This proves the output contains no leaked input path and no enumeration-order
   dependence on the filesystem.

Record all three exit codes in the completion doc. From 3950 onward, `--check` is the cheap "are the
committed assets stale?" gate.

### 8. slnx

Append exactly one entry, to the `/tools/` folder, per SPEC §3.4's incremental-growth table:

```xml
<Project Path="tools/Enigma.Icons.Generator/Enigma.Icons.Generator.csproj" />
```

Nothing else. `src/Enigma.Icons.Phosphor` and `tests/Enigma.Icons.Phosphor.UnitTests` are
FEATURE-3950's rows; adding them now would reference csproj files that do not exist and break
`dotnet build`.

### 9. Verifying the generated C# compiles, without adding a project to the repo

Because no csproj compiles the two `.g.cs` files until FEATURE-3950, a syntax slip or a missing
`<summary>` would lie dormant for a whole item. Guard against it with a throwaway probe **outside the
repository**: in a temp directory, a minimal `net10.0` classlib csproj that `<Compile Include=…>`s
the two generated files by absolute path and sets `LangVersion 14`, `Nullable enable`,
`ImplicitUsings disable`, `GenerateDocumentationFile true`, `TreatWarningsAsErrors true` (it is
outside the repo, so `Directory.Build.props` does not reach it and the properties must be restated).
`dotnet build` it, confirm zero warnings — which is the CS1591 proof across all 1,512 members — then
delete the temp directory. Nothing is added to the working tree or the slnx.

### 10. Wrap-up

Confirm `THIRD-PARTY-NOTICES.md` is present and carries the verbatim Phosphor MIT text (SPEC §14.2)
now that artwork-derived data is committed. Review `git status`: the only additions are
`tools/Enigma.Icons.Generator/*`, `src/Enigma.Icons.Phosphor/{PhosphorIcon.g.cs,PhosphorIconNames.g.cs,Assets/*.dat}`,
the slnx edit, and the docs updates. Never commit (SPEC §2.9) — stage and print the suggested
message.

## Dependencies & ordering

- **Depends on FEATURE-21C4** — repo on `main`, slnx with the `/tools/` folder, `Directory.Build.props`,
  `.gitattributes`/`.editorconfig` (so the generated files are LF from birth), `THIRD-PARTY-NOTICES.md`.
- **Depends on FEATURE-24DD** — *conceptually only* (roadmap sequencing item 3, SPEC §16): the
  `.dat` layer semantics mirror SPEC §4.2's `IconLayer`. There is deliberately **no build reference**
  (Design step 1), so the tool compiles even if `Enigma.Icons` is mid-refactor.
- **Blocks FEATURE-3950**, which adds `Enigma.Icons.Phosphor.csproj` (embedding these `.dat` files
  and compiling these `.g.cs` files), the reader, and the SPEC §12.2 full-corpus tests — and thereby
  blocks 3ADD, 469B, 718F, and 74DC.
- **slnx delta:** `tools/Enigma.Icons.Generator` only (SPEC §3.4).
- Branch `feature/feature-2dde-asset-generator`, cut from `HEAD` at `/build` time.

## Acceptance criteria

**No test project exists in this item** — the corpus tests are FEATURE-3950's (SPEC §12.2), and
correctly so: they assert the data *through* `PhosphorIconSet`, which does not exist yet. DoD
criterion 2 is therefore satisfied here by **(a)** the tool building clean, **(b)** `--check` passing
on the tool's own committed output three times including from a fresh extraction, **(c)** the direct
ground-truth assertions below, run against the committed bytes, and **(d)** the temp-project compile
probe of the generated C# (Design step 9).

- [ ] `tools/Enigma.Icons.Generator/Enigma.Icons.Generator.csproj` exists: `net10.0`, `OutputType Exe`,
      `IsPackable false`, `ImplicitUsings disable`, **no** `PackageReference`, **no** `ProjectReference`.
- [ ] Slnx contains exactly one new entry — `tools/Enigma.Icons.Generator` under `/tools/` — and no
      `src/Enigma.Icons.Phosphor` or `tests/Enigma.Icons.Phosphor.UnitTests` entry (SPEC §3.4).
- [ ] **`dotnet build Enigma.Icons.slnx` succeeds with zero warnings** (`TreatWarningsAsErrors`,
      SPEC §2.1); capture the output as build evidence.
- [ ] Archive sha256 verified against `docs/reference/phosphor/README.md` before extraction;
      extraction target is a temp directory and `git status` shows **no** `.svg` files anywhere in
      the working tree.
- [ ] Six files `src/Enigma.Icons.Phosphor/Assets/phosphor.{thin,light,regular,bold,fill,duotone}.dat`
      exist, each: UTF-8 **without BOM** (first bytes are not `EF BB BF`), LF only (no `0D` bytes),
      final newline, 1,513 lines (header + 1,512 icons).
- [ ] Every header is `v1<TAB><weight><TAB>0 0 256 256<TAB>1512` with the correct weight name, and
      the declared count equals the actual icon-line count (SPEC §7.2).
- [ ] **9,072 pairs:** each `.dat` holds 1,512 icon lines; the six name columns are **byte-identical
      across all six weights** (e.g. `cut -f1` on each, all six diffs empty) — SPEC §7.4.6. Names are
      ordinal-sorted, unique within a weight, and all match `^[a-z-]+$`.
- [ ] **thin, light, regular, bold:** every icon line has exactly 2 fields (name + 1 layer) and no
      `@` opacity prefix anywhere in those four files (SPEC §7.4.8).
- [ ] **fill outliers, exact:** exactly these 8 icons have more than one layer field, with these
      counts — `bookmarks-simple` 2, `crane-tower` 2, `hard-drives` 2, `lasso` 2, `music-notes-minus`
      3, `speaker-simple-x` 2, `stack` 3, `stack-simple` 2 — every other fill icon has exactly 1, and
      no fill layer carries an `@` prefix (SPEC §7.4.8).
- [ ] **duotone outliers, exact:** 1,510 icons have exactly 2 layers with the first prefixed `@0.2:`;
      exactly `cell-signal-none` and `wifi-none` have 1 layer with **no** prefix (SPEC §7.4.8).
- [ ] Total layer fields across the six files = **10,592** (SPEC §7.4.9).
- [ ] The two SPEC §7.4.4 files lacking the root `fill` attribute did **not** break generation:
      `cricket` and `signature` are present in `phosphor.duotone.dat` with 2 layers each.
- [ ] Every source file's `viewBox` was asserted to be `0 0 256 256` (SPEC §7.4.3) — the generator
      read it rather than hard-coding it, and all six headers carry it.
- [ ] Path data is verbatim: spot-check ≥ 3 icons across ≥ 3 weights (including one duotone and one
      multi-layer fill) by comparing each `.dat` field byte-for-byte with the `d` attribute of the
      corresponding extracted `.svg`.
- [ ] Per-weight **whole-file** byte sizes match SPEC §7.3's measured targets within **±2 %** — thin
      678,696 B, light 678,649 B, regular 632,699 B, bold 629,731 B, fill 553,221 B, duotone
      778,298 B, total **3,951,294 B (3.77 MiB)**. These are whole-file figures (header line, icon
      names, TABs, `@0.2:` prefixes and newlines all included) — **not** the §7.3 path-data
      subtotals, which are ~3–4 % smaller and are not the target. A deviation beyond ±2 % on any
      weight means the format or the data is wrong — investigate, do not accept.
- [ ] `git check-attr text eol -- src/Enigma.Icons.Phosphor/Assets/phosphor.thin.dat` shows the file
      as **text** with `eol=lf`, so future artwork refreshes diff line-by-line (SPEC §3.6).
- [ ] `PhosphorIcon.g.cs`: `// <auto-generated/>` first line, `#nullable enable`, no `using`
      directives, `namespace Enigma.Icons.Phosphor;`, exactly **1,512** members in `.dat` order,
      PascalCase, **no explicit numeric values** (no `= N` anywhere — SPEC §8.4), and **exactly 1,512
      member-level `<summary>` lines** — one per member (SPEC §8.4, §2.8).
- [ ] The `PhosphorIcon` type-level doc carries the SPEC §8.4 ordinal-instability caveat — numeric
      values are positional and shift on an artwork refresh; persist `PhosphorIconNames.ToKebabCase`
      and read back with `TryParse`.
- [ ] `PhosphorIconNames.g.cs`: the SPEC §8.4 public surface (`ToKebabCase`, `TryParse`, `All`) with
      XML docs on the class and all three members; `Names` holds 1,512 entries one per line in enum
      order; the lookup dictionary uses `StringComparer.OrdinalIgnoreCase`; **no `Enum.ToString`,
      `Enum.Parse`, `Enum.IsDefined`, or reflection appears in either generated file** (SPEC §2.11).
- [ ] **Generated C# compiles clean:** the temp-directory probe project (Design step 9) builds both
      `.g.cs` files with `GenerateDocumentationFile=true` and `TreatWarningsAsErrors=true` at zero
      warnings — i.e. CS1591 is satisfied on all 1,512 members — and the temp directory is deleted
      afterwards. Nothing was added to the repo or the slnx.
- [ ] **Reproducibility:** `--check` exits 0 (a) immediately after generation, (b) on an immediate
      second run, and (c) after re-extracting the archive into a differently-named temp directory.
      All three exit codes recorded.
- [ ] `--check` is proven non-destructive: run it against a deliberately absent/modified copy of one
      `.dat` **outside** the repo (or on a scratch copy of the output tree) and confirm it exits
      non-zero, reports the offending file, and writes nothing; the repo copy is untouched.
- [ ] Usage/diagnostic behaviour: missing `--input`, missing `--output`, an unknown argument, and a
      non-existent `--input` directory each exit non-zero with a readable message on `stderr` and no
      unhandled exception.
- [ ] `src/Enigma.Icons.Phosphor/` contains **only** the 2 `.g.cs` files and `Assets/` with the 6
      `.dat` files — **no csproj** (FEATURE-3950 owns it, SPEC §16.2).
- [ ] `THIRD-PARTY-NOTICES.md` present with the verbatim Phosphor MIT text (SPEC §14.2), now that
      the artwork is committed.
- [ ] Every generated and hand-written text file is LF with a final newline (SPEC §2.7).
- [ ] **Roadmap + this plan flipped to `DONE`** (`docs/roadmap.md` FEATURE-2DDE row; this file's
      status header). (DoD criterion 4.)
- [ ] **Completion doc `docs/done/FEATURE-2DDE.md` written** — summary, files touched, deviations,
      build/`--check` evidence including the generator's stdout summary and the three `--check` exit
      codes. (DoD criterion 5.)

## Notes / risks

- **OPEN QUESTION 1 — unexpected `<path>` attributes: warn or abort?** SPEC §8.3 step 4 says to
  *ignore* elements and attributes other than `<path>`/`d`/`opacity`, and SPEC §7.4.1–2 assert none
  exist. But the v1 `.dat` format can carry only path data and opacity, so a future upstream refresh
  that introduced `fill-rule="evenodd"` or a `stroke` on a `<path>` would be **silently dropped** —
  the artwork would render wrongly with no failure anywhere in the pipeline. This plan follows the
  SPEC (ignore) and adds a non-fatal warning block with per-name counts (Design step 4.8). Ask the
  user whether that should instead be **fatal**; if so, SPEC §8.3 step 4 needs a wording change, and
  a `.dat` v2 with layer attributes becomes a real question.
- **OPEN QUESTION 2 — one view box per weight.** SPEC §7.2's header carries a single `viewBox` for a
  whole weight file, so the format cannot represent a weight whose icons have differing view boxes.
  SPEC §7.4.3 makes that unreachable for Phosphor 2.1.1, and this plan aborts rather than choosing a
  winner. Worth confirming the intent, because it is the format constraint that would block reusing
  `.dat` v1 for a non-uniform icon family (SPEC §17's "additional icon families").
- **Enum ordinals are positional, not a stable ABI — SETTLED by SPEC §8.4.** Members are numbered by
  ordinal position in the kebab name list, so upstream adding a single icon named `aardvark` renumbers
  every value after it. SPEC §8.4 now states this and its consequences, so this item simply obeys
  them: emit **implicit** values in name order (do **not** add explicit `= N` initializers — §8.4
  forbids them), and emit the ordinal-instability caveat into the enum's own XML doc summary
  (Design step 6), telling callers to persist `PhosphorIconNames.ToKebabCase` and read back with
  `TryParse` rather than the numeric value. The same caveat in the `Enigma.Icons.Phosphor` README is
  **FEATURE-718F's** obligation and the "an artwork refresh is at least a MINOR version bump" rule is
  **FEATURE-74DC's** — both mandated by SPEC §8.4, so the delegation is valid and needs no further
  decision here.
- **Writing into a project that does not exist yet is by design** (SPEC §16.2). Do not "fix" it by
  adding the csproj; that is FEATURE-3950's first step.
- **Never extract into the working tree.** 9,072 untracked `.svg` files are not covered by
  `.gitignore` and would be swept into a commit. Temp directory only.
- **Determinism is the whole point.** No timestamps, no absolute paths, no host names, no culture-
  sensitive formatting or comparison, no `Directory.EnumerateFiles` order dependence (always
  re-sorted `Ordinal`), no hash-set iteration order in any emitted output. If `git diff` after a
  regeneration ever shows a change that no icon caused, that rule was broken somewhere.
- **The TAB/newline/`@` guard on `d`** (Design step 4.5) is this plan's addition, not the SPEC's: the
  v1 line format cannot represent such a value, and the alternative — rewriting the path data — would
  violate SPEC §8.3's verbatim rule. Aborting with the file name is the only safe option. Today no
  upstream `d` contains any of them.
- **CS1591 is the single biggest generated-code risk.** 1,512 members with `GenerateDocumentationFile`
  on means 1,512 build **errors** in FEATURE-3950 if the per-member `<summary>` emission has a gap
  (e.g. an off-by-one in a loop that skips the last member). Design step 9's temp compile probe exists
  precisely to catch that here, one item early, rather than during 3950.
- **`--check` must never be a second implementation.** If someone re-derives the expected bytes in
  check mode instead of reusing `DatRenderer`/`EnumRenderer`/`NamesRenderer` through `OutputTarget`,
  the two paths will drift and `--check` becomes theatre. One renderer, two consumers.
- **The 8 fill and 2 duotone outliers are data, not code.** The generator must be driven by what it
  reads (SPEC §7.4's closing note); the exact counts appear in the acceptance criteria as *checks on
  the output*, never as hard-coded expectations inside the tool.
