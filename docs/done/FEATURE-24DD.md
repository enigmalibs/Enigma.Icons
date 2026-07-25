# FEATURE-24DD — Enigma.Icons base library + UnitTests · DONE

**Branch:** `feature/feature-24dd-icons-base` · Single-phase · Plan: `docs/plan/FEATURE-24DD.md`

## Summary

Built the framework-agnostic foundation the whole solution sits on: the immutable icon data model
(SPEC §4), the hardened SVG parser (SPEC §5), the `IIconSet` abstraction and its
bring-your-own-icons implementation `SvgIconSet` (SPEC §6), the packable project with **zero
package references**, a real packed README documenting the supported subset *and* its exclusions
(SPEC §13), and the `Enigma.Icons.UnitTests` suite covering every bullet of SPEC §12.1.

`src/Enigma.Icons` builds warning-free across `netstandard2.0;net8.0;net10.0`, packs cleanly to a
zero-dependency `.nupkg`, and the 266-test suite is green — including the two mandatory security
tests (XXE non-resolution and billion-laughs fail-fast).

The parser is deliberately narrow and deliberately hard to misuse:

- **Untrusted input by default.** Every document is read through an `XmlReader` with
  `DtdProcessing.Prohibit` and `XmlResolver = null`, behind a byte-count gate applied *before*
  parsing. No entity is ever defined, so neither XXE nor entity expansion is reachable.
- **The walk is iterative**, over an explicit `Stack<StyleFrame>` carrying `reader.Depth`, so a
  pathologically nested document cannot overflow the CLR stack (asserted at 2,000 levels of `<g>`).
- **Transforms are baked into path data**, so an `IconLayer` never carries one — and when the
  composed matrix is the identity, `<path d>` is passed through **byte-identical**, with no
  tokenization at all.

## Files touched

### Created — `src/Enigma.Icons/` (library)

| File | What |
|---|---|
| `Enigma.Icons.csproj` | Three TFMs, full packaging metadata, trim/AOT conditioned off `netstandard2.0`, `InternalsVisibleTo`, **zero** `<PackageReference>`. |
| `README.md` | Packed. Model, `IIconSet`, the precedence table, the worked "your own SVG folder" example, the supported-subset table, the exclusions, security notes. |
| `IconViewBox.cs` | `readonly struct` + the value-type completion set SPEC §4.1 elides. |
| `IconLayer.cs` | `IconLayer` plus `IconFillRule`, `IconLineCap`, `IconLineJoin`. |
| `IconGlyph.cs` | Defensive copy into `ReadOnlyCollection<IconLayer>`; `IsSingleLayer`. |
| `SvgParseException.cs`, `IconNotFoundException.cs` | SPEC §5.3, no serialization constructors. |
| `SvgIconParser.cs` | Size gate, hardened reader, element dispatch, `<g>` stack, viewBox ladder, glyph assembly. |
| `IIconSet.cs` | The abstraction, its implementer contract and the miss-vs-broken-source precedence table. |
| `SvgIconSet.cs` | Four factories, eager discovery, `Lazy`-backed reference-equal glyph cache. |
| `Internal/Matrix2D.cs` | SVG's `[a c e; b d f]` affine. |
| `Internal/SvgLexer.cs` | Shared number/separator/arc-flag scanner. |
| `Internal/SvgNumber.cs` | The single deterministic number formatter. |
| `Internal/SvgValueParser.cs` | Attribute values: numbers, lengths, point lists, viewBox, opacity, keywords. |
| `Internal/SvgTransformParser.cs` | `transform="…"` → `Matrix2D`. |
| `Internal/SvgShapeConverter.cs` | One converter per SPEC §5.1 shape row. |
| `Internal/SvgPathTransformer.cs` | Bakes a matrix into path data, including elliptical arcs. |
| `Internal/SvgStyleContext.cs` | The inheritable `<g>` presentation state, plus `SvgPresentationAttributes`. |
| `Internal/IconNameNormalizer.cs` | kebab / `snake_case` / PascalCase → kebab-case; variant-suffix stripping. |
| `Internal/PathSafety.cs` | The `IsWithin` containment check. |

### Created — `tests/Enigma.Icons.UnitTests/` (266 tests)

`Enigma.Icons.UnitTests.csproj`, `AssemblyInfo.cs` (parallelization disabled, justified),
`TestSupport/TempDirectory.cs`, six `TestAssets/{thin,bold}/*.svg` (embedded *and* copied to
output), and: `SvgIconParserShapeTests`, `SvgIconParserGroupTests`, `SvgIconParserTransformTests`,
`SvgIconParserPaintTests`, `SvgIconParserViewBoxTests`, `SvgIconParserErrorTests`,
`SvgIconParserSecurityTests`, `IconViewBoxTests`, `IconLayerTests`, `IconGlyphTests`,
`SvgIconSetTests`, `SvgIconSetErrorPrecedenceTests`, `SvgIconSetPathSafetyTests`,
`IconNameNormalizerTests`.

### Modified

- `Enigma.Icons.slnx` — exactly the two rows Design step 9 prescribes, nothing else.
- `docs/roadmap.md`, `docs/plan/FEATURE-24DD.md` — status `TODO` → `IN PROGRESS` → `DONE`.

`Directory.Packages.props`, `Directory.Build.props`, `global.json` and the three config files were
**not** touched.

## Deviations & follow-ups

1. **`xunit.v3` 3.2.2 resolved — FEATURE-21C4's delegated obligation is discharged.** The test
   project's first restore resolved `xunit.v3/3.2.2` from the local cache (confirmed in
   `obj/project.assets.json`). No pin bump was needed and `Directory.Packages.props` was not edited.
   The plain `xunit.v3` metapackage drives Microsoft.Testing.Platform correctly through
   `global.json`'s runner entry — no `mtp-v2` switch, no `Microsoft.NET.Test.Sdk`.

2. **Arc recomputation uses the closed form, as the plan preferred** — the Bézier fallback was not
   needed. `Internal/SvgPathTransformer.WriteArc` forms the ellipse shape matrix
   `E = R(φ)·diag(rx, ry)`, transforms it to `E' = L·E`, and recovers `rx'`, `ry'` and `φ'` from the
   eigen-decomposition of the symmetric `S = E'·E'ᵀ`. `large-arc-flag` is unchanged; `sweep-flag`
   flips exactly when `Determinant < 0`. Verified against four independent cases (non-uniform scale,
   rotation, mirror, degenerate radius).

3. **The conformal fast path was omitted; a translation-only fast path was added instead.** The plan
   suggested detecting a uniform-scale-plus-rotation `L` (`a == d && b == -c`) as a shortcut. It was
   left out on purpose: the general closed form reduces to exactly the same result for that case, and
   one code path is one thing to verify rather than two. What *was* added is a cheaper and more
   valuable guard — when the linear part is the identity (a pure translation, by far the most common
   transform), the arc's `rx`/`ry`/`φ` are emitted unchanged rather than re-derived into an
   equivalent-but-textually-different representation.

4. **Deliberate additions to the SPEC listings** (additive, not contradictory, all flagged in the
   plan): `IconViewBox` gains `==`, `!=`, `Equals(object?)`, `GetHashCode()` and an
   invariant-culture `ToString()` emitting the SPEC §7.2 `.dat` header shape; `IconGlyph` rejects a
   null *element* inside `layers`, naming the index; the csproj carries `InternalsVisibleTo` for the
   test project. `GetHashCode` is hand-rolled because `System.HashCode` is .NET Standard 2.1+.

5. **Two small internal files beyond the plan's table.** `Internal/SvgLexer.cs` holds the number,
   separator and arc-flag scanning shared by the transform parser, the point-list parser and the
   path tokenizer — the alternative was three near-identical copies. `Internal/PathSafety.cs` holds
   `IsWithin`, which the plan specifies without naming a file.

6. **`FromFiles` / `FromSvgSources` default set name.** SPEC §6.2 gives a default name for
   `FromDirectory` (the directory leaf) and `FromAssembly` (the assembly's simple name) but not for
   these two. They default to `"SvgIcons"`, documented on both factories.

7. **`FromAssembly` with a mixed resource layout.** SPEC §6.2 is silent on resources where some
   carry a variant segment and others do not. Variant-less ones are grouped under a nameless
   variant that is not listed in `Variants`; the XML doc says so and states that a uniform layout is
   expected. Nothing is silently dropped.

8. **Presentation attributes are lenient, geometry attributes are strict.** An unparseable
   `opacity`, `stroke-width` or keyword falls back to "unspecified" / the CSS initial value, matching
   SVG's own error handling. An unparseable *geometry* attribute (`<rect width="wide">`) is an error
   naming the element and the attribute, per plan step 4d.

9. **`docs/reference/release/RELEASE.template.md` is present on disk but still untracked by git.**
   FEATURE-21C4's completion doc prescribed a one-time `git add -f` for it (`.gitignore:22`'s
   `[Rr]elease/` rule swallows the directory) and asked the next item to verify it — it was not in
   the bootstrap commit. Out of scope here and harmless for now, but **FEATURE-74DC depends on that
   file**. Recommended, alongside this dev's commit:
   `git add -f docs/reference/release/RELEASE.template.md`.

10. **`dotnet test <solution>` no longer accepts a positional solution path on SDK 10.0.103.** It
    reports *"Specifying a solution for 'dotnet test' should be via '--solution'"*. `CLAUDE.md`'s
    documented command is now stale; see the documentation sweep below.

11. **Line endings — nothing to report.** Every authored file is LF with a final newline (verified
    across all 33 created files). The only CRLF in the tree is in xunit's generated
    `obj/…/XunitAutoGeneratedEntryPoint.cs`, which is a gitignored build artifact, not an authored
    file. **No CRLF recommendation.**

## Build/test evidence

**1. Zero-warning build across all three TFMs** (clean rebuild, `bin`/`obj` deleted first):

```
$ dotnet build Enigma.Icons.slnx
  Enigma.Icons -> bin/Debug/netstandard2.0/Enigma.Icons.dll
  Enigma.Icons -> bin/Debug/net8.0/Enigma.Icons.dll
  Enigma.Icons -> bin/Debug/net10.0/Enigma.Icons.dll
  Enigma.Icons.UnitTests -> bin/Debug/net10.0/Enigma.Icons.UnitTests.dll
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

No IL2xxx/IL3xxx: `IsTrimmable` and `IsAotCompatible` are on for `net8.0`/`net10.0` and evaluate
empty for `netstandard2.0` (verified per-TFM with `dotnet msbuild -getProperty:`).

**2. Whole suite green:**

```
$ dotnet test --solution Enigma.Icons.slnx
Test run summary: Passed!
  total: 266   failed: 0   succeeded: 266   skipped: 0
```

The two mandatory security tests pass: the XXE sentinel appears in neither the exception's
`ToString()` nor any `IconLayer.PathData`, and the billion-laughs document fails fast with **no
timing-based assertion** — the proof is structural (`DtdProcessing.Prohibit`).

**3. Packaging verified by an actual pack**, not by inspection:

```
$ dotnet pack src/Enigma.Icons/Enigma.Icons.csproj -c Release
  Successfully created package 'bin/Release/Enigma.Icons.1.0.0.nupkg'
  Successfully created package 'bin/Release/Enigma.Icons.1.0.0.snupkg'
```

The nupkg holds `lib/{netstandard2.0,net8.0,net10.0}/Enigma.Icons.{dll,xml}`, `README.md` and
`LICENSE.md` — and **no `THIRD-PARTY-NOTICES.md`**, correct per SPEC §14.2. Its nuspec declares
`<group>` elements for all three TFMs with **zero dependency entries**, carries no
`<packageReleaseNotes>` (FEATURE-74DC owns it), and `GeneratePackageOnBuild` is not set.

**4. Zero package references:** `grep -c "<PackageReference" src/Enigma.Icons/Enigma.Icons.csproj`
→ `0`; `git diff Directory.Packages.props` → empty.

**5. Public surface checked against the SPEC** by dumping the generated XML documentation: the
`Enigma.Icons` namespace exposes exactly SPEC §4's three model types plus three enums, §5.3's two
exceptions, §5's `SvgIconParser` (`Parse(string)`, `Parse(Stream)`, `MaxDocumentBytes`), §6.1's
`IIconSet` — **with no default interface members** — and §6.2's `SvgIconSet` with its four
factories, plus the additions listed in deviation 4. Both `GetGlyph` declarations carry the
`variant = null` default.

**6. Acceptance criteria** — all met. Every SPEC §12.1 bullet has at least one focused test,
including the arc-flag path data pinned character-for-character for `circle`, `ellipse` and rounded
`rect`, exercised through **both** `SvgIconParser` and `SvgIconSet`.
