# FEATURE-2DDE — Asset generator tool + generated Phosphor resources and enum · DONE

**Branch:** `feature/feature-2dde-asset-generator` · Single-phase · Plan: `docs/plan/FEATURE-2DDE.md`

## Summary

Built the deterministic asset generator (`tools/Enigma.Icons.Generator`, SPEC §8) **and** ran it to
produce the eight committed artifacts it owns: the six `Assets/phosphor.<weight>.dat` resources
(SPEC §7.2) plus `PhosphorIcon.g.cs` and `PhosphorIconNames.g.cs` (SPEC §8.4), all written into
`src/Enigma.Icons.Phosphor/` — which still has **no csproj**, exactly as SPEC §16.2 prescribes. Only
`tools/Enigma.Icons.Generator` was appended to the slnx.

The output landed on the SPEC's measured size budget **exactly** — every weight byte-for-byte on the
§7.3 figure, 3,951,294 B total, a 0.00 % deviation against a ±2 % allowance — and reproduces every
SPEC §7.4 ground-truth fact from the data rather than from a hard-coded expectation.

The generator is a text emitter with no dependencies at all — no `PackageReference`, and
deliberately no `ProjectReference` to `Enigma.Icons`, so the artwork can be regenerated from the
pinned snapshot at any commit regardless of the library's build state.

Three properties make future artwork refreshes reviewable rather than risky:

- **One renderer, two consumers.** Every artifact is rendered to a `byte[]` and then either written
  or byte-compared through the same `OutputTarget`. `--check` cannot drift from generate mode
  because there is no second implementation to drift.
- **Nothing non-deterministic reaches the output.** No timestamps, no host paths, no version
  strings, no culture-sensitive formatting or comparison, no enumeration-order dependence (names are
  re-sorted `Ordinal`), and LF is written explicitly rather than inherited from `Environment.NewLine`
  or from the tool's own source encoding.
- **Every upstream invariant is an assertion, not an assumption.** The cross-weight name-set
  equality, the one-view-box-per-weight rule, the kebab alphabet, the `d`/`opacity` validity, and the
  vocabulary the v1 format cannot carry are all checked; a future upstream change fails loudly
  instead of silently shrinking the enum or dropping artwork.

## Files touched

### Created — `tools/Enigma.Icons.Generator/` (the tool)

| File | What |
|---|---|
| `Enigma.Icons.Generator.csproj` | `net10.0`, `Exe`, `IsPackable=false`, `ImplicitUsings=disable`. **Zero** `PackageReference`, **zero** `ProjectReference`. |
| `Program.cs` | Orchestration, the SPEC §8.3 step 7 summary, the vocabulary warning block, exit codes 0/1/2/3, and the catch-all that keeps stack traces off the console. |
| `CommandLine.cs` | `--input`/`--output`/`--check` and nothing else; rejects unknown args, missing values, repeats and missing required options. |
| `Weights.cs` | The six weights in SPEC §9 enum order and their filename suffixes — declared once. |
| `SvgPathReader.cs` | Hardened `XmlReader` extraction of the root `viewBox` and the ordered `(d, opacity)` list; per-file validation; the `Vocabulary` tally. |
| `CorpusLoader.cs` | Enumeration, name derivation, per-weight view-box agreement, the cross-weight name-set assertion with a capped diff listing; plus `LayerRecord`/`IconRecord`/`WeightCorpus` and `GeneratorException`. |
| `DatRenderer.cs` | One `WeightCorpus` → `byte[]` in the SPEC §7.2 `v1` format. |
| `EnumRenderer.cs` | `PhosphorIcon.g.cs` → `byte[]`, with the per-member `<summary>` and the ordinal-instability caveat. |
| `NamesRenderer.cs` | `PhosphorIconNames.g.cs` → `byte[]` — a fixed template plus the one-name-per-line data block. |
| `NameMapper.cs` | kebab → Pascal, the mappable-name guard, and the runtime normalizer emitted as template text. |
| `OutputTarget.cs` | The single write-or-compare path, the UTF-8-no-BOM/LF encoder, and the shared generated-file header. |

### Created — `src/Enigma.Icons.Phosphor/` (inert committed data; no csproj yet)

| File | Bytes |
|---|---|
| `Assets/phosphor.thin.dat` | 678,696 |
| `Assets/phosphor.light.dat` | 678,649 |
| `Assets/phosphor.regular.dat` | 632,699 |
| `Assets/phosphor.bold.dat` | 629,731 |
| `Assets/phosphor.fill.dat` | 553,221 |
| `Assets/phosphor.duotone.dat` | 778,298 |
| `PhosphorIcon.g.cs` | 103,482 |
| `PhosphorIconNames.g.cs` | 39,936 |

### Modified

- `Enigma.Icons.slnx` — exactly one new entry, `tools/Enigma.Icons.Generator`, under `/tools/`.
  No `src/Enigma.Icons.Phosphor` and no `tests/Enigma.Icons.Phosphor.UnitTests` row (SPEC §3.4).
- `docs/roadmap.md`, `docs/plan/FEATURE-2DDE.md` — status `TODO` → `IN PROGRESS` → `DONE`.
- `CLAUDE.md` — documentation freshness sweep, accepted by the user: replaced the stale
  "project-less solution / `dotnet test` finds no tests" caveat with an accurate statement of what
  exists as of this item, added the generator's 0/1/2/3 exit-code contract, and added the
  never-extract-into-the-working-tree warning.
- `docs/reference/phosphor/README.md` — same sweep: the refresh procedure now ends with a `--check`
  run and a step recording that an artwork refresh renumbers enum ordinals and is therefore at least
  a MINOR version bump (SPEC §8.4).

`Directory.Build.props`, `Directory.Packages.props`, `global.json`, `.gitattributes`,
`.editorconfig`, `.gitignore`, `README.md`, `RELEASENOTES.md` and `src/Enigma.Icons` were **not**
touched.

## Deviations & follow-ups

1. **Icons are sorted by the derived *name*, not by filename.** The plan's algorithm step 1 sorts
   file names `Ordinal`, and step 5 requires the `.dat` lines sorted `Ordinal` by *name*. For the
   five suffixed weights those two orders are not the same relation — compare `a-bold.svg` against
   `a-b-bold.svg`, whose names are `a` and `a-b`: filename order puts `a-b` first, name order puts
   `a` first. `CorpusLoader` therefore enumerates in filename order (deterministic) and then
   explicitly re-sorts by name, which is what fixes the `.dat` order and the enum ordinals. Phosphor
   2.1.1 happens not to contain such a pair, so the two orders coincide today — but relying on that
   would have been an accident waiting for the next refresh.

2. **A kebab-alphabet assertion was added** (fatal, exit 2). The plan's per-file validation list does
   not include one, but SPEC §8.4's "mechanical and lossless, no exception list" mapping is only
   lossless because all 1,512 names match `^[a-z-]+$`. A name outside that alphabet would silently
   produce a colliding or invalid enum member. This is the same class of tripwire as the plan's own
   TAB/newline/`@` guard on `d`, and it is data-driven, not a hard-coded expectation.

3. **`PhosphorIconNames.g.cs` carries four `using` directives, not two.** The plan says the file
   "needs `System` and `System.Collections.Generic`" while also specifying
   `ReadOnlyCollection<string> AllNames = Array.AsReadOnly(Names)` and a `StringBuilder`-based
   normalizer — which additionally require `System.Collections.ObjectModel` and `System.Text`. The
   field type was kept as specified and the two missing usings added; the plan's using list was
   simply incomplete. `PhosphorIcon.g.cs` has none, as specified.

4. **The eleven-type file layout keeps to the plan's ten-file table.** `GeneratorException` and the
   three record shapes live in `CorpusLoader.cs` (the module that produces and throws them), and
   `Vocabulary` in `SvgPathReader.cs` (the module that fills it), rather than in new files the
   plan's table does not list.

5. **The compile probe multi-targets `netstandard2.0;net8.0;net10.0`, not `net10.0` alone.** The plan
   asks for a minimal `net10.0` classlib. Using FEATURE-3950's real TFM set is a strict superset and
   it proves something the `net10.0`-only probe could not: the collection-expression `Names = [ … ]`
   and `Array.AsReadOnly` are `netstandard2.0`-clean, so 3950's multi-targeted build will not trip
   over the generated code on its lowest floor. The probe lived outside the repository, carried its
   own empty `Directory.Build.props` to stop the MSBuild walk-up, and was deleted afterwards.

6. **Verbatim path data was verified over the whole corpus, not spot-checked.** The criterion asks
   for ≥ 3 icons across ≥ 3 weights. All **10,592** layer fields in the six `.dat` files were instead
   compared byte-for-byte against the `d` attribute of their source `.svg`, together with the
   `@0.2:` prefix placement — 0 problems. (A first pass did use spot-checks and one of them named
   `zoom-in`, which does not exist upstream; both sides came back empty and the check was vacuously
   green. The full-corpus comparison replaced it and would have caught that.)

7. **The generated C# is deliberately ASCII-only.** No `©`, no em dash. A C# file with no BOM leaves
   the compiler to detect the encoding, so keeping the generated sources inside ASCII removes that
   variable entirely. The attribution instead reads
   `Copyright (c) 2020 Phosphor Icons` and points at `THIRD-PARTY-NOTICES.md`.

8. **Exit code 2 also covers a non-existent `--input` or `--output` directory.** The plan assigns 1
   to usage errors and 2 to input/validation failures; a directory that does not exist is a
   well-formed command line naming something absent, so it reports as 2, not 1.

9. **A functional smoke test of the generated API was run in the temp probe** — 17 checks including
   the whole-corpus round-trip (`ToKebabCase` → `TryParse` → identity for all 1,512 members), all
   six accepted input spellings, `null`/empty/miss handling, and the out-of-range throw at both ends.
   All passed. It is not a substitute for FEATURE-3950's test project, which asserts the corpus
   *through* `PhosphorIconSet`; it just moves a template bug's discovery one item earlier. Both temp
   projects were deleted; nothing was added to the repo or the slnx.

10. **OPEN QUESTION 1 was implemented as the SPEC directs, and is still open.** Unexpected elements
    and `<path>` attributes are ignored (SPEC §8.3 step 4) but tallied and reported as a non-fatal
    `warning:` block with per-name counts; the exit code stays 0. On the pinned snapshot the block is
    **empty**, verified by an empty stderr on the real run. If it should instead be **fatal**,
    SPEC §8.3 step 4 needs a wording change and a `.dat` v2 carrying layer attributes becomes a real
    question. Unchanged from the plan; still the user's call.

11. **OPEN QUESTION 2 was implemented as the plan directs, and is still open.** A weight whose files
    disagree on `viewBox` aborts (exit 2) naming both files, rather than picking a winner — the v1
    header carries one view box per weight. Independently confirmed that all 9,072 source files carry
    `viewBox="0 0 256 256"`, so the constraint is unreachable for Phosphor 2.1.1. It would, however,
    block reusing `.dat` v1 for a non-uniform icon family (SPEC §17).

12. **`docs/reference/release/RELEASE.template.md` is still present on disk but untracked** —
    `.gitignore:22`'s `[Rr]elease/` rule swallows the directory, confirmed with `git check-ignore`.
    FEATURE-21C4 prescribed a one-time force-add that has still not happened, and FEATURE-24DD's
    completion doc carried the same recommendation forward. Out of scope here, but **FEATURE-74DC
    depends on that file**. Recommended alongside this dev's commit:
    `git add -f docs/reference/release/RELEASE.template.md`.

13. **Line endings — nothing to report.** Every authored and generated text file is LF with a final
    newline, verified byte-wise across all 21 new files (`\r` count 0, last byte `0x0A`).
    `git check-attr` reports the `.dat` files as `text: auto`, `eol: lf` and the `.g.cs` files as
    `text: set`, `eol: lf`, so future artwork refreshes diff line-by-line. **No CRLF
    recommendation.**

## Build/test evidence

**No test project exists in this item** — SPEC §12.2's corpus tests belong to FEATURE-3950, which is
the right owner because they assert the data *through* `PhosphorIconSet`. DoD criterion 2 is met
here by (a) the zero-warning build, (b) three `--check` runs, (c) the ground-truth assertions below
run against the committed bytes, and (d) the compile probe of the generated C#. The pre-existing
suite was also run and is green.

**1. Archive verified before extraction, extracted to a temp directory:**

```
$ sha256sum docs/reference/phosphor/phosphor-2.1.1-svgs-flat.tar.gz
6dc577a39a5a0cb72984e698ba93e7e3736da99b5884fcb221e564d5244c301e   ← matches docs/reference/phosphor/README.md
$ stat -c%s …                     1412717 bytes                    ← matches
$ tar -xzf … -C /tmp/phosphor-flat ; find /tmp/phosphor-flat -name '*.svg' | wc -l
9072                              (1,512 per weight × 6)
```

`git status --untracked-files=all` shows **no** `.svg` anywhere in the working tree. The only `.svg`
files on disk are FEATURE-24DD's six tracked test assets and their gitignored `bin/` copies.

**2. Zero-warning solution build** (`--no-incremental`):

```
$ dotnet build Enigma.Icons.slnx
  Enigma.Icons.Generator -> tools/Enigma.Icons.Generator/bin/Debug/net10.0/Enigma.Icons.Generator.dll
  Enigma.Icons          -> src/Enigma.Icons/bin/Debug/{netstandard2.0,net8.0,net10.0}/Enigma.Icons.dll
  Enigma.Icons.UnitTests -> tests/Enigma.Icons.UnitTests/bin/Debug/net10.0/Enigma.Icons.UnitTests.dll
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

**3. The generator's stdout summary** (SPEC §8.3 step 7) — stderr was **empty**, so the
unexpected-vocabulary block is empty as SPEC §7.4.1-2 requires:

```
weight      icons  layers  multi-layer      bytes
thin         1512    1512            0     678696
light        1512    1512            0     678649
regular      1512    1512            0     632699
bold         1512    1512            0     629731
fill         1512    1522            8     553221
duotone      1512    3022         1510     778298
total        9072   10592         1518    3951294

PhosphorIcon.g.cs            103482
PhosphorIconNames.g.cs        39936

8 artifacts written.
```

**4. Reproducibility — the three `--check` runs, all exit 0:**

| # | Run | Exit |
|---|---|---|
| 1 | immediately after generation | **0** |
| 2 | immediate second run | **0** |
| 3 | after re-extracting the archive into a differently-named temp directory | **0** |

Run 3 proves the output contains no leaked input path and no dependence on filesystem enumeration
order.

**5. `--check` is non-destructive and reports precisely.** Against a scratch copy of the output tree
(outside the repo) with one `.dat` deleted, one extended by a byte and one `.g.cs` corrupted at
offset 5000:

```
error: the committed assets differ from what the generator produces:
  Assets/phosphor.bold.dat: 629732 bytes on disk, 629731 bytes generated; identical up to offset 629731, then truncated or extended.
  Assets/phosphor.fill.dat: missing.
  PhosphorIcon.g.cs: 103482 bytes on disk, 103482 bytes generated; first difference at offset 5000.
re-run the generator without --check and review the diff.
                                                            exit 3
```

Nothing was written — the deleted file stayed deleted — and a sha256 fingerprint of
`src/Enigma.Icons.Phosphor/` was identical before and after.

**6. Size budget — SPEC §7.3's whole-file targets, met exactly:**

| weight | target | actual | deviation |
|---|---|---|---|
| thin | 678,696 | 678,696 | 0.00 % |
| light | 678,649 | 678,649 | 0.00 % |
| regular | 632,699 | 632,699 | 0.00 % |
| bold | 629,731 | 629,731 | 0.00 % |
| fill | 553,221 | 553,221 | 0.00 % |
| duotone | 778,298 | 778,298 | 0.00 % |
| **total** | **3,951,294** | **3,951,294** | **0.00 %** |

**7. Format and ground-truth assertions against the committed bytes** — all six `.dat` files:

- First three bytes `76 31 09` (`v1<TAB>`), i.e. **no** `EF BB BF` BOM; `\r` count **0**; last byte
  `0x0A`; **1,513** lines (header + 1,512).
- Headers exactly `v1<TAB><weight><TAB>0 0 256 256<TAB>1512`, weight name correct, declared count
  equal to the actual icon-line count.
- Name columns **byte-identical across all six weights** (`cut -f1` diffed pairwise, all empty);
  1,512 names, all unique, ordinal-sorted (`LC_ALL=C`), all matching `^[a-z-]+$`.
- thin/light/regular/bold: **every** line has exactly 2 fields, and `grep -c '@'` is **0** in all
  four files.
- fill outliers, exact and complete: `bookmarks-simple` 2, `crane-tower` 2, `hard-drives` 2,
  `lasso` 2, `music-notes-minus` 3, `speaker-simple-x` 2, `stack` 3, `stack-simple` 2 — the other
  1,504 have exactly 1 layer, and no fill layer carries an `@` prefix.
- duotone outliers, exact: 1,510 icons with 2 layers whose **first** field is prefixed `@0.2:`, and
  exactly `cell-signal-none` and `wifi-none` with 1 layer and **no** prefix. No second layer carries
  a prefix.
- Total layer fields across the six files = **10,592** (SPEC §7.4.9).
- SPEC §7.4.4's two root-`fill`-less files did not break generation: `cricket` and `signature` are
  present in `phosphor.duotone.dat` with 2 layers each.
- `viewBox` was **read, not assumed**: independently confirmed that all 9,072 source files carry
  `viewBox="0 0 256 256"` and none lacks the attribute, and the generator aborts (exit 2, both
  filenames named) when a weight's files disagree.
- Path data **verbatim**: all 10,592 layer fields compared byte-for-byte with their source `d`
  attribute — 0 mismatches (see deviation 6).
- `git check-attr text eol` → `text: auto`, `eol: lf` on the `.dat` files.

**8. The generated C#:**

- `PhosphorIcon.g.cs` — `// <auto-generated/>` first line, one `#nullable enable`, **0** `using`
  directives, file-scoped `namespace Enigma.Icons.Phosphor;`, exactly **1,512** members, all
  PascalCase, **no** `= N` initializer anywhere (the only three `=` in the file are `cref=`
  attributes in XML docs), and exactly **1,512** member-level `<summary>` lines — one per member.
  Members equal `pascal(dat names)` and summaries equal the `.dat` names, in order, verified
  programmatically. First `Acorn`, last `YoutubeLogo`. The type-level doc carries the SPEC §8.4
  ordinal-instability caveat pointing at `ToKebabCase`/`TryParse`.
- `PhosphorIconNames.g.cs` — the SPEC §8.4 surface (`ToKebabCase`, `TryParse`, `All`) with XML docs
  on the class and all three members; `Names` holds **1,512** entries, one per line, in enum order,
  declared first; `AllNames` is `Array.AsReadOnly(Names)`; the lookup uses
  `StringComparer.OrdinalIgnoreCase`.
- **Neither file calls `Enum.ToString`, `Enum.Parse`, `Enum.IsDefined` or any reflection API.** The
  only textual occurrences are in comments and XML docs that state those APIs are not used.
- Both files: no BOM, `\r` count 0, final newline, ASCII-only.

**9. Generated C# compiles clean** — temp probe outside the repository, `GenerateDocumentationFile`
and `TreatWarningsAsErrors` both on, over FEATURE-3950's full TFM set:

```
$ dotnet build <temp>/Probe.csproj
  Probe -> bin/Debug/netstandard2.0/Probe.dll
  Probe -> bin/Debug/net8.0/Probe.dll
  Probe -> bin/Debug/net10.0/Probe.dll
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

Zero warnings **is** the CS1591 proof across all 1,512 members. The temp directory was deleted.

**10. Diagnostics — every failure path is a message, never a stack trace:**

| Invocation | Exit | Message |
|---|---|---|
| no arguments / missing `--input` / missing `--output` | 1 | `--input is required.` / `--output is required.` + usage |
| `--input` with no value | 1 | `--input requires a directory.` |
| unknown argument `--verbose` | 1 | `unknown argument "--verbose".` |
| repeated `--input` | 1 | `--input was given more than once.` |
| non-existent `--input` | 2 | `the --input directory does not exist: …` |
| `--check` with a non-existent `--output` | 2 | `the --output directory does not exist: …` |
| input missing the `duotone` weight | 2 | `the --input directory has no "duotone" subdirectory.` |
| a weight missing 2 names | 2 | name-set diff listing, capped at 25 per side |
| a weight with 1 extra name | 2 | name-set diff listing |
| a file with zero `<path>` | 2 | `fill/acorn-fill.svg: it contains no <path> element.` |
| a file with an empty `d` | 2 | `… a <path> has a missing or empty 'd' attribute.` |
| a divergent `viewBox` within a weight | 2 | names both files and the two values |
| a `viewBox` that is not four numbers | 2 | `… its 'viewBox' is not four numbers: "0 0 256".` |
| `opacity="1.5"` | 2 | `… not a number in [0,1]: "1.5".` |
| a `d` containing a TAB | 2 | `… contains a TAB or newline, which the v1 .dat line format cannot represent.` |
| a `<circle>` and `<path stroke fill-rule>` | **0** | non-fatal `warning:` block with per-name counts |

A sha256 fingerprint of `src/Enigma.Icons.Phosphor/` was identical before and after the whole
diagnostic sweep — no failing run touched the committed assets.

**11. The pre-existing suite is green:**

```
$ dotnet test --solution Enigma.Icons.slnx
Test run summary: Passed!
  total: 266   failed: 0   succeeded: 266   skipped: 0
```

**12. Attribution and scope:**

- `THIRD-PARTY-NOTICES.md` is present and contains `docs/reference/phosphor/LICENSE` **verbatim**
  (1,071 bytes, substring match) including `Copyright (c) 2020 Phosphor Icons` — SPEC §14.2 is
  satisfied now that artwork-derived data is committed.
- `src/Enigma.Icons.Phosphor/` contains **only** the two `.g.cs` files and `Assets/` with the six
  `.dat` files — **no csproj** (SPEC §16.2, FEATURE-3950 owns it).
- The slnx has **3** `<Project>` entries and **0** matching `Enigma.Icons.Phosphor*.csproj`.
- `grep -c "<PackageReference"` and `grep -c "<ProjectReference"` on the tool's csproj → **0** and
  **0**.

**13. Acceptance criteria** — all 28 met and ticked in `docs/plan/FEATURE-2DDE.md`.
