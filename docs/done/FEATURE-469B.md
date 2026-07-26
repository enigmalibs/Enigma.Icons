# FEATURE-469B — Icon gallery sample app · DONE

**Branch:** `feature/feature-469b-gallery-sample` · Single-phase · Plan: `docs/plan/FEATURE-469B.md`

> **How the manual criteria were split.** This session has no input-automation tool (`xdotool`,
> `ydotool`, `wtype` all absent), so typing, clicking, dragging and tabbing could not be driven from
> here. Everything reachable by launching the app, reading the rendered frame and measuring at runtime
> was verified here and is evidenced below; the six input-driven criteria were walked by the user
> against this exact build and confirmed — see *Interactive pass* at the end. All 24 acceptance
> criteria are met.

## Summary

Built `samples/Enigma.Icons.Avalonia.Gallery` (SPEC §11) — the solution's first and only application,
and the eighth and last project of the SPEC §3.4 slnx end state. It is a **consumer**, not a
component: it gained no API, no `InternalsVisibleTo` and no accommodation anywhere in `src/`. No
`.cs`, `.csproj` or generated file under `src/`, `tests/` or `tools/` was touched — the only change
below the sample is one **prose** line in `src/Enigma.Icons.Avalonia/README.md`, from the
documentation sweep (deviation 6), which alters no code and no API.

What shipped:

- **Host-based startup.** `Program.Main` is synchronous and `[STAThread]`, running
  `StartWithClassicDesktopLifetime`; `App.OnFrameworkInitializationCompleted` builds a
  `HostApplicationBuilder`, registers the clipboard writer, the ViewModel and the window, `Start()`s
  the host (never `RunAsync`), and hands Avalonia a **resolved** `MainWindow`. There is no
  `new MainWindow(...)` anywhere.
- **A virtualized grid of 1,512 icons.** Avalonia 12 ships exactly one general-purpose virtualizing
  panel — `VirtualizingStackPanel`; `ItemsRepeater` is absent from `Avalonia.Controls` 12.0.4 and has
  no central pin — so the grid is a vertical virtualized list of `IconRow`s, each row a
  non-virtualizing `UniformGrid Columns="8"`. Measured: **67–78 realized `Icon` controls**, never
  1,512.
- **Shared presentation state binds *up*.** `Weight`, `Size` and `Foreground` are bound from each cell
  to the window's ViewModel through `$parent[Window].((vm:MainWindowViewModel)DataContext)`, so
  changing one repaints the already-realized `Icon` instances via SPEC §10.2's
  `AffectsRender`/`AffectsMeasure` registrations instead of rebuilding the list. Baking them into
  `IconEntry` would have proven nothing.
- **The three proofs FEATURE-3ADD delegated here.** A single
  `xmlns:ei="https://github.com/josueclement/Enigma.Icons"` resolves `ei:Icon`,
  `{ei:IconGeometry}` **and** `{ei:IconImage}` — the window compiling is that proof. The two
  no-`Foreground` icons in the header render in their scope's brush and repaint on a theme switch.
  All four `Stretch` modes were checked by eye on a deliberately non-square target.
- **Plain Avalonia + Fluent + Inter, and nothing else.** No `Carbon.Avalonia.Desktop` (SPEC §11):
  a reference consumer must let a reader see exactly which elements come from
  `Enigma.Icons.Avalonia`. No `StyleInclude` for the package either — it ships no XAML, and
  `App.axaml` demonstrating that is part of the point.

## Files touched

### Created — `samples/Enigma.Icons.Avalonia.Gallery/`

| File | What |
|---|---|
| `Enigma.Icons.Avalonia.Gallery.csproj` | `net10.0`, `WinExe`, `ImplicitUsings=disable`, `IsPackable=false`, `ApplicationManifest`; six `PackageReference`s plus Debug-only `AvaloniaUI.DiagnosticsSupport`, **none** with `Version=`; one `ProjectReference` to `Enigma.Icons.Avalonia`. Also `AvaloniaUseCompiledBindingsByDefault` — see deviation 2. |
| `app.manifest` | Standard Avalonia manifest: `assemblyIdentity` + PerMonitorV2 DPI awareness. Windows-only effect, inert on Linux. |
| `.editorconfig` | One rule: `avalonia_xaml_diagnostic.AVLN3001.severity = none` — see deviation 4. |
| `Program.cs` | `[STAThread]` synchronous `Main`; `BuildAvaloniaApp()` public for the previewer, with `.WithDeveloperTools()` under `#if DEBUG`. |
| `App.axaml` | `RequestedThemeVariant="Default"` + `FluentTheme`. No `StyleInclude`. |
| `App.axaml.cs` | Host build/start, three singleton registrations, resolved `MainWindow`, `desktop.Exit` → host stopped and disposed. |
| `MainWindow.axaml` | The five-row window: controls, weight strip + inheritance proof, API-coverage row, virtualized grid + empty state, status line. Plus the `Button.cell` and caption styles. |
| `MainWindow.axaml.cs` | ViewModel by constructor injection, `DataContext` assigned after `InitializeComponent()`, `Opened` → `SearchBox.Focus()` (the constructor is too early — no visual root yet). |
| `MainWindowViewModel.cs` | The catalog, the 200 ms debounce, `ApplyFilter`, the status line, the snippet builder and the copy command. Explicit MVVM — see deviation 1. |
| `IconEntry.cs` | `record IconEntry(PhosphorIcon Kind, string Name)` — deliberately carries no presentation state. |
| `IconRow.cs` | `record IconRow(IReadOnlyList<IconEntry> Cells)` — the virtualized unit. |
| `ColorPreset.cs` | `record ColorPreset(string Name, IBrush Brush)`; `Brush` non-null because a null `Foreground` paints nothing (SPEC §10.2). |
| `IClipboardTextWriter.cs` | `Task<bool> TryWriteAsync(string)`. Never throws. |
| `AvaloniaClipboardTextWriter.cs` | No constructor dependencies; resolves the clipboard lazily per call, which is what avoids the cycle `MainWindow → MainWindowViewModel → writer → MainWindow`. |

### Created — other

| File | What |
|---|---|
| `docs/img/gallery.png` | 1222×810, window only. Weight strip + a populated **filtered** grid (`arrow` → "Showing 109 of 1,512 icons · Regular"). For FEATURE-718F. See deviation 9. |

### Modified

| File | Change |
|---|---|
| `Enigma.Icons.slnx` | The one `/samples/` entry. **The SPEC §3.4 end state is now reached: eight `<Project>` entries** (3 src + 3 tests + 1 samples + 1 tools). |
| `docs/roadmap.md` | FEATURE-469B → `DONE`. |
| `docs/plan/FEATURE-469B.md` | Status header → `DONE`; all 24 acceptance criteria ticked, each annotated with how it was verified (screenshot, measurement, or the user's interactive pass); OPEN QUESTION 2 recorded as resolved. |

### Modified — documentation freshness sweep (all three candidates accepted by the user)

| File | Change |
|---|---|
| `README.md`, `src/Enigma.Icons.Avalonia/README.md`, `docs/SPEC.md` §10.3 | `SystemAccentColorBrush` → `SystemControlForegroundAccentBrush` in the XAML usage snippet, plus a `>` note in the SPEC explaining why the old key was wrong and why it mattered (deviation 6). |
| `CLAUDE.md` | The incremental-growth note now states **all eight** end-state projects exist, and gains the `dotnet run --project samples/…` command with its desktop-session caveat. Hard rule 1 gains the caveat that `TreatWarningsAsErrors` does **not** promote Avalonia's `AVLN*` task-logged warnings — a false-confidence trap this item walked into twice. |
| `docs/SPEC.md` §11 | The MVVM clause now describes the explicit house style with a note on why it changed (deviation 1); a new bullet records that Avalonia 12.0.4 defaults `AvaloniaUseCompiledBindingsByDefault` to `false` and that an app must set it (deviation 2). |

No root **config** file was edited — `Directory.Packages.props` in particular needed no change, as the
plan predicted. No code, csproj or generated file outside `samples/` was touched.

## Deviations & follow-ups

1. **Explicit MVVM, not the SPEC §11 source generators — the plan's OPEN QUESTION 2, decided by the
   user.** SPEC §11 mandates `[ObservableProperty]`/`[RelayCommand]`; the house
   `communitytoolkit-mvvm` skill forbids every MVVM generator attribute. Both cannot hold, and the
   user chose the skill. `MainWindowViewModel` therefore uses `field`-keyword properties with
   `SetProperty`, a get-only `AsyncRelayCommand<IconEntry>` initialized in the constructor, and is
   **not** `partial`. The plan's step-5 member table maps one-to-one; `OnSearchTextChanged` became the
   setter body. **SPEC §11's `CommunityToolkit.Mvvm for [ObservableProperty]/[RelayCommand]` clause is
   now stale** — a candidate for the documentation sweep.

2. **The plan's "compiled bindings are on by default in Avalonia 12" is wrong for 12.0.4 —
   `AvaloniaUseCompiledBindingsByDefault=true` had to be set.**
   `~/.nuget/packages/avalonia/12.0.4/build/AvaloniaBuildTasks.targets` line 5 defaults that property
   to **`false`** and passes it to the XAML compiler as `DefaultCompileBindings`, so without it every
   `x:DataType` in this window would have been decorative and every binding a silent reflection
   binding. Proven, not assumed: with the property set, renaming one bound path to a non-existent
   member fails the build with `AVLN2000: Unable to resolve property or method of name
   'NoSuchPropertyXyz' on type 'MainWindowViewModel'`. The plan's instruction "do not set
   `AvaloniaUseCompiledBindingsByDefault`" is a **stale assumption inherited from the `avalonia`
   skill**, which states the same thing.

3. **`TextBox.Watermark` is obsolete in Avalonia 12 — it is `PlaceholderText`.** It emitted
   `AVLN5001` and therefore broke the zero-warning rule. Not in the `avalonia` skill's
   "v11-isms to avoid" table; worth adding there.

4. **`AVLN3001` is suppressed by a project-local `.editorconfig`.** The warning is *"XAML resource
   won't be reachable via runtime loader, as no public constructor was found"* — the direct
   consequence of `MainWindow` taking its ViewModel by constructor injection, which SPEC §11 requires
   and the plan explicitly protects ("do not add a second constructor that `new`s a ViewModel just to
   please the designer"). Nothing loads this window by URI: the XAML is compiled and `App` resolves
   the window from the container. Suppressed with the vendor's own mechanism
   (`avalonia_xaml_diagnostic.AVLN3001.severity = none`, whose key format was read out of
   `Avalonia.Build.Tasks.dll`) and scoped to this project so it keeps firing for any future window
   that really is runtime-loaded. Zero-warning builds (SPEC §2.1) left no other option.

5. **`AvaloniaUI.DiagnosticsSupport` is Debug-gated by asset conditions, not by a `Condition` on the
   `PackageReference`.** The plan specified `Condition="'$(Configuration)' == 'Debug'"`; the
   `avalonia` skill's form — `<IncludeAssets Condition="'$(Configuration)' != 'Debug'">None` plus
   `<PrivateAssets …>All` — is used instead. Same outcome (nothing ships in Release, and
   `.WithDeveloperTools()` is inside `#if DEBUG`), but the restore graph stays identical across
   configurations. Mechanical.

6. **📌 Documentation defect found from the consumer seat: `SystemAccentColorBrush` does not exist in
   Avalonia 12.** The key is cited in three places as the recommended way to colour an icon —
   `README.md:54`, `src/Enigma.Icons.Avalonia/README.md:44` and `docs/SPEC.md:1160`
   (`Foreground="{DynamicResource SystemAccentColorBrush}"`). `Avalonia.Themes.Fluent` 12.0.4 defines
   `SystemAccentColor` (a **Color**) and brushes such as `SystemControlForegroundAccentBrush`, but
   **no** `SystemAccentColorBrush` — verified by scanning the compiled theme assembly's string heap
   (0 occurrences vs 1 for `SystemControlForegroundAccentBrush`). A consumer copying the documented
   snippet gets an unresolved `DynamicResource` → a null `Foreground` → **an icon that paints
   nothing** (SPEC §10.2), with no error. This is the highest-value finding of the item and it lands
   squarely in FEATURE-718F's path. The gallery uses the working key. **Fixed in all three documents
   in this dev's documentation sweep**, with the reasoning recorded inline in SPEC §10.3.

7. **`Stretch="None"` draws at the glyph's natural 256 DIP and is not clipped by the control.**
   `Icon.Render` clips only `UniformToFill` (SPEC §10.2), so `None` on a Phosphor glyph inside a 96×32
   container painted a 256×256 acorn straight across the rest of the header — observed, then fixed in
   the sample by putting `ClipToBounds="True"` on the demo `Border`. The control is behaving exactly
   as specified (scale 1.0, `Size` irrelevant); this is a **consumer-facing sharp edge worth one
   sentence in the `Stretch` documentation**, not a bug to patch in `src/`.

8. **Cosmetic departures from the plan's literal figures**, all in service of the acceptance criteria:
   - Window `1100×750` → **`1220×780`**, so the Row 0 control strip fits on one line instead of
     wrapping (visible in the first capture).
   - Weight-strip icons `34` → **`40`** px, so the duotone two-tone reads at a glance.
   - The first colour preset (and therefore the default) is a **mid** tone `#7A8899` rather than the
     dark `#333D4B` first tried: the app follows the OS theme variant, and a dark default was nearly
     invisible on the dark variant.
   - The "text scope" half of the inheritance proof sets `TextElement.Foreground` on a `StackPanel`
     to `{DynamicResource SystemControlForegroundAccentBrush}` rather than nesting the icon inside a
     `TextBlock`. **This is stronger, not weaker:** `Icon.Foreground`'s effective default already *is*
     the ambient `TextElement.Foreground` (FEATURE-3ADD deviation 7), so an ambient-only demo cannot
     distinguish "inherited from the scope" from "the property's default value". An explicit,
     non-default, theme-aware scope brush can — and the `TextBlock` beside the icon must always match
     it.

9. **The committed screenshot was captured with a temporary `SearchText = "arrow"` seed**, because the
   plan asks for a *filtered* grid and there is no input-automation tool in this session
   (`xdotool`/`ydotool`/`wtype` all absent). The seed was reverted immediately afterwards; the shipped
   default is `string.Empty`. Say the word and it can be recaptured live in any state you prefer.

10. **The `Kind` → XAML attribute name in the copied snippet uses `entry.Kind.ToString()`.** One call
    per user click; SPEC §2.11's no-`Enum.ToString` rule guards the library's per-glyph lookup, which
    this sample never goes near. The two places that *would* have been hot both avoid it: the 1,512-entry
    catalog is built by casting the index (`(PhosphorIcon)i`, valid because SPEC §8.4 guarantees
    `PhosphorIconNames.All` is in enum order), and the status line indexes a `WeightNames` array by the
    enum's numeric value.

11. **Line endings:** nothing to report. Every text file created or modified is LF with a final
    newline, verified byte-wise; no CRLF churn anywhere in the diff.

## Build/test evidence

### Package pins — the SPEC §3.3 restore obligation FEATURE-21C4 delegated here

This item is the first consumer of the `CommunityToolkit.Mvvm` and `Microsoft.Extensions.Hosting`
pins. §3.3's rule is "verify at restore time, do not assume". **Both resolved exactly at their pinned
versions on the gallery's first restore — no fallback, no bump, `Directory.Packages.props`
untouched.** Read from `samples/…/obj/project.assets.json` (`net10.0`):

| Package | Pinned (SPEC §3.3) | Resolved |
|---|---|---|
| `CommunityToolkit.Mvvm` | 8.4.2 | **8.4.2** |
| `Microsoft.Extensions.Hosting` | 10.0.8 | **10.0.8** |
| `Avalonia` | 12.0.4 | 12.0.4 |
| `Avalonia.Desktop` | 12.0.4 | 12.0.4 |
| `Avalonia.Themes.Fluent` | 12.0.4 | 12.0.4 |
| `Avalonia.Fonts.Inter` | 12.0.4 | 12.0.4 |
| `AvaloniaUI.DiagnosticsSupport` | 2.2.1 | 2.2.1 |

```
Restored /home/jo/Dev/Enigma.Icons/samples/Enigma.Icons.Avalonia.Gallery/…csproj (in 553 ms).
7 of 8 projects are up-to-date for restore.
```

### Build — zero warnings, both configurations

```
$ dotnet build Enigma.Icons.slnx                 $ dotnet build Enigma.Icons.slnx -c Release
    0 Warning(s)                                     0 Warning(s)
    0 Error(s)                                       0 Error(s)
```

All **eight** slnx projects restore and build. Two warnings were found and fixed rather than
tolerated (deviations 3 and 4) — `TreatWarningsAsErrors` does not promote Avalonia's `AVLN*`
task-logged warnings to errors, so a zero-warning build here is a deliberate check, not something the
build enforces on its own. **Worth remembering for future Avalonia work in this solution.**

### Tests — whole suite green, unchanged

```
$ dotnet test --solution Enigma.Icons.slnx
Test run summary: Passed!
  total: 434   failed: 0   succeeded: 434   skipped: 0
```

The same 434 tests FEATURE-3ADD left. The gallery adds none, by design: SPEC §12 defines exactly
three test projects and SPEC §17 rejects visual-regression/bitmap-baseline testing.

### Virtualization — measured, not assumed

A temporary Debug-only `DispatcherTimer` counted
`this.GetVisualDescendants().OfType<Icon>()` three times after `Opened`, printing to stdout:

```
[virtualization-probe] realized Icon controls in the visual tree: 67   (window 1100×750)
[virtualization-probe] realized Icon controls in the visual tree: 75   (window 1220×780)
[virtualization-probe] realized Icon controls in the visual tree: 78   (1220×780, light variant)
```

**67–78 out of 1,512.** The number tracks window height exactly as a viewport-bound realization
should: 11 of those are the fixed header icons (6 weight strip + 2 inheritance + 3 API row), leaving
56 / 64 / 67 grid cells — that is 7–8 rows of 8. `ItemsControl` + `VirtualizingStackPanel` inside a
`ScrollViewer` **does** virtualize; the plan's `ListBox` fallback was not needed. The probe has been
removed from `MainWindow.axaml.cs` (verified by grep).

### Visual verification — what was seen

The app launched with `dotnet run --project samples/Enigma.Icons.Avalonia.Gallery` on the KDE/Wayland
session (Avalonia's X11 backend via XWayland, `DISPLAY=:1`) and showed its window. Screenshots were
taken with `spectacle -b -n -a` and read back at 3–4× magnification.

| Criterion | Observed |
|---|---|
| **App launches, window shows** | ✅ Title, 1220×780, all five rows laid out. |
| **All six weights distinct** | ✅ Thin hairline → Light → Regular → Bold visibly heavier → Fill solid → Duotone. Confirmed on **both** theme variants. |
| **Duotone is two-tone** | ✅ The 0.2-opacity backing shape is plainly visible behind the full-opacity outline, and clearly different from `Fill`. Clearest on the light variant, where the backing reads as pale grey inside a dark outline. SPEC §7.4's layered model reaches the screen. |
| **Search box focused on start** | ✅ The focus ring is on the `TextBox` in every capture. |
| **Filter + status line** | ✅ `arrow` → only `arrow-*` icons, and "Showing **109** of 1,512 icons · Regular". |
| **One `xmlns:ei` resolves everything** | ✅ Compile-time (the window builds) **and** visually: `ei:Icon`, `<Path Data="{ei:IconGeometry Acorn}">` and `<Image Source="{ei:IconImage Acorn, Brush=DodgerBlue}">` all render side by side under the single prefix. |
| **Inherited `Foreground`, no binding written** | ✅ Dark variant: the Button-scope star is **white**, matching its button label; the text-scope leaf is **accent blue**, matching its label. Neither sets `Foreground`. |
| **…and it follows a theme switch** | ✅ Route taken: **`RequestedThemeVariant="Light"`, re-run, observe, revert** — the alternative the criterion sanctions, chosen over flipping the OS preference because that would have changed the user's live desktop theme. The Button-scope star repainted **white → dark**, still matching its button label, with no `Foreground` set and no binding written anywhere. The delegated proof is discharged; the OS-preference flip remains available as an optional stronger check. |
| **`Stretch` — all four modes** | ✅ Checked side by side in a temporary four-up on a 96×32 target. `None` → 1:1, 256 DIP, overflows (deviation 7). `Uniform` → fits the height, aspect kept. `UniformToFill` → fills the width, clipped by the control itself. `Fill` → squashed to exactly 96×32. All four distinct and exactly as SPEC §10.2 describes. |
| **`SvgIconSet` cross-check** | ✅ The one-glyph inline-SVG set renders its `circle`, `ellipse` and rounded `rect` correctly — FEATURE-24DD's shape → arc conversion (SPEC §5.1) confirmed by eye, through `IconSet`/`IconName` rather than Phosphor. |
| **README screenshot** | ✅ `docs/img/gallery.png` (see deviation 9). |
| **The exact shipped source renders** | ✅ Re-launched after every temporary aid was removed — probe gone, `SearchText` back to `string.Empty`, `RequestedThemeVariant` back to `Default` — and captured: focused empty search box with its placeholder, "Showing **1,512** of 1,512 icons · Regular", all six weights, both inheritance icons, the single-prefix trio, the clipped `Stretch` box, the `SvgIconSet` glyph, and the full virtualized grid. Clean log, no exceptions. |

> One intermediate capture came back entirely black. That was the desktop lock screen
> (`loginctl … LockedHint=yes`), not an app regression — the whole 1920×1200 framebuffer was black, the
> process was alive, and the log was clean. Re-verified above once the session was unlocked. Recorded
> because a black screenshot is an easy thing to misread as a rendering failure.

### Interactive pass — walked by the user, all six confirmed

The six criteria that need live keyboard/mouse input were walked by the user against the build in this
commit, on the running app, and reported good:

| # | Criterion | Result |
|---|---|---|
| 1 | Typing in the search box stays smooth with no perceptible stall; matching is debounced | ✅ |
| 2 | A non-matching term shows `No icons match '<term>'` and hides the grid; clearing restores 1,512 | ✅ |
| 3 | Changing the weight selector re-renders the already-realized grid icons | ✅ |
| 4 | The size slider and colour preset repaint live, without rebuilding the list or restarting | ✅ |
| 5 | Grid cells are keyboard-reachable (Tab in, move between cells, Enter/Space activates) | ✅ |
| 6 | Clicking an icon copies `<ei:Icon Kind="…" Weight="…" Size="…" />`, **confirmed by pasting into an editor**; a confirmation appears in the status line | ✅ |

Criteria 3 and 4 are the ones that matter most for the library rather than the sample: they are the
only direct confirmation anywhere in the solution that SPEC §10.2's `AffectsRender`/`AffectsMeasure`
registrations actually repaint **live** `Icon` instances. FEATURE-3ADD's deviation 6 records that
`AffectsRender` exposes no publicly observable flag and so could not be unit-tested, and explicitly
defers the proof to this item. That debt is now discharged.

The optional stronger theme check — flipping the OS light/dark preference while the app runs — was not
needed: the `RequestedThemeVariant` route recorded above already discharges that criterion, as the
criterion itself permits.
