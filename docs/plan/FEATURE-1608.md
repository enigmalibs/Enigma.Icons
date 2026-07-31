**Status:** TODO · Single-phase · Branch `feature/feature-1608-internals-doc`

# FEATURE-1608 — `docs/internals.html`, the maintainer's how-it-works explainer

## Objective

Write **one** self-contained HTML page, `docs/internals.html`, that explains to the repository owner
how this solution actually works end to end: how the Phosphor artwork is embedded, what the `.dat`
format is, how SVG is parsed, what `Matrix2D` is really for, and how a glyph becomes pixels in
Avalonia. Roughly **2-3 pages of reading** — condensed, not exhaustive.

No source-code change. No README change. No `CLAUDE.md` change.

## Context

- The four existing READMEs are **consumer-facing** (how to install and use the packages) and
  `docs/SPEC.md` is a **1,537-line build specification**. Neither answers "how does this thing work,
  in twenty minutes". That gap is what this page fills.
- The trigger question was specifically about the `Matrix2D` transform code, on the reasonable
  assumption that it participates in rendering the Phosphor icons. **It does not** — and saying so
  clearly is one of the page's jobs (see *Section 2* below).
- The page is **standalone by decision**: it is *not* linked from `CLAUDE.md`, the root README, or
  `docs/roadmap.md`. Do not add such a link. It is also **not** published as an Artifact.
- `docs/` is never packed into any NuGet package (SPEC §1), so this file ships nothing to consumers.

## Scope

### In scope
- `docs/internals.html` — the single new file, and the item's only deliverable.

### Out of scope
- Any `.cs`, `.axaml`, `.csproj` or generated-asset change.
- `README.md` (root or packed), `CLAUDE.md`, `docs/SPEC.md`, `docs/roadmap.md`, `RELEASENOTES.md`.
  SPEC §13.2 gives the root README's shipped state to FEATURE-718F and its what's-new callout to
  FEATURE-74DC — this item edits neither.
- A consumer usage guide, an API reference, or an icon catalog. The READMEs and the gallery's search
  already cover those.
- Publishing the page anywhere, or generating it from source. It is hand-authored prose, kept
  accurate by being read, not by a build step.
- The `CODE-REVIEW-1FD4` fixes. **Sequencing note:** if that item is built first, re-check Section 3
  against the shipped `Icon.Render` before describing the per-render path.

## Hard technical constraints

1. **Self-contained, opens from `file://`, no network.** All CSS inline in a `<style>` block. No CDN
   script, no external stylesheet, no web font, no remote image, no `fetch`. The page must render
   identically on a machine with no internet connection.
2. **Diagrams are hand-authored inline `<svg>`.** No mermaid (it needs a script the page cannot
   load), no image files, no ASCII art. Two diagrams:
   - the end-to-end pipeline: upstream `.svg` → generator → `.dat` → embedded resource → weight
     table → `IconGlyph` → `Geometry` → screen;
   - the layer/transform model: nested `<g>` with transforms collapsing into one flat list of
     `IconLayer`s carrying baked path data.
   Use `currentColor` and CSS custom properties inside the SVG so both diagrams follow the page
   theme rather than hard-coding ink colours.
3. **Light and dark**, via `@media (prefers-color-scheme: dark)`. Readable in both; no theme toggle
   needed.
4. **LF line endings and a final newline** (SPEC §2.7). No BOM.
5. Code samples are `<pre><code>` with hand-applied styling — **no** syntax-highlighting library.
6. Wide content (the `.dat` format table, long path-data samples) scrolls inside its own
   `overflow-x: auto` container; the page body must never scroll horizontally.

## Content — four sections

Every factual claim must be **verified against the shipped code**, not written from memory or from
the SPEC alone. Where code and SPEC disagree, the code is what the page describes; note the
divergence at the bottom of the page rather than silently picking one.

### Section 1 — The asset pipeline: `.svg` → `.dat` → `IconGlyph`

- The pinned snapshot: `docs/reference/phosphor/phosphor-2.1.1-svgs-flat.tar.gz`, 9,072 files,
  six weight directories, 1,512 identical names per weight.
- What `tools/Enigma.Icons.Generator` does: reads every `<path>`'s `d` and `opacity` **verbatim**
  (no re-flow, no re-rounding), asserts the six name sets match, emits six `.dat` files plus
  `PhosphorIcon.g.cs` and `PhosphorIconNames.g.cs`. Deterministic — same input, byte-identical
  output — which is what makes `--check` a real staleness gate and `git diff` the review surface.
- **The `v1` wire format, annotated field by field.** Header
  `v1⇥<weight>⇥<viewBox>⇥<count>`, then one line per icon: `name⇥layer⇥layer…`, layers in paint
  order, each optionally prefixed `@<opacity>:`. UTF-8 no BOM, LF, final newline, lines sorted
  `Ordinal`. Show a real three-line excerpt including a duotone `@0.2:` line.
- **Why six resources and not 9,072.** Be accurate and do not repeat the myth: it is **not** a
  package-size win — compressed, the two formats are a wash and `.dat` is marginally *larger*
  (SPEC §7.3 has the measured numbers). The real reasons are 6 manifest-resource entries instead of
  9,072, no XML at runtime, reviewable refreshes, and a layered model duotone needs.
- **The two-level cache** (`PhosphorIconSet`): a `ConcurrentDictionary` of weight → name-to-line
  table, built on first touch of that weight; and a `ConcurrentDictionary` of `(weight, name)` →
  `IconGlyph`, giving the reference-equal guarantee. Never evicted; bounded by the corpus.
- The header validation tripwires — version, weight name, view box, declared count, duplicate
  names — and why a mismatch is `InvalidDataException` (a broken source) rather than a miss.

### Section 2 — The SVG parser, and what `Matrix2D` is actually for

**Lead with the correction**, because it is the question that prompted this page:

> `Matrix2D` and the whole transform machinery are **not** used to render Phosphor icons. The `.dat`
> resources contain path data, not XML — `SvgIconParser` is not on that code path at all. The parser
> exists for *your own* SVG, via `SvgIconParser.Parse` and `SvgIconSet`.

Then explain the parser itself:

- The supported subset: `<path>` (`d` taken verbatim), `<rect>`/`<circle>`/`<ellipse>`/`<line>`/
  `<polyline>`/`<polygon>` converted to path data, `<g>` flattened with presentation-attribute
  inheritance, `transform` lists, `opacity`, `fill`/`fill-rule`, the stroke quartet, `viewBox`. And
  what is deliberately out: gradients, clips, masks, `<use>`, CSS, text, filters, animation.
- **Transform baking — the core idea.** An element's effective transform is
  `M_ancestor × M_own`, composed as the walker descends; `SvgPathTransformer.Bake` then applies that
  matrix to **every coordinate** and emits new absolute path data, so an `IconLayer` never carries a
  transform. Renderers therefore need no transform support at all. When the matrix is the identity —
  the overwhelmingly common case — the `d` string passes through **byte for byte**.
- The interesting details worth a paragraph each: `H`/`V` are expanded to `L` because a horizontal
  segment stops being horizontal after a rotation; `S`/`T` need only their explicit points
  transformed, because an affine map preserves the reflected-control-point relation
  (`M(2P − Q) = 2M(P) − M(Q)`); an elliptical arc has its ellipse **re-derived** by eigen-decomposing
  `E′E′ᵀ` where `E′ = L·R(φ)·diag(rx, ry)`, and its sweep flag flips when the transform mirrors
  (negative determinant).
- **Opacity multiplies down the chain** while every other presentation attribute is overridden by
  the nearest specifier — `<g opacity="0.5">` around `opacity="0.4"` gives `0.2`.
- **`"currentColor"` normalizes to `null`**, which is precisely what makes "inherit the renderer's
  brush" the default, and why `IconLayer` carries raw paint *strings* rather than a colour type
  (the base package has zero dependencies and so no colour type to use).
- **XML hardening**: `DtdProcessing.Prohibit` + `XmlResolver = null` closes XXE and billion-laughs
  outright; `MaxDocumentBytes` caps input before parsing; the walk is **iterative over an explicit
  stack**, never recursive, so deep nesting cannot overflow the stack. Say why this matters on
  `netstandard2.0`/.NET Framework, where `XmlDocument`'s defaults are *not* safe.

### Section 3 — From `IconGlyph` to pixels in Avalonia

- The four ways to render, and when each is right:
  the `Icon` control, `ToGeometry()`, `ToDrawing()`/`ToDrawingImage()`, and the
  `{ei:IconGeometry}` / `{ei:IconImage}` markup extensions.
- **Why the `Icon` control is the default answer.** It is a plain `Control` overriding
  `MeasureOverride` and `Render`, not a `TemplatedControl` — so the package ships no XAML and a
  consumer needs no `<StyleInclude>`. Its `Foreground` is `TextElement.ForegroundProperty` re-owned,
  so it inherits from the enclosing text scope and follows theme switches with no binding written.
  A markup extension structurally cannot do this: it is evaluated once at load time.
- **The deliberate asymmetry:** `Icon.Render` never throws and paints nothing on failure (to keep the
  XAML previewer alive); the markup extensions fail fast (a bad value in XAML is an authoring error).
- **Why `ToGeometry` loses duotone.** A `GeometryGroup` has no per-child opacity and one shared fill
  rule, so a duotone glyph collapsed to a single `Geometry` paints its tinted backing layer at full
  opacity. Use `ToDrawing` or the `Icon` control instead. Show the difference.
- The view-box → bounds scaling maths and the four `Stretch` modes, including why `UniformToFill` is
  the only one that clips.
- The single shared layer-paint decision (`TryGetLayerPaint`) that `ToDrawing` and `Icon.Render`
  both funnel through, so the two cannot drift; the consumer's brush always wins for **fills**,
  while a layer's named **stroke** paint is honoured — and why that asymmetry is right.

### Section 4 — Architecture and packaging (keep it short; link out)

A closing section, not a restatement of `docs/SPEC.md`:

- The three packages and the one-way dependency chain
  `Enigma.Icons ← Enigma.Icons.Phosphor ← Enigma.Icons.Avalonia`, and why the split exists (a future
  WPF renderer, and other artwork families as sibling packs behind `IIconSet`).
- The TFM sets and what drove them: `netstandard2.0;net8.0;net10.0` on the two lower packages keeps
  .NET Framework 4.6.2+ reachable; `net8.0;net10.0` on the Avalonia one because Avalonia 12 ships no
  `netstandard2.0` asset. Note the consequence — per-TFM assembly duplication makes the Phosphor
  nupkg ≈3.4-3.5 MB, a known and accepted trade (SPEC §7.3), **not** a defect to optimize away.
- Zero third-party runtime dependencies, and what that rules out (no logging, no caching library, no
  polyfill packages); trim/AOT cleanliness via literal-name resource lookup and generated name
  tables instead of `Enum.ToString`.
- **The enum-ordinals caveat**, prominently: `PhosphorIcon` values are positional. Never persist or
  transmit `(int)icon` — persist `PhosphorIconNames.ToKebabCase(icon)` and read it back with
  `TryParse`.
- One line pointing at `docs/SPEC.md` for the authoritative detail and `docs/roadmap.md` for history.

## Acceptance criteria

1. `docs/internals.html` exists, is valid HTML, and **opens correctly from `file://` with networking
   disabled** — verified, not assumed.
2. Grep the file: no `http://` or `https://` in any `src`, `href` to a stylesheet, or script tag.
   Outbound links to nuget.org / phosphoricons.com in prose are fine; loaded *resources* are not.
3. Both inline-SVG diagrams render, scale with the page, and are legible in light **and** dark.
4. No horizontal scrolling of the page body at 1280px, 1024px and a narrow (~700px) window; wide
   blocks scroll within their own container.
5. Reading length is ~2-3 pages. If a section outgrows that, cut detail and link to `docs/SPEC.md` —
   do not let the page become a second SPEC.
6. **Every technical claim is verified against the shipped code**, with `file:line` checked at
   authoring time. In particular: the `v1` header field order, the `@<opacity>:` prefix form, the
   `Stretch` behaviour, the `Foreground`-inherits mechanism, and the "Phosphor never touches
   `SvgIconParser`" claim.
7. LF line endings, final newline, no BOM.
8. **Nothing else in the repository is modified** — `git status` shows exactly one new file (plus the
   roadmap/plan/done updates the workflow itself requires).
9. Build and test suite are untouched and still green (nothing to build for the page itself; state
   that explicitly in the completion doc, per the `dev-workflow` no-build clause).

## Definition of Done

1. Nothing to compile — state so explicitly; confirm the solution still builds clean and the 434+
   tests still pass, unchanged.
2. The acceptance criteria above are verified, including the offline `file://` open.
3. `docs/roadmap.md` and this plan's status flip to `DONE`.
4. `docs/done/FEATURE-1608.md` is written and lands in the **same commit** as the page.
5. Print the suggested commit message (`docs(FEATURE-1608): …`) and stop — the user commits.
