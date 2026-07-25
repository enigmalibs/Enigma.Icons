**Status:** TODO · Single-phase · Suggested build branch `feature/feature-24dd-icons-base`

# FEATURE-24DD — Enigma.Icons base library + UnitTests

## Objective

Build the framework-agnostic foundation of the whole solution: the immutable icon data model
(SPEC §4), the hardened SVG parser (SPEC §5), the `IIconSet` abstraction and its
bring-your-own-icons implementation `SvgIconSet` (SPEC §6), the packable project with **zero
package references** (SPEC §2.10), a real packed README documenting the supported subset *and* its
exclusions (SPEC §13), and the `Enigma.Icons.UnitTests` suite covering every bullet of SPEC §12.1.

Deliverable: `src/Enigma.Icons` builds warning-free across `netstandard2.0;net8.0;net10.0`, and the
unit-test suite is green — including the two mandatory security tests (XXE non-resolution and
billion-laughs fail-fast).

## Context

This is the largest and most load-bearing item in the roadmap. Everything downstream sits on the
types produced here:

- **FEATURE-2DDE** (generator) mirrors the layer model in the `.dat` resource format (SPEC §7.2:
  one field per layer, `@<opacity>:` prefix) — that format only makes sense against `IconLayer` /
  `IconGlyph` as defined here.
- **FEATURE-3950** (`Enigma.Icons.Phosphor`) implements `IIconSet` and returns `IconGlyph`s built
  directly from `.dat` fields, deliberately **without** going through `SvgIconParser` (SPEC §9.1).
- **FEATURE-3ADD** (`Enigma.Icons.Avalonia`) translates `IconGlyph`/`IconLayer` into Avalonia
  `Geometry`/`Drawing`, and its `Icon` control accepts any `IIconSet` (SPEC §10.1, §10.2).

Two structural constraints shape almost every decision below:

1. **`netstandard2.0` is in the TFM set** (SPEC §0). That forbids default interface members
   (SPEC §6.1 says so explicitly), `required`/`init` members, `Span<T>` anywhere — public *or*
   internal, because `System.Memory` is not in the `netstandard2.0` reference set — and a number of
   BCL conveniences listed in Notes / risks. SPEC §3.3 / §15 forbid adding a polyfill package
   without a justified csproj comment, so the correct move is to write around the gaps.
2. **Zero runtime dependencies** (SPEC §2.10). The csproj has *no* `<PackageReference>` at all. This
   is why paint values are raw strings rather than a colour type (SPEC §4.2) and why there is no
   logging (SPEC §15, Observability).

The retired `PhosphorIconsAvalonia` is context only (SPEC §0.1) — it had no model layer, no icon-set
abstraction, and parsed with `XmlDocument.SelectSingleNode` over its own embedded resources. Nothing
is ported. In particular its XML handling is **not** a template: this library accepts untrusted
user SVG, so SPEC §5.2 hardening is mandatory.

## Scope

### In scope

- `src/Enigma.Icons/Enigma.Icons.csproj` — TFMs, packable metadata, trim/AOT conditioning, packed
  README + root LICENSE, **zero** `PackageReference` entries.
- Model: `IconViewBox.cs`, `IconLayer.cs` (+ `IconFillRule`, `IconLineCap`, `IconLineJoin`),
  `IconGlyph.cs` — SPEC §4.
- Exceptions: `SvgParseException.cs`, `IconNotFoundException.cs` — SPEC §5.3.
- Parser: `SvgIconParser.cs` plus the `Internal/` helper set — SPEC §5.
- Abstraction: `IIconSet.cs` (SPEC §6.1) and `SvgIconSet.cs` with its four factories (SPEC §6.2).
- `src/Enigma.Icons/README.md` — packed; a real, correct README (polished later by FEATURE-718F).
- `tests/Enigma.Icons.UnitTests/` — xunit.v3 project + the full SPEC §12.1 suite + its test assets.
- `Enigma.Icons.slnx`: append `src/Enigma.Icons` and `tests/Enigma.Icons.UnitTests` (SPEC §3.4).

### Out of scope

- **Anything Phosphor.** No `PhosphorIcon` enum, no `.dat` reader, no `Assets/` — FEATURE-2DDE and
  FEATURE-3950. `SvgIconParser` is deliberately *not* used by the Phosphor set (SPEC §9.1).
- **Anything Avalonia.** No `Geometry`, no `Drawing`, no controls, no markup extensions —
  FEATURE-3ADD. This project must not reference Avalonia, directly or transitively.
- **`<PackageReleaseNotes>`** on the csproj — filled by FEATURE-74DC (SPEC §13).
- **README polish / screenshots** — FEATURE-718F owns the final pass over all four READMEs. This
  item ships a correct, complete README, not a placeholder.
- **Root files** (`Directory.Build.props`, `Directory.Packages.props`, `global.json`, `LICENSE.md`,
  `.editorconfig`, the slnx itself) — created by FEATURE-21C4; this item only appends two slnx rows.
- **Full SVG rendering** — gradients, `clipPath`, `mask`, `<defs>`/`<use>`/`<symbol>`, `style=`/
  `<style>`, `<text>`, `<image>`, filters, animation (SPEC §5.1, §17). Documented as exclusions,
  not implemented.
- **New central package pins.** `Directory.Packages.props` is not touched; `xunit.v3` 3.2.2 is
  already pinned there by FEATURE-21C4 (SPEC §3.3).

## Design

All paths are relative to the repo root `/home/jo/Dev/Enigma.Icons`. Every text file is LF with a
final newline; the build is zero-warning with `TreatWarningsAsErrors`; `ImplicitUsings` is disabled
so every file declares explicit `using` directives; CPM means no `Version=` on any
`PackageReference` — all per SPEC §2. Public namespace is `Enigma.Icons`; internal helpers live in
`Enigma.Icons.Internal`.

### 1. `src/Enigma.Icons/Enigma.Icons.csproj`

Shared settings (`LangVersion 14`, `Nullable`, `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`,
`Authors`, `Copyright`) are inherited from `Directory.Build.props` (SPEC §3.2) — do **not** redeclare
them. Shape:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFrameworks>netstandard2.0;net8.0;net10.0</TargetFrameworks>
    <ImplicitUsings>disable</ImplicitUsings>

    <PackageId>Enigma.Icons</PackageId>
    <Title>Enigma.Icons — icon model, SVG parser and icon-set abstraction</Title>
    <Version>1.0.0</Version>
    <Description>Framework-agnostic icon model, hardened SVG icon parser and icon-set abstraction for .NET. Bring your own folder of .svg files, or plug in an icon pack such as Enigma.Icons.Phosphor. Zero dependencies.</Description>
    <PackageTags>enigma icons icon svg vector geometry icon-set parser dotnet netstandard zero-dependencies open-source</PackageTags>

    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageLicenseFile>LICENSE.md</PackageLicenseFile>
    <RepositoryUrl>https://github.com/josueclement/Enigma.Icons</RepositoryUrl>
    <PackageProjectUrl>https://github.com/josueclement/Enigma.Icons</PackageProjectUrl>
    <RepositoryType>git</RepositoryType>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>

  <!-- SPEC §10.4: trim/AOT annotations only on the modern TFMs. -->
  <PropertyGroup Condition="'$(TargetFramework)' != 'netstandard2.0'">
    <IsTrimmable>true</IsTrimmable>
    <IsAotCompatible>true</IsAotCompatible>
  </PropertyGroup>

  <ItemGroup>
    <None Include="README.md" Pack="true" PackagePath="\" />
    <None Include="..\..\LICENSE.md" Pack="true" PackagePath="\" />
  </ItemGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="Enigma.Icons.UnitTests" />
  </ItemGroup>

</Project>
```

Constraints and rationale:

- **Zero `<PackageReference>` — call this out in the completion doc.** SPEC §2.10 makes it a hard
  constraint, not a preference. Everything the library needs (`System.Xml.ReaderWriter`,
  `System.Collections.Concurrent`, `System.IO`, `System.Globalization`) is inside the
  `netstandard2.0` reference set. If a compile error under `netstandard2.0` seems to demand a
  polyfill, the first answer is to rewrite the code (see Notes / risks); adding a package requires a
  justified csproj comment per SPEC §3.3.
- `GeneratePackageOnBuild` is **not set** — pack is explicit (`dotnet pack`). The retired package set
  it to `True`; do not copy that.
- `PackageReleaseNotes` is **absent here** — FEATURE-74DC adds it (SPEC §13).
- README is project-local; LICENSE is the shared root file `..\..\LICENSE.md` (SPEC §14.1).
  `THIRD-PARTY-NOTICES.md` is **not** packed by this project — it ships only with
  `Enigma.Icons.Phosphor` (SPEC §14.2).
- `InternalsVisibleTo` exists so the tests can exercise `Enigma.Icons.Internal` helpers directly
  (the path-containment check and the name normalizer, step 8). It adds nothing to the public API.

### 2. Model types (SPEC §4)

#### 2a. `src/Enigma.Icons/IconViewBox.cs`

`readonly struct IconViewBox : IEquatable<IconViewBox>` exactly per SPEC §4.1, plus the value-type
completion set that the SPEC listing elides: `==`, `!=`, `Equals(object?)`, `GetHashCode()`, and an
invariant-culture `ToString()` emitting `"X Y Width Height"` (the same shape as the SPEC §7.2 `.dat`
header, which keeps FEATURE-2DDE's job trivial).

- Guard: write it as `if (!(width > 0)) throw new ArgumentOutOfRangeException(...)`. Phrasing the
  test as a *negated* `> 0` — rather than `<= 0` — also rejects `double.NaN`, which SPEC §4.1's
  "must be > 0" requires but a `<= 0` test would silently admit. Same for `height`.
- `Default` is a `static readonly` backing field surfaced through the property; value `0, 0, 256, 256`
  (SPEC §4.1).
- `GetHashCode` must be **hand-rolled**: `System.HashCode` is .NET Standard 2.1+, so it does not
  exist on `netstandard2.0`. Use a conventional unchecked multiply-add fold over the four
  `double.GetHashCode()` values. Do not `#if` this — one implementation for all TFMs.

#### 2b. `src/Enigma.Icons/IconLayer.cs`

One file holding `IconLayer` plus `IconFillRule`, `IconLineCap`, `IconLineJoin` — the grouping SPEC
§4.2 itself uses. `sealed class`, constructor signature and property set **exactly** as SPEC §4.2
(the optional-parameter defaults are part of the contract; do not reorder or rename parameters).

- Guards: `pathData` null/empty/whitespace → `ArgumentException`; `opacity` outside `[0,1]` →
  `ArgumentOutOfRangeException` (again phrase it so `NaN` is rejected).
- `IsStroked` / `IsFilled` are computed per the SPEC §4.2 comments (`Stroke is not null &&
  Stroke != "none"`; `Fill != "none"` — so a `null` `Fill` *is* filled, because null means "inherit
  the renderer's brush"). Compare with `StringComparison.Ordinal`. Compute in the constructor and
  store in fields, since the type is immutable.
- **`"currentColor"` normalization lives in the parser, not the constructor.** SPEC §4.2 attributes
  it to the parser ("`\"currentColor\"` is normalized to `null` by the parser"), and SPEC §5.1
  repeats it as a parser rule. The constructor therefore stores `fill`/`stroke` verbatim. Say so in
  the constructor's XML doc so a hand-built layer's behaviour is unsurprising, and do **not** write a
  test asserting the constructor normalizes.
- **Why the paint values are raw strings**, restated in the type's XML doc per SPEC §4.2: the package
  has zero dependencies and therefore no framework colour type; each renderer interprets
  `Fill`/`Stroke` in its own terms and, in the normal icon case, ignores them in favour of the
  consumer's brush. `null` (the normalized form of `currentColor`) is what makes "inherit the brush"
  the default.

#### 2c. `src/Enigma.Icons/IconGlyph.cs`

`sealed class` per SPEC §4.3.

- `layers` null → `ArgumentNullException`; empty → `ArgumentException`; a null *element* →
  `ArgumentException` naming the index (SPEC §4.3 guarantees "never empty" and the list is walked by
  every renderer, so a null element must not reach FEATURE-3ADD).
- Defensive copy: materialize into an array, then wrap in
  `System.Collections.ObjectModel.ReadOnlyCollection<IconLayer>` so the exposed `IReadOnlyList<T>`
  cannot be mutated through a cast. Enumerate the input exactly once (it may be a lazy sequence).
- `IsSingleLayer` = exactly one layer, that layer fully opaque (`Opacity >= 1.0`) and not stroked —
  per SPEC §4.3's wording. Compute once in the constructor.

### 3. Exceptions (SPEC §5.3)

#### 3a. `src/Enigma.Icons/SvgParseException.cs`

`sealed class SvgParseException : Exception` with exactly the two constructors SPEC §5.3 lists.
**Do not add the `protected SvgParseException(SerializationInfo, StreamingContext)` constructor** —
binary serialization of exceptions is obsolete on `net8.0`/`net10.0` and adding it produces
`SYSLIB0051`, which `TreatWarningsAsErrors` turns into a build error. Same for `IconNotFoundException`.

#### 3b. `src/Enigma.Icons/IconNotFoundException.cs`

`sealed class IconNotFoundException : Exception` with `IconName` / `Variant` / `SetName` per SPEC §5.3.
The message is built by a `private static string BuildMessage(...)` invoked in the `base(...)` call.
Exact shapes:

- variant present: `Icon 'acorn' (variant 'bold') was not found in icon set 'Phosphor'.` — verbatim
  from SPEC §5.3.
- variant `null`: `Icon 'acorn' was not found in icon set 'Phosphor'.` — the parenthetical is
  dropped. SPEC §5.3 does not give this form; see OPEN QUESTION 2.

`iconName` and `setName` null → `ArgumentNullException`; `variant` may be null. Set the three
properties from the *arguments*, not by re-parsing the message.

### 4. `src/Enigma.Icons/SvgIconParser.cs` + `Internal/` helpers (SPEC §5)

Public surface exactly per SPEC §5 — `Parse(string)`, `Parse(Stream)`, and the mutable static
`MaxDocumentBytes` — with the SPEC's XML docs reproduced. Back `MaxDocumentBytes` with a
`private static volatile int` defaulting to `1024 * 1024`; the setter rejects `<= 0` with
`ArgumentOutOfRangeException`. Document that it is **process-wide** mutable configuration.

The parser is split across these files:

| File | Responsibility |
|---|---|
| `SvgIconParser.cs` | Size gate, `XmlReader` construction, the element-dispatch walk, the `<g>` inheritance stack, the viewBox ladder, glyph assembly. |
| `Internal/Matrix2D.cs` | `internal readonly struct` — SVG's `[a c e; b d f; 0 0 1]`; `Multiply`, `Transform(x, y)`, `Determinant`, `IsIdentity`, `Identity`. |
| `Internal/SvgTransformParser.cs` | `transform="…"` → `Matrix2D`. |
| `Internal/SvgShapeConverter.cs` | One converter per SPEC §5.1 shape row → path data. |
| `Internal/SvgPathTransformer.cs` | Bakes a `Matrix2D` into path data. |
| `Internal/SvgValueParser.cs` | Attribute value parsing: numbers, lengths, point lists, viewBox, opacity, `fill-rule`, `stroke-linecap`, `stroke-linejoin`. |
| `Internal/SvgNumber.cs` | The single deterministic number→string formatter. |
| `Internal/SvgStyleContext.cs` | The inheritable presentation state pushed/popped per `<g>`. |

#### 4a. Size gate and hardened `XmlReader` (SPEC §5.2 — mandatory)

`MaxDocumentBytes` is a **byte** limit, so:

- `Parse(string)`: null → `ArgumentNullException`; empty/whitespace → `SvgParseException`. Then
  early-out if `svg.Length > MaxDocumentBytes` (a UTF-8 byte count is always `>=` the UTF-16
  code-unit count, so this is a sound over-limit proof), otherwise take the exact count via
  `Encoding.UTF8.GetByteCount(svg)`. Over limit → `SvgParseException` naming the limit and the
  actual size. Feed a `StringReader` to `XmlReader.Create`.
- `Parse(Stream)`: null → `ArgumentNullException`. Copy into a `MemoryStream` with a **bounded** loop
  that reads at most `MaxDocumentBytes + 1` bytes; if the extra byte materializes, throw
  `SvgParseException` before any parsing. Never trust `stream.Length` (the stream may be
  non-seekable). Hand the buffered `MemoryStream` — not the caller's stream — to
  `XmlReader.Create(Stream, settings)` so the XML declaration's encoding and any BOM are honoured.
  The caller's stream is read but **not disposed** (SPEC §5).

`XmlReaderSettings` are exactly the SPEC §5.2 block: `DtdProcessing.Prohibit`, `XmlResolver = null`,
`IgnoreComments`, `IgnoreProcessingInstructions`, `IgnoreWhitespace`, `CloseInput = false`.

**Why this is mandatory rather than cosmetic** — state it in a comment above the settings, because a
future maintainer will be tempted to "simplify" it:

- On `netstandard2.0` / .NET Framework, `XmlDocument`'s and `XmlTextReader`'s defaults resolve DTDs
  and external entities. `XmlDocument.LoadXml` there will happily dereference
  `<!ENTITY x SYSTEM "file:///etc/passwd">` (XXE, arbitrary local-file disclosure) and will expand a
  nested-entity bomb ("billion laughs") until memory is exhausted. The retired package got away with
  `XmlDocument.LoadXml` only because its input was always its own embedded resources; **this**
  parser's input is arbitrary user SVG, so the defaults are unacceptable.
- `DtdProcessing.Prohibit` closes both holes at once: the DTD is rejected outright as an
  `XmlException`, so no entity is ever *defined*, let alone resolved or expanded. `XmlResolver = null`
  is belt-and-braces for any other external reference.
- Wrap every `XmlException` from the walk in `SvgParseException` with the `XmlException` as
  `innerException` — the typed contract SPEC §15 promises for malformed SVG.
- The walk is **iterative** over an explicit `Stack<…>`, never recursive, so a pathologically nested
  document cannot overflow the stack; nesting memory is bounded by `MaxDocumentBytes`.

#### 4b. Element dispatch

Single forward-only `while (reader.Read())` loop over `XmlNodeType.Element` / `EndElement`, matching
on **`reader.LocalName`** so the SVG namespace is honoured but not required (SPEC §5.2). Read all
needed attributes with `reader.GetAttribute("name")` while positioned on the element (presentation
attributes are never prefixed). State machine:

1. First element must have local name `svg`, else `SvgParseException("… root element is not <svg> …")`.
   Read `viewBox` / `width` / `height` (step 4f) and push the **root context** built from the root's
   own presentation attributes — SPEC §7.4 point 4 shows `fill="currentColor"` sitting on `<svg>`,
   so the root participates in inheritance like any `<g>`.
2. `g` → push `parent.Merge(ownAttributes)` (step 4e). If `reader.IsEmptyElement`, pop immediately.
3. A shape from the SPEC §5.1 table (`path`, `rect`, `circle`, `ellipse`, `line`, `polyline`,
   `polygon`) → build zero or one `IconLayer` (step 4c) and append to the layer list.
   **Document order is paint order**; index 0 is bottom-most (SPEC §4.3).
4. `title`, `desc`, `metadata` → `reader.Skip()` silently. These are non-paintable, not
   "unsupported constructs", and must not appear in an error message.
5. Any element in the known-unsupported set — `linearGradient`, `radialGradient`, `pattern`,
   `clipPath`, `mask`, `defs`, `use`, `symbol`, `style`, `text`, `tspan`, `image`, `filter`,
   `marker`, a nested `svg`, and the `animate*`/`set` family (SPEC §5.1 exclusions) — plus any
   unrecognized element: record its local name in a `firstUnsupported` field **if that field is
   still null**, then `reader.Skip()` the whole subtree.
6. A `style="…"` attribute on an otherwise-supported element is also recorded in
   `firstUnsupported` (as `style attribute`) but does **not** suppress the layer: the geometry is
   still usable, only the CSS-expressed paint is lost.
7. `EndElement` with local name `g` → pop. Track `reader.Depth` alongside each pushed context and
   pop while the top's depth `>= reader.Depth`, so the stack cannot desynchronize.

Termination (SPEC §5.1, last paragraph):

- Layers produced → build the `IconGlyph`.
- Zero layers **and** `firstUnsupported != null` → `SvgParseException` whose message **names the
  construct**, e.g.
  `The SVG document contains no supported paintable element; its only paintable content is inside an unsupported <linearGradient> construct. Enigma.Icons supports shapes, groups and transforms only — see the package README.`
- Zero layers and nothing unsupported seen → `SvgParseException("The SVG document contains no paintable element.")`.

#### 4c. Building one layer from a shape element

1. Merge the current inherited context with the element's own presentation attributes (step 4e) —
   the element's own value always wins.
2. Produce raw path data:
   - `path` → the `d` attribute **verbatim** (SPEC §5.1). `d` missing or whitespace → produce no
     layer and continue (an empty `<path>` paints nothing; it is not an error).
   - every other shape → `SvgShapeConverter` (step 4d).
3. Bake the composed transform into the path data (step 4g). **When the composed matrix is the
   identity, emit the path data unchanged** — no tokenization at all. This is the overwhelmingly
   common case and it keeps `<path d>` byte-identical to the source, exactly as SPEC §5.1 requires.
4. Construct the `IconLayer` with the merged opacity, fill rule and paint values. `IconLayer` never
   carries a transform (SPEC §5.1) — by this point the transform is gone into the geometry.

#### 4d. `Internal/SvgShapeConverter.cs` — one converter per SPEC §5.1 row

All emitted numbers go through `SvgNumber.Format` (step 4h). A shape whose dimensions make it
non-rendering (per the SVG spec: `width`/`height`/`r`/`rx`/`ry` `<= 0`) produces **no layer** rather
than an exception. An attribute present but unparseable → `SvgParseException` naming the element and
the attribute.

- **`rect`** — `x`/`y` default `0`; `width`/`height` required and `> 0`.
  - **The `rx`/`ry` mirroring rule (SPEC §5.1):** if only `rx` is given, `ry = rx`; if only `ry` is
    given, `rx = ry`; if neither, corners are sharp. A negative value is treated as absent. Then
    clamp `rx <= width / 2` and `ry <= height / 2` independently.
  - Sharp: `M x,y L x+w,y L x+w,y+h L x,y+h Z`. Emit explicit `L` rather than `H`/`V` — the
    transformer has to expand `H`/`V` anyway (they are not axis-aligned after a rotation), so not
    emitting them keeps the round trip shorter.
  - Rounded: four `L` edges joined by four corner arcs, each `A rx,ry 0 0 1 …`, starting at
    `(x+rx, y)` and closing with `Z`. **`sweep-flag = 1`** because SVG's y-axis points down, so
    "positive angular direction" is the on-screen clockwise direction the corner sequence
    top→right→bottom→left follows. `large-arc-flag = 0` (a corner is a quarter arc).
- **`circle`** — `cx`/`cy` default `0`; `r > 0`. Two semicircular arcs (SPEC §5.1):
  `M cx-r,cy A r,r 0 1 1 cx+r,cy A r,r 0 1 1 cx-r,cy Z`.
  For `cx=128, cy=128, r=100` that is exactly `M 28,128 A 100,100 0 1 1 228,128 A 100,100 0 1 1 28,128 Z`
  — pin that string in a unit test (see Notes / risks: this is the easiest thing in the item to get
  subtly wrong).
- **`ellipse`** — `cx`/`cy` default `0`; `rx`/`ry` required and `> 0`. Same two-arc construction with
  `A rx,ry 0 1 1 …`. (SVG 2's `rx="auto"` is not supported; an unparseable value is an error.)
- **`line`** — `M x1,y1 L x2,y2` (SPEC §5.1); all four coordinates default `0`. The layer is emitted
  even though it is invisible unless stroked — renderers skip non-filled, non-stroked layers
  (SPEC §5.1, last row).
- **`polyline` / `polygon`** — parse `points` as a whitespace-and/or-comma-separated number list. A
  trailing odd number is dropped (SVG error handling: render up to the last complete pair). Fewer
  than two complete pairs → no layer. Emit `M p0 L p1 L p2 …`, and `polygon` appends ` Z`.

#### 4e. `Internal/SvgStyleContext.cs` — the `<g>` inheritance/override chain (SPEC §5.1)

An immutable value carrying: `Matrix2D Transform`, `double Opacity`, `string? Fill`, `string? Stroke`,
`double? StrokeWidth`, `IconLineCap? StrokeLineCap`, `IconLineJoin? StrokeLineJoin`,
`IconFillRule FillRule`. Plus a `Merge(...)` producing the child context from the parent and one
element's attributes:

- `Transform` — `child = parent.Transform × own.Transform` (parent on the **left**; see step 4f).
- `Opacity` — **multiplied** down the chain (SPEC §5.1): `child = parent.Opacity * own`, each factor
  clamped into `[0,1]` first. This is what makes a `<g opacity="0.5">` containing an
  `opacity="0.4"` shape come out at `0.2`.
- `Fill`, `Stroke`, `StrokeWidth`, `StrokeLineCap`, `StrokeLineJoin`, `FillRule` — **override**: the
  element's own value wins, otherwise the parent's is inherited unchanged.
- Attribute value handling: trim whitespace; `"currentColor"` → `null` (SPEC §4.2/§5.1);
  `"inherit"` → treat as "not specified" so the parent's value flows through; `"none"` is kept
  verbatim, because `IconLayer.IsFilled`/`IsStroked` are defined against it. `fill-rule="evenodd"`
  → `IconFillRule.EvenOdd`, anything else → `NonZero`. `stroke-linecap` `butt`→`Flat`,
  `round`→`Round`, `square`→`Square`; `stroke-linejoin` `miter`→`Miter`, `round`→`Round`,
  `bevel`→`Bevel`; an unrecognized keyword falls back to the CSS initial value rather than throwing.
- Root defaults: identity transform, `Opacity = 1.0`, all paint values `null`, `FillRule = NonZero`.
  Note the deliberate divergence from CSS, whose initial `fill` is black: here "unspecified" means
  **inherit the consumer's brush** (SPEC §4.2), which is the behaviour an icon library wants.
- `fill-opacity` / `stroke-opacity` are **ignored** — see OPEN QUESTION 4.

#### 4f. `Internal/SvgTransformParser.cs` and `Internal/Matrix2D.cs`

`Matrix2D` is the SVG 2×3 affine `[a c e; b d f]`; `Transform(x, y) = (a·x + c·y + e, b·x + d·y + f)`;
`Determinant = a·d − b·c`. `IsIdentity` compares against `1,0,0,1,0,0` exactly (the fast path in
step 4c depends on it, and the identity always arrives as a literal, never as a rounding artefact).

Parsing: tokenize a sequence of `name(arg, arg …)` groups separated by whitespace and/or commas, then
build each primitive (angles in degrees → radians):

- `translate(tx [ty])` — `ty` defaults `0`.
- `scale(sx [sy])` — `sy` defaults **`sx`**.
- `rotate(a)`, and `rotate(a cx cy)` = `translate(cx,cy) × rotate(a) × translate(−cx,−cy)`.
- `matrix(a b c d e f)`.
- `skewX(a)` — `c = tan(a)`. `skewY(a)` — `b = tan(a)`.
- An unknown function name, or the wrong argument count for a known one → `SvgParseException` naming
  the function.

**Composition order — get this exactly right.** SVG applies a transform *list* as nested coordinate
systems, so for `transform="A B C"` the resulting matrix is `M = A × B × C` and a point maps as
`A × (B × (C × p))`: the accumulator is multiplied on the **right** as the list is read
left-to-right, and the *effect* is that the **last-listed** transform applies to the geometry
**first** — i.e. right-to-left. An inherited transform composes the same way with the ancestor on
the left: `M_effective = M_ancestor × M_own`. The regression test is a non-commutative pair —
`translate(10,0) rotate(90)` and `rotate(90) translate(10,0)` must produce *different* baked
coordinates, and the plan's chosen convention decides which.

**viewBox fallback ladder** (SPEC §5.1, last rows), read off the root `<svg>`:

1. `viewBox` present → parse exactly four whitespace/comma-separated numbers. Malformed (not four
   numbers, or non-positive width/height) → `SvgParseException` naming the attribute. The ladder is
   for an *absent* viewBox, not a broken one.
2. Absent, but `width` **and** `height` both present and both parse `> 0` →
   `new IconViewBox(0, 0, width, height)`.
3. Otherwise → `IconViewBox.Default` (`0 0 256 256`).

Length parsing for `width`/`height`: accept a bare number or a `px` suffix. Anything else — notably
`width="100%"`, which is common on a root `<svg>` — counts as "not usable", so the ladder falls
through to rung 3 rather than throwing. On a *shape* attribute, by contrast, an unparseable value is
an error (step 4d).

#### 4g. `Internal/SvgPathTransformer.cs` — baking a transform into path data

`Bake(string pathData, in Matrix2D m)`:

- `m.IsIdentity` → return `pathData` unchanged. **No tokenization, no validation.** This preserves
  SPEC §5.1's "`d` taken verbatim" and means malformed `d` with no transform passes straight through
  to the renderer — deliberate, and worth a sentence in the README.
- Otherwise tokenize the SVG path mini-language and re-emit:
  1. Walk commands, tracking the current point and the subpath start point. Convert **relative to
     absolute** (`m l h v c s q t a z` → their upper-case forms) and expand `H`/`V` into `L`, since a
     horizontal segment is no longer horizontal after a rotation or skew.
  2. Transform every absolute coordinate pair with `m.Transform`. `S` and `T` need **no** special
     handling beyond transforming their explicit points: their implied control point is the
     reflection `2·P_current − P_prevControl`, and an affine map preserves that relation exactly
     (`M(2P − Q) + t = 2(MP + t) − (MQ + t)`), so the reflection stays correct.
  3. `Z` passes through.
  4. **`A` (elliptical arc)** — the only genuinely hard case. Transform the endpoint normally, then
     recompute the ellipse:
     - Represent the arc's ellipse by its shape matrix `E = R(φ) · diag(rx, ry)` where `φ` is the
       x-axis-rotation. The transformed ellipse's shape matrix is `E' = L · E`, with `L` the linear
       (2×2) part of `m`.
     - Recover `rx'`, `ry'`, `φ'` from `E'` in closed form: form the symmetric `S = E' · E'ᵀ`; its
       eigenvalues are `rx'²` and `ry'²` and its eigenvectors give `φ'`. (Equivalently: the singular
       values and left singular vector of `E'`.)
     - `large-arc-flag` is **unchanged**. `sweep-flag` **flips iff `m.Determinant < 0`**, because a
       reflection reverses angular direction.
     - Fast paths worth having: identity `L` (already handled), and a *conformal* `L` (uniform scale
       `s` plus rotation `θ`, no skew — detected by `a == d && b == −c`), for which
       `rx' = s·rx`, `ry' = s·ry`, `φ' = φ + θ` and the sweep flag is unchanged.
     - `rx` or `ry` zero → per the SVG spec the arc degenerates to a straight line; emit `L` to the
       transformed endpoint.
  5. Malformed path data (unknown command letter, missing operands, unparseable number) →
     `SvgParseException` naming the offending command and, for a shape-derived path, the element.

#### 4h. `Internal/SvgNumber.cs` — deterministic formatting

A single `internal static string Format(double value)` used by **every** emitter, so the same input
SVG produces the same path data on every TFM:

- Reject `NaN` / `±Infinity` → `SvgParseException` (a transform or attribute produced a
  non-representable coordinate).
- Round to 6 decimal places, format with `"0.######"` and `CultureInfo.InvariantCulture`, and
  normalize `-0` to `0`.
- **Do not use the default `double.ToString()`.** On `net8.0`/`net10.0` it is shortest-round-trippable;
  on `netstandard2.0` running against .NET Framework it is `G15`. Same input, different emitted
  bytes — an avoidable cross-TFM behaviour difference in an otherwise deterministic library.

### 5. `src/Enigma.Icons/IIconSet.cs` (SPEC §6.1)

Reproduce SPEC §6.1's interface **and its XML docs** verbatim, then restate the implementer contract
from SPEC §6.1 in the doc comments: case-insensitive `OrdinalIgnoreCase` lookup accepting kebab-case,
`snake_case` or PascalCase; `variant == null` means `DefaultVariant`; **a variant the set does not
have is a miss, never a silent fallback** (a caller asking for `bold` must not get `regular`);
repeated calls for the same `(icon, variant)` return a **cached, reference-equal** `IconGlyph`; all
members safe for concurrent use. SPEC §6.1's `TryGetGlyph` doc line reads "Non-throwing lookup." —
reproduce it verbatim, then immediately qualify it with §6.1's **precedence table**: non-throwing means
*for a miss*, and a broken source (malformed SVG, I/O error, missing resource stream, `.dat`
header/count mismatch) propagates from `TryGetGlyph` exactly as it does from `GetGlyph` (Design step 6).

**No default interface members — call this out in a comment on the interface.** SPEC §6.1 says
`GetGlyph` "may be provided once as a default interface member only on the modern TFMs; because
`netstandard2.0` is in the TFM set, implement it explicitly in each set instead". So the interface
declares `GetGlyph` abstractly and *every* implementation writes its own — `SvgIconSet` here, and
`PhosphorIconSet` in FEATURE-3950. Note also that the `variant = null` default argument in
`GetGlyph(string icon, string? variant = null)` must be **repeated on each implementing class's
method**, or callers holding a class-typed reference lose it.

### 6. `src/Enigma.Icons/SvgIconSet.cs` (SPEC §6.2)

`sealed class SvgIconSet : IIconSet`. The four factories are exactly SPEC §6.2's signatures. A single
`private` constructor takes the finished name / variant list / default variant / index, so all four
factories share one code path.

**Internal shape**

- `SvgSource` — an `internal sealed class` holding a `Func<IconGlyph> Parse` closure plus a
  `string Description` used in error messages. Each factory builds the closure appropriately
  (`File.OpenRead` → `SvgIconParser.Parse(Stream)`; `assembly.GetManifestResourceStream` →
  `Parse(Stream)`; an in-memory string → `Parse(string)`). Preferring the `Stream` overload for
  files and resources means BOM/encoding detection is handled by `XmlReader`. `IOException` /
  `UnauthorizedAccessException` / a null resource stream are wrapped in `SvgParseException` naming
  the `Description` — SPEC §6.2's "a file that disappears between construction and first parse
  surfaces as `SvgParseException` wrapping the I/O error".
- Index: `Dictionary<string, Dictionary<string, SvgSource>>` — outer keyed by normalized variant
  (`""` for a set with no variants), inner by normalized icon name, both `StringComparer.Ordinal`
  (keys are already normalized at construction, so the lookup normalizes the *input* instead of
  paying for a case-insensitive comparer on every probe). Two dictionary hits, no per-lookup
  allocation.
- Glyph cache: `ConcurrentDictionary<(string Variant, string Name), Lazy<IconGlyph>>` with
  `LazyThreadSafetyMode.ExecutionAndPublication`. `Lazy` — not a bare `GetOrAdd` factory — is what
  guarantees SPEC §6.1's "cached, **reference-equal**" glyph and "parsed exactly once" even under a
  race, since `GetOrAdd`'s factory can run more than once and hand different instances to concurrent
  callers. On a faulted `Lazy`, `TryRemove` the entry before rethrowing so a transient I/O failure
  is not cached for the life of the process.
- `IconNames` (SPEC §6.1: "lower-case kebab-case, ordered") — the distinct union of names across all
  variants, `Ordinal`-sorted, materialized once at construction and returned as a read-only snapshot.
- **Discovery is eager, parsing is lazy** (SPEC §6.2): the index is fully built in the factory, so
  `IconNames` is complete and a `TryGetGlyph` miss is a dictionary miss rather than an I/O probe.

**Lookup**

`TryGetGlyph(icon, variant, out glyph)`: normalize `icon`; resolve `variant` — `null` →
`DefaultVariant`, otherwise normalize it and require it to be a *known* variant (an unknown variant
is an immediate miss, **never** a fallback to the default). Index miss → `false` with
`glyph = null`. Hit → resolve through the glyph cache. `GetGlyph` calls `TryGetGlyph` and throws
`IconNotFoundException(icon, variant, Name)` on a miss — passing the caller's *original* strings so
the exception echoes what was asked for.

**Miss vs. broken source — settled by SPEC §6.1's precedence table; no open question remains.** The
`Try` prefix governs *absence*, not *corruption*:

- An ordinary **miss** — the icon name or the variant is not in the index — returns `false` with
  `glyph = null` from `TryGetGlyph`, and only `GetGlyph` turns that into `IconNotFoundException`.
- A **broken source** that *is* indexed but cannot be read or parsed — malformed SVG, an I/O error, a
  file that vanished between construction and first parse, a null manifest-resource stream —
  **propagates from both methods** as `SvgParseException` (wrapping the underlying `IOException` per
  SPEC §6.2). `TryGetGlyph` therefore does **not** wrap its cache resolution in a `catch`: reporting a
  malformed file as "icon not found" would hide a real defect behind a blank space.

Say this in the XML docs of both members. FEATURE-3950's `PhosphorIconSet` follows the same table —
there a `.dat` header/count mismatch surfaces as `InvalidDataException` from both methods.

**`Internal/IconNameNormalizer.cs`** — the filename→name and input→key normalization (SPEC §6.2):

- `Normalize(string)`: trim; map `_` and spaces to `-`; split PascalCase by inserting `-` before an
  upper-case char when the previous char is lower-case or a digit, **or** when the previous char is
  upper-case and the next is lower-case (so `HTTPServer` → `http-server`); `ToLowerInvariant`;
  collapse runs of `-`; trim leading/trailing `-`. Empty result → `ArgumentException`.
- `StripVariantSuffix(string name, string variant)`: if `name` ends with `-` + `variant` (Ordinal,
  post-normalization) **and** the remainder is non-empty, return the remainder.

**Filename → icon name.** Drop the extension, `Normalize`, then — when `variantsFromSubfolders` is
`true` — `StripVariantSuffix` with the containing subfolder's name. **This is what makes an extracted
Phosphor tree work with `FromDirectory` out of the box** (SPEC §6.2): `bold/acorn-bold.svg` and
`bold/acorn.svg` both yield icon `acorn` in variant `bold`, and `regular/acorn.svg` — upstream
carries no suffix on regular (SPEC §7.4 point 7) — yields `acorn` in `regular`. Suffix stripping does
**not** apply to `FromFiles` or `FromSvgSources`, which have no variant concept. If two files in one
variant collide on the normalized name, **the first wins** in `Ordinal` filename order (enumeration
is sorted, so the outcome is deterministic).

**`FromDirectory` — path safety (SPEC §6.2, §15)**

- `root = Path.GetFullPath(path)`. `!Directory.Exists(root)` → `DirectoryNotFoundException` naming
  `root`, at construction.
- Variant mode: `Directory.EnumerateDirectories(root)`, `Ordinal`-sorted, **skipping any directory
  whose `FileAttributes` include `ReparsePoint`** — that is the "does not follow directory symlinks
  out of the root" rule, and `FileAttributes.ReparsePoint` is the portable test (`FileSystemInfo.LinkTarget`
  is .NET 6+, unavailable on `netstandard2.0`). Variant name = `Normalize(directoryName)`.
- Enumerate `*.svg` with `SearchOption.TopDirectoryOnly` in each bucket directory — **never**
  `AllDirectories`.
- For each file: `full = Path.GetFullPath(file)` and require containment via an
  `internal static bool IsWithin(string root, string candidate)` helper — `full` must start with
  `root` plus a directory separator, compared `Ordinal` on Unix and `OrdinalIgnoreCase` where
  `Path.DirectorySeparatorChar == '\\'`. A non-contained path is silently dropped, not thrown, so one
  hostile entry cannot deny service on an otherwise valid directory. Per SPEC §6.2 the symlink rule
  is scoped to *directories*; a file symlink whose own path is inside the root is accepted.
- `defaultVariant`: if supplied it must be one of the discovered variants, else `ArgumentException`.
  If not supplied — `"regular"` when present, otherwise the first variant in `Ordinal` order (see
  OPEN QUESTION 3). With `variantsFromSubfolders: false`, `Variants` is empty, `DefaultVariant` is
  `null`, the index has the single `""` bucket, and passing a non-null `defaultVariant` is an
  `ArgumentException`.
- `name` defaults to the directory's own leaf name (SPEC §6.2).

**`FromAssembly`** — enumerate `assembly.GetManifestResourceNames()`, keep those starting with
`resourcePrefix` (`Ordinal`) and ending `.svg` (`OrdinalIgnoreCase`). Manifest names are
dot-flattened, so after stripping the prefix and the `.svg`: the **last** dot-separated segment is
the icon name; if at least one segment precedes it, the one immediately before is the variant, and
anything further left is ignored. Apply `StripVariantSuffix` when a variant was found. `name` defaults
to the assembly's simple name. A prefix matching zero resources → `ArgumentException` naming the
prefix and assembly (see OPEN QUESTION 5).

**`FromFiles`** — one bucket, no variants; each path's filename supplies the name; a path that does
not exist → `FileNotFoundException` at construction (discovery is eager). **`FromSvgSources`** — one
bucket, no variants; the key supplies the name, the value is the SVG text; a null/empty value →
`ArgumentException`. Both: duplicate normalized names → first wins in enumeration order.

### 7. `src/Enigma.Icons/README.md` (packed; SPEC §13)

A real README, correct at the API level shipped here — FEATURE-718F polishes wording and adds
cross-links, it does not fill a blank. Sections:

- Title and a one-line description. **No badges** — badges live in the root `README.md` only
  (SPEC §13.1); a packed README that carries them is wrong.
- What the package is: the framework-agnostic layer — model + parser + icon-set abstraction, **zero
  dependencies**, no rendering. Point Avalonia users at `Enigma.Icons.Avalonia` and Phosphor users at
  `Enigma.Icons.Phosphor`.
- Install (`dotnet add package Enigma.Icons`) and supported TFMs (`netstandard2.0`, `net8.0`,
  `net10.0`).
- The model: `IconViewBox`, `IconLayer` (including *why* `Fill`/`Stroke` are raw strings and what
  `null` / `"none"` / `"currentColor"` mean — SPEC §4.2), `IconGlyph` with paint order and
  `IsSingleLayer`.
- `IIconSet`: the contract, and the "a missing variant is a miss, not a fallback" and reference-equal
  caching guarantees.
- **The parser's supported subset** — reproduce SPEC §5.1's table as a list.
- **The exclusions, explicitly** — gradients, `clipPath`, `mask`, `<defs>`/`<use>`/`<symbol>`,
  `style="…"` and `<style>` CSS, `<text>`, `<image>`, filters, animation, plus
  `fill-opacity`/`stroke-opacity` — with the SPEC §5.1 pointer: for arbitrary artwork use
  `Avalonia.Svg` / `Svg.Skia`; **this library is about icons.** State that a document whose only
  paintable content sits inside an excluded construct throws `SvgParseException` naming it.
- **The worked "use your own SVG folder" example** (SPEC §13) — a directory-tree diagram with variant
  subfolders, the `SvgIconSet.FromDirectory` call, a `GetGlyph` call, and the note that a
  `-<variant>` filename suffix is stripped so an extracted Phosphor tree works unchanged. Plus one
  short `FromSvgSources` snippet as the in-memory escape hatch.
- Security notes: DTDs prohibited (no XXE, no entity expansion), `MaxDocumentBytes` (default 1 MiB,
  process-wide), and `FromDirectory` path containment.
- Licence: MIT, link `LICENSE.md`.

### 8. `tests/Enigma.Icons.UnitTests/` (SPEC §12, §12.1)

`Enigma.Icons.UnitTests.csproj`: `net10.0`, `<OutputType>Exe</OutputType>`,
`<ImplicitUsings>disable</ImplicitUsings>` (SPEC §2 rule 2 and §3.2 — solution-wide, **not** a
packable-only property; SPEC §10.4 excludes it from the packaging table for exactly that reason, so the
test project sets it as well), `<IsPackable>false</IsPackable>`, one
`<PackageReference Include="xunit.v3" />` with **no `Version`** (CPM, pin already in
`Directory.Packages.props`), and a `ProjectReference` to `src/Enigma.Icons`. **No**
`Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio`, or coverlet — the MTP runner comes from
`global.json` (SPEC §12). Do **not** set `GenerateDocumentationFile` here: with
`TreatWarningsAsErrors` it would make CS1591 fire on every public test class.

- **Confirm the `xunit.v3` pin resolves — FEATURE-21C4's delegated obligation.** SPEC §3.3's "verify at
  restore time, do not assume" box could not be exercised in FEATURE-21C4 (nothing compiled there), so
  it delegated the `xunit.v3` half to **this** item — the first project in the solution to reference the
  package (its two sibling delegations land in FEATURE-3ADD for the Avalonia group and FEATURE-469B for
  the gallery pair). Confirm `xunit.v3` **3.2.2** resolves on the test project's first restore and
  record the resolved version in the completion doc. If it does not resolve, raise it as a **recorded
  deviation** and get it decided — bumping the pin is *not* pre-authorized here, and
  `Directory.Packages.props` stays untouched (see Out of scope).

`AssemblyInfo.cs`: `[assembly: CollectionBehavior(DisableTestParallelization = true)]`, justified in
a comment — `SvgIconParser.MaxDocumentBytes` is process-global mutable state (SPEC §5), so a test
that lowers it would otherwise be observable by tests running concurrently in other classes.

`TestSupport/TempDirectory.cs`: an `IDisposable` that creates a uniquely named directory under
`Path.GetTempPath()` and recursively deletes it on dispose. Used by every `FromDirectory` test.

`TestAssets/`: a handful of tiny hand-written `.svg` files under `TestAssets/thin/` and
`TestAssets/bold/`, declared `<EmbeddedResource>` so `FromAssembly` has real resources to discover,
and also copied to output for the `FromDirectory`/`FromFiles` cases. The set **must** include a
`circle`, an `ellipse` and a rounded-`rect` SVG, so the arc-flag conversions are exercised through
`SvgIconSet` as well as through `SvgIconParser` directly, with their expected path data pinned in
the tests (see Notes / risks).

Test files, covering **every bullet of SPEC §12.1**:

| File | Covers |
|---|---|
| `SvgIconParserShapeTests.cs` | one focused case per SPEC §5.1 shape row: `path`; `rect` plain / `rx` only / `rx`+`ry`; `circle`; `ellipse`; `line`; `polyline`; `polygon`. Assert the **exact** emitted path data for `circle`/`ellipse`/rounded `rect` (the arc flags — see Notes / risks). |
| `SvgIconParserGroupTests.cs` | nested `<g>` inheritance and child override; `opacity` multiplied down a `<g>` chain (`0.5 × 0.4 == 0.2`). |
| `SvgIconParserTransformTests.cs` | `translate`, `scale` (one arg and two), `rotate` with and without a centre, `matrix`, `skewX`, `skewY`, a composed list, and an ancestor-`<g>` transform composed with the element's own. Includes the non-commutative ordering assertion and the **identity ⇒ `d` verbatim** case. |
| `SvgIconParserPaintTests.cs` | `fill="none"` (and `IsFilled == false`); `fill="currentColor"` → `null`; `fill-rule="evenodd"`; the stroke attribute quartet on the layer. |
| `SvgIconParserViewBoxTests.cs` | `viewBox` present; absent with `width`/`height`; absent entirely → `IconViewBox.Default`. |
| `SvgIconParserErrorTests.cs` | not XML; empty string; `<svg>` with no paintable child; a document whose only content is an unsupported construct — **assert the message names it**; input exceeding `MaxDocumentBytes`, both overloads, restoring the static in a `finally`. |
| `SvgIconParserSecurityTests.cs` | the two mandatory security tests — detailed below. |
| `IconViewBoxTests.cs` | constructor guards (zero, negative, `NaN`), `Default`, equality/hash. |
| `IconLayerTests.cs` | `pathData` and `opacity` guards; `IsFilled`/`IsStroked` truth table including `null` fill and `"none"`. |
| `IconGlyphTests.cs` | null/empty `layers` guards, `IsSingleLayer`, and the **defensive copy** (mutate the source `List<IconLayer>` after construction; the glyph is unaffected). |
| `SvgIconSetTests.cs` | `FromDirectory` with and without variant subfolders; `-<variant>` suffix stripping; `FromAssembly`; `FromFiles`; `FromSvgSources`; case-insensitive plus `snake_case`/PascalCase lookup; **a variant the set lacks is a miss, not a fallback**; a missing icon → `TryGetGlyph` false and `GetGlyph` throwing `IconNotFoundException` with the right `IconName`/`Variant`/`SetName`; caching returns a **reference-equal** instance (`Assert.Same`); a non-existent directory throws at construction. Plus a small parallel-`TryGetGlyph` case asserting the cache stays reference-consistent. |
| `SvgIconSetErrorPrecedenceTests.cs` | **Both halves of SPEC §6.1's precedence table, one test each.** (a) *Miss* — a name that is not in the set returns `false` from `TryGetGlyph` and throws **nothing**; same for a variant the set lacks. (b) *Broken source* — a `FromSvgSources` entry whose value is not valid SVG, and a `FromDirectory`/`FromFiles` file deleted after construction, each throw `SvgParseException` from **`TryGetGlyph`** (not just from `GetGlyph`), with the deleted-file case wrapping the underlying `IOException`. Also assert the faulted entry is **not** cached: a second call after the file is restored succeeds (the `Lazy` `TryRemove` in Design step 6). |
| `SvgIconSetPathSafetyTests.cs` | the traversal rejection: a directory symlink inside the root pointing at an outside folder containing an `.svg` is **not** followed (that icon never appears in `IconNames`), and a direct test of the `IsWithin` containment helper via `InternalsVisibleTo`. Skip the symlink half gracefully where symlink creation is unprivileged-unavailable (Windows). |
| `IconNameNormalizerTests.cs` | kebab / `snake_case` / PascalCase / acronym inputs and `StripVariantSuffix`, via `InternalsVisibleTo`. |

**The two security tests (SPEC §12.1, mandatory).**

1. **XXE must not resolve.** Write a temp file whose contents are a unique sentinel
   (`ENIGMA-XXE-SENTINEL-<guid>`). Build an SVG carrying
   `<!DOCTYPE svg [<!ENTITY xxe SYSTEM "file:///…">]>` and a `<path d="&xxe;"/>`. Assert
   `SvgParseException` is thrown (`DtdProcessing.Prohibit` rejects the DTD outright, so the entity is
   never even *defined*), **and** — the part that actually proves non-resolution — assert the
   sentinel string appears **nowhere** in the exception's `ToString()`. Also run the negative-control
   variant that would succeed under a permissive reader and assert the sentinel never reaches any
   `IconLayer.PathData`. Delete the temp file in a `finally`.
2. **Billion laughs must fail fast.** A document with nested entity definitions
   (`lol` → `lol1` → … expanding exponentially) must throw `SvgParseException` and must not expand.
   **No timing assertion** — SPEC §15 forbids timing-based tests; the proof is structural: DTD
   processing is prohibited, so the entities are never defined and there is nothing to expand.
   Optionally also assert the parse allocates nothing pathological by keeping the document small.

Note that `xunit.v3` ships analyzers and `TreatWarningsAsErrors` applies to test projects too
(SPEC §2.1) — any `xUnit1xxx`/`xUnit2xxx` diagnostic is a build failure, so use the analyzer-preferred
assertion forms.

### 9. `Enigma.Icons.slnx` — solution registration

Per SPEC §3.4's incremental-growth contract, FEATURE-24DD appends **exactly two** rows and nothing
else:

```xml
<Folder Name="/src/">
  <Project Path="src/Enigma.Icons/Enigma.Icons.csproj" />
</Folder>
<Folder Name="/tests/">
  <Project Path="tests/Enigma.Icons.UnitTests/Enigma.Icons.UnitTests.csproj" />
</Folder>
```

FEATURE-21C4 leaves the slnx with the four folders and zero projects; the `/samples/` and `/tools/`
folders stay empty here. The slnx must never reference a project that does not exist yet (SPEC §3.4),
so do not pre-add the Phosphor, Avalonia, gallery, or generator rows. Keep the step idempotent.

## Dependencies & ordering

- **Requires FEATURE-21C4.** This item consumes `Enigma.Icons.slnx`, `Directory.Build.props`
  (SPEC §3.2 — supplies every shared build setting the csproj inherits), `Directory.Packages.props`
  (SPEC §3.3 — the `xunit.v3` 3.2.2 pin the test project needs; a missing pin is NU1010),
  `global.json` (SPEC §3.1 — the SDK pin and the MTP test runner), `.editorconfig`/`.gitattributes`
  (LF and code-style enforcement), and root `LICENSE.md` (packed by this csproj). Second row of the
  roadmap; SPEC §16.
- **Blocks everything after it.** FEATURE-2DDE needs the layer model that SPEC §7.2's resource format
  mirrors; FEATURE-3950 implements `IIconSet` and constructs `IconGlyph`/`IconLayer` directly;
  FEATURE-3ADD converts them to Avalonia types and accepts any `IIconSet`; FEATURE-469B, 718F and
  74DC follow those (roadmap "Sequencing & dependencies" 2–8).
- No dependency on any Phosphor artefact — this project must build and test green with
  `src/Enigma.Icons.Phosphor` entirely absent from the tree.

## Acceptance criteria

- [ ] **Null-argument behaviour is established here as the precedent for every `IIconSet`**, because
      `SvgIconSet` is the first implementation and FEATURE-3950's `PhosphorIconSet` is required to
      match it: `TryGetGlyph(null, …)` returns `false` **without throwing** (a null name is a miss, per
      SPEC §6.1's precedence table), while `GetGlyph(null, …)` fails fast with
      `ArgumentNullException`. A null guard sits **ahead of** name normalization so a null never
      reaches it. Both halves are tested, and the rule is stated in the `IIconSet` XML docs so the
      second implementation cannot diverge.

- [ ] `dotnet build src/Enigma.Icons/Enigma.Icons.csproj` succeeds with **zero warnings across all
      three TFMs** (`netstandard2.0`, `net8.0`, `net10.0`). Warnings are errors (SPEC §2.1);
      `GenerateDocumentationFile` is on, so a missing or broken XML doc (CS1591/CS1574) fails the
      build — every public member is documented (SPEC §2.8).
- [ ] The csproj has **zero `<PackageReference>` entries** (SPEC §2.10) and
      `Directory.Packages.props` gained **no** new pin (SPEC §3.3). If a polyfill proved
      unavoidable, its csproj comment justifies it and the deviation is recorded in the completion doc.
- [ ] Packaging metadata present and correct: `PackageId Enigma.Icons`, `Title`, `Version 1.0.0`,
      `Description`, `PackageTags`, `PackageReadmeFile`, `PackageLicenseFile`, `RepositoryUrl`,
      `PackageProjectUrl`, `RepositoryType git`, `IncludeSymbols` + `SymbolPackageFormat snupkg`,
      `GenerateDocumentationFile`; `GeneratePackageOnBuild` **not** set; `PackageReleaseNotes`
      **absent** (FEATURE-74DC owns it); `README.md` and `..\..\LICENSE.md` packed;
      `THIRD-PARTY-NOTICES.md` **not** packed (SPEC §14.2).
- [ ] `IsTrimmable` and `IsAotCompatible` are set **only** for `'$(TargetFramework)' != 'netstandard2.0'`
      (SPEC §10.4), and the `net8.0`/`net10.0` builds are free of IL2xxx/IL3xxx.
- [ ] Public surface matches SPEC §4, §5, §5.3 and §6.1/§6.2 **exactly** — type names, member names,
      signatures, and optional-parameter defaults. Verified by inspection against those sections.
- [ ] `IIconSet` declares **no default interface members**; `GetGlyph` is implemented explicitly by
      `SvgIconSet` (SPEC §6.1).
- [ ] `SvgIconParser` reads exclusively through an `XmlReader` configured with the SPEC §5.2 settings
      block; `MaxDocumentBytes` is enforced **before** parsing on both overloads; the walk is
      iterative, not recursive.
- [ ] `IconLayer` never carries a transform — every transform is baked into the emitted path data,
      and an identity transform leaves `<path d>` byte-identical (SPEC §5.1).
- [ ] A document whose only paintable content is inside an unsupported construct throws
      `SvgParseException` whose message **names that construct** (SPEC §5.1).
- [ ] `dotnet test Enigma.Icons.slnx` is green — **the whole suite**, zero warnings, with at least
      one focused test per SPEC §12.1 bullet.
- [ ] The two security tests pass: the XXE sentinel never appears in the result or the exception, and
      the billion-laughs document fails fast with **no timing-based assertion** (SPEC §12.1, §15).
- [ ] `SvgIconSet` behaviour verified: eager discovery / lazy cached parsing; reference-equal glyph
      caching (`Assert.Same`); `-<variant>` suffix stripping; an unknown variant is a **miss, not a
      fallback**; a path-traversal / directory-symlink attempt is rejected; a non-existent directory
      throws at construction (SPEC §6.2, §12.1).
- [ ] **SPEC §6.1's precedence table is implemented and tested on both halves:** a miss returns `false`
      from `TryGetGlyph` (and `IconNotFoundException` from `GetGlyph`), while a broken source — malformed
      SVG, I/O error, vanished file, null resource stream — **propagates `SvgParseException` from
      `TryGetGlyph` too**. One test per half; both members' XML docs say so.
- [ ] **`xunit.v3` 3.2.2 resolved on the test project's first restore**, and the resolved version is
      recorded in the completion doc — SPEC §3.3's "verify at restore time, do not assume" obligation,
      delegated to this item by FEATURE-21C4. A failure to resolve is a **recorded deviation**, not a
      pin bump; `Directory.Packages.props` is not edited either way.
- [ ] `src/Enigma.Icons/README.md` documents the supported subset **and** the exclusions, and carries
      the worked "use your own SVG folder" example (SPEC §13).
- [ ] `Enigma.Icons.slnx` gained exactly the two rows in Design step 9 and nothing else; the solution
      restores and builds as a whole (SPEC §3.4).
- [ ] Every text file created is LF with a final newline (SPEC §2.7).
- [ ] **Roadmap + this plan flipped to `DONE`** (`docs/roadmap.md` FEATURE-24DD row; this file's
      status header). (DoD criterion 4.)
- [ ] **Completion doc `docs/done/FEATURE-24DD.md` written** — summary, files touched, deviations,
      build/test evidence. (DoD criterion 5.)

## Notes / risks

- **`netstandard2.0` restricts the language and the BCL — plan around it, do not paper over it.**
  SPEC §15 (Compatibility) and §3.3 forbid adding a polyfill package without a justified csproj
  comment. Concretely, the following are unavailable and each has a named workaround above:
  `System.HashCode` (hand-roll `IconViewBox.GetHashCode`); `Span<T>`/`ReadOnlySpan<T>` and
  `stackalloc`-based helpers, **including internally**, because `System.Memory` is not in the
  reference set; `[NotNullWhen]` and friends (SPEC §6.1's `out IconGlyph?` signature deliberately
  does not need them); range/index syntax `s[1..]` (`System.Index`/`System.Range` are 2.1+);
  `string.Contains(string, StringComparison)` and `StartsWith(char)`; the two-argument
  `Path.GetFullPath`; `FileSystemInfo.LinkTarget`; `Math.Clamp`. Language features that need no
  runtime support — file-scoped namespaces, switch expressions, pattern matching, target-typed
  `new` — are fine at `LangVersion 14`. Avoid `record` and `init`/`required` (they need
  `IsExternalInit`/`RequiredMemberAttribute`).
- **Arc flags are the single easiest thing in this item to get subtly wrong.** The `circle`/`ellipse`
  two-arc conversion and the rounded-`rect` corner arcs depend on `large-arc-flag`/`sweep-flag`
  being right *in SVG's y-down coordinate system*, where the positive angular direction reads as
  clockwise on screen. The base library has no geometry evaluator to check against, so the defence
  must be **self-contained in this item**: `TestAssets/` carries a `circle`, an `ellipse` and a
  rounded-`rect` SVG (Design step 8), each exercised through `SvgIconSet` as well as through
  `SvgIconParser`, and the expected emitted path data — arc flags included — is pinned
  character-for-character in the tests (Design step 4d gives the canonical circle string). Nothing
  downstream may stand in for that check: FEATURE-469B's gallery renders `PhosphorIconSet` glyphs,
  which come from `.dat` path data and never touch `SvgIconParser` (SPEC §9.1). That gallery does
  additionally render one `SvgIconSet.FromSvgSources` glyph built from these same primitives as a
  visual cross-check, but it is a bonus — this item's correctness must not depend on it.
- **Transform baking has three orderings to keep straight.** (1) Within one `transform` attribute the
  list composes so the **last-listed** primitive applies to the geometry **first** (right-to-left in
  effect), i.e. `M = A × B × C`. (2) An ancestor `<g>`'s transform sits on the **left** of the
  element's own: `M_ancestor × M_own`. (3) Inside `SvgPathTransformer`, an arc's `sweep-flag` flips
  iff the matrix determinant is negative. Test (1) and (2) with a deliberately non-commutative pair.
- **Arc recomputation fallback.** If the closed-form ellipse recovery in Design step 4g cannot be
  validated with confidence, the acceptable degradation is to convert `A` to cubic Béziers *before*
  transforming and emit `C` commands. It is correct and easier to verify; the cost is that a
  transformed arc no longer round-trips as an arc. Choose the closed form first; record the choice in
  the completion doc.
- **Cross-TFM determinism of emitted numbers.** Default `double.ToString()` is
  shortest-round-trippable on .NET Core 3.0+ and `G15` on .NET Framework — the same SVG would emit
  different path data depending on which runtime loaded the `netstandard2.0` assembly. That is why
  `Internal/SvgNumber.Format` is mandatory and used by every emitter (Design step 4h).
- **No serialization constructors on the two exceptions.** Adding the
  `(SerializationInfo, StreamingContext)` overload triggers `SYSLIB0051` on `net8.0`/`net10.0`, which
  `TreatWarningsAsErrors` makes a build error.
- **`MaxDocumentBytes` is process-global mutable state**, which is what SPEC §5 specifies. Tests that
  lower it are only safe because parallelization is disabled assembly-wide (Design step 8); do not
  re-enable it without reworking those tests.
- **Symlink tests are environment-sensitive.** Creating a directory symlink is unprivileged on Linux
  but may not be on Windows. The path-safety test must skip that half gracefully rather than fail;
  the `IsWithin` unit test covers the logic unconditionally.
- **Deliberate additions to the SPEC listings** (additive, not contradictory; record in the
  completion doc): `IconViewBox` gains `==`/`!=`/`Equals(object?)`/`GetHashCode()`/`ToString()`, which
  SPEC §4.1's snippet elides; `IconGlyph` rejects a null *element* inside `layers`; the csproj carries
  `InternalsVisibleTo` for the test project.
- **`TryGetGlyph` "never throws" vs. a vanished file — SETTLED, no longer an open question.** SPEC §6.1
  now carries an explicit precedence table and SPEC §15's Error-handling row defers to it: the `Try`
  prefix governs *absence*, not *corruption*. A **miss** returns `false` from `TryGetGlyph` (and
  `IconNotFoundException` from `GetGlyph`); a **broken source** — malformed SVG, an unreadable or
  vanished file, a missing embedded-resource stream, a `.dat` header/count mismatch — **propagates**
  from *both* methods as `SvgParseException` / `InvalidDataException` / the underlying `IOException`.
  Implement exactly that (Design step 6), test both halves (Design step 8), and document it on both
  members. Nothing here is left for FEATURE-3950 to decide: `PhosphorIconSet` follows the same table.
- **OPEN QUESTION 2 — `IconNotFoundException` message with a null variant.** SPEC §5.3 gives only the
  variant-present form. Prescribed: drop the parenthetical —
  `Icon 'acorn' was not found in icon set 'Phosphor'.`
- **OPEN QUESTION 3 — `DefaultVariant` when none is supplied.** SPEC §6.2 makes `defaultVariant`
  optional but SPEC §6.1 requires `variant == null` to mean `DefaultVariant`, so a variant-ful set
  with a null default would miss on every defaulted lookup. Prescribed ladder: the explicit argument
  wins; else `"regular"` if discovered; else the first variant in `Ordinal` order.
- **OPEN QUESTION 4 — `fill-opacity` / `stroke-opacity`.** They appear neither in SPEC §5.1's
  supported table nor in its exclusion list. Prescribed: **ignored** (not folded into
  `IconLayer.Opacity`), and listed among the README's exclusions so the behaviour is documented
  rather than surprising. Only `opacity` is multiplied down the chain — which is all the Phosphor
  duotone corpus uses (SPEC §7.4 point 2).
- **OPEN QUESTION 5 — `FromAssembly` with a prefix matching nothing.** SPEC §6.2 is silent.
  Prescribed: `ArgumentException` naming the prefix and the assembly, because a mistyped
  `resourcePrefix` silently yielding an empty icon set is a nasty debugging trap.
- **Line endings.** LF and final-newline discipline is self-enforcing from FEATURE-21C4's
  `.gitattributes` (`* text=auto eol=lf`) and `.editorconfig` (SPEC §2.7, §3.5); author every file
  with LF and nothing needs normalizing.
