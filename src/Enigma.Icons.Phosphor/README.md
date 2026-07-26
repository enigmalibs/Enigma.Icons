# Enigma.Icons.Phosphor

The [Phosphor](https://phosphoricons.com) artwork as an interchangeable asset pack for .NET —
**1,512 icons × 6 weights**, served from six embedded resources through a strongly-typed enum and the
`IIconSet` abstraction. **Zero third-party dependencies**: it references only its sibling
`Enigma.Icons`.

## What this package is

`Enigma.Icons.Phosphor` is artwork plus lookup, and nothing else. It does **no rendering** and has no
dependency on any UI framework.

| You want… | Use |
|---|---|
| The model, the parser, your own `.svg` files | `Enigma.Icons` |
| The Phosphor artwork — 1,512 icons × 6 weights | **`Enigma.Icons.Phosphor`** (this package) |
| Avalonia rendering — `Geometry`, markup extensions, the `Icon` control | `Enigma.Icons.Avalonia` |

## Install

```bash
dotnet add package Enigma.Icons.Phosphor
```

Zero dependencies · target frameworks: `netstandard2.0`, `net8.0`, `net10.0`.

## The weights

`PhosphorWeight` has six members, in this order: `Thin`, `Light`, `Regular`, `Bold`, `Fill`,
`Duotone`. `Duotone` glyphs carry two layers — a tinted backing shape at 20 % opacity behind the
foreground shape — which is why an icon is a **list** of layers rather than one path.

## The strongly-typed surface

`PhosphorIcon` is a generated enum with one member per icon, named from the upstream kebab-case name
(`address-book` → `AddressBook`):

```csharp
using Enigma.Icons;
using Enigma.Icons.Phosphor;

IconGlyph acorn  = PhosphorIconSet.Instance.GetGlyph(PhosphorIcon.Acorn);                        // regular
IconGlyph bold   = PhosphorIconSet.Instance.GetGlyph(PhosphorIcon.Acorn, PhosphorWeight.Bold);
IconGlyph duo    = PhosphorIconSet.Instance.GetGlyph(PhosphorIcon.Acorn, PhosphorWeight.Duotone); // 2 layers

if (PhosphorIconSet.Instance.TryGetGlyph(PhosphorIcon.Acorn, PhosphorWeight.Fill, out IconGlyph? fill))
{
    // fill is never null here
}
```

Names and enum members convert without `Enum.ToString`, `Enum.Parse` or reflection — which is what
keeps the package trim- and AOT-clean:

```csharp
string name = PhosphorIconNames.ToKebabCase(PhosphorIcon.AddressBook);   // "address-book"
PhosphorIconNames.TryParse("address_book", out PhosphorIcon icon);       // true — kebab, snake and Pascal all work
IReadOnlyList<string> all = PhosphorIconNames.All;                       // 1,512 names, in enum order
```

> The numeric value of a `PhosphorIcon` member is **positional** and is not a stable ABI: an artwork
> refresh that adds one icon renumbers every member after it. Persist the *name*, never the ordinal.

## The string-keyed surface

`PhosphorIconSet.Instance` is an `IIconSet`, so it drops into anything written against the
abstraction:

```csharp
IIconSet icons = PhosphorIconSet.Instance;

icons.Name;            // "Phosphor"
icons.Variants;        // ["thin", "light", "regular", "bold", "fill", "duotone"]
icons.DefaultVariant;  // "regular"
icons.IconNames;       // 1,512 names, ordinal-sorted

IconGlyph acorn     = icons.GetGlyph("acorn");                  // the default variant
IconGlyph addressBk = icons.GetGlyph("AddressBook", "bold");    // PascalCase input is fine
icons.TryGetGlyph("no-such-icon", null, out IconGlyph? missing); // false — a miss never throws
```

Lookup is case-insensitive and accepts kebab-case, `snake_case` or PascalCase. **A variant the set
does not have is a miss, never a silent fallback** — asking for `heavy` throws `IconNotFoundException`
rather than quietly handing back `regular`.

## Loading and caching

Nothing is read when you touch `Instance`. A weight's table is loaded from its embedded resource on
first use of that weight, and each resolved glyph is cached, so repeated lookups return a
**reference-equal** `IconGlyph`. Every member is safe for concurrent use from any thread.

The embedded resources hold path data, not SVG, so **no XML is parsed at runtime** for this set.

A resource that is missing or corrupt is a *broken source*, not a miss: it raises
`InvalidDataException` from `TryGetGlyph` just as it does from `GetGlyph`.

## Rendering

This package produces `IconGlyph` values; it does not draw them. For Avalonia, add
[`Enigma.Icons.Avalonia`](https://www.nuget.org/packages/Enigma.Icons.Avalonia), which converts a
glyph to a `Geometry` or `Drawing` and adds the `Icon` control and the XAML markup extensions.

## Icon artwork credit

Icon artwork from Phosphor Icons (MIT), © 2020 Phosphor Icons — https://phosphoricons.com

Phosphor's full MIT copyright and permission notice ships with this package in
`THIRD-PARTY-NOTICES.md`. The version bundled here is Phosphor **2.1.1**.

## Refreshing the artwork

The embedded resources and the `PhosphorIcon` enum are **generated** — never hand-edited. Refreshing
to a newer Phosphor release is a documented procedure in the repository:
`docs/reference/phosphor/README.md` ("Refreshing to a newer Phosphor release"), which runs the
generator described in `docs/SPEC.md` §8 and reviews the resulting diff.

## Licence

MIT — see [LICENSE.md](LICENSE.md). The icon artwork is separately MIT-licensed by Phosphor Icons;
see `THIRD-PARTY-NOTICES.md`.
