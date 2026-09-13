**Status:** DONE · Multi-phase (6 phases, all complete) · Branches `feature/feature-4e1f-phaseNN-<slug>`
**Run:** feature/2026-09-13-app-icon-studio

# FEATURE-4E1F — App icon studio (.ico + .png generator)

## Objective

A desktop tool in this solution that **composes an application icon** — a rounded plate (solid
colour or a two-stop linear gradient) with a Phosphor glyph in a chosen colour on top — and
**generates the shipping assets** from it: one multi-frame `.ico` and a set of standalone `.png`
files.

The reference look is the existing `Enigma.MarkdownEditor` app icon: a blue rounded square with a
white glyph, corner radius ≈ 22 % of the edge, glyph occupying ≈ 60 % of the plate.

## Context

### Why both an `.ico` and PNGs — the question the request asked

`Enigma.MarkdownEditor` uses **both**, for different jobs:

| Asset | Consumed by |
|---|---|
| `src/…Desktop/Assets/app.ico` | `<ApplicationIcon>` in the csproj (the `.exe`'s Explorer/taskbar icon) and `Window.Icon` |
| `src/…Desktop/Assets/app-256.png` | `AboutView.axaml` and `SplashWindow` — its plan file records that *which frame Skia picks out of an icon is unspecified*, which is why a real PNG was extracted |
| `packaging/linux/enigma-markdown-editor.png` | the Linux `.desktop` entry |

So the splash/about artwork is a **PNG**, not the `.ico`. The tool must therefore emit both, and the
output file names must drop straight into that layout: `<base>.ico` and `<base>-<size>.png`.

### Verified before planning (do not re-derive)

| Question | Answer, measured in this repo on 2026-09-13 |
|---|---|
| Does the .NET 10 SDK accept a PNG-framed `.ico` for `<ApplicationIcon>`? | **Yes.** A probe project built clean and the PNG payload is embedded byte-for-byte in the `.exe` icon resource; `Icon.ExtractAssociatedIcon` reads it back. |
| Can `RenderTargetBitmap` be driven under `Avalonia.Headless`? | **Yes**, with `AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }` plus `.UseSkia()`. Gradients, `RoundedRect` and `DrawGeometry` all raster correctly. |
| How is straight (unpremultiplied) BGRA obtained? | `RenderTargetBitmap.CopyPixels(ILockedFramebuffer)` into a `WriteableBitmap(PixelFormat.Bgra8888, AlphaFormat.Unpremul)` — Skia performs the un-premultiply. The render target itself is `Bgra8888`/`Premul`. |
| Is `Bitmap.Save(Stream, int?)` usable? | **No** — obsolete in Avalonia 12.1 (`CS0618`), which `TreatWarningsAsErrors` turns into a build error. Use `Save(Stream, BitmapEncoderOptions)` with `PngBitmapEncoderOptions`. |
| Is `Avalonia.Controls.ColorPicker` 12.1.0 available? | **Yes**, and it ships its own themes — `App.axaml` needs `<StyleInclude Source="avares://Avalonia.Controls.ColorPicker/Themes/Fluent/Fluent.xaml" />`; the Fluent theme package does **not** carry them. |
| Baseline | `dotnet build Enigma.Icons.slnx` → succeeded, **0 warnings**. `dotnet test --solution Enigma.Icons.slnx` → **479 total, 474 passed, 0 failed, 5 skipped** (the 5 are `SvgIconSetPathSafetyTests` symlink cases, skipped because this Windows session cannot create symlinks). |

### Constraints inherited from the solution

- The **1.0.0 line is feature-complete and unpublished**. This item adds two *non-packable* projects
  and must not touch a `<Version>`, `PackageReleaseNotes`, `RELEASENOTES.md`, the root README
  what's-new callout, or any file under `src/`.
- `FEATURE-6FA1` (WPF) stays deferred and is not touched.
- SPEC §2 rules 1–11 apply in full. SPEC §3.4's eight-entry slnx is *final for the 1.0.0 line*; it
  explicitly anticipates post-1.0 growth, and this item appends two entries.
- SPEC §2.10 (zero third-party runtime dependencies) is **unaffected**: the new package pin is
  referenced only by a non-packable project, so nothing enters a `.nupkg` dependency graph and
  `THIRD-PARTY-NOTICES.md` needs no edit.

## Scope

### In scope

- `tools/Enigma.Icons.AppIconStudio/` — new non-packable Avalonia desktop app (`net10.0`).
- `tests/Enigma.Icons.AppIconStudio.UnitTests/` — new `xunit.v3` test project (`net10.0`).
- `Enigma.Icons.slnx` — two appended `<Project>` entries.
- `Directory.Packages.props` — one new pin, `Avalonia.Controls.ColorPicker` 12.1.0, added to the
  version-coupled Avalonia group.
- `docs/SPEC.md` (new §18 plus §1 tree and §3.4 note), root `README.md`, `CLAUDE.md`, and the tool's
  own `README.md` — PHASE06 only.

### Out of scope

- **Any change under `src/`**, any version bump, `RELEASENOTES.md`, `PackageReleaseNotes`, or
  `THIRD-PARTY-NOTICES.md`.
- **Generated Phosphor assets** (`Assets/*.dat`, `PhosphorIcon.g.cs`, `PhosphorIconNames.g.cs`) and
  `tools/Enigma.Icons.Generator`. The generator's `--check` must still exit 0 after every phase.
- The gallery sample, and `FEATURE-1608` (`docs/internals.html`).
- **Design presets / persistence** — no saved profiles, no remembered output folder, no settings
  file. The tool keeps no state between runs. (Decision 10.)
- **SVG master export.** The request names `.ico` and a raster splash asset. (Decision 11.)
- Committing any generated `.ico`/`.png` into this repository. The tool writes to a folder the user
  picks; nothing it produces is tracked here.
- An installer, a publish profile, or a `dotnet tool` package. Run from source, like the gallery.
- Localization, telemetry, logging.

## Design

### Projects

```
tools/Enigma.Icons.AppIconStudio/          net10.0 · WinExe · IsPackable=false
├─ Enigma.Icons.AppIconStudio.csproj
├─ README.md                               tool docs, NOT packed (PHASE06)
├─ app.manifest
├─ Program.cs · App.axaml · App.axaml.cs
├─ MainWindow.axaml · MainWindow.axaml.cs · MainWindowViewModel.cs
├─ Design/      IconDesign.cs · PlateFill.cs · PlateFillMode.cs
├─ Rendering/   IconLayout.cs · IIconRasterizer.cs · AvaloniaIconRasterizer.cs
├─ Export/      IcoFrame.cs · IcoFrameFormat.cs · IcoWriter.cs ·
│               ExportRequest.cs · ExportResult.cs · IconExporter.cs
└─ Services/    IFolderPicker.cs · AvaloniaFolderPicker.cs

tests/Enigma.Icons.AppIconStudio.UnitTests/  net10.0 · Exe · xunit.v3 + Avalonia.Headless.XUnit
```

Namespaces mirror the folders (`Enigma.Icons.AppIconStudio.Design`, `.Rendering`, `.Export`,
`.Services`); the window, ViewModel and bootstrap sit in the root namespace.

Types are **public and XML-documented** rather than internal plus `InternalsVisibleTo`: the test
project takes a plain `ProjectReference`, and `GenerateDocumentationFile` is deliberately *not* set
on a non-packable project (the gallery precedent), so CS1591 does not fire — the docs are house
style, not a build gate.

### The design model (`Design/`)

```csharp
public enum PlateFillMode { Solid, LinearGradient }

public sealed class PlateFill          // immutable
{
    PlateFillMode Mode
    Color PrimaryColor                 // Avalonia.Media.Color — the solid colour, or stop 0
    Color SecondaryColor               // stop 1; ignored when Mode == Solid
    double AngleDegrees                // 0 = left to right, 90 = top to bottom; wrapped into [0,360)
}

public sealed class IconDesign         // immutable
{
    PhosphorIcon Icon
    PhosphorWeight Weight
    Color GlyphColor
    PlateFill Plate
    double CornerRadiusRatio           // 0.0-0.5 of the plate edge; 0.5 == circle; default 0.22
    double GlyphScale                  // 0.2-1.0 fraction of the edge the glyph view box spans; default 0.60
}
```

Constructors validate and throw `ArgumentOutOfRangeException` on a NaN or out-of-range ratio, using
the same negated-comparison idiom as `IconViewBox` so NaN is rejected rather than admitted.

`Avalonia.Media.Color` is used deliberately: it is a plain struct in `Avalonia.Base` needing no
platform, it binds straight to `ColorPicker.Color`, and this project already depends on Avalonia.

### Layout math (`Rendering/IconLayout.cs`) — pure, no platform needed

```csharp
static RoundedRect PlateRect(double size, double cornerRadiusRatio)
static Matrix GlyphTransform(IconViewBox viewBox, double size, double glyphScale)
static (RelativePoint Start, RelativePoint End) GradientLine(double angleDegrees)
```

`GlyphTransform` mirrors `Icon.Render`'s `Stretch.Uniform` maths — uniform scale to the *smaller*
axis, centred, minus the scaled view-box origin — but into the centred square of side
`size * glyphScale` rather than the full bounds. `GradientLine` maps an angle to two unit-square
relative points, so 0° is left-to-right and 90° is top-to-bottom.

### Rasterizer (`Rendering/`)

```csharp
public interface IIconRasterizer
{
    byte[] RenderPng(IconDesign design, int sizePx);    // a complete PNG file
    byte[] RenderBgra(IconDesign design, int sizePx);   // straight BGRA, top-down, stride = sizePx*4
}
```

`AvaloniaIconRasterizer` implements both over one private `RenderTargetBitmap` pass:

1. `new RenderTargetBitmap(new PixelSize(size, size))` — default 96 DPI, so 1 DIP == 1 px.
2. `ctx.DrawRectangle(plateBrush, null, IconLayout.PlateRect(size, ratio))`.
3. `using (ctx.PushTransform(IconLayout.GlyphTransform(...)))` then one `DrawGeometry` per glyph
   layer, taking the paint decision from `IconGlyphExtensions` semantics — the glyph colour brush
   for fills, layer opacity honoured via `PushOpacity`.
4. PNG: `Save(stream, new PngBitmapEncoderOptions())`. BGRA: `CopyPixels` into a
   `WriteableBitmap(Bgra8888, Unpremul)` and `Marshal.Copy` out of the locked framebuffer, row by
   row, honouring `RowBytes`.

Glyphs come from `PhosphorIconSet.Instance.GetGlyph(icon, weight)`; layer geometry is parsed with
`Geometry.Parse`. Rendering is **UI-thread work** and is never moved off it.

### ICO container (`Export/IcoWriter.cs`) — pure byte assembly

`PngFrameThreshold = 256`: a frame of 256 px or more is stored as a **PNG file**; anything smaller
as a **32-bpp BMP DIB**. This is the conventional layout — it keeps a 7-frame icon around 10-15 KB
instead of ~290 KB, while the small sizes that legacy shell paths render directly stay in the format
they have always used.

```
ICONDIR       reserved u16 = 0 · type u16 = 1 · count u16 = N
ICONDIRENTRY  bWidth u8 (0 means 256) · bHeight u8 (0 means 256) · bColorCount u8 = 0 ·
              bReserved u8 = 0 · wPlanes u16 = 1 · wBitCount u16 = 32 ·
              dwBytesInRes u32 · dwImageOffset u32
payloads      in the same order as the entries
```

A DIB payload is `BITMAPINFOHEADER` (40 B, `biHeight = size * 2`, `biBitCount = 32`,
`biCompression = BI_RGB`, `biSizeImage = xorBytes + andBytes`), then the XOR image — the BGRA pixels
written **bottom-up** — then the AND mask: 1 bit per pixel, rows padded to 4 bytes, bottom-up, all
zero (the 32-bpp alpha channel is authoritative). A PNG payload is the PNG file verbatim.

Frames are de-duplicated and written in ascending size order. Sizes outside 1-256 are rejected.

### Export (`Export/IconExporter.cs`)

```csharp
sealed class ExportRequest { IconDesign Design; string OutputDirectory; string BaseName;
                             IReadOnlyList<int> IcoSizes; IReadOnlyList<int> PngSizes; }
sealed class ExportResult  { IReadOnlyList<string> WrittenFiles; }

Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
```

Rasterizes on the calling (UI) thread, writes with `File.WriteAllBytesAsync`. Writes `<base>.ico`
when `IcoSizes` is non-empty and `<base>-<size>.png` per PNG size. `BaseName` is validated against
`Path.GetInvalidFileNameChars()` and rejected if it is empty, a bare dot or double dot, or contains
a separator; each resolved output path is re-checked to be inside `OutputDirectory`
(`Path.GetFullPath` plus an ordinal prefix test) before anything is written. Existing files are
overwritten and named in the result.

### UI (`MainWindow`, `MainWindowViewModel`)

Two columns in a ~1180×800 window.

- **Left** — the catalog: a debounced search box over all 1,512 `PhosphorIconNames.All` entries and a
  `ListBox` on a `VirtualizingStackPanel`, each row an `ei:Icon` plus the name. Empty-result state
  message. Search box focused on open.
- **Right, top** — the preview: a 256 px `Image` bound to the rasterized PNG, on a checkerboard
  `DrawingBrush` so the transparent corners are visible, plus a 64/48/32/16 thumbnail strip — the
  place where a too-large `GlyphScale` becomes obvious before export.
- **Right, middle** — the design controls: weight selector, plate fill mode toggle, primary and
  secondary `ColorPicker`s (secondary disabled in `Solid` mode), gradient angle slider, corner
  radius slider, glyph `ColorPicker`, glyph scale slider.
- **Right, bottom** — output: folder box plus *Browse…*, base-name box, the ICO and PNG size
  check-lists, a *Generate* button, and a status line.

MVVM is the explicit CommunityToolkit style (`field` plus `SetProperty`, get-only command properties
initialized in the constructor) — **no generator attributes**, per the house skill and the gallery
precedent. `AvaloniaUseCompiledBindingsByDefault` is set to `true` and every view and `DataTemplate`
root carries `x:DataType`.

Preview bitmaps are **deliberately not disposed**: an `Image` keeps the `Bitmap` it was handed, and
disposing the previous one races the render thread. They are left to GC, and regeneration is
debounced at 150 ms so churn stays bounded.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Where does the project live, and what is it called? | `tools/Enigma.Icons.AppIconStudio` plus a matching `.UnitTests` | `tools/` already holds this repo's non-packable utilities; `samples/` is reserved for consumer-facing library demos | `samples/…`; a console tool — the request explicitly asks for colour pickers and buttons |
| How is the artwork rasterized? | Avalonia `RenderTargetBitmap` plus `DrawingContext` | No new rendering dependency, and the preview can be the exact exported bytes | SkiaSharp or ImageSharp directly (a third-party dependency for what Avalonia already does); a hand-rolled rasterizer |
| ICO frame encoding | PNG at 256 px and above, 32-bpp BMP DIB below | Conventional and maximally compatible; ~10 KB instead of ~290 KB. PNG-frame acceptance was verified against the SDK before planning | all-BMP (what the existing placeholder does — 285 KB); all-PNG (less safe on legacy small-icon paths) |
| Which sizes are offered? | ICO 16/24/32/48/64/128/256, all on by default; PNG 16/32/64/128/256/512/1024, with 256/512/1024 on | Covers Windows' recommended icon set plus the splash/about asset the request asks for | a fixed non-configurable set; free-form numeric entry |
| Output file naming | `<base>.ico`, `<base>-<size>.png` | Exactly the shape `Enigma.MarkdownEditor` consumes today | timestamped names; a per-run subfolder |
| Colour input | `Avalonia.Controls.ColorPicker` 12.1.0 | The request is explicit about free colour choice for both plate and glyph; a preset list cannot satisfy it | the gallery's `ColorPreset` list; a hex-only `TextBox` |
| Gradient shape | Two-stop linear gradient with a 0-360° angle slider | Covers every plate look described without a stop editor | a multi-stop editor; radial gradients |
| Plate geometry | Edge-to-edge rounded square, radius 0-50 % of the edge (50 % = circle), default 22 % | 22 % matches the existing MarkdownEditor plate; the one slider yields square and circle for free | a fixed radius; a separate shape selector; a plate margin/inset slider |
| Preview strategy | Rendered through the *same* rasterizer at 256 px, plus 64/48/32/16 thumbnails, over a checkerboard | Guarantees WYSIWYG; the thumbnails are where a bad glyph scale shows up | composing the preview from `Border` plus `ei:Icon` — cheaper, but the preview would no longer be the output |
| Presets / persistence | Out of scope | Not requested, and it would add a settings store this repo does not have | a JSON preset file; remembering the last output folder |
| SVG master export | Out of scope | The request names `.ico` and a raster splash asset | emitting an `.svg` alongside |
| Threading | Rasterize on the UI thread; only file writes are async | Avalonia's drawing surface and `Geometry.Parse` are UI-thread work here, and an export is a handful of ≤ 1024 px frames | wrapping the whole export in `Task.Run` |
| Test strategy | Model, layout math, ICO bytes and exporter unit-tested; the rasterizer exercised under headless Skia; window and ViewModel verified by eye | Matches SPEC §12's split and the gallery precedent, and headless-Skia rasterization was verified feasible before planning | bitmap baseline / visual-regression tests (SPEC §17 excludes them); no tests at all |
| MVVM style | Explicit `field` plus `SetProperty`, get-only command properties | The house `communitytoolkit-mvvm` skill bans every MVVM generator attribute; the gallery already follows it | `[ObservableProperty]` / `[RelayCommand]` |
| xUnit version | Stay on the solution's pinned `xunit.v3` 3.2.2 | The `xunit-v3` skill prescribes 4.x but also says to stay consistent with an existing solution; migrating is its own work item | adding a second xUnit version to CPM |
| Does anything under `src/` change? | No | The 1.0.0 line is feature-complete; the tool consumes the shipped public API exactly as a consumer would, which is itself a useful check | adding a plate/glyph composition helper to `Enigma.Icons.Avalonia` |

## Cross-phase constraints

Every phase must hold all of these, not only its own acceptance criteria:

1. **Zero-warning build.** `dotnet build Enigma.Icons.slnx` reports `0 Warning(s)`. Avalonia's
   `AVLN*` XAML diagnostics are **not** promoted by `TreatWarningsAsErrors` — read the per-project
   warning count on anything that compiles `.axaml`, never the exit code alone.
2. **Whole suite green.** `dotnet test --solution Enigma.Icons.slnx`; the 5 pre-existing symlink
   skips stay skipped and no new skip appears.
3. **Nothing under `src/` is modified**, and the generator's `--check` still exits 0.
4. SPEC §2 rules 2-5, 7, 8, 11 (explicit usings, nullable, LangVersion 14, CPM with no inline
   `Version=`, LF plus final newline, XML docs on public members, no `Enum.ToString` on a hot path).
5. No new package pin beyond `Avalonia.Controls.ColorPicker` 12.1.0, and it joins the coupled
   Avalonia group with a comment saying why it is pinned at the same version.

---

## PHASE01 — Project scaffolding & design model

**Status:** DONE · Branch `feature/feature-4e1f-phase01-scaffolding`

### Steps

1. Create `tools/Enigma.Icons.AppIconStudio/Enigma.Icons.AppIconStudio.csproj`: `net10.0`, `WinExe`,
   `ImplicitUsings` disable, `IsPackable=false`, `ApplicationManifest=app.manifest`,
   `AvaloniaUseCompiledBindingsByDefault=true`. Package references (no `Version=`): `Avalonia`,
   `Avalonia.Desktop`, `Avalonia.Themes.Fluent`, `Avalonia.Fonts.Inter`,
   `Avalonia.Controls.ColorPicker`, `CommunityToolkit.Mvvm`, `Microsoft.Extensions.Hosting`, and
   `AvaloniaUI.DiagnosticsSupport` with the gallery's Debug-only asset conditions. One
   `ProjectReference` to `src/Enigma.Icons.Avalonia`.
2. Copy the gallery's `app.manifest` shape.
3. Add `Avalonia.Controls.ColorPicker` 12.1.0 to the coupled Avalonia `ItemGroup` in
   `Directory.Packages.props`, with a comment recording that it belongs to the version-coupled set
   and is referenced only by the non-packable studio tool.
4. `Program.cs` (STAThread, `StartWithClassicDesktopLifetime`, `WithDeveloperTools()` under
   `#if DEBUG`, `WithInterFont`, `LogToTrace`) and `App.axaml` / `App.axaml.cs` following the gallery
   exactly, plus the `ColorPicker` Fluent `StyleInclude`. The host registers `IIconRasterizer`,
   `IFolderPicker`, `IconExporter`, `MainWindowViewModel` and `MainWindow` as they arrive in later
   phases; in this phase it registers `MainWindowViewModel` and `MainWindow` only.
5. A placeholder `MainWindow` (title plus a single `TextBlock`) and an empty `MainWindowViewModel`,
   so the app builds and runs from the first phase.
6. `Design/PlateFillMode.cs`, `Design/PlateFill.cs`, `Design/IconDesign.cs` per *Design* above, with
   validation and full XML docs.
7. Create `tests/Enigma.Icons.AppIconStudio.UnitTests/` — `net10.0`, `Exe`, `IsPackable=false`,
   `ImplicitUsings` disable, references `xunit.v3` and `Avalonia.Headless.XUnit` (no `Version=`),
   `ProjectReference` to the tool. Add `TestAppBuilder.cs` with
   `UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).UseSkia()` and
   the `[assembly: AvaloniaTestApplication]` attribute — PHASE02 needs real rasterization.
8. Append both projects to `Enigma.Icons.slnx` under `/tools/` and `/tests/`.
9. Tests: `IconDesignTests`, `PlateFillTests` — defaults, boundary acceptance (0.0, 0.5, 1.0),
   rejection of out-of-range and NaN ratios, angle wrapping.

### Acceptance criteria

- `dotnet build Enigma.Icons.slnx` succeeds with **0 warnings**; the two new projects report 0.
- `dotnet test --solution Enigma.Icons.slnx` passes, now with a fourth test assembly.
- `dotnet run --project tools/Enigma.Icons.AppIconStudio` opens a window (verified by launch on a
  desktop session; if the session is headless, record that in the completion doc).
- `Directory.Packages.props` carries exactly one new pin, and no `<PackageReference>` anywhere gained
  a `Version=`.
- `Enigma.Icons.slnx` has ten `<Project>` entries.

---

## PHASE02 — Layout math & Avalonia rasterizer

**Status:** DONE · Branch `feature/feature-4e1f-phase02-rasterizer`

### Steps

1. `Rendering/IconLayout.cs` — `PlateRect`, `GlyphTransform`, `GradientLine` as specified.
   `GlyphTransform` mirrors `Icon.Render`'s uniform-stretch maths against the centred
   `size * glyphScale` square; document the correspondence in the XML docs so the two cannot drift
   silently.
2. `Rendering/IIconRasterizer.cs` and `Rendering/AvaloniaIconRasterizer.cs` per *Design*. Build the
   plate brush from `PlateFill` (`SolidColorBrush`, or a `LinearGradientBrush` whose
   `StartPoint`/`EndPoint` come from `GradientLine`). Use
   `Save(stream, new PngBitmapEncoderOptions())` — **never** the obsolete `Save(Stream, int?)`.
3. Register `IIconRasterizer` to `AvaloniaIconRasterizer` as a singleton in `App`.
4. Tests:
   - `IconLayoutTests` (no platform): corner radius scaling, 0.5 producing a circle-equivalent
     radius, glyph transform centring for the 0 0 256 256 view box and for an offset view box,
     gradient line endpoints at 0/45/90/180/270/360°, angle wrap-around.
   - `AvaloniaIconRasterizerTests` (`[AvaloniaFact]`, headless plus Skia): PNG output starts with the
     PNG signature and decodes to the requested `PixelSize`; BGRA output has length `size*size*4`;
     a corner pixel of a 22 %-radius plate is fully transparent while the centre is opaque; a solid
     plate's mid-edge pixel matches `PrimaryColor`; a gradient plate's two opposite mid-edge pixels
     differ; the glyph colour is present in the rendered pixels.

### Acceptance criteria

- All cross-phase constraints hold.
- The rasterizer tests run green under `Avalonia.Headless` plus Skia — no `[Fact]` that silently
  skips.
- No `CS0618` anywhere (the obsolete `Save` overload is not used).
- `IIconRasterizer` has no `System.Drawing`, SkiaSharp or ImageSharp dependency.

---

## PHASE03 — ICO container writer & exporter

**Status:** DONE · Branch `feature/feature-4e1f-phase03-ico-export`

### Steps

1. `Export/IcoFrameFormat.cs`, `Export/IcoFrame.cs` (size plus format plus payload, validated), and
   `Export/IcoWriter.cs` with `PngFrameThreshold`, `UsesPngFrame(int)` and
   `Write(IReadOnlyList<IcoFrame>)` per *Design*. Reject an empty frame list, a size outside 1-256,
   a payload whose length does not match a DIB frame's expected pixel count, and duplicate sizes.
2. `Export/ExportRequest.cs`, `Export/ExportResult.cs`, `Export/IconExporter.cs` with the path
   validation described above. Register `IconExporter` in `App`.
3. Tests:
   - `IcoWriterTests`: ICONDIR fields; one ICONDIRENTRY per frame with correct
     `dwBytesInRes`/`dwImageOffset`; 256 encoded as `bWidth = bHeight = 0`; a DIB frame's
     `BITMAPINFOHEADER` (`biSize`, `biWidth`, `biHeight == 2*size`, `biBitCount == 32`,
     `biCompression == 0`) and total payload length including the padded AND mask; bottom-up row
     order verified with a synthetic two-row gradient; a PNG frame's payload byte-identical to the
     input; ascending order and de-duplication; every rejection path.
   - `IconExporterTests` (temp directory, in the style of the existing `TempDirectory` helpers):
     writes the expected file set and names them all in the result; overwrites an existing file;
     rejects an invalid or traversing base name; rejects a missing output directory; produces an
     `.ico` whose header parses back to the requested frame count.

### Acceptance criteria

- All cross-phase constraints hold.
- A generated `.ico` round-trips: re-reading its ICONDIR yields the requested sizes, and each
  `dwImageOffset`/`dwBytesInRes` pair addresses its payload exactly.
- Temporary directories created by the tests are removed even when a test fails.

---

## PHASE04 — Main window: catalog, design controls, live preview

**Status:** DONE · Branch `feature/feature-4e1f-phase04-design-ui`

### Steps

1. `MainWindowViewModel` — catalog (`PhosphorIconNames.All`, index == `PhosphorIcon` value, no
   `Enum.Parse`), debounced search, selected icon, weight, plate fill state, glyph colour, corner
   radius, glyph scale; a debounced preview regeneration that calls `IIconRasterizer.RenderPng` and
   exposes the 256 px preview plus the 64/48/32/16 thumbnails as `Bitmap`s.
2. `MainWindow.axaml` — the two-column layout from *Design*, with `x:DataType` on every binding root,
   the checkerboard `DrawingBrush`, the `ColorPicker`s, and the sliders. Search box focused on open,
   as the gallery does.
3. Resolve `MainWindow` from the host; no service is constructed in code-behind.
4. Preview rendering is wrapped so a failure paints nothing and reports in the status line rather
   than throwing at the dispatcher.

### Acceptance criteria

- All cross-phase constraints hold, **including reading the tool project's own warning count** for
  `AVLN*` diagnostics.
- Launching the app shows a live preview that updates when any design control changes, and the
  thumbnails track it.
- Selecting a different weight or icon repaints; searching filters the catalog and shows the
  empty-state message when nothing matches.
- No `[ObservableProperty]`/`[RelayCommand]` anywhere; the ViewModel is not `partial`.

---

## PHASE05 — Output panel & Generate command

**Status:** DONE · Branch `feature/feature-4e1f-phase05-export-ui`

### Steps

1. `Services/IFolderPicker.cs` plus `Services/AvaloniaFolderPicker.cs` — resolves the desktop
   lifetime's main window `StorageProvider` lazily per call (no constructor dependency, so the
   `MainWindow` to ViewModel to picker to `MainWindow` cycle never forms), returns `null` when
   cancelled or unavailable, and never throws.
2. A `SizeOption` item type (size plus `IsSelected`) and the two check-lists; selecting or clearing a
   size updates the Generate command's `CanExecute`.
3. Output folder box plus *Browse…* command, base-name box (default `app`), *Generate*
   `AsyncRelayCommand` that builds an `ExportRequest` and awaits `IconExporter.ExportAsync`, reports
   the written file count and the folder in the status line, and reports failures instead of
   throwing. `CanExecute` requires an icon, a non-empty base name, an existing folder, and at least
   one selected size; the command is non-reentrant while running.
4. Tests: `ExportRequestBuildingTests` over the ViewModel's request construction — selected sizes in
   ascending order, base name trimmed, ICO sizes capped at 256, empty selections excluded — plus
   `CanExecute` transitions. These need no rasterization.

### Acceptance criteria

- All cross-phase constraints hold.
- Running the app, choosing a folder, and pressing *Generate* writes `<base>.ico` and the selected
  `<base>-<size>.png` files, and the status line names the count and the folder.
- A generated `.ico` is accepted by `<ApplicationIcon>`: verified by pointing a throwaway project at
  it and building (the probe is not committed; the completion doc records the result).
- Cancelling the folder picker leaves the output folder unchanged and reports nothing alarming.

---

## PHASE06 — Documentation

**Status:** DONE · Branch `feature/feature-4e1f-phase06-docs`

### Steps

1. `docs/SPEC.md` — a new **§18 `tools/Enigma.Icons.AppIconStudio`** section covering purpose,
   TFM and packability, the composition model, the ICO frame policy and its threshold, the size
   sets, the output naming contract, the `Avalonia.Controls.ColorPicker` pin, and the test split.
   Update the §1 repository tree and add the two new slnx entries to §3.4's incremental-growth table
   with a note that they are the post-1.0 extension the section already anticipates.
2. `tools/Enigma.Icons.AppIconStudio/README.md` — what the tool is, how to run it, what it writes,
   and how to wire the output into an app (`<ApplicationIcon>`, `Window.Icon`, `AvaloniaResource`
   glob, and the About/Splash PNG), citing the `Enigma.MarkdownEditor` layout as the worked example.
3. Root `README.md` — add the tool to the repository-layout section only. **Do not** touch the
   badges, the what's-new callout, or any package section (SPEC §13.2).
4. `CLAUDE.md` — add the run command, the tool's line in the architecture block, and a one-line note
   that it is non-packable and that `dotnet pack` still applies to exactly three projects.

### Acceptance criteria

- All cross-phase constraints hold (this phase changes no code, so the build and suite are
  unchanged).
- Every path, command and size list quoted in the new docs matches the shipped code — checked by
  reading, not assumed.
- `RELEASENOTES.md`, `THIRD-PARTY-NOTICES.md`, `PackageReleaseNotes` and every `<Version>` are
  untouched.
- All new and edited files are LF with a final newline.
