# Enigma.Icons

A framework-agnostic icon model and SVG parser, an interchangeable Phosphor asset pack, and an
Avalonia renderer — three sibling packages under one umbrella. The artwork sits behind an open
`IIconSet` abstraction, so you can use the batteries-included Phosphor pack, point the same API at
your own `.svg` files, or add another family as a new sibling package rather than a fork.
Dependencies are kept at the floor: **`Enigma.Icons` has zero dependencies at all**, and
**`Enigma.Icons.Phosphor` has zero third-party dependencies** — it declares exactly one package
dependency, the sibling `Enigma.Icons`.

## Packages

**[Enigma.Icons](src/Enigma.Icons/README.md)** — the icon model, SVG parser, `IIconSet` abstraction,
and bring-your-own-SVG support.
[![NuGet](https://img.shields.io/nuget/v/Enigma.Icons.svg)](https://www.nuget.org/packages/Enigma.Icons)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.md)

**[Enigma.Icons.Phosphor](src/Enigma.Icons.Phosphor/README.md)** — 1,512 Phosphor icons × 6 weights
as embedded resources.
[![NuGet](https://img.shields.io/nuget/v/Enigma.Icons.Phosphor.svg)](https://www.nuget.org/packages/Enigma.Icons.Phosphor)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.md)

**[Enigma.Icons.Avalonia](src/Enigma.Icons.Avalonia/README.md)** — `Geometry`/`Drawing`/`DrawingImage`
conversion, markup extensions, and the `Icon` control.
[![NuGet](https://img.shields.io/nuget/v/Enigma.Icons.Avalonia.svg)](https://www.nuget.org/packages/Enigma.Icons.Avalonia)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.md)

## When to use which

| You want | Reference |
|---|---|
| Your own artwork, or another icon family | `Enigma.Icons` — the model, the parser, and `SvgIconSet` |
| The batteries-included Phosphor artwork | `Enigma.Icons.Phosphor` |
| Icons in an Avalonia app | `Enigma.Icons.Avalonia` — it brings the other two with it |

## Supported target frameworks

| Package | Target frameworks |
|---|---|
| `Enigma.Icons` | `netstandard2.0` · `net8.0` · `net10.0` |
| `Enigma.Icons.Phosphor` | `netstandard2.0` · `net8.0` · `net10.0` |
| `Enigma.Icons.Avalonia` | `net8.0` · `net10.0` |

A WPF renderer, `Enigma.Icons.Wpf`, is deferred to post-1.0.

## Quick start

XAML:

```xml
xmlns:ei="https://github.com/josueclement/Enigma.Icons"

<ei:Icon Kind="Acorn" Weight="Duotone" Size="24"
         Foreground="{DynamicResource SystemAccentColorBrush}" />
```

C#:

```csharp
using Enigma.Icons.Avalonia;
using Enigma.Icons.Phosphor;

var geometry = PhosphorIconSet.Instance
    .GetGlyph(PhosphorIcon.Acorn, PhosphorWeight.Bold)
    .ToGeometry();
```

## Credits

Icon artwork from Phosphor Icons (MIT), © 2020 Phosphor Icons — https://phosphoricons.com

The full notice is in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## Licence

MIT — see [LICENSE.md](LICENSE.md).
