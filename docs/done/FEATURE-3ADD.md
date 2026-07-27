# FEATURE-3ADD — Enigma.Icons.Avalonia + UnitTests · DONE

**Branch:** `feature/feature-3add-avalonia-renderer` · Single-phase · Plan: `docs/plan/FEATURE-3ADD.md`

## Summary

Built the third shipping package, `Enigma.Icons.Avalonia` (SPEC §10): the Avalonia rendering layer.
It adds no icon data and no parsing — it consumes FEATURE-3950's `PhosphorIconSet`/`PhosphorIcon`/
`PhosphorWeight` and, equally, any other `IIconSet`. With this item the solution can put an icon on
screen; FEATURE-469B's gallery is the visual proof.

What shipped:

- **`IconGlyphExtensions`** — the three SPEC §10.1 signatures, verbatim. `ToGeometry` discriminates on
  `Layers.Count == 1` rather than `IconGlyph.IsSingleLayer`, so a one-layer *stroked* or *translucent*
  glyph still collapses to a bare geometry instead of being boxed in a one-child group.
  `ToDrawing` wraps only sub-1.0-opacity layers in their own `DrawingGroup`, skips the
  neither-filled-nor-stroked spacer entirely, and builds a `Pen` from the layer's width/cap/join.
- **`Icon : Control`** — not a `TemplatedControl`, so the package ships **no XAML at all** and a
  consumer adds nothing to `App.axaml`. `Foreground` re-owns `TextElement.ForegroundProperty`, so an
  icon inside any text scope inherits that scope's brush and follows a theme switch with no binding —
  the capability a markup extension structurally cannot have. `Render` never throws.
- **`IconGeometryExtension` / `IconImageExtension`** — the SPEC §10.3 markup extensions, which
  deliberately **fail fast** where the control paints nothing: a bad icon in XAML is an authoring
  error that should surface at load time, while a throwing `Render` would kill the previewer surface
  for the whole window.
- **One XML namespace for both CLR namespaces** — the two `XmlnsDefinition` attributes in
  `Properties/AssemblyInfo.cs`, so a single `xmlns:ei="https://github.com/josueclement/Enigma.Icons"`
  resolves `ei:Icon`, `{ei:IconGeometry}` and `{ei:IconImage}`.
- **65 headless tests** covering every SPEC §12.3 bullet.

Three properties are worth calling out:

- **The layer walk exists exactly once.** `IconGlyphExtensions.TryGetLayerPaint` is the single
  per-layer paint decision; `ToDrawing` and `Icon.Render` both call it and differ only in emission
  (object graph vs. `DrawingContext` calls). It has its own direct tests through
  `InternalsVisibleTo`, so the shared contract is pinned rather than merely intended.
- **The render pass is asserted precisely, not just "it didn't throw."** Draw count, per-draw brush
  and pen, the pushed `Matrix`, the per-layer opacity group and the `UniformToFill` clip are all
  read back out of a recorded drawing tree (see deviation 3), including the exact centring
  arithmetic for all four `Stretch` modes on a deliberately non-square 100×50 target.
- **Zero IL2xxx/IL3xxx.** `IsTrimmable` + `IsAotCompatible` are on unconditionally (SPEC §10.4) and
  the trim/AOT analysers ran clean on both TFMs — the design is reflection-free, `Brush.Parse`
  included.

## Files touched

### Created — `src/Enigma.Icons.Avalonia/` (the package)

| File | What |
|---|---|
| `Enigma.Icons.Avalonia.csproj` | Packable, `net8.0;net10.0` only, `ImplicitUsings=disable`, the full SPEC §10.4 property set, `IsTrimmable`/`IsAotCompatible` **unconditional** with the §10.4 citation in a comment, one `PackageReference` (`Avalonia`, no `Version=`), one `ProjectReference` (`Enigma.Icons.Phosphor`), the two packed files, the SPEC §14.2 comment explaining why `THIRD-PARTY-NOTICES.md` is **not** packed, and `InternalsVisibleTo`. |
| `IconGlyphExtensions.cs` | `ToGeometry` / `ToDrawing` / `ToDrawingImage` + the `internal TryGetLayerPaint` helper and the `IconFillRule`/`IconLineCap`/`IconLineJoin` → Avalonia mappings. The opacity-loss caveat is in the `ToGeometry` XML doc, naming `ToDrawing` as the duotone-correct alternative. |
| `Icon.cs` | The control: eight `StyledProperty` registrations, the static constructor (`AffectsRender` ×8, `AffectsMeasure` on `Size`/`Stretch`, `Focusable` default `false`), `MeasureOverride`, `Render` with the four-mode `Stretch` transform and the `UniformToFill` clip, and the private `IconAutomationPeer`. |
| `Markup/IconGeometryExtension.cs` | `{ei:IconGeometry Acorn, Weight=Bold}` — parameterless + positional constructors, `Weight = Regular`. |
| `Markup/IconImageExtension.cs` | `{ei:IconImage Acorn, Weight=Fill, Brush=Red}` — same, plus `Brush = Brushes.Black`, with the "why not `IconSource`" rationale in the class remarks. |
| `Properties/AssemblyInfo.cs` | The two `XmlnsDefinition` attributes (SPEC §10.3). |
| `README.md` | Packed. Install + TFM note, the single-`xmlns` quick start with the per-namespace `using:` fallback, the control's property table, the `Foreground`-inherits section, control-vs-markup-extension guidance, the `ToGeometry` opacity caveat, the no-`StyleInclude` note, and a bring-your-own-SVG paragraph. FEATURE-718F owns the final polish. |

### Created — `tests/Enigma.Icons.Avalonia.UnitTests/` (65 tests)

| File | What |
|---|---|
| `Enigma.Icons.Avalonia.UnitTests.csproj` | `net10.0`, `OutputType=Exe`, `IsPackable=false`, `xunit.v3` + `Avalonia.Headless.XUnit` (both from CPM), no `Microsoft.NET.Test.Sdk`. |
| `TestAppBuilder.cs` | `[assembly: AvaloniaTestApplication]` + `BuildAvaloniaApp()` — a plain `Application` on the headless platform, no Fluent theme. |
| `TestSupport/TestGlyphs.cs` | Synthetic glyphs so the shape assertions do not depend on the Phosphor corpus: single filled, duotone-shaped, opaque twin, spacer, stroked, stroke-only, garbage stroke paint, even-odd-first, offset view box, and an in-memory SVG source. |
| `TestSupport/RenderRecorder.cs` | Lays an `Icon` out, records its render pass, and flattens the result into leaves / nested groups / the pushed matrix. |
| `IconGlyphExtensionsGeometryTests.cs` | 7 tests — parseable single layer with non-empty bounds, the one-layer-stroked collapse, child count, first-layer `FillRule`, the asserted opacity loss, real Phosphor duotone, null guard. |
| `IconGlyphExtensionsDrawingTests.cs` | 12 tests — wrapping/non-wrapping, spacer skipped, pen width/cap/join, own stroke paint honoured, garbage paint falls back, outline-only, `ToDrawingImage`, 4 null guards. |
| `LayerPaintTests.cs` | 4 tests on the shared `internal` helper directly. |
| `MarkupExtensionTests.cs` | 10 tests — return types, positional constructors, `Weight`/`Brush` defaults, weight honoured, supplied brush, fail-fast. |
| `IconControlTests.cs` | 32 tests (25 facts + 2 theories) — defaults, measure (`Size`, explicit `Width`/`Height`, non-finite), measure invalidation, the duotone two-layer draw with its 0.2 group, all four `Stretch` modes with centring, the `UniformToFill` clip and its absence elsewhere, offset view box, zero bounds, four never-throw cases, `IconSet` precedence, `Foreground` inheritance in a shown window, the real headless render pass, and the automation peer. |

### Modified

| File | Change |
|---|---|
| `Enigma.Icons.slnx` | Exactly the two SPEC §3.4 entries for this item — 7 of the 8 end-state `<Project>` entries now present; only the gallery remains. |
| `docs/roadmap.md` | FEATURE-3ADD → `DONE`. |
| `docs/plan/FEATURE-3ADD.md` | Status → `DONE`; all 21 acceptance criteria ticked. |
| `CLAUDE.md` | Documentation sweep: the incremental-growth note now reads "seven of the eight end-state projects", lists the three new entries, and states that `dotnet pack` applies to all three packable projects. |
| `docs/SPEC.md` | Documentation sweep: §10.2's verified-API table row for `Render` corrected to `public` (deviation 1), and §12.3's `[AvaloniaTest]` corrected to `[AvaloniaFact]`/`[AvaloniaTheory]` with a note on the Avalonia 11 → 12 rename (deviation 2). No normative change — both are Avalonia-12 spelling corrections. |

Nothing else was touched: `Directory.Packages.props`, `Directory.Build.props`, `global.json`, the two
existing packages and their test projects are byte-identical to what FEATURE-3950 left.

## Deviations & follow-ups

1. **`Visual.Render` is `public` in Avalonia 12, not `protected`.** SPEC §10.2's verified-API table
   says `protected override void Render(DrawingContext)`; that is the Avalonia 11 signature, and an
   override may not narrow accessibility (CS0507). `Icon.Render` is therefore `public override`, with
   a one-line XML-doc remark citing the reason. Mechanical, not a design change — and it is what makes
   deviation 3's precise render assertions possible. **SPEC §10.2's table row was corrected in this
   dev's documentation sweep.**

2. **The headless attribute is `[AvaloniaFact]`, not `[AvaloniaTest]`.** Avalonia 12.0.4's
   `Avalonia.Headless.XUnit` exposes `AvaloniaFactAttribute` and `AvaloniaTheoryAttribute`; there is
   no `AvaloniaTestAttribute` in the assembly. The plan's and SPEC §12.3's `[AvaloniaTest]` spelling
   is the Avalonia 11 name. The acceptance criterion behind it holds exactly as intended: **zero**
   `[Fact]`/`[Theory]` in the project — all 65 tests are `[AvaloniaFact]`/`[AvaloniaTheory]`, verified
   by grep. **SPEC §12.3 was corrected in this dev's documentation sweep.**

3. **`DrawingContext` cannot be subclassed outside Avalonia 12**, so the render pass is recorded with
   Avalonia's own context instead of a hand-rolled one. One of its abstract members is
   `DrawBitmap(IRef<IBitmapImpl>, …)`, and `IRef<T>` is `internal` — an external subclass fails with
   CS0122/CS0534. `RenderRecorder` therefore renders into `DrawingGroup.Open()` and reads the
   resulting tree back. This is strictly better than the plan's fallback: the recorded tree carries
   real Avalonia semantics (a pushed transform, opacity or clip each becomes a nested `DrawingGroup`),
   so the layer walk, the `Matrix`, the 0.2 opacity group and the `UniformToFill` clip are all
   asserted directly rather than inferred. The plan's `InternalsVisibleTo` fallback was consequently
   **not** needed for the never-throw contract — but `InternalsVisibleTo` is kept and earns its place:
   `LayerPaintTests` exercises the shared `TryGetLayerPaint` contract directly.

4. **OPEN QUESTION resolved — a non-null `IconLayer.Stroke` is parsed.** The user chose the plan's
   proposal: `Brush.Parse(layer.Stroke)` with a silent fallback to the supplied brush on
   `FormatException`/`ArgumentException`, never throwing. The deliberate asymmetry with fills — where
   the consumer's brush always wins and `layer.Fill` is ignored — stands, and both branches have
   tests.

5. **SPEC §10.1's "`Stroke` is null → default to the supplied brush" is unreachable through
   `IsStroked`.** `IconLayer.IsStroked` is `Stroke is not null && Stroke != "none"` (SPEC §4.2), so a
   layer with a null stroke is never stroked in the first place, and the parser normalizes
   `stroke="currentColor"` to null. The null branch is implemented anyway — defensively, with a
   comment — so the code and the SPEC agree. **Follow-up (informational only):** if a
   `stroke="currentColor"` SVG should render as a *stroked* layer taking the consumer's brush,
   that is a change to `Enigma.Icons`' model semantics (FEATURE-24DD), not to this renderer.

6. **Render invalidation is not asserted; measure invalidation is.** `AffectsRender` exposes no public
   observable flag, so — as the plan anticipated — it is verified by the static-constructor
   registration plus FEATURE-469B's gallery, not by a reflection test. `AffectsMeasure` *is* asserted
   exactly, through `IsMeasureValid` going false after a `Size` or `Stretch` change. This is the one
   SPEC §12.3 sub-clause ("`Kind`/`Weight`/`Foreground`/`Size`/`Stretch` changes invalidate
   render/measure") not fully covered by a test.

7. **`Foreground`'s effective default is black, not null.** `AddOwner` inherits
   `TextElement.ForegroundProperty`'s default, so a bare `<ei:Icon/>` is visible rather than invisible.
   SPEC §10.2 states only what happens *when* `Foreground` resolves to null (paint nothing), which is
   implemented and tested with an explicit null.

8. **The single-`xmlns` claim is proven at compile time by FEATURE-469B, as the plan directs.** The
   two `XmlnsDefinition` attributes are present in the built assembly (verified in the Release
   binary); no `Avalonia.Markup.Xaml.Loader` was added for a runtime XAML-load test, per the plan.

9. **`Stretch` on a non-square target is asserted structurally only.** The scaling maths, including
   the centring translate and the view-box origin subtraction, is asserted numerically for all four
   modes — but a sign error that passes arithmetic can still look wrong on screen. FEATURE-469B
   carries the matching by-eye criterion.

10. **Line endings:** nothing to report. Every file this dev created or modified is LF with a final
    newline, verified byte-wise; no CRLF churn was observed anywhere in the diff.

## Build/test evidence

**Avalonia coupled-set resolution (acceptance criterion 1).** The pinned **12.0.4** resolved on this
item's first restore — no fallback to 12.0.2 was needed, so `Directory.Packages.props` is untouched:

```
Restored src/Enigma.Icons.Avalonia/Enigma.Icons.Avalonia.csproj (in 550 ms).
Restored tests/Enigma.Icons.Avalonia.UnitTests/Enigma.Icons.Avalonia.UnitTests.csproj (in 935 ms).
```

Resolved from `tests/.../obj/project.assets.json`, for both `net8.0` and `net10.0`:

| Package | Resolved | Note |
|---|---|---|
| `Avalonia` | 12.0.4 | already in the local cache |
| `Avalonia.Headless` | 12.0.4 | pulled transitively |
| `Avalonia.Headless.XUnit` | 12.0.4 | **fetched from nuget.org** — only 12.0.2 was cached |
| `xunit.v3` | 3.2.2 | |

`Avalonia.Headless.XUnit` **12.0.4**'s own nuspec — re-verified as the plan requires, on the version
that actually restored rather than on the 12.0.2 fallback — declares, for both `net8.0` and `net10.0`:

```xml
<dependency id="xunit.v3.extensibility.core" version="3.2.2" exclude="Build,Analyzers" />
<dependency id="Avalonia.Headless" version="12.0.4" exclude="Build,Analyzers" />
```

It is xUnit **v3**-native, so there is no v2/v3 conflict with `xunit.v3` 3.2.2.

**TFM justification (recorded as the plan requires).** `~/.nuget/packages/avalonia/12.0.4/lib/` holds
exactly `net8.0` and `net10.0` — no `netstandard2.0` asset — which is why this package alone drops the
`netstandard2.0` target.

**Build — `dotnet build Enigma.Icons.slnx -c Release`:**

```
  Enigma.Icons.Avalonia -> src/Enigma.Icons.Avalonia/bin/Release/net10.0/Enigma.Icons.Avalonia.dll
  Enigma.Icons.Avalonia -> src/Enigma.Icons.Avalonia/bin/Release/net8.0/Enigma.Icons.Avalonia.dll
  Enigma.Icons.Avalonia.UnitTests -> tests/.../bin/Release/net10.0/Enigma.Icons.Avalonia.UnitTests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)
```

Both TFMs, with `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`, `GenerateDocumentationFile`
(CS1591) and the trim/AOT analysers (IL2xxx/IL3xxx) all active.

**Tests — `dotnet test --solution Enigma.Icons.slnx`:**

```
Test run summary: Passed!
  total: 434
  failed: 0
  succeeded: 434
  skipped: 0
```

434 = 65 new (`Enigma.Icons.Avalonia.UnitTests`) + the 369 existing tests of FEATURE-24DD and
FEATURE-3950, all still green.

**Pack smoke test — `dotnet pack -c Release`** (verification only; the artefacts were discarded and
`GeneratePackageOnBuild` is absent, so packing stays the release step's job):

```
Successfully created package 'Enigma.Icons.Avalonia.1.0.0.nupkg'.
Successfully created package 'Enigma.Icons.Avalonia.1.0.0.snupkg'.
```

Contents confirm the SPEC §14.2 decision — `README.md` and `LICENSE.md` are packed, and
`THIRD-PARTY-NOTICES.md` is **not**:

```
lib/net10.0/Enigma.Icons.Avalonia.dll   lib/net10.0/Enigma.Icons.Avalonia.xml
lib/net8.0/Enigma.Icons.Avalonia.dll    lib/net8.0/Enigma.Icons.Avalonia.xml
README.md                               LICENSE.md
```

The nuspec carries the SPEC §10.5 URLs, no `<icon>`, no `<releaseNotes>`, and exactly two
dependencies per TFM group — `Enigma.Icons.Phosphor` 1.0.0 and `Avalonia` 12.0.4, with **no** direct
`Enigma.Icons` edge, as SPEC §10 specifies.

**Assembly attributes.** Both `XmlnsDefinition` attributes are present in the Release binary
(`XmlnsDefinitionAttribute` + the `https://github.com/josueclement/Enigma.Icons` URI).

**No XAML.** The package contains no `.axaml` or `.xaml` file of any kind, which is the whole point of
deriving `Icon` from `Control`.
