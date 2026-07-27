# FEATURE-3950 — Enigma.Icons.Phosphor package + UnitTests · DONE

**Branch:** `feature/feature-3950-phosphor-pack` · Single-phase · Plan: `docs/plan/FEATURE-3950.md`

## Summary

Turned FEATURE-2DDE's inert committed output in `src/Enigma.Icons.Phosphor/` into the shipping
`Enigma.Icons.Phosphor` package: a packable `netstandard2.0;net8.0;net10.0` library that embeds the
six `.dat` weight tables and serves them through `PhosphorIconSet` on both the strongly-typed
(`PhosphorIcon`/`PhosphorWeight`) and the string-keyed `IIconSet` surface (SPEC §9), plus the
full-corpus test project (SPEC §12.2) that proves the artwork and the generated code agree across all
9,072 `(icon, weight)` pairs.

Nothing was regenerated and nothing generated was edited: the six `.dat` files and both `.g.cs` files
are byte-identical to what 2DDE committed, `git status` on them is clean, and the generator's
`--check` still exits 0 both before and after this dev.

Four properties are worth calling out:

- **The resource lookup is a literal plus an array index**, never `Enum.GetName`/`ToString`/`Parse`
  and never reflection over either enum — the only reflection in the package is
  `typeof(PhosphorIconSet).Assembly`, which trimming preserves. That is what keeps the IL2xxx/IL3xxx
  surface empty on `net8.0`/`net10.0`.
- **A broken source is never degraded to a miss.** The `.dat` reader validates version, weight name,
  view box, count, line shape and duplicate names, and every `InvalidDataException` it raises
  propagates out of `TryGetGlyph` exactly as it does out of `GetGlyph` (SPEC §6.1's precedence
  table). One lookup core sits behind all four public members, so the typed and string surfaces
  cannot diverge.
- **A variant the set lacks is a miss on every path**, never a silent downgrade to `regular`.
- **The tripwires encode SPEC §7.4's measured ground truth as literals in the test files** — the 8
  multi-layer fill icons with their exact counts, the 2 single-layer duotone icons, and the 10,592
  total layer count — so an upstream refresh that changes the shape of the corpus fails loudly
  instead of drifting in.

One finding was surfaced rather than silently patched: a name-normalization divergence between the
generated `PhosphorIconNames.TryParse` and the base library's `IconNameNormalizer` (deviation 1).

## Files touched

### Created — `src/Enigma.Icons.Phosphor/` (the package)

| File | What |
|---|---|
| `Enigma.Icons.Phosphor.csproj` | Packable, three TFMs, `ImplicitUsings=disable`, `GenerateDocumentationFile=true`, `IsTrimmable`/`IsAotCompatible` on the modern TFMs only, **zero** `PackageReference`, one `ProjectReference` to `Enigma.Icons`, no `RootNamespace` override, the `Assets\phosphor.*.dat` embed, and the three packed files with the SPEC §14.2 licence-obligation comment. |
| `PhosphorWeight.cs` | The six members in SPEC §9 order, each XML-documented, with the load-bearing-declaration-order note and the `default(PhosphorWeight) == Thin` warning. |
| `PhosphorIconSet.cs` | Both surfaces, the singleton, the two-level cache, the `v1` `.dat` reader and the glyph materializer. |
| `README.md` | Packed first cut (FEATURE-718F finalizes it): weights, both surfaces, caching, the verbatim SPEC §14.2 credit line, the `THIRD-PARTY-NOTICES.md` pointer, and the refresh pointer to `docs/reference/phosphor/README.md`. No badges (SPEC §13.1). |

### Created — `tests/Enigma.Icons.Phosphor.UnitTests/` (103 tests)

| File | Covers |
|---|---|
| `Enigma.Icons.Phosphor.UnitTests.csproj` | `net10.0`, `Exe`, `IsPackable=false`, one `xunit.v3` reference (no `Version=`), one `ProjectReference`. |
| `ResourceManifestTests.cs` | The manifest holds **exactly** the six SPEC §7.1 names; each opens non-empty; the product's own lookup reaches every one of them. |
| `DatFormatTests.cs` | The SPEC §7.2 header on all six resources, declared-vs-actual line counts, UTF-8-without-BOM, a single final newline, zero CR bytes, no malformed line — plus 13 synthetic-stream negative cases for the reader and 7 for the materializer. |
| `CorpusIntegrityTests.cs` | All 9,072 pairs resolve with ≥ 1 layer, non-empty path data and the default view box; each weight table holds exactly the enum's 1,512 names, distinct; each table is ordinal-sorted. |
| `EnumNameTests.cs` | `All` ↔ `ToKebabCase` over all 1,512; the full `TryParse` round trip; the `^[a-z-]+$` alphabet and ordinal order; accepted and rejected input spellings; `ToKebabCase` out-of-range at both ends; `All` is not castable to `string[]`. |
| `LayerShapeTests.cs` | The SPEC §7.4 tripwires: 1 opaque layer per icon for thin/light/regular/bold; the 8 named fill outliers with exact counts and no translucent fill layer; duotone's 2 layers at 0.2/1.0 with the 2 named exceptions; the 10,592 total. |
| `PhosphorIconSetTests.cs` | `Name`, `Variants`, `DefaultVariant`, `IconNames`, the `Regular` default, typed-vs-string agreement, reference-equal caching, case/snake/Pascal input, the no-fallback rule with full `IconNotFoundException` content, null handling, undefined enum values, duotone opacity, and the `IIconSet` view. |
| `ConcurrencyTests.cs` | Threads racing the first touch of one weight and of all six; 8 threads × 6 weights × 96 overlapping pairs never throw and never observe two instances for a pair. |
| `TestSupport/DatResource.cs` | Reads the six resources directly, bypassing the type under test. |
| `TestSupport/ViolationLog.cs` | Collects corpus violations and fails once with the first 10 plus a total. |

### Modified

- `Enigma.Icons.slnx` — exactly the two `<Project>` entries of Design step 7, into `/src/` and
  `/tests/`. The solution now holds 5 projects. No Avalonia or Gallery rows.
- `docs/roadmap.md`, `docs/plan/FEATURE-3950.md` — status `TODO` → `IN PROGRESS` → `DONE`; all 17
  acceptance criteria ticked.

`Directory.Build.props`, `Directory.Packages.props`, `global.json`, `.editorconfig`,
`.gitattributes`, `src/Enigma.Icons/`, `tests/Enigma.Icons.UnitTests/` and
`tools/Enigma.Icons.Generator/` were **not** touched. Neither were the eight generated artifacts.

## Deviations & follow-ups

1. **FINDING — `PhosphorIconNames.TryParse` rejects SCREAMING_SNAKE_CASE, and `SvgIconSet` accepts
   it.** The two `IIconSet` implementations therefore disagree on one class of input. The generated
   normalizer (`PhosphorIconNames.g.cs`, from the generator's `NameMapper` template) inserts a
   separator before **every** upper-case letter, so `ADDRESS_BOOK` normalizes to
   `A-D-D-R-E-S-S-B-O-O-K` and misses; `Enigma.Icons`' `IconNameNormalizer` uses a word-boundary rule
   (previous char lower or digit, or previous upper followed by a lower) and correctly yields
   `address-book`. Everything else agrees: kebab in any casing, `address_book`, `Address_Book`,
   `AddressBook` and `addressBook` all resolve.

   SPEC §12.2 asks that `TryParse` accept "kebab, snake, and Pascal forms case-insensitively", so on
   a strict reading this is a real gap. It was **not** fixed here: the fix belongs in
   `tools/Enigma.Icons.Generator/NameMapper.cs` and requires regenerating `PhosphorIconNames.g.cs`,
   which this item's acceptance criteria explicitly forbid ("byte-identical to what FEATURE-2DDE
   committed"), and which is FEATURE-2DDE's to own. Two tests pin the current behaviour with a
   `KNOWN GAP` comment — `EnumNameTests.TryParse_DoesNotAcceptARunOfUpperCaseLetters` and
   `PhosphorIconSetTests.NameLookup_MissesOnARunOfUpperCaseLetters` — so it is visible and cannot
   regress silently; both fail the day it is fixed, which is the intended prompt to update them.

   **Recommended follow-up:** a `BUG-HHHH` item against the generator's normalizer template, aligning
   its boundary rule with `IconNameNormalizer`'s and regenerating. It is a MINOR-level behaviour
   change, not a breaking one (it only turns misses into hits), and it does not renumber any enum
   ordinal.

2. **Two members are `internal` rather than `private`, and the csproj carries an
   `InternalsVisibleTo`.** The plan specifies `private static ... LoadWeight/ReadTable/BuildGlyph`,
   but its own acceptance criteria require "a negative case over a synthetic in-memory header", and
   the six real resources are valid — so the rejection paths are unreachable through the public API.
   `ReadTable(Stream, string, string)` and `BuildGlyph(string, string, string)` are therefore
   `internal`, exposed to `Enigma.Icons.Phosphor.UnitTests` only, exactly as FEATURE-24DD did for
   `Enigma.Icons`' internals. **Nothing was added to the public API**, and no product code exists
   solely for tests: both methods are the real production code path, merely not `private`.

3. **`ContainsKey` + `Add` instead of `Dictionary.TryAdd`.** `TryAdd` is `netstandard2.1`+; one
   implementation for every TFM beats an `#if`, matching the precedent in `IconViewBox.GetHashCode`.

4. **`double.TryParse` instead of the plan's `double.Parse` for the opacity prefix.** The plan asks
   for `Parse` while also requiring a malformed prefix to raise `InvalidDataException`; `Parse` would
   raise `FormatException`. `TryParse` plus an explicit throw delivers the specified exception type.

5. **The reader tolerates one trailing blank line but rejects an interior one.** The plan says to
   "skip a single trailing empty line". `StreamReader.ReadLine` does not synthesize a line for a
   file's final newline, so an empty line can only mean `\n\n`; it is accepted only when nothing
   follows it, and an interior blank line is `InvalidDataException`.

6. **`ConcurrencyTests` builds its own uncached instance by reflecting over the private
   constructor.** By the time the class runs, `Instance` has loaded every weight, so a *first-touch*
   race — the interesting one — is unreachable through it. Reflection is confined to the test
   assembly; the package itself contains none, which is what SPEC §10.4 constrains.

7. **Two shared test helpers were added** (`TestSupport/DatResource.cs`, `TestSupport/ViolationLog.cs`)
   beyond the plan's seven-file table, mirroring `tests/Enigma.Icons.UnitTests/TestSupport/`. The
   seven files the plan names all exist and carry the assertions it lists.

8. **The synthetic-header cases live in `DatFormatTests.cs`** rather than an eighth file, since that
   file already owns the SPEC §7.2 format.

9. **The `InvalidDataException`-propagates-from-`TryGetGlyph` guarantee is asserted structurally, not
   end-to-end.** Both public lookup paths funnel through one `TryGetGlyphCore` with no `catch`
   anywhere between it and the reader, and the reader's rejections are covered directly. Corrupting
   an *embedded* resource at runtime would require building a second assembly, which was judged
   disproportionate; the acceptance criterion asks for the header assertions plus a synthetic
   negative case, and both are present.

10. **`docs/reference/release/RELEASE.template.md` is still untracked** — `.gitignore`'s `[Rr]elease/`
    rule swallows it. Carried forward unchanged from FEATURE-21C4, FEATURE-24DD and FEATURE-2DDE, and
    **FEATURE-74DC depends on that file**. Recommended alongside this dev's commit:
    `git add -f docs/reference/release/RELEASE.template.md`.

11. **Line endings — nothing to report.** All 14 new text files and the 3 modified ones are LF with a
    final newline (CR count 0, last byte `0x0A`, verified byte-wise). **No CRLF recommendation.**

## Build/test evidence

**1. Pre-flight — the committed assets were current before a line was written:**

```
$ dotnet run --project tools/Enigma.Icons.Generator -- \
      --input <extracted snapshot> --output src/Enigma.Icons.Phosphor --check
8 artifacts match the committed bytes.                                    exit 0
```

Re-run after the dev: **exit 0** again, and `git status --porcelain` on `PhosphorIcon.g.cs`,
`PhosphorIconNames.g.cs` and `Assets/` prints **nothing** — the generated artifacts are untouched.

**2. Zero-warning Release build across all TFMs** (`--no-incremental`):

```
$ dotnet build Enigma.Icons.slnx -c Release --no-incremental
  Enigma.Icons.Generator  -> tools/.../net10.0/Enigma.Icons.Generator.dll
  Enigma.Icons            -> src/.../{netstandard2.0,net8.0,net10.0}/Enigma.Icons.dll
  Enigma.Icons.Phosphor   -> src/.../{netstandard2.0,net8.0,net10.0}/Enigma.Icons.Phosphor.dll
  Enigma.Icons.UnitTests          -> tests/.../net10.0/Enigma.Icons.UnitTests.dll
  Enigma.Icons.Phosphor.UnitTests -> tests/.../net10.0/Enigma.Icons.Phosphor.UnitTests.dll
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

Zero warnings **is** the CS1591 proof over the 1,512 generated enum members and the IL2xxx/IL3xxx
proof on `net8.0`/`net10.0` (`TreatWarningsAsErrors` + `EnforceCodeStyleInBuild`).

**3. The whole suite is green — not just this item's project:**

```
$ dotnet test --solution Enigma.Icons.slnx
  Enigma.Icons.UnitTests.dll          (net10.0|x64) passed (777ms)
  Enigma.Icons.Phosphor.UnitTests.dll (net10.0|x64) passed (945ms)

Test run summary: Passed!
  total: 369   failed: 0   succeeded: 369   skipped: 0   duration: 1s 089ms
```

266 pre-existing + **103 new**. The full-corpus passes (9,072 resolutions, 9,072 layer-shape checks,
another 9,072 for the layer total, 1,512 round trips) run in well under a second, so no coverage was
traded for speed — the corpus is never sampled.

**4. `dotnet pack` — local verification only, nothing published:**

```
$ dotnet pack src/Enigma.Icons.Phosphor/Enigma.Icons.Phosphor.csproj -c Release
Successfully created package 'Enigma.Icons.Phosphor.1.0.0.nupkg'   (3,704,751 B)
Successfully created package 'Enigma.Icons.Phosphor.1.0.0.snupkg'
```

nupkg contents:

| Entry | Present |
|---|---|
| `README.md`, `LICENSE.md`, `THIRD-PARTY-NOTICES.md` | ✅ all three at the package root |
| `lib/{netstandard2.0,net8.0,net10.0}/Enigma.Icons.Phosphor.{dll,xml}` | ✅ three TFMs, XML docs included |
| the six `.dat` resources | ✅ **6 per assembly, in all three**, names verified verbatim against SPEC §7.1 |
| dependencies | ✅ **one entry per TFM group, `Enigma.Icons` 1.0.0** — no third-party dependency on any TFM |

The 3.53 MB nupkg is the expected outcome of the deliberate three-TFM choice recorded in SPEC §7.3
(≈3.4–3.5 MB predicted), not a defect. No `PackageReleaseNotes` — FEATURE-74DC owns it.

**5. Manifest resource names, asserted rather than inspected** — `ResourceManifestTests` compares
`GetManifestResourceNames()` against the six SPEC §7.1 literals as an exact set, and a third test
drives a real lookup through every weight so a `RootNamespace` or folder rename cannot pass as green.

**6. No enum reflection anywhere in the package:**

```
$ grep -nE 'Enum\.(ToString|Parse|GetName|IsDefined|GetValues)|Reflection' \
      src/Enigma.Icons.Phosphor/PhosphorIconSet.cs src/Enigma.Icons.Phosphor/PhosphorWeight.cs
  → only two comment lines stating those APIs are not used
$ grep -n typeof src/Enigma.Icons.Phosphor/PhosphorIconSet.cs
  → typeof(PhosphorIconSet).Assembly, twice — the resource stream and the assembly name in an error
```

The generated files were independently verified free of the same APIs by FEATURE-2DDE.

**7. Behaviour asserted by test, not by inspection:**

| Guarantee | Test |
|---|---|
| A variant the set lacks is a miss on **both** methods, never a fallback to `regular` | `AVariantTheSetLacks_IsAMissAndNeverFallsBackToRegular` (`heavy`, `outline`, `""`, `"regular "`) |
| `IconNotFoundException` carries `IconName`/`Variant`/`SetName` and the SPEC §5.3 message shape | same, plus `AnUnknownIconName_IsAMiss` |
| `TryGetGlyph(null, …)` is `false`; `GetGlyph(null, …)` throws `ArgumentNullException` — matching `SvgIconSet` exactly | `ANullIconName_IsAMissForTryGetGlyphAndThrowsFromGetGlyph` |
| Version / weight-name / view-box / count / duplicate-name / line-shape corruption each raise `InvalidDataException` naming the resource | `Reader_RejectsACorruptTable`, 13 cases |
| Repeated and cross-surface lookups are reference-equal | `RepeatedLookups_AreReferenceEqual`, `TypedAndStringSurfaces_ReturnTheSameInstance` |
| An out-of-range cast `PhosphorWeight`/`PhosphorIcon` throws `ArgumentOutOfRangeException` | `AnUndefinedWeight_ThrowsArgumentOutOfRange`, `AnUndefinedIcon_ThrowsArgumentOutOfRange` |
| Concurrent first-touch and hammering never throw and never split an instance | `ConcurrencyTests`, 3 tests |

**8. SPEC §7.4 ground truth, re-derived through the public API:** thin/light/regular/bold 1 opaque
layer each; fill 1 each except the 8 named icons at 2/2/2/2/3/2/3/2 with no translucent layer;
duotone 2 layers at `0.2`/`1.0` except `cell-signal-none` and `wifi-none` at 1 layer, opacity 1;
**10,592** layers in total. Opacity compared with a `1e-9` tolerance, never `==`.

**9. Line endings:** all 17 new and modified text files — CR count `0`, final byte `0x0A`.

**10. Acceptance criteria** — all 17 met and ticked in `docs/plan/FEATURE-3950.md`.
