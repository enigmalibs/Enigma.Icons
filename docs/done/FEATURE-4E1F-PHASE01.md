# FEATURE-4E1F-PHASE01 — Project scaffolding & design model

**Branch:** `feature/feature-4e1f-phase01-scaffolding` · **Plan:** `docs/plan/FEATURE-4E1F.md`

## Summary

Two new **non-packable** projects join the solution — `tools/Enigma.Icons.AppIconStudio`, an Avalonia
desktop app, and `tests/Enigma.Icons.AppIconStudio.UnitTests` — taking the slnx from eight
`<Project>` entries to ten. The app boots end to end: Avalonia's classic desktop lifetime runs it,
a `HostApplicationBuilder` supplies the services, and `MainWindow` is *resolved* from the container,
never constructed. What it shows is a deliberate placeholder; the catalog, controls, preview and
output panel land in PHASE04 and PHASE05.

The phase's real deliverable is the **design model**: `PlateFillMode`, `PlateFill` and `IconDesign`,
the immutable description of one composed icon that every later phase reads. Because the same
instance drives both the preview and the export, the preview cannot drift from what is written to
disk.

One package pin was added — `Avalonia.Controls.ColorPicker` 12.1.0, into the version-coupled Avalonia
group. It is referenced by exactly one project, and that project is not packable, so nothing enters
a `.nupkg` dependency graph and SPEC §2.10 is untouched.

## Files/modules touched

**Created — `tools/Enigma.Icons.AppIconStudio/`**

- `Enigma.Icons.AppIconStudio.csproj` — `net10.0`, `WinExe`, `IsPackable=false`,
  `AvaloniaUseCompiledBindingsByDefault=true`, `ApplicationManifest`; the gallery's Avalonia package
  set plus `Avalonia.Controls.ColorPicker`; one `ProjectReference` to `src/Enigma.Icons.Avalonia`.
- `.editorconfig` — suppresses `AVLN3001` for `*.axaml`, mirroring the gallery's copy. See
  *Deviations*.
- `app.manifest` — PerMonitorV2 DPI awareness.
- `Program.cs` · `App.axaml` · `App.axaml.cs` — the house bootstrap. `App.axaml` additionally
  `StyleInclude`s `avares://Avalonia.Controls.ColorPicker/Themes/Fluent/Fluent.xaml`.
- `MainWindow.axaml` · `MainWindow.axaml.cs` · `MainWindowViewModel.cs` — placeholder shell.
- `Design/PlateFillMode.cs` · `Design/PlateFill.cs` · `Design/IconDesign.cs` — the design model.

**Created — `tests/Enigma.Icons.AppIconStudio.UnitTests/`**

- `Enigma.Icons.AppIconStudio.UnitTests.csproj` — `net10.0`, `Exe`, `xunit.v3` +
  `Avalonia.Headless.XUnit`, no VSTest packages.
- `TestAppBuilder.cs` — headless app builder with `UseHeadlessDrawing = false` and `.UseSkia()`,
  which PHASE02's rasterizer tests require.
- `PlateFillTests.cs` (14 tests) · `IconDesignTests.cs` (16 tests) — 30 in the new assembly.

**Modified**

- `Enigma.Icons.slnx` — two appended `<Project>` entries.
- `Directory.Packages.props` — the `Avalonia.Controls.ColorPicker` pin, with a comment recording why
  it belongs to the coupled set and why it does not breach SPEC §2.10.
- `docs/roadmap.md`, `docs/plan/FEATURE-4E1F.md` — statuses.

**Modified by the documentation freshness sweep**

- `CLAUDE.md` — the "the slnx holds **all eight** end-state projects" claim became false the moment
  this phase appended two entries. It now says the eight were the 1.0.0 end state and the file holds
  ten, and repeats that `dotnet pack` still applies to the same three packable projects.
- `docs/SPEC.md` §3.4 — the same correction, plus a `FEATURE-4E1F` row in the incremental-growth
  table. The **new §18** and the §1 repository tree are PHASE06's planned work and were left alone;
  only the statements this phase falsified were touched.

Untouched, as the plan requires: everything under `src/`, every `<Version>` and
`<PackageReleaseNotes>`, `RELEASENOTES.md`, `THIRD-PARTY-NOTICES.md`, the root README, the gallery
sample, and every generated Phosphor asset.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| How does the test project reach the tool's types? | A plain `ProjectReference` to public, XML-documented types | `InternalsVisibleTo` would buy nothing: the tool packs nothing, so there is no public surface to keep small, and `GenerateDocumentationFile` is off on a non-packable project, so the docs cost no build gate |
| What does a fresh studio start from? | `MainWindowViewModel.DefaultDesign` — `PencilSimple`/`Fill`, white glyph, `#3B72F0` plate, 22 % radius, 60 % glyph | It is the existing `Enigma.MarkdownEditor` look, so the reference icon is reproducible without touching a control |
| `PlateFill.Solid` and the second colour | Seeds `SecondaryColor` with the same colour rather than leaving it `default` | `default(Color)` is transparent black; toggling Solid → Gradient in the UI would otherwise start from an invisible stop |
| Gradient angle out of range | Wrapped into [0, 360), non-finite rejected | An angle slider that stops dead at its ends is worse than one that wraps; NaN must still be a hard error, so the guard is a negated finiteness test |
| Ratio validation idiom | Negated range comparisons (`!(x >= a && x <= b)`) | The idiom `IconViewBox` and `IconLayer` already use — it rejects NaN, which a `< a \|\| > b` test silently admits |

## Build/test evidence

| Check | Result |
|---|---|
| `dotnet build Enigma.Icons.slnx` | Build succeeded · **0 Warning(s)**, 0 Error(s) — read from the per-project output, not the exit code (CLAUDE.md's `AVLN*` caveat) |
| `dotnet test --solution Enigma.Icons.slnx` | **509 total · 504 passed · 0 failed · 5 skipped** (up from 479/474 — 30 new tests) |
| Pre-existing skips | The same 5 `SvgIconSetPathSafetyTests` symlink cases; this Windows session cannot create symlinks. No new skip appeared. |
| App launch | The built `Enigma.Icons.AppIconStudio.exe` was started, stayed alive, and reported `MainWindowTitle` = `Enigma.Icons — App Icon Studio`, then was stopped. |
| `Enigma.Icons.slnx` | 10 `<Project>` entries |

## Deviations & follow-ups

**1. A project-local `.editorconfig` was added, which the plan's step list did not mention.** The
first build produced one warning — `AVLN3001`, *"XAML resource … won't be reachable via runtime
loader, as no public constructor was found"* — because `MainWindow` takes its ViewModel by
constructor injection and so has no parameterless constructor. The gallery hits exactly the same
case and already solves it with a project-scoped `.editorconfig` setting
`avalonia_xaml_diagnostic.AVLN3001.severity = none`; this phase copies that solution rather than
inventing a second one, or adding a designer-only constructor that news up a ViewModel. Suppressed
per-project, deliberately, so the diagnostic keeps firing for any window that really is loaded by
URI.

**2. `Avalonia.Skia` needs no pin of its own.** `TestAppBuilder` calls `.UseSkia()`, and the
assembly arrives transitively through `Avalonia.Desktop` on the tool's `ProjectReference`. The build
confirms it compiles; no second package pin was needed, so the plan's "one new pin" constraint
holds exactly.

**3. The `MainWindowViewModel` placeholder has no settable observable property yet.** `StatusText` is
get-only for now; PHASE04 turns it into a `field`-backed `SetProperty` property when there is
something to report. The class already derives from `ObservableObject` so that change costs nothing.

**4. Line endings (recommendation only).** Every file this phase created is LF with a final newline,
and `.gitattributes` (`* text=auto eol=lf`) already governs the repository. No action taken or
needed.

**5. Follow-up, not a defect.** The placeholder window uses `ei:Icon` for its header glyph, which
means PHASE01 already exercises the library's control the way a consumer would — worth keeping when
the real layout replaces it in PHASE04.
