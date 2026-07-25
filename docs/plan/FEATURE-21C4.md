**Status:** DONE · Single-phase · Built directly on `main` (bootstrap exception, SPEC §16.1) ·
Completion record: `docs/done/FEATURE-21C4.md`

# FEATURE-21C4 — Solution scaffolding & shared config

## Objective

Stand up the empty, buildable `Enigma.Icons` repository skeleton: initialize the git repo on `main`,
create the solution file in its project-less form and the shared build configuration (SDK pin,
MSBuild defaults, Central Package Management), copy in the text-hygiene config, and write the
root-level documentation (README landing page, `LICENSE.md`, `RELEASENOTES.md` skeleton,
`THIRD-PARTY-NOTICES.md`, `CLAUDE.md`). **No product code is written here.** The deliverable is a
solution that builds clean with **zero projects** plus a set of well-formed config/doc files that
every later work item builds on. Produces exactly what SPEC §16 lists for this ID.

## Context

This is the **first** build of the solution and is special in two ways, both of which must be
honored at `/build` time:

1. **No git repo exists yet.** The working tree currently contains only `docs/` (`SPEC.md`,
   `roadmap.md`, the `plan/` and `done/` folders, and the `reference/` snapshots —
   `reference/config/`, `reference/phosphor/`, and `reference/release/RELEASE.template.md`), carried
   in by the user. The very first action of this build is `git init -b main`, which creates the
   repository **and** its `main` branch. That satisfies the normal dev-workflow "branch from HEAD"
   rule — the init *is* the branch creation (SPEC §16.1). **No separate `feature/...` sub-branch is
   cut for this bootstrap item; all work happens directly on `main`.** The
   `feature/feature-21c4-solution-scaffolding` name in the header above is the nominal dev-workflow
   name only; it is **not created**, because there is no pre-existing HEAD to branch from. Every
   *subsequent* work item does cut its own `feature/...` branch from HEAD as usual (roadmap
   "Sequencing & dependencies" item 1).

2. **`docs/` is committed by this bootstrap.** The SPEC, roadmap, plan files, and the reference
   snapshots already present in the working tree — including the 1.35 MiB
   `docs/reference/phosphor/phosphor-2.1.1-svgs-flat.tar.gz` binary archive — are picked up by
   `git init` and land in the first commit alongside the scaffolding produced here. `docs/` is a
   planning aid and is **never** packed into any NuGet package (SPEC §1).

The end state is three packable sibling projects, three test projects, a gallery sample, and a
generator tool — see the target tree in SPEC §1 and the work-item map in SPEC §16. This item builds
none of them; it lays the ground.

## Scope

### In scope
- `git init -b main` (repo + branch creation; see Context).
- Root config: `global.json`, `Directory.Build.props`, `Directory.Packages.props` — verbatim per
  SPEC §3.1 / §3.2 / §3.3.
- `Enigma.Icons.slnx` in its **initial, project-less** form: the four solution folders `/src/`,
  `/tests/`, `/samples/`, `/tools/`, and **zero** `<Project>` entries (SPEC §3.4
  incremental-growth contract).
- `.gitignore` and `.editorconfig` copied **byte-identical** from the snapshots (SPEC §3.5).
- `.gitattributes` copied byte-identical from the snapshot **plus** the single appended
  `*.tar.gz binary` line (SPEC §3.6 — the only permitted deviation).
- Root `README.md` — landing page skeleton (SPEC §13; full polish is FEATURE-718F).
- `LICENSE.md` — exact MIT text of SPEC §14.1.
- `RELEASENOTES.md` — skeleton, three sections, bodies empty (SPEC §13).
- `THIRD-PARTY-NOTICES.md` — **complete**, not a skeleton (SPEC §14.2).
- `CLAUDE.md` — build/test/pack/generate commands, three-package map, the hard rules, SPEC pointer
  (SPEC §13).
- Empty `src/`, `tests/`, `samples/`, `tools/` directories.

### Out of scope (deferred to the items that own them)
- **Any `.csproj`, any `.cs`, any `.axaml`** — FEATURE-24DD / 2DDE / 3950 / 3ADD / 469B own theirs.
- **Appending any `<Project>` entry to the slnx** — each later item appends only its own
  (SPEC §3.4 table).
- **The three packed per-package `README.md` files** — each written as a first cut by its owning
  packable item (FEATURE-24DD / FEATURE-3950 / FEATURE-3ADD) and finalized by FEATURE-718F (SPEC §13).
- **`RELEASENOTES.md` body content** and per-csproj `<PackageReleaseNotes>` — FEATURE-74DC.
- **`docs/RELEASE.md`** (the `dotnet-release` runbook) — FEATURE-74DC.
- **Any polish of the root README** beyond the initial landing page — FEATURE-718F.
- **Extracting the Phosphor archive, or generating any asset** — FEATURE-2DDE.

## Design

All files are written to the **repo root** (`/home/jo/Dev/Enigma.Icons/`) unless a path says
otherwise. Every text file authored here is LF with a final newline; zero-warning build rules,
CPM, and explicit usings are as stated in SPEC §2 — do not restate them in the files.

### 1. `git init -b main`
First action, run at the repo root. Verify afterward: `git rev-parse --abbrev-ref HEAD` prints
`main`. Do **not** create a `feature/...` branch (see Context). Do **not** commit — per SPEC §2.9
the user owns all commits; this build stages the tree and prints a suggested commit message at the
end.

### 2. `global.json`
Reproduce **verbatim from SPEC §3.1**. LF + final newline.

### 3. `Directory.Build.props`
Reproduce **verbatim from SPEC §3.2**. Add **no** property not in that block — in particular no
`ImplicitUsings` (it is set per csproj, always `disable`, so the value is visible where it applies),
and no `GenerateDocumentationFile`, `TargetFramework(s)`, or packaging metadata.

### 4. `Directory.Packages.props`
Copy the SPEC §3.3 block **verbatim, byte-for-byte**. Do **not** restate or re-derive its contents
here: §3.3 is the single place a version literal appears, and the acceptance criterion is byte
identity — a second listing would only be a second, driftable source of truth. The pins are present
now even though no project consumes them yet; that is fine — CPM only *supplies* versions to
projects that reference a package. Add **nothing** else: no `System.Memory`, `Microsoft.Bcl.*`, or
polyfill pin unless a concrete `netstandard2.0` compile error demands one, which cannot happen in
this item because nothing compiles (SPEC §3.3 closing note).

The "verify at restore time, do not assume" box in SPEC §3.3 cannot be exercised here — no project
restores yet, so nothing resolves these versions. That obligation lands on the first item that
actually references each package (FEATURE-3ADD for the Avalonia group, FEATURE-24DD for `xunit.v3`,
FEATURE-469B for the gallery pair). Do **not** bump anything here on a hunch; ship the SPEC's values.

### 5. `Enigma.Icons.slnx` — **initial project-less form**
The SPEC §3.4 listing is the **end state** (eight `<Project>` entries), reached only after
FEATURE-469B. Scaffolding creates the file with the four solution folders and **no `<Project>`
children**:

```xml
<Solution>
  <Folder Name="/src/" />
  <Folder Name="/tests/" />
  <Folder Name="/samples/" />
  <Folder Name="/tools/" />
</Solution>
```

**Incremental-growth contract (SPEC §3.4; restate in the completion doc).** This item appends the
four `<Folder>` elements and **zero** `<Project>` entries. Later items append only their own rows
per the SPEC §3.4 table; FEATURE-469B is the one that reaches the end state. The slnx must reference
**only projects that exist** at that point so it stays buildable at every step. Empty `<Folder>`
elements are valid and `dotnet build` reads them without complaint.

### 6. `.gitignore` and `.editorconfig` (SPEC §3.5)
Copy **byte-identical** from the snapshots to the dotted root names:
- `docs/reference/config/gitignore` → `.gitignore` (6,238 bytes)
- `docs/reference/config/editorconfig` → `.editorconfig` (9,465 bytes)

Use a byte copy (`cp`), not an editor round-trip, then verify with
`cmp docs/reference/config/gitignore .gitignore` and the editorconfig equivalent — **exit 0 is the
authority**. Do not reformat, do not re-wrap, do not add or remove a trailing blank line: byte
identity wins over any other formatting instinct (see Notes / risks on `.gitignore`'s missing final
newline). `.editorconfig` already covers this solution's file types including
`[*.{xml,axaml,xaml}] indent_size = 2`, so nothing is appended to it.

### 7. `.gitattributes` (SPEC §3.6)
Copy `docs/reference/config/gitattributes` → `.gitattributes` (653 bytes), then append exactly one
line to the **binary block** at the end of the file, after `*.snk binary`:

```gitattributes
*.tar.gz binary
```

Result: 653 bytes + that line, ending LF. This is the **only** permitted deviation from a
byte-identical copy in this item and it **must** be recorded in the completion doc. Verification is
therefore not a plain `cmp`: assert that the first 653 bytes are byte-identical to the snapshot
(`head -c 653 .gitattributes | cmp - docs/reference/config/gitattributes`) and that the remainder is
exactly `*.tar.gz binary\n`. Note in the completion doc that the line is optional — `* text=auto`
already detects the archive as binary — so the user may drop it with no behaviour change.

`.gitattributes` carries `* text=auto eol=lf`, which governs line endings for the whole repo from
the first commit. Per SPEC §3.6 line endings are never a work item; there is nothing to normalize.

### 8. Root `README.md` — landing page skeleton (SPEC §13)
A real landing page, polished later by FEATURE-718F. Sections, in order:
- **Title + one-paragraph intro:** what the `Enigma.Icons` umbrella is — a framework-agnostic icon
  model and SVG parser, an interchangeable Phosphor asset pack, and an Avalonia renderer, with
  `Enigma.Icons` carrying **zero dependencies at all** and `Enigma.Icons.Phosphor` **zero
  third-party dependencies** — it declares exactly one package dependency, the sibling
  `Enigma.Icons` (SPEC §0).
- **The three packages**, one line each with the badge pair, using the exact badge template of
  SPEC §13.1 substituted per `<PackageId>`. Badges belong **here and only here** — the three packed
  per-package READMEs — written by FEATURE-24DD / FEATURE-3950 / FEATURE-3ADD, finalized by
  FEATURE-718F — carry none (SPEC §13.1):
  - `Enigma.Icons` — the model, SVG parser, `IIconSet` abstraction, and bring-your-own-SVG support.
  - `Enigma.Icons.Phosphor` — 1,512 Phosphor icons × 6 weights as embedded resources.
  - `Enigma.Icons.Avalonia` — `Geometry`/`Drawing`/`DrawingImage` conversion, markup extensions, and
    the `Icon` control.
- **When to use which:** base = your own artwork or another family; Phosphor = the batteries-included
  artwork; Avalonia = what you reference from an Avalonia app (it brings the other two).
- **Supported target frameworks:** the TFM column of the SPEC §0 table for the three 1.0.0 packages.
  Mention `Enigma.Icons.Wpf` as deferred post-1.0 (SPEC §17) — one line, no promises.
- **Quick start:** one XAML snippet and one C# snippet, taken from SPEC §10.3 (the `ei:Icon` element
  and the `PhosphorIconSet.Instance.GetGlyph(...).ToGeometry()` call). Keep it to those; the full
  usage story is FEATURE-718F's.
- **Phosphor credit:** the exact credit line of SPEC §14.2, plus a link to
  `THIRD-PARTY-NOTICES.md`.
- **Links to the per-package READMEs** — `src/Enigma.Icons/README.md`,
  `src/Enigma.Icons.Phosphor/README.md`, `src/Enigma.Icons.Avalonia/README.md`. These are
  **forward references**: the files are created by FEATURE-718F and the links resolve then. Intended
  and acceptable at scaffolding time.
- **Licence** line pointing at `LICENSE.md` (MIT).

### 9. `LICENSE.md` (SPEC §14.1)
Reproduce the **exact** text of the SPEC §14.1 block verbatim — the `MIT License` heading line, the
`Copyright (c) 2026 Josué Clément` line, the permission paragraph, the inclusion paragraph, and the
**bold** all-caps warranty-disclaimer paragraph exactly as shown, including the `**` markers and the
SPEC's line wrapping. LF + final newline. (Later items pack this file into all three packages per
SPEC §14.1; no packing wiring happens here.)

### 10. `RELEASENOTES.md` — skeleton (SPEC §13)
One file: a top-level title plus one `## <PackageId> v1.0.0 Release Notes` section per package, in
package order `Enigma.Icons`, `Enigma.Icons.Phosphor`, `Enigma.Icons.Avalonia`, each body an empty
placeholder comment naming FEATURE-74DC as the owner. No `Enigma.Icons.Wpf` section — it is deferred
and gets no 1.0.0 (SPEC §0, §17). Newest-first ordering applies to sections added after 1.0.0.

### 11. `THIRD-PARTY-NOTICES.md` — **complete** (SPEC §14.2)
Unlike `RELEASENOTES.md` this file is **finished here**, because it is a licence obligation the
moment the artwork lands in the repo — and the pinned archive lands in this very commit. Two parts:
1. A short preamble naming Phosphor Icons, version **2.1.1**, and <https://phosphoricons.com>, and
   stating that the embedded `.dat` resources of `Enigma.Icons.Phosphor` are derived from that
   artwork.
2. The **verbatim** MIT text of `docs/reference/phosphor/LICENSE` (`Copyright (c) 2020 Phosphor
   Icons`). Copy it out of that file — do not retype it — and preserve its hard line wrapping
   exactly; do not reflow it to fit a markdown ruler.

No mention of packing here; FEATURE-3950 wires the `<None Include="..\..\THIRD-PARTY-NOTICES.md" …>`
item into the Phosphor csproj (SPEC §14.2).

### 12. `CLAUDE.md` (SPEC §13)
Concise agent-instruction file, four parts:
- **Commands:** build (`dotnet build Enigma.Icons.slnx`), test (`dotnet test Enigma.Icons.slnx`),
  pack (`dotnet pack src/<Project>/<Project>.csproj -c Release`), and **generate** — the two-step
  extract-then-run invocation of SPEC §8.1 plus the `--check` staleness gate (SPEC §8.5). Note the
  MTP-native test runner comes from `global.json` (SPEC §3.1) and that these commands will only do
  something once the projects exist.
- **Architecture map:** `Enigma.Icons` (model + parser + `IIconSet`) ← `Enigma.Icons.Phosphor`
  (artwork pack) ← `Enigma.Icons.Avalonia` (renderer); plus `tools/Enigma.Icons.Generator` writing
  the committed assets into the Phosphor project, and `samples/Enigma.Icons.Avalonia.Gallery`
  (SPEC §0, §1, §16).
- **Hard rules, one line each, referencing SPEC §2:** zero-warning build, zero dependencies at all
  in `Enigma.Icons` and zero **third-party** dependencies in `Enigma.Icons.Phosphor` (its sibling
  reference is not third-party, SPEC §2 rule 10), no `Enum.ToString()`/`Enum.Parse` on hot paths,
  XML doc on every public member, `ImplicitUsings` disabled, CPM (never a `Version=` on a `PackageReference`),
  `.slnx` never `.sln`, LF + final newline, and never commit.
- **Pointer:** `docs/SPEC.md` is authoritative; `docs/roadmap.md` is the work-item registry.

### 13. Empty `src/`, `tests/`, `samples/`, `tools/`
Create all four. **git does not track empty directories**, so none of them appears in the first
commit until a later item adds a project file — expected, not a defect. Do **not** add `.gitkeep`
placeholders; keep the tree to exactly the files SPEC §1 lists. `docs/` already exists, snapshots
included; `docs/img/` is **not** created here — FEATURE-469B creates it when it captures
`docs/img/gallery.png` (SPEC §1, §13).

### 14. Verification pass
Run, and capture the output as build evidence for the completion doc:
1. `git rev-parse --abbrev-ref HEAD` → `main`.
2. `cmp` for `.gitignore` and `.editorconfig` (exit 0), and the split check for `.gitattributes`
   (step 7).
3. `sha256sum docs/reference/phosphor/phosphor-2.1.1-svgs-flat.tar.gz` →
   `6dc577a39a5a0cb72984e698ba93e7e3736da99b5884fcb221e564d5244c301e`, matching
   `docs/reference/phosphor/README.md`. This proves the archive survived being carried into the tree
   intact, before FEATURE-2DDE depends on it.
4. `dotnet build Enigma.Icons.slnx` — succeeds with zero projects and zero warnings.
5. Eyeball each authored file for LF endings and a final newline (`file` / `tail -c 1`).

## Dependencies & ordering

- **No prior work items.** This is the first row of `docs/roadmap.md` and the only row in SPEC §16
  with no dependency. Nothing to build before it.
- It is a hard prerequisite for **every** later item: FEATURE-24DD, 2DDE, 3950, 3ADD, 469B, 718F,
  74DC, and the deferred 6FA1 all depend on the repo, the solution, the shared build config, and the
  root docs created here (roadmap "Sequencing & dependencies" item 1).
- Nothing here depends on the Phosphor archive's *contents*; only its integrity is checked (Design
  step 14.3). Extraction and generation belong to FEATURE-2DDE.

## Acceptance criteria

Definition of Done (dev-workflow) — this item contains **no compilable code and no test project**,
so DoD criteria 1–2 are satisfied by *"the project-less solution builds clean and every config/doc
file is well-formed by inspection"*, evidenced by the Design step 14 run. Criteria 3–5 apply
normally.

- [ ] **Repo on `main`:** `git init -b main` run; `git rev-parse --abbrev-ref HEAD` == `main`; no
      `feature/...` branch created (bootstrap exception, recorded in the completion doc).
- [ ] `global.json`, `Directory.Build.props`, `Directory.Packages.props` present at root and
      **byte-for-byte** identical to SPEC §3.1 / §3.2 / §3.3 respectively — no extra properties, no
      extra `PackageVersion` entries.
- [ ] `Enigma.Icons.slnx` present, well-formed XML, with the four `/src/` `/tests/` `/samples/`
      `/tools/` folders and **zero** `<Project>` entries (initial form, **not** the SPEC §3.4 end
      state).
- [ ] `.gitignore` and `.editorconfig` at root, byte-identical to
      `docs/reference/config/{gitignore,editorconfig}` — `cmp` exits 0 for both.
- [ ] `.gitattributes` at root: first 653 bytes byte-identical to
      `docs/reference/config/gitattributes`, followed by exactly `*.tar.gz binary` + LF (SPEC §3.6);
      the deviation is recorded in the completion doc.
- [ ] Root `README.md` present as a landing page: intro, the three packages with NuGet + MIT badges
      each, when-to-use guidance, TFM list, XAML + C# quick start, the Phosphor credit line, forward
      links to the three per-package READMEs, and a licence link (SPEC §13).
- [ ] `LICENSE.md` present with the **exact** MIT text of SPEC §14.1, bold disclaimer included.
- [ ] `RELEASENOTES.md` present with the three `## <PackageId> v1.0.0 Release Notes` sections
      (`Enigma.Icons`, `Enigma.Icons.Phosphor`, `Enigma.Icons.Avalonia`), bodies empty placeholders,
      no WPF section.
- [ ] `THIRD-PARTY-NOTICES.md` present and **complete**: preamble naming Phosphor Icons 2.1.1 and
      its URL, plus the MIT text of `docs/reference/phosphor/LICENSE` reproduced verbatim
      (`Copyright (c) 2020 Phosphor Icons`, wrapping preserved) (SPEC §14.2).
- [ ] `CLAUDE.md` present: build/test/pack/generate commands, the three-package + tool/sample
      architecture map, the SPEC §2 hard rules, and the `docs/SPEC.md` pointer (SPEC §13).
- [ ] `src/`, `tests/`, `samples/`, `tools/` directories exist and are empty (no `.gitkeep`).
- [ ] Every text file authored by this item uses **LF** and ends with a **final newline** — the
      snapshot copies excepted, where byte identity governs (see Notes / risks).
- [ ] `sha256sum docs/reference/phosphor/phosphor-2.1.1-svgs-flat.tar.gz` ==
      `6dc577a39a5a0cb72984e698ba93e7e3736da99b5884fcb221e564d5244c301e` and size == 1,412,717
      bytes, matching `docs/reference/phosphor/README.md`.
- [ ] **`dotnet build Enigma.Icons.slnx` succeeds with zero warnings** (0 projects → nothing to
      build; capture the output as build evidence). Satisfies DoD criteria 1–2 for this code-less
      item; **no test suite exists yet**, so the "whole suite green" criterion is satisfied
      vacuously and must be stated as such in the completion doc.
- [ ] **Roadmap + this plan flipped to `DONE`** (`docs/roadmap.md` FEATURE-21C4 row; this file's
      status header). (DoD criterion 4.)
- [ ] **Completion doc `docs/done/FEATURE-21C4.md` written** — summary, files touched, deviations
      (the `.gitattributes` extra line; the bootstrap branch exception), build/test evidence.
      (DoD criterion 5.)

## Notes / risks

- **`.gitignore` has no final newline — settled, not a defect.** SPEC §3.5 now states the precedence
  in a dedicated blockquote: *"Byte identity wins over §2 rule 7 for these three files."* Measured:
  `gitignore` is 6,238 B ending `0x64` (`d`) with **no** trailing LF, while `gitattributes` (653 B) and
  `editorconfig` (9,465 B) both end `0x0A`. Copy all three as-is; `cmp` exit 0 is the authority. Do
  **not** add a newline, do not "normalize" the copy, and do not re-take the snapshot upstream.
- **git-init-as-first-action (special case).** There is no repo to branch from; `git init -b main`
  both creates the repo and satisfies the dev-workflow "branch from HEAD" rule. Work stays on
  `main` — do **not** cut `feature/feature-21c4-solution-scaffolding`. Record the deviation in the
  completion doc so it is on the record for the audit trail.
- **Incremental-slnx contract.** A builder who pastes the SPEC §3.4 **end-state** slnx here breaks
  `dotnet build` immediately: it would reference eight csproj files, none of which exist. Ship the
  project-less form of Design step 5 and nothing more.
- **`.gitattributes` deviation is deliberate and reversible.** `*.tar.gz binary` is belt-and-braces
  only — `* text=auto` already classifies the archive as binary — so the user may delete the line
  with no behaviour change. It must still be flagged in the completion doc, because it is the single
  place this item departs from a byte copy.
- **LF discipline is self-enforcing from this commit on.** `.gitattributes` (`* text=auto eol=lf`)
  and `.editorconfig` (`end_of_line = lf`, `insert_final_newline = true`) are both added *in this
  item*, so every later item inherits the rule. Author all files here with LF anyway — the tooling
  is not yet in place when the first bytes are written.
- **`docs/` provenance and packing.** The `docs/` tree (SPEC, roadmap, plans, reference snapshots
  including the 1.35 MiB tar.gz) is carried in by the user and committed by this bootstrap. It is a
  planning aid and is **never** packed into any NuGet package (SPEC §1). The archive is the only
  binary asset the repo tracks.
- **Empty directories & git.** `src/`, `tests/`, `samples/`, `tools/` stay untracked until a later
  item adds a project file — normal git behaviour, not a defect. No `.gitkeep`.
- **Forward-reference README links** to the three per-package READMEs are intentional and resolve
  when FEATURE-718F creates those files. They are not broken links to fix here.
- **Root-README overlap with FEATURE-718F is sanctioned, not a violation.** SPEC §13.2 states that
  sole ownership governs the *shipped* state, so this item's first-cut root README — badges, package
  list, TFM list included — is explicitly not a breach of the §13.2 ownership table; FEATURE-718F
  rewrites the root README wholesale. A compliance check against this item's completion doc must not
  read the overlap as a deviation.
- **CPM pins are unverified at this point.** Nothing restores in this item, so the SPEC §3.3
  "verify at restore time" instruction cannot be discharged here; the first item to reference each
  package group owns that check. If a version turns out to need a bump later, that is a change to
  `Directory.Packages.props` in *that* item, not a defect in this one.
