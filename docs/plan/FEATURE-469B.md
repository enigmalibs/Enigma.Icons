**Status:** DONE · Single-phase · Suggested build branch `feature/feature-469b-gallery-sample`

# FEATURE-469B — Icon gallery sample app

## Objective

Build `samples/Enigma.Icons.Avalonia.Gallery`, the desktop Avalonia app specified in SPEC §11: a
searchable, virtualized browser over all 1,512 Phosphor icons in all six weights, with live weight /
size / colour switching and a click-to-copy XAML snippet. It is the **visual** verification the
headless unit tests structurally cannot do (does duotone actually render two tones? does a bound
`Foreground` actually repaint?), it exercises the exact public API a consumer would, and it produces
the screenshot material FEATURE-718F needs for the READMEs.

## Context

FEATURE-3ADD has just shipped `Enigma.Icons.Avalonia` — the `Icon` control, the two markup
extensions, and `IconGlyphExtensions` (SPEC §10). Everything below it (`Enigma.Icons`,
`Enigma.Icons.Phosphor`, the six `.dat` resources, `PhosphorIcon`, `PhosphorIconNames`) is in place
and tested. This item adds the first and only **application** in the solution and the last project in
the tree, reaching the SPEC §3.4 slnx end state.

Two consequences shape the whole item:

1. **It is a consumer, not a component.** It must not gain any API, any `InternalsVisibleTo`, or any
   accommodation inside the three packages. If something is awkward here it is awkward for every
   consumer — that is a finding for the completion doc, not a reason to change the library from
   inside the sample.
2. **Nothing packable may reference it** (SPEC §1). `IsPackable=false`, and no other project gains a
   `ProjectReference` to it.

The retired `PhosphorIconsAvalonia` repo had a `TesterApp` (a static grid of five `PathIcon`s driven
by markup extensions). It is context only, not a template: it had no host, no MVVM, no search, and no
virtualization, and its markup-extension-only approach is precisely what SPEC §10.2 replaced.

## Scope

### In scope
- The `samples/Enigma.Icons.Avalonia.Gallery/` project in full: csproj, `Program.cs`, `App.axaml(.cs)`,
  `app.manifest`, `MainWindow.axaml(.cs)`, `MainWindowViewModel.cs`, the small item/service types.
- Appending the one `<Project>` entry to `Enigma.Icons.slnx` under `/samples/` (SPEC §3.4).
- Every SPEC §11 feature: debounced case-insensitive substring search, six-weight selector, size
  slider, foreground colour presets, virtualized icon grid, copy-XAML-to-clipboard, filtered-count
  status line, empty-result state, search box focused on start. English only.
- A small API-coverage demo row: both markup extensions under the single `xmlns:ei`, the `Stretch`
  visual check, and one `SvgIconSet.FromSvgSources` glyph (Design step 6, Row 2).
- The manual visual verification pass, and capturing the README screenshot `docs/img/gallery.png`
  (SPEC §1, §13) for FEATURE-718F.

### Out of scope (deferred, or owned elsewhere)
- **A test project for the gallery.** SPEC §12 defines exactly three test projects and SPEC §17
  explicitly rejects visual-regression / bitmap-baseline testing. See "Acceptance criteria" for how
  DoD 1–2 are met without one.
- **Any change to `src/`** — the packages are frozen by 3ADD; API polish found here is reported, not
  patched (see Context).
- **`Carbon.Avalonia.Desktop`** and its skill — Design step 7.
- **Icon metadata / categories / tag search** — SPEC §17; substring search over names is the whole
  search feature.
- **Responsive column count, an in-app light/dark theme toggle, favourites, per-icon detail pane,
  localization** — not in SPEC §11. (The theme-switch half of the inherited-`Foreground` proof is
  verified through the OS theme variant or a temporary `RequestedThemeVariant` edit instead — step 8.)
- **Embedding the screenshot in any README** — FEATURE-718F owns all README bodies (SPEC §13).
- **An application `.ico`** — the `avalonia` skill's app-icon guidance is for user-facing apps; SPEC
  §1/§11 call for no art asset and the repo tracks none. Deliberate skill deviation; no `Assets/`
  folder, no `<ApplicationIcon>`, no `<AvaloniaResource>` items.
- **Bumping the Avalonia coupled set** away from the version pinned in `Directory.Packages.props`
  (SPEC §3.3). A bump is solution-wide by SPEC §3.3's coupled-set rule and is not this item's call.

## Design

All paths are relative to `samples/Enigma.Icons.Avalonia.Gallery/` unless stated. LF + final newline,
zero-warning build, `ImplicitUsings` disabled with explicit per-file usings, no `Version=` on any
`PackageReference` — SPEC §2.

### 1. `Enigma.Icons.Avalonia.Gallery.csproj`

```xml
<PropertyGroup>
  <OutputType>WinExe</OutputType>          <!-- suppresses the console window on Windows; inert on Linux -->
  <TargetFramework>net10.0</TargetFramework>
  <ImplicitUsings>disable</ImplicitUsings>
  <IsPackable>false</IsPackable>
  <ApplicationManifest>app.manifest</ApplicationManifest>
</PropertyGroup>
```

- `LangVersion`, `Nullable`, `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild` come from
  `Directory.Build.props` (SPEC §3.2) — do not repeat them.
- Do **not** set `GenerateDocumentationFile`: SPEC §2.8's XML-doc rule exists to satisfy CS1591 on
  **packed** API surface, and the gallery packs nothing. `EnforceCodeStyleInBuild` still applies, so
  the house `.editorconfig` warning-severity rules are build **errors** here: file-scoped namespaces,
  `using` directives **outside** the namespace, predefined type keywords (`string`, not `String`),
  and explicit accessibility modifiers on every member.
- Do **not** set `AvaloniaUseCompiledBindingsByDefault` — compiled bindings are on by default in
  Avalonia 12; supply `x:DataType` instead (steps 6/7).
- Do **not** set `IsTrimmable`/`IsAotCompatible` — SPEC §10.4 scopes those to the three packages.
- `PackageReference` (no `Version=`, all supplied by `Directory.Packages.props` §3.3):
  `Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent`, `Avalonia.Fonts.Inter`,
  `CommunityToolkit.Mvvm`, `Microsoft.Extensions.Hosting`, plus `AvaloniaUI.DiagnosticsSupport`
  under a Debug-only condition (`Condition="'$(Configuration)' == 'Debug'"`), per SPEC §11. That
  package is **already centrally pinned** (SPEC §3.3) — **no `Directory.Packages.props` edit is
  needed or authorized.** There is **no `Avalonia.Diagnostics` package for Avalonia 12**; it must
  not be used (SPEC §3.3, §11).
- One `ProjectReference` to `..\..\src\Enigma.Icons.Avalonia\Enigma.Icons.Avalonia.csproj`, which
  brings `Enigma.Icons.Phosphor` and `Enigma.Icons` transitively (SPEC §10). The multi-targeted
  library resolves to its `net10.0` TFM. No project references this project.
- `.axaml` files are auto-included by the Avalonia build targets; add no `<AvaloniaXaml>` /
  `<AvaloniaResource>` item groups.
- Do **not** add `Tmds.DBus.Protocol` (the retired `TesterApp` carried it): it arrives transitively
  via `Avalonia.FreeDesktop` and has no central pin, so a direct reference would fail CPM (NU1010).
  If restore genuinely demands one, that is a SPEC §3.3 change plus a csproj comment stating why —
  raise it, do not add it silently.

### 2. `Enigma.Icons.slnx` — append the sample

Append exactly one entry, inside the existing `<Folder Name="/samples/">`:

```xml
<Project Path="samples/Enigma.Icons.Avalonia.Gallery/Enigma.Icons.Avalonia.Gallery.csproj" />
```

This **reaches the SPEC §3.4 end state**: eight `<Project>` entries (3 `src` + 3 `tests` +
1 `samples` + 1 `tools`). No item in the 1.0.0 line (718F, 74DC) adds any; the deferred
FEATURE-6FA1 would extend the table post-1.0 (SPEC §3.4). Append it in the same step as the
csproj so the incremental-growth contract holds — the slnx never names a project that does not exist.

### 3. Bootstrapping — `Program.cs`, `App.axaml`, `App.axaml.cs`, `app.manifest`

Per SPEC §11 and the `avalonia` + `dotnet-solution-setup` skills. The load-bearing rule: **Avalonia's
lifetime runs the app, the host only supplies services.** `await host.RunAsync()` alone never shows a
window.

- `Program.cs` — `[STAThread] public static void Main(string[] args)` (synchronous) calling
  `BuildAvaloniaApp().StartWithClassicDesktopLifetime(args)`; `BuildAvaloniaApp()` returns
  `AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace()` plus, under
  `#if DEBUG`, `.WithDeveloperTools()` from `AvaloniaUI.DiagnosticsSupport` — not
  `this.AttachDevTools()`, and never `Avalonia.Diagnostics` (SPEC §3.3, §11). Keep
  `BuildAvaloniaApp()` public — the XAML previewer calls it.
- `App.axaml` — `<Application x:Class="…Gallery.App" RequestedThemeVariant="Default">` with
  `<Application.Styles><FluentTheme /></Application.Styles>`. **No `<StyleInclude>` for
  `Enigma.Icons.Avalonia`** — the package ships no XAML and needs none (SPEC §10.2); the gallery
  demonstrating that is part of the point.
- `App.axaml.cs` — `Initialize()` loads the XAML; `OnFrameworkInitializationCompleted()`:
  1. `HostApplicationBuilder builder = Host.CreateApplicationBuilder();`
  2. register `IClipboardTextWriter` → `AvaloniaClipboardTextWriter` (singleton),
     `MainWindowViewModel` (singleton), `MainWindow` (singleton);
  3. `_host = builder.Build(); _host.Start();`
  4. when `ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop`, set
     `desktop.MainWindow = _host.Services.GetRequiredService<MainWindow>()` — **resolved, never
     `new`ed** (SPEC §11); hook `desktop.Exit` to stop the host;
  5. `base.OnFrameworkInitializationCompleted()`.
  Keep the host in a private field and dispose/stop it on exit. No service instantiation in any
  window's code-behind.
- `MainWindow` therefore takes `MainWindowViewModel` by constructor injection and assigns
  `DataContext` after `InitializeComponent()`. Consequence to accept: the XAML previewer cannot
  instantiate `MainWindow` without a parameterless constructor — acceptable for a sample; do not add
  a second constructor that `new`s a ViewModel just to please the designer.
- `app.manifest` — the standard Avalonia template manifest (`assemblyIdentity` + PerMonitorV2 DPI
  awareness). Windows-only effect, inert on Linux; LF + final newline like every other text file.
- **Namespace trap.** The app's namespace is `Enigma.Icons.Avalonia.Gallery`, so inside it the simple
  name `Avalonia` binds to `Enigma.Icons.Avalonia`, not to the framework root. Never write
  `Avalonia.`-qualified type names in the namespace body: rely on `using` directives (which sit
  *outside* the namespace, as `.editorconfig` requires) and use `global::Avalonia.…` if a
  qualification is unavoidable.

### 4. Item models and the clipboard service

- `IconEntry.cs` — `public sealed record IconEntry(PhosphorIcon Kind, string Name);`
- `IconRow.cs` — `public sealed record IconRow(IReadOnlyList<IconEntry> Cells);` one grid row
  (step 6 explains why rows, not cells, are the virtualized unit).
- `ColorPreset.cs` — `public sealed record ColorPreset(string Name, IBrush Brush);` `Brush` is
  **non-null**: SPEC §10.2 says a null `Foreground` paints nothing, so a null preset would look like
  a rendering bug. Inheritance is demonstrated separately (step 6).
- `IClipboardTextWriter.cs` / `AvaloniaClipboardTextWriter.cs` —
  `public interface IClipboardTextWriter { Task<bool> TryWriteAsync(string text); }`. The
  implementation takes **no constructor dependencies** and resolves the clipboard lazily per call:
  `Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime` →
  `.MainWindow?.Clipboard` → `await clipboard.SetTextAsync(text)`, returning `false` when either is
  null. Injecting `MainWindow` instead would create the cycle
  `MainWindow → MainWindowViewModel → writer → MainWindow`. Note for the builder:
  `SetTextAsync` is an **extension** on `IClipboard` in Avalonia 12 —
  `using Avalonia.Input.Platform;` is required, and `IClipboard` itself only exposes
  `SetDataAsync`/`TryGetDataAsync`.
- The one-time catalog lives in the ViewModel: a `static readonly IconEntry[]` of 1,512 entries built
  as `new IconEntry((PhosphorIcon)i, PhosphorIconNames.All[i])`, valid because SPEC §8.4 guarantees
  `All` is in enum order (asserted by the §12.2 tests). `PhosphorIconNames.TryParse` is the
  equivalent defensive form if the builder prefers it; either way no `Enum.Parse`/`ToString`
  (SPEC §2.11).

### 5. `MainWindowViewModel.cs`

`sealed partial class MainWindowViewModel : ObservableObject`, CommunityToolkit.Mvvm attributes per
SPEC §11 (**see OPEN QUESTION 2** — the house skill forbids the generators SPEC mandates). Surface,
with the generator-produced names the XAML binds to:

| Member | Type / shape | Notes |
|---|---|---|
| `SearchText` | `[ObservableProperty] string` | `OnSearchTextChanged` partial hook restarts the debounce timer |
| `SelectedWeight` | `[ObservableProperty] PhosphorWeight` | default `Regular` (SPEC §9) |
| `IconSize` | `[ObservableProperty] double` | default 32 |
| `SelectedColor` | `[ObservableProperty] ColorPreset` | default = first preset |
| `SelectedStretch` | `[ObservableProperty] Stretch` | default `Uniform`; drives the non-square `Stretch` demo (step 6, Row 2) |
| `Rows` | `[ObservableProperty] IReadOnlyList<IconRow>` | replaced wholesale, never mutated |
| `StatusText` / `EmptyMessage` / `HasResults` | `[ObservableProperty]` | recomputed by the filter |
| `Weights` | `IReadOnlyList<PhosphorWeight>` get-only | an explicit six-element array, not `Enum.GetValues` |
| `ColorPresets` | `IReadOnlyList<ColorPreset>` get-only | ~6 named brushes, e.g. theme-ish neutral, black, white, accent-blue, red, green |
| `Stretches` | `IReadOnlyList<Stretch>` get-only | an explicit four-element array — `None`, `Uniform`, `UniformToFill`, `Fill` |
| `ShapesIconSet` | `IIconSet` get-only | one-glyph `SvgIconSet.FromSvgSources` set for the step 6 Row 2 cross-check |
| `CopyXamlCommand` | `[RelayCommand] Task CopyXamlAsync(IconEntry? entry)` | `AsyncRelayCommand<IconEntry>` |

**Debounce.** A single `DispatcherTimer` (interval ≈200 ms, one-shot: `Stop()` inside `Tick` before
applying). Each `SearchText` change calls `Stop(); Start();`. The debounce is not about I/O — the
filter over 1,512 short strings is microseconds — it is about not rebuilding the visual tree on every
keystroke.

**`ApplyFilter()` algorithm.**
1. `term = SearchText.Trim()`; if empty, matches = the full catalog, else
   `entry.Name.Contains(term, StringComparison.OrdinalIgnoreCase)` (SPEC §11: case-insensitive
   substring, no fuzzy matching, no regex).
2. Chunk matches into rows of `ColumnsPerRow` (a `private const int` = 8; the last row is partial)
   and assign `Rows` **once**. Do not use an `ObservableCollection` cleared and refilled per item —
   1,512 collection notifications would defeat the virtualization this item exists to demonstrate.
3. `HasResults = matches.Count > 0`; `StatusText` = e.g. `"Showing 37 of 1,512 icons · Bold"`;
   `EmptyMessage` = `$"No icons match '{term}'"` (SPEC §11's exact wording pattern), formatted with
   invariant culture.

**`CopyXamlAsync`.** Builds the snippet from the entry plus the current selection, in SPEC §10.3's
usage form — `<ei:Icon Kind="AddressBook" Weight="Duotone" Size="32" />` — always emitting `Kind`,
`Weight`, and `Size`, never `Foreground` (the consumer's brush is theirs, and SPEC §10.2 makes it
inherit). Invariant culture; `Size` rendered without a trailing `.0`. Then `TryWriteAsync`, and on
success surface a short-lived confirmation (`StatusText` swapped for `"Copied <snippet>"`, restored
by a second one-shot `DispatcherTimer` after ~2 s). A `false` result must not throw.

### 6. `MainWindow.axaml` + `MainWindow.axaml.cs` — the virtualized grid

`<Window x:DataType="vm:MainWindowViewModel">`, **one** icons namespace declaration —
`xmlns:ei="https://github.com/josueclement/Enigma.Icons"` (SPEC §10.3: the two
`XmlnsDefinition` attributes make that single prefix reach `Enigma.Icons.Avalonia` **and**
`Enigma.Icons.Avalonia.Markup`; the per-namespace `using:` forms are the documented fallback) —
plus `xmlns:vm="using:Enigma.Icons.Avalonia.Gallery"`, `Width="1100" Height="750"`,
`Title="Enigma.Icons — Phosphor Gallery"`. Root `Grid RowDefinitions="Auto,Auto,Auto,*,Auto"`:

- **Row 0 — controls.** `TextBox x:Name="SearchBox"` bound to `SearchText`
  (`Watermark="Search 1,512 icons…"`); a horizontal `ListBox` over `Weights` with
  `SelectedItem="{Binding SelectedWeight}"` so all six weights are visible and one click apart; a
  `Slider Minimum="12" Maximum="96"` bound to `IconSize`; a `ComboBox` over `ColorPresets`
  (`DisplayMemberBinding` → `Name`) bound to `SelectedColor`.
- **Row 1 — weight strip + inheritance demo.** One fixed icon rendered six times, one per weight,
  each labelled. This is a small addition beyond SPEC §11's feature list, justified by the acceptance
  criteria: it is what makes "all six weights correct, duotone visibly two-tone" a one-glance check
  and it is the natural README screenshot. Beside it, one `ei:Icon` with **no `Foreground` set** inside
  a `Button` and another inside a `TextBlock` scope — the visual proof of SPEC §10.2's
  `TextElement.Foreground` `AddOwner` inheritance and theme following, which cannot be shown by
  binding a brush. FEATURE-3ADD delegates this proof here, so it carries its own acceptance criterion
  below and its own entry in the step 8 manual walk — do not treat it as decoration.
- **Row 2 — API-coverage demo row.** One small strip whose only job is to prove, at compile time and
  by eye, the three things FEATURE-3ADD delegates here. Keep it to a single row — it is not a feature:
  - **Both markup extensions under the single prefix.** Side by side: an `<ei:Icon Kind="Acorn" />`, a
    `<Path Data="{ei:IconGeometry Acorn}" Fill="…" Stretch="Uniform" Width="24" Height="24" />`, and an
    `<Image Source="{ei:IconImage Acorn}" Width="24" Height="24" />` — all resolved through the **one**
    `xmlns:ei` declared above. This is the compile-time proof that SPEC §10.3's `XmlnsDefinition`
    pair reaches the control *and* both markup extensions; the gallery's compiled XAML is where that
    claim is actually tested.
  - **`Stretch` visual check** (delegated here by FEATURE-3ADD): one `ei:Icon` inside a deliberately
    non-square container — e.g. a `Border Width="96" Height="32"` — with
    `Stretch="{Binding SelectedStretch}"` and a small `ComboBox` over `Stretches`, so `None` /
    `Uniform` / `UniformToFill` / `Fill` can be cycled and judged by eye. Record the observation in
    `docs/done/FEATURE-469B.md`.
  - **`SvgIconSet` cross-check:** one `ei:Icon` with `IconSet="{Binding ShapesIconSet}"` and
    `IconName="shapes"`, the set being a single-entry `SvgIconSet.FromSvgSources` built from inline
    `circle` / `ellipse` / rounded-`rect` markup — the eyeball check on FEATURE-24DD's shape→arc
    conversion (SPEC §5.1). One glyph, nothing more.
- **Row 3 — the virtualized grid.** `ScrollViewer` → `ItemsControl ItemsSource="{Binding Rows}"` with
  `ItemsPanel` explicitly set to `<VirtualizingStackPanel />`; item template (`x:DataType="vm:IconRow"`)
  is a `UniformGrid Columns="8"` over `Cells`; the cell template (`x:DataType="vm:IconEntry"`) is a
  chrome-less `Button Classes="cell"` containing an `ei:Icon` plus a small name `TextBlock`.
  - **Why this shape.** The pinned Avalonia version's core assemblies are expected to ship exactly one
    general-purpose virtualizing panel — `VirtualizingStackPanel`: no `ItemsRepeater`, no
    `UniformGridLayout`, no virtualizing wrap panel, and `Avalonia.Controls.ItemsRepeater` is not a
    centrally pinned package. **Re-verify that against `Avalonia.Controls` at the version pinned in
    `Directory.Packages.props` (SPEC §3.3) at build time** — it is a step, not a settled fact.
    A grid is therefore built as a **vertical virtualized list of rows**, each row a
    non-virtualizing `UniformGrid` of ≤8 cells:
    ~190 rows realized a screenful at a time instead of 1,512 icons realized at once. `UniformGrid`
    lives in `Avalonia.Controls.Primitives` in 12 but needs no extra xmlns.
  - **Fallback if `ItemsControl` + `VirtualizingStackPanel` does not virtualize** (it depends on the
    panel seeing a viewport through the parent `ScrollViewer`): use a `ListBox` instead — its default
    template already contains both the `ScrollViewer` and a `VirtualizingStackPanel` — with a style
    neutralizing the row selection highlight. Choose by measurement, not by assumption (step 8).
  - **Cell bindings.** `Kind` and the label come from the cell's own `IconEntry`; `Weight`, `Size`,
    and `Foreground` bind **up** to the window ViewModel via the compiled-binding parent-cast form
    `{Binding $parent[ItemsControl].((vm:MainWindowViewModel)DataContext).SelectedWeight}` (likewise
    `IconSize`, `SelectedColor.Brush`). Deliberate: shared presentation properties changing must
    **re-render the existing `Icon` instances** through SPEC §10.2's `AffectsRender`/`AffectsMeasure`
    registrations — baking them into the row records instead would rebuild the list and prove
    nothing. `Command` uses the same parent-cast form to reach `CopyXamlCommand`, with
    `CommandParameter="{Binding}"`; if the cast binding proves awkward, a `Click` handler in
    code-behind that executes the command is the acceptable fallback.
  - `IsVisible="{Binding HasResults}"` on the `ScrollViewer`, and a centred `TextBlock` bound to
    `EmptyMessage` with `IsVisible="{Binding !HasResults}"` for the empty-result state.
- **Row 4 — status line.** `TextBlock` bound to `StatusText`.
- **`MainWindow.axaml.cs`** — constructor takes `MainWindowViewModel`, calls `InitializeComponent()`,
  assigns `DataContext`; subscribes `Opened` once to call `SearchBox.Focus()` (SPEC §11: focused on
  start — focusing in the constructor is too early). A `<Style Selector="Button.cell">` in
  `Window.Styles` flattens the cell buttons (transparent background, no border, uniform padding,
  a hover highlight). Keyboard navigability comes free from the focusable cell buttons (SPEC §15).

### 7. Theming: plain Avalonia + Fluent, deliberately not Carbon.Avalonia.Desktop

SPEC §11 is explicit and this step is where the decision is honoured, so record it in the completion
doc: styling is `Avalonia.Themes.Fluent` + `Avalonia.Fonts.Inter` and nothing else.
`Carbon.Avalonia.Desktop` (and its skill) is **not** used, even though it would give a nicer shell.
Reasons: (a) a sample is a **reference consumer** — a reader must be able to see exactly which
elements come from `Enigma.Icons.Avalonia` and copy them into a stock Avalonia app, which a
third-party control library's chrome obscures; (b) it would couple this library's repo to another of
the user's libraries, inverting the dependency story and adding a package with no central pin in SPEC
§3.3; (c) the README screenshot would advertise Carbon rather than the icons. Consequences to accept:
the gallery looks plain, and the colour picker is a preset `ComboBox` — `Avalonia.Controls.ColorPicker`
is not centrally pinned either (and the local cache has no 12.x of it), which is exactly why SPEC §11
offers "picker **or** a small preset set". The preset set is the chosen half.

### 8. Verification pass and the README screenshot

1. `dotnet build Enigma.Icons.slnx` → zero warnings; `dotnet test Enigma.Icons.slnx` → all three
   existing test projects still green (the sample adds none).
2. `dotnet run --project samples/Enigma.Icons.Avalonia.Gallery` on the Linux desktop session, and
   walk the acceptance list below by hand: all six weights, duotone two-tone, live colour/size,
   **the two no-`Foreground` icons of Row 1 inheriting their `Button` / `TextBlock` scope brush**,
   search responsiveness, empty state, initial focus, clipboard paste-back.
   - **The theme-switch half of that inheritance check.** `App.axaml` sets
     `RequestedThemeVariant="Default"`, so the app follows the OS theme variant: flip the desktop
     light/dark preference while the app is running and confirm both no-`Foreground` icons repaint with
     the new scope brush. If the Linux session does not propagate the preference to Avalonia, set
     `RequestedThemeVariant="Dark"` in `App.axaml`, re-run, observe, and revert — there is no in-app
     toggle (out of scope). Either route discharges FEATURE-3ADD's delegated theme-following proof;
     record which one was used, with the observation, in `docs/done/FEATURE-469B.md`.
3. **Prove virtualization empirically.** Temporarily count realized controls — e.g. a Debug-only
   handler that reports `this.GetVisualDescendants().OfType<ei:Icon>().Count()`, or read the count off
   the dev-tools visual tree. Expect the order of a viewport (tens), never 1,512. Remove any
   temporary diagnostic before finishing.
4. Capture one screenshot with the OS screenshot tool showing the weight strip and a populated,
   filtered grid, and commit it as **`docs/img/gallery.png`** — the single canonical path (SPEC §1,
   §13) — for FEATURE-718F to embed. `*.png binary` is already covered by `.gitattributes`.

## Dependencies & ordering

- **Depends on FEATURE-3ADD** (roadmap sequencing 6, SPEC §16): the `Icon` control, `PhosphorIcon`,
  `PhosphorWeight`, and `PhosphorIconNames` must all exist and be tested first. Transitively depends
  on 21C4 (slnx, props, config), 24DD, 2DDE, and 3950.
- **Blocks FEATURE-718F**: the READMEs are written after the gallery so the documented API is the
  shipped API and the screenshot exists (roadmap sequencing 7).
- Branch `feature/feature-469b-gallery-sample` cut from `HEAD` at `/build` time.
- Nothing in this item is a prerequisite for FEATURE-74DC beyond 718F, and nothing here is packed.

## Acceptance criteria

There is **no test project for the gallery** — SPEC §12 defines exactly three, and SPEC §17 rejects
visual-regression/bitmap-baseline tests. DoD criteria 1–2 are therefore satisfied by: *the sample
compiles into the solution with zero warnings, the whole existing suite still passes unchanged, and
the app is verified by the manual visual pass — which is the entire point of this item.*

- [x] `samples/Enigma.Icons.Avalonia.Gallery/Enigma.Icons.Avalonia.Gallery.csproj` exists with
      `net10.0`, `OutputType WinExe`, `ImplicitUsings disable`, `IsPackable false`,
      `ApplicationManifest app.manifest`; every `PackageReference` carries **no** `Version=`; one
      `ProjectReference` to `Enigma.Icons.Avalonia`; **no** project references the sample.
- [x] **`Microsoft.Extensions.Hosting` and `CommunityToolkit.Mvvm` resolved at the SPEC §3.3 pinned
      versions on the gallery's first restore**, and the resolved versions are recorded in
      `docs/done/FEATURE-469B.md`. This item is the first consumer of those two pins, so FEATURE-21C4
      delegates SPEC §3.3's "verify at restore time, do not assume" obligation here. A pin that does
      not resolve is a **recorded deviation**, never a pre-authorized bump.
- [x] `Enigma.Icons.slnx` contains the `/samples/` project entry and now matches the SPEC §3.4
      end state exactly — eight `<Project>` entries — and `dotnet build Enigma.Icons.slnx` restores
      and builds all eight.
- [x] Startup is host-based per SPEC §11: `Host.CreateApplicationBuilder`, `MainWindow` **and**
      `MainWindowViewModel` registered and the window obtained via
      `host.Services.GetRequiredService<MainWindow>()` (no `new MainWindow(...)` anywhere), and the
      app runs Avalonia's classic desktop lifetime — not `host.RunAsync()`.
- [x] `App.axaml` contains a `FluentTheme` and **no `StyleInclude`** for `Enigma.Icons.Avalonia`
      (SPEC §10.2), and the app references neither `Carbon.Avalonia.Desktop` nor any package lacking
      a central pin in SPEC §3.3.
- [x] **The app launches on the Linux desktop session** via
      `dotnet run --project samples/Enigma.Icons.Avalonia.Gallery` and shows the window.
- [x] **Manual visual check across all six weights:** the same icon rendered as Thin, Light, Regular,
      Bold, Fill, Duotone is visibly correct and distinct for each; the grid re-renders when the
      weight selector changes. *(verified: six weights confirmed distinct on both theme variants by screenshot; the **weight-selector re-render** confirmed by the user in the manual pass.)*
- [x] **Duotone shows its two-tone rendering** — the ~0.2-opacity backing layer is visible behind the
      foreground layer (SPEC §7.4), i.e. the layered glyph model reaches the screen.
- [x] **Colour and size update live:** moving the size slider and changing the colour preset
      repaints the already-realized icons (SPEC §10.2 `AffectsRender`/`AffectsMeasure`) without
      rebuilding the list or restarting the app. *(verified by the user in the manual pass: slider and colour preset repaint live.)*
- [x] **Search over 1,512 names stays responsive:** typing is smooth with no perceptible stall,
      matching is case-insensitive substring, debounced, and the status line shows the filtered count
      out of 1,512. *(verified: substring filter and the "Showing 109 of 1,512 icons · Regular" status line confirmed by screenshot; **typing smoothness/debounce** confirmed by the user in the manual pass.)*
- [x] **Virtualization verified by measurement**, not assumption: the number of realized `Icon`
      instances is on the order of the visible viewport (tens), never 1,512; any temporary
      diagnostic used to measure it is removed. Method and observed number recorded in the completion doc.
- [x] Empty-result state shows `No icons match '<term>'` and the grid is hidden; clearing the box
      restores all 1,512. *(verified by the user in the manual pass.)*
- [x] The search box has keyboard focus on start, and grid cells are keyboard-reachable. *(verified: initial focus confirmed in every capture; **cell keyboard reachability** confirmed by the user in the manual pass.)*
- [x] Clicking an icon copies a snippet of the SPEC §10.3 form
      `<ei:Icon Kind="…" Weight="…" Size="…" />` reflecting the current selection — **verified by
      pasting into an editor** — and a confirmation appears; a clipboard failure does not throw. *(verified by the user in the manual pass, including the paste-back.)*
- [x] **A single `xmlns:ei="https://github.com/josueclement/Enigma.Icons"` resolves everything:**
      `MainWindow.axaml` declares that one prefix and the window **compiles and loads** with
      `ei:Icon`, `<Path Data="{ei:IconGeometry Acorn}"/>`, and `<Image Source="{ei:IconImage Acorn}"/>`
      all bound through it (SPEC §10.3) — the compile-time proof FEATURE-3ADD delegates here.
- [x] **Inherited `Foreground` / theme following (the visual proof FEATURE-3ADD delegates here):** the
      two Row 1 `ei:Icon` instances with **no `Foreground` set** — one inside a `Button` scope, one
      inside a `TextBlock` scope — both render visibly (never blank) in their enclosing scope's brush,
      i.e. SPEC §10.2's `TextElement.Foreground` `AddOwner` inheritance reaches the screen with no
      binding written. **And the inherited brush follows a theme switch:** `RequestedThemeVariant`
      is `Default`, so flipping the OS light/dark preference while the app runs must repaint both
      icons; if the session does not propagate it, the theme-switch half is verified by setting
      `RequestedThemeVariant="Dark"`, re-running, observing, and reverting (there is no in-app
      toggle). Route taken and observation recorded in `docs/done/FEATURE-469B.md`. *(verified: both no-`Foreground` icons render in their scope's brush, and the Button-scope icon repainted white → dark via the **`RequestedThemeVariant` route**, which the criterion sanctions. The OS-preference variant of the check is offered as an optional extra.)*
- [x] **`Stretch` visual check:** one `ei:Icon` in a deliberately non-square container renders as
      expected for `None`, `Uniform`, `UniformToFill`, and `Fill`; verified by eye and the observation
      recorded in `docs/done/FEATURE-469B.md`.
- [x] **`SvgIconSet` cross-check:** the one glyph built by `SvgIconSet.FromSvgSources` from inline
      `circle` / `ellipse` / rounded-`rect` markup renders correctly, eyeballing FEATURE-24DD's
      shape→arc conversion (SPEC §5.1).
- [x] **A screenshot suitable for the README is captured and committed for FEATURE-718F** (weight
      strip + populated filtered grid visible), at `docs/img/gallery.png` (SPEC §1, §13).
- [x] Every text file added is LF with a final newline (SPEC §2.7).
- [x] **`dotnet build Enigma.Icons.slnx` succeeds with zero warnings** (`TreatWarningsAsErrors` +
      `EnforceCodeStyleInBuild`, SPEC §2.1) — capture the output as build evidence. (DoD criteria 1–2.)
- [x] **`dotnet test Enigma.Icons.slnx` — whole suite green**, all three existing test projects, no
      test added, none skipped or modified. (DoD criteria 1–2.)
- [x] **Roadmap + this plan flipped to `DONE`** (`docs/roadmap.md` FEATURE-469B row; this file's
      status header). (DoD criterion 4.)
- [x] **Completion doc `docs/done/FEATURE-469B.md` written** — summary, files touched, deviations,
      build/test evidence. (DoD criterion 5.)

## Notes / risks

- **Developer tools are settled — no props change.** The gallery references
  `AvaloniaUI.DiagnosticsSupport` (2.2.1, already pinned in `Directory.Packages.props`, SPEC §3.3)
  under a Debug-only condition, and `BuildAvaloniaApp()` calls `.WithDeveloperTools()` rather than
  `this.AttachDevTools()`. **No `Directory.Packages.props` edit is needed or authorized** — the pin
  exists, so the csproj only has to reference the id with no `Version=`. There is **no
  `Avalonia.Diagnostics` release for Avalonia 12** (its newest is 11.3.12) and it must not be used
  (SPEC §3.3, §11).
- **OPEN QUESTION 2 — RESOLVED (build time): the user chose the house skill, not the SPEC.**
  `MainWindowViewModel` uses explicit `field`-keyword properties with `SetProperty` and a get-only
  `AsyncRelayCommand<IconEntry>` initialized in the constructor; the class is not `partial` and carries
  no generator attributes. SPEC §11's `[ObservableProperty]`/`[RelayCommand]` clause is therefore stale.
  Original question, kept for the record:

- **OPEN QUESTION 2 — MVVM source generators.** SPEC §11 mandates `[ObservableProperty]` /
  `[RelayCommand]`; the house `communitytoolkit-mvvm` skill forbids all MVVM generator attributes and
  requires explicit `{ get; set => SetProperty(ref field, value); }` properties plus get-only
  `RelayCommand` properties initialized in the constructor. This plan follows the SPEC (attributes),
  but the two cannot both be satisfied and the skill is the newer house convention. If the user
  prefers the skill, step 5's table maps one-to-one onto the explicit style with no other change
  (the class stops needing `partial`, and `OnSearchTextChanged` becomes the setter body).
- **The app must actually launch to be verifiable.** On Linux/Wayland, Avalonia 12 renders through
  the X11 backend (XWayland); the run command is
  `dotnet run --project samples/Enigma.Icons.Avalonia.Gallery`. A session without
  `DISPLAY`/`WAYLAND_DISPLAY` (bare SSH, container) cannot complete this item — the manual visual
  criteria are not substitutable by a headless render, and a build-only pass must **not** be reported
  as DONE. If the window fails to open, that is a blocker to raise, not a criterion to waive.
- **Virtualization is a correctness-of-the-demo issue, not a nicety.** Realizing all 1,512 cells
  would force every visible weight's `.dat` table to load, parse ~1,512 glyph records per weight, and
  allocate thousands of Avalonia geometries in one layout pass — a large allocation spike and a
  multi-second stall, which would misrepresent a library whose whole performance story (SPEC §15) is
  a cached dictionary hit. A gallery that stutters is worse than no gallery: it would read as the library
  being slow. Hence the empirical virtualization criterion above.
- **`ItemsControl` + `VirtualizingStackPanel` needs proving.** The panel derives from
  `VirtualizingPanel`, which obtains its viewport from an ancestor `ScrollViewer` via
  `EffectiveViewportChanged`; if that wiring does not engage in an `ItemsControl`, switch to `ListBox`
  (step 6's fallback) rather than shipping a silently non-virtualizing grid.
- **Confirm the two gallery-only pins resolve at first restore** (also an acceptance criterion —
  FEATURE-21C4 delegates SPEC §3.3's restore obligation for these two pins to this item). SPEC §3.3 pins
  `Microsoft.Extensions.Hosting` **10.0.8** and `CommunityToolkit.Mvvm` **8.4.2** — the only pins this
  item is the first to consume. §3.3's rule is "**Verify at restore time, do not assume**": confirm
  both resolve at the gallery's first restore. If either does not, raise it as a recorded deviation
  and get it decided — a version change here is **never** a pre-authorized bump.
- **Project count.** The item brief describes the §3.4 end state as "7 projects"; the SPEC §3.4
  listing has **eight** `<Project>` entries (3 + 3 + 1 + 1). The SPEC wins; the criteria say eight.
- **`.editorconfig` at warning severity is a build gate** for this project too. The first app in the
  solution is the most likely place to trip `csharp_style_namespace_declarations = file_scoped`,
  `csharp_using_directive_placement = outside_namespace`, and
  `dotnet_style_require_accessibility_modifiers` — all `:warning`, hence errors under
  `TreatWarningsAsErrors`. Write the files in house style the first time rather than fixing a wall of
  build errors afterwards.
- **Compiled bindings are on by default in Avalonia 12**: every view root and every `DataTemplate`
  needs `x:DataType` or the build fails. Do not sprinkle `x:CompileBindings="True"`, and do not
  disable it to make a binding compile — a binding that needs `ReflectionBinding` here is a smell.
- **Namespace shadowing** (step 3, final bullet) is the single most likely compile failure in this
  item: `Enigma.Icons.Avalonia.Gallery` makes bare `Avalonia.` qualifications resolve to
  `Enigma.Icons.Avalonia`.
- **API feedback loop.** If any SPEC §10 API turns out to be awkward from the consumer seat (e.g.
  the `Foreground`-null-paints-nothing rule biting the colour presets), record it in the completion
  doc as a finding for a post-1.0 item. Do not patch `src/` from inside this sample.
