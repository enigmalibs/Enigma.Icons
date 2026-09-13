**Status:** TODO · Single-phase · Branch `feature/feature-0dd4-icon-studio-reset`
**Type:** FEATURE
**Run:** feature/2026-09-13-icon-studio-reset

# FEATURE-0DD4 — Reset button in the app icon studio

## Objective

Give `tools/Enigma.Icons.AppIconStudio` a **Reset** control that puts the design and the output
settings back to the values the window opens with — the colours, the sizes, the weight, the fill
mode, the corner radius, the base name and the size check-lists — in one click.

## Context & constraints

### What the studio starts with today

Every startup value is currently expressed **once, as a property initializer**, scattered across
`MainWindowViewModel`:

| Setting | Startup value | Where it lives today |
|---|---|---|
| `ActiveIcon` / `SelectedIcon` | `PhosphorIcon.MarkdownLogo` | constructor body |
| `SelectedWeight` | `WeightOptionList[4]` (Fill) | property initializer |
| `SelectedFillMode` | `FillModeOptionList[0]` (Solid) | property initializer |
| `PrimaryColor` | `#3B72F0` | property initializer |
| `SecondaryColor` | `#1E3A8A` | property initializer |
| `GradientAngle` | `PlateFill.DefaultAngleDegrees` (45°) | property initializer |
| `GlyphColor` | `Colors.White` | property initializer |
| `CornerRadiusRatio` | `IconDesign.DefaultCornerRadiusRatio` (0.22) | property initializer |
| `GlyphScale` | `IconDesign.DefaultGlyphScale` (0.60) | property initializer |
| `BaseName` | `"app"` | property initializer |
| `IcoSizes` ticks | all of 16/24/32/48/64/128/256 | `OfferedIcoSizes` + `BuildSizeOptions` |
| `PngSizes` ticks | 256/512/1024 of 16…1024 | `DefaultPngSizes` + `BuildSizeOptions` |
| `SearchText` | `string.Empty` | property initializer |
| `OutputDirectory` | `string.Empty` | property initializer |

**That layout is the real problem this item has to solve.** A `Reset()` that re-states those
literals is a second source of truth, and the two drift the first time a default is tuned. So the
work is: hoist the defaults into **one** place, then have both the constructor and `Reset()` read
from it.

### Constraints inherited from the solution

- SPEC §2 rules 1–11 apply in full — zero-warning build, `ImplicitUsings` disable, nullable, LF with
  a final newline, XML docs on every public member, no `Enum.ToString`/`Enum.Parse` on a hot path.
- **Nothing under `src/` is touched**, no `<Version>` moves, `RELEASENOTES.md` and
  `PackageReleaseNotes` stay as `FEATURE-74DC` wrote them. This is a change to a **non-packable**
  tool only.
- **No new package pin.** A plain `Button` and a `RelayCommand` need nothing that is not already
  referenced.
- MVVM stays the house explicit CommunityToolkit style: `field` + `SetProperty`, get-only command
  properties initialized in the constructor, **no `[ObservableProperty]` / `[RelayCommand]`**.
- Avalonia's `AVLN*` XAML diagnostics are **not** promoted by `TreatWarningsAsErrors` — read the
  per-project warning count on the studio project, never the exit code alone.
- The generator's `--check` must still exit 0 (it is untouched, but the gate stands).

## Scope

### In scope

- `tools/Enigma.Icons.AppIconStudio/StudioDefaults.cs` — new, the single source of truth for every
  startup value.
- `tools/Enigma.Icons.AppIconStudio/MainWindowViewModel.cs` — read the defaults from
  `StudioDefaults`, add `ResetCommand` and the `Reset()` it runs.
- `tools/Enigma.Icons.AppIconStudio/SizeOption.cs` — carry `IsSelectedByDefault`, so a size list's
  default ticks travel with the options rather than being re-derived.
- `tools/Enigma.Icons.AppIconStudio/MainWindow.axaml` — the Reset button on the design pane's header
  row, with a tooltip naming what it restores.
- `tests/Enigma.Icons.AppIconStudio.UnitTests/ResetTests.cs` — new.
- Documentation sweep: the tool's `README.md` and `docs/SPEC.md` §18.5.

### Out of scope

- **Any change under `src/`**, any version bump, `RELEASENOTES.md`, `PackageReleaseNotes`,
  `THIRD-PARTY-NOTICES.md`.
- **Presets, profiles or persistence.** `FEATURE-4E1F` decision 10 put a settings store out of
  scope and this item does not reopen it: Reset restores *compiled* defaults, not a remembered
  state.
- **Undo/redo.** A reset is not a history stack, and building one is a different item.
- A confirmation dialog, and any dialog infrastructure to host one.
- `FEATURE-6FA1` (WPF, deferred), `FEATURE-1608`, the gallery sample, the generator.

## Design

### `StudioDefaults` — one place, read twice

```csharp
public static class StudioDefaults
{
    public const PhosphorIcon Icon = PhosphorIcon.MarkdownLogo;
    public const PhosphorWeight Weight = PhosphorWeight.Fill;
    public const PlateFillMode FillMode = PlateFillMode.Solid;
    public const double GradientAngleDegrees = PlateFill.DefaultAngleDegrees;
    public const double CornerRadiusRatio = IconDesign.DefaultCornerRadiusRatio;
    public const double GlyphScale = IconDesign.DefaultGlyphScale;
    public const string BaseName = "app";

    public static Color PrimaryColor { get; }    // #3B72F0
    public static Color SecondaryColor { get; }  // #1E3A8A
    public static Color GlyphColor { get; }      // white
}
```

`Color` cannot be a `const`, so the three colours are get-only static properties; everything else is
a `const` so a property initializer can use it. The `IconDesign.*` and `PlateFill.*` constants are
**forwarded, not copied** — the design model stays the authority on its own ranges.

Public rather than internal: the test project takes a plain `ProjectReference` (no
`InternalsVisibleTo` in this solution), and the tests assert against these names instead of
re-typing `#3B72F0` — which is the whole point of the type.

### `SizeOption.IsSelectedByDefault`

```csharp
public SizeOption(int sizePx, bool isSelected)   // isSelected is also the default
{
    SizePx = sizePx;
    IsSelectedByDefault = isSelected;
    IsSelected = isSelected;
}

public bool IsSelectedByDefault { get; }
```

The default travels with the option, so `BuildSizeOptions` stays the one place the offered set and
its initial ticks are expressed, and `Reset()` never needs to know what `DefaultPngSizes` holds.

### `Reset()` and `ResetCommand`

```csharp
public RelayCommand ResetCommand { get; }   // get-only, initialized in the constructor
public void Reset()
```

`Reset()` restores, in order: `SearchText` (which re-runs the filter), the active icon, weight, fill
mode, all three colours, gradient angle, corner radius, glyph scale, base name, and both size
lists' ticks. It then **stops the debounce timer and calls `RegeneratePreview()` synchronously** —
the same closing move the constructor already makes — so:

- the preview and the thumbnail strip are correct the instant the button is released, rather than
  150 ms later;
- `StatusText` ends up describing the restored design, via the existing `DescribeDesign()`;
- a test can assert the whole outcome without advancing a `DispatcherTimer`.

`OutputDirectory` is **deliberately not reset.** It is a session destination, not a design value:
clearing it re-disables *Generate* and sends the user back through the folder dialog, which costs
more than the reset saves. The button's tooltip says so, so the behaviour is not a surprise.

`RefreshGenerateState()` is called at the end: the base name and the size ticks both feed
`CanGenerate`, and restoring them can flip the button either way.

### UI

The design-controls pane's first line becomes a header row — the existing `Glyph` section label on
the left, the Reset button right-aligned on the same line:

```xml
<Grid ColumnDefinitions="*,Auto">
  <TextBlock Grid.Column="0" Classes="section" Text="Glyph" Margin="0,0,0,2" />
  <Button Grid.Column="1"
          Content="Reset"
          Padding="14,2"
          Command="{Binding ResetCommand}"
          ToolTip.Tip="Restore the icon, colours, sizes and output options to their startup
                       values. The output folder is kept." />
</Grid>
```

Placed on the design pane rather than beside *Generate*: the pinned output panel holds the primary,
irreversible-ish action, and a Reset one misclick away from it is a trap.

## Acceptance criteria

1. `dotnet build Enigma.Icons.slnx` succeeds with **0 warnings**, and the studio project itself
   reports 0 (checked in the per-project output, not from the exit code).
2. `dotnet test --solution Enigma.Icons.slnx` passes; the 5 pre-existing `SvgIconSetPathSafetyTests`
   symlink skips stay skipped and no new skip appears.
3. Nothing under `src/` is modified; `Directory.Packages.props` gains no pin; no
   `<PackageReference>` anywhere gains a `Version=`.
4. A new `StudioDefaults` type is the only place each startup value is written, and
   `MainWindowViewModel`'s property initializers read from it.
5. `ResetCommand` is bound from `MainWindow.axaml` and the window still builds its compiled
   bindings (`AvaloniaUseCompiledBindingsByDefault` is on, so a bad path is a build failure).
6. `ResetTests.cs` covers, at minimum:
   - every design value changed away from its default and restored by `Reset()`;
   - the ICO and PNG tick sets restored, including a size the user unticked and one they ticked;
   - `BaseName` restored to `"app"`;
   - `SearchText` cleared, the full catalog listed again, and `SelectedIcon` highlighted;
   - `OutputDirectory` **unchanged** by a reset;
   - the preview, thumbnails and `StatusText` refreshed synchronously (no timer advance);
   - `Reset()` on a freshly constructed ViewModel is a no-op that leaves the defaults in place;
   - `GenerateCommand.CanExecute` re-evaluated after a reset that restores an untickable state.
7. The tool's `README.md` documents the button, and `docs/SPEC.md` §18.5 names it in the UI
   description.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| What does Reset restore? | The design values, the ICO/PNG tick sets, the base name, and the search box | The plain reading of "back to how it opened"; clearing the search is what makes the restored icon visible and re-highlighted in the list | resetting only the design controls, leaving the output panel stale |
| Is the output folder reset? | **No**, deliberately kept, and the tooltip says so | The folder is a session destination, not a design value; clearing it re-disables *Generate* and forces another file-dialog trip — an undo that costs more than it saves | clearing it for literal symmetry with "as the app opened" |
| Where do the defaults live? | A new `StudioDefaults` static class, read by both the constructor and `Reset()`; `SizeOption` carries its own `IsSelectedByDefault` | A reset that repeats literals is a reset that drifts the first time a default is tuned. One source of truth is the only version of this feature worth shipping | re-stating the literals inside `Reset()`; a `MainWindowViewModel.CreateDefault()` factory that the constructor cannot use |
| Are the defaults public? | Yes | The test project uses a plain `ProjectReference` (this solution has no `InternalsVisibleTo`), and the tests assert against the named defaults rather than re-typing `#3B72F0` | `internal` + `InternalsVisibleTo`, new infrastructure for no gain |
| Does Reset wait for the debounce? | No — stop the timer, render synchronously, exactly as the constructor already does | The button feels instant, `StatusText` lands on the restored design, and the tests need no dispatcher-timer advance | letting the 150 ms debounce pick it up (untestable without sleeping) |
| Confirmation prompt? | No | Nothing is written to disk, and the app has no dialog infrastructure — adding one is infrastructure this request did not ask for | a `ContentDialog`-style confirm |
| Disabled when already at defaults? | No, always enabled | Would mean comparing eleven values on every property change for near-zero benefit, and "why is Reset greyed out?" is a worse question than a harmless click | a `CanExecute` that tracks dirtiness |
| Placement | Right-aligned on the design pane's header row | Discoverable where the values it restores are, and safely away from the pinned *Generate* primary action | beside *Generate* (misclick risk); a menu bar (the window has none) |
| Keyboard shortcut | None | Not requested, and `Ctrl+R` has no settled meaning in this kind of tool | a `HotKey` on the button |
| Command type | `RelayCommand`, get-only, initialized in the constructor | Matches `BrowseCommand`/`GenerateCommand`; the work is synchronous, so `AsyncRelayCommand` would be dishonest | `AsyncRelayCommand`; a code-behind `Click` handler |
| Test placement | A new `ResetTests.cs` | Reset spans the design pane *and* the output panel; one file keeps the behaviour together instead of splitting it across `MainWindowViewModelTests` and `ExportUiTests` | appending to both existing files |
