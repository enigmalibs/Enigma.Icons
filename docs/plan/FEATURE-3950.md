**Status:** DONE · Single-phase · Suggested build branch `feature/feature-3950-phosphor-pack`

# FEATURE-3950 — Enigma.Icons.Phosphor package + UnitTests

## Objective

Turn the inert, already-committed generator output in `src/Enigma.Icons.Phosphor/` (six
`Assets/phosphor.*.dat` files, `PhosphorIcon.g.cs`, `PhosphorIconNames.g.cs`) into the shipping
`Enigma.Icons.Phosphor` package: a packable, zero-dependency, `netstandard2.0;net8.0;net10.0`
library that embeds those resources and serves them through `PhosphorIconSet` on both the
strongly-typed and the `IIconSet` string-keyed surface (SPEC §9), plus the full-corpus integrity
test project (SPEC §12.2) that proves the artwork and the generated code agree across all
9,072 `(icon, weight)` pairs.

Two things ship here that no other item can ship: the **licence obligation** (the `.dat` files *are*
the Phosphor artwork, so `THIRD-PARTY-NOTICES.md` is packed by this package and only this package —
SPEC §14.2), and the **tripwires** that make a bad regeneration or an upstream shape change fail
loudly instead of silently.

## Context

Per SPEC §16, FEATURE-2DDE already wrote the six `.dat` files and the two `.g.cs` files into
`src/Enigma.Icons.Phosphor/` and proved they are reproducible via `--check` (SPEC §8.5) — but that
directory has **no csproj yet** and is **not** in the slnx. 2DDE owns *producing* the assets; this
item owns *packaging and serving* them.

The asset format is fully specified (SPEC §7.2) and so is what the reader must find in it
(SPEC §7.4 — the measured ground truth over all 9,072 upstream files). The runtime path is
deliberately XML-free: the resource holds path data, not SVG, so `SvgIconParser` (SPEC §5) is not on
this code path at all (SPEC §9.1).

The layer model this package materializes (`IconViewBox`, `IconLayer`, `IconGlyph`) and the
abstraction it implements (`IIconSet`) come from `Enigma.Icons`, built in FEATURE-24DD (SPEC §4,
§6.1).

## Scope

### In scope
- `src/Enigma.Icons.Phosphor/Enigma.Icons.Phosphor.csproj` — packable, three TFMs, zero
  `PackageReference`, embeds the six `.dat` resources, packs `README.md` + `LICENSE.md` +
  `THIRD-PARTY-NOTICES.md`.
- `src/Enigma.Icons.Phosphor/PhosphorWeight.cs` — the six-member enum (SPEC §9).
- `src/Enigma.Icons.Phosphor/PhosphorIconSet.cs` — both surfaces, the two-level cache (SPEC §9.1),
  the `.dat` reader, the glyph materializer.
- `src/Enigma.Icons.Phosphor/README.md` — packed; the Phosphor credit line and the refresh pointer.
- `tests/Enigma.Icons.Phosphor.UnitTests/` — csproj + every bullet of SPEC §12.2.
- Appending this item's two `<Project>` entries to `Enigma.Icons.slnx` (SPEC §3.4).

### Out of scope (owned elsewhere)
- **Regenerating anything.** `PhosphorIcon.g.cs`, `PhosphorIconNames.g.cs`, and the `.dat` files are
  FEATURE-2DDE's output, compiled/embedded here **as-is**. No hand edits, ever (Design step 3).
- **Any Avalonia type** — `Geometry`/`Drawing` conversion, markup extensions, the `Icon` control:
  FEATURE-3ADD (SPEC §10).
- **`SvgIconParser` / `SvgIconSet` changes** — FEATURE-24DD owns the base library; this item must not
  need to touch it. If it does, that is a finding to report, not a silent edit.
- **`PackageReleaseNotes`** and `RELEASENOTES.md` bodies — FEATURE-74DC (SPEC §13, §16).
- **The polished packed README** — FEATURE-718F owns the final text (SPEC §13, §13.2); this item
  writes the first-cut file the csproj packs.
- **Publishing / tagging / `dotnet nuget push`** — FEATURE-74DC prints a runbook; nothing is pushed.

## Design

Global rules (LF + final newline, zero-warning build with `TreatWarningsAsErrors`,
`ImplicitUsings disable` with explicit per-file `using`s, `Nullable enable`, no `Version=` on any
`PackageReference` under CPM) apply to every file below — see SPEC §2 and §3.3.

### 1. `src/Enigma.Icons.Phosphor/Enigma.Icons.Phosphor.csproj`

Same house shape as the base library's csproj (FEATURE-24DD), with these specifics:

- `<OutputType>Library</OutputType>`, `<TargetFrameworks>netstandard2.0;net8.0;net10.0</TargetFrameworks>`,
  `<ImplicitUsings>disable</ImplicitUsings>`.
- Package metadata: `PackageId`/`Title` `Enigma.Icons.Phosphor`, `Version 1.0.0`, a one-line
  `Description` matching the SPEC §0 table row ("The Phosphor artwork as an interchangeable asset
  pack: 6 embedded resources, the 1,512-member `PhosphorIcon` enum, `PhosphorWeight`,
  `PhosphorIconSet`."), `PackageTags`, `PackageReadmeFile README.md`,
  `PackageLicenseFile LICENSE.md`, `RepositoryUrl`/`PackageProjectUrl`
  `https://github.com/josueclement/Enigma.Icons`, `RepositoryType git`, `IncludeSymbols true`,
  `SymbolPackageFormat snupkg`, `GenerateDocumentationFile true`. **No `PackageReleaseNotes`** — 74DC
  adds it (SPEC §16).
- **Do not set `RootNamespace`.** The default (= project name) is what makes the manifest resource
  names come out as SPEC §7.1 requires; changing it silently breaks every lookup (see step 4 and
  Notes / risks).
- **Zero `<PackageReference>`.** This is a hard constraint (SPEC §2.10), not a default — no polyfill,
  no `System.Memory`, no `Microsoft.Bcl.*` (SPEC §3.3). One `ProjectReference`:
  `..\Enigma.Icons\Enigma.Icons.csproj`.
- Embedded assets, exactly as SPEC §7.1 states:
  `<ItemGroup><EmbeddedResource Include="Assets\phosphor.*.dat" /></ItemGroup>`.
- Trim/AOT per SPEC §10.4 — `IsTrimmable` + `IsAotCompatible` in an ItemGroup-free PropertyGroup
  conditioned `'$(TargetFramework)' != 'netstandard2.0'`.
- Packed files:
  ```xml
  <None Include="README.md" Pack="true" PackagePath="\" />
  <None Include="..\..\LICENSE.md" Pack="true" PackagePath="\" />
  <None Include="..\..\THIRD-PARTY-NOTICES.md" Pack="true" PackagePath="\" />
  ```
  Add a short csproj comment on the third line stating **why**: the `.dat` resources *are* the
  Phosphor artwork, so Phosphor's MIT copyright + permission notice must travel with the package —
  a licence obligation, and the reason this file is packed **here and nowhere else** (SPEC §14.2).
  `THIRD-PARTY-NOTICES.md` already exists at the repo root (written by FEATURE-21C4, SPEC §13).

### 2. `src/Enigma.Icons.Phosphor/PhosphorWeight.cs`

`namespace Enigma.Icons.Phosphor;` · the six members in the SPEC §9 order —
`Thin, Light, Regular, Bold, Fill, Duotone` — with a one-line `<summary>` on the enum and on **every**
member (CS1591 is an error, SPEC §2.8). No explicit numeric values.

State in a code comment that the **declaration order is load-bearing**: it indexes the weight-name
table of step 4 and defines the order of `IIconSet.Variants` (SPEC §9). Reordering is a breaking
change to the resource lookup, not a cosmetic edit.

### 3. Wiring the generated files (no new files)

`PhosphorIcon.g.cs` and `PhosphorIconNames.g.cs` sit in the project directory and are picked up by
the SDK's default `**/*.cs` glob — **no explicit `<Compile>` item, no `<Compile Remove>`**. They are
compiled exactly as committed.

- **Never hand-edit them.** A needed change means editing the generator and re-running it
  (SPEC §8.1), then reviewing the `git diff`.
- Pre-flight before writing any code: confirm the six `.dat` files and both `.g.cs` files are present,
  that their namespace is `Enigma.Icons.Phosphor`, and that
  `dotnet run --project tools/Enigma.Icons.Generator -- --input <extracted> --output src/Enigma.Icons.Phosphor --check`
  exits 0 (SPEC §8.5). A stale asset caught here is five minutes; caught in the corpus tests it is a
  confusing red suite.
- These files carry `// <auto-generated/>`, which suppresses analyzer/style diagnostics but **not**
  CS1591 — the generator already emits a `<summary>` per member (SPEC §8.4), so the zero-warning
  build across 1,512 members is expected to pass unchanged.

### 4. `src/Enigma.Icons.Phosphor/PhosphorIconSet.cs`

The public surface is fixed by SPEC §9 — implement it exactly as written there, including
`PhosphorWeight.Regular` as the default parameter value and `GetGlyph(string icon, string? variant = null)`.
Because `netstandard2.0` is in the TFM set, **both** `IIconSet` members are implemented explicitly on
the class; no default interface members (SPEC §6.1).

**4a. Static lookup data — literals only, no reflection.**

```csharp
private const string ResourcePrefix = "Enigma.Icons.Phosphor.Assets.phosphor.";
private const string ResourceSuffix = ".dat";
private const string ExpectedVersion = "v1";
private const string ExpectedViewBox = "0 0 256 256";
private static readonly string[] WeightNames = { "thin", "light", "regular", "bold", "fill", "duotone" };
```

- Resource name = `ResourcePrefix + WeightNames[(int)weight] + ResourceSuffix` — a compile-time
  literal plus a table entry, never `Enum.GetName`/`ToString`/`Enum.Parse` (SPEC §7.1, §2.11,
  §10.4). Trimming cannot break a literal.
- `Variants` returns a cached `ReadOnlyCollection<string>` wrapping `WeightNames` — never the array
  itself (an array handed out as `IReadOnlyList<string>` is castable back to a mutable `string[]`).
- Variant string → weight uses a `Dictionary<string, PhosphorWeight>(StringComparer.OrdinalIgnoreCase)`
  built once from `WeightNames`; an unrecognized variant returns *false* from the lookup — it is
  never coerced.
- Weight validation on the typed surface: a `PhosphorWeight` outside `0..5` (a cast integer) throws
  `ArgumentOutOfRangeException` before any resource work.
- `Instance`: private constructor + `private static readonly PhosphorIconSet Singleton = new();`
  exposed through the get-only property. The constructor does **no I/O** — resource reads are
  per-weight and lazy (SPEC §9.1), so touching `Instance` costs nothing.
- `IconNames => PhosphorIconNames.All;` — 1,512 names, already in enum order which *is* ordinal name
  order (SPEC §7.2 sorting + §8.4). Note the consequence: `IconNames` is complete **without reading
  any resource**.

**4b. Name normalization — reuse the generated tables, do not write a second normalizer.**

`Enigma.Icons`' internal `IconNameNormalizer` is not visible across the package boundary, and it must
not be made visible. The string-keyed surface instead canonicalizes through the generated code
(SPEC §8.4), which already accepts kebab / `snake_case` / PascalCase case-insensitively:

```
string key → PhosphorIconNames.TryParse(key, out PhosphorIcon icon)   // false ⇒ miss
           → PhosphorIconNames.ToKebabCase(icon)                      // canonical .dat key
```

This satisfies the SPEC §6.1 implementer contract with zero duplicated logic, and it means the
typed and string surfaces provably resolve the same rows (asserted in step 6).

**4c. Two-level cache (SPEC §9.1).**

Instance fields, both never evicted:

1. `ConcurrentDictionary<PhosphorWeight, Dictionary<string, string>>` — the weight index. Value maps
   canonical icon name → the **raw remainder of its line** (everything after the first TAB, i.e. the
   TAB-joined layer fields). Inner dictionary is `StringComparer.Ordinal` (keys are already
   canonical); built once per weight by `GetOrAdd(weight, LoadWeight)`.
2. `ConcurrentDictionary<(PhosphorWeight Weight, string Name), IconGlyph>` — the glyph cache, filled
   by `GetOrAdd`, so repeated lookups return a **reference-equal** `IconGlyph` (SPEC §6.1).

A benign duplicate table build or glyph build under a race is acceptable (both results are equal);
what must hold is that the published value is a single consistent instance — which `GetOrAdd` gives.
`ValueTuple` is available on `netstandard2.0` without any package; if that turns out false, use a
composite `string` key rather than adding a dependency (SPEC §3.3).

**4d. The `.dat` reader — `private static Dictionary<string, string> LoadWeight(PhosphorWeight weight)`.**

Single pass, no XML, no `SvgIconParser` (SPEC §9.1 — there is no XML in the resource; the parser
exists for user-supplied SVG and `SvgIconSet`).

1. `typeof(PhosphorIconSet).Assembly.GetManifestResourceStream(name)`; a `null` stream throws
   `InvalidDataException` naming the resource it looked for (this is a packaging defect, and step 6
   asserts the names so it cannot reach a consumer).
2. `new StreamReader(stream, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: false)` —
   the resource is UTF-8 **without** BOM (SPEC §7.2). Read with `ReadLine()`.
3. **Header validation** (line 1, split on `'\t'`) — each failure throws `InvalidDataException` whose
   message names the resource, the field, and expected-vs-actual:
   - exactly 4 fields;
   - field 0 `== "v1"` (ordinal) — unrecognized version;
   - field 1 `== WeightNames[(int)weight]` — a `.dat` embedded under the wrong name;
   - field 2 `== "0 0 256 256"` (ordinal, SPEC §7.4 fact 3) — the glyph view box is then
     `IconViewBox.Default`, which SPEC §4.1 documents as exactly that box, so the reader needs **no
     number parsing at all** for the view box;
   - field 3 parses as a non-negative `int` with `CultureInfo.InvariantCulture` — the declared count.
4. **Body**: for each subsequent line, skip a single trailing empty line (the file ends with a
   newline, SPEC §7.2) and otherwise split at the **first** TAB via `IndexOf('\t')`:
   name = prefix, remainder = suffix. No TAB, empty name, or empty remainder →
   `InvalidDataException`. A duplicate name → `InvalidDataException` (this is the "no duplicate names
   within a weight" guarantee of SPEC §12.2, enforced at load, not just asserted in a test).
5. **Count check**: parsed line count must equal the header count → else `InvalidDataException`. This
   plus the version check is the tripwire for a corrupted or half-written resource (SPEC §7.2). Every
   `InvalidDataException` raised in this reader **propagates out of the calling lookup member**,
   `TryGetGlyph` included — a broken source is not a miss (SPEC §6.1 precedence table).
6. Return the dictionary. Use `IndexOf`/`Substring`/`Split(char)` only — see Notes / risks on
   `netstandard2.0` and `Span<T>`.

**4e. Glyph materialization — `private static IconGlyph BuildGlyph(string remainder)`.**

1. Split `remainder` on `'\t'` → the layer fields **in paint order** (index 0 = bottom-most, SPEC §7.2).
2. Per field: if it starts with `'@'`, the opacity prefix runs to the first `':'` — parse the text
   between with `double.Parse(..., NumberStyles.Float, CultureInfo.InvariantCulture)` (SPEC §7.2 says
   the prefix is invariant-culture); the path data is everything after the `':'`. No `'@'` ⇒ opacity
   `1.0`, whole field is path data. A malformed prefix or an empty path data →
   `InvalidDataException`.
3. `new IconLayer(pathData, opacity)` — every other `IconLayer` argument stays at its default: the
   `.dat` carries no paint information, and the upstream `fill="currentColor"` normalizes to `null`
   (SPEC §4.2, §5.1) so the layer inherits the renderer's brush. `FillRule` stays `NonZero`.
4. `new IconGlyph(IconViewBox.Default, layers)`.

**4f. Lookup core and the no-silent-fallback rule.**

One private core — `private bool TryGetGlyphCore(PhosphorWeight weight, string canonicalName, out IconGlyph? glyph)`
— resolves the weight table, misses if the name is absent, otherwise `GetOrAdd`s the glyph. All four
public lookup members funnel through it, so the typed and string surfaces cannot diverge.

- `variant == null` ⇒ `DefaultVariant` (`"regular"`). A variant the set **does have a slot for but
  the caller spelled differently** is still resolved case-insensitively; a variant the set **does not
  have** (`"heavy"`, `"outline"`, `""`) is a **MISS** — `TryGetGlyph` returns `false`, `GetGlyph`
  throws `IconNotFoundException`. It is **never** silently downgraded to `regular` (SPEC §6.1). A
  caller asking for `bold` must not receive `regular`; state this in the XML docs of both string
  members so the guarantee is visible at the call site.
- `GetGlyph` misses throw `IconNotFoundException(iconName, variant, "Phosphor")` (SPEC §5.3), with
  `variant` reported as the **caller's** string for the string surface and `WeightNames[(int)weight]`
  for the typed surface.
- `TryGetGlyph` never throws **for a miss** — and only for a miss (SPEC §6.1's precedence table,
  which §15 now defers to). A `null` icon name is a miss and returns `false`; `GetGlyph(null, …)`
  fails fast with `ArgumentNullException`. A **broken source is not a miss**: a missing embedded
  resource stream or a `.dat` version/weight/view-box/count mismatch raises `InvalidDataException`
  out of **both** methods and is never swallowed into a `false`. Match whatever null-argument
  behaviour FEATURE-24DD established for `SvgIconSet`; if it differs, follow it and record the choice
  in the completion doc.
- A typed `GetGlyph(PhosphorIcon, PhosphorWeight)` miss is only reachable if the `.dat` and the enum
  disagree — it still throws the documented `IconNotFoundException`; an out-of-range `PhosphorIcon`
  surfaces as `ArgumentOutOfRangeException` from `ToKebabCase` (SPEC §8.4) and is documented as such.
- Thread safety: all mutable state is `ConcurrentDictionary`; the model types are immutable
  (SPEC §15). Document the type as safe for concurrent use from any thread.

### 5. `src/Enigma.Icons.Phosphor/README.md` (packed)

First cut, polished later by FEATURE-718F (SPEC §13). Content:

- Title and a one-paragraph intro. **No badges** — a packed per-package README carries none
  (SPEC §13.1); badges live in the root README only.
- The six weights, `PhosphorIcon` (1,512 members), `PhosphorWeight`, and `PhosphorIconSet.Instance`
  with the C# example from SPEC §10.3 plus a string-keyed `IIconSet` example.
- "Zero dependencies · `netstandard2.0;net8.0;net10.0`" and a pointer to `Enigma.Icons.Avalonia` for
  rendering.
- **The credit line verbatim** (SPEC §14.2):
  `Icon artwork from Phosphor Icons (MIT), © 2020 Phosphor Icons — https://phosphoricons.com`
  followed by a pointer to the packed `THIRD-PARTY-NOTICES.md`.
- **Refresh pointer**, not a copy of the procedure: artwork updates go through
  `docs/reference/phosphor/README.md` ("Refreshing to a newer Phosphor release") and the generator in
  SPEC §8 — the `.dat` files are never edited by hand.

### 6. `tests/Enigma.Icons.Phosphor.UnitTests/`

`Enigma.Icons.Phosphor.UnitTests.csproj`: `net10.0`, `OutputType Exe`, `ImplicitUsings disable`,
`IsPackable false`, one `<PackageReference Include="xunit.v3" />` (no `Version=` — CPM), one
`ProjectReference` to `src/Enigma.Icons.Phosphor` (SPEC §12).

**Why this project is the centrepiece.** Everything downstream trusts 3,951,294 B (3.77 MiB) of
committed generated data that no human reviews line by line. Two failure modes must be impossible to ship: (a) a **bad
generator run** — truncated `.dat`, a weight written under the wrong name, an enum out of step with
the artwork; (b) an **upstream refresh that changes the shape** of the corpus — a new icon, a fill
icon that gained a second path, a duotone icon that lost its tinted layer. Both are invisible to a
compiler and to any spot-check test. The full-corpus assertions catch (a); the SPEC §7.4 layer-shape
assertions catch (b) by encoding the measured ground truth as the *expected* value, so a change
fails loudly and forces a deliberate decision instead of drifting in.

Files, one per SPEC §12.2 bullet group:

1. **`ResourceManifestTests.cs`** — `typeof(PhosphorIconSet).Assembly.GetManifestResourceNames()`
   contains **exactly** the six names listed in SPEC §7.1, and each opens a non-empty stream.
   Manifest names are case-sensitive and produced by an MSBuild glob; this asserts them instead of
   trusting the build (Notes / risks).
2. **`DatFormatTests.cs`** — reads each of the six resources directly and asserts the SPEC §7.2
   header: `v1`, the matching weight name, `0 0 256 256`, `1512`, and that the actual data-line count
   equals the declared count; plus UTF-8-without-BOM and a final newline.
3. **`CorpusIntegrityTests.cs`** — all 1,512 × 6 = 9,072 pairs resolve through the typed surface;
   every glyph has ≥ 1 layer, every layer non-empty path data; per weight the resolved names are
   1,512 distinct values equal to `PhosphorIconNames.All` as a set.
4. **`EnumNameTests.cs`** — `ToKebabCase` → `TryParse` round-trips for all 1,512 members; every name
   matches `^[a-z-]+$`; `PhosphorIconNames.All.Count == 1512` and its order matches enum order;
   `TryParse` accepts kebab, `snake_case`, and PascalCase case-insensitively and rejects garbage
   (`""`, `"not-an-icon"`, `"acorn "`); `ToKebabCase((PhosphorIcon)999999)` throws
   `ArgumentOutOfRangeException`.
5. **`LayerShapeTests.cs`** — the SPEC §7.4 tripwires, with the expectation written as literal data
   in the test file (never derived from the asset being tested):
   - thin / light / regular / bold: **every** icon exactly 1 layer, opacity 1.
   - fill: exactly the 8 named icons have > 1 layer with the stated counts — `bookmarks-simple` 2,
     `crane-tower` 2, `hard-drives` 2, `lasso` 2, `music-notes-minus` 3, `speaker-simple-x` 2,
     `stack` 3, `stack-simple` 2 — every other fill icon exactly 1, and no fill layer has
     opacity < 1.
   - duotone: every icon exactly 2 layers with the first at opacity `0.2`, **except**
     `cell-signal-none` and `wifi-none` — exactly 1 layer at opacity 1.
   - Total layer count across the corpus is 10,592 (SPEC §7.4 fact 9) — one arithmetic assertion that
     catches any drift the per-weight rules miss.
   - Compare opacity with a tolerance (`1e-9`), not `==`.
6. **`PhosphorIconSetTests.cs`** — `Name == "Phosphor"`; `Variants` is the six names in SPEC §9
   order; `DefaultVariant == "regular"`; `IconNames.Count() == 1512` and is ordinal-sorted;
   `GetGlyph(icon)` equals `GetGlyph(icon, PhosphorWeight.Regular)`; typed and string overloads
   return the **same instance** for the same pair; `GetGlyph("acorn")` and `GetGlyph("ACORN")`
   resolve case-insensitively, and `GetGlyph("address_book")` and `GetGlyph("AddressBook")` both
   resolve to `address-book` — snake and Pascal input per SPEC §6.1. Use a multi-word icon for those
   two cases: all 1,512 names match `^[a-z-]+$` with no digits (SPEC §7.4, §8.4), so a digit-bearing
   input is never a valid case and a one-word name like `acorn` has no distinct snake form.
   **A variant the set lacks is a miss, not a fallback**
   (`TryGetGlyph("acorn", "heavy", …)` is `false`; `GetGlyph("acorn", "heavy")` throws and
   the caught `IconNotFoundException` carries `IconName`/`Variant`/`SetName` and the SPEC §5.3
   message shape); an unknown icon name likewise; repeated calls are reference-equal;
   an out-of-range cast `PhosphorWeight` throws `ArgumentOutOfRangeException`;
   `TryGetGlyph` with a `null` name returns `false` without throwing.
7. **`ConcurrencyTests.cs`** — many threads hammering many `(icon, weight)` pairs across all six
   weights (including several threads racing the *first* touch of the same weight) never throw and
   never observe two different `IconGlyph` instances for the same pair (SPEC §15 concurrency row).

**Test shape.** Do **not** write a `[Theory]` with 1,512 (or 9,072) cases — that inflates the run and
buries the signal. Use one `[Fact]` per weight (or per rule) that loops the corpus, collects every
violation, and asserts the collected list is empty, reporting the first ~10 offenders in the failure
message. That keeps the whole corpus covered, the run at a second or two, and a failure readable.

### 7. `Enigma.Icons.slnx`

Per the SPEC §3.4 incremental-growth contract, append **exactly two** entries — nothing else:

```xml
<Project Path="src/Enigma.Icons.Phosphor/Enigma.Icons.Phosphor.csproj" />        <!-- into /src/ -->
<Project Path="tests/Enigma.Icons.Phosphor.UnitTests/Enigma.Icons.Phosphor.UnitTests.csproj" />  <!-- into /tests/ -->
```

The solution then holds 5 projects (`Enigma.Icons`, `Enigma.Icons.UnitTests`,
`Enigma.Icons.Generator`, and the two added here) and must still build and test clean. Do not
pre-add the Avalonia or Gallery entries — their projects do not exist yet.

## Dependencies & ordering

- **Depends on FEATURE-2DDE** (roadmap sequencing 4; SPEC §16): the six `.dat` files,
  `PhosphorIcon.g.cs`, and `PhosphorIconNames.g.cs` must already be committed in
  `src/Enigma.Icons.Phosphor/`. Nothing here regenerates them; step 3's `--check` pre-flight confirms
  they are current.
- **Depends transitively on FEATURE-24DD** for `IconViewBox`/`IconLayer`/`IconGlyph`/`IIconSet`/
  `IconNotFoundException` (SPEC §4, §6.1) and on FEATURE-21C4 for the root config,
  `LICENSE.md`, and `THIRD-PARTY-NOTICES.md`.
- **Blocks FEATURE-3ADD**, which needs the `PhosphorIcon`/`PhosphorWeight`/`PhosphorIconSet` surface
  for its markup extensions and `Icon` control (SPEC §10), and therefore blocks 469B → 718F → 74DC.
- Build branch: `feature/feature-3950-phosphor-pack`, cut from `HEAD` at `/build` time.

## Acceptance criteria

- [x] `src/Enigma.Icons.Phosphor/Enigma.Icons.Phosphor.csproj` exists: TFMs
      `netstandard2.0;net8.0;net10.0`, `ImplicitUsings disable`, `GenerateDocumentationFile true`,
      **zero** `<PackageReference>`, one `ProjectReference` to `Enigma.Icons`, no `RootNamespace`
      override, `IsTrimmable`/`IsAotCompatible` on the two modern TFMs only (SPEC §10.4).
- [x] `<EmbeddedResource Include="Assets\phosphor.*.dat" />` present and the built assembly exposes
      **exactly** the six manifest names of SPEC §7.1, verified by a test (not by inspection).
- [x] The csproj packs `README.md`, `..\..\LICENSE.md`, **and `..\..\THIRD-PARTY-NOTICES.md`**, the
      last with a comment recording the SPEC §14.2 licence obligation.
- [x] `dotnet pack src/Enigma.Icons.Phosphor/Enigma.Icons.Phosphor.csproj -c Release` succeeds and
      the resulting nupkg contains `README.md`, `LICENSE.md`, `THIRD-PARTY-NOTICES.md`, the six
      `.dat` resources (inside the assembly), and a dependency list whose **only** entry is
      `Enigma.Icons` — no third-party dependency on any TFM (SPEC §2.10). Local verification only;
      nothing is published.
- [x] `PhosphorWeight` declares the six members in the SPEC §9 order, each XML-documented.
- [x] `PhosphorIcon.g.cs` / `PhosphorIconNames.g.cs` are byte-identical to what FEATURE-2DDE
      committed (`git diff` clean on both), and the generator's `--check` exits 0.
- [x] `PhosphorIconSet` implements the full SPEC §9 surface — typed and `IIconSet` — with the SPEC §9.1
      two-level cache, and contains **no** `Enum.ToString`/`Enum.Parse`/`Enum.GetName`/reflection over
      `PhosphorWeight` or `PhosphorIcon` (grep-verifiable; SPEC §2.11, §7.1, §10.4).
- [x] A variant the set lacks is a **miss** on every path — `TryGetGlyph` false, `GetGlyph` throws
      `IconNotFoundException` — with no fallback to `regular` (SPEC §6.1), asserted by test.
- [x] A corrupted resource fails loudly: version, weight-name, view-box, and count mismatches each
      raise `InvalidDataException` (SPEC §7.2), and it **propagates from `TryGetGlyph` as well as
      `GetGlyph`** — never degraded to a silent `false` (SPEC §6.1 precedence table). Covered at least
      by the header assertions on the six real resources plus a negative case over a synthetic
      in-memory header.
- [x] `src/Enigma.Icons.Phosphor/README.md` exists, is packed, and carries the SPEC §14.2 credit line
      verbatim plus the refresh pointer to `docs/reference/phosphor/README.md`.
- [x] `tests/Enigma.Icons.Phosphor.UnitTests` covers **every** bullet of SPEC §12.2, including the
      full-corpus 9,072-pair integrity pass, the full-corpus enum↔name round trip, and all SPEC §7.4
      layer-shape tripwires (the 8 fill exceptions and the 2 duotone exceptions named explicitly).
- [x] `Enigma.Icons.slnx` gained exactly the two `<Project>` entries listed in Design step 7 and no
      others (SPEC §3.4).
- [x] Every new/edited text file is LF with a final newline (SPEC §2.7).
- [x] **`dotnet build Enigma.Icons.slnx -c Release` succeeds with zero warnings** across all TFMs
      (`TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`) — including CS1591 over the 1,512 generated
      enum members and IL2xxx/IL3xxx clean on `net8.0`/`net10.0` (SPEC §2, §10.4).
- [x] **`dotnet test Enigma.Icons.slnx` — the whole suite green**, not just this item's project
      (`Enigma.Icons.UnitTests` + `Enigma.Icons.Phosphor.UnitTests`), with the run output captured as
      evidence.
- [x] **Roadmap + this plan flipped to `DONE`** (`docs/roadmap.md` FEATURE-3950 row; this file's
      status header). (DoD criterion 4.)
- [x] **Completion doc `docs/done/FEATURE-3950.md` written** — summary, files touched, deviations,
      build/test evidence. (DoD criterion 5.)

## Notes / risks

- **The corpus tests take a second or two. That is accepted.** Reading six ~600 KB resources and
  materializing up to 9,072 glyphs is the point of the item, and SPEC §15 explicitly forbids
  timing-based tests. Do not trade coverage for speed by sampling the corpus.
- **`netstandard2.0` ⇒ no `Span<T>` parsing.** The `.dat` reader must be written with
  `string.IndexOf`/`Substring`/`Split(char)` only, or the `Span`/`ReadOnlySpan` variant must be
  TFM-conditioned. SPEC §3.3 forbids adding `System.Memory` (or any polyfill) without a concrete
  compile error and a justified csproj comment — so the default answer is the single portable
  implementation. The extra allocations are a bounded one-off per weight, not a hot path.
- **Manifest resource names are case-sensitive and easy to get wrong.** They are synthesized from
  `RootNamespace` + the folder path + the file name, so a renamed folder (`assets/` vs `Assets/`), a
  `RootNamespace` override, or a glob that matches nothing all produce a silently empty resource set
  — the build stays green and every lookup throws at runtime. `ResourceManifestTests` is the gate;
  treat it as load-bearing, not as a formality. Related: SPEC §7.1 writes the glob with a backslash
  (`Assets\phosphor.*.dat`); MSBuild normalizes separators on Linux, so keep the SPEC form and let
  the test prove it resolved.
- **`default(PhosphorWeight)` is `Thin`, not `Regular`.** SPEC §9's default *parameter* values are
  explicit, so this package is fine — but FEATURE-3ADD must set `WeightProperty`'s registered default
  to `Regular` explicitly (SPEC §10.2). Flagged here because that is where the trap will bite.
- **`ToGeometry` opacity loss is downstream.** This package faithfully carries duotone's per-layer
  opacity into `IconLayer.Opacity`; the caveat about collapsing it lives in FEATURE-3ADD (SPEC §10.1).
  Nothing to do here beyond not flattening the layers.
- **Packed-README ownership — settled, no question (SPEC §13, §13.2).** This item writes the
  **first cut** of `src/Enigma.Icons.Phosphor/README.md` and FEATURE-718F finalizes it; the csproj
  declares `PackageReadmeFile README.md` and packs the file here, unconditionally.
- **Exception for a *missing* embedded resource — settled (SPEC §6.1).** The §6.1 precedence table
  names a missing embedded resource stream as a **broken source** and lists `InvalidDataException`
  among the types such a source may raise, so this plan's choice stands: a `null` stream throws
  `InvalidDataException`, giving "the embedded asset layer is broken" one failure mode that propagates
  from both lookup methods. It is unreachable in a correctly built package, and
  `ResourceManifestTests` guarantees that.
- **Null-argument consistency with `SvgIconSet`.** The rule is **already settled** by FEATURE-24DD,
  which establishes it as the precedent for the whole abstraction in its own acceptance criteria and
  states it in the `IIconSet` XML docs: `TryGetGlyph(null, …)` returns `false` (a null name is a miss,
  per SPEC §6.1's precedence table), `GetGlyph(null, …)` throws `ArgumentNullException`, and the null
  guard sits ahead of name normalization. Match it exactly — do not re-derive it. The two `IIconSet`
  implementations disagreeing on null handling would be a real defect.
- **Do not "fix" the generated files.** If a corpus or layer-shape test fails, the bug is upstream in
  the snapshot or in the generator (SPEC §8), never in the `.g.cs`/`.dat` files. Editing generated
  output to make a test pass destroys the reproducibility guarantee that `--check` exists to defend.
