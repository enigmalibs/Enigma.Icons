# Enigma.Icons.Avalonia

Icons for [Avalonia](https://avaloniaui.net) — the `Icon` control, two XAML markup extensions, and
the extension methods that turn any `IconGlyph` into an Avalonia `Geometry`, `Drawing` or
`DrawingImage`. Ships with the 1,512 × 6 Phosphor glyphs of `Enigma.Icons.Phosphor`, and renders your
own SVGs just as happily.

## What this package is

| You want… | Use |
|---|---|
| The model, the parser, your own `.svg` files | [`Enigma.Icons`](https://www.nuget.org/packages/Enigma.Icons) |
| The Phosphor artwork — 1,512 icons × 6 weights | [`Enigma.Icons.Phosphor`](https://www.nuget.org/packages/Enigma.Icons.Phosphor) |
| Avalonia rendering — `Geometry`, markup extensions, the `Icon` control | **`Enigma.Icons.Avalonia`** (this package) |

Target frameworks: `net8.0`, `net10.0`. Unlike its two siblings this package has **no**
`netstandard2.0` target — Avalonia 12 ships `net8.0` and `net10.0` assets only, so a
`netstandard2.0` consumer could not resolve it anyway.

It depends on `Avalonia` and on `Enigma.Icons.Phosphor` — which brings `Enigma.Icons` transitively —
and on nothing else.

## Quick start

```bash
dotnet add package Enigma.Icons.Avalonia
```

One namespace declaration reaches the control **and** both markup extensions:

```xml
xmlns:ei="https://github.com/josueclement/Enigma.Icons"
```

```xml
<ei:Icon Kind="Acorn" Weight="Duotone" Size="24"
         Foreground="{DynamicResource SystemControlForegroundAccentBrush}" />
```

The per-namespace `using:` forms remain the documented fallback — Avalonia's `using:` mapping covers
one CLR namespace and not its sub-namespaces, so that route needs **two** declarations
(`xmlns:ei="using:Enigma.Icons.Avalonia"` for the control and
`xmlns:eim="using:Enigma.Icons.Avalonia.Markup"` for the extensions).

> **Nothing goes into `App.axaml`.** `Icon` derives from `Control`, not `TemplatedControl`, and
> renders itself. The package ships **no XAML and no theme resources**, so there is no
> `<StyleInclude>` to add and no resource key to get wrong. Add the package, declare the namespace,
> use the control.

And from C#:

```csharp
using Avalonia.Media;
using Enigma.Icons.Avalonia;
using Enigma.Icons.Phosphor;

Geometry geometry = PhosphorIconSet.Instance
    .GetGlyph(PhosphorIcon.Acorn, PhosphorWeight.Bold)
    .ToGeometry();
```

## The `Icon` control

| Property | Default | What it does |
|---|---|---|
| `Kind` | `Acorn` (enum value 0) | The built-in Phosphor icon to draw |
| `Weight` | `Regular` | `Thin`, `Light`, `Regular`, `Bold`, `Fill`, `Duotone` |
| `IconSet` | `null` | A custom `IIconSet`. When set, it **wins over** `Kind`/`Weight` |
| `IconName` | `null` | The name to resolve against `IconSet` |
| `Variant` | `null` | The variant of `IconName`, or the set's default |
| `Foreground` | inherited | The brush every layer paints with |
| `Size` | `16` | The measured size on both axes |
| `Stretch` | `Uniform` | `None`, `Uniform`, `UniformToFill`, `Fill` |

Three behaviours are worth stating outright:

- **`Foreground` inherits.** It re-owns `TextElement.ForegroundProperty`, which is an inheriting
  property, so an icon inside a `Button`, a `MenuItem`, a `TextBlock` — any text scope — picks up
  that scope's brush and **follows a theme switch with no binding written by you**. When `Foreground`
  resolves to `null` the control paints nothing; it does not fall back to black.
- **An explicit `Width`/`Height` wins over `Size`.** `Size` is the measured square; the framework's
  own explicit-size coercion takes precedence over it.
- **`Render` never throws.** A missing glyph, an unresolvable name, or a misbehaving third-party icon
  set all paint nothing — a throwing render pass would take down the XAML previewer's surface for the
  whole window, not just the icon.

```xml
<Button Content="Delete">
    <Button.Template>
        <ControlTemplate>
            <StackPanel Orientation="Horizontal">
                <ei:Icon Kind="Trash" />   <!-- takes the button's foreground, hover state included -->
                <ContentPresenter />
            </StackPanel>
        </ControlTemplate>
    </Button.Template>
</Button>
```

**Accessibility.** `Icon` is decorative by default: `Focusable` is `false` and a screen reader skips
it. Set `AutomationProperties.Name` when the icon carries meaning of its own, and it joins the
automation content view.

## Control or markup extension?

A markup extension is evaluated **once, at load time**. It therefore cannot follow a bound brush, a
`DynamicResource`, or a theme switch — not as a design choice, but structurally. The control can,
because it resolves its brush at render time.

- Use **`ei:Icon`** for anything themed, bound, or interactive. This is most icons in most apps.
- Use the **markup extensions** for a static `Path.Data` or `Image.Source` that never changes.

There is one more deliberate asymmetry: the markup extensions **fail fast** — a bad icon or weight in
XAML throws at load time, because that is an authoring error you want to see — while `Icon.Render`
never throws.

## The markup extensions

```xml
<Path Data="{ei:IconGeometry Acorn, Weight=Bold}" Fill="Black" Stretch="Uniform" />
<Image Source="{ei:IconImage Acorn, Weight=Fill, Brush=Red}" Width="24" Height="24" />
```

Both take the icon as a positional argument and default `Weight` to `Regular`; `IconImage` also takes
a `Brush`, defaulting to black. It is called `IconImage` rather than `IconSource` because it returns
a `DrawingImage` and Avalonia already has an `IconSource` concept of its own.

## The extension methods

```csharp
Geometry      ToGeometry(this IconGlyph glyph);
Drawing       ToDrawing(this IconGlyph glyph, IBrush brush);
DrawingImage  ToDrawingImage(this IconGlyph glyph, IBrush brush);
```

They take an `IconGlyph` — from **any** `IIconSet` — so nothing here is Phosphor-specific.

**`ToGeometry` collapses a multi-layer glyph into a `GeometryGroup`, and per-layer opacity is lost.**
A `Geometry` has no per-child opacity and one shared fill rule, so every layer paints at full
opacity. For the two-layer `Duotone` weight that means the backing shape comes out solid — visually
wrong, and deliberately allowed, because a single `Geometry` is what `Path.Data` needs. **Use
`ToDrawing`, or the `Icon` control, for duotone**; both walk the layers and honour each one's opacity.

## Works with any `IIconSet`

The extension methods and the control's `IconSet` / `IconName` / `Variant` path take *any* icon set,
including an `SvgIconSet` over a folder of your own artwork — the built-in set has no privileged
route:

```csharp
using Enigma.Icons;

// Build the set once — at start-up, or as a ViewModel property — and bind it.
IIconSet mine = SvgIconSet.FromDirectory("Assets/Icons", name: "House");
```

```xml
<ei:Icon IconSet="{Binding MyIconSet}" IconName="logo" Variant="bold" Size="32" />
```

Stroked and translucent layers survive the trip: `ToDrawing` and the control build a `Pen` from the
layer's stroke width, cap and join, and honour a layer's own stroke colour when it names one. See
[`Enigma.Icons`](https://www.nuget.org/packages/Enigma.Icons) for the set factories and the parser's
supported subset.

## Trimming and AOT

The package is marked `IsTrimmable` and `IsAotCompatible` on the modern target frameworks, and builds
free of `IL2xxx`/`IL3xxx` warnings.

## Gallery

The repository ships a gallery sample that filters all 1,512 icons by name as you type, switches
between the six weights, and offers size and colour controls — click an icon to copy its XAML
snippet:
[`samples/Enigma.Icons.Avalonia.Gallery`](https://github.com/josueclement/Enigma.Icons/tree/main/samples/Enigma.Icons.Avalonia.Gallery).

## Licence

MIT — see [LICENSE.md](LICENSE.md), which ships in this package.

The icon artwork is separately MIT-licensed by [Phosphor Icons](https://phosphoricons.com); its
notice travels with
[`Enigma.Icons.Phosphor`](https://github.com/josueclement/Enigma.Icons/blob/main/src/Enigma.Icons.Phosphor/README.md),
the package that carries the artwork.
