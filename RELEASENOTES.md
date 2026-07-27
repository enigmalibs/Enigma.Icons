# Enigma.Icons — Release Notes

Release notes for the three packages of the `Enigma.Icons` umbrella, newest first.

## Enigma.Icons v1.0.0 Release Notes

### New Features

Initial release. `Enigma.Icons` is a new package identity with no predecessor — nothing upgrades into
it.

- **An immutable icon model.** `IconGlyph` is a view box plus an ordered list of paintable
  `IconLayer`s; `IconViewBox` carries the coordinate system. Layers are ordered because painting
  order is meaning — it is what makes a duotone glyph a duotone glyph rather than two unrelated
  shapes.
- **A hardened SVG-subset parser.** `SvgIconParser` reads `<path>`, `<rect>`, `<circle>`,
  `<ellipse>`, `<line>`, `<polyline>`, `<polygon>` and `<g>`, flattening groups and baking
  `translate` / `scale` / `rotate` / `matrix` / `skewX` / `skewY` transforms into the emitted path
  data. `opacity` multiplies down the inheritance chain, `fill="currentColor"` becomes `null` so the
  host decides the colour, and `fill-rule="evenodd"` becomes `IconFillRule.EvenOdd`. When a document
  carries no transform, its `<path d>` reaches you **byte-identical** to the source — the parser does
  not tokenize or rewrite it.
- **Documented exclusions, rejected rather than half-rendered.** Gradients and `pattern`; `clipPath`,
  `mask` and filters; `<defs>`, `<use>`, `<symbol>` and nested `<svg>`; `style="…"` attributes and
  `<style>` CSS blocks; `<text>`, `<image>` and animation; `fill-opacity` and `stroke-opacity` (only
  `opacity` is honoured). A document whose only paintable content sits inside an excluded construct
  throws `SvgParseException` **naming that construct**. This library is about icons; arbitrary
  artwork wants a full SVG renderer.
- **Mandatory XML hardening.** DTD processing is prohibited outright and no external resource is ever
  resolved, so neither XXE nor an entity-expansion bomb is reachable — an entity is never even
  *defined*. This is not a default worth trusting: on `netstandard2.0` the platform's own XML
  settings are not safe. Input above `MaxDocumentBytes` is rejected before parsing, and
  `SvgIconSet.FromDirectory` stays inside its root — it never recurses, never follows a directory
  symlink out of the root, and drops any resolved path that escapes it. No package in this family
  makes network access of any kind.
- **`IIconSet`, and bring-your-own-SVG behind it.** `SvgIconSet` offers four factories —
  `FromDirectory`, `FromAssembly`, `FromFiles` and `FromSvgSources` — so a folder of `.svg` files, an
  assembly's embedded resources, an explicit file list, or in-memory SVG text all become an icon set
  that every renderer in this family accepts. The built-in artwork has no privileged path.

### Compatibility

Target frameworks: **`netstandard2.0`**, **`net8.0`**, **`net10.0`** — unchanged since the project
was created.

The `netstandard2.0` floor is deliberate, not inherited inertia. It keeps .NET Framework 4.6.2+
reachable for the deferred WPF sibling, and it is the reason the public API carries no default
interface members, no `required` or `init` members, and no `Span<T>`. Those absences are a
compatibility contract, not an oversight.

On `net8.0` and `net10.0` the assembly is annotated `IsTrimmable` and `IsAotCompatible`, and builds
trim/AOT-clean.

### Dependencies

**None at all** — and that is stronger than "no third-party dependencies". `Enigma.Icons` declares no
package dependency whatsoever, on any target framework. It is a hard constraint of the project, not a
preference: everything the library needs lives inside the `netstandard2.0` reference set.

### Version

1.0.0.

## Enigma.Icons.Phosphor v1.0.0 Release Notes

### New Features

Initial release: the Phosphor artwork as an interchangeable asset pack, served through the same
`IIconSet` abstraction as anyone's own `.svg` folder.

- **1,512 icons × 6 weights** — thin, light, regular, bold, fill, and **duotone**. Duotone is the
  sixth weight and the new one relative to the retired predecessor's five, and it is precisely what
  forced the layered glyph model: a duotone glyph is two ordered layers at different opacities, which
  a single-path model cannot express.
- **A generated, strongly-typed surface.** The `PhosphorIcon` enum and the `PhosphorIconNames` lookup
  tables are generated from the pinned artwork, so name↔enum conversion is a table lookup — no
  `Enum.ToString` or `Enum.Parse` on any hot path.
- **Six embedded `.dat` resources**, one per weight, totalling **3,951,294 B (3.77 MiB)** whole-file,
  of which **3,820,927 B (3.64 MiB)** is path data.
- **Lazy per-weight loading with reference-equal glyph caching** — a weight is decoded the first time
  it is asked for, and repeated requests for the same glyph return the same instance.

> **On package size — no reduction is claimed.** The `.dat` format does **not** save package size.
> Deflated as one stream the `.dat` corpus is 1,150,587 B against 1,118,874 B for the same artwork as
> raw `.svg` content: compressed, the two formats are a wash and `.dat` is about 2.8 % *larger*. And
> because a multi-targeted package ships one assembly per target framework, this nupkg is larger than
> the retired package, not smaller. The format was chosen for four reasons, none of them size: six
> manifest resources instead of 9,072, no runtime XML parse, refresh diffs that are reviewable across
> six files, and the layered model duotone requires.

### Compatibility

Target frameworks: **`netstandard2.0`**, **`net8.0`**, **`net10.0`** — unchanged since the project was
created. As with `Enigma.Icons`, the `netstandard2.0` floor is deliberate: it keeps .NET Framework
4.6.2+ reachable for the deferred WPF sibling, and it is why the public API uses no default interface
members, no `required`/`init`, and no `Span<T>`. Trim/AOT-clean on the modern target frameworks.

Artwork pin: **Phosphor Icons 2.1.1**, MIT, © 2020 Phosphor Icons. The licence is not merely credited
— `THIRD-PARTY-NOTICES.md` is packed **inside this package**, because this is the package that
carries a substantial portion of the artwork.

### Dependencies

No third-party runtime dependencies; the only package dependency is the sibling `Enigma.Icons` 1.0.0.
A dependency *within* the `Enigma.Icons` family is not a third-party dependency and does not breach
the zero-dependency posture.

**`ZiggyCreatures.FusionCache` — the retired package's memoization dependency — has been dropped.**
Its role is served by two plain `ConcurrentDictionary` levels, which is all the caching an
immutable, process-lifetime icon set ever needed.

### Version

1.0.0.

**Persist names, never ordinals.** `PhosphorIcon`'s numeric values are **positional, not a stable
ABI** — the enum is generated from the artwork in name order, so adding a single icon renumbers every
member after it. Anything you store or transmit must go through
`PhosphorIconNames.ToKebabCase(icon)` and come back through `PhosphorIconNames.TryParse`; never round-trip
the integer. It follows that **any future artwork refresh is at least a MINOR version bump** of this
package.

## Enigma.Icons.Avalonia v1.0.0 Release Notes

### New Features

Initial release: Avalonia rendering for `Enigma.Icons`.

- **The `Icon` control**, deriving from `Control`. There is **no `StyleInclude` to add to
  `App.axaml`** — the package ships no XAML at all. `Foreground` is inherited from the enclosing
  `TextElement` scope, so an icon follows bound brushes and live theme switches the way text does.
  That is the capability a markup extension structurally cannot have: a markup extension is evaluated
  once and has no place in the visual tree to inherit from.
- **The `{ei:IconGeometry}` and `{ei:IconImage}` markup extensions**, for the cases where you want a
  `Geometry` or an `IImage` directly — binding `Path.Data`, filling an `Image.Source` — rather than a
  control.
- **Conversion extension methods** — `ToGeometry`, `ToDrawing` and `ToDrawingImage` — turning any
  `IconGlyph` into the corresponding Avalonia type. `ToGeometry` collapses a glyph to a single
  `Geometry` and therefore **loses per-layer opacity**; that caveat is documented on the method, and
  it is why duotone artwork wants `ToDrawing` or the `Icon` control.

One XAML namespace declaration reaches the control and both markup extensions:

```xml
xmlns:ei="https://github.com/josueclement/Enigma.Icons"
```

### Compatibility

Target frameworks: **`net8.0`**, **`net10.0`** — **no `netstandard2.0`**, because Avalonia 12 ships
modern .NET assets only and a .NET Standard target could not resolve the dependency at all.

**This package supersedes the retired `PhosphorIconsAvalonia` 1.2.0, under a new package identity and
new API names.** The break is deliberate and complete: there is **no upgrade path, no migration
guide, no compatibility shim, and no NuGet deprecation of the old package**. A consumer moving across
changes the `PackageReference` id, the XAML namespace, and the type names by hand:

| Retired (`PhosphorIconsAvalonia`) | Now (`Enigma.Icons.Avalonia`) |
|---|---|
| `Icon` | `PhosphorIcon` |
| `IconType` | `PhosphorWeight` |
| `IconService` | `PhosphorIconSet.Instance` |
| `IconSourceExtension` | `IconImageExtension` |

### Dependencies

`Avalonia` **12.1.0** and `Enigma.Icons.Phosphor` 1.0.0 (which brings `Enigma.Icons` transitively).

Dependency transitions applied for this release:

- **`Avalonia` 12.0.4 → 12.1.0.** The whole version-coupled Avalonia set moved together to one
  identical version — `Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent`,
  `Avalonia.Fonts.Inter`, `Avalonia.Headless`, `Avalonia.Headless.XUnit` — and
  `AvaloniaUI.DiagnosticsSupport` moved to **2.2.1 → 2.2.3** on its own independent version line.
  `Avalonia` is the only one of these that ships in a package, so **the published dependency floor
  for `Enigma.Icons.Avalonia` consumers is Avalonia 12.1.0**; the rest are sample- and test-only.
  `Avalonia.Headless.XUnit` 12.1.0 was re-verified to depend on `xunit.v3.extensibility.core` 3.2.2,
  i.e. it remains xUnit v3-native.
- **Solution-internal, non-shipping:** `Microsoft.Extensions.Hosting` 10.0.8 → 10.0.10 (gallery
  sample only). `CommunityToolkit.Mvvm` 8.4.2 and `xunit.v3` 3.2.2 were already current and are
  unchanged. Neither appears in any published package.

### Version

1.0.0.
