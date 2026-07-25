# Enigma.Icons

Framework-agnostic icon model, hardened SVG icon parser and icon-set abstraction for .NET.
**Zero dependencies.**

## What this package is

`Enigma.Icons` is the bottom layer of the Enigma.Icons family. It knows what an icon *is* — a view
box plus ordered, paintable layers — and how to read one out of an SVG file. It does **no
rendering**: it has no dependency on any UI framework, and none on anything else either.

| You want… | Use |
|---|---|
| The model, the parser, your own `.svg` files | **`Enigma.Icons`** (this package) |
| The Phosphor artwork — 1,512 icons × 6 weights | `Enigma.Icons.Phosphor` |
| Avalonia rendering — `Geometry`, markup extensions, the `Icon` control | `Enigma.Icons.Avalonia` |

## Install

```bash
dotnet add package Enigma.Icons
```

Target frameworks: `netstandard2.0`, `net8.0`, `net10.0`.

## The model

```csharp
IconGlyph glyph = SvgIconParser.Parse(File.ReadAllText("acorn.svg"));

foreach (IconLayer layer in glyph.Layers)   // index 0 is painted first (bottom-most)
{
    Console.WriteLine(layer.PathData);      // SVG path mini-language, never null or empty
}
```

- **`IconViewBox`** — the coordinate rectangle the path data is expressed in. `Width`/`Height` are
  always greater than zero. `IconViewBox.Default` is `0 0 256 256`.
- **`IconLayer`** — one paintable element: `PathData`, `Opacity`, `FillRule`, and the paint
  properties `Fill`, `Stroke`, `StrokeWidth`, `StrokeLineCap`, `StrokeLineJoin`. A layer **never**
  carries a transform — every SVG transform is baked into `PathData`.
- **`IconGlyph`** — a view box plus a never-empty, defensively copied layer list.
  `IsSingleLayer` is true for the common case of one fully-opaque, unstroked layer, which a renderer
  can collapse to a single filled geometry.

**Why `Fill` and `Stroke` are raw strings and not a colour type.** This package has zero
dependencies and therefore no framework colour type. Each renderer interprets them in its own terms
and, in the normal icon case, ignores them in favour of the consumer's brush:

| Value | Meaning |
|---|---|
| `null` | Inherit the renderer's brush. This is what `"currentColor"` normalizes to. |
| `"none"` | Do not paint. `IsFilled` / `IsStroked` are false. |
| anything else | The raw SVG paint value, verbatim — `"#112233"`, `"red"`, … |

## Icon sets

`IIconSet` is the lookup abstraction every icon pack implements:

```csharp
public interface IIconSet
{
    string Name { get; }
    IReadOnlyList<string> Variants { get; }
    string? DefaultVariant { get; }
    IEnumerable<string> IconNames { get; }
    bool TryGetGlyph(string icon, string? variant, out IconGlyph? glyph);
    IconGlyph GetGlyph(string icon, string? variant = null);
}
```

Guarantees every implementation makes:

- Lookup is **case-insensitive** and accepts kebab-case, `snake_case` or PascalCase — `acorn`,
  `Acorn`, `my_icon` and `MyIcon` all work.
- `variant == null` means `DefaultVariant`. **A variant the set does not have is a miss, never a
  silent fallback** — asking for `bold` never gets you `regular`.
- Repeated lookups return a **cached, reference-equal** `IconGlyph`.
- All members are safe for concurrent use.

### Misses versus broken sources

The `Try` prefix governs *absence*, not *corruption*:

| Situation | `TryGetGlyph` | `GetGlyph` |
|---|---|---|
| The icon or variant is not in the set | returns `false`, glyph is `null` | throws `IconNotFoundException` |
| The source is present but **broken** — malformed SVG, unreadable or vanished file, missing resource | **throws** `SvgParseException` | throws the same |

A miss is a normal outcome you branch on; a broken source is a defect, and reporting it as "icon not
found" would turn a fixable bug into an invisible blank space.

## Bring your own SVG folder

`SvgIconSet.FromDirectory` turns a directory of `.svg` files into an icon set. Each immediate
subdirectory is a variant:

```
my-icons/
├─ regular/
│   ├─ acorn.svg
│   └─ address-book.svg
├─ bold/
│   ├─ acorn-bold.svg        ← the "-bold" suffix is stripped: this is "acorn" in variant "bold"
│   └─ address-book-bold.svg
└─ thin/
    ├─ acorn-thin.svg
    └─ address-book-thin.svg
```

```csharp
IIconSet icons = SvgIconSet.FromDirectory("my-icons");

// Variants: ["bold", "regular", "thin"]; DefaultVariant: "regular"
IconGlyph acorn     = icons.GetGlyph("acorn");            // the default variant
IconGlyph acornBold = icons.GetGlyph("Acorn", "bold");    // PascalCase input is fine

if (icons.TryGetGlyph("no-such-icon", null, out IconGlyph? missing))
{
    // not reached — a miss returns false rather than throwing
}
```

Because the `-<variant>` filename suffix is stripped, **an extracted Phosphor tree works unchanged**.

Three more factories:

```csharp
// Flat directory, no variants at all.
SvgIconSet.FromDirectory("my-icons", variantsFromSubfolders: false);

// An explicit file list.
SvgIconSet.FromFiles(new[] { "icons/acorn.svg", "icons/leaf.svg" });

// Your assembly's embedded resources: <prefix>[.<variant>].<icon>.svg
SvgIconSet.FromAssembly(typeof(MyType).Assembly, "MyApp.Icons.");
```

And the in-memory escape hatch:

```csharp
IIconSet icons = SvgIconSet.FromSvgSources(new[]
{
    new KeyValuePair<string, string>(
        "square",
        "<svg viewBox=\"0 0 16 16\"><path d=\"M 0,0 L 16,0 L 16,16 Z\" /></svg>"),
});
```

Discovery is **eager** — `IconNames` is complete the moment the set is built — while parsing is
**lazy** and cached per glyph.

## What the parser supports

| SVG construct | Handling |
|---|---|
| `<path d="…">` | `d` taken **verbatim** as `IconLayer.PathData`. |
| `<rect x y width height rx ry>` | Converted to path data. A single supplied corner radius mirrors to the other axis, per SVG rules. |
| `<circle cx cy r>` | Converted to path data (two arcs). |
| `<ellipse cx cy rx ry>` | Converted to path data (two arcs). |
| `<line x1 y1 x2 y2>` | Converted to `M x1,y1 L x2,y2`. |
| `<polyline points>` | Converted to `M … L …` (open). |
| `<polygon points>` | Converted to `M … L … Z` (closed). |
| `<g>` | Flattened. Its `transform`, `opacity`, `fill`, `stroke`, `fill-rule` and stroke properties inherit to descendants; a child's own value wins. Nests to any depth. |
| `transform="…"` | `translate`, `scale`, `rotate` (with and without a centre), `matrix`, `skewX`, `skewY`, and lists thereof — composed with any inherited transform and **baked into the emitted path data**. |
| `opacity` | Multiplied down the inheritance chain into `IconLayer.Opacity`. |
| `fill`, `fill-rule` | Captured. `"currentColor"` → `null`. `fill-rule="evenodd"` → `IconFillRule.EvenOdd`. |
| `stroke`, `stroke-width`, `stroke-linecap`, `stroke-linejoin` | Captured on the layer. |
| `viewBox` | → `IconGlyph.ViewBox`. Absent → derived from `width`/`height` if usable, else `0 0 256 256`. |

When a document carries no `transform`, its `<path d>` reaches you **byte-identical** to the source —
the parser does not tokenize or rewrite it at all.

## What the parser does not support

Explicitly out of scope, and rejected rather than half-rendered:

- gradients (`linearGradient`, `radialGradient`), `pattern`
- `clipPath`, `mask`, filters
- `<defs>`, `<use>`, `<symbol>`, nested `<svg>`
- `style="…"` attributes and `<style>` CSS blocks
- `<text>`, `<image>`, animation
- `fill-opacity` and `stroke-opacity` (only `opacity` is honoured)

A document whose only paintable content sits inside an excluded construct throws `SvgParseException`
with a message **naming that construct**. A `style="…"` attribute on an otherwise-supported shape
does not suppress the layer — only the CSS-expressed paint is lost.

**This library is about icons.** For arbitrary artwork, use a full SVG renderer such as
[`Avalonia.Svg`](https://www.nuget.org/packages/Avalonia.Svg) or
[`Svg.Skia`](https://www.nuget.org/packages/Svg.Skia).

## Security

SVG from outside your application is untrusted XML, and this parser treats it that way.

- **DTDs are prohibited outright** (`DtdProcessing.Prohibit`), so neither XXE (external-entity
  file disclosure) nor an entity-expansion bomb ("billion laughs") is possible — an entity is never
  even *defined*. `XmlResolver` is `null`, so no external reference is ever fetched. This matters:
  on .NET Framework and `netstandard2.0`, `XmlDocument`'s own defaults are **not** safe.
- **`SvgIconParser.MaxDocumentBytes`** caps input at 1 MiB by default; anything larger is rejected
  *before* parsing. It is process-wide mutable configuration — set it once at start-up if the
  default does not suit you.
- **`SvgIconSet.FromDirectory` stays inside its root**: it never recurses, never follows a directory
  symlink out of the root, and drops any resolved path that escapes it.
- No package in this family makes network access of any kind.

## Licence

MIT — see [LICENSE.md](LICENSE.md).
