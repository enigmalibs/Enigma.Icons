**Status:** DONE · Single-phase · Suggested build branch `feature/feature-3add-avalonia-renderer`

# FEATURE-3ADD — Enigma.Icons.Avalonia + UnitTests

## Objective

Build the third shipping package, `Enigma.Icons.Avalonia`: the rendering layer that turns an
`IconGlyph` (from **any** `IIconSet`) into Avalonia `Geometry` / `Drawing` / `DrawingImage`, plus the
`Icon` control and the two XAML markup extensions — everything specified in SPEC §10. Ship it with a
packed README and a headless xUnit v3 test project covering every bullet of SPEC §12.3. After this
item the solution can actually put a Phosphor icon on screen; the gallery (FEATURE-469B) is the
visual proof.

## Context

FEATURE-3950 has landed, so `PhosphorIconSet.Instance`, `PhosphorIcon`, and `PhosphorWeight`
(SPEC §9) exist and are corpus-tested. This item consumes them; it adds **no** icon data and **no**
parsing (the built-in set never touches XML — SPEC §9.1).

Two design commitments from the interview drive the whole item and must not be quietly relaxed:

1. **`Icon` derives from `Control`, not `TemplatedControl`** (SPEC §10.2). The consumer therefore
   needs **no `<StyleInclude>` in `App.axaml`**, the package ships **no XAML**, and there is no
   theme-resource key to get wrong. This is also the capability the retired `PhosphorIconsAvalonia`
   structurally could not have: a markup extension is evaluated once at load time and can never
   follow a bound brush or a theme switch.
2. **The renderer is framework-agnostic in its input.** `IconGlyphExtensions` takes an `IconGlyph`,
   so a `SvgIconSet` built from the consumer's own `.svg` files renders exactly like Phosphor. The
   `Icon` control exposes both paths (`Kind`/`Weight` and `IconSet`/`IconName`/`Variant`).

This is the only package in the solution with a **third-party** NuGet dependency (`Avalonia`) and the
only one that cannot target `netstandard2.0`. (`Enigma.Icons` has zero dependencies at all;
`Enigma.Icons.Phosphor` declares exactly one package dependency, the sibling `Enigma.Icons`, which is
not third-party — SPEC §2 rule 10.) It carries no third-party artwork, so it does **not** pack
`THIRD-PARTY-NOTICES.md` (SPEC §14.2).

## Scope

### In scope
- `src/Enigma.Icons.Avalonia/` — csproj, `IconGlyphExtensions.cs`, `Icon.cs`,
  `Markup/IconGeometryExtension.cs`, `Markup/IconImageExtension.cs`, packed `README.md`
  (SPEC §10.1–§10.4).
- `tests/Enigma.Icons.Avalonia.UnitTests/` — `Avalonia.Headless.XUnit` + `xunit.v3`, every bullet of
  SPEC §12.3.
- Appending this item's two `<Project>` entries to `Enigma.Icons.slnx` (SPEC §3.4 table).

### Out of scope (owned elsewhere)
- **The gallery sample** — FEATURE-469B (SPEC §11). No `samples/` work here, and the slnx gets no
  gallery entry.
- **Final README polish** — FEATURE-718F owns the shipped wording of the packed READMEs (SPEC §13).
  This item writes a real, complete-enough README because `PackageReadmeFile` points at it; 718F
  refines it (and adds gallery screenshots).
- **`<PackageReleaseNotes>`, version finalization, `RELEASENOTES.md` bodies, `docs/RELEASE.md`** —
  FEATURE-74DC (SPEC §16).
- **WPF rendering** — FEATURE-6FA1, deferred post-1.0 (SPEC §17). Do not add a shared "renderer
  helpers" abstraction speculatively; the framework-agnostic split already lives in `Enigma.Icons`.
- **A DI helper (`AddEnigmaIcons()`)**, visual/bitmap regression baselines, icon metadata — all
  explicitly not selected (SPEC §17).
- **Any change to `Directory.Packages.props`** — the Avalonia group is already pinned there (SPEC §3.3)
  and this item does not re-decide the version. The only permitted move is the whole-group fallback
  described in the test-project bullet of Design step 7, applied to the whole coupled set at once
  (SPEC §3.3 coupled-set rule).

## Design

Numbered in build order. All paths are repo-relative. LF + final newline on every file, zero-warning
build, `ImplicitUsings` disabled with explicit per-file usings, no `Version=` on any
`PackageReference` — per SPEC §2.

### 1. `src/Enigma.Icons.Avalonia/Enigma.Icons.Avalonia.csproj`

- `TargetFrameworks` = `net8.0;net10.0` — **not** the `netstandard2.0;net8.0;net10.0` of the other
  two packages. This is fixed by SPEC §0's TFM table; the reason is that Avalonia 12 ships **only**
  `net8.0` and `net10.0` lib assets, so `netstandard2.0` cannot resolve the dependency at all. Verify
  version-agnostically by listing the `lib/` folder of the `Avalonia` package at the version pinned in
  `Directory.Packages.props` (SPEC §3.3), and record the observed TFM list in the completion doc.
- `OutputType Library`, plus `ImplicitUsings disable` — which is **solution-wide** per SPEC §2 rule 2
  and §3.2, in *every* csproj packable or not, and is deliberately **not** part of §10.4's packable
  table — plus the **SPEC §10.4 packable property set** in full: `GenerateDocumentationFile true` — set **per packable project**, because SPEC §10.4 keeps
  it deliberately out of `Directory.Build.props` so the test and gallery projects do not inherit it,
  and with `TreatWarningsAsErrors` it is what makes CS1591 a build error (SPEC §2 rule 8);
  `IncludeSymbols true` + `SymbolPackageFormat snupkg`; `PackageReadmeFile` / `PackageLicenseFile`;
  and **no `GeneratePackageOnBuild`** (absent, or explicitly `false` — packing is the release step's
  job, never every local build). `LangVersion`, `Nullable`, `TreatWarningsAsErrors`,
  `EnforceCodeStyleInBuild` are inherited from `Directory.Build.props` — do not repeat them.
- Packable metadata in the **house shape**, matching the field set and ordering already used by
  `src/Enigma.Icons/Enigma.Icons.csproj` and `src/Enigma.Icons.Phosphor/Enigma.Icons.Phosphor.csproj`:
  `PackageId`, `Title`, `Version` (`1.0.0`), `Description`, `PackageTags`, `PackageReadmeFile`,
  `PackageLicenseFile`, `RepositoryUrl`, `PackageProjectUrl`, `RepositoryType`, `IncludeSymbols`,
  `SymbolPackageFormat snupkg`. The URL values are the canonical ones of SPEC §10.5, verbatim:
  `RepositoryUrl` and `PackageProjectUrl` are both `https://github.com/josueclement/Enigma.Icons` and
  `RepositoryType` is `git`; **no `PackageIcon`** on this or any package.
  **No `PackageReleaseNotes`** — FEATURE-74DC adds it. Tags should
  include the Avalonia-specific terms (`avalonia`, `avaloniaui`, `icons`, `phosphor`, `svg`, `xaml`,
  `markup-extension`) since this is the package an Avalonia user searches for.
- `<IsTrimmable>true</IsTrimmable>` and `<IsAotCompatible>true</IsAotCompatible>` in the
  **unconditional** `PropertyGroup`. This is not this plan's own reasoning — **SPEC §10.4 states it
  explicitly**: the two `netstandard2.0`-multi-targeting packages set both properties under
  `'$(TargetFramework)' != 'netstandard2.0'`, while `Enigma.Icons.Avalonia` sets them
  **unconditionally** because that condition is vacuous for its `net8.0;net10.0` TFM set. SPEC §10.4
  calls the asymmetry "intentional and SPEC-sanctioned" and directs that a release audit must **not**
  flag it as a deviation nor "fix" it by adding a no-op condition. Add a one-line csproj comment
  citing SPEC §10.4, so FEATURE-74DC's audit has an unambiguous reference rather than an apparent
  oversight.
- `<ItemGroup>`: `<PackageReference Include="Avalonia" />` (version from CPM, SPEC §2.5) and
  `<ProjectReference Include="..\Enigma.Icons.Phosphor\Enigma.Icons.Phosphor.csproj" />`. Do **not**
  add a second direct `ProjectReference` to `Enigma.Icons` — SPEC §10 specifies the Phosphor
  reference brings it transitively; a redundant direct edge is a second thing to keep in sync.
- Pack items: `<None Include="README.md" Pack="true" PackagePath="\" />` and
  `<None Include="..\..\LICENSE.md" Pack="true" PackagePath="\" />`. **Do not** pack
  `..\..\THIRD-PARTY-NOTICES.md` — this package ships no Phosphor artwork and no third-party code, so
  the MIT attribution obligation does not attach to it (SPEC §14.2). Put that reason in a csproj
  comment; it is the kind of omission a later reader "fixes".
- `<InternalsVisibleTo Include="Enigma.Icons.Avalonia.UnitTests" />` — the shared layer-walk helper
  (step 2) is `internal`, and the never-throw contract of `Icon.Render` may have to be asserted
  through it if the headless platform cannot be driven to a render pass (step 7). Keep it to this one
  assembly.

### 2. `src/Enigma.Icons.Avalonia/IconGlyphExtensions.cs`

`public static class IconGlyphExtensions` in namespace `Enigma.Icons.Avalonia`. The three public
signatures are **verbatim from SPEC §10.1** — do not vary names, order, or nullability.

1. **`ToGeometry`** — discriminate on `glyph.Layers.Count == 1`, **not** on `glyph.IsSingleLayer`.
   `IsSingleLayer` (SPEC §4.3) additionally requires opacity 1 and no stroke; a one-layer stroked or
   translucent glyph must still collapse to a single `Geometry.Parse(PathData)` rather than be boxed
   in a one-child group. Multi-layer → `GeometryGroup` with the parsed layers as children in paint
   order and `FillRule` mapped from **the first layer** (`IconFillRule.NonZero`/`EvenOdd` →
   Avalonia `FillRule.NonZero`/`EvenOdd`).
2. **The opacity-loss caveat is a documentation deliverable, not a remark.** It must appear (a) in the
   `<summary>`/`<remarks>` XML doc of `ToGeometry`, naming `ToDrawing` as the duotone-correct
   alternative, and (b) in `src/Enigma.Icons.Avalonia/README.md` (step 6). A `GeometryGroup` has no
   per-child opacity and one shared `FillRule`, so a duotone glyph collapsed this way paints both
   layers at full opacity — visually wrong, deliberately allowed, and asserted by a test (step 7) so
   it stays deliberate.
3. **`ToDrawing(glyph, brush)`** — root `DrawingGroup`; walk `glyph.Layers` in paint order (index 0
   first / bottom-most):
   - Skip a layer with `!IsFilled && !IsStroked` (the legitimate transparent spacer of SPEC §5.1's
     last row). Nothing is emitted for it — not an empty group.
   - Filled layer → `GeometryDrawing { Geometry = Geometry.Parse(layer.PathData), Brush = brush }`.
     The consumer's brush always wins over `layer.Fill`; the raw paint string is deliberately ignored
     for fills (SPEC §4.2 rationale — `currentColor` is already normalized to `null` by the parser).
   - Stroked layer → the same `GeometryDrawing` with a `Pen`: thickness `layer.StrokeWidth ?? 1.0`
     (the SVG default), `PenLineCap` from `IconLineCap` (`Flat`/`Round`/`Square`, default `Flat`),
     `PenLineJoin` from `IconLineJoin` (`Miter`/`Round`/`Bevel`, default `Miter`), brush = the
     supplied `brush` when `layer.Stroke` is `null` (SPEC §10.1). For a **non-null** `Stroke` string
     see the OPEN QUESTION in Notes / risks; the proposed default is `Brush.Parse` with a silent
     fallback to the supplied brush. A stroked-but-not-filled layer gets `Brush = null` on the
     drawing so it is outlined only.
   - `layer.Opacity < 1.0` → wrap **that layer's** drawing in its own
     `DrawingGroup { Opacity = layer.Opacity }` and add the wrapper to the root, because
     `GeometryDrawing` has no `Opacity` of its own. Layers at opacity 1 are added to the root
     directly (no wrapper) — the test asserts exactly this nesting shape.
4. **`ToDrawingImage(glyph, brush)`** — `new DrawingImage(glyph.ToDrawing(brush))`, nothing more.
5. Guards: `ArgumentNullException.ThrowIfNull` on glyph and brush (available on both TFMs — no
   `netstandard2.0` here). A `Geometry.Parse` failure **propagates**: malformed path data in a
   committed asset is a bug, not a runtime condition (SPEC §10.1).
6. Factor the per-layer paint decision into one `internal` helper so `Icon.Render` (step 3) cannot
   drift from `ToDrawing`. Suggested shape, pinning the reuse contract:

   ```csharp
   internal static bool TryGetLayerPaint(IconLayer layer, IBrush brush, out IBrush? fill, out IPen? pen);
   ```

   Returns `false` for a skipped layer. `ToDrawing` and `Render` both call it; only the emission
   differs (object graph vs. `DrawingContext` calls).

### 3. `src/Enigma.Icons.Avalonia/Icon.cs`

`public sealed class Icon : Control` in namespace `Enigma.Icons.Avalonia`. Properties, defaults, and
behaviour are **verbatim from SPEC §10.2**; the notes below are the build decisions that section
leaves to the implementer.

1. **Registration.** All `StyledProperty` fields are `public static readonly` with
   `AvaloniaProperty.Register<Icon, T>(nameof(X))`, except `ForegroundProperty`, which is
   `TextElement.ForegroundProperty.AddOwner<Icon>()`. Defaults per SPEC §10.2: `Weight = Regular`,
   `Size = 16`, `Stretch = Stretch.Uniform`, `IconSet`/`IconName`/`Variant` `null`. Note that
   `Kind`'s default is `default(PhosphorIcon)` = enum value 0 = `Acorn` — a bare `<ei:Icon/>` renders
   an acorn; that is the unavoidable consequence of a value-type default and is worth one line of XML
   doc rather than a workaround.
2. **Static constructor** performs, in this order: `AffectsRender<Icon>(...)` over all eight
   properties, `AffectsMeasure<Icon>(SizeProperty, StretchProperty)`, and
   `FocusableProperty.OverrideDefaultValue<Icon>(false)`. Field initializers run before the static
   constructor body, so `AddOwner` has already happened by then — but keep every registration inside
   `Icon`'s own static state and never let another type's static initializer be the first toucher
   (see Notes / risks).
3. **What `AddOwner` buys, and why it is the headline feature.** `TextElement.ForegroundProperty`
   is an inheriting attached property, so an `Icon` inside a `Button`, `MenuItem`, or any
   `TextElement` scope picks up that scope's foreground and follows a theme switch with **zero
   consumer binding**. Say this in the class `<remarks>` and in the README. When `Foreground`
   resolves to `null`, `Render` paints nothing (SPEC §10.2) — it does not fall back to black.
4. **`MeasureOverride(Size availableSize)`** returns `new Size(Size, Size)`, clamped to `>= 0` and
   guarded against `NaN`/infinity (fall back to 0, never throw). Do **not** hand-roll `Width`/`Height`
   precedence: `Layoutable.MeasureCore` already coerces the measured result with explicit
   `Width`/`Height`/`Min*`/`Max*`, so "explicit `Width`/`Height` wins over `Size`" falls out of the
   framework. Document that so nobody adds a second, conflicting rule.
5. **`Render(DrawingContext context)`**, in this order:
   1. `Foreground` is `null` → return.
   2. Resolve the glyph. `IconSet != null` **wins over** `Kind`/`Weight` (SPEC §10.2): require a
      non-empty `IconName`, then `IconSet.TryGetGlyph(IconName, Variant, out glyph)`. Otherwise
      `PhosphorIconSet.Instance.TryGetGlyph(Kind, Weight, out glyph)`. A miss → return (paints
      nothing). No local glyph cache: the sets already return cached, reference-equal instances
      (SPEC §6.1, §9.1), and a second cache is only an invalidation bug waiting to happen.
   3. Compute the viewBox→bounds transform from `glyph.ViewBox` and `Bounds.Size` per `Stretch`:
      `None` → scale 1,1; `Uniform` → `min(sx, sy)` on both axes; `UniformToFill` → `max(sx, sy)` on
      both; `Fill` → `sx`, `sy` independently, where `sx = bounds.Width / viewBox.Width` and
      likewise for y. Then centre the scaled content in the bounds and subtract the scaled viewBox
      origin: translate by `(-vb.X * sx + (bounds.Width - vb.Width * sx) / 2, …)`. Guard a zero or
      non-finite bounds dimension by returning early. For `UniformToFill` push a clip to
      `new Rect(Bounds.Size)` so an overflowing icon cannot paint over its neighbours (matching what
      `Image` does); no clip for the other three modes.
   4. Walk the layers with the **same** `TryGetLayerPaint` helper as `ToDrawing`, drawing directly
      into the `DrawingContext` — no intermediate `DrawingImage` per render (SPEC §10.2). Per-layer
      opacity `< 1` is applied with `PushOpacity`, the transform with `PushTransform`, each in a
      `using` so the pushed state is popped on every path.
   5. **Never throw.** Wrap the resolve-and-draw body in a `try`/`catch (Exception)` that returns
      silently, and comment *why*: a third-party `IIconSet` is arbitrary code, `Geometry.Parse` on
      third-party path data can fail, and the Avalonia XAML previewer/designer must stay alive — a
      throwing `Render` kills the preview surface for the whole window, not just the icon. No
      logging: SPEC §2.10 / §15 (observability: none, deliberately). The same never-throw rule
      applies to any property-changed handling.
6. **Accessibility (SPEC §10.2, §15).** `Focusable = false` via the static-constructor default
   override. Override `OnCreateAutomationPeer()` to return a private `IconAutomationPeer :
   ControlAutomationPeer` whose `GetAutomationControlTypeCore()` is `AutomationControlType.Image` and
   whose `IsContentElementCore()` returns `false` unless `AutomationProperties.GetName(Owner)` is
   non-empty — i.e. an icon is decoration a screen reader skips until the consumer names it.
7. Instance CLR properties are thin `GetValue`/`SetValue` wrappers, each with XML doc naming its
   default.

### 4. `src/Enigma.Icons.Avalonia/Markup/IconGeometryExtension.cs` and `Markup/IconImageExtension.cs`

Namespace `Enigma.Icons.Avalonia.Markup`; both `public sealed class … : MarkupExtension`
(`Avalonia.Markup.Xaml`); signatures **verbatim from SPEC §10.3**.

1. Each has a parameterless constructor **and** a positional one taking `PhosphorIcon`, so
   `{ei:IconGeometry Acorn}` works; the `Extension` suffix is what makes the short XAML name resolve.
2. Defaults: `Weight = PhosphorWeight.Regular` on both, `Brush = Brushes.Black` on `IconImage` — the
   latter deliberately preserves the retired package's behaviour (SPEC §10.3).
3. `ProvideValue` = `PhosphorIconSet.Instance.GetGlyph(Icon, Weight)` then `.ToGeometry()` /
   `.ToDrawingImage(Brush)`. Declared return type stays `object` per SPEC. `serviceProvider` is
   unused — that is fine and needs no suppression.
4. **Fail fast here, unlike the control** (SPEC §15): the throwing `GetGlyph` overload is correct for
   a markup extension because a bad `Kind`/`Weight` in XAML is an authoring error that should surface
   at load time. State this asymmetry with `Icon.Render` in the XML doc of both extensions so it does
   not look like an oversight.
5. **Why `IconImage`, not `IconSource`** (SPEC §10.3): it returns a `DrawingImage`, and Avalonia has
   its own unrelated `IconSource` concept — the retired package's `IconSourceExtension` name actively
   misled users into expecting an `IconSource`. Put that rationale in the class `<remarks>` and one
   line of the README, because the rename is the single most visible break from the old API.

### 5. `src/Enigma.Icons.Avalonia/Properties/AssemblyInfo.cs`

The single-XML-namespace wiring is **mandatory** per SPEC §10.3, and it is what makes one prefix reach
the control *and* both markup extensions. The whole file is two attributes:

```csharp
using Avalonia.Metadata;

[assembly: XmlnsDefinition("https://github.com/josueclement/Enigma.Icons", "Enigma.Icons.Avalonia")]
[assembly: XmlnsDefinition("https://github.com/josueclement/Enigma.Icons", "Enigma.Icons.Avalonia.Markup")]
```

1. The CLR namespaces do **not** move: the control stays in `Enigma.Icons.Avalonia`, the markup
   extensions stay in `Enigma.Icons.Avalonia.Markup` (SPEC §10, §10.3). The attributes map both onto
   the one XML namespace URI, so a consumer writes a single
   `xmlns:ei="https://github.com/josueclement/Enigma.Icons"` and gets `ei:Icon`,
   `{ei:IconGeometry …}` and `{ei:IconImage …}`. The URI follows the ecosystem convention of using the
   repository URL and matches SPEC §10.5's canonical URL.
2. `XmlnsDefinitionAttribute` lives in `Avalonia.Metadata` (`Avalonia.Base`) and is **verified present**
   in Avalonia 12 (SPEC §10.3) — no version literal needed here, the pin is in
   `Directory.Packages.props`.
3. The per-namespace `using:` forms (`using:Enigma.Icons.Avalonia` plus
   `using:Enigma.Icons.Avalonia.Markup`) keep working and are documented as the **fallback** in the
   README (step 6).
4. This file declares no namespace of its own, so `using Avalonia.Metadata;` resolves globally and the
   shadowing trap in Notes / risks does not bite. SPEC §10.3 also notes that
   `dotnet_style_namespace_match_folder` is **suggestion** severity in the house `.editorconfig`, so the
   `Properties/` folder cannot fail the build.
5. Proof is **compile-time**: FEATURE-469B's gallery XAML uses the single `ei` prefix for the control
   and both extensions. Do **not** add `Avalonia.Markup.Xaml.Loader` for a runtime XAML-load test — it
   is not pinned in SPEC §3.3 and must not be added for this.

### 6. `src/Enigma.Icons.Avalonia/README.md` (packed)

Initial complete version; FEATURE-718F owns the final polish and screenshots (SPEC §13). Contents:

- Title and a one-paragraph intro. **No badges** — per SPEC §13.1 badges live in the root `README.md`
  only; a badge in a packed README is wrong.
- Install line, and the TFM note (`net8.0`/`net10.0`, because Avalonia 12 ships no `netstandard2.0`
  asset).
- **XAML quick start** documenting the single
  `xmlns:ei="https://github.com/josueclement/Enigma.Icons"` form as **primary** — it reaches the control
  and both markup extensions thanks to step 5's `XmlnsDefinition` attributes — with the per-namespace
  `using:Enigma.Icons.Avalonia` + `using:Enigma.Icons.Avalonia.Markup` pair shown as the documented
  **fallback** (SPEC §10.3). Then the `<ei:Icon …>`, `<Path Data="{ei:IconGeometry …}">`, and
  `<Image Source="{ei:IconImage …}">` snippets of SPEC §10.3, plus the C# `ToGeometry()` snippet.
- **Control vs. markup extension guidance:** use `Icon` whenever the brush is bound, themed, or
  inherited, and whenever the icon must follow a theme switch; use the markup extensions for a
  one-shot static `Geometry`/`DrawingImage` (they are evaluated once at load time and never update).
- **The `ToGeometry` opacity caveat** — one short section, with "use `Icon` or `ToDrawing` for
  duotone".
- **The no-`StyleInclude` note:** the package ships no XAML and no theme resources; nothing goes into
  `App.axaml`.
- A "bring your own SVG" paragraph pointing at `IconSet`/`IconName`/`Variant` with `SvgIconSet`.
- Links to the root README and `LICENSE.md`.

### 7. `tests/Enigma.Icons.Avalonia.UnitTests/`

- **csproj** — `net10.0`, `OutputType Exe`, `ImplicitUsings disable`, `IsPackable false` (the house
  shape used by the two existing test projects); `<PackageReference Include="xunit.v3" />` and
  `<PackageReference Include="Avalonia.Headless.XUnit" />`; `ProjectReference` to
  `..\..\src\Enigma.Icons.Avalonia\Enigma.Icons.Avalonia.csproj`. No `Microsoft.NET.Test.Sdk`, no
  `xunit.runner.visualstudio`, no coverlet (SPEC §12).
  Both versions come from CPM — the Avalonia group is pinned in `Directory.Packages.props` (SPEC §3.3),
  and no version literal belongs in this plan or in the csproj.
  **The one version note this item carries:** SPEC §3.3 authorizes a **whole-group** fallback to
  Avalonia **12.0.2** — and only that, applied to the entire coupled set at once, never to one package —
  **if and only if** `Avalonia.Headless.XUnit` at the pinned version fails to resolve on the first
  restore. As **observed on that 12.0.2 fallback package**, `Avalonia.Headless.XUnit`'s nuspec declares
  `xunit.v3.extensibility.core` **3.2.2**, i.e. it is xUnit v3-native and there is **no v2/v3
  conflict**; re-verify that dependency against whichever version actually restores and record it in
  the completion doc.
- **`TestAppBuilder.cs`** — the `[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]`
  attribute plus `public static AppBuilder BuildAvaloniaApp()` returning
  `AppBuilder.Configure<Application>().UseHeadless(new AvaloniaHeadlessPlatformOptions())`. Plain
  `Application`, no Fluent theme: nothing under test is templated.
- **Every test method uses `[AvaloniaTest]`** (or `[AvaloniaTheory]`), never `[Fact]`/`[Theory]` —
  see the `Geometry.Parse` risk below. Grep for `[Fact]` in this project as a check.
- **`TestGlyphs.cs`** — a small factory of synthetic `IconGlyph` values so the shape assertions do
  not depend on the Phosphor corpus: single filled layer; two layers with the first at opacity 0.2
  (duotone shape); a `fill="none"` unstroked spacer layer; a stroked layer with known
  width/cap/join; an `evenodd` layer. A couple of tests additionally use `PhosphorIconSet.Instance`
  for the real `Kind`/`Weight` path.
- **Test files → SPEC §12.3 bullets:**
  - `IconGlyphExtensionsGeometryTests.cs` — `ToGeometry` single-layer parseable with non-empty
    bounds; multi-layer child count and first-layer `FillRule`; the opacity-loss behaviour asserted
    explicitly (a 0.2 layer's opacity appears nowhere in the returned `GeometryGroup`).
  - `IconGlyphExtensionsDrawingTests.cs` — `ToDrawing` layer count and nesting; the 0.2 layer wrapped
    in a `DrawingGroup` with `Opacity == 0.2` while an opaque layer is **not** wrapped; skipped
    spacer layer; stroked layer produces a `Pen` with the expected thickness/cap/join;
    `ToDrawingImage` returns a `DrawingImage` whose `Drawing` matches `ToDrawing`; null-argument
    guards on all three.
  - `MarkupExtensionTests.cs` — `ProvideValue` returns `Geometry` / `DrawingImage`; positional
    constructor sets `Icon`; `Weight` defaults to `Regular`; `Brush` defaults to `Brushes.Black`.
  - `IconControlTests.cs` — default `Size == 16`; `MeasureOverride` honours `Size` and is overridden
    by explicit `Width`/`Height`; `Size`/`Stretch` change ⇒ `IsMeasureValid == false` after a
    measure (the public, exact way to assert `AffectsMeasure`); duotone draws two layers;
    `IconSet` + `IconName` resolves against a `SvgIconSet.FromSvgSources` set and wins over
    `Kind`/`Weight`; **a missing glyph and a `null` Foreground both render without throwing**;
    `Foreground` inherits (put the `Icon` in a `Border` carrying `TextElement.Foreground` inside a
    shown `Window` and assert `icon.Foreground`); `Focusable` is `false`.
- **Driving a real render pass.** For the never-throw and layer-count assertions: `[AvaloniaTest]`,
  `new Window { Content = icon }`, `window.Show()`, `Dispatcher.UIThread.RunJobs()`, then
  `AvaloniaHeadlessPlatform.ForceRenderTimerTick()`. Verify these APIs exist as named in
  `Avalonia.Headless` at the version pinned in `Directory.Packages.props` (SPEC §3.3) before relying on
  them. If the headless platform in that version cannot
  be driven to invoke `Render`, fall back to asserting the layer walk through the `internal`
  `TryGetLayerPaint` helper (hence `InternalsVisibleTo`, step 1) plus `ToDrawing`, and record the
  chosen mechanism in the completion doc. Do **not** add a Skia/frame-capture package to reach a
  pixel buffer — bitmap baselines are explicitly out of scope (SPEC §17).
- `AffectsRender` has no public observable flag. Assert measure invalidation exactly (above), and
  treat render invalidation as verified by the static-constructor registration plus the gallery
  (FEATURE-469B). Note this limit in the completion doc rather than inventing a reflection test.

### 8. `Enigma.Icons.slnx` — append this item's entries only

Per the SPEC §3.4 incremental-growth contract, append exactly two lines and nothing else:

```xml
<!-- inside <Folder Name="/src/"> -->
<Project Path="src/Enigma.Icons.Avalonia/Enigma.Icons.Avalonia.csproj" />
<!-- inside <Folder Name="/tests/"> -->
<Project Path="tests/Enigma.Icons.Avalonia.UnitTests/Enigma.Icons.Avalonia.UnitTests.csproj" />
```

After this item the slnx holds **7 of the 8** end-state `<Project>` entries (SPEC §3.4); only the
gallery (FEATURE-469B) remains. Do not pre-add it — the slnx must never reference a project that does
not exist.

### 9. Verify

`dotnet build Enigma.Icons.slnx` (zero warnings, both TFMs of the new package), then
`dotnet test Enigma.Icons.slnx` — the whole suite, i.e. this project plus the FEATURE-24DD and
FEATURE-3950 projects. Capture both outputs as build/test evidence for the completion doc. Confirm that
`Avalonia` at the version pinned in `Directory.Packages.props` (SPEC §3.3) restores for both `net8.0`
and `net10.0`, and record the resolved version in the completion doc rather than asserting a literal
here.

## Dependencies & ordering

- **Depends on FEATURE-3950** (`PhosphorIconSet`, `PhosphorIcon`, `PhosphorWeight` — SPEC §9) and
  transitively on FEATURE-2DDE (the generated assets), FEATURE-24DD (the model, `IIconSet`,
  `SvgIconSet`), and FEATURE-21C4 (repo, slnx, shared config). Roadmap sequencing item 5; SPEC §16.
- **Blocks FEATURE-469B** (the gallery consumes the `Icon` control and `PhosphorIconNames.All`), and
  through it FEATURE-718F (documentation) and FEATURE-74DC (release).
- `Directory.Packages.props` already pins the whole Avalonia group from FEATURE-21C4 at the version
  SPEC §3.3 fixes — no new pin and no version edit is needed for this item.
- Branch `feature/feature-3add-avalonia-renderer`, cut from `HEAD` at `/build` time.

## Acceptance criteria

- [x] **The pinned Avalonia coupled set resolved on this item's first restore** — `Avalonia` and
      `Avalonia.Headless.XUnit` included — for both `net8.0` and `net10.0`, and the resolved version is
      recorded in `docs/done/FEATURE-3ADD.md`. FEATURE-21C4 delegates SPEC §3.3's "verify at restore
      time, do not assume" obligation for the Avalonia group to this item. If `Avalonia.Headless.XUnit`
      did **not** resolve at the pinned version, the **whole** coupled set was moved to 12.0.2 as one
      unit (never one package), and that package's `xunit.v3.extensibility.core` dependency was
      re-verified and the observation recorded (SPEC §3.3).

- [x] `src/Enigma.Icons.Avalonia/Enigma.Icons.Avalonia.csproj` targets **`net8.0;net10.0`** only and
      carries `ImplicitUsings disable` (solution-wide, SPEC §2 rule 2 / §3.2 — not a §10.4 property)
      and the full **SPEC §10.4 packable property set** — `GenerateDocumentationFile true`, `IncludeSymbols` + `SymbolPackageFormat snupkg`,
      `PackageReadmeFile`/`PackageLicenseFile`, no `GeneratePackageOnBuild` — with
      `IsTrimmable`/`IsAotCompatible` true in an **unconditional** `PropertyGroup` per SPEC §10.4
      (the `netstandard2.0` condition is vacuous for this TFM set; the asymmetry with the two
      multi-targeting packages is SPEC-sanctioned, is **not** a deviation, and must not be "fixed"
      with a no-op condition), house packable metadata with no `PackageReleaseNotes`, and the
      SPEC §10.5 canonical URLs (`RepositoryUrl` = `PackageProjectUrl` =
      `https://github.com/josueclement/Enigma.Icons`, `RepositoryType` = `git`, no `PackageIcon`).
- [x] It references `Avalonia` **without `Version=`** (CPM, SPEC §2.5) and `Enigma.Icons.Phosphor`
      by `ProjectReference`, with **no** direct `ProjectReference` to `Enigma.Icons`.
- [x] It packs `README.md` and `..\..\LICENSE.md`, and **does not** pack
      `THIRD-PARTY-NOTICES.md` (SPEC §14.2), with the reason recorded in a csproj comment.
- [x] `IconGlyphExtensions` exposes exactly the three SPEC §10.1 signatures; multi-layer
      `ToGeometry` returns a `GeometryGroup` with one child per layer and the first layer's
      `FillRule`; the opacity-loss caveat appears **both** in the XML doc and in
      `src/Enigma.Icons.Avalonia/README.md`.
- [x] `ToDrawing` wraps a sub-1.0-opacity layer in its own `DrawingGroup` of that opacity, leaves
      opaque layers unwrapped, skips non-filled non-stroked layers, and builds a `Pen` for stroked
      layers; `ToDrawingImage` wraps `ToDrawing`; all three throw `ArgumentNullException` on null
      arguments.
- [x] `Icon` derives from `Control` (not `TemplatedControl`), the package contains **no `.axaml`/XAML
      file at all**, and the README states that no `<StyleInclude>` is required.
- [x] `Icon` registers all eight SPEC §10.2 properties with the stated defaults, `Foreground` via
      `TextElement.ForegroundProperty.AddOwner<Icon>()`, `AffectsRender` on all eight and
      `AffectsMeasure` on `Size`/`Stretch`; `Focusable` is `false`; the automation peer reports the
      control as non-content unless `AutomationProperties.Name` is set.
- [x] `Icon.Render` never throws: a missing glyph, an unresolvable `IconSet`/`IconName`, and a null
      `Foreground` each paint nothing and pass their headless test.
- [x] `IconSet` (with `IconName`) takes precedence over `Kind`/`Weight`, proven by a test using a
      `SvgIconSet` built from in-memory sources.
- [x] `Stretch` scaling from `glyph.ViewBox` into `Bounds` is implemented for all four modes, with
      centring, and an explicit `Width`/`Height` overriding `Size` in measure.
- [x] Both markup extensions exist in `Enigma.Icons.Avalonia.Markup` with the SPEC §10.3 signatures,
      positional constructors, `Weight = Regular`, `Brush = Brushes.Black`, and the `IconImage`
      naming rationale documented.
- [x] `src/Enigma.Icons.Avalonia/Properties/AssemblyInfo.cs` exists and carries the **two** SPEC §10.3
      `XmlnsDefinition` attributes (one for `Enigma.Icons.Avalonia`, one for
      `Enigma.Icons.Avalonia.Markup`, both mapped to
      `https://github.com/josueclement/Enigma.Icons`), so a **single**
      `xmlns:ei="https://github.com/josueclement/Enigma.Icons"` resolves `ei:Icon`,
      `{ei:IconGeometry}` **and** `{ei:IconImage}` — proven by the gallery (FEATURE-469B) compiling
      with that one prefix; the packed README documents that form as primary and the per-namespace
      `using:` forms as the fallback.
- [x] `tests/Enigma.Icons.Avalonia.UnitTests` exists with `Avalonia.Headless.XUnit` + `xunit.v3`,
      an `AvaloniaTestApplication` builder, and **zero** `[Fact]`/`[Theory]` attributes — every test
      is `[AvaloniaTest]`/`[AvaloniaTheory]`.
- [x] Every bullet of SPEC §12.3 has at least one passing test, and the SPEC §12.3 items that turn
      out not to be assertable headlessly (render invalidation, and the render pass itself if the
      platform cannot be driven) are named in the completion doc with what was asserted instead.
- [x] `Enigma.Icons.slnx` gained exactly the two `<Project>` entries of the SPEC §3.4 table row for
      FEATURE-3ADD, and references no non-existent project.
- [x] Every new text file is **LF with a final newline**; every `.cs` file declares its own explicit
      `using` directives (SPEC §2).
- [x] **`dotnet build Enigma.Icons.slnx` succeeds with zero warnings** for both TFMs
      (`TreatWarningsAsErrors`, including IL2xxx/IL3xxx trim-analyzer warnings — SPEC §2, §10.4).
- [x] **`dotnet test Enigma.Icons.slnx` is green for the whole suite** (Enigma.Icons,
      Enigma.Icons.Phosphor, and Enigma.Icons.Avalonia unit tests); output captured as evidence.
- [x] **Roadmap + this plan flipped to `DONE`** (`docs/roadmap.md` FEATURE-3ADD row; this file's
      status header). (DoD criterion 4.)
- [x] **Completion doc `docs/done/FEATURE-3ADD.md` written** — summary, files touched, deviations,
      build/test evidence. (DoD criterion 5.)

## Notes / risks

- **`Geometry.Parse` needs the Avalonia platform render interface.** This makes the headless fixture
  **required even for pure geometry tests**: a test that forgets `[AvaloniaTest]` fails with an
  obscure "Unable to locate `IPlatformRenderInterface`" rather than an assertion failure, and the
  cause is easy to misdiagnose as a bad path string. Every test in the project gets the attribute;
  the `[Fact]` grep in the acceptance criteria is the cheap guard.
- **`AddOwner` must run before first use.** `TextElement.ForegroundProperty.AddOwner<Icon>()` sits in
  a `static readonly` field initializer, so it completes before `Icon`'s static constructor body and
  before any instance exists. Keep every registration (`AffectsRender`, `AffectsMeasure`,
  `OverrideDefaultValue`) inside `Icon`'s own static constructor; registering an `Icon` property from
  another type's static initializer would make correctness depend on which type is touched first.
- **Namespace shadowing: `Enigma.Icons.Avalonia` vs. `Avalonia`.** Inside namespace
  `Enigma.Icons.Avalonia` (and `…Avalonia.Markup`), a *qualified* name like `Avalonia.Media.Geometry`
  resolves `Avalonia` against the enclosing `Enigma.Icons` namespace and finds
  `Enigma.Icons.Avalonia` first — CS0234. Mitigations, to be applied consistently: put all `using`
  directives **above** the file-scoped `namespace …;` declaration so they resolve globally; use
  unqualified type names in code; and in XML docs use the `cref="T:Avalonia.Media.Geometry"`
  documentation-comment ID form (or a `global::` qualified name in code) where a fully-qualified
  reference is unavoidable. Also confirm at build time that `Enigma.Icons.Avalonia.Icon` does not
  collide ambiguously with any `Avalonia.Controls` type named `Icon` in Avalonia 12; if it does, use
  an alias rather than renaming the control (the name is fixed by SPEC §10.2).
- **Why the `xmlns` needs the two `XmlnsDefinition` attributes (settled — SPEC §10.3).** Avalonia's
  `using:` mapping covers one CLR namespace and **not** its sub-namespaces, so a lone
  `xmlns:ei="using:Enigma.Icons.Avalonia"` would reach `<ei:Icon>` but not `{ei:IconGeometry …}`. The
  settled fix is design step 5: keep both CLR namespaces exactly as SPEC §10 fixes them and map both
  onto `https://github.com/josueclement/Enigma.Icons` in
  `src/Enigma.Icons.Avalonia/Properties/AssemblyInfo.cs`, so one prefix reaches the control and both
  extensions. The per-namespace `using:` forms remain the documented fallback. This is not an open
  question — build it that way, and let FEATURE-469B's compiled gallery XAML be the proof.
- **OPEN QUESTION — how a non-null `IconLayer.Stroke` paint string maps to an Avalonia brush.**
  SPEC §10.1 specifies only the `Stroke == null` case ("default to the supplied brush"). Proposal:
  attempt Avalonia's `Brush.Parse(layer.Stroke)` and fall back silently to the supplied brush on
  failure, never throwing. Note the deliberate asymmetry this creates with fills, where the
  consumer's brush always wins and `layer.Fill` is ignored (SPEC §4.2's rationale) — the asymmetry
  follows SPEC §10.1's wording, so it is recorded here rather than "fixed" in code. If the intent was
  that strokes also always use the supplied brush, say so and the pen simplifies.
- **`Stretch` on a non-square target must be checked visually.** Uniform/UniformToFill/Fill/None all
  produce a plausible-looking measured size, and a sign error in the centring translate or a swapped
  `min`/`max` passes every reasonable assertion while looking wrong on screen. FEATURE-469B carries the
  matching criterion and is the verification: **one `ei:Icon` in a deliberately non-square container,
  cycling `Stretch` through `None` / `Uniform` / `UniformToFill` / `Fill`**, checked by eye and recorded
  in that item's completion doc. This item asserts the scaling maths structurally only.
- **Avalonia 12 API names.** `PushTransform`, `PushOpacity`, `AffectsRender`, `AffectsMeasure`,
  `AddOwner`, `ControlAutomationPeer`, and `AvaloniaHeadlessPlatform.ForceRenderTimerTick` are all
  used from memory of the Avalonia 11/12 surface. Avalonia 12 is a major version: check the actual
  signatures in the referenced assemblies before writing code, and prefer the compiler over
  recollection. Any renamed member is a mechanical fix, not a design change.
- **Trim/AOT analyzer warnings are build failures** here (`IsTrimmable` + `TreatWarningsAsErrors`,
  SPEC §10.4). The design is reflection-free — `PhosphorIconSet` looks resources up by literal name
  and the name tables replace `Enum.ToString()` — so warnings should not appear; if one does (most
  likely from an automation-peer or markup-extension path), annotate the member properly rather than
  relaxing the flags.
- **`Geometry.Parse` runs on every `Render`.** No geometry cache is planned for 1.0: glyph instances
  are already cached and reference-equal, path data is a few hundred bytes, and a cache keyed by path
  string is one more invalidation surface. If the gallery's virtualized 1,512-icon grid shows scroll
  jank, revisit with a static `ConcurrentDictionary<string, Geometry>` — but only with the gallery as
  evidence.
- **Headless tests do not prove pixels.** They prove structure (drawing graphs, layer counts,
  property plumbing) and the never-throw contract. Actual appearance — weights, duotone tinting,
  theme-following foreground — is FEATURE-469B's job, by design (SPEC §17 rules out bitmap
  baselines).
