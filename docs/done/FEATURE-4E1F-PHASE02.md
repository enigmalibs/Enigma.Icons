# FEATURE-4E1F-PHASE02 — Layout math & Avalonia rasterizer

**Branch:** `feature/feature-4e1f-phase02-rasterizer` · **Plan:** `docs/plan/FEATURE-4E1F.md`

## Summary

The studio can now turn an `IconDesign` into pixels. Two pieces:

`Rendering/IconLayout` is the geometry, and nothing but arithmetic over Avalonia value types — no
render backend needed, so every layout decision is unit-testable on its own. It answers three
questions: where the plate is and how round its corners are (`PlateRect`), where the glyph lands on
it (`GlyphTransform`), and which way a gradient runs (`GradientLine`).

`Rendering/AvaloniaIconRasterizer` draws that layout into an off-screen `RenderTargetBitmap` and
hands back either a PNG file or raw straight-alpha BGRA — the two forms the exporter needs, one
render per call. The glyph reaches the bitmap through **`IconGlyphExtensions.ToDrawing`**, the same
conversion the shipped `Icon` control paints with, so duotone's tinted backing layer, per-layer
opacity, strokes and `fill="none"` behave here exactly as they do on screen with no second
implementation to keep in step.

`GlyphTransform` is deliberately `Icon.Render`'s `Stretch.Uniform` arithmetic with one change — the
target is the centred `size × glyphScale` square rather than the full bounds — and the XML docs say
so, so the two cannot drift silently.

## Files/modules touched

**Created**

- `tools/Enigma.Icons.AppIconStudio/Rendering/IconLayout.cs` — `PlateRect`, `GlyphTransform`,
  `GradientLine`, plus `MinSizePx`/`MaxSizePx`.
- `tools/Enigma.Icons.AppIconStudio/Rendering/IIconRasterizer.cs` — `RenderPng` / `RenderBgra`.
- `tools/Enigma.Icons.AppIconStudio/Rendering/AvaloniaIconRasterizer.cs` — the implementation.
- `tests/Enigma.Icons.AppIconStudio.UnitTests/IconLayoutTests.cs` — 41 tests, no platform.
- `tests/Enigma.Icons.AppIconStudio.UnitTests/AvaloniaIconRasterizerTests.cs` — 16 tests, headless
  Skia.

**Modified**

- `tools/Enigma.Icons.AppIconStudio/App.axaml.cs` — registers
  `IIconRasterizer` → `AvaloniaIconRasterizer` as a singleton.
- `docs/roadmap.md`, `docs/plan/FEATURE-4E1F.md` — statuses.

**Documentation freshness sweep:** nothing to do. No README, `CLAUDE.md` or `docs/SPEC.md` statement
describes the rendering path yet — SPEC §18 is PHASE06's planned work — so this phase falsified
nothing.

Untouched, as the plan requires: everything under `src/`, every `<Version>`, `RELEASENOTES.md`,
`THIRD-PARTY-NOTICES.md`, the root README, the gallery sample, and every generated Phosphor asset.
No package reference was added anywhere.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| How does the glyph get painted? | `glyph.ToDrawing(brush)` then `Drawing.Draw(context)` inside a pushed transform | The alternative was re-deriving the per-layer paint rules, but `IconGlyphExtensions.TryGetLayerPaint` is `internal` to `Enigma.Icons.Avalonia` and visible only to *its* test project. Going through the public `ToDrawing` reuses the exact shipped semantics instead of forking them. |
| Where is the gradient line? | Through the plate centre, half-length `(\|cos θ\| + \|sin θ\|) / 2` | It is what puts 45° exactly on two opposite corners instead of stopping short, and it keeps the visible colour range constant as the angle turns. A naive unit-vector line would compress the ramp off-axis. |
| Straight vs premultiplied alpha | `CopyPixels` into a `WriteableBitmap` declared `Bgra8888`/`Unpremul` | The render target is **premultiplied**; reading it directly would darken every part-transparent edge pixel once an ICO BMP frame reinterpreted it as straight alpha. Skia does the conversion during the copy. A test asserts a 50 %-alpha plate still reports a near-full colour channel. |
| One render or two per frame? | One per call, two methods | A frame is either a PNG payload or a BMP payload, never both, so rendering twice would be waste with no caller. |
| Native size vs downsampling | Each size rendered natively | A 16 px frame's corner radius and stroke weights get resolved by the rasterizer rather than blurred by a resample, and it keeps the preview byte-identical to the export at any size. |
| Where do per-size bounds live? | `IconLayout.MinSizePx` = 1, `MaxSizePx` = 2048 | Above the 1,024 px the studio will offer and below where a square 32-bpp buffer stops being reasonable (2,048 px is 16 MiB) — a sanity bound, not a limitation. |
| `RenderTargetBitmap` lifetime | Created last, inside a `try`/`catch` that disposes on throw | It holds a native surface; a throw between construction and the caller's `using` would leak it. Glyph resolution and brush construction happen first so a bad icon can never reach that window. |

## Build/test evidence

| Check | Result |
|---|---|
| `dotnet build Enigma.Icons.slnx` | Build succeeded · **0 Warning(s)**, 0 Error(s), first try |
| `dotnet test --solution Enigma.Icons.slnx` | **566 total · 561 passed · 0 failed · 5 skipped** (up from 509/504 — 57 new tests, all green on the first run) |
| Studio assembly alone | 87 passed, 0 skipped — confirming every `[AvaloniaFact]` really executed rather than being quietly skipped for want of a platform |
| Pre-existing skips | The same 5 `SvgIconSetPathSafetyTests` symlink cases. No new skip. |
| `CS0618` | None. `Save(Stream, BitmapEncoderOptions)` is used throughout; the obsolete `Save(Stream, int?)` appears nowhere. |
| Dependencies | No `System.Drawing`, SkiaSharp or ImageSharp anywhere in the tool — Skia is reached only as Avalonia's own backend. |

**Visual verification.** A throwaway `[AvaloniaFact]` wrote four probe PNGs to the scratchpad and was
then deleted (it is not in the commit). All four were inspected:

- `MarkdownLogo`/Fill, white on `#3B72F0`, 22 % radius, 60 % scale — reproduces the existing
  `Enigma.MarkdownEditor` icon closely enough to be its replacement.
- The same design at 32 px — the glyph stays legible; native rendering was the right call.
- `Rocket`/Duotone on a 45° purple→cyan gradient — both duotone tones are visible, which is the
  proof that the layered glyph model survives the whole path.
- `Heart`/Bold on a 135° gradient at `cornerRadiusRatio = 0.5` — a clean circle, confirming the one
  slider covers square, squircle and circle.

## Deviations & follow-ups

**1. No deviation from the plan.** Every file, method and test the phase called for exists, and the
acceptance criteria were met on the first build and the first test run — no fix cycle was needed.

**2. `IconLayout` takes `int sizePx`, not `double`.** The plan's sketch wrote `double size`. An
output frame is a whole number of pixels in every case the studio has, and an `int` lets the same
range check serve the rasterizer, so the signature was tightened. Purely a narrowing; nothing in the
plan depended on the wider type.

**3. Follow-up, not a defect: supersampling.** Small frames are rendered natively and anti-aliased by
Skia. If a 16 px frame of some thin-weight glyph ever looks muddier than the equivalent from another
tool, rendering at 4× and downsampling is the lever — deliberately not built now, since native
rendering is what keeps preview and export identical.

**4. Line endings (recommendation only).** All new files are LF with a final newline; `.gitattributes`
already governs this. No action taken.
