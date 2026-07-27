# Enigma.Icons — Roadmap

Single persistent registry of every tracked work item. Summary only — full details live in the
linked plan files (`docs/plan/<ID>.md`); completion records in `docs/done/<ID>.md`. The shared
build specification every plan references is `docs/reference/`-backed `docs/SPEC.md`.

Status vocabulary: `TODO`, `IN PROGRESS`, `DONE`, `ABANDONED`.
Row order is the intended build order — `/build` surfaces the topmost `TODO` first.

| ID           | Title                                                                        | Status | Plan                      |
|--------------|------------------------------------------------------------------------------|--------|---------------------------|
| FEATURE-21C4 | Solution scaffolding & shared config (git init main, slnx, props, root docs)  | DONE   | docs/plan/FEATURE-21C4.md |
| FEATURE-24DD | Enigma.Icons base library (model, SVG parser, IIconSet, SvgIconSet) + UnitTests | DONE   | docs/plan/FEATURE-24DD.md |
| FEATURE-2DDE | Asset generator tool + generated Phosphor resources and enum                  | DONE   | docs/plan/FEATURE-2DDE.md |
| FEATURE-3950 | Enigma.Icons.Phosphor package + UnitTests (full-corpus integrity)             | DONE   | docs/plan/FEATURE-3950.md |
| FEATURE-3ADD | Enigma.Icons.Avalonia (extensions, markup extensions, Icon control) + UnitTests | DONE   | docs/plan/FEATURE-3ADD.md |
| FEATURE-469B | Icon gallery sample app                                                      | DONE   | docs/plan/FEATURE-469B.md |
| FEATURE-718F | Documentation (root + 3 packed READMEs)                                      | DONE   | docs/plan/FEATURE-718F.md |
| FEATURE-74DC | Release preparation & NuGet publish runbook (v1.0.0 ×3)                      | DONE   | docs/plan/FEATURE-74DC.md |
| FEATURE-6FA1 | Enigma.Icons.Wpf renderer package — **DEFERRED post-1.0, do not build yet**   | TODO   | docs/plan/FEATURE-6FA1.md |
| CODE-REVIEW-1FD4 | Post-1.0 review fixes (6 accepted findings, 4 phases)                    | IN PROGRESS | docs/plan/CODE-REVIEW-1FD4.md |
| - PHASE01    | [Medium] Icon.Render re-parses geometry every render pass                    | DONE   | (in CODE-REVIEW-1FD4.md)  |
| - PHASE02    | [Medium] PhosphorIconSet variant lookup diverges from the IIconSet contract  | TODO   | (in CODE-REVIEW-1FD4.md)  |
| - PHASE03    | [Low] SvgIconSet.FromDirectory follows symlinked .svg files out of its root  | TODO   | (in CODE-REVIEW-1FD4.md)  |
| - PHASE04    | [Low] Documentation and cosmetic sweep (byte count, IconNames doc, Array.Empty) | TODO | (in CODE-REVIEW-1FD4.md) |
| FEATURE-1608 | docs/internals.html — maintainer's how-it-works explainer                    | TODO   | docs/plan/FEATURE-1608.md |

`FEATURE-21C4` … `FEATURE-6FA1` are single-phase `FEATURE`s — no phase rows. `CODE-REVIEW-1FD4` is
multi-phase: one phase per accepted review finding, ordered highest-severity first.

> **`CODE-REVIEW-1FD4` and `FEATURE-1608` are post-1.0 items, planned 2026-07-27.** They come after
> the deferred `FEATURE-6FA1` in row order but are **buildable now** — 6FA1 is the one row `/build`
> must skip. Neither changes a package version: the review fixes fold into the **unpublished** 1.0.0
> (no git remote, nothing on nuget.org), so `RELEASENOTES.md`, `PackageReleaseNotes` and the root
> README callout stay exactly as `FEATURE-74DC` wrote them, and the stale `artifacts/*.nupkg` are
> simply re-packed at publish time per `docs/RELEASE.md`.

> **`FEATURE-6FA1` is not buildable work in this line.** It is a placeholder plan recording decisions
> already made for a future WPF sibling (SPEC §17). `/build` must **skip** it: it is not part of the
> 1.0.0 release, it holds no code, and WPF cannot be built or run on the planning machine (Linux).
> The last *buildable* item is `FEATURE-74DC`. Before 6FA1 is ever started its plan must be
> re-validated against the then-current .NET/WPF landscape.

## Sequencing & dependencies

1. **FEATURE-21C4** first — creates the repository, the solution, the shared build config, and the
   root docs everything else builds on. Special case: `git init -b main` *is* the branch creation,
   so this item works directly on `main` with no `feature/…` sub-branch (SPEC §16.1).
2. **FEATURE-24DD** — the framework-agnostic model, SVG parser, and icon-set abstraction. Every
   later library depends on it.
3. **FEATURE-2DDE** — the generator tool, and the generated `.dat` resources + `PhosphorIcon` enum
   it writes into `src/Enigma.Icons.Phosphor/`. Depends on 24DD for the layer model the resource
   format mirrors. The generated files are inert committed data at this point (SPEC §16 note).
4. **FEATURE-3950** — the `Enigma.Icons.Phosphor` package that embeds and serves those resources,
   plus the full-corpus integrity tests that validate the generator's output across all 9,072
   glyphs.
5. **FEATURE-3ADD** — the Avalonia rendering layer. Depends on 3950 for the strongly-typed
   `PhosphorIcon`/`PhosphorWeight` surface its markup extensions and `Icon` control expose.
6. **FEATURE-469B** — the gallery sample. Depends on 3ADD; it is the visual verification that unit
   tests cannot provide, and it produces the README screenshot material 718F uses.
7. **FEATURE-718F** — the four READMEs. Placed after the gallery so the documented API is the
   shipped API and the screenshots exist.
8. **FEATURE-74DC** — finalizes versions, release-notes bodies, `PackageReleaseNotes`, and
   `docs/RELEASE.md`, then **prints** (never runs) the pack/tag/push runbook for all three packages
   at 1.0.0.
9. **FEATURE-6FA1** — the WPF sibling, deliberately **deferred to post-1.0**. Its plan file is a
   placeholder recording the design constraints already satisfied for it (SPEC §17); it holds no
   code and is not part of the 1.0.0 release. WPF cannot be built or run on the planning machine.

Branches are cut from `HEAD` at `/build` time as `feature/<id-lowercased>-<slug>` (each plan's
header carries the exact name), except FEATURE-21C4 (see 1 above).

## Reference material

`docs/` is self-contained: the solution can be rebuilt from A to Z with nothing but this directory.

| Path | What it is |
|---|---|
| `docs/SPEC.md` | The master build specification. Authoritative over any plan file. |
| `docs/reference/config/{gitignore,gitattributes,editorconfig}` | Byte-identical snapshots of the house root config files, copied from `/home/jo/Dev/Enigma.Core`. |
| `docs/reference/release/RELEASE.template.md` | Snapshot of the `dotnet-release` skill's runbook template, so FEATURE-74DC can write `docs/RELEASE.md` without the skill. |
| `docs/reference/phosphor/phosphor-2.1.1-svgs-flat.tar.gz` | Pinned upstream artwork — 9,072 flat SVGs, 1.35 MiB, sha256 recorded in its README. |
| `docs/reference/phosphor/LICENSE` | Phosphor Icons' MIT licence, verbatim. |
| `docs/reference/phosphor/README.md` | Snapshot provenance, layout, verification, and refresh procedure. |
