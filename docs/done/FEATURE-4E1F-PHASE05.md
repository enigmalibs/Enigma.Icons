# FEATURE-4E1F-PHASE05 — Output panel & Generate command

**Branch:** `feature/feature-4e1f-phase05-export-ui` · **Plan:** `docs/plan/FEATURE-4E1F.md`

## Summary

The studio is now end to end. A pinned output panel carries the folder box and *Browse…*, the base
name with a live "writes `app.ico` and `app-256.png`" hint, the two size check-lists, and a
*Generate* button that writes the files and reports what it wrote in the status line.

`Generate` is disabled until it can actually run, and says why: *Choose an output folder.* · *That
folder does not exist.* · *The base name must be a plain file name.* · *Select at least one size.* The
base-name rule is `ExportRequest.IsValidBaseName` — the same predicate the request constructor
enforces, so the button and the constructor cannot disagree and neither learns the answer by catching
an exception per keystroke.

The request is built from the **live control values**, not from the debounced `CurrentDesign`:
pressing Generate within 150 ms of moving a slider must write what the user just set, not what the
preview last rendered.

## Files/modules touched

**Created**

- `tools/…/Services/IFolderPicker.cs` · `Services/AvaloniaFolderPicker.cs` — resolves the window
  lazily per call, so the `MainWindow → ViewModel → picker → MainWindow` cycle never forms, and
  returns null for every failure path.
- `tools/…/SizeOption.cs` — one observable tick-box.
- `tests/…/TestSupport/FakeFolderPicker.cs` · `tests/…/ExportUiTests.cs` (16 tests).

**Modified**

- `tools/…/MainWindowViewModel.cs` — the two size lists, `OutputDirectory`, `BaseName`,
  `IsExporting`, `GenerateHint`, `BrowseCommand`, `GenerateCommand`, and `BuildExportRequest`.
- `tools/…/MainWindow.axaml` — the output pane, and the right column restructured so that pane is
  pinned (see *Deviations* 2).
- `tools/…/App.axaml.cs` — registers `IFolderPicker`.
- `tests/…/MainWindowViewModelTests.cs` — the constructor gained two dependencies.
- `docs/roadmap.md`, `docs/plan/FEATURE-4E1F.md` — statuses.

**Documentation freshness sweep:** nothing to do. No README, `CLAUDE.md` or `docs/SPEC.md` statement
describes the studio's UI or its output contract yet — SPEC §18 is PHASE06's planned work, and it is
the next dev.

Untouched, as the plan requires: everything under `src/`, every `<Version>`, `RELEASENOTES.md`,
`THIRD-PARTY-NOTICES.md`, the root README, the gallery sample, and every generated Phosphor asset. No
package reference was added. No screenshot was committed — SPEC §1 makes `docs/img/gallery.png` the
single permitted screenshot path.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Which sizes are offered? | ICO 16/24/32/48/64/128/256, all ticked; PNG 16/32/64/128/256/512/1024, with 256/512/1024 ticked | The ICO list is Windows' recommended set and 256 is the format's ceiling. The PNG defaults are the About/splash assets the icon itself serves poorly; the small PNGs are already inside the `.ico`, so they start unticked. |
| Why a hint beside a disabled button? | `GenerateHint` names the blocker | Every run starts in the "no folder chosen" state, and a greyed button with no explanation is a guessing game. |
| Which design does Generate write? | A fresh `BuildDesign()`, not `CurrentDesign` | The preview is debounced by 150 ms. Using the rendered design would silently write the previous slider position if the user was quick. A test asserts exactly this divergence. |
| How does the ViewModel hear about a tick-box? | Subscribes to each `SizeOption.PropertyChanged` | A `CheckBox` reports to its own item, not to the ViewModel, and `CanExecute` depends on at least one being ticked. The alternative — an `Action` callback threaded through the item's constructor — puts view-model plumbing inside a list item. |
| Folder picker failure modes | All of them return null | A cancelled dialog, a platform with no picker, a portal that is not running, and a cloud folder with no local path are one outcome to the caller: no folder chosen, keep what you had. |
| Export failure handling | Caught broadly, reported in the status line | `AsyncRelayCommand` runs a task nobody awaits, so an escaping exception is an unobserved crash rather than a message. A test deletes the output folder between the button enabling and the command running — the race no `CanExecute` can close. |
| Re-entrancy | `IsExporting` gates `CanExecute`, and the command is not concurrent | Two exports into one folder would interleave their writes. |

## Build/test evidence

| Check | Result |
|---|---|
| `dotnet build Enigma.Icons.slnx` | Build succeeded · **0 Warning(s)**, 0 Error(s) — the studio project's own warning count was read, because `AVLN*` XAML diagnostics are not promoted by `TreatWarningsAsErrors` |
| `dotnet test --solution Enigma.Icons.slnx` | **667 total · 662 passed · 0 failed · 5 skipped** (up from 651/646 — 16 new tests) |
| Studio assembly alone | 188 passed, 0 skipped |
| Pre-existing skips | The same 5 `SvgIconSetPathSafetyTests` symlink cases. No new skip. |
| Fix cycles used | **1 of 3** — see *Deviations* 1 |

`Generate_WritesTheAssetsAndReportsThem` is the end-to-end proof inside the suite: it drives the real
`GenerateCommand` with the real `IconExporter` and the real `AvaloniaIconRasterizer`, and asserts the
`.ico` and the PNG exist on disk and that the status line names the count and the folder.

**Visual verification.** The built executable was launched on a real desktop session and captured
twice — once to find the layout problem in *Deviations* 2, once to confirm the fix. The final capture
shows, without scrolling: the catalog, the 256 px preview with its 64/48/32/16 strip, every design
control, and the whole output panel with all seven ICO boxes ticked, PNG 256/512/1024 ticked, the
`writes app.ico and app-256.png` hint following the base-name box, the Angle slider correctly greyed
while the plate is solid, and *Generate* disabled beside *Choose an output folder.*

## Deviations & follow-ups

**1. One fix cycle: the ViewModel constructor grew two parameters.** `MainWindowViewModelTests` from
PHASE04 constructed it with the rasterizer alone, so the first build after the ViewModel change
failed with two `CS7036`s. Fixed by adding `FakeFolderPicker` and routing both test classes through
one `Create` helper — and the null-guard test now covers all three dependencies rather than one.

**2. The output panel was moved out of the scroll viewer, which the plan did not specify.** The first
capture showed it below the fold at the window's default height: the *Generate* button — the whole
point of the window — needed a scroll to reach. The right column is now a two-row grid with the
preview and design controls scrolling above a **pinned** output pane, and the gradient-angle and
corner-radius sliders share a row so the design pane fits above it. The window's default height went
from 800 to 820. This is a layout correction found by looking at the running app, which is what the
visual-verification step is for.

**3. `GenerateHint` is public API the plan did not name**, for the same reason
`ExportRequest.IsValidBaseName` was added in PHASE03: a disabled control has to be able to explain
itself, and the explanation belongs beside the rule, not in the view.

**4. Follow-up, not a defect: no cancellation UI.** `IconExporter.ExportAsync` takes a
`CancellationToken` and the ViewModel passes `CancellationToken.None`. A full export of the default
set is well under a second, so a Cancel button would be UI nobody reaches. The seam is there if that
ever changes.

**5. Follow-up: the folder is not remembered between runs.** Deliberate, per the plan's out-of-scope
list — the studio keeps no state on disk. It would be a one-file settings store if it ever becomes
annoying.

**6. Line endings (recommendation only).** All new files are LF with a final newline;
`.gitattributes` already governs this. No action taken.
