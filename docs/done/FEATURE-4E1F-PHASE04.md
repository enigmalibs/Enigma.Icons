# FEATURE-4E1F-PHASE04 — Main window: catalog, design controls, live preview

**Branch:** `feature/feature-4e1f-phase04-design-ui` · **Plan:** `docs/plan/FEATURE-4E1F.md`

## Summary

The studio now has its design surface. Left: a virtualized, debounce-filtered list of all 1,512
Phosphor icons, each row previewing its glyph in the currently selected weight. Right: the live
preview — the real 256 px render on a checkerboard, with a 64/48/32/16 strip beside it — over the
design controls: weight, glyph colour, glyph size, plate fill mode, plate colour, second colour,
gradient angle, corner radius. A status line describes the current design.

**The preview is the export.** Every image on screen comes out of the same `IIconRasterizer`, at the
size it will be written, through the same `IconDesign` instance the exporter will be handed in
PHASE05. There is no second composition path that could disagree with the files on disk.

Two selection states, deliberately: `SelectedIcon` is what the list highlights and goes null the
moment a filter hides the chosen row; `ActiveIcon` is what the design uses and only changes when the
user picks something. Typing in the search box therefore never empties the preview.

## Files/modules touched

**Created**

- `tools/…/IconEntry.cs` · `IconThumbnail.cs` · `WeightOption.cs` · `FillModeOption.cs` — the four
  small record types the templates bind to.
- `tests/…/MainWindowViewModelTests.cs` — 12 tests.

**Modified**

- `tools/…/MainWindowViewModel.cs` — rewritten from the PHASE01 placeholder: catalog, filter,
  selection, the nine design properties, the debounced preview, and `CurrentDesign`.
- `tools/…/MainWindow.axaml` — the two-column layout, the checkerboard `DrawingBrush`, the three
  `ColorPicker`s, the sliders, and the status line.
- `tools/…/MainWindow.axaml.cs` — focuses the search box on `Opened`.
- `docs/roadmap.md`, `docs/plan/FEATURE-4E1F.md` — statuses.

**Documentation freshness sweep:** nothing to do. No README, `CLAUDE.md` or `docs/SPEC.md` statement
describes the studio's UI yet — SPEC §18 is PHASE06's planned work.

Untouched, as the plan requires: everything under `src/`, every `<Version>`, `RELEASENOTES.md`,
`THIRD-PARTY-NOTICES.md`, the root README, the gallery sample, and every generated Phosphor asset. No
package reference was added. **No screenshot was committed** — SPEC §1 makes `docs/img/gallery.png`
the single permitted screenshot path, and this phase's verification image stayed in the scratchpad.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| What happens when a filter hides the selected icon? | Two properties: `SelectedIcon` (list state, nullable) and `ActiveIcon` (design state, never null) | A `ListBox` pushes null the instant its selected item leaves the source collection. Folding that into one property means a keystroke in the search box blanks the preview — the opposite of what the user is doing. Clearing the search restores the highlight. |
| Weight and fill-mode selectors | `WeightOption` / `FillModeOption` records carrying their own label | Binding the enum directly would put `Enum.ToString` on the display path (SPEC §2.11) and hard-wire the identifier as the label. |
| Preview background | A tiled `DrawingBrush` checkerboard | An app icon is transparent outside its rounded corners, and against a flat panel that is invisible — which is exactly the part worth seeing before writing the file. |
| Thumbnail strip | 64/48/32/16, rendered natively and shown at 1:1 | A glyph scale that looks generous at 256 px is often a smudge at 16. Showing them scaled would have hidden the very thing the strip is for. |
| Preview bitmap lifetime | Never disposed | An `Image` keeps the `Bitmap` it was handed and draws it on the render thread; disposing the previous one on each render is a race with a native surface, not a tidy-up. The 150 ms debounce bounds the churn at about 340 KB per render. Documented on the decode helper. |
| Preview failure | Caught broadly, preview cleared, message in the status line | It runs off a dispatcher tick, where an escaping exception takes the window down rather than the one image that failed — the same reasoning SPEC §10.2 gives for `Icon.Render`. |
| `CurrentDesign` initialization | Assigned in the constructor before `RegeneratePreview` | Avoids a `= null!` on a property that bindings read, and keeps it valid even if the very first render fails. |
| Default weight | `Fill` | A solid glyph is what reads at 16 px, which is the size an app icon is judged at most often — and it is what the reference `Enigma.MarkdownEditor` icon uses. |

## Build/test evidence

| Check | Result |
|---|---|
| `dotnet build Enigma.Icons.slnx` | Build succeeded · **0 Warning(s)**, 0 Error(s) — the studio project's own warning count was read, not just the exit code, because `AVLN*` XAML diagnostics are not promoted by `TreatWarningsAsErrors` |
| `dotnet test --solution Enigma.Icons.slnx` | **651 total · 646 passed · 0 failed · 5 skipped** (up from 639/634 — 12 new tests) |
| Pre-existing skips | The same 5 `SvgIconSetPathSafetyTests` symlink cases. No new skip. |
| Fix cycles used | **1 of 3** — see *Deviations* 1 |
| Generator `--check` | Not run: nothing under `src/Enigma.Icons.Phosphor` or `tools/Enigma.Icons.Generator` was touched in any phase of this item. |

**Visual verification.** The built executable was launched on a real desktop session and its window
captured (the image stayed in the scratchpad). Confirmed by eye:

- the catalog lists and scrolls, with the selected row highlighted and each row previewing its glyph;
- the 256 px preview shows the blue plate and the white `markdown-logo` glyph, with the checkerboard
  visible through the rounded corners — reproducing the existing `Enigma.MarkdownEditor` icon;
- the 64/48/32/16 strip tracks it and is legible at every size;
- all three `ColorPicker`s render with their swatches, which is the proof that the
  `Avalonia.Controls.ColorPicker` theme `StyleInclude` in `App.axaml` is doing its job;
- with `Solid` selected, the second-colour picker and the angle slider are visibly disabled;
- the status line reads `markdown-logo · Fill · Solid plate · 22% corner · 60% glyph`.

## Deviations & follow-ups

**1. One fix cycle: `Assert.All(..., throwIfEmpty: true)` does not exist here.** That overload arrives
in xUnit v3 **4.0**, and this solution is pinned at `xunit.v3` 3.2.2 (a deliberate divergence from the
house `xunit-v3` skill, recorded in the plan). Replaced with an explicit `Assert.NotEmpty` in front of
the `Assert.All`, which is what the overload is sugar for, plus a comment so the next reader does not
try the shorter form again.

**2. ViewModel tests were added, which the plan scheduled for PHASE05.** The plan lists
`ExportRequestBuildingTests` under PHASE05 and no tests at all here. The filter and selection rules
written in this phase are synchronous, cheap to assert, and are exactly the kind of logic that
silently regresses, so 12 tests landed with the code that introduced them. PHASE05 still owns the
export-request tests.

**3. `MainWindowViewModel.DefaultDesign` from PHASE01 is gone.** It was a placeholder static that
existed only so the scaffolded ViewModel had something to show. Its values now live where they are
used — the constructor's `ActiveIcon` seed and the property initializers — so there is one source of
the default design rather than two that could drift.

**4. Re-renders are debounced through a `DispatcherTimer` and are not unit-tested.** A dispatcher
timer cannot be advanced from a test without sleeping, and sleeping in a suite is how flakes start.
What the debounce coalesces — the render itself — is covered by the rasterizer's own tests, and the
wiring was verified by driving the real window. Noted rather than worked around.

**5. Line endings (recommendation only).** All new files are LF with a final newline;
`.gitattributes` already governs this. No action taken.
