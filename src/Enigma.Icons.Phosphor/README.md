# Enigma.Icons.Phosphor

The [Phosphor](https://phosphoricons.com) artwork as an interchangeable asset pack for .NET —
**1,512 icons × 6 weights**, served through a generated enum and the `IIconSet` abstraction.
**Zero third-party dependencies**: it declares exactly one package dependency, the sibling
`Enigma.Icons`.

## What this package is

`Enigma.Icons.Phosphor` is artwork plus lookup, and nothing else. It does **no rendering** and has no
dependency on any UI framework.

The artwork is embedded as **six tables — one per weight — not as 9,072 `.svg` files**, so there is
no XML parsing at runtime: a lookup reads path data that is already path data. The upstream
version pinned here is Phosphor **2.1.1**.

| You want… | Use |
|---|---|
| The model, the parser, your own `.svg` files | [`Enigma.Icons`](https://www.nuget.org/packages/Enigma.Icons) |
| The Phosphor artwork — 1,512 icons × 6 weights | **`Enigma.Icons.Phosphor`** (this package) |
| Avalonia rendering — `Geometry`, markup extensions, the `Icon` control | [`Enigma.Icons.Avalonia`](https://www.nuget.org/packages/Enigma.Icons.Avalonia) |

```bash
dotnet add package Enigma.Icons.Phosphor
```

Target frameworks: `netstandard2.0`, `net8.0`, `net10.0`.

## The six weights

`PhosphorWeight` has six members, in this order:

| Weight | What it looks like |
|---|---|
| `Thin` | The finest outline — hairline strokes, converted to filled shapes like every other weight |
| `Light` | Still light, a step heavier than `Thin` |
| `Regular` | The default, and the set's default variant |
| `Bold` | Heavy outlines, for emphasis or small sizes |
| `Fill` | A solid silhouette rather than an outline |
| `Duotone` | **Two layers** — a backing shape at 20 % opacity under the foreground shape |

`Duotone` is the reason an icon is a *list* of layers rather than one path, and it is the one weight
with a rendering consequence: it needs a layer-aware render path. In
[`Enigma.Icons.Avalonia`](https://www.nuget.org/packages/Enigma.Icons.Avalonia) that means the `Icon`
control or `ToDrawing` — `ToGeometry` collapses the layers and paints the backing shape at full
opacity. To see all six side by side, run the gallery sample in the
[repository](https://github.com/josueclement/Enigma.Icons).

## `PhosphorIcon` and `PhosphorIconNames`

`PhosphorIcon` is a generated enum with one member per icon, named from the upstream kebab-case name
(`address-book` → `AddressBook`):

```csharp
using Enigma.Icons;
using Enigma.Icons.Phosphor;

IconGlyph acorn = PhosphorIconSet.Instance.GetGlyph(PhosphorIcon.Acorn);                        // regular
IconGlyph bold  = PhosphorIconSet.Instance.GetGlyph(PhosphorIcon.Acorn, PhosphorWeight.Bold);
IconGlyph duo   = PhosphorIconSet.Instance.GetGlyph(PhosphorIcon.Acorn, PhosphorWeight.Duotone); // 2 layers
```

`PhosphorIconNames` converts between member and name through generated lookup tables — no
`Enum.ToString`, no `Enum.Parse`, no reflection — which is what keeps hot paths fast and the package
trim- and AOT-clean:

```csharp
using System.Collections.Generic;
using Enigma.Icons.Phosphor;

string name = PhosphorIconNames.ToKebabCase(PhosphorIcon.AddressBook);   // "address-book"
PhosphorIconNames.TryParse("address_book", out PhosphorIcon icon);       // true — kebab, snake and Pascal all work
IReadOnlyList<string> all = PhosphorIconNames.All;                       // 1,512 names, in enum order
```

### Enum ordinals are positional — never persist them

> The numeric value of a `PhosphorIcon` member is **positional, not a stable ABI**. The enum is
> generated from the artwork in name order, so refreshing to a newer Phosphor release renumbers every
> member after the first insertion. **Never persist or transmit the numeric value** — to a database,
> a config file, a wire format, or anything else that outlives the process.

Persist the name instead, and read it back by name:

```csharp
using Enigma.Icons.Phosphor;

string stored = PhosphorIconNames.ToKebabCase(PhosphorIcon.AddressBook);   // save this

if (PhosphorIconNames.TryParse(stored, out PhosphorIcon restored))
{
    // restored == PhosphorIcon.AddressBook, whatever the ordinal happens to be today
}
```

For the same reason, an artwork refresh is at least a **minor** version bump for this package.

## Using the set

`PhosphorIconSet.Instance` is the single shared set — the type has no public constructor. The typed
pair takes the enum directly, with `Regular` as the default weight:

```csharp
using Enigma.Icons;
using Enigma.Icons.Phosphor;

IconGlyph glyph = PhosphorIconSet.Instance.GetGlyph(PhosphorIcon.Acorn, PhosphorWeight.Fill);

if (PhosphorIconSet.Instance.TryGetGlyph(PhosphorIcon.Acorn, PhosphorWeight.Fill, out IconGlyph? found))
{
    // found is non-null whenever the call returned true
}
```

The `out` parameter is typed `IconGlyph?` and carries no nullable flow attribute, so under
`Nullable: enable` the compiler cannot prove that for you — null-check `found`, or assert it, before
dereferencing.

The same object is an `IIconSet`, so it drops into anything written against the abstraction:

```csharp
using System.Collections.Generic;
using Enigma.Icons;
using Enigma.Icons.Phosphor;

IIconSet icons = PhosphorIconSet.Instance;

string setName = icons.Name;                        // "Phosphor"
IReadOnlyList<string> variants = icons.Variants;    // ["thin", "light", "regular", "bold", "fill", "duotone"]
string? fallback = icons.DefaultVariant;            // "regular"
IEnumerable<string> names = icons.IconNames;        // 1,512 kebab-case names

IconGlyph acorn     = icons.GetGlyph("acorn");                   // the default variant
IconGlyph addressBk = icons.GetGlyph("AddressBook", "bold");     // PascalCase input is fine
icons.TryGetGlyph("no-such-icon", null, out IconGlyph? missing); // false — a miss, no exception
```

Lookup is case-insensitive and accepts kebab-case, `snake_case` or PascalCase. The same applies to
variant names, so `" Duotone "` resolves exactly as `"duotone"` does. **A variant the set does not
have is a miss, never a silent fallback** — asking for `heavy` throws `IconNotFoundException` rather
than quietly handing back `regular`.

### Loading, caching and thread safety

Nothing is read when you touch `Instance`. A weight's table is loaded from its embedded resource on
**first use of that weight**, and each resolved glyph is cached, so repeated lookups return a
**reference-equal** `IconGlyph`. Every member is safe for concurrent use from any thread.

A resource that is missing or corrupt is a *broken source*, not a miss: it raises
`InvalidDataException` from `TryGetGlyph` just as it does from `GetGlyph`.

## Icon artwork credit

Icon artwork from Phosphor Icons (MIT), © 2020 Phosphor Icons — https://phosphoricons.com

Phosphor's full MIT copyright and permission notice ships with this package as
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) — this is the only package in the family that
carries the artwork, and the only one that carries the notice.

## Refreshing the artwork

The embedded tables and the `PhosphorIcon` enum are **generated** — never hand-edited. From a clone
of the repository: extract the pinned upstream snapshot to a scratch directory, run the generator
over it, review the resulting `git diff`, and re-run the full-corpus integrity tests.

```bash
dotnet run --project tools/Enigma.Icons.Generator -- \
    --input  /tmp/phosphor-flat \
    --output src/Enigma.Icons.Phosphor
```

Remember the ordinal caveat above: a refresh that adds or removes an icon renumbers the enum, so it
is at least a minor version bump. Snapshot provenance, the pinned `sha256`, and the procedure for
moving to a newer Phosphor release are documented in
[`docs/reference/phosphor/README.md`](https://github.com/josueclement/Enigma.Icons/blob/main/docs/reference/phosphor/README.md).

## Licence

MIT for the code — see [LICENSE.md](LICENSE.md), which ships in this package.

MIT for the artwork, separately licensed by Phosphor Icons — see
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md), which ships alongside it.
