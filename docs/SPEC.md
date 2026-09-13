# Enigma.Icons — Master Build Specification (SPEC)

> **Single source of truth** for building the `Enigma.Icons` solution from scratch.
> Every plan file in `docs/plan/` references sections of this document. This file plus the
> `docs/reference/` snapshots are **self-contained** — neither the retired
> `PhosphorIconsAvalonia` repository nor the upstream `PhosphorIcons` checkout is needed to
> build the solution.
>
> Authoritative because it encodes the validated `/interview` outcome. Where a plan and this
> SPEC disagree, **this SPEC wins** — fix the plan.

Date planned: 2026-07-25 · Author: Josué Clément · Planned via `/interview` v8 (Enigma.Icons).

---

## 0. Goal

Restructure the retired **`PhosphorIconsAvalonia`** package into three sibling NuGet packages under
an `Enigma.Icons` umbrella, plus a deferred WPF sibling:

| Package | TFMs | Purpose | v1.0.0 |
|---|---|---|---|
| `Enigma.Icons` | `netstandard2.0;net8.0;net10.0` | Framework-agnostic icon model, SVG parser, icon-set abstraction, caching, bring-your-own-SVG support. **Zero dependencies at all.** | yes |
| `Enigma.Icons.Phosphor` | `netstandard2.0;net8.0;net10.0` | The Phosphor artwork as an interchangeable asset pack: 6 embedded resources, the 1,512-member `PhosphorIcon` enum, `PhosphorWeight`, `PhosphorIconSet`. **Zero third-party dependencies** (declares one package dependency: the sibling `Enigma.Icons`). | yes |
| `Enigma.Icons.Avalonia` | `net8.0;net10.0` | Avalonia rendering: `Geometry`/`Drawing`/`DrawingImage` conversion, markup extensions, the `Icon` control. | yes |
| `Enigma.Icons.Wpf` | `net462;net8.0-windows;net10.0-windows` | WPF rendering. **Deferred, post-1.0 — no code in this plan.** | no |

This is **not** a one-to-one port. It is a re-architecture with the same goal:

- The parsing/model layer is extracted so Avalonia and a future WPF renderer share it.
- The Phosphor artwork becomes a *pack* behind an open abstraction, so another family (Lucide,
  Tabler, …) is a new sibling package rather than a fork, and so a user can point the same API at
  their own `.svg` files.
- Weight coverage grows from 5 to **6** (duotone is new), which forces a **layered** glyph model.
- The `ZiggyCreatures.FusionCache` dependency is dropped; the runtime dependency count of the base
  and Phosphor packages is **zero**.

### 0.1 What the retired package did (for context only — do not port literally)

`PhosphorIconsAvalonia` 1.2.0, `net8.0;net10.0`, nupkg 2.1 MB. A static `IconService` read one of
7,560 embedded `.svg` resources, pulled the first `<path>`'s `d` attribute out with
`XmlDocument`/`SelectSingleNode`, memoized it in FusionCache, and returned
`Geometry.Parse(d)` / a `DrawingImage`. Public surface: `Icon` (1,512 lowercase enum members),
`IconType` (`thin light regular bold fill`), `IconService`, `IconGeometryExtension`,
`IconSourceExtension`, `IPhosphorIconsAvaloniaMarker`.

**Clean break** — a decision of the interview. There is **no migration guide, no NuGet
deprecation, and no compatibility shim**. The new packages have new identities and new API names.

---

## 1. Repository structure (target)

```
Enigma.Icons/                                  git repo, default branch: main
├─ Enigma.Icons.slnx
├─ Directory.Build.props
├─ Directory.Packages.props
├─ global.json
├─ .gitignore                                  from docs/reference/config/gitignore
├─ .gitattributes                              from docs/reference/config/gitattributes (+1 line, §3.6)
├─ .editorconfig                               from docs/reference/config/editorconfig
├─ README.md                                   solution landing page
├─ LICENSE.md                                  MIT (yours) — §14.1
├─ RELEASENOTES.md
├─ THIRD-PARTY-NOTICES.md                      Phosphor MIT — §14.2
├─ CLAUDE.md
├─ src/
│   ├─ Enigma.Icons/                           packable
│   │   ├─ Enigma.Icons.csproj
│   │   ├─ README.md                           packed
│   │   ├─ IconViewBox.cs · IconLayer.cs · IconGlyph.cs
│   │   ├─ IIconSet.cs · SvgIconSet.cs
│   │   ├─ SvgIconParser.cs
│   │   ├─ IconNotFoundException.cs · SvgParseException.cs
│   │   └─ Internal/  (SvgShapeConverter, SvgTransformParser, IconNameNormalizer, …)
│   ├─ Enigma.Icons.Phosphor/                  packable
│   │   ├─ Enigma.Icons.Phosphor.csproj
│   │   ├─ README.md                           packed
│   │   ├─ PhosphorIcon.g.cs                   GENERATED · committed
│   │   ├─ PhosphorIconNames.g.cs              GENERATED · committed
│   │   ├─ PhosphorWeight.cs
│   │   ├─ PhosphorIconSet.cs
│   │   └─ Assets/phosphor.{thin,light,regular,bold,fill,duotone}.dat
│   │                                          GENERATED · committed · EmbeddedResource
│   └─ Enigma.Icons.Avalonia/                  packable
│       ├─ Enigma.Icons.Avalonia.csproj
│       ├─ README.md                           packed
│       ├─ IconGlyphExtensions.cs
│       ├─ Icon.cs
│       ├─ Properties/AssemblyInfo.cs        XmlnsDefinition attributes — §10.3
│       └─ Markup/IconGeometryExtension.cs · Markup/IconImageExtension.cs
├─ tests/
│   ├─ Enigma.Icons.UnitTests/                 net10.0 · xunit.v3
│   ├─ Enigma.Icons.Phosphor.UnitTests/        net10.0 · xunit.v3
│   ├─ Enigma.Icons.Avalonia.UnitTests/        net10.0 · xunit.v3 + Avalonia.Headless.XUnit
│   └─ Enigma.Icons.AppIconStudio.UnitTests/   net10.0 · xunit.v3 + Avalonia.Headless.XUnit  (§18)
├─ samples/
│   └─ Enigma.Icons.Avalonia.Gallery/          net10.0 · NOT packable
├─ tools/
│   ├─ Enigma.Icons.Generator/                 net10.0 · NOT packable
│   └─ Enigma.Icons.AppIconStudio/             net10.0 · NOT packable  (§18, post-1.0)
│       ├─ Enigma.Icons.AppIconStudio.csproj · README.md · app.manifest · .editorconfig
│       ├─ Program.cs · App.axaml(.cs) · MainWindow.axaml(.cs) · MainWindowViewModel.cs
│       ├─ IconEntry.cs · IconThumbnail.cs · WeightOption.cs · FillModeOption.cs · SizeOption.cs
│       ├─ Design/     PlateFillMode.cs · PlateFill.cs · IconDesign.cs
│       ├─ Rendering/  IconLayout.cs · IIconRasterizer.cs · AvaloniaIconRasterizer.cs
│       ├─ Export/     IcoFrameFormat.cs · IcoFrame.cs · IcoWriter.cs ·
│       │              ExportRequest.cs · ExportResult.cs · IconExporter.cs
│       └─ Services/   IFolderPicker.cs · AvaloniaFolderPicker.cs
└─ docs/
    ├─ roadmap.md · SPEC.md · RELEASE.md
    ├─ plan/<ID>.md · done/<ID>.md
    ├─ img/gallery.png                       gallery screenshot — §11, §13
    └─ reference/
        ├─ config/{gitignore,gitattributes,editorconfig}
        ├─ release/RELEASE.template.md
        └─ phosphor/{phosphor-2.1.1-svgs-flat.tar.gz, LICENSE, README.md}
```

**`docs/img/gallery.png` is the single canonical screenshot path.** FEATURE-469B captures it;
FEATURE-718F embeds it in the root README. No other path is permitted — a plan proposing one is
wrong.

**`docs/reference/release/RELEASE.template.md`** is a snapshot of the `dotnet-release` skill's
bundled `templates/RELEASE.md`, carried here so FEATURE-74DC can write `docs/RELEASE.md` without
the skill being available. It is reference material, not the runbook itself.

`samples/` and `tools/` extend the `src`/`tests`/`docs` layout: they hold the icon gallery and the
asset generator respectively. Neither is packable; neither is referenced by any packable project.

`docs/` is a planning aid. It is **never** packed into any NuGet package.

---

## 2. Global rules & standards

These apply to every project and every file in the solution. They come from the house convention
skills (`dev-workflow`, `dotnet-solution-setup`, `dotnet-solution-config`, `xunit-v3`,
`dotnet-release`) and from the validated interview.

§2 has **no subsections**: its numbered rules below are the citable units, and elsewhere in this
document and in the plan files they are referenced interchangeably as **`§2.N`** or **"§2 rule N"** —
e.g. `§2.8` and "§2 rule 8" both mean the XML-documentation rule. A `§2.N` reference is a rule number,
never a subsection.

1. **Zero-warning builds.** `TreatWarningsAsErrors=true` and `EnforceCodeStyleInBuild=true` for the
   whole solution, libraries and tests alike. A warning is a build failure.
2. **`ImplicitUsings` is disabled everywhere.** Every file declares its own `using` directives.
3. **`Nullable` enable** everywhere. No `!` null-forgiveness without a commented justification.
4. **`LangVersion 14`.**
5. **Central Package Management.** Versions live only in `Directory.Packages.props`; a
   `<PackageReference>` never carries `Version=`.
6. **`.slnx`**, never `.sln`.
7. **Every text file is LF with a final newline.** `.gitattributes` (`* text=auto eol=lf`) and
   `.editorconfig` (`end_of_line = lf`, `insert_final_newline = true`) enforce this from the first
   commit. Generated files included.
8. **XML documentation comments on every public member.** `GenerateDocumentationFile=true` plus
   `TreatWarningsAsErrors` makes CS1591 a build **error** — this includes all 1,512 generated enum
   members (see §8.4).
9. **Never commit.** Per `dev-workflow`, the user owns all commits, tags, and pushes. A build stages
   the tree and prints a suggested commit message.
10. **Zero third-party runtime dependencies** for `Enigma.Icons` and `Enigma.Icons.Phosphor`.
    `Enigma.Icons` has no dependencies at all; `Enigma.Icons.Phosphor` declares exactly one — the
    sibling `Enigma.Icons` — and nothing else on any TFM. Dependencies *within* the
    `Enigma.Icons` family are not third-party and do not breach this rule. This is a hard
    constraint, not a preference — it is why FusionCache was dropped and why the libraries do no
    logging. `Enigma.Icons.Avalonia` depends on `Avalonia` and `Enigma.Icons.Phosphor` only.
11. **No `Enum.ToString()` / `Enum.Parse` on hot paths.** Generated lookup tables instead (§8.4) —
    faster and trim/AOT-clean.

---

## 3. Root configuration files

### 3.1 `global.json` (verbatim)

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

### 3.2 `Directory.Build.props` (verbatim)

```xml
<Project>

  <!-- Shared settings for every project in the solution. Package versions are
       centralized separately in Directory.Packages.props (Central Package Management). -->
  <PropertyGroup>
    <Authors>Josué Clément</Authors>
    <Copyright>Copyright © 2026 Josué Clément</Copyright>
    <LangVersion>14</LangVersion>
    <Nullable>enable</Nullable>
    <!-- Warnings are treated as errors across the whole solution (libraries + tests). -->
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <!-- Enforce the .editorconfig code-style rules (the warning-level subset) at build time. -->
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>

</Project>
```

Do not add any property not listed. In particular `ImplicitUsings` is set **per project** (always
`disable`) so the value is visible in each csproj.

### 3.3 `Directory.Packages.props` (verbatim)

```xml
<Project>

  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <!-- Avalonia ecosystem — version-coupled, ALWAYS bumped together, never individually.
       Version aligned with /home/jo/Dev/Draw, the house's current Avalonia 12 project.
       AvaloniaUI.DiagnosticsSupport is versioned independently of Avalonia but belongs to the
       same coupled set (dotnet-release). -->
  <ItemGroup>
    <PackageVersion Include="Avalonia" Version="12.1.0" />
    <PackageVersion Include="Avalonia.Desktop" Version="12.1.0" />
    <PackageVersion Include="Avalonia.Themes.Fluent" Version="12.1.0" />
    <PackageVersion Include="Avalonia.Fonts.Inter" Version="12.1.0" />
    <PackageVersion Include="Avalonia.Headless" Version="12.1.0" />
    <PackageVersion Include="Avalonia.Headless.XUnit" Version="12.1.0" />
    <PackageVersion Include="AvaloniaUI.DiagnosticsSupport" Version="2.2.3" />
  </ItemGroup>

  <!-- Gallery sample -->
  <ItemGroup>
    <PackageVersion Include="CommunityToolkit.Mvvm" Version="8.4.2" />
    <PackageVersion Include="Microsoft.Extensions.Hosting" Version="10.0.10" />
  </ItemGroup>

  <!-- Tests -->
  <ItemGroup>
    <PackageVersion Include="xunit.v3" Version="3.2.2" />
  </ItemGroup>

</Project>
```

> **Verify at restore time, do not assume.** The block above records the pins as of the **1.0.0
> release** (FEATURE-74DC), which moved the coupled Avalonia set 12.0.4 → 12.1.0,
> `AvaloniaUI.DiagnosticsSupport` 2.2.1 → 2.2.3, and `Microsoft.Extensions.Hosting` 10.0.8 → 10.0.10.
> All were verified by a green Release build and the full 434-test suite at that version.
>
> - The Avalonia set **moves as a unit** — never mix versions within it. `Avalonia` is the only id of
>   the group that ships in a package, so bumping it raises the published dependency floor for
>   `Enigma.Icons.Avalonia` consumers: treat it as a compatibility decision, not housekeeping.
> - `AvaloniaUI.DiagnosticsSupport` belongs to the coupled set but is versioned on its **own** line —
>   it can never share the Avalonia version number.
> - `Avalonia.Headless.XUnit` 12.1.0's nuspec declares a dependency on
>   `xunit.v3.extensibility.core` **3.2.2** on both `net8.0` and `net10.0` — Avalonia 12's headless
>   test package is xUnit **v3**-native, so there is no v2/v3 conflict. **Re-verify this after any
>   Avalonia bump**; a v2-based headless package would break the whole Avalonia test project.
> - **There is no `Avalonia.Diagnostics` for Avalonia 12** — its newest version is 11.3.12. The
>   Avalonia 12 replacement is `AvaloniaUI.DiagnosticsSupport`. Do not reintroduce the old id.
> - `/home/jo/Dev/Draw` remains on Avalonia 12.0.4 / DiagnosticsSupport 2.2.1. It is a house
>   reference point, **not** a constraint — this solution is deliberately ahead of it as of 1.0.0.
> - `CommunityToolkit.Mvvm` 8.4.2 and `xunit.v3` 3.2.2 were already current at the 1.0.0 release.
>
> **A known house inconsistency, resolved here:** `Draw` uses the `xunit.v3.mtp-v2` 3.2.2
> meta-package, while `Enigma.Logging` and the `xunit-v3` skill use `xunit.v3` 3.2.2. This solution
> follows the **Enigma family and the skill — `xunit.v3`**. Do not switch to `mtp-v2` mid-build.
>
> `Enigma.Icons` and `Enigma.Icons.Phosphor` reference **nothing**. Do not add a central pin for
> `System.Memory`, `Microsoft.Bcl.*`, or a polyfill package unless a concrete compile error under
> `netstandard2.0` demands it — and if one does, record the reason in a csproj comment.

### 3.4 `Enigma.Icons.slnx` — end state

```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/Enigma.Icons/Enigma.Icons.csproj" />
    <Project Path="src/Enigma.Icons.Phosphor/Enigma.Icons.Phosphor.csproj" />
    <Project Path="src/Enigma.Icons.Avalonia/Enigma.Icons.Avalonia.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/Enigma.Icons.UnitTests/Enigma.Icons.UnitTests.csproj" />
    <Project Path="tests/Enigma.Icons.Phosphor.UnitTests/Enigma.Icons.Phosphor.UnitTests.csproj" />
    <Project Path="tests/Enigma.Icons.Avalonia.UnitTests/Enigma.Icons.Avalonia.UnitTests.csproj" />
  </Folder>
  <Folder Name="/samples/">
    <Project Path="samples/Enigma.Icons.Avalonia.Gallery/Enigma.Icons.Avalonia.Gallery.csproj" />
  </Folder>
  <Folder Name="/tools/">
    <Project Path="tools/Enigma.Icons.Generator/Enigma.Icons.Generator.csproj" />
  </Folder>
</Solution>
```

The 1.0.0 end state has **eight** `<Project>` entries — 3 `src` + 3 `tests` + 1 `samples` +
1 `tools`. Use that number; do not recount. Post-1.0, `FEATURE-4E1F` appended two more (see the
table below), so the committed file now holds **ten**.

**Incremental-growth contract.** The slnx must never reference a project that does not exist yet,
so it must stay buildable at every step. Each work item appends only its own entries:

| Work item | Appends |
|---|---|
| FEATURE-21C4 | the four empty `<Folder>` elements, **zero** `<Project>` entries |
| FEATURE-24DD | `src/Enigma.Icons`, `tests/Enigma.Icons.UnitTests` |
| FEATURE-2DDE | `tools/Enigma.Icons.Generator` |
| FEATURE-3950 | `src/Enigma.Icons.Phosphor`, `tests/Enigma.Icons.Phosphor.UnitTests` |
| FEATURE-3ADD | `src/Enigma.Icons.Avalonia`, `tests/Enigma.Icons.Avalonia.UnitTests` |
| FEATURE-469B | `samples/Enigma.Icons.Avalonia.Gallery` → reaches the §3.4 end state (8 entries) |
| FEATURE-718F, FEATURE-74DC | nothing |
| FEATURE-4E1F (post-1.0) | `tools/Enigma.Icons.AppIconStudio`, `tests/Enigma.Icons.AppIconStudio.UnitTests` → 10 entries |

The eight-entry end state is final **for the 1.0.0 line**. Post-1.0 growth is expected and is not a
contradiction of "end state": `FEATURE-4E1F` has already added the two non-packable app-icon-studio
entries above, and the deferred `FEATURE-6FA1` (WPF) would add `src/Enigma.Icons.Wpf` and
`tests/Enigma.Icons.Wpf.UnitTests`.

### 3.5 `.gitignore` / `.editorconfig`

Copy **byte-identical** from the snapshots:

- `docs/reference/config/gitignore` → `.gitignore` (6,238 bytes)
- `docs/reference/config/editorconfig` → `.editorconfig` (9,465 bytes)

Verify with `cmp` (exit 0).

> **Byte identity wins over §2 rule 7 for these three files.** Measured: `gitattributes` (653 B) and
> `editorconfig` (9,465 B) both end with `0x0A`, but **`gitignore` (6,238 B) ends with `0x64` (`d`) —
> it has no final newline.** Copy it as-is; do **not** "fix" it to satisfy the final-newline rule, and
> do not treat the `cmp` result as a defect. §2 rule 7 governs files this solution authors.

> **`.gitignore` excludes `docs/reference/release/` — the template is tracked only by a force-add.**
> Line 22 of the snapshot carries the stock Visual Studio build-output rule `[Rr]elease/`, which
> matches the **`docs/reference/release/`** directory of §1. Left alone, git silently drops
> `docs/reference/release/RELEASE.template.md` from every commit — breaking the guarantee that
> `docs/` is self-contained, and taking with it the file **FEATURE-74DC** needs to write
> `docs/RELEASE.md` without the `dotnet-release` skill. A nested `.gitignore` negation **cannot**
> fix this: git never descends into an excluded directory. Since byte identity governs `.gitignore`,
> the resolution is a one-time force-add, performed with FEATURE-21C4's bootstrap commit:
>
> ```bash
> git add -f docs/reference/release/RELEASE.template.md
> ```
>
> gitignore applies only to *untracked* files, so once the file is tracked it behaves normally and
> needs no further special handling. **FEATURE-74DC must confirm the file is present before relying
> on it.**

These snapshots are byte-identical copies of `/home/jo/Dev/Enigma.Core`'s root files — the house standard, chosen in the interview. The `/home/jo/Dev/resources` directory
named in the original brief does not exist; these snapshots replace it.

`.editorconfig` already covers this solution's file types, including
`[*.{xml,axaml,xaml}] indent_size = 2`. Its naming rules put public constants (which is what enum
members are, to Roslyn) at **`suggestion`** severity, so they do not fail the build — but the
generated enum is PascalCase anyway (§8.4), so the question does not arise.

### 3.6 `.gitattributes`

Copy `docs/reference/config/gitattributes` → `.gitattributes` (653 bytes), then **append one line**
to the binary block:

```gitattributes
*.tar.gz binary
```

Rationale: `* text=auto` already detects the Phosphor snapshot archive as binary, but the house file
lists `*.zip binary` explicitly and the archive is the only binary asset this repo tracks — being
explicit matches the file's own stated intent ("Belt-and-suspenders: force known binaries so git
never converts them"). This is the **only** permitted deviation from a byte-identical copy; record
it in the completion doc. If the deviation is unwanted, drop the line — behaviour is unchanged.

**Line endings are never a work item.** Per `dev-workflow`, CRLF/LF normalization is
recommendation-only. `.gitattributes` carries `* text=auto eol=lf` from the first commit, so there
is nothing to normalize and no action to take.

---

## 4. Icon data model — `Enigma.Icons`

Namespace `Enigma.Icons`. All types immutable and thread-safe.

### 4.1 `IconViewBox`

```csharp
public readonly struct IconViewBox : IEquatable<IconViewBox>
{
    public IconViewBox(double x, double y, double width, double height);
    public double X { get; }
    public double Y { get; }
    public double Width { get; }
    public double Height { get; }

    /// <summary>The 0 0 256 256 view box used by every Phosphor icon.</summary>
    public static IconViewBox Default { get; }      // 0, 0, 256, 256
}
```

`Width`/`Height` must be > 0; the constructor throws `ArgumentOutOfRangeException` otherwise.
Renderers use the view box to scale a glyph into the target bounds.

### 4.2 `IconLayer`

One paintable element of a glyph. Duotone's tinted backing shape and its foreground shape are two
layers; a stroked user SVG contributes layers carrying stroke metadata.

```csharp
public sealed class IconLayer
{
    public IconLayer(
        string pathData,
        double opacity = 1.0,
        IconFillRule fillRule = IconFillRule.NonZero,
        string? fill = null,
        string? stroke = null,
        double? strokeWidth = null,
        IconLineCap? strokeLineCap = null,
        IconLineJoin? strokeLineJoin = null);

    /// <summary>SVG path mini-language geometry. Never null, never empty.</summary>
    public string PathData { get; }
    /// <summary>0.0–1.0. 1.0 for a fully opaque layer.</summary>
    public double Opacity { get; }
    public IconFillRule FillRule { get; }
    /// <summary>Raw SVG paint value, or null when the layer inherits the renderer's brush.
    /// "none" means "do not fill". "currentColor" is normalized to null.</summary>
    public string? Fill { get; }
    public string? Stroke { get; }
    public double? StrokeWidth { get; }
    public IconLineCap? StrokeLineCap { get; }
    public IconLineJoin? StrokeLineJoin { get; }

    /// <summary>True when this layer should be stroked rather than (or as well as) filled.</summary>
    public bool IsStroked { get; }        // Stroke is not null && Stroke != "none"
    /// <summary>True when this layer should be filled.</summary>
    public bool IsFilled { get; }         // Fill != "none"
}

public enum IconFillRule { NonZero, EvenOdd }
public enum IconLineCap  { Flat, Round, Square }
public enum IconLineJoin { Miter, Round, Bevel }
```

`pathData` null/empty/whitespace throws `ArgumentException`; `opacity` outside `[0,1]` throws
`ArgumentOutOfRangeException`.

**Why paint values are raw strings, not a colour type.** `Enigma.Icons` has zero dependencies and
no framework colour type. Each renderer interprets `Fill`/`Stroke` in its own terms and, in the
normal icon case, ignores them entirely in favour of the consumer's brush. `"currentColor"` is
normalized to `null` by the parser precisely so "inherit the brush" is the default.

### 4.3 `IconGlyph`

```csharp
public sealed class IconGlyph
{
    public IconGlyph(IconViewBox viewBox, IReadOnlyList<IconLayer> layers);

    public IconViewBox ViewBox { get; }
    /// <summary>Paint order: index 0 is painted first (bottom-most). Never empty.</summary>
    public IReadOnlyList<IconLayer> Layers { get; }
    /// <summary>True when the glyph has exactly one fully-opaque, unstroked layer —
    /// the common case, and the one a renderer can collapse to a single filled geometry.</summary>
    public bool IsSingleLayer { get; }
}
```

`layers` null throws `ArgumentNullException`; empty throws `ArgumentException`. The layer list is
defensively copied into a read-only snapshot.

---

## 5. SVG parser — `SvgIconParser`

```csharp
public static class SvgIconParser
{
    /// <summary>Parses an SVG document into a glyph.</summary>
    /// <exception cref="SvgParseException">The document is malformed, or contains no
    /// paintable element.</exception>
    public static IconGlyph Parse(string svg);

    /// <summary>Parses an SVG document from a stream. The stream is read to the end but
    /// not disposed.</summary>
    public static IconGlyph Parse(Stream stream);

    /// <summary>Maximum accepted document size in bytes. Default 1 MiB.</summary>
    public static int MaxDocumentBytes { get; set; }
}
```

### 5.1 Supported subset (decided in the interview: "shapes + groups + transforms")

| SVG construct | Handling |
|---|---|
| `<path d="…">` | `d` taken verbatim as `IconLayer.PathData`. |
| `<rect x y width height rx ry>` | Converted to path data. Rounded corners via `rx`/`ry` (a single supplied radius mirrors to the other axis, per SVG rules). |
| `<circle cx cy r>` | Converted to path data (two arcs). |
| `<ellipse cx cy rx ry>` | Converted to path data (two arcs). |
| `<line x1 y1 x2 y2>` | Converted to `M x1,y1 L x2,y2`. |
| `<polyline points>` | Converted to `M … L …` (open). |
| `<polygon points>` | Converted to `M … L … Z` (closed). |
| `<g>` | Flattened. Its `transform`, `opacity`, `fill`, `stroke`, `fill-rule`, and stroke properties **inherit** to descendants; a child's own value wins. Nesting to any depth. |
| `transform="…"` | `translate`, `scale`, `rotate` (with and without a centre), `matrix`, `skewX`, `skewY`, and whitespace/comma-separated lists thereof. Composed with any inherited transform and **baked into the emitted path data** — an `IconLayer` never carries a transform. |
| `opacity` | Multiplied down the inheritance chain into `IconLayer.Opacity`. |
| `fill`, `fill-rule` | Captured. `"currentColor"` → `null`. `fill-rule="evenodd"` → `IconFillRule.EvenOdd`. |
| `stroke`, `stroke-width`, `stroke-linecap`, `stroke-linejoin` | Captured on the layer. |
| `viewBox` | → `IconGlyph.ViewBox`. Absent → derive from `width`/`height` if present, else `IconViewBox.Default` (0 0 256 256). |
| `<rect>` covering the whole view box with `fill="none"` | Kept as a normal layer (it is a legitimate transparent spacer). Renderers skip non-filled, non-stroked layers. |

**Explicitly out of scope**, and documented as such in the `Enigma.Icons` README: gradients
(`linearGradient`, `radialGradient`), `clipPath`, `mask`, `<defs>`/`<use>`/`<symbol>`, `style="…"`
attributes and `<style>` CSS blocks, `<text>`, `<image>`, filters, animation. A document whose only
paintable content sits inside an unsupported construct raises `SvgParseException` with a message
naming the construct. The README points users at `Avalonia.Svg` / `Svg.Skia` for arbitrary artwork —
this library is about icons.

### 5.2 XML hardening (mandatory)

User-supplied SVG is untrusted XML. The parser **must** read through `XmlReader` configured as:

```csharp
var settings = new XmlReaderSettings
{
    DtdProcessing = DtdProcessing.Prohibit,   // XXE + external entity expansion
    XmlResolver = null,                       // no external resource resolution
    IgnoreComments = true,
    IgnoreProcessingInstructions = true,
    IgnoreWhitespace = true,
    CloseInput = false,
};
```

- `DtdProcessing.Prohibit` blocks XXE and the "billion laughs" entity-expansion attack. This matters
  on `netstandard2.0`/.NET Framework, where `XmlDocument`'s defaults are *not* safe — the retired
  package's `XmlDocument.LoadXml` was safe only because its input was always its own embedded
  resources.
- Input larger than `MaxDocumentBytes` (default 1 MiB) raises `SvgParseException` before parsing.
- The SVG namespace is honoured but not required: elements are matched on local name, so a document
  without `xmlns="http://www.w3.org/2000/svg"` still parses. (Two upstream Phosphor files also lack
  the root `fill` attribute — see §7.4 — so no root attribute may be treated as mandatory.)

### 5.3 Exceptions

```csharp
public sealed class SvgParseException : Exception
{
    public SvgParseException(string message);
    public SvgParseException(string message, Exception innerException);
}

public sealed class IconNotFoundException : Exception
{
    public IconNotFoundException(string iconName, string? variant, string setName);
    public string IconName { get; }
    public string? Variant { get; }
    public string SetName { get; }
}
```

`IconNotFoundException`'s message names all three parts, e.g.
`Icon 'acorn' (variant 'bold') was not found in icon set 'Phosphor'.`

---

## 6. Icon-set abstraction — `IIconSet` and `SvgIconSet`

### 6.1 `IIconSet`

```csharp
public interface IIconSet
{
    /// <summary>Display name of the set, used in exception messages. E.g. "Phosphor".</summary>
    string Name { get; }

    /// <summary>The variant names this set understands, lower-case. Empty for a set with
    /// a single style.</summary>
    IReadOnlyList<string> Variants { get; }

    /// <summary>The variant used when the caller passes null. Null when the set has no
    /// variants.</summary>
    string? DefaultVariant { get; }

    /// <summary>Every icon name in the set, lower-case kebab-case, ordered.</summary>
    IEnumerable<string> IconNames { get; }

    /// <summary>Non-throwing lookup.</summary>
    bool TryGetGlyph(string icon, string? variant, out IconGlyph? glyph);

    /// <summary>Throwing lookup.</summary>
    /// <exception cref="IconNotFoundException"/>
    IconGlyph GetGlyph(string icon, string? variant = null);
}
```

Contract for implementers:

- Name lookup is **case-insensitive** (`OrdinalIgnoreCase`) and accepts kebab-case,
  `snake_case`, or PascalCase input — normalized internally to kebab-case. Same for variants.
- `variant == null` means `DefaultVariant`. A variant the set does not have is a miss, **never** a
  silent fallback to the default — a caller asking for `bold` must not get `regular` back.
- Repeated calls for the same `(icon, variant)` return a **cached, reference-equal** `IconGlyph`.
- All members are safe for concurrent use from any thread.

**What "`TryGetGlyph` never throws" means — precedence, settled here.** The `Try` prefix governs
*absence*, not *corruption*. Both lookup methods therefore behave as follows, and this rule overrides
any looser phrasing elsewhere in this document:

| Situation | `TryGetGlyph` | `GetGlyph` |
|---|---|---|
| The icon or variant is not in the set — an ordinary **miss** | returns `false`, `glyph` is `null` | throws `IconNotFoundException` |
| The source is present but **broken** — malformed SVG, unreadable file, missing embedded resource stream, `.dat` header/count mismatch | **throws** (`SvgParseException` / `InvalidDataException` / the underlying `IOException`) | throws the same |

Rationale: a miss is a normal, expected outcome a caller should branch on; a broken source is a defect
in the package or in the user's own asset folder, and silently reporting it as "icon not found" would
turn a fixable bug into an invisible blank space. `TryGetGlyph` is not a general-purpose exception
swallow.

`GetGlyph` may be provided once as a default interface member only on the modern TFMs; because
`netstandard2.0` is in the TFM set, implement it explicitly in each set instead (no default
interface members).

### 6.2 `SvgIconSet` — bring your own icons

```csharp
public sealed class SvgIconSet : IIconSet
{
    /// <summary>Icons from a directory of .svg files.</summary>
    /// <param name="variantsFromSubfolders">When true (default) each immediate subdirectory is a
    /// variant and its .svg files are its icons; when false the directory's own .svg files are
    /// the icons and the set has no variants.</param>
    public static SvgIconSet FromDirectory(
        string path,
        bool variantsFromSubfolders = true,
        string? defaultVariant = null,
        string? name = null);

    /// <summary>Icons from an assembly's embedded resources whose names start with
    /// <paramref name="resourcePrefix"/>.</summary>
    public static SvgIconSet FromAssembly(
        Assembly assembly,
        string resourcePrefix,
        string? defaultVariant = null,
        string? name = null);

    /// <summary>Icons from an explicit list of .svg file paths.</summary>
    public static SvgIconSet FromFiles(
        IEnumerable<string> files,
        string? name = null);

    /// <summary>Icons from name/SVG-text pairs — the in-memory escape hatch.</summary>
    public static SvgIconSet FromSvgSources(
        IEnumerable<KeyValuePair<string, string>> sources,
        string? name = null);
}
```

Behaviour:

- Discovery is **eager** (names are enumerated at construction so `IconNames` is complete and
  `TryGetGlyph` is a dictionary miss, not an I/O probe); **parsing is lazy** and cached per glyph.
- **Filename → icon name:** the extension is dropped and the name normalized to kebab-case. When
  `variantsFromSubfolders` is true and a file's name ends with `-<variant>`, that suffix is stripped
  — so `bold/acorn-bold.svg` and `bold/acorn.svg` both yield the icon `acorn` in variant `bold`.
  This is what makes an extracted Phosphor tree work with `FromDirectory` out of the box.
- **Path safety:** `FromDirectory` resolves the root with `Path.GetFullPath`, enumerates only within
  it, skips symlinked variant subdirectories **and** symlinked `.svg` files (`Path.GetFullPath` does
  not resolve a symlink, so a link inside the root would otherwise be read), and rejects any resolved
  file path that does not start with the resolved root — no traversal via a crafted subdirectory name.
  A skipped entry is dropped silently, so one hostile entry cannot deny service on an otherwise valid
  directory.
- A directory that does not exist throws `DirectoryNotFoundException` at construction. A file that
  disappears between construction and first parse surfaces as `SvgParseException` wrapping the I/O
  error.
- `name` defaults to the directory or assembly name.

---

## 7. Embedded asset resource format

### 7.1 File set

Six resources, one per weight, embedded in `Enigma.Icons.Phosphor`:

```
Enigma.Icons.Phosphor.Assets.phosphor.thin.dat
Enigma.Icons.Phosphor.Assets.phosphor.light.dat
Enigma.Icons.Phosphor.Assets.phosphor.regular.dat
Enigma.Icons.Phosphor.Assets.phosphor.bold.dat
Enigma.Icons.Phosphor.Assets.phosphor.fill.dat
Enigma.Icons.Phosphor.Assets.phosphor.duotone.dat
```

Declared as `<EmbeddedResource Include="Assets\phosphor.*.dat" />`. The manifest resource name is
built from a **compile-time literal** format string plus the weight's table entry — never from
reflection over the enum — so trimming cannot break it.

### 7.2 Format (`v1`)

UTF-8, **no BOM**, LF line endings, final newline. Line 1 is the header; every following line is one
icon. Fields are separated by a single TAB (`U+0009`).

```
v1<TAB>regular<TAB>0 0 256 256<TAB>1512
acorn<TAB>M232,104a56.06,56.06,0,0,0-56-56H136a24,…Z
address-book<TAB>M216,40H40A16,16,0,0,0,24,56V200…Z
…
```

- **Header:** `v1`, weight name, view box as four space-separated numbers, icon count. A reader that
  does not recognize the version, or whose line count disagrees with the header count, throws
  `InvalidDataException` — this is the tripwire for a corrupted or half-written resource.
- **Icon line:** `name` then one field per layer, in paint order (first field = bottom-most layer).
- **Layer field:** the path data, optionally prefixed with `@<opacity>:` when opacity ≠ 1.
  The prefix uses the invariant culture and the shortest round-trippable form (`@0.2:`).
- Lines are sorted by `name` using `StringComparer.Ordinal`, giving a stable diff across
  regenerations.
- Icon names are lower-case kebab-case, exactly as upstream.

Worked examples:

```
acorn<TAB>M232,104a56.06,…Z                        # single layer (thin/light/regular/bold, most fill)
bookmarks-simple<TAB>M160,…160,56Z<TAB>M192,…192,24Z   # 2-layer fill icon (real values)
acorn<TAB>@0.2:M216,112v16c0,53-88,88-88,112,…Z<TAB>M232,104a56.06,…Z   # duotone
cell-signal-none<TAB>M…Z                           # duotone with only 1 layer (see §7.4)
```

### 7.3 Size budget (measured, not estimated)

Produced by simulating the §7.2 format over the pinned snapshot. These are **whole-file** sizes
including the header line, icon names, TABs, `@0.2:` prefixes and newlines — use them as the
acceptance target, not the path-data subtotals:

| weight | `.dat` whole file | of which path data |
|---|---|---|
| thin | 678,696 B (662.8 KiB) | 658,481 B (643.0 KiB) |
| light | 678,649 B (662.7 KiB) | 658,433 B (643.0 KiB) |
| regular | 632,699 B (617.9 KiB) | 612,481 B (598.1 KiB) |
| bold | 629,731 B (615.0 KiB) | 609,516 B (595.2 KiB) |
| fill | 553,221 B (540.3 KiB) | 532,996 B (520.5 KiB) |
| duotone | 778,298 B (760.1 KiB) | 749,020 B (731.5 KiB) |
| **total** | **3,951,294 B (3.77 MiB)** | **3,820,927 B (3.64 MiB)** |

The **whole-file** column is the acceptance target: a deviation beyond ±2 % on any weight means the
format or the data is wrong — investigate, do not accept. The path-data column is informational (the
difference is names, TABs, `@0.2:` prefixes, the header line and newlines).

#### Why this format was chosen — and what it does *not* buy

**It does not save package size. That earlier claim was wrong; these are the measured numbers.**

Embedded resources live *inside the assembly*, and the assembly is a **single** `.nupkg` entry — so
the whole resource blob is deflated as one stream. Per-entry deflate never applies to the individual
icons, and the comparison is therefore corpus-vs-corpus:

| | raw content | deflated as one stream |
|---|---|---|
| 9,072 `.svg` files' content | 4,767,467 B (4.55 MiB) | 1,118,874 B (1.07 MiB) |
| 6 `.dat` resources | 3,951,294 B (3.77 MiB) | 1,150,587 B (1.10 MiB) |
| difference | 17.1 % **smaller** | 2.8 % **larger** |

Stripping the per-file `<svg xmlns=… viewBox=… fill=…>` wrapper removes 17 % of the raw bytes, but
that wrapper repeats 9,072 times and deflates to almost nothing — so removing it also removes
compressible redundancy. Compressed, the two formats are a **wash** (`.dat` is marginally worse).

**Per-TFM duplication dominates the package size, not the format.** A multi-targeted package ships
one assembly per TFM, each carrying a full copy of the resources. Proof from the retired package,
still in the local NuGet cache: `PhosphorIconsAvalonia` 1.2.0 contains the *same* 4,459,520 B
assembly twice — `lib/net8.0/` and `lib/net10.0/` — which is why its nupkg is 2,121,835 B for five
weights. At the §0 TFM set (`netstandard2.0;net8.0;net10.0`), `Enigma.Icons.Phosphor`'s nupkg will be
roughly **3 × 1.15 MB ≈ 3.4–3.5 MB**. It will be *larger* than the retired package, not smaller. Say
so in the release notes; do not publish a size-reduction claim.

> **Do not repeat the "36 MB" figure** for the raw corpus. `du -sh` reports 36 MB, but that is
> filesystem block allocation (9,072 files × 4 KiB blocks), not data. The correct raw figure is
> **4.55 MiB** (4,767,467 B).

The reasons the format *is* correct are all non-size, and all still hold:

1. **6 manifest resources instead of 9,072.** Assembly metadata carries one manifest-resource entry
   per embedded file; 9,072 of them is real metadata bloat and real assembly-load cost.
2. **No XML at runtime for the built-in set.** A lookup is a dictionary hit into a pre-split table,
   not `XmlDocument.LoadXml` + XPath per icon on first access.
3. **Reviewable refreshes.** A Phosphor update is a diff over 6 sorted text files, not 9,072 files.
4. **A layered model that duotone actually needs**, encoded once in the resource rather than
   re-derived from XML per icon.

> **Settled — do not re-open.** The alternative was put to the repository owner with these measured
> numbers: targeting `Enigma.Icons.Phosphor` at **`netstandard2.0` alone** would ship one `lib/` folder
> and cut its nupkg from ≈3.4 MB to ≈1.15 MB (a ~46 % reduction against the retired package), at the
> cost of `IsTrimmable`/`IsAotCompatible` on that package (§10.4 — they require net8+).
>
> **The owner chose to keep `netstandard2.0;net8.0;net10.0`**, accepting the ≈3.4–3.5 MB nupkg in
> exchange for real modern-TFM builds and uniformity with the other two packages. That is a deliberate,
> informed decision, not an oversight: a builder or reviewer must **not** "optimize" the TFM set, and
> the ≈3.4 MB result is the expected outcome, not a defect. State the size plainly in the release notes
> and make no reduction claim (§16 → FEATURE-74DC D5).

### 7.4 Upstream facts the generator and the tests must respect

Measured across all 9,072 files of the pinned snapshot. These are the SPEC's ground truth.

1. Element vocabulary is exactly `<svg>` and `<path>`. Nothing else.
2. `<path>` carries exactly two possible attributes: `d` (all 10,592 paths) and `opacity`
   (1,510 paths — **all** in duotone).
3. `viewBox` is `0 0 256 256` on every file, without exception.
4. **9,070 files carry `fill="currentColor"` on `<svg>`; 2 do not** —
   `duotone/cricket-duotone.svg` and `duotone/signature-duotone.svg`. The generator must not treat
   any root attribute as required.
5. Files have **no trailing newline**.
6. The 1,512 icon names are **identical across all six weights** (verified by diff).
7. `regular` filenames carry **no** suffix (`acorn.svg`); the other five carry `-<weight>`
   (`acorn-bold.svg`).
8. Path counts:
   - thin, light, regular, bold — exactly 1 path each, 1,512 files each.
   - fill — 1 path, **except these 8**: `bookmarks-simple` (2), `crane-tower` (2), `hard-drives` (2),
     `lasso` (2), `music-notes-minus` (3), `speaker-simple-x` (2), `stack` (3), `stack-simple` (2).
     None of them carries `opacity`.
   - duotone — exactly 2 paths with the first at `opacity="0.2"`, **except these 2**:
     `cell-signal-none` (1 path, no opacity) and `wifi-none` (1 path, no opacity).
9. Total paths = **10,592** = 9,072 + **10** extra in fill + 1,512 extra in duotone − 2 missing in
   duotone. The fill term is 10, not 8: the 8 outlier icons carry 18 paths between them (two of them
   have 3), so they contribute 18 − 8 = 10 paths beyond one-each.

The generator must be driven by what it reads, not by these counts — but the tests assert them, so a
future upstream refresh that changes them fails loudly instead of silently.

---

## 8. Generator tool — `tools/Enigma.Icons.Generator`

A plain, non-packable `net10.0` console app. Deterministic: same input ⇒ byte-identical output, so
`git diff` is the review surface for every icon refresh.

### 8.1 Invocation

```bash
# 1. extract the pinned snapshot
mkdir -p /tmp/phosphor-flat
tar -xzf docs/reference/phosphor/phosphor-2.1.1-svgs-flat.tar.gz -C /tmp/phosphor-flat

# 2. generate (paths relative to the repo root)
dotnet run --project tools/Enigma.Icons.Generator -- \
    --input  /tmp/phosphor-flat \
    --output src/Enigma.Icons.Phosphor
```

`--input` is a directory containing the six weight subdirectories. `--output` is the
`Enigma.Icons.Phosphor` project directory. Both required; `--check` (see §8.5) optional. Exit code 0
on success, non-zero with a diagnostic on any failure. No other arguments.

### 8.2 Outputs (all committed)

| Path | Content |
|---|---|
| `Assets/phosphor.<weight>.dat` × 6 | §7.2 format |
| `PhosphorIcon.g.cs` | the 1,512-member enum |
| `PhosphorIconNames.g.cs` | the name lookup tables |

### 8.3 Algorithm

1. For each of the six weights, enumerate `*.svg` in `<input>/<weight>`, sorted `Ordinal`.
2. Icon name = filename minus `.svg` minus a trailing `-<weight>` (regular has none).
3. Assert the six name sets are identical; abort with a diagnostic listing the differences if not.
4. For each file, extract every `<path>` in document order, capturing `d` and `opacity`. Fail if a
   file yields zero paths, or if any `d` is empty. Ignore all other elements and attributes.
5. Emit the `.dat` file: header, then one line per icon, sorted `Ordinal` by name.
6. Emit `PhosphorIcon.g.cs` and `PhosphorIconNames.g.cs` (§8.4).
7. Print a summary: per weight, icon count and byte size; plus the count of multi-layer icons.

Do **not** re-flow, re-round, or re-format path data. Copy the `d` attribute value verbatim, so the
artwork stays byte-traceable to upstream.

### 8.4 Generated C#

Both files begin with an auto-generated header, `// <auto-generated/>`, `#nullable enable`, and no
`using` directives beyond what they need (§2.2 — no implicit usings).

`PhosphorIcon.g.cs` — PascalCase members, ordered as the `.dat` files are ordered, with a one-line
`<summary>` on every member. The summary is what satisfies CS1591 (§2.8) and it doubles as a useful
IDE tooltip showing the upstream name:

```csharp
/// <summary>The Phosphor icon set. 1,512 icons, 6 weights.</summary>
public enum PhosphorIcon
{
    /// <summary>The "acorn" icon.</summary>
    Acorn,
    /// <summary>The "address-book" icon.</summary>
    AddressBook,
    /// <summary>The "address-book-tabs" icon.</summary>
    AddressBookTabs,
    …
}
```

Name mapping is mechanical and lossless — verified: all 1,512 upstream names match `^[a-z-]+$`, with
no digits, no leading numeral, and no other characters. kebab → Pascal: split on `-`, upper-case
each segment's first character. Pascal → kebab: insert `-` before each upper-case character except
the first, then lower-case. **No special cases, no exception list.**

#### Enum ordinals are positional, not a stable ABI

Members are emitted in name order with **implicit** values, so an artwork refresh that adds one icon
renumbers every member after it — upstream adding `aardvark` would shift all 1,512 values by one.
Consequences, all mandatory:

- **Never persist or transmit the numeric value** of a `PhosphorIcon`. Persist
  `PhosphorIconNames.ToKebabCase(icon)` and read it back with `TryParse`. This caveat must appear in
  the `Enigma.Icons.Phosphor` README (§13) and in the enum's own XML doc summary.
- **An artwork refresh is at least a minor version bump** for `Enigma.Icons.Phosphor`, because the
  ordinals are part of the compiled surface even though the names are the contract.
- Do **not** try to stabilize this with explicit `= N` initializers. That would freeze ordinals
  forever and make every future refresh append-only in a way upstream's alphabetical ordering does not
  support — the name-based contract above is the correct answer instead.

`PhosphorIconNames.g.cs` — the reason no `Enum.ToString()` or `Enum.Parse` is ever called:

```csharp
public static class PhosphorIconNames
{
    /// <summary>The upstream kebab-case name of an icon, e.g. "address-book-tabs".</summary>
    public static string ToKebabCase(PhosphorIcon icon);        // array index by (int)icon

    /// <summary>Looks an icon up by name. Accepts kebab-case, snake_case, or PascalCase,
    /// case-insensitively.</summary>
    public static bool TryParse(string name, out PhosphorIcon icon);

    /// <summary>Every icon name, in enum order.</summary>
    public static IReadOnlyList<string> All { get; }
}
```

`ToKebabCase` indexes a `static readonly string[]` by `(int)icon` and throws
`ArgumentOutOfRangeException` for a value outside the enum. `TryParse` uses a
`Dictionary<string, PhosphorIcon>(StringComparer.OrdinalIgnoreCase)` built once from the array,
after normalizing the input (`_` → `-`, PascalCase → kebab).

### 8.5 `--check` mode

`--check` regenerates into memory and compares against what is on disk, writing nothing and exiting
non-zero on any difference. Two uses: a fast "are the committed assets stale?" gate, and the way
FEATURE-2DDE proves its own output is reproducible.

---

## 9. `Enigma.Icons.Phosphor`

Namespace `Enigma.Icons.Phosphor`. Zero dependencies; `ProjectReference` to `Enigma.Icons` only.

```csharp
/// <summary>The six Phosphor icon weights.</summary>
public enum PhosphorWeight { Thin, Light, Regular, Bold, Fill, Duotone }

public sealed class PhosphorIconSet : IIconSet
{
    /// <summary>The shared, thread-safe instance. Resources are loaded lazily per weight.</summary>
    public static PhosphorIconSet Instance { get; }

    // Strongly-typed surface — the ergonomic path.
    public IconGlyph GetGlyph(PhosphorIcon icon, PhosphorWeight weight = PhosphorWeight.Regular);
    public bool TryGetGlyph(PhosphorIcon icon, PhosphorWeight weight, out IconGlyph? glyph);

    // IIconSet — the open, string-keyed surface.
    public string Name => "Phosphor";
    public IReadOnlyList<string> Variants { get; }        // thin light regular bold fill duotone
    public string? DefaultVariant => "regular";
    public IEnumerable<string> IconNames { get; }         // 1,512, ordinal-sorted
    public bool TryGetGlyph(string icon, string? variant, out IconGlyph? glyph);
    public IconGlyph GetGlyph(string icon, string? variant = null);
}
```

- `PhosphorWeight.Regular` is the default parameter value, preserving the retired package's
  `IconType.regular` default.
- **No `Icons`/`IconService` static god-class.** A caller either uses `PhosphorIconSet.Instance` or,
  in Avalonia, the markup extensions and the `Icon` control (§10), which resolve it internally.

### 9.1 Loading & caching

Two levels, both `ConcurrentDictionary`, both never evicted (bounded: 6 tables, 9,072 glyphs,
≈3.77 MiB worst case if an app touches literally every icon in every weight):

1. **Weight index** — `ConcurrentDictionary<PhosphorWeight, Dictionary<string, string>>`, built on
   first access to that weight by reading its embedded resource once (a single pass, split on TAB;
   no XML). Roughly 600 KB and a few hundred microseconds per weight.
2. **Glyph cache** — `ConcurrentDictionary<(PhosphorWeight, string), IconGlyph>`, so repeated
   lookups return the same instance.

Layer fields are parsed straight into `IconLayer` (opacity prefix stripped, path data verbatim) —
`SvgIconParser` is **not** involved: there is no XML in the resource. The parser exists for
user-supplied SVG (§5) and for `SvgIconSet` (§6.2).

`Lazy<T>`-style double-init is acceptable; a benign duplicate table build under a race is fine (both
results are equal), but the published value must be a single consistent table.

---

## 10. `Enigma.Icons.Avalonia`

TFMs `net8.0;net10.0`. References `Avalonia` and `Enigma.Icons.Phosphor` (which brings
`Enigma.Icons` transitively). Namespace `Enigma.Icons.Avalonia`, markup extensions in
`Enigma.Icons.Avalonia.Markup`.

### 10.1 `IconGlyphExtensions`

Framework-agnostic in its input — works for **any** `IIconSet`, including `SvgIconSet`.

```csharp
public static class IconGlyphExtensions
{
    /// <summary>The glyph as a single geometry. Multi-layer glyphs are combined into a
    /// GeometryGroup; per-layer opacity is LOST — use ToDrawing for duotone.</summary>
    public static Geometry ToGeometry(this IconGlyph glyph);

    /// <summary>The glyph as a Drawing, preserving per-layer opacity.</summary>
    public static Drawing ToDrawing(this IconGlyph glyph, IBrush brush);

    /// <summary>The glyph as a DrawingImage, suitable for Image.Source.</summary>
    public static DrawingImage ToDrawingImage(this IconGlyph glyph, IBrush brush);
}
```

- `ToGeometry`: single layer → `Geometry.Parse(PathData)`. Multiple layers → a `GeometryGroup` whose
  children are the parsed layers, `FillRule` taken from the first layer. The XML-doc **must** state
  the opacity-loss caveat, and the `Enigma.Icons.Avalonia` README must repeat it.
- `ToDrawing`: a `DrawingGroup`. Each layer becomes a `GeometryDrawing { Geometry, Brush = brush }`;
  a layer with `Opacity < 1` is wrapped in its own `DrawingGroup { Opacity = layer.Opacity }`,
  because `GeometryDrawing` has no opacity of its own. Layers with `IsFilled == false &&
  IsStroked == false` are skipped. A stroked layer gets a `Pen` built from
  `Stroke`/`StrokeWidth`/`StrokeLineCap`/`StrokeLineJoin`, defaulting to the supplied brush when
  `Stroke` is `null`.
- `ToDrawingImage`: `new DrawingImage(glyph.ToDrawing(brush))`.
- All three throw `ArgumentNullException` on a null glyph/brush. A `Geometry.Parse` failure is
  allowed to propagate — malformed path data in a committed asset is a bug, not a runtime condition.

### 10.2 The `Icon` control

Derives from `Control` and overrides `MeasureOverride` and `Render` — **not** a `TemplatedControl`.
Consequence, and the reason for the choice: a consumer needs **no `<StyleInclude>` in `App.axaml`**,
the package ships no XAML, and there is no theme-resource dependency to get wrong. This is the
capability the retired package structurally could not have: a markup extension is evaluated once at
load time and can never follow a bound brush or a theme switch.

```csharp
public sealed class Icon : Control
{
    // Built-in Phosphor path
    public static readonly StyledProperty<PhosphorIcon> KindProperty;
    public static readonly StyledProperty<PhosphorWeight> WeightProperty;   // default Regular

    // Any-icon-set path (custom sets). When IconSet is non-null it wins over Kind/Weight.
    public static readonly StyledProperty<IIconSet?> IconSetProperty;
    public static readonly StyledProperty<string?> IconNameProperty;
    public static readonly StyledProperty<string?> VariantProperty;

    // Presentation
    public static readonly StyledProperty<IBrush?> ForegroundProperty;      // TextElement.Foreground AddOwner
    public static readonly StyledProperty<double> SizeProperty;             // default 16, sets both axes
    public static readonly StyledProperty<Stretch> StretchProperty;         // default Uniform

    public PhosphorIcon Kind { get; set; }
    public PhosphorWeight Weight { get; set; }
    public IIconSet? IconSet { get; set; }
    public string? IconName { get; set; }
    public string? Variant { get; set; }
    public IBrush? Foreground { get; set; }
    public double Size { get; set; }
    public Stretch Stretch { get; set; }
}
```

Behaviour:

**Verified Avalonia 12.0.4 API coordinates** (checked against
`~/.nuget/packages/avalonia/12.0.4/lib/net8.0/*.xml` — do not go hunting for these):

| What | Exact member |
|---|---|
| Inheritable foreground | `Avalonia.Controls.Documents.TextElement.ForegroundProperty` — note the `Documents` namespace |
| Re-own it on `Icon` | `Avalonia.StyledProperty<TValue>.AddOwner<TOwner>(StyledPropertyMetadata<TValue>)` — the metadata argument is optional, so `AddOwner<Icon>()` is expected to compile; pass `null` explicitly if it does not |
| Render invalidation | `Avalonia.Visual.AffectsRender<TOwner>(params AvaloniaProperty[])` |
| Measure invalidation | `Avalonia.Layout.Layoutable.AffectsMeasure<TOwner>(params AvaloniaProperty[])` — on `Layoutable`, **not** `Visual` |
| Render override | `public override void Render(Avalonia.Media.DrawingContext)` — declared **public** on `Avalonia.Visual` in Avalonia 12 (it was `protected` in 11), so an override may not narrow it: `protected` is CS0507. Corrected against 12.0.4 by FEATURE-3ADD. |
| Stretch enum | `Avalonia.Media.Stretch` |

- `Foreground` is registered with `TextElement.ForegroundProperty.AddOwner<Icon>()`, so an `Icon`
  inside a `Button`, `MenuItem`, or `TextBlock` scope **inherits that scope's foreground** and
  follows theme switches with no binding written by the consumer. When `Foreground` resolves to
  `null`, the control paints nothing.
- `Size` sets the measured size on both axes. An explicit `Width`/`Height` wins over `Size`.
- `Render` resolves the glyph, then draws each layer scaled from `glyph.ViewBox` into the control's
  bounds per `Stretch`, honouring per-layer opacity — the same layer walk as `ToDrawing`, drawn
  directly into the `DrawingContext` (no intermediate `DrawingImage` allocation per render).
- **Never throws from `Render` or from a property change.** A missing glyph paints nothing. This
  keeps the Avalonia XAML previewer and the designer alive.
- `Kind`, `Weight`, `IconSet`, `IconName`, `Variant`, `Foreground`, `Size`, `Stretch` are all
  registered with `AffectsRender<Icon>` (and `Size`/`Stretch` additionally with
  `AffectsMeasure<Icon>`), so a property change re-renders.
- **Accessibility:** `Focusable = false`; the automation peer reports the control as decorative
  (not in the content view) unless `AutomationProperties.Name` is set on it. An icon is decoration
  by default and a screen reader should skip it.

### 10.3 Markup extensions

```csharp
namespace Enigma.Icons.Avalonia.Markup;

/// <summary>{ei:IconGeometry Acorn, Weight=Bold} — icon geometry for Path.Data etc.</summary>
public sealed class IconGeometryExtension : MarkupExtension
{
    public IconGeometryExtension();
    public IconGeometryExtension(PhosphorIcon icon);          // positional
    public PhosphorIcon Icon { get; set; }
    public PhosphorWeight Weight { get; set; } = PhosphorWeight.Regular;
    public override object ProvideValue(IServiceProvider serviceProvider);   // → Geometry
}

/// <summary>{ei:IconImage Acorn, Weight=Fill, Brush=Red} — a DrawingImage for Image.Source.</summary>
public sealed class IconImageExtension : MarkupExtension
{
    public IconImageExtension();
    public IconImageExtension(PhosphorIcon icon);             // positional
    public PhosphorIcon Icon { get; set; }
    public PhosphorWeight Weight { get; set; } = PhosphorWeight.Regular;
    public IBrush Brush { get; set; } = Brushes.Black;
    public override object ProvideValue(IServiceProvider serviceProvider);   // → DrawingImage
}
```

`IconImage` is deliberately **not** called `IconSource`: it returns a `DrawingImage`, and Avalonia
has its own `IconSource` concept — the retired package's name actively misled. The `Brush` default
stays `Brushes.Black`, matching the retired behaviour.

#### One XML namespace for both CLR namespaces (mandatory)

`Icon` lives in `Enigma.Icons.Avalonia`, the markup extensions in `Enigma.Icons.Avalonia.Markup`.
With plain `using:` syntax a consumer would need **two** `xmlns` declarations to use both — exactly
the papercut the retired package had (its tester needed
`xmlns:pia="using:PhosphorIconsAvalonia.Markup"` separately from the library namespace).

Fix it with assembly-level attributes in `src/Enigma.Icons.Avalonia/Properties/AssemblyInfo.cs`
(`Avalonia.Metadata.XmlnsDefinitionAttribute` — **verified present** in Avalonia 12.0.4's
`Avalonia.Base`):

```csharp
[assembly: XmlnsDefinition("https://github.com/josueclement/Enigma.Icons", "Enigma.Icons.Avalonia")]
[assembly: XmlnsDefinition("https://github.com/josueclement/Enigma.Icons", "Enigma.Icons.Avalonia.Markup")]
```

The URI follows the ecosystem convention of using the repository URL (cf. Avalonia's own
`https://github.com/avaloniaui`). One declaration then reaches the control **and** both markup
extensions:

```xml
xmlns:ei="https://github.com/josueclement/Enigma.Icons"
```

The per-namespace `using:` forms keep working and must be documented as the fallback. The gallery
(§11) is the compile-time proof that both resolve under a single prefix.

Note: the house `.editorconfig` sets `dotnet_style_namespace_match_folder = true:suggestion` —
**suggestion** severity, so the `Markup/` folder carrying the `.Markup` namespace is consistent
anyway, and no `Properties/` namespace mismatch can fail the build.

Usage:

```xml
xmlns:ei="https://github.com/josueclement/Enigma.Icons"

<ei:Icon Kind="Acorn" Weight="Duotone" Size="24"
         Foreground="{DynamicResource SystemControlForegroundAccentBrush}" />

<Path Data="{ei:IconGeometry Acorn, Weight=Bold}" Fill="Black" Stretch="Uniform" />
<Image Source="{ei:IconImage Acorn, Weight=Fill, Brush=Red}" Width="24" Height="24" />
```

> **The accent brush key is `SystemControlForegroundAccentBrush`, not `SystemAccentColorBrush`.**
> Corrected during FEATURE-469B, which is the first code in this solution to actually resolve it.
> `Avalonia.Themes.Fluent` 12.0.4 defines `SystemAccentColor` (a **`Color`**) and derived brushes such
> as `SystemControlForegroundAccentBrush`, but no `SystemAccentColorBrush` — verified against the
> compiled theme assembly. The distinction matters more here than elsewhere: an unresolved
> `DynamicResource` yields a null `Foreground`, and §10.2 makes a null `Foreground` paint **nothing**,
> so the documented snippet would have produced an invisible icon with no error anywhere.

C#:

```csharp
using Enigma.Icons.Avalonia;
using Enigma.Icons.Phosphor;

var geometry = PhosphorIconSet.Instance
    .GetGlyph(PhosphorIcon.Acorn, PhosphorWeight.Bold)
    .ToGeometry();
```

> **§10.4 and §10.5 are solution-wide, not Avalonia-specific.** They keep these numbers because
> plans already cite them. Read them as applying to all three packable projects.

### 10.4 Packable csproj shared property set

Every **packable** project (`Enigma.Icons`, `Enigma.Icons.Phosphor`, `Enigma.Icons.Avalonia`) sets
these *packaging* properties. The gallery, the generator (`IsPackable=false`) and the three test
projects set **none** of them.

(`ImplicitUsings` is **not** in this table: per §2 rule 2 and §3.2 it is `disable` in **every** csproj
in the solution, packable or not — including the tests, the gallery and the generator, whose SDK
templates would otherwise default it to `enable`.)

| Property | Value | Why |
|---|---|---|
| `GenerateDocumentationFile` | `true` | The mechanism behind §2 rule 8: with `TreatWarningsAsErrors`, CS1591 becomes a build **error**, so every public member — including all 1,512 generated enum members (§8.4) — must carry an XML doc comment. Deliberately **not** in `Directory.Build.props`, because test and sample projects must not inherit it. |
| `IncludeSymbols` + `SymbolPackageFormat` | `true` + `snupkg` | Debuggable published packages. |
| `PackageReadmeFile` / `PackageLicenseFile` | `README.md` / `LICENSE.md` | With the matching packed `<None>` items (§13, §14). |
| `GeneratePackageOnBuild` | **absent or `false`** | A publishable library is packed explicitly by the release step, never on every local build (`dotnet-release`). |

**Trimming / AOT.** `<IsTrimmable>true</IsTrimmable>` and `<IsAotCompatible>true</IsAotCompatible>`
apply to the `net8.0` and `net10.0` TFMs only, because `netstandard2.0` supports neither:

- `Enigma.Icons` and `Enigma.Icons.Phosphor` multi-target `netstandard2.0`, so they set both
  properties in a `PropertyGroup` conditioned on `'$(TargetFramework)' != 'netstandard2.0'`.
- `Enigma.Icons.Avalonia` targets only `net8.0;net10.0`, so the condition would be vacuous — it sets
  both **unconditionally**. This asymmetry is intentional and SPEC-sanctioned; a release audit must
  **not** flag it as a deviation, and must not "fix" it by adding a no-op condition.

The build must be free of IL2xxx/IL3xxx warnings — which `TreatWarningsAsErrors` guarantees. Achieved
by: literal-name resource lookup, generated name tables instead of `Enum.ToString()`, and no
reflection anywhere else.

### 10.5 Canonical package URLs

Every packable csproj uses these exact values — they are authoritative, not a guess for a builder
to re-derive:

```xml
<RepositoryUrl>https://github.com/josueclement/Enigma.Icons</RepositoryUrl>
<PackageProjectUrl>https://github.com/josueclement/Enigma.Icons</PackageProjectUrl>
<RepositoryType>git</RepositoryType>
```

No `PackageIcon` on any package (matching `Enigma.Logging`). The same URL is the XAML namespace URI
(§10.3) — one string, three uses.

---

## 11. `samples/Enigma.Icons.Avalonia.Gallery`

`net10.0`, `<IsPackable>false</IsPackable>`, desktop Avalonia app. Not referenced by any packable
project. Its job is the visual verification that unit tests cannot do, plus README screenshot
material.

- Wires `Host.CreateApplicationBuilder`, resolves `MainWindow` and its ViewModel from
  `host.Services`, and runs Avalonia's own lifetime (per the `avalonia` and `dotnet-solution-setup`
  skills). `CommunityToolkit.Mvvm` in the **house explicit style** — `field`-keyword properties with
  `SetProperty`, and get-only `RelayCommand`/`AsyncRelayCommand` properties initialized in the
  constructor.
  > **Corrected during FEATURE-469B.** This clause previously read "`CommunityToolkit.Mvvm` for
  > `[ObservableProperty]`/`[RelayCommand]`", which the house `communitytoolkit-mvvm` skill forbids
  > outright — it bans every MVVM generator attribute. The two could not both be satisfied; the user
  > chose the skill, so the ViewModel carries no generator attributes and is not `partial`. The
  > shipped code is the authority here.
- **Compiled bindings must be switched on explicitly.** Avalonia 12.0.4's
  `AvaloniaBuildTasks.targets` defaults `AvaloniaUseCompiledBindingsByDefault` to **`false`** and
  passes it to the XAML compiler as `DefaultCompileBindings`, so an app that omits it gets reflection
  bindings and every `x:DataType` becomes decorative. The gallery sets it to `true`, which is what
  makes a mistyped binding path a build error (`AVLN2000`) instead of a silent runtime miss. Verified
  at FEATURE-469B build time against the pinned version — do not assume the Avalonia templates' or
  the `avalonia` skill's "on by default" claim.
- Plain Avalonia + `Avalonia.Themes.Fluent` + `Avalonia.Fonts.Inter`, plus
  `AvaloniaUI.DiagnosticsSupport` under a `Debug`-only condition (**not** `Avalonia.Diagnostics`,
  which has no Avalonia 12 release — see §3.3). **Deliberately not** `Carbon.Avalonia.Desktop` —
  a sample must not couple this library to another of yours.
- Features: search box filtering all 1,512 icons by name (debounced, case-insensitive, substring);
  weight selector for all six weights; size slider; foreground colour picker or a small preset set;
  a virtualized grid of `ei:Icon` instances; click an icon to copy its XAML snippet to the clipboard;
  a status line showing the filtered count.
- Uses `PhosphorIconNames.All` for the name list and the `Icon` control for rendering — i.e. it
  exercises the same public API a consumer would, including the virtualization path.
- Handles the empty-result state ("No icons match 'xyz'") and keeps the search box focused on start.
- English only. No localization.

---

## 12. Testing

xUnit v3 (`xunit.v3` 3.2.2), `net10.0`, `OutputType Exe`, MTP runner via `global.json`. **No**
`Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio`, or coverlet. Test projects are named
`<ProjectUnderTest>.UnitTests` — a documented deviation from the `xunit-v3` skill's `.Tests`,
matching the user's instruction and the other Enigma repos.

No coverage percentage target. The bar is: every public behaviour in this SPEC has a test, and the
full-corpus integrity assertions pass.

### 12.1 `Enigma.Icons.UnitTests`

- `SvgIconParser`, one focused case per §5.1 row: `path`; `rect` (plain, `rx` only, `rx`+`ry`);
  `circle`; `ellipse`; `line`; `polyline`; `polygon`; nested `<g>` inheritance and override;
  `transform` — `translate`, `scale`, `rotate` with and without centre, `matrix`, `skewX`, `skewY`,
  and a composed list; `opacity` multiplied down a `<g>` chain; `fill="none"`;
  `fill="currentColor"` → `null`; `fill-rule="evenodd"`; the stroke attribute quartet.
- `viewBox` present / absent-with-width-height / absent entirely (→ `IconViewBox.Default`).
- Malformed input: not XML; empty string; `<svg>` with no paintable child; a document whose only
  content is an unsupported construct (assert the message names it); input exceeding
  `MaxDocumentBytes`.
- **Security:** an SVG carrying a DTD with an external entity must throw `SvgParseException` and must
  not resolve the entity (write the entity target to a temp file and assert its content never
  appears in the result); a billion-laughs document must fail fast rather than expand.
- `IconLayer`/`IconGlyph`/`IconViewBox` guard clauses, `IsSingleLayer`, `IsFilled`, `IsStroked`,
  defensive copy of the layer list.
- `SvgIconSet`: `FromDirectory` with and without variant subfolders; `-<variant>` suffix stripping;
  `FromAssembly`; `FromFiles`; `FromSvgSources`; case-insensitive and `snake_case`/PascalCase name
  lookup; a variant the set lacks is a **miss, not a fallback**; missing icon → `TryGetGlyph` false
  and `GetGlyph` throws `IconNotFoundException` with the right `IconName`/`Variant`/`SetName`;
  glyph caching returns a reference-equal instance; a path-traversal attempt is rejected;
  a non-existent directory throws at construction.

### 12.2 `Enigma.Icons.Phosphor.UnitTests`

The highest-value tests in the solution — they validate the generator's output across the entire
corpus, so a bad regeneration cannot ship silently.

- **Integrity, full corpus:** all 1,512 × 6 = 9,072 `(icon, weight)` pairs resolve; every one yields
  ≥ 1 layer with non-empty path data; no duplicate names within a weight; the six `.dat` headers
  each declare `v1`, the right weight, `0 0 256 256`, and `1512`, and the actual line count matches.
- **Enum ↔ name, full corpus:** `ToKebabCase` round-trips through `TryParse` for all 1,512 members;
  every name matches `^[a-z-]+$`; `PhosphorIconNames.All.Count == 1512`; `All` order matches enum
  order; `TryParse` accepts kebab, snake, and Pascal forms case-insensitively and rejects garbage;
  `ToKebabCase((PhosphorIcon)999999)` throws.
- **Layer-shape assertions from §7.4** — the tripwires for an upstream change:
  - thin/light/regular/bold: every icon has exactly 1 layer, opacity 1.
  - fill: exactly the 8 named icons have > 1 layer, with the stated counts; every other fill icon
    has exactly 1; none carries opacity < 1.
  - duotone: every icon has exactly 2 layers with the first at opacity `0.2`, **except**
    `cell-signal-none` and `wifi-none`, which have exactly 1 at opacity 1.
- `PhosphorIconSet`: `Name`, `Variants` (the six, in order), `DefaultVariant == "regular"`,
  `IconNames.Count() == 1512` and ordinal-sorted; `GetGlyph(icon)` defaults to `Regular`;
  string and typed overloads agree; a bad variant misses rather than falling back;
  `IconNotFoundException` content; reference-equal caching; concurrent hammering from many threads
  produces consistent results and never throws.

### 12.3 `Enigma.Icons.Avalonia.UnitTests`

`Avalonia.Headless.XUnit` at **whatever version `Directory.Packages.props` pins for the Avalonia
group** (§3.3 — 12.0.4, with 12.0.2 as the documented whole-group fallback), used via
`[AvaloniaFact]` / `[AvaloniaTheory]`. §3.3 is the only **normative** place a version is pinned. Version numbers may appear elsewhere in
this document and in plans only as dated verification notes — never as the value to write into a
project file.

> **Attribute names.** Avalonia 12's `Avalonia.Headless.XUnit` exposes `AvaloniaFactAttribute` and
> `AvaloniaTheoryAttribute`; there is no `AvaloniaTestAttribute` — that was the Avalonia 11 name.
> Verified against 12.0.4 by FEATURE-3ADD. The rule behind the name is unchanged: **every** test
> method carries the Avalonia attribute, never a plain `[Fact]`/`[Theory]`, because `Geometry.Parse`
> needs the platform render interface even in a pure geometry test.

- `ToGeometry`: single layer → parseable, non-empty bounds; multi-layer → `GeometryGroup` with the
  right child count; the documented opacity-loss behaviour is asserted so it stays deliberate.
- `ToDrawing`: layer count and nesting; a layer with opacity 0.2 is wrapped in a `DrawingGroup` of
  `Opacity == 0.2`; non-filled non-stroked layers are skipped; a stroked layer produces a `Pen` with
  the expected width/cap/join; null-argument guards.
- `ToDrawingImage`: returns a `DrawingImage` whose `Drawing` matches `ToDrawing`.
- Markup extensions: `ProvideValue` returns `Geometry` / `DrawingImage`; positional constructor
  works; `Weight` defaults to `Regular`; `Brush` defaults to black.
- `Icon` control: default `Size` is 16; `MeasureOverride` honours `Size` and an explicit
  `Width`/`Height`; `Kind`/`Weight`/`Foreground`/`Size`/`Stretch` changes invalidate render/measure;
  duotone renders two layers; `IconSet` + `IconName` path works with a `SvgIconSet` built from
  in-memory sources; **a missing glyph and a null `Foreground` both render without throwing**;
  `Foreground` inherits from an enclosing `TextElement` scope; `Focusable` is false.

---

## 13. Documentation set

| File | Owner item | Content |
|---|---|---|
| `README.md` (root) | FEATURE-21C4 skeleton, FEATURE-718F full, FEATURE-74DC callout | Landing page: what the umbrella is, the three packages one line each with NuGet + MIT badges, when to use which, quick start (XAML + C#), supported target frameworks, the gallery screenshot, the **what's-new callout** (a single blockquote — `> **What's new in 1.0** — … See the [release notes](RELEASENOTES.md).` — added by FEATURE-74DC, per `dotnet-release`), the Phosphor credit, links to the per-package READMEs, licence link. |
| `src/Enigma.Icons/README.md` | FEATURE-24DD first cut, FEATURE-718F full | Packed. The model, `IIconSet`, the parser's supported subset **and its documented exclusions**, and a worked "use your own SVG folder" example. |
| `src/Enigma.Icons.Phosphor/README.md` | FEATURE-3950 first cut, FEATURE-718F full | Packed. The 6 weights, the enum, `PhosphorIconSet`, C# usage, the Phosphor credit and licence pointer, and how to refresh the artwork. |
| `src/Enigma.Icons.Avalonia/README.md` | FEATURE-3ADD first cut, FEATURE-718F full | Packed. XAML quick start, the `Icon` control (and why it beats a markup extension for bound/themed brushes), the markup extensions, the extension methods, the `ToGeometry` opacity caveat, and the "no `StyleInclude` needed" note. |
| `RELEASENOTES.md` | FEATURE-21C4 skeleton, FEATURE-74DC body | One `## Enigma.Icons[.X] v1.0.0 Release Notes` section per package, newest-first ordering thereafter. |
| `THIRD-PARTY-NOTICES.md` | FEATURE-21C4 | §14.2. |
| `CLAUDE.md` | FEATURE-21C4 | Build/test/pack/generate commands; the three-package architecture map; the zero-dependency and zero-warning rules; a pointer at `docs/SPEC.md`. |
| `docs/RELEASE.md` | FEATURE-74DC | From the `dotnet-release` template, placeholders filled for three packages. |

Plus `docs/img/gallery.png` — captured by FEATURE-469B, embedded in the root README by
FEATURE-718F. Not packed into any package.

### 13.1 Badges — root README only

Badges appear in the **root `README.md` only**, one pair per package. The three **packed** per-package
READMEs carry **no badges**: nuget.org already displays the version on the package page, and a
relative `LICENSE.md` link does not resolve the same way there. A plan that puts a badge in a packed
README is wrong.

Badge template, substituting `<PackageId>`:

```markdown
[![NuGet](https://img.shields.io/nuget/v/<PackageId>.svg)](https://www.nuget.org/packages/<PackageId>)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.md)
```

### 13.2 Single ownership of shared README sections

**Sole ownership governs the *shipped* state, not the skeleton.** FEATURE-21C4 writes a first-cut root
README (badges, package list, TFM list) so the repository is never without one; FEATURE-718F then
rewrites the root README wholesale into its shipped form. That is the "FEATURE-21C4 skeleton,
FEATURE-718F full" split in the §13 table, and it is **not** a violation of the table below — a
compliance check against FEATURE-21C4 must not read it as one. The same applies to the three packed
per-package READMEs: their owning items (24DD/3950/3ADD) write a correct first cut, FEATURE-718F
finalizes them.

With that understood, to keep two items from writing the same lines in the shipped state:

| README section | Sole owner |
|---|---|
| Supported target frameworks (one table, three packable projects) | FEATURE-718F. FEATURE-74DC only **confirms** it, and edits it only if it changed a TFM set. |
| What's-new callout — **root README only** | FEATURE-74DC. FEATURE-718F leaves a place for it and must not write it. The three packed READMEs get **no** callout. |
| Gallery screenshot embed | FEATURE-718F, from `docs/img/gallery.png`. |
| Badges | FEATURE-718F (root README only, per §13.1). |

---

## 14. Licensing & attribution

### 14.1 `LICENSE.md` — the solution's own licence (verbatim)

```markdown
MIT License

Copyright (c) 2026 Josué Clément

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
associated documentation files (the "Software"), to deal in the Software without restriction,
including without limitation the rights to use, copy, modify, merge, publish, distribute,
sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or
substantial portions of the Software.

**THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.**
```

Packed into all three packages via `<None Include="..\..\LICENSE.md" Pack="true" PackagePath="\" />`.

### 14.2 `THIRD-PARTY-NOTICES.md` — Phosphor attribution (obligation, not courtesy)

The generated `.dat` resources **are** the Phosphor artwork, so the MIT requirement that its
copyright and permission notice travel with any substantial portion applies. The file contains a
short preamble naming the icons, the version (2.1.1), and the project URL, followed by the
**verbatim** MIT text from `docs/reference/phosphor/LICENSE` (`Copyright (c) 2020 Phosphor Icons`).

Packed into **`Enigma.Icons.Phosphor` only** — it is the only package carrying the artwork:

```xml
<None Include="..\..\THIRD-PARTY-NOTICES.md" Pack="true" PackagePath="\" />
```

The root README and the Phosphor package README each carry the credit line:

```
Icon artwork from Phosphor Icons (MIT), © 2020 Phosphor Icons — https://phosphoricons.com
```

`Enigma.Icons` and `Enigma.Icons.Avalonia` ship no third-party artwork or code and do not pack the
notices file.

---

## 15. Cross-cutting non-functional requirements

| Concern | Requirement |
|---|---|
| **Security** | §5.2 XML hardening is mandatory and tested. `SvgIconSet.FromDirectory` constrains resolved paths to its root. `MaxDocumentBytes` caps input. No network access anywhere in any package. |
| **Performance** | Cold cost of a weight ≈ one resource read plus a TAB split (~600 KB, sub-millisecond order); a warm lookup is a dictionary hit returning a cached instance. No XML parsing at runtime for the built-in set. Design intent, not a contractual SLA — do not add a timing-based test. |
| **Concurrency** | `IconGlyph`, `IconLayer`, `IconViewBox` immutable. Both caches `ConcurrentDictionary`. `IIconSet` implementations documented thread-safe; asserted by a concurrency test (§12.2). Avalonia's `Icon` is UI-thread-bound like any control. |
| **Error handling** | `TryGetGlyph` never throws **for a miss** — but a broken source still propagates; see §6.1's precedence table, which governs. `GetGlyph` throws `IconNotFoundException` on a miss. Malformed SVG throws `SvgParseException`. A corrupt/mismatched `.dat` throws `InvalidDataException`. The `Icon` control **never** throws from render or property change. Markup extensions fail fast. |
| **Observability** | None, deliberately. Logging would breach the zero-dependency constraint (§2.10). Failures surface as typed exceptions with actionable messages. |
| **Accessibility** | `Icon` is decorative by default: not focusable, excluded from the automation content view unless `AutomationProperties.Name` is set. Gallery is keyboard-navigable. |
| **Localization** | Not applicable. No user-facing strings in the libraries; icon names are identifiers and are never localized. Exception messages are English. Gallery is English-only. |
| **Trim / AOT** | §10.4. |
| **Compatibility** | `netstandard2.0` floor on the base and Phosphor packages keeps .NET Framework 4.6.2+ reachable for the deferred WPF sibling. No default interface members, no `required`/`init` members, no `Span<T>` in public API — anything needing a polyfill must be justified in a csproj comment (§3.3). |
| **Migration** | None. Clean break (§0.1). |

---

## 16. Work-item map

| ID | Title | Depends on | Produces |
|---|---|---|---|
| `FEATURE-21C4` | Solution scaffolding & shared config | — | git repo on `main`, slnx (folders only), `global.json`, both props files, the three config files, root `README`/`LICENSE.md`/`RELEASENOTES.md`/`THIRD-PARTY-NOTICES.md`/`CLAUDE.md`, empty `src`/`tests`/`samples`/`tools` |
| `FEATURE-24DD` | `Enigma.Icons` base library + UnitTests | 21C4 | §4 model, §5 parser, §6 abstraction + `SvgIconSet`, packed `src/Enigma.Icons/README.md` (first cut, §13), §12.1 tests |
| `FEATURE-2DDE` | Generator tool + generated Phosphor assets | 24DD | §8 tool, the 6 `.dat` files, `PhosphorIcon.g.cs`, `PhosphorIconNames.g.cs` (written into `src/Enigma.Icons.Phosphor/` ahead of its csproj) |
| `FEATURE-3950` | `Enigma.Icons.Phosphor` + UnitTests | 2DDE | §9 package wiring the generated assets, packed `src/Enigma.Icons.Phosphor/README.md` (first cut, §13), §12.2 tests |
| `FEATURE-3ADD` | `Enigma.Icons.Avalonia` + UnitTests | 3950 | §10 extensions, `Icon` control, markup extensions, `Properties/AssemblyInfo.cs` `XmlnsDefinition`s (§10.3), packed `src/Enigma.Icons.Avalonia/README.md` (first cut, §13), §12.3 headless tests |
| `FEATURE-469B` | Icon gallery sample app | 3ADD | §11, plus `docs/img/gallery.png` (§1, §13) |
| `FEATURE-718F` | Documentation | 469B | §13 READMEs — root README shipped state (badges, TFM table, screenshot embed) + the 3 packed READMEs finalized |
| `FEATURE-74DC` | Release preparation & NuGet runbook | 718F | `RELEASENOTES.md` bodies, `PackageReleaseNotes` ×3, root-README what's-new callout (§13.2), any non-coupled `Directory.Packages.props` bumps, `docs/RELEASE.md` (from the §1 snapshot), printed pack/tag/push runbook |
| `FEATURE-6FA1` | `Enigma.Icons.Wpf` | 74DC | **Deferred, post-1.0.** Placeholder plan only; no code in this plan. |

### 16.1 Branching

Per `dev-workflow`, each dev cuts its own branch from the current `HEAD` as
`feature/<id-lowercased>-<slug>`; each plan's status header carries the exact name.

**`FEATURE-21C4` is the one exception.** No repository exists when it runs, so `git init -b main`
both creates the repository and *is* the branch creation — that satisfies the "branch from `HEAD`"
rule, because there is no prior `HEAD` to branch from. All of its work happens directly on `main`
and **no `feature/…` sub-branch is created**; the branch name in its plan header is nominal only.
Every subsequent item branches normally.

### 16.2 Dependency note on 2DDE → 3950

The generator writes into `src/Enigma.Icons.Phosphor/` before
that project has a csproj. That is intentional: 2DDE owns *producing* the assets and proving they
are reproducible (`--check`), 3950 owns *packaging* them. 2DDE therefore adds only the generator
project to the slnx; the generated files are inert committed data until 3950 adds the csproj that
embeds them.

---

## 17. Deferred / explicitly out of scope

- **`Enigma.Icons.Wpf`** — `FEATURE-6FA1`, post-1.0. The base library's `netstandard2.0` target and
  the framework-agnostic model exist precisely so this needs no change to the shipped packages.
  WPF cannot be built or run on the planning machine (Linux).
- **Icon metadata** — categories, tags, and search aliases. Upstream keeps these in the
  `@phosphor-icons/core` npm package, which is **not** present in the local checkout
  (`node_modules` is absent) and is not derivable from filenames. Revisit if a searchable catalog
  API is wanted; the gallery's substring search covers the practical need for now.
- **Additional icon families** (Lucide, Tabler, Material, …) — the `IIconSet` abstraction and the
  three-package split make each one a new sibling package. None planned.
- **Full SVG rendering** — gradients, clips, masks, `<use>`, CSS, text (§5.1). Users wanting
  arbitrary artwork are pointed at `Avalonia.Svg` / `Svg.Skia`.
- **Migration material for `PhosphorIconsAvalonia`** — no guide, no NuGet deprecation, no
  compatibility shim, and no update to the `phosphor-icons-avalonia` skill. Decided in the
  interview. Known consequence: that skill will keep documenting the retired API.
- **A `dotnet` DI registration helper** (`AddEnigmaIcons()`) — not selected. The statics and the
  markup extensions cover the use cases; markup extensions cannot reach a container anyway.
- **Visual regression / bitmap baseline tests** — not selected. The gallery app covers visual
  verification more cheaply and without cross-platform renderer flakiness.
- **Coverage thresholds, CI pipelines, `dotnet format` gates** — not in scope.

---

## 18. `tools/Enigma.Icons.AppIconStudio` — the app-icon studio

`net10.0`, `WinExe`, `<IsPackable>false</IsPackable>`, Avalonia desktop app. Added post-1.0 by
`FEATURE-4E1F`. Not referenced by any packable project, and — like the generator and the gallery — it
ships nothing.

Its job is to **compose an application icon and write the assets an app actually consumes**: a
rounded plate (solid colour or a two-stop linear gradient) with a Phosphor glyph in a chosen colour
on top, exported as one multi-frame `.ico` plus standalone `.png` files.

### 18.1 Why both an `.ico` and PNGs

An app needs two different things, and the `.ico` cannot be both:

| Asset | Consumed by |
|---|---|
| `<base>.ico` | `<ApplicationIcon>` in the csproj — the `.exe`'s Explorer and taskbar icon — and `Window.Icon` |
| `<base>-256.png` | An About dialog or a splash window. Which frame a decoder picks out of an `.ico` is unspecified, so a real bitmap is the safe asset. |
| `<base>-<n>.png` | Linux `.desktop` entries, store listings, READMEs |

The naming is the contract: `<base>.ico` and `<base>-<size>.png`, so the output drops straight into an
`Assets/` folder.

### 18.2 The composition model (`Design/`)

- `PlateFillMode` — `Solid` or `LinearGradient`.
- `PlateFill` — immutable: mode, primary colour, secondary colour, angle in degrees clockwise from
  left-to-right. Any finite angle is accepted and wrapped into 0–360; `NaN` and infinity are rejected.
  The secondary colour is kept even in `Solid` mode, so toggling the selector does not lose it.
- `IconDesign` — immutable: icon, weight, glyph colour, plate, corner-radius ratio (0.0–0.5, default
  **0.22**; 0.5 is a circle) and glyph scale (0.2–1.0, default **0.60**). Range checks are phrased as
  negated comparisons so `NaN` is rejected rather than admitted, matching `IconViewBox`.

Colours are `Avalonia.Media.Color`: a plain struct needing no platform, and what `ColorPicker` binds
to.

### 18.3 Rendering (`Rendering/`)

`IconLayout` is pure arithmetic over Avalonia value types — no render backend, so it is unit-testable
on its own:

- `PlateRect(sizePx, ratio)` — the plate is always **edge to edge**; a platform's own margin is its
  business, and baking one in would shrink the artwork twice.
- `GlyphTransform(viewBox, sizePx, glyphScale)` — `Icon.Render`'s `Stretch.Uniform` arithmetic
  (§10.2) against the centred `sizePx × glyphScale` square. **If §10.2's centring ever changes, this
  is the method that must follow.**
- `GradientLine(angleDegrees)` — through the plate centre, half-length `(|cos θ| + |sin θ|) / 2`,
  which is what puts 45° exactly on two opposite corners and keeps the colour range constant as the
  angle turns.

`IIconRasterizer` exposes `RenderPng` and `RenderBgra`; `AvaloniaIconRasterizer` implements both over
one `RenderTargetBitmap` pass at 96 DPI, so one drawing unit is one output pixel.

- **The glyph is painted through `IconGlyphExtensions.ToDrawing`** — the same conversion the shipped
  `Icon` control uses. Duotone, per-layer opacity, strokes and `fill="none"` therefore behave
  identically, with no second implementation to keep in step.
- **Each size is rendered natively**, never downsampled from a master, so a 16 px frame's corner
  radius and stroke weights are resolved by the rasterizer rather than blurred by a resample.
- `RenderBgra` copies through a `WriteableBitmap` declared `Bgra8888`/**`Unpremul`**: the render
  target is premultiplied, and reading it directly would darken every part-transparent edge pixel
  once an ICO BMP frame reinterpreted it as straight alpha.
- `Bitmap.Save(Stream, int?)` is **obsolete** in Avalonia 12.1 and would fail the zero-warning build
  (`CS0618`). Use `Save(Stream, BitmapEncoderOptions)` with `PngBitmapEncoderOptions`.

### 18.4 The `.ico` container (`Export/`)

`IcoWriter` is pure byte assembly — no Avalonia, no file system:

```
ICONDIR        reserved u16 = 0 · type u16 = 1 · count u16 = N
ICONDIRENTRY   bWidth u8 (0 means 256) · bHeight u8 (0 means 256) · bColorCount u8 = 0 ·
    x N        bReserved u8 = 0 · wPlanes u16 = 1 · wBitCount u16 = 32 ·
               dwBytesInRes u32 · dwImageOffset u32
payloads       in the same order as the entries, ascending by size
```

**`PngFrameThreshold = 256`.** A frame of 256 px or more is stored as a **PNG file**, anything smaller
as a **32-bpp BMP DIB** (`BITMAPINFOHEADER` with `biHeight = 2 × edge`, bottom-up BGRA rows, then an
all-zero AND mask — with 32-bpp alpha the mask is redundant, and one that disagreed would fringe the
rounded corners). Measured, for the studio's seven-size set: **372,526 bytes all-BMP** against
**107,580 bytes hybrid**. (`Enigma.MarkdownEditor`'s existing four-frame all-BMP icon is 285,478
bytes, which the same arithmetic reproduces exactly — a useful check on the DIB layout.)

Two facts verified against this toolchain, not assumed:

- The .NET 10 SDK's `<ApplicationIcon>` copies a PNG frame into the executable's icon resource
  **verbatim**, and Windows renders it.
- **GDI+ (`System.Drawing.Icon`) cannot decode a PNG frame** and falls back to the largest BMP one, so
  such a caller sees 128 px as the icon's top size. The Windows shell, WIC, Skia (so Avalonia) and the
  SDK are unaffected; an app wanting a large bitmap should use the standalone PNGs. Moving
  `PngFrameThreshold` is the single lever if that trade ever has to change.

`ExportRequest` validates and normalizes on construction — sizes de-duplicated and sorted ascending,
ICO sizes capped at 256, PNG sizes at 2048, base name required to be a plain file name
(`IsValidBaseName`, which the UI reuses so the button and the constructor cannot disagree).
`IconExporter` **renders every frame before opening a file**, so a rendering failure leaves the output
directory untouched; only the writes are asynchronous, because rasterizing is UI-thread work.

### 18.5 UI

Two columns. Left: a virtualized, debounce-filtered list of all 1,512 icons, each row previewing its
glyph in the currently selected weight. Right, scrolling: the live 256 px preview on a checkerboard —
transparency is the part worth seeing before writing — with a 64/48/32/16 strip beside it, over the
design controls (weight, glyph colour, glyph size, fill mode, two colours, gradient angle, corner
radius), whose header row carries a right-aligned *Reset*. Right, **pinned below the scroll**: the
output panel — folder and *Browse…*, base name, the ICO and PNG size check-lists, *Generate*, and a
hint naming whatever is blocking it. A status line spans the window.

- **The preview is the export.** Every image on screen comes from the same `IIconRasterizer`, at the
  size it will be written; `Generate` rebuilds the design from the **live** control values rather than
  the debounced preview, so pressing it straight after a slider move writes what was just set.
- Two selection states: `SelectedIcon` is what the list highlights and goes null when a filter hides
  the row; `ActiveIcon` is what the design uses and never does.
- **Every startup value lives in `StudioDefaults`**, read by the ViewModel's property initializers
  *and* by `Reset` — a reset that re-stated those literals would be a second source of truth, and the
  two drift the first time a default is tuned. Which sizes start ticked travels with the options
  instead, as `SizeOption.IsSelectedByDefault`. `Reset` restores the design, the base name and both
  size lists, clears the search box, and renders **synchronously** rather than through the debounce;
  it deliberately **keeps the output folder**, which is a session destination rather than a design
  value.
- Preview bitmaps are **not disposed** — an `Image` draws the `Bitmap` it holds on the render thread,
  so disposing the previous one is a race with a native surface, not a tidy-up. The 150 ms debounce
  bounds the churn.
- MVVM is the house explicit CommunityToolkit style, exactly as §11 requires of the gallery: no
  generator attributes, `field` + `SetProperty`, get-only command properties.
  `AvaloniaUseCompiledBindingsByDefault` is `true`, and `AVLN3001` is suppressed by a project-local
  `.editorconfig` for the same reason the gallery suppresses it.
- `App.axaml` carries a second `StyleInclude` —
  `avares://Avalonia.Controls.ColorPicker/Themes/Fluent/Fluent.xaml`. The Fluent theme package does
  **not** merge the ColorPicker control themes, and without it every picker renders untemplated.

### 18.6 Dependencies and testing

One package pin joins §3.3's version-coupled Avalonia group: **`Avalonia.Controls.ColorPicker`
12.1.0**, referenced by this project alone. Because the project is not packable, nothing enters a
`.nupkg` dependency graph, **§2.10 is untouched**, and `THIRD-PARTY-NOTICES.md` needs no edit.

`tests/Enigma.Icons.AppIconStudio.UnitTests` mirrors §12's split: the design model, the layout maths,
the ICO container and the export orchestration are pure and tested without a platform; the rasterizer
and the ViewModel run under `Avalonia.Headless` with **`UseHeadlessDrawing = false`** plus
`.UseSkia()`, which is what makes real pixels available to assert on. The window itself is verified by
running it, per §11's precedent — and §1's rule stands: `docs/img/gallery.png` remains the only
screenshot path in the repository.
