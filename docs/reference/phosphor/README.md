# Phosphor Icons — pinned upstream snapshot

This directory is the **self-contained** source of the icon artwork shipped by
`Enigma.Icons.Phosphor`. It exists so `docs/` can be carried to another machine and the whole
solution rebuilt from A to Z without the original Phosphor checkout being present.

## Contents

| File | What it is |
|---|---|
| `phosphor-2.1.1-svgs-flat.tar.gz` | The pinned upstream asset snapshot — 9,072 flat SVG files (1,512 icons × 6 weights). |
| `LICENSE` | Phosphor Icons' MIT licence, verbatim. Its text is reproduced in the repo's root `THIRD-PARTY-NOTICES.md`. |
| `README.md` | This file. |

## Provenance

| | |
|---|---|
| Upstream project | Phosphor Icons — <https://phosphoricons.com> · <https://github.com/phosphor-icons/homepage> |
| Upstream version | **2.1.1** (`package.json` of the `phosphor-icons/homepage` repo) |
| Source directory | `<checkout>/public/assets/SVGs Flat` |
| Licence | MIT — Copyright (c) 2020 Phosphor Icons |
| Archive sha256 | `6dc577a39a5a0cb72984e698ba93e7e3736da99b5884fcb221e564d5244c301e` |
| Archive size | 1,412,717 bytes (1.35 MiB) |
| Files inside | 9,072 `.svg` |

Verify the archive before use:

```bash
sha256sum -c <<<'6dc577a39a5a0cb72984e698ba93e7e3736da99b5884fcb221e564d5244c301e  phosphor-2.1.1-svgs-flat.tar.gz'
```

## Layout inside the archive

The six weight directories sit at the archive **root** (the upstream `SVGs Flat/` wrapper — whose
name contains a space — was deliberately stripped):

```
bold/      1512 files    acorn-bold.svg,     address-book-bold.svg,     …
duotone/   1512 files    acorn-duotone.svg,  address-book-duotone.svg,  …
fill/      1512 files    acorn-fill.svg,     address-book-fill.svg,     …
light/     1512 files    acorn-light.svg,    address-book-light.svg,    …
regular/   1512 files    acorn.svg,          address-book.svg,          …   ← no suffix
thin/      1512 files    acorn-thin.svg,     address-book-thin.svg,     …
```

Extract with:

```bash
mkdir -p /tmp/phosphor-flat
tar -xzf docs/reference/phosphor/phosphor-2.1.1-svgs-flat.tar.gz -C /tmp/phosphor-flat
```

## Why "SVGs Flat" and not "SVGs"

Upstream ships **two** SVG sets, and they are not interchangeable:

| | `SVGs/` | `SVGs Flat/` ← **this snapshot** |
|---|---|---|
| Outline weights (thin/light/regular/bold) | 3–8 elements, `fill="none" stroke="currentColor" stroke-width="16"` | **one filled `<path>`**, `fill="currentColor"` |
| Elements used | `path`, `line`, `rect`, `circle`, `polyline`, `polygon`, `ellipse`, `g` | `path` only |
| Renders as a single fillable `Geometry` | no | **yes** |

`SVGs Flat` is the outline-converted-to-fill form: the *rendered* result of the source set, already
flattened. It is what the retired `PhosphorIconsAvalonia` package embedded, and it is what lets a
weight be reduced to path data that `Geometry.Parse` consumes directly. See SPEC §7.

## Archive reproducibility

The archive is byte-reproducible from the same input tree:

```bash
cd "<checkout>/public/assets/SVGs Flat"
tar --sort=name --owner=0 --group=0 --numeric-owner --mtime=@1735689600 \
    -cf - thin light regular bold fill duotone | gzip -9n > phosphor-<version>-svgs-flat.tar.gz
```

## Refreshing to a newer Phosphor release

1. Update the upstream checkout and read its new `package.json` version.
2. Rebuild the archive with the command above, named `phosphor-<newversion>-svgs-flat.tar.gz`.
3. Delete the old archive, update this file (version, sha256, size, file count).
4. Re-run the generator (SPEC §8) — `git diff` on `src/Enigma.Icons.Phosphor/` then shows exactly
   which icons changed and which were added. Then run it once more with `--check`: it must exit 0,
   which is what proves the committed bytes are what the generator actually produces.
5. Confirm the full-corpus integrity tests still pass (SPEC §12).
6. **Treat the refresh as at least a MINOR version bump** for `Enigma.Icons.Phosphor`. `PhosphorIcon`
   members are numbered by position in the ordinal-sorted name list, so adding or removing a single
   icon renumbers every member after it (SPEC §8.4). The names are the contract; the ordinals are
   not.
