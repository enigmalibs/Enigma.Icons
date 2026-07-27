# FEATURE-718F — Documentation (root + 3 packed READMEs) · DONE

**Branch:** `feature/feature-718f-documentation` · Single-phase · Plan: `docs/plan/FEATURE-718F.md`

## Summary

Brought all four READMEs to their **shipped** state. The root `README.md` was rewritten wholesale
from the FEATURE-21C4 skeleton into the full SPEC §13 landing page — intro, the three packages with
their SPEC §13.1 badge pairs, when-to-use-which guidance, a two-path quick start, the
supported-target-frameworks table this item owns, the gallery screenshot, bring-your-own-SVGs, the
resolved per-package documentation links, the SPEC §14.2 credit line, and the licence. The three
packed per-package READMEs were finalized from their owning items' first cuts into standalone
nuget.org product pages.

**No code changed.** No `.cs` or `.axaml` file was touched, `Enigma.Icons.slnx` is byte-unchanged,
and no version number or what's-new callout was written anywhere — FEATURE-74DC owns those. The slot
left for 74DC's blockquote is the blank line between the root README's intro paragraph and its
`## Packages` heading; nothing was written into it, and the TFM facts now appear in exactly one place
(the table) so 74DC's confirmation is a one-line edit.

**Every code sample was verified against the shipped signatures, not written from memory.** The
signature ledger was harvested first, from the real source, before any sample was written — and the
samples were then *compiled*, not just eyeballed. See *Build/test evidence*.

## Files touched

### Modified

| File | What changed |
|---|---|
| `README.md` | Rewritten in full (SPEC §13 row 1). Was the 21C4 skeleton; now the shipped landing page. |
| `src/Enigma.Icons/README.md` | Finalized: reordered into the plan's section order, parser section split into supported subset / hardening / exclusions, a dedicated **Errors** table added (miss vs broken source vs `DirectoryNotFoundException`), `FromDirectory` example shown with its real parameter list, samples given their `using` directives. |
| `src/Enigma.Icons.Phosphor/README.md` | Finalized: weights table giving a visual sense of each, the enum-ordinal caveat promoted to its own section with a persist-by-name example, typed and string surfaces split, refresh procedure with the SPEC §8.1 generator invocation. |
| `src/Enigma.Icons.Avalonia/README.md` | Finalized: the `<StyleInclude>` note promoted to a callout under the quick start, control-vs-extension rewritten around the load-time-evaluation *reason*, `ToGeometry` opacity loss stated as a caveat, accessibility and trimming/AOT lines added, gallery and sibling links made absolute. |
| `docs/roadmap.md` | FEATURE-718F row → `IN PROGRESS` → `DONE`. |
| `docs/plan/FEATURE-718F.md` | Status header → `DONE`; all 17 acceptance criteria ticked. |
| `CLAUDE.md` | Documentation freshness sweep, accepted by the user: the incremental-growth note said "the two remaining 1.0.0 items … FEATURE-718F writes the READMEs, FEATURE-74DC prepares the release"; with 718F done, only 74DC remains. Prose only — no command, rule or path changed. |

### Created

| File | What it is |
|---|---|
| `docs/done/FEATURE-718F.md` | This record. |

Nothing was created under `src/`, `tests/`, `tools/` or `samples/`. `docs/img/gallery.png` already
existed (captured by FEATURE-469B) and was embedded, not modified.

## Deviations & follow-ups

1. **No deviation from the plan's scope or section structure.** All five writing steps and the
   six-part consistency sweep were carried out as specified.

2. **Follow-up against FEATURE-3ADD/24DD (new, minor, API ergonomics — not fixed here).**
   `IIconSet.TryGetGlyph`'s `out IconGlyph? glyph` carries **no `[NotNullWhen(true)]`** — the
   attribute appears nowhere in `src/` and is not mandated by the SPEC. Consequence: a
   `Nullable: enable` consumer who follows the obvious `if (set.TryGetGlyph(...)) { use(glyph); }`
   shape gets **CS8602** and must null-check or assert. This surfaced from the sample compile, which
   is exactly what that pass is for. Per the plan's *"do not paper over a gap"* rule the READMEs
   document the **shipped** behaviour — the Phosphor README now says the compiler cannot prove
   non-nullness and that you should null-check — and **no library code was touched**. Adding
   `[NotNullWhen(true)]` to the interface and its two implementations would be a source-compatible
   quality-of-life fix worth considering post-1.0; it is a defect against the owning items, not
   against this one.

3. **The plan's "no size claim" rule was applied more strictly than the letter.** The Phosphor
   README's first cut had no byte figure, but the phrase "six **compact** tables" could be read as a
   size claim, so "compact" was dropped. SPEC §7.3 is explicit that the format saves no package size.

4. **Two wording corrections against reality, not against the plan.** The gallery description was
   rewritten from a paraphrase ("shows every icon in the six weights") to what SPEC §11 actually
   specifies — name filtering as you type, a weight selector for the grid, size slider, colour
   picker, click-to-copy-XAML. And the note under the root TFM table was reworded to stop naming
   individual TFMs outside the table, so 74DC's confirmation stays surgical (plan *Notes / risks*,
   "doc-drift seam").

5. **Line endings: nothing to report.** All four READMEs are LF with a final newline; no CRLF was
   observed in any touched file, so no recommendation is warranted.

6. **`Enigma.Icons.Wpf`** is named in the root README in exactly one line, as post-1.0 and not part
   of this release (SPEC §17). No timeline, no API. No README mentions `PhosphorIconsAvalonia`,
   `IconService`, `IconType` or the retired enum — the clean break holds.

## Build/test evidence

DoD criteria 1–2 are regression checks here: this item changed no code.

| Check | Result |
|---|---|
| `dotnet build Enigma.Icons.slnx` | **Build succeeded — 0 Warning(s), 0 Error(s)** across all 8 projects. |
| `dotnet test --solution Enigma.Icons.slnx` | **Passed — total 434, failed 0, succeeded 434, skipped 0.** No test added or removed. |
| `git diff --name-only` filtered to `\.(cs\|axaml)$` | **empty** — no code file modified. |
| `git diff --stat -- Enigma.Icons.slnx` | **empty** — byte-unchanged, zero `<Project>` entries appended (SPEC §3.4). |

### Sample verification — compiled, not eyeballed

Both harnesses were built in the **scratchpad**, never in the repository, and discarded.

| Pass | How | Result |
|---|---|---|
| **C# samples** | Throwaway `net10.0` class library `ProjectReference`-ing all three `src` projects; every C# sample from all four READMEs pasted in verbatim with its own `using` directives. | **Build succeeded — 0 Warning(s), 0 Error(s).** |
| **`IIconSet` declaration block** | The interface listing in `src/Enigma.Icons/README.md` compiled in its own namespace, plus an adapter that implements the documented shape by delegating to the shipped `Enigma.Icons.IIconSet` — so a member-for-member mismatch would be a compile error. | Compiles; shapes match. |
| **Extension-method signature listing** | Each of `ToGeometry` / `ToDrawing` / `ToDrawingImage` bound to a `Func<>` of exactly the documented shape. | Binds; signatures match `IconGlyphExtensions`. |
| **XAML samples** | Throwaway Avalonia 12.0.4 project; every XAML sample from all four READMEs placed in one `.axaml` under the single documented `ei` prefix and run through Avalonia's XAML compiler. | **Build succeeded — 0 Warning(s), 0 Error(s)** (checked by warning count, per the CLAUDE.md `AVLN*` caveat). |
| **Gallery cross-check** | Samples compared against `samples/Enigma.Icons.Avalonia.Gallery/MainWindow.axaml`, which is known to load. | Same prefix, same positional-argument form, same property names. |

The XAML harness initially failed with `AVLN2100` on `{Binding MyIconSet}`: Avalonia 12 compiles
bindings and needs an `x:DataType`. That was a **gap in the harness, not in the README** — the
gallery declares `x:DataType` at its root for the same reason, and a README binding fragment always
presupposes a host view. A stub ViewModel exposing `IIconSet? MyIconSet` was added, which also
verified that the bound property's type is assignable to `Icon.IconSet`.

### Pack wiring (`dotnet pack -c Release`, all three projects)

All three packed and their contents were listed. Versions were **not** bumped (74DC owns versions);
the nupkgs are build output and are not committed. No csproj needed a fix — `<PackageReadmeFile>`,
`<PackageLicenseFile>` and the matching `<None … Pack="true">` items were already correct in all
three.

| Package | `README.md` at root | `LICENSE.md` | `THIRD-PARTY-NOTICES.md` | Images |
|---|---|---|---|---|
| `Enigma.Icons` | ✅ | ✅ | — (correct) | none |
| `Enigma.Icons.Phosphor` | ✅ | ✅ | ✅ (only package that carries it, SPEC §14.2) | none |
| `Enigma.Icons.Avalonia` | ✅ | ✅ | — (correct) | none |

### Link and hygiene audit

| Check | Result |
|---|---|
| Root README relative links resolve in-repo | All 7 targets exist (`src/*/README.md` ×3, `LICENSE.md`, `THIRD-PARTY-NOTICES.md`, `docs/img/gallery.png`, `docs/SPEC.md`). The three 21C4 forward references are discharged. |
| Packed-README link rule (FEATURE-74DC D6a) | The **only** relative links in the three packed READMEs are `LICENSE.md` (all three) and `THIRD-PARTY-NOTICES.md` (Phosphor only) — both packed alongside. Everything unpacked (`docs/**`, `samples/**`, sibling READMEs) is absolute against `https://github.com/josueclement/Enigma.Icons`. |
| No images, no badges in packed READMEs (SPEC §13.1) | Confirmed — zero `![…]`, zero `img.shields.io` in `src/*/README.md`. |
| Phosphor credit line byte-identical in root + Phosphor READMEs (SPEC §14.2) | Confirmed by hash; appears exactly once in each. |
| No size claim or byte figure in any README (SPEC §7.3) | Confirmed. |
| No version number / what's-new callout in any README | Confirmed — the only version-like strings in the root README are the TFM table cells and the single allowed "post-1.0" WPF line. |
| LF + final newline, all four files (SPEC §2) | Confirmed — 0 CRLF lines, last byte `\n` in each. |
