# Enigma.Icons.AppIconStudio

A desktop tool that composes an application icon — a rounded plate with a Phosphor glyph on it — and
writes the assets an app actually consumes: one multi-frame `.ico` and a set of standalone `.png`
files.

It is **not a shipped package**. It lives in this repository the way the asset generator does: a
maintainer's utility, `IsPackable=false`, referenced by nothing.

```bash
dotnet run --project tools/Enigma.Icons.AppIconStudio
```

It needs a real desktop session (`DISPLAY` / `WAYLAND_DISPLAY` on Linux); a headless shell cannot show
its window.

## What you choose

| Control | Range | Default |
|---|---|---|
| Icon | any of the 1,512 Phosphor icons, searchable by name | `markdown-logo` |
| Weight | Thin · Light · Regular · Bold · Fill · Duotone | Fill |
| Glyph colour | any colour, including alpha | white |
| Glyph size | 20 %–100 % of the plate edge | 60 % |
| Plate fill | solid, or a two-stop linear gradient | solid |
| Plate colours | any two colours | `#3B72F0` and `#1E3A8A` |
| Gradient angle | 0°–360°, clockwise from left-to-right — 0° left to right, 90° top to bottom | 45° |
| Corner radius | 0 %–50 % of the edge; 0 % is a square, 50 % a circle | 22 % |

The glyph colour is a free choice on purpose: an icon on a pale plate needs a dark glyph, and forcing
white would make half the plate colours unusable.

**Reset**, at the top right of the design pane, puts every row of the table above back to its
*Default* — and, in the output panel below, the base name and the ticked sizes too. It also clears
the search box, so the restored icon is visible and highlighted again.

The one thing it keeps is the **output folder**. That is a session destination rather than a design
value: clearing it would grey out *Generate* and send you back through the folder dialog, which is an
undo that costs more than it saves.

The **preview is the export** — it comes out of the same renderer, at the size it will be written. The
64/48/32/16 strip beside it is where a too-generous glyph size stops being legible, which is worth
knowing before the file is written rather than after.

## What it writes

Pick a folder and a base name, tick the sizes, press **Generate**:

| File | Default sizes |
|---|---|
| `<base>.ico` | frames at 16, 24, 32, 48, 64, 128 and 256 px |
| `<base>-<size>.png` | 256, 512 and 1024 px (16, 32, 64 and 128 are also offered) |

Existing files are overwritten — it is a generator, and a run that refused to replace its own previous
output would be no use.

### Inside the `.ico`

Frames of 256 px are stored as PNG files, smaller ones as 32-bpp BMP bitmaps. That is the conventional
layout: it takes the seven-size icon from 372,526 bytes to 107,580 — measured — while leaving the
small sizes in the format the oldest rendering paths expect.

One consequence, measured rather than assumed: **GDI+ (`System.Drawing.Icon`) cannot decode a PNG
frame** and falls back to the largest BMP one, so that legacy Windows-only API sees 128 px as the
icon's top size. The Windows shell, WIC, Skia (and therefore Avalonia) and the .NET SDK's
`<ApplicationIcon>` all read the 256 px frame normally — and anything wanting a large bitmap should
use the standalone PNGs, which is what they are for.

## Wiring the output into an app

The file names match what an Avalonia desktop app consumes. With `app.ico` and `app-256.png` dropped
into `src/<App>.Desktop/Assets/`:

```xml
<!-- <App>.Desktop.csproj — embeds the icon in the Windows .exe; inert on Linux and macOS -->
<PropertyGroup>
  <ApplicationIcon>Assets/app.ico</ApplicationIcon>
</PropertyGroup>

<ItemGroup>
  <AvaloniaResource Include="Assets/**" />
</ItemGroup>
```

```xml
<!-- MainWindow.axaml — the runtime titlebar and taskbar icon, on every platform -->
<Window ... Icon="/Assets/app.ico">
```

```xml
<!-- AboutView.axaml or a splash window — the PNG, not the .ico:
     which frame a decoder picks out of an icon is unspecified. -->
<Image Source="/Assets/app-256.png" Width="96" Height="96" />
```

For a Linux `.desktop` entry, copy one of the PNGs (256 px is the usual choice) into the packaging
folder and point `Icon=` at where the installer puts it.

`Enigma.MarkdownEditor` is the worked example this layout is taken from: `Assets/app.ico` drives
`<ApplicationIcon>` and the window, `Assets/app-256.png` is what its About dialog and splash window
display, and `packaging/linux/*.png` is the same artwork again for the desktop entry.

## How it is built

```
Design/     IconDesign, PlateFill — the immutable description of one icon
Rendering/  IconLayout (pure geometry) and AvaloniaIconRasterizer (pixels)
Export/     IcoWriter (the .ico container, byte by byte) and IconExporter (the files)
Services/   IFolderPicker
```

The glyph is painted through `IconGlyphExtensions.ToDrawing` — the same conversion the shipped `Icon`
control uses — so duotone's two tones, per-layer opacity and stroked weights render here exactly as
they do on screen. The studio consumes the three packages through their public API, exactly as an
external consumer would.

`docs/SPEC.md` §18 is the full specification.
