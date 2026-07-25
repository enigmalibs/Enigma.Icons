# FEATURE-21C4 — Solution scaffolding & shared config

**Status:** DONE · Single-phase · Built directly on `main` (bootstrap exception, SPEC §16.1)

## Summary

Stood up the `Enigma.Icons` repository skeleton. Initialized the git repository on `main`, created
the project-less solution file and the shared build configuration (SDK pin, MSBuild defaults,
Central Package Management), copied in the three text-hygiene config files, and wrote the five
root documentation files. No product code was written — the deliverable is a solution that builds
clean with **zero projects**, plus the config and documentation every later work item builds on.

`Directory.Build.props`, `Directory.Packages.props`, `global.json` and `LICENSE.md` were verified
**byte-identical** to their verbatim SPEC blocks (§3.1, §3.2, §3.3, §14.1) by extracting each fenced
block from `docs/SPEC.md` and comparing programmatically — not by eye.

**Incremental-slnx contract (SPEC §3.4), restated as the plan requires.** This item appended the
four `<Folder>` elements and **zero** `<Project>` entries. Later items append only their own rows:
24DD adds `src/Enigma.Icons` + `tests/Enigma.Icons.UnitTests`; 2DDE adds
`tools/Enigma.Icons.Generator`; 3950 adds `src/Enigma.Icons.Phosphor` +
`tests/Enigma.Icons.Phosphor.UnitTests`; 3ADD adds `src/Enigma.Icons.Avalonia` +
`tests/Enigma.Icons.Avalonia.UnitTests`; 469B adds the gallery and reaches the eight-entry end
state. The slnx must reference only projects that exist, so it stays buildable at every step.

## Files/modules touched

### Created — root config
| File | Provenance |
|---|---|
| `global.json` | verbatim SPEC §3.1 (141 B) |
| `Directory.Build.props` | verbatim SPEC §3.2 (697 B) |
| `Directory.Packages.props` | verbatim SPEC §3.3 (1,284 B) |
| `Enigma.Icons.slnx` | initial project-less form — 4 folders, 0 projects |
| `.gitignore` | `cp` from `docs/reference/config/gitignore` (6,238 B, `cmp` exit 0) |
| `.editorconfig` | `cp` from `docs/reference/config/editorconfig` (9,465 B, `cmp` exit 0) |
| `.gitattributes` | `cp` from snapshot (653 B) + one appended line → 669 B |

### Created — root documentation
| File | Notes |
|---|---|
| `README.md` | Landing-page skeleton: intro, three packages with NuGet + MIT badge pairs, when-to-use table, TFM table, XAML + C# quick start, Phosphor credit, forward links to the three per-package READMEs, licence link. Rewritten wholesale by FEATURE-718F. |
| `LICENSE.md` | Exact MIT text of SPEC §14.1, bold disclaimer included. |
| `RELEASENOTES.md` | Skeleton — three `## <PackageId> v1.0.0 Release Notes` sections, bodies empty placeholders naming FEATURE-74DC. No WPF section. |
| `THIRD-PARTY-NOTICES.md` | **Complete**, not a skeleton. Preamble (Phosphor Icons 2.1.1, phosphoricons.com, derivation statement) + the MIT text `cat`-appended from `docs/reference/phosphor/LICENSE`, verified byte-identical to the snapshot. |
| `CLAUDE.md` | Build/test/pack/generate commands (incl. the two-step extract-then-run of §8.1 and the `--check` gate of §8.5), three-package architecture map with the tool and sample, the eleven SPEC §2 hard rules, and the `docs/SPEC.md` / `docs/roadmap.md` pointers. |

### Created — directories
`src/`, `tests/`, `samples/`, `tools/` — all empty, **no `.gitkeep`** per the plan. Git does not
track empty directories, so none appears in the first commit until a later item adds a project
file. Expected, not a defect.

### Modified
- `docs/roadmap.md` — FEATURE-21C4 row `TODO` → `IN PROGRESS` → `DONE`.
- `docs/plan/FEATURE-21C4.md` — status header `TODO` → `IN PROGRESS` → `DONE`.
- `docs/SPEC.md` — **documentation freshness sweep.** Added a blockquote to §3.5 recording that the
  snapshot `.gitignore`'s `[Rr]elease/` rule excludes `docs/reference/release/`, and that the
  template is tracked only via the one-time force-add (see deviation 4). Neither §1 nor §3.5
  mentioned the interaction, and FEATURE-74DC would have hit it.

### Deleted
None.

## Deviations & follow-ups

1. **`.gitattributes` carries one appended line — the plan's single sanctioned deviation.**
   `*.tar.gz binary`, appended after `*.snk binary`. Verified as a split check rather than a plain
   `cmp`: the first 653 bytes are byte-identical to the snapshot and the remainder is exactly
   `*.tar.gz binary\n`. **The line is optional** — `* text=auto` already classifies the archive as
   binary — so it may be dropped with no behaviour change.

2. **No `feature/…` branch was created (bootstrap exception, SPEC §16.1).** No repository existed
   when this item ran, so `git init -b main` both created the repo and *was* the branch creation,
   satisfying the dev-workflow "branch from `HEAD`" rule. All work happened directly on `main`. The
   `feature/feature-21c4-solution-scaffolding` name in the plan header is nominal only and was
   deliberately not created. Every subsequent item branches normally.

3. **The "zero warnings" acceptance criterion cannot be met literally by a project-less solution —
   accepted as a transient bootstrap artifact (user-approved).** `dotnet build Enigma.Icons.slnx`
   succeeds (exit 0) but emits one warning:
   `NuGet.targets(196,5): warning : Unable to find a project to restore!`. This is a NuGet **restore**
   warning, not a compiler warning, so `TreatWarningsAsErrors` never sees it. Verified that it
   disappears the moment the first project exists, and that `dotnet build --no-restore` reports
   0 warnings / 0 errors. **No configuration was changed to suppress it** — suppressing via
   `HideWarningsAndErrors` would have added a property outside the SPEC §3.2 verbatim block and
   would globally silence genuine restore errors in every later item. **Self-resolves at
   FEATURE-24DD; no follow-up action required.**

4. **`docs/reference/release/` is excluded by `.gitignore` — the first commit needs a force-add.**
   `.gitignore:22`'s stock Visual Studio build-output rule `[Rr]elease/` matches the directory, so
   `docs/reference/release/RELEASE.template.md` would be **silently dropped** from the first commit.
   That file is listed in SPEC §1's target tree and the roadmap guarantees `docs/` is self-contained;
   **FEATURE-74DC depends on it** to write `docs/RELEASE.md` without the `dotnet-release` skill.
   A nested `.gitignore` negation was tested and **cannot** rescue it — git never descends into an
   excluded directory. Resolution chosen by the user: keep `.gitignore` byte-identical and force-add
   the file once —

   ```bash
   git add -f docs/reference/release/RELEASE.template.md
   ```

   Because gitignore applies only to *untracked* files, this is a one-time action: once tracked, the
   file behaves normally forever after. **Verify it is present in the first commit before starting
   FEATURE-24DD.**

5. **Plan verification step 14.1 used an unusable command; substituted the correct one.** The plan
   specifies `git rev-parse --abbrev-ref HEAD` → `main`, but that fails on an **unborn** `HEAD`
   (`fatal: ambiguous argument 'HEAD'`) because the repository has no commits yet. Used
   `git symbolic-ref --short HEAD` (and cross-checked `git branch --show-current`), both printing
   `main`. A documentation-only correction to the plan's verification recipe — the deliverable is
   unaffected. It becomes valid once the first commit exists.

6. **Line endings — nothing to report.** All nine authored text files are LF with a final newline
   (CR count 0 across the board). The two byte-copied snapshots that end without a final newline
   (`.gitignore`, 6,238 B ending `0x64`) are correct as-is per SPEC §3.5's explicit precedence rule:
   byte identity wins over §2 rule 7 for the copied config files. **No CRLF recommendation.**

7. **Minor, informational:** `docs/done/.gitkeep` (carried in by the user) is now redundant, since
   this completion doc is the first real file in `docs/done/`. Left untouched — out of scope.

8. **Root-README overlap with FEATURE-718F is sanctioned, not a deviation** (SPEC §13.2): sole
   ownership governs the *shipped* state, so this item's first-cut README — badges, package list and
   TFM table included — is explicitly permitted. A release audit must not flag it.

9. **CPM pins remain unverified**, as the plan states. Nothing restores in this item, so SPEC §3.3's
   "verify at restore time" obligation could not be discharged here. It lands on the first item to
   reference each group: **FEATURE-3ADD** (Avalonia group — note `Avalonia.Headless.XUnit` 12.0.4 is
   *not* in the local cache and must resolve from nuget.org, else pin the whole group at 12.0.2),
   **FEATURE-24DD** (`xunit.v3`), **FEATURE-469B** (the gallery pair).

## Build/test evidence

Definition of Done criteria 1–2 are satisfied in their code-less form, as the plan's acceptance
criteria specify: the project-less solution builds clean and every config/doc file is well-formed by
inspection. **No test project exists yet**, so the "whole suite green" criterion is satisfied
**vacuously** — there was nothing to test.

**1. Repository on `main`**
```
$ git symbolic-ref --short HEAD
main
```

**2. Config byte identity**
```
$ cmp docs/reference/config/gitignore .gitignore                        → exit 0
$ cmp docs/reference/config/editorconfig .editorconfig                  → exit 0
$ head -c 653 .gitattributes | cmp - docs/reference/config/gitattributes → exit 0
  remainder (bytes 654-669): *.tar.gz binary\n
```

**3. Verbatim SPEC blocks — extracted from `docs/SPEC.md` and compared programmatically**
```
global.json                IDENTICAL to SPEC §3.1   (141 bytes)
Directory.Build.props      IDENTICAL to SPEC §3.2   (697 bytes)
Directory.Packages.props   IDENTICAL to SPEC §3.3   (1284 bytes)
LICENSE.md                 IDENTICAL to SPEC §14.1  (1074 bytes)
```

**4. Phosphor archive integrity — survived being carried into the tree intact**
```
$ sha256sum docs/reference/phosphor/phosphor-2.1.1-svgs-flat.tar.gz
6dc577a39a5a0cb72984e698ba93e7e3736da99b5884fcb221e564d5244c301e   ← matches reference/phosphor/README.md
size: 1412717 bytes                                                ← matches
```

**5. Well-formed markup**
```
Enigma.Icons.slnx          parsed OK — <Project> entries: 0 | <Folder> entries: 4
global.json                parsed OK (JSON)
Directory.Build.props      parsed OK (XML)
Directory.Packages.props   parsed OK (XML)
```

**6. `THIRD-PARTY-NOTICES.md` licence text is verbatim**
```
$ tail -c 1071 THIRD-PARTY-NOTICES.md | cmp - docs/reference/phosphor/LICENSE → exit 0
```

**7. LF + final newline** — all nine authored files: CR count 0, last byte `\n`
(`global.json`, `Directory.Build.props`, `Directory.Packages.props`, `Enigma.Icons.slnx`,
`README.md`, `LICENSE.md`, `RELEASENOTES.md`, `THIRD-PARTY-NOTICES.md`, `CLAUDE.md`).

**8. Build**
```
$ dotnet build Enigma.Icons.slnx
Build succeeded.
/home/jo/.dotnet/sdk/10.0.103/NuGet.targets(196,5): warning : Unable to find a project to restore!
    1 Warning(s)
    0 Error(s)
EXIT=0

$ dotnet build --no-restore Enigma.Icons.slnx
Build succeeded.
    0 Warning(s)
    0 Error(s)
EXIT=0
```
The single warning is the transient zero-project restore artifact of deviation 3 — not a compiler
warning, and gone as soon as FEATURE-24DD adds the first project.

**9. Empty directories** — `src/`, `tests/`, `samples/`, `tools/` each contain 0 entries.
