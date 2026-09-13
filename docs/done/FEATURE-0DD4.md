# FEATURE-0DD4 — Reset button in the app icon studio

**Branch:** `feature/feature-0dd4-icon-studio-reset` · **Plan:** `docs/plan/FEATURE-0DD4.md`

## Summary

The studio has a **Reset** button, right-aligned on the design pane's header row. One click puts the
icon, weight, glyph colour, glyph size, fill mode, both plate colours, gradient angle, corner radius,
the base name and both size check-lists back to the values the window opened with, and clears the
search box so the restored icon is visible and highlighted again.

The button was the easy half. The half worth reviewing is that **every startup value now lives in one
place**. Before this item they were scattered across eleven property initializers, and a `Reset()`
that re-stated those literals would have been a second source of truth — one that silently stops
restoring the real defaults the first time one of them is tuned. So the values moved into a new
`StudioDefaults`, which the ViewModel's property initializers *and* `Reset()` both read, and the
"which sizes start ticked" question moved onto the options themselves as
`SizeOption.IsSelectedByDefault`, so a reset can restore a whole list without knowing what
`BuildSizeOptions` was handed.

Two deliberate exceptions, both documented in the code and in the button's tooltip:

- **The output folder is kept.** It is a session destination, not a design value: clearing it would
  re-disable *Generate* and send the user back through the file dialog — an undo that costs more than
  it saves.
- **Reset does not wait for the preview debounce.** `SchedulePreview` exists to coalesce slider drags
  and keystrokes; a reset is one deliberate click, so it stops the timer and calls
  `RegeneratePreview()` directly — the same closing move the constructor already makes. The preview,
  the thumbnail strip and the status line are therefore correct the instant the button is released,
  and the tests can assert the whole outcome without advancing a `DispatcherTimer`.

## Files/modules touched

**Created**

- `tools/Enigma.Icons.AppIconStudio/StudioDefaults.cs` — the single source of truth for every startup
  value. `const` where C# allows one; get-only static properties for the three `Color`s, because a
  struct cannot be a compile-time constant. `CornerRadiusRatio`, `GlyphScale` and
  `GradientAngleDegrees` are **forwarded** from `IconDesign`/`PlateFill`, not copied, so the design
  model stays the authority on its own ranges. The class remarks record why the output folder and the
  size ticks are deliberately *not* here.
- `tests/Enigma.Icons.AppIconStudio.UnitTests/ResetTests.cs` — 12 `[AvaloniaFact]` tests.

**Modified**

- `tools/Enigma.Icons.AppIconStudio/MainWindowViewModel.cs` — every default initializer now reads
  `StudioDefaults`; new `ResetCommand` (a plain `RelayCommand`, always executable) and the public
  `Reset()` it runs; new private statics `DefaultIconEntry`, `FindWeight`, `FindFillMode` and
  `RestoreDefaultSizes`.
- `tools/Enigma.Icons.AppIconStudio/SizeOption.cs` — new `IsSelectedByDefault`, captured from the
  constructor's `isSelected`.
- `tools/Enigma.Icons.AppIconStudio/MainWindow.axaml` — the design pane's first line became a
  two-column `Grid`: the existing *Glyph* section label, and the Reset button with a tooltip naming
  what it restores and that the folder is kept.
- `tools/Enigma.Icons.AppIconStudio/README.md` — documents the button and the folder exception.
- `docs/SPEC.md` §18.5 — names *Reset* in the UI description, and adds the one-source-of-truth rule
  so the next change to a default knows where to make it.
- `CLAUDE.md` — records the item as `DONE` and states the `StudioDefaults` rule.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Where does the weight default come from, now that `WeightOptionList[4]` is a magic index? | A `FindWeight(PhosphorWeight)` lookup over the selector list, with `StudioDefaults.Weight` as the input | The default is stated once, *as a weight*; reordering the selector can no longer silently change what the studio opens with. Six comparisons, run on construction and on a reset — never on the render path, so SPEC §2.11 is untouched (it is a comparison, not an `Enum.Parse`) |
| What if no selector entry matches? | `throw new InvalidOperationException` | A silent fallback to entry 0 would turn "someone dropped a weight from the list" into "the default quietly changed". Unreachable while the list covers all six declared weights |
| Are the lookups static *methods* rather than static *fields*? | Methods | A `static readonly DefaultWeightOption` field would have to be declared after `WeightOptionList` or initialize to null — static initializers run in textual order. A method has no ordering hazard at all |
| Does `Reset()` re-filter unconditionally? | Yes, one explicit `ApplyFilter()` at the end | `SearchText`'s setter only filters when the text actually *changed*, so a reset from an already-empty search box would otherwise leave the old row highlighted. `ResetTests.Reset_RestoresTheHighlightEvenWhenTheSearchBoxIsAlreadyEmpty` is exactly that case |
| Does the constructor call `Reset()` to share the code? | No | `ActiveIcon` and `CurrentDesign` are non-nullable, and the compiler's definite-assignment analysis does not see through a method call — CS8618 would fire. The constructor keeps its direct assignments and shares the *values* through `StudioDefaults`, which is the part that matters |
| Does `Reset()` end with an explicit `RefreshGenerateState()`? | No | Every mutation it performs already routes through a setter that refreshes — `BaseName`'s setter, and `OnSizeOptionChanged` for each tick. A call that can only ever be redundant is a call a reviewer has to disprove |
| Is Reset disabled during an export? | No | It touches no file and the running export already holds its own `ExportRequest`; blocking the button would be state to maintain for a case that cannot go wrong |

## Deviations & follow-ups

- **No deviation from the plan.** All seven acceptance criteria were met as written, including the
  nine bullets of criterion 6.
- `StudioDefaults.SearchText` is spelled `""` rather than `string.Empty` — only the former is a
  compile-time constant, and the class is all `const` where it can be. Noted in its XML docs so the
  next reader does not "fix" it.
- **Line endings (recommendation only, no action taken):** the working tree checks out CRLF
  (`core.autocrlf=true`), while `.gitattributes` pins `* text=auto eol=lf` plus `*.cs text eol=lf`, so
  the committed bytes are LF as SPEC §2.7 requires. Nothing to do; recorded because the working-tree
  files *look* mixed if inspected directly.
- Follow-up, not taken here: the three `.axaml` panes are getting long enough that the design
  controls could become their own `UserControl`. That is a refactor, not this item's scope.

## Build/test evidence

| Gate | Result |
|---|---|
| `dotnet build Enigma.Icons.slnx` | **Build succeeded · 0 Warning(s) · 0 Error(s)** — every project, the two `.axaml`-compiling ones included, reported no `AVLN*` diagnostic |
| `dotnet test --solution Enigma.Icons.slnx` | **679 total · 674 passed · 0 failed · 5 skipped** — up exactly 12 from the 667 `FEATURE-4E1F-PHASE06` left behind, which is the 12 new tests; the 5 skips are still the pre-existing `SvgIconSetPathSafetyTests` symlink cases |
| Generator staleness gate (`--check`) | **exit 0** — "8 artifacts match the committed bytes" |
| `git diff --name-only -- src/` | **empty** — nothing under `src/` was touched, no `<Version>` moved, `Directory.Packages.props` gained no pin |
| Compiled bindings | `AvaloniaUseCompiledBindingsByDefault` is `true` and `MainWindow` carries `x:DataType`, so `{Binding ResetCommand}` was verified by the XAML compiler, not at runtime |
| Launched | `dotnet run --project tools/Enigma.Icons.AppIconStudio` ran for 25 s on this Windows desktop session and stayed up with a clean log — the window opens and every binding resolves |

Fix cycles used: **0** — the build and the whole suite were green on the first run.
