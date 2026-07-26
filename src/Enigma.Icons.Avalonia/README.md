# Enigma.Icons.Avalonia

Icons for [Avalonia](https://avaloniaui.net) — the `Icon` control, two XAML markup extensions, and
the extension methods that turn any `IconGlyph` into an Avalonia `Geometry`, `Drawing` or
`DrawingImage`. Ships with the 1,512 × 6 Phosphor glyphs of `Enigma.Icons.Phosphor`, and renders your
own SVGs just as happily.

## What this package is

| You want… | Use |
|---|---|
| The model, the parser, your own `.svg` files | `Enigma.Icons` |
| The Phosphor artwork — 1,512 icons × 6 weights | `Enigma.Icons.Phosphor` |
| Avalonia rendering — `Geometry`, markup extensions, the `Icon` control | **`Enigma.Icons.Avalonia`** (this package) |

## Install

```bash
dotnet add package Enigma.Icons.Avalonia
```

Target frameworks: `net8.0`, `net10.0`. Unlike its two siblings this package has **no**
`netstandard2.0` target — Avalonia 12 ships `net8.0` and `net10.0` assets only, so a
`netstandard2.0` consumer could not resolve it anyway.

`Enigma.Icons.Phosphor` (and through it `Enigma.Icons`) comes along automatically.

## Nothing goes into `App.axaml`

`Icon` derives from `Control`, not `TemplatedControl`. The package ships **no XAML and no theme
resources**, so there is no `<StyleInclude>` to add and no resource key to get wrong. Add the package,
declare the namespace, use the control.

## XAML quick start

One `xmlns` reaches the control **and** both markup extensions:

```xml
xmlns:ei="https://github.com/josueclement/Enigma.Icons"
```

```xml
<ei:Icon Kind="Acorn" Weight="Duotone" Size="24"
         Foreground="{DynamicResource SystemAccentColorBrush}" />

<Path Data="{ei:IconGeometry Acorn, Weight=Bold}" Fill="Black" Stretch="Uniform" />
<Image Source="{ei:IconImage Acorn, Weight=Fill, Brush=Red}" Width="24" Height="24" />
```

<details>
<summary>Fallback: the per-namespace <code>using:</code> form</summary>

Avalonia's `using:` mapping covers one CLR namespace and not its sub-namespaces, so this form needs
**two** declarations — the control and the markup extensions live in different namespaces:

```xml
xmlns:ei="using:Enigma.Icons.Avalonia"
xmlns:eim="using:Enigma.Icons.Avalonia.Markup"
```

</details>

And from C#:

```csharp
using Enigma.Icons;
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

`Size` sets both axes; an explicit `Width`/`Height` wins over it. `Focusable` is `false`, and the
automation peer reports the control as an image that a screen reader **skips** — until you give it an
`AutomationProperties.Name`, at which point it joins the content view.

### `Foreground` inherits — this is the headline feature

`Icon.Foreground` re-owns `TextElement.ForegroundProperty`, which is an inheriting property. An icon
inside a `Button`, a `MenuItem`, or any other text scope therefore picks up that scope's foreground
and **follows a theme switch with no binding at all**:

```xml
<Button Content="Delete">
    <Button.Template>
        ...
        <StackPanel Orientation="Horizontal">
            <ei:Icon Kind="Trash" />   <!-- takes the button's foreground, hover state included -->
            <ContentPresenter />
        </StackPanel>
        ...
    </Button.Template>
</Button>
```

When `Foreground` resolves to `null` the control paints nothing — it does not fall back to black.

### Control vs. markup extension

Use the **control** whenever the brush is bound, themed, or inherited, and whenever the icon must
follow a theme switch. Use the **markup extensions** for a one-shot static `Geometry` or
`DrawingImage`: they are evaluated once at load time and never update.

There is one more deliberate asymmetry. The markup extensions **fail fast** — a bad icon or weight in
XAML throws at load time, because that is an authoring error you want to see. `Icon.Render` **never
throws**: a missing glyph, an unresolvable name, or a misbehaving third-party icon set all paint
nothing, so the XAML previewer stays alive.

## The extension methods

```csharp
Geometry      ToGeometry(this IconGlyph glyph);
Drawing       ToDrawing(this IconGlyph glyph, IBrush brush);
DrawingImage  ToDrawingImage(this IconGlyph glyph, IBrush brush);
```

They take an `IconGlyph` — from **any** `IIconSet` — so nothing here is Phosphor-specific.

### `ToGeometry` loses per-layer opacity

A `Geometry` has no per-child opacity and one shared fill rule, so a multi-layer glyph collapsed into
a `GeometryGroup` paints **every layer at full opacity**. For the two-layer `Duotone` weight that
means the tinted backing shape comes out solid — visually wrong, and deliberately allowed, because a
single `Geometry` is what `Path.Data` needs.

For duotone, use the `Icon` control or `ToDrawing`, both of which walk the layers and honour each
one's opacity.

## Bring your own SVGs

The control's `IconSet` / `IconName` / `Variant` path takes any `IIconSet`, so a folder of your own
artwork renders exactly like the built-in set:

```csharp
IIconSet mine = SvgIconSet.FromDirectory("Assets/Icons", name: "House");
```

```xml
<ei:Icon IconSet="{Binding MyIconSet}" IconName="logo" Size="32" />
```

Stroked and translucent layers survive the trip: `ToDrawing` and the control build a `Pen` from the
layer's stroke width, cap and join, and honour a layer's own stroke colour when it names one.

## Licence

MIT — see [LICENSE.md](LICENSE.md).

Icon artwork in `Enigma.Icons.Phosphor` is separately MIT-licensed by
[Phosphor Icons](https://phosphoricons.com); its notice ships with that package.
