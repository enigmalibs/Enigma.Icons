**Status:** TODO · Single-phase · Suggested build branch `feature/feature-6fa1-wpf-renderer`

# FEATURE-6FA1 — Enigma.Icons.Wpf renderer package — deferred to post-1.0

## Objective

Record, while the 1.0.0 planning context is fresh, what was already decided *for* a WPF sibling
renderer, so a later builder does not have to re-derive it. The eventual deliverable is a fourth
package, `Enigma.Icons.Wpf`, rendering the existing framework-agnostic glyph model with
`System.Windows.Media` — the WPF analogue of `Enigma.Icons.Avalonia` (SPEC §10).

## Context

**This item is DEFERRED to post-1.0. It is NOT part of the 1.0.0 release, and this plan holds no
code.** Per SPEC §0 and §17 it is the one package in the table with `v1.0.0 = no`; the roadmap
sequences it last, after FEATURE-74DC has released the three 1.0.0 packages.

It can be deferred *safely* because the shipped packages already satisfy it — the load-bearing
decision this placeholder exists to preserve. `Enigma.Icons` and `Enigma.Icons.Phosphor` target
`netstandard2.0;net8.0;net10.0`, and that `netstandard2.0` floor exists **precisely** so .NET
Framework 4.6.2+ WPF apps can consume them (SPEC §0, §15 "Compatibility", §17). The model is
framework-agnostic by construction (SPEC §4–§6): raw-string paint values, no framework colour or
geometry type in the public surface, no default interface members / `required` / `init` / `Span<T>`.
Therefore **this item requires no change to any shipped package** — it is purely additive.

WPF also cannot be built, run, or tested on the planning machine (Linux), which is the other reason
it is deferred rather than attempted.

## Scope

### In scope
- `src/Enigma.Icons.Wpf/` — TFMs **`net462;net8.0-windows;net10.0-windows`**, packable, with a
  `ProjectReference` to `Enigma.Icons.Phosphor` (bringing `Enigma.Icons` transitively).
- The SPEC §10 surface mapped to WPF: extension methods, the `Icon` control, two markup extensions.
- `tests/Enigma.Icons.Wpf.UnitTests/` — `xunit.v3`, mirroring SPEC §12.3's shape.
- Packed `src/Enigma.Icons.Wpf/README.md`, a root-README package row, a `RELEASENOTES.md` section.

### Out of scope
- **Any change to the three 1.0.0 packages.** If this item needs one, that is a defect in this plan.
- Re-embedding the artwork — the whole reason for the three-package split (SPEC §0) is that this
  package *references* `Enigma.Icons.Phosphor` and adds ~40 KB — not the ≈1.15 MB of deflated
  resources per `lib/` folder (≈3.4 MB across this package's three WPF TFMs) that re-embedding the
  artwork would cost (SPEC §7.3).
- A **WPF gallery sample** (`samples/Enigma.Icons.Wpf.Gallery`) — a separate, later work item.
- Trim/AOT annotations: unlike SPEC §10.4, WPF is neither trimmable nor AOT-compatible, so do **not**
  set `IsTrimmable`/`IsAotCompatible`.
- Any WinUI / MAUI / Uno sibling.

## Design

SPEC §2 rules apply unchanged (LF + final newline, zero-warning `TreatWarningsAsErrors` build,
`ImplicitUsings` disabled, CPM with no `Version=` on `PackageReference`).

1. **Re-validate this plan** (see Notes / risks) and secure a Windows build machine.
2. **`src/Enigma.Icons.Wpf/Enigma.Icons.Wpf.csproj`** — the three TFMs above; `UseWPF true` for the
   `net*-windows` TFMs only, since `UseWPF` is not supported on .NET Framework: `net462` needs a
   TFM-conditioned `<Reference Include="PresentationCore|PresentationFramework|WindowsBase|System.Xaml" />`
   group. Verify at build time. Packs `LICENSE.md` (SPEC §14.1) but **not**
   `THIRD-PARTY-NOTICES.md` — it carries no artwork (SPEC §14.2).
3. **`IconGlyphExtensions.cs`** — SPEC §10.1's contract in WPF types, `IIconSet`-agnostic so
   `SvgIconSet` works too:
   ```csharp
   public static Geometry     ToGeometry(this IconGlyph glyph);              // GeometryGroup if multi-layer; opacity LOST
   public static Drawing      ToDrawing(this IconGlyph glyph, Brush brush);  // DrawingGroup, per-layer opacity kept
   public static DrawingImage ToDrawingImage(this IconGlyph glyph, Brush brush);
   ```
   Same layer walk, caveats, and null guards as SPEC §10.1. Two WPF deltas: the parse entry point is
   `System.Windows.Media.Geometry.Parse(string)`, and **fill-rule handling differs** — WPF's path
   mini-language defaults to `EvenOdd`, so an `IconFillRule.NonZero` layer needs an explicit `F1`
   prefix. `Freeze()` cached `Freezable`s for cost and cross-thread safety.
4. **`Icon.cs`** — derives from **`FrameworkElement`**, overriding `MeasureOverride` and `OnRender`.
   That is the WPF equivalent of SPEC §10.2's "no `<StyleInclude>` needed" property: a bare
   `FrameworkElement` needs no theme resource dictionary, no `Generic.xaml`, and the package ships
   zero XAML. Dependency properties mirror SPEC §10.2 one-for-one (`Kind`, `Weight`, `IconSet`,
   `IconName`, `Variant`, `Foreground`, `Size` default 16, `Stretch`) with
   `FrameworkPropertyMetadataOptions.AffectsRender`, plus `AffectsMeasure` for `Size`/`Stretch`.
   `Foreground` is **`TextElement.ForegroundProperty.AddOwner(typeof(Icon), …)`** — the counterpart
   of Avalonia's `AddOwner<Icon>()`, giving identical inheritance from an enclosing
   `Button`/`MenuItem`/`TextBlock` scope and identical theme-switch following. Invariants carry over
   verbatim: never throw from `OnRender` or a property change, null `Foreground` or missing glyph
   paints nothing, explicit `Width`/`Height` beats `Size`. SPEC §15 accessibility is nearly free —
   a plain `FrameworkElement` exposes no automation peer unless `OnCreateAutomationPeer` is
   overridden, so it is decorative by default; still set `Focusable = false`.
5. **`Markup/IconGeometryExtension.cs`**, **`Markup/IconImageExtension.cs`** — SPEC §10.3's shape on
   `System.Windows.Markup.MarkupExtension`, `ProvideValue` → `Geometry` / `DrawingImage`, `Brush`
   defaulting to `Brushes.Black`. Keep the `IconImage` name; §10.3's reasoning holds in WPF.
6. **XAML namespace** — WPF has no `using:` syntax, so add assembly-level
   `[assembly: XmlnsDefinition(…)]` for `Enigma.Icons.Wpf` and `…Wpf.Markup` so consumers write a
   clean `xmlns:ei=` rather than `clr-namespace:…;assembly=…`. The chosen URI is a public contract
   once shipped — document it in the packed README.
7. **`tests/Enigma.Icons.Wpf.UnitTests/`** — `xunit.v3`, `OutputType Exe`, MTP runner (SPEC §12),
   `net10.0-windows` only. Cover SPEC §12.3's checklist translated to WPF, once the harness open
   question below is resolved.
8. **`Enigma.Icons.slnx`** — append `src/Enigma.Icons.Wpf/Enigma.Icons.Wpf.csproj` to `/src/` and
   `tests/Enigma.Icons.Wpf.UnitTests/Enigma.Icons.Wpf.UnitTests.csproj` to `/tests/`. SPEC §3.4
   settles this explicitly: its eight-entry end state (reached by FEATURE-469B) is final **for the
   1.0.0 line**, and this deferred item is the named post-1.0 extension of the incremental-growth
   table — so growing the slnx to ten entries here is expected and is **not** a contradiction of
   FEATURE-469B's "end state". No new `Directory.Packages.props` pins — WPF is in-box and `xunit.v3`
   is already pinned (SPEC §3.3).
9. **Release** — a **new package at its own 1.0.0**, released by this item (or its own release item),
   **not** by FEATURE-74DC. The `ProjectReference` becomes a NuGet dependency on the then-current
   `Enigma.Icons.Phosphor` version; confirm that version is published before pushing.

## Dependencies & ordering

- Hard prerequisites: **FEATURE-3950** (the `PhosphorIcon`/`PhosphorWeight`/`PhosphorIconSet` surface
  it exposes) and **FEATURE-3ADD** (the Avalonia renderer it mirrors — read it first so the two
  renderers stay behaviourally identical).
- Sequenced **after FEATURE-74DC** (SPEC §16 map; roadmap "Sequencing" item 9): 1.0.0 ships first.
- Blocks nothing. A future WPF gallery sample would depend on this item.

## Acceptance criteria

These are the criteria a **future** build must meet; nothing here is verifiable today, and this plan
stays `TODO` until the item is actually built.

- [ ] Built on a **Windows** machine (Linux cannot compile WPF).
- [ ] `dotnet build Enigma.Icons.slnx -c Release` succeeds with **zero warnings** across all three
      WPF TFMs.
- [ ] `dotnet test Enigma.Icons.slnx` — the **whole suite** green, base/Phosphor/Avalonia projects
      included (proof this item changed nothing they depend on).
- [ ] **No file under `src/Enigma.Icons/`, `src/Enigma.Icons.Phosphor/`, or
      `src/Enigma.Icons.Avalonia/` modified** (`git diff --stat` proves it).
- [ ] `dotnet pack` yields `Enigma.Icons.Wpf.1.0.0.nupkg` depending on `Enigma.Icons.Phosphor`, with
      **no embedded artwork** (no `.dat` inside the nupkg).
- [ ] A hand-run WPF smoke app renders `Icon` in all six weights, inherits `Foreground` from an
      enclosing `Button`, and survives a brush/theme change without throwing.
- [ ] Packed `src/Enigma.Icons.Wpf/README.md` written (SPEC §13 pattern), stating the `ToGeometry`
      opacity caveat and the "no resource dictionary needed" note; root README row and
      `RELEASENOTES.md` section added.
- [ ] **Roadmap + this plan flipped to `DONE`** (`docs/roadmap.md` FEATURE-6FA1 row; this file's
      status header). (DoD criterion 4.)
- [ ] **Completion doc `docs/done/FEATURE-6FA1.md` written** — summary, files touched, deviations,
      build/test evidence. (DoD criterion 5.)

## Notes / risks

- **Re-validate before building.** Written at 1.0.0 planning time (2026-07-25) against the
  then-current Avalonia/WPF and .NET landscape. Re-check first: the shipped `Enigma.Icons` public
  surface (this design assumes it is unchanged), the SDK's WPF TFM story, whether `net462` is still
  worth supporting, and whether SPEC §10 still describes the Avalonia renderer as shipped. Every
  "verify at build time" marker above is mandatory.
- **OPEN QUESTION — WPF test harness.** WPF has no `Avalonia.Headless` equivalent and the SPEC does
  not address it. The later builder must choose and record: (a) an xUnit fixture pumping a dedicated
  **STA thread** with a `Dispatcher`, exercising the real control through
  `Measure`/`Arrange`/`RenderTargetBitmap`; or (b) testing only the **non-visual** surface
  (`IconGlyphExtensions`, `ProvideValue`, DP metadata/defaults), leaving `OnRender` and `Foreground`
  inheritance to the smoke app. (b) is cheaper; (a) is what SPEC §12.3 parity actually demands.
- **OPEN QUESTION — cross-platform builds.** *Whether* this item extends the slnx is settled (SPEC
  §3.4 names it as the post-1.0 extension; see step 8). What is still open is that the SPEC never
  says whether the solution must stay buildable on Linux, and adding Windows-only projects makes a
  full-solution Linux build fail. Options: accept it (repo becomes Windows-primary), keep the WPF
  projects out of the slnx, or add a second solution file. The SPEC should be amended with the
  answer.
- **`net462` narrows the surface further than `netstandard2.0`.** `LangVersion 14` compiles, but
  features needing runtime/BCL support (index/range, `Span<T>`, generic math,
  `[CallerArgumentExpression]`, `ValueTuple` without a package) are not free. Avoid them internally
  too, or justify a polyfill package in a csproj comment (SPEC §3.3).
- **Fill-rule and geometry-parse differences** (step 3) are the likeliest source of visual divergence
  between the two renderers; the 8 multi-layer `fill` icons and all duotone icons (SPEC §7.4) are the
  cases to eyeball in the smoke app.
- **Do not "improve" the `Icon` API here.** If WPF suggests something better than SPEC §10, propose it
  for *both* renderers rather than letting them drift.
