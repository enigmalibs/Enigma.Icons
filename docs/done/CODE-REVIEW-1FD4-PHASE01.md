# CODE-REVIEW-1FD4-PHASE01 — [Medium] `Icon.Render` re-parses geometry on every render pass

**Branch:** `review/code-review-1fd4-phase01-geometry-cache` · **Plan:** `docs/plan/CODE-REVIEW-1FD4.md`

## Summary

`Icon.DrawLayers` called `Geometry.Parse(layer.PathData)` for every layer on every render pass, so a
resize, a theme switch or a scroll through the gallery's virtualized grid re-parsed several hundred
bytes of path data per visible icon per frame. Parsing now happens once per glyph.

The cache is a `ConditionalWeakTable<IconGlyph, Geometry[]>` in the new internal
`IconGeometryCache`, keyed on the **glyph instance** rather than on its path string. That is what
makes it invalidation-free: `IconGlyph` is immutable, and `PhosphorIconSet` and `SvgIconSet` both
hand out a cached, reference-equal instance per `(icon, variant)`, so a key can never come to mean
different geometry. Weak keys mean a custom `IIconSet` that goes out of scope takes its glyphs and
their geometry with it — nothing is rooted for the life of the process.

The cache is **scoped to `Icon.Render`**; `ToGeometry`/`ToDrawing` still parse afresh. See
*Deviations* for the evidence behind that decision.

## Files/modules touched

**Created**

- `src/Enigma.Icons.Avalonia/IconGeometryCache.cs` — the cache. Internal, no public API growth.
- `tests/Enigma.Icons.Avalonia.UnitTests/IconGeometryCacheTests.cs` — 10 tests.

**Modified**

- `src/Enigma.Icons.Avalonia/Icon.cs` — `DrawLayers` resolves through the cache; the old
  "No geometry cache: …" comment is replaced by one explaining the key choice; the class `<remarks>`
  gained a paragraph on parse-once-per-glyph and the weak keys.
- `src/Enigma.Icons.Avalonia/IconGlyphExtensions.cs` — `<remarks>` now states that these methods
  parse afresh on every call *by design*, and why.
- `docs/roadmap.md`, `docs/plan/CODE-REVIEW-1FD4.md` — statuses.

**Modified by the documentation freshness sweep** (accepted by the user; the two README edits are a
deliberate extension of the plan's scope, which lists "the READMEs" as out of scope)

- `CLAUDE.md` — the "only remaining roadmap item is FEATURE-6FA1" claim was stale from the moment
  the planning commit added `CODE-REVIEW-1FD4` and `FEATURE-1608`. It now names both as buildable
  and keeps FEATURE-6FA1 as the one row `/build` must skip.
- `src/Enigma.Icons.Avalonia/README.md` — a fourth "worth stating outright" bullet on
  parse-once-per-glyph and the weak keys, and a note in the extension-methods section that
  `ToGeometry`/`ToDrawing` return a fresh, safely-mutable instance per call.

Untouched, as the plan requires: every `Assets/*.dat`, `PhosphorIcon.g.cs`, `PhosphorIconNames.g.cs`,
`tools/Enigma.Icons.Generator`, `docs/SPEC.md`, all three `<Version>`/`<PackageReleaseNotes>` values,
`RELEASENOTES.md`, and the root README callout. No `PackageReference` was added anywhere.

## Deviations & follow-ups

**1. The cache is scoped to `Icon.Render` — `ToGeometry`/`ToDrawing` were deliberately left alone.**
Design step 3 required this to be verified against Avalonia 12.1.0 before sharing. Two throwaway
probe tests were run and then deleted:

- *Framework ownership is a non-issue.* `Avalonia.Media.Geometry` has no parent/owner field at all
  (`_isDirty`, `_canInvaldate`, `_platformImpl`, `Changed`, `_resource`); only `GeometryCollection`
  carries a `Parent` back-pointer, and it points at the group, not at the child. One instance in two
  `GeometryGroup`s, in two `GeometryDrawing`s, and simultaneously in a group and a direct
  `DrawGeometry` call all behaved correctly.
- *Consumer-visible aliasing is the real objection.* `Geometry.Parse` returns a `StreamGeometry`
  whose `Transform` is **public and settable**, and setting it visibly moves the instance
  (`Bounds` went `0,0,64,64` → `100,0,64,64`). `ToGeometry`/`ToDrawing` hand their result to the
  caller, so a shared instance would let one consumer's mutation reach every other consumer of the
  same glyph — a silent behaviour change to a shipped public API, which this item may not make.

`Icon.Render` has no such exposure: the geometry goes into a transient draw operation and is never
handed out. So the cache lands exactly where the per-frame cost is, which the plan names as an
acceptable outcome. Two tests (`ToGeometry_StillReturnsAFreshInstance`,
`ToDrawing_StillReturnsFreshGeometry`) pin the decision down so it cannot be undone by accident.

**2. The `Pen` allocation in `TryGetLayerPaint` was left as-is** (design step 4 allows this). It
allocates only when `layer.IsStroked`, and the `.dat` resources carry no stroke data, so no built-in
Phosphor glyph ever reaches it. Hoisting it is not clean: the pen depends on the caller's brush as
well as the layer, so a cache would need a `(layer, brush)` key — more machinery and a second
invalidation surface for an allocation that only a custom stroked `SvgIconSet` can trigger.

**3. Two intentional behaviour changes at the edges**, both documented in `IconGeometryCache`:

- All layers of a glyph are parsed on first use, including a `fill="none"` spacer the paint pass
  skips, so the array stays index-aligned with `IconGlyph.Layers`.
- A glyph with one malformed layer now paints **nothing** instead of painting the layers ahead of
  the bad one. Half a broken glyph was never the intended output, and `Icon.Render` still swallows
  the failure — `Render_MalformedPathData_PaintsNothingRatherThanThrowing` covers it. A failed parse
  caches nothing, so it is retried rather than poisoning the entry.

**4. Reuse is asserted by marking, not by reference identity, in the render tests.**
`DrawingGroup.Open()`'s recording context does not store the `Geometry` it is handed — it re-wraps
the platform impl in Avalonia's internal `PlatformGeometry`, so a recorded
`GeometryDrawing.Geometry` is never reference-equal to what `Icon.Render` passed, cached or not.
(`Geometry.PlatformImpl` is documented in `Avalonia.Base.xml` but is not accessible to consumers.)
The tests therefore translate the cached instances by a marker offset between two renders and assert
the mark comes out the other end — a render that re-parsed could not possibly see it. Per-layer
reference equality itself is asserted directly against the internal cache, as the acceptance
criterion asks.

**5. Findings deliberately NOT actioned** (recorded per the plan, so a future reviewer does not
re-raise them): `SvgIconParser.MaxDocumentBytes` staying process-wide mutable static config, and
`Icon.Render`'s blanket `catch (Exception)`. Both are SPEC-mandated and were declined during review.

**6. Line endings:** no CRLF noise observed in any touched file; no action taken, none recommended.

## Build/test evidence

| Check | Result |
|---|---|
| `dotnet build Enigma.Icons.slnx -c Release` | Build succeeded · **0 Warning(s)**, 0 Error(s) — count read explicitly, not inferred from the exit code |
| `dotnet test --solution Enigma.Icons.slnx -c Release` | **444 passed**, 0 failed, 0 skipped (434 before, +10 new) |
| Generator `--check` (snapshot extracted to the scratchpad, never the working tree) | 8 artifacts match the committed bytes · exit 0 · 3,951,294 B total, exact to SPEC §7.3 |
| `Cache_DoesNotRetainAGlyphThatBecomesUnreachable` | run 5× in isolation and again in the full Release suite — stable, no GC flakiness |

Every existing test passes **unmodified**: `IconControlTests`, `LayerPaintTests`,
`IconGlyphExtensionsDrawingTests` and `IconGlyphExtensionsGeometryTests` were not touched.

### Acceptance criteria

| Criterion | Evidence |
|---|---|
| Two renders of the same glyph use a reference-equal `Geometry` per layer, asserted via the internal cache | `GetLayerGeometries_ReturnsTheSameArrayForTheSameGlyph` (array **and** per-element `Assert.Same`), `Render_TwiceForTheSameGlyph_ReusesTheCachedGeometryPerLayer`, `Render_TwoIconsSharingOneGlyph_ShareTheCachedGeometry` |
| Behaviour unchanged for single-layer, multi-layer, duotone, stroked and `fill="none"` spacer layers | The four existing test classes pass unmodified; `GetLayerGeometries_IsIndexAlignedWithTheLayers_SpacersIncluded` covers the spacer alignment |
| An unreachable glyph is not retained by the cache | `Cache_DoesNotRetainAGlyphThatBecomesUnreachable` — the glyph is cached in a `[MethodImpl(NoInlining)]` frame, then proven dead through a `WeakReference` after a full GC |
| `Icon.Render` still never throws, including on malformed third-party path data | `Render_MalformedPathData_PaintsNothingRatherThanThrowing`, plus the untouched `Render_ThrowingIconSet_…` and `Render_UndefinedKind_…` |
