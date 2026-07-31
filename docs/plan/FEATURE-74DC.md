**Status:** DONE · Single-phase · Built on branch `feature/feature-74dc-release-prep`

# FEATURE-74DC — Release preparation & NuGet publish runbook (v1.0.0 ×3)

## Objective

Bring all three packable `Enigma.Icons` libraries to a publishable **1.0.0** and produce the release
collateral, then **print** (never run) the pack/tag/push runbook for the three packages that ship
together under one tag. Concretely: confirm the target-framework sets and the versions, add
`<PackageReleaseNotes>` to each packable csproj, write the three `RELEASENOTES.md` v1.0.0 bodies,
add the README what's-new callouts, refresh non-coupled NuGet pins, create
`docs/RELEASE.md`, audit the packable-library prerequisites, and verify a Release-configuration
build + test + `dotnet pack` ×3. Follows the `dotnet-release` skill and its **execution boundary:
in-repo edits only; every outward-facing command is printed for the user to run.**

## Context

This is the terminal 1.0.0 item. Per SPEC §0 the three packages ship together at **1.0.0**:

| PackageId | TFMs (SPEC §0) | csproj created by |
|---|---|---|
| `Enigma.Icons` | `netstandard2.0;net8.0;net10.0` | `src/Enigma.Icons/Enigma.Icons.csproj` (FEATURE-24DD) |
| `Enigma.Icons.Phosphor` | `netstandard2.0;net8.0;net10.0` | `src/Enigma.Icons.Phosphor/Enigma.Icons.Phosphor.csproj` (FEATURE-3950) |
| `Enigma.Icons.Avalonia` | `net8.0;net10.0` | `src/Enigma.Icons.Avalonia/Enigma.Icons.Avalonia.csproj` (FEATURE-3ADD) |

`Enigma.Icons.Wpf` is **not** part of this release (SPEC §17, FEATURE-6FA1 — deferred post-1.0).

Everything this item finalizes already exists: the three csprojs (FEATURE-24DD/3950/3ADD), the four
READMEs (FEATURE-718F, SPEC §13), `LICENSE.md` / `THIRD-PARTY-NOTICES.md` / the `RELEASENOTES.md`
skeleton (FEATURE-21C4, SPEC §14). **This item finalizes them for release — it does not recreate
them.** Where a required release element is present it is *confirmed*; where SPEC §16 explicitly
defers it here (`PackageReleaseNotes` ×3, the `RELEASENOTES.md` bodies, `docs/RELEASE.md`) it is
*written*.

There is **no new product code and no new test** in this item. Per the `dev-workflow` no-build/no-test
clause, DoD criteria 1–2 are satisfied by a zero-warning **Release** build, a green **Release** test
run, and a successful `dotnet pack` ×3 with nupkg inspection (stated explicitly in Acceptance).

**slnx:** per the SPEC §3.4 incremental-growth contract, FEATURE-74DC appends **nothing** to
`Enigma.Icons.slnx` — the end state was reached by FEATURE-469B. Do not touch the file.

## Scope

### In scope
- Confirm the three packable csprojs' `<TargetFrameworks>` against the `dotnet-release` normalized
  shape (no edit expected — see D1) and their `<Version>1.0.0</Version>` (D2).
- Packable-library prerequisite audit across all three csprojs (D3).
- Add `<PackageReleaseNotes>` to each of the three csprojs (D4; SPEC §16).
- Write the three `## Enigma.Icons[.X] v1.0.0 Release Notes` bodies in `RELEASENOTES.md` (D5;
  SPEC §13).
- Add the what's-new callout to the **root README only** (SPEC §13.2 — the three packed READMEs get
  none); **confirm** (do not
  re-add) the supported-target-frameworks table FEATURE-718F wrote, updating it only if D1 changed a
  TFM set (D6; SPEC §13.2).
- Dependency refresh in `Directory.Packages.props`: non-coupled bumps applied, the Avalonia coupled
  set held back unless the user opts in (D7).
- `docs/RELEASE.md` from the snapshotted template at `docs/reference/release/RELEASE.template.md`,
  filled for three packages, create-only-if-missing (D8; SPEC §1, §13).
- Release-configuration build + test + `dotnet pack` ×3 verification with nupkg inspection (D9).
- **Print** the pack/tag/push runbook (D10).

### Out of scope
- **Any product source, public API, test, generated asset, or SPEC change.** If the release
  verification finds a defect, it is a new work item — not a silent fix here.
- **Appending to `Enigma.Icons.slnx`** — nothing to append (SPEC §3.4).
- **An MSI profile.** `dotnet-release` makes profiles app-only; all three projects carry a
  `PackageId`, so none applies. The gallery (`samples/…`) and generator (`tools/…`) are
  `IsPackable=false` and are not released either (SPEC §1, §11).
- **NuGet deprecation / unlisting of `PhosphorIconsAvalonia`, any migration guide, any compatibility
  shim, and any update to the `phosphor-icons-avalonia` skill.** Clean break, decided in the
  interview (SPEC §0.1, §17). Do not touch the `/home/jo/Dev/PhosphorIconsAvalonia` repo.
- **Running any outward-facing command**: the merge to `main`, `git tag`, the publish `dotnet pack`,
  `dotnet nuget push`. Those are **printed**; the user runs them. The NuGet API key is never stored,
  committed, or echoed.
- `LICENSE.md`, `THIRD-PARTY-NOTICES.md`, `CLAUDE.md`, `global.json`, `Directory.Build.props`, the
  three config files, and the README *bodies* — owned by FEATURE-21C4 / FEATURE-718F; the only
  README line this item writes is the what's-new callout (D6). The supported-target-frameworks table,
  the gallery screenshot embed and the badges are FEATURE-718F's (SPEC §13.2).

## Design

All files are LF with a final newline; the build is zero-warning under `TreatWarningsAsErrors`;
CPM means no `Version=` on any `PackageReference` (SPEC §2).

### D0. Preconditions (read-only, before any edit)
1. Confirm FEATURE-21C4/24DD/2DDE/3950/3ADD/469B/718F are `DONE` in `docs/roadmap.md` and the
   solution builds and tests green in Debug.
2. `git tag` → expect **empty**. That is why the tag form is bare `X.Y.Z`, not `vX.Y.Z`
   (`dotnet-release` tag-format rule). Re-check rather than assume.
3. Confirm `artifacts/` is git-ignored (it is — `docs/reference/config/gitignore` line 66), so the
   D9 verification pack leaves no untracked files.
4. `git remote -v` → record whether a publish remote exists (see OPEN QUESTION 2). Nothing in the
   SPEC establishes one.

### D1. Target-framework confirmation — expect **no edit**
Target files: `src/Enigma.Icons/Enigma.Icons.csproj`,
`src/Enigma.Icons.Phosphor/Enigma.Icons.Phosphor.csproj`,
`src/Enigma.Icons.Avalonia/Enigma.Icons.Avalonia.csproj`.

Read each `<TargetFrameworks>` and compare against the SPEC §0 sets. Both agnostic packages should
read `netstandard2.0;net8.0;net10.0` and the Avalonia package `net8.0;net10.0` — which is **already**
the `dotnet-release` normalized shape (`netstandard*` preserved; plain-`net` targets exactly the
`net8.0`+`net10.0` LTS pair; no `net` older than 8; no stray `net9.0`). So the expected outcome is:
**verified, unchanged, nothing written.**

If anything has moved, the `dotnet-release` TFM rule is the one in-repo edit that is **proposed and
confirmed before writing**: print the `old → new` set for the affected project, **ask the user and
wait for the OK**, then write the plural `<TargetFrameworks>` element and log the transition in that
package's `RELEASENOTES.md` *Compatibility* sub-section (D5) plus the affected row of FEATURE-718F's
supported-target-frameworks table (D6). A dropped TFM is a compatibility break and must be labelled
as one.

Two guard rails, both from the SPEC, that override any mechanical normalization:
- The two agnostic packages must **keep `netstandard2.0`** — it is the .NET Framework 4.6.2+ floor
  that exists for the deferred WPF sibling (SPEC §15 *Compatibility*, §17).
- `Enigma.Icons.Avalonia` must **not gain `netstandard2.0`** — Avalonia 12 is net8+ (SPEC §0, §10).

Also confirm in passing that `<IsTrimmable>`/`<IsAotCompatible>` are still set exactly as SPEC §10.4
prescribes — a TFM edit is the classic way to break that:
- `Enigma.Icons` and `Enigma.Icons.Phosphor` — the two packages that multi-target `netstandard2.0` —
  keep both properties in a `PropertyGroup` conditioned on `'$(TargetFramework)' != 'netstandard2.0'`.
- `Enigma.Icons.Avalonia` targets only `net8.0;net10.0`, so that condition would be vacuous: it sets
  both **unconditionally**. This asymmetry is **intentional and SPEC-sanctioned** — do **not** flag it
  as a deviation and do **not** "fix" it by adding a no-op condition.

### D2. Version confirmation — `1.0.0` ×3
Same three csprojs. `<Version>1.0.0</Version>` is set at creation by FEATURE-24DD/3950/3ADD;
**verify, do not add a second element.** Only if one is missing or wrong does this item write it —
this item owns version truth for the release. Do **not** add a `<Version>` to the non-packable
projects (`tools/Enigma.Icons.Generator`, `samples/Enigma.Icons.Avalonia.Gallery`, the three test
projects); they are never published.

**Artwork-refresh versioning rule** — delegated here by FEATURE-2DDE and mandated by SPEC §8.4: any
future Phosphor artwork refresh is **at least a MINOR** version bump of `Enigma.Icons.Phosphor`,
because the generated enum's ordinals are positional (adding one icon renumbers every member after
it), never a stable ABI. Not applicable to this 1.0.0 release; it is the rule this item's version
ownership carries forward, and D5 records it in the Phosphor release notes.

### D3. Packable-library prerequisite audit (all three csprojs)
Pulled ahead of D4 so both csproj passes are one edit. Confirm each element, editing only what is
wrong or missing, and record every deviation in the completion doc:

| Element | Required | Authority |
|---|---|---|
| `<PackageId>` | `Enigma.Icons` / `Enigma.Icons.Phosphor` / `Enigma.Icons.Avalonia` | SPEC §0 |
| `<RepositoryUrl>` / `<PackageProjectUrl>` | both `https://github.com/josueclement/Enigma.Icons`; `<RepositoryType>` `git`; **no `<PackageIcon>`** on any package | SPEC §10.5 |
| `<PackageReadmeFile>` | `README.md`, packed via `<None Include="README.md" Pack="true" PackagePath="\" />` (the README is project-local — SPEC §1) | SPEC §13, `dotnet-release` |
| `<PackageLicenseFile>` | `LICENSE.md`, packed via `<None Include="..\..\LICENSE.md" Pack="true" PackagePath="\" />` | SPEC §14.1 (verbatim item) |
| `<None … THIRD-PARTY-NOTICES.md>` | **`Enigma.Icons.Phosphor` only** — `<None Include="..\..\THIRD-PARTY-NOTICES.md" Pack="true" PackagePath="\" />`; must be **absent** from the other two | SPEC §14.2 |
| `<PackageReleaseNotes>` | present — written in D4 | SPEC §16, `dotnet-release` |
| `<IncludeSymbols>` + `<SymbolPackageFormat>` | `true` + `snupkg` | SPEC §10.4 |
| `GeneratePackageOnBuild` | **absent or `false`** — packing is explicit in the release step, per the print-don't-run boundary | SPEC §10.4, `dotnet-release` |
| `<GenerateDocumentationFile>` | `true`, **per packable project** — deliberately *not* in `Directory.Build.props`, so the tests and the gallery do not inherit it; CS1591 as an error is what forces the XML docs on all 1,512 generated enum members | SPEC §10.4, §2.8, §8.4 |
| `<IsPackable>` | not `false` on these three; `false` on gallery + generator | SPEC §1, §11 |

A symbol package **is** mandated: SPEC §10.4 requires `<IncludeSymbols>true</IncludeSymbols>` and
`<SymbolPackageFormat>snupkg</SymbolPackageFormat>` on all three, so one `.snupkg` per package is the
expected published artifact set (D9.3). Add them if a csproj is missing them — no user confirmation
needed. What §10.4 still does not list is `<Title>`/`<Description>`/`<PackageTags>`; see OPEN
QUESTION 1.

Also note, for the D9 nuspec check, what each package's dependency groups must look like — a
`ProjectReference` from a packable project packs as a **package dependency**:
- `Enigma.Icons` — no dependencies at all.
- `Enigma.Icons.Phosphor` — `Enigma.Icons` `1.0.0` only (SPEC §9).
- `Enigma.Icons.Avalonia` — `Enigma.Icons.Phosphor` `1.0.0` + `Avalonia` at the `Directory.Packages.props`
  version (SPEC §10, §3.3).

### D4. `<PackageReleaseNotes>` ×3
One element in the metadata `<PropertyGroup>` of each of the three csprojs: short prose mirroring the
opening of that package's `RELEASENOTES.md` section (D5), **ending with**
`See RELEASENOTES.md for the full details.` (`dotnet-release`). Suggested single-line texts:

- **`Enigma.Icons`** — `Enigma.Icons 1.0.0 — initial release. Framework-agnostic icon model (IconGlyph / IconLayer / IconViewBox), a hardened SVG-subset parser, and the IIconSet abstraction with SvgIconSet for bringing your own .svg files. No third-party dependencies. Targets netstandard2.0, net8.0 and net10.0. See RELEASENOTES.md for the full details.`
- **`Enigma.Icons.Phosphor`** — `Enigma.Icons.Phosphor 1.0.0 — initial release. The Phosphor artwork as an interchangeable asset pack: 1,512 icons in 6 weights (thin, light, regular, bold, fill and the new duotone), the strongly-typed PhosphorIcon enum, and PhosphorIconSet. Artwork is Phosphor Icons 2.1.1 (MIT, © 2020 Phosphor Icons). No third-party dependencies. See RELEASENOTES.md for the full details.`
- **`Enigma.Icons.Avalonia`** — `Enigma.Icons.Avalonia 1.0.0 — initial release. Avalonia rendering for Enigma.Icons: the Icon control (no StyleInclude needed; inherits Foreground and follows theme switches), the {ei:IconGeometry} and {ei:IconImage} markup extensions, and Geometry / Drawing / DrawingImage conversion extensions. Supersedes the retired PhosphorIconsAvalonia package — new identity, no upgrade path. See RELEASENOTES.md for the full details.`

The Phosphor artwork/version/licence mention is **mandatory** here (SPEC §14.2 is an obligation, not
a courtesy).

### D5. `RELEASENOTES.md` bodies ×3
File: `RELEASENOTES.md` (repo root). Keep the H1 the skeleton established and fill the three `##`
sections in SPEC §13 order, headings exactly:

```markdown
## Enigma.Icons v1.0.0 Release Notes
## Enigma.Icons.Phosphor v1.0.0 Release Notes
## Enigma.Icons.Avalonia v1.0.0 Release Notes
```

Each section uses the house sub-section style — **New Features · Compatibility · Dependencies ·
Version**. Required content:

**`Enigma.Icons`**
- *New Features:* initial release, new package identity, no predecessor. The immutable icon model
  (§4), the `SvgIconParser` supported subset **and its documented exclusions** plus the mandatory XML
  hardening (§5.1, §5.2), `IIconSet` and `SvgIconSet`'s four factories for bring-your-own-SVG (§6).
- *Compatibility:* `netstandard2.0`, `net8.0`, `net10.0` — unchanged from creation (or the confirmed
  `old → new` set from D1). State that the `netstandard2.0` floor is deliberate: it keeps .NET
  Framework 4.6.2+ reachable for the deferred WPF sibling, and it is why the public API carries no
  default interface members, no `required`/`init`, and no `Span<T>` (SPEC §15, §17). Trim/AOT-clean
  on the modern TFMs (§10.4).
- *Dependencies:* **none at all** — not merely zero third-party: `Enigma.Icons` declares no package
  dependency on any TFM. A hard constraint, not a preference (SPEC §0, §2 rule 10).
- *Version:* 1.0.0.

**`Enigma.Icons.Phosphor`**
- *New Features:* the artwork as an interchangeable asset pack behind `IIconSet` — **1,512 icons** ×
  **6 weights**, the sixth (**duotone**) being new relative to the retired package's five, which is
  what forced the layered glyph model (SPEC §0). The generated `PhosphorIcon` enum and
  `PhosphorIconNames` lookup tables (no `Enum.ToString`/`Enum.Parse` on hot paths, §2.11, §8.4), six
  embedded `.dat` resources totalling **3,951,294 B (3.77 MiB)** whole-file, of which **3,820,927 B
  (3.64 MiB)** is path data (§7.3); lazy per-weight load with reference-equal glyph caching (§9.1).

  > **Publish NO size-reduction claim.** SPEC §7.3 is explicit: *"It does not save package size. That
  > earlier claim was wrong."* Deflated as one stream the `.dat` corpus is **1,150,587 B** against
  > **1,118,874 B** for the same artwork as raw `.svg` content — i.e. compressed the two formats are a
  > wash and `.dat` is **2.8 % larger**. And because a multi-targeted package ships one assembly per
  > TFM, this nupkg will be **≈3.4–3.5 MB — larger than the retired 2,121,835 B package**, not smaller.
  > Do not write "~45 % below", "≈4.05 MiB", "36 MB", "≈3.74 MB", or "the real win is compression".
  > If size is mentioned at all, state the four non-size reasons the format was chosen (§7.3): six
  > manifest resources instead of 9,072, no runtime XML parse, reviewable six-file refresh diffs, and
  > the layered model duotone requires.
- *Compatibility:* `netstandard2.0`, `net8.0`, `net10.0` (same wording as above). Note the artwork
  pin: **Phosphor Icons 2.1.1**, MIT, © 2020 Phosphor Icons — the licence travels in the packed
  `THIRD-PARTY-NOTICES.md` (SPEC §14.2).
- *Dependencies:* the **prescribed** wording, settled by SPEC §0, §2 rule 10 and §9 — "no third-party
  runtime dependencies; the only package dependency is the sibling `Enigma.Icons` 1.0.0." Dependencies
  *within* the `Enigma.Icons` family are not third-party and do not breach the zero-dependency rule,
  so this sentence is exact rather than a hedge and needs no confirmation before publishing. Say
  explicitly that **`ZiggyCreatures.FusionCache` — the retired package's memoization dependency — has
  been dropped**, replaced by two plain `ConcurrentDictionary` levels (SPEC §0, §9.1).
- *Version:* 1.0.0. State the artwork-refresh rule here too: `PhosphorIcon` ordinals are **positional,
  not a stable ABI**, so consumers must persist/transmit `PhosphorIconNames.ToKebabCase(icon)` and read
  it back with `TryParse` — never the numeric value — and any future artwork refresh is at least a
  **minor** version bump of this package (SPEC §8.4).

**`Enigma.Icons.Avalonia`**
- *New Features:* the `Icon` control deriving from `Control` — **no `StyleInclude` in `App.axaml`**,
  no shipped XAML, `Foreground` inherited from the enclosing `TextElement` scope so it follows bound
  brushes and theme switches (the capability a markup extension structurally cannot have, SPEC §10.2);
  the `{ei:IconGeometry}` / `{ei:IconImage}` markup extensions (§10.3); the `ToGeometry` /
  `ToDrawing` / `ToDrawingImage` extensions with the documented `ToGeometry` opacity-loss caveat
  (§10.1).
- *Compatibility:* `net8.0`, `net10.0` — **no `netstandard2.0`** (Avalonia 12 is net8+). Then the
  supersede note, in full: this package **supersedes the retired `PhosphorIconsAvalonia` 1.2.0**
  under a **new package identity and new API names**; there is **no upgrade path, no migration guide,
  no compatibility shim, and no NuGet deprecation of the old package** — a deliberate clean break
  (SPEC §0.1, §17). A consumer changes the `PackageReference` id, the XAML namespace, and the type
  names by hand: `Icon` → `PhosphorIcon`, `IconType` → `PhosphorWeight`, `IconService` →
  `PhosphorIconSet.Instance`, `IconSourceExtension` → `IconImageExtension`.
- *Dependencies:* `Avalonia` (at the `Directory.Packages.props` version) and `Enigma.Icons.Phosphor`
  1.0.0 (which brings `Enigma.Icons` transitively). Record here every `old → new` transition from D7
  and, if the Avalonia coupled set was **held back**, say so explicitly with the held version. Since
  this section is the last and the only one with third-party dependencies, it also carries the
  one-line record of any solution-internal (test/sample-only, non-shipping) bumps from D7, labelled
  as such.
- *Version:* 1.0.0.

Each section's opening prose must stay consistent with its `<PackageReleaseNotes>` (D4).

### D6. README what's-new callout (+ confirm 718F's supported-TFMs table)
Files: `README.md` (root), `src/Enigma.Icons/README.md`, `src/Enigma.Icons.Phosphor/README.md`,
`src/Enigma.Icons.Avalonia/README.md`.

**Ownership is settled, and the callout is ROOT README ONLY.** SPEC §13's root-README row and §13.2's
owner table name the **root README what's-new blockquote** as FEATURE-74DC's sole property:
FEATURE-718F leaves a place for it and must **not** write it, so a missing callout after 718F is
correct, not a defect. §13.2 is equally explicit the other way — *"The three packed READMEs get **no**
callout."* Do not add one to them; FEATURE-718F states the same rule, and writing one would both
contradict the SPEC and edit files this item does not own.

**a) Callout** — one blockquote in the **root README only**, immediately after the intro
(`dotnet-release`):
`> **What's new in 1.0** — <one-line highlight>. See the [release notes](RELEASENOTES.md).`
Highlight: "first release of the Enigma.Icons umbrella — three sibling packages: the
framework-agnostic core, the Phosphor asset pack, and the Avalonia renderer". The root README is not
packed, so the relative `RELEASENOTES.md` link is correct here.

**Packed-README links are FEATURE-718F's, and this item only audits them.** The settled rule (SPEC
§10.5, and 718F's own design): files that ship **inside** the package — `LICENSE.md`, plus
`THIRD-PARTY-NOTICES.md` in the Phosphor package — are linked **relatively**, because they are packed
alongside the README; everything **not** packed (`RELEASENOTES.md`, anything under `docs/`, sibling
package READMEs) is **absolute** against `https://github.com/josueclement/Enigma.Icons`, since a
relative link renders dead on nuget.org. Verify that state in D6c; do not rewrite those lines.

**b) Supported-target-frameworks table — confirm, do not re-add.** The table is
**FEATURE-718F's** (SPEC §13.2); this item only reads it and checks it against the D1 result. Edit it
**only** if D1 changed a TFM set, and then only the affected row. Do not add a second TFM line
anywhere.

**c) Confirm only** (do not re-add): the NuGet + MIT badge pair in the **root README only** — the
three packed READMEs carry **no badges** (SPEC §13.1), so a missing badge there is correct, not a
defect; the gallery screenshot embed in the root README (718F's, from `docs/img/gallery.png`); and
the Phosphor credit line from SPEC §14.2 in the root and Phosphor READMEs. The NuGet-version badge
auto-tracks the published version — never a per-release edit.

### D7. NuGet dependency refresh — `Directory.Packages.props`
1. `dotnet list package --outdated` (read-only) and record the output verbatim for the completion doc.
2. Apply the **non-coupled** bumps by editing the `<PackageVersion>` entries in
   `Directory.Packages.props` — here that means `CommunityToolkit.Mvvm`,
   `Microsoft.Extensions.Hosting` (gallery-only) and `xunit.v3` (tests-only). None of these ships in
   any package. SPEC §3.3 gives **exact verified pins** under "*Verify at restore time, do not
   assume*" — it requires the pins to be verified, it does **not** authorize a bump. So use step 1's
   output to propose each non-coupled bump to the user as `old → new` and get the OK **before**
   editing `Directory.Packages.props`.
3. **Hold back the Avalonia coupled set** — the six Avalonia ids (`Avalonia`, `Avalonia.Desktop`,
   `Avalonia.Themes.Fluent`, `Avalonia.Fonts.Inter`, `Avalonia.Headless`, `Avalonia.Headless.XUnit`)
   **plus `AvaloniaUI.DiagnosticsSupport`**, at the versions pinned in `Directory.Packages.props`
   (SPEC §3.3) — **unless the user opts in**. There is **no `Avalonia.Diagnostics` for Avalonia 12**;
   `AvaloniaUI.DiagnosticsSupport` is its replacement and must never be swapped back (SPEC §3.3,
   §11). If the user opts in: move the **six Avalonia ids together to one identical new version**,
   never one at a time, and move `AvaloniaUI.DiagnosticsSupport` to **its own** current version — it
   belongs to the coupled set but is versioned **independently**, so it can never share the Avalonia
   version number. Then re-verify the SPEC §3.3 condition that the new `Avalonia.Headless.XUnit`
   still depends on `xunit.v3.extensibility.core` (i.e. is still xUnit v3-native) before accepting
   the bump. This set is the only one that changes a **published** dependency floor
   (`Enigma.Icons.Avalonia`), so treat it as a compatibility decision, not housekeeping.
4. Do **not** add a central pin for `System.Memory`, `Microsoft.Bcl.*`, or any polyfill unless a
   concrete `netstandard2.0` compile error demands it, and then only with a csproj comment
   (SPEC §3.3). The zero-third-party-dependency posture of the two agnostic packages is a hard
   constraint (SPEC §2.10).
5. Record every `old → new` transition, and any deliberately held-back set, in `RELEASENOTES.md`
   (D5) per `dotnet-release`.

### D8. `docs/RELEASE.md` — from the template, three packages
Create `docs/RELEASE.md` from the in-repo template snapshot
`docs/reference/release/RELEASE.template.md` (SPEC §1 — a snapshot of the `dotnet-release` bundled
`templates/RELEASE.md`, carried here so `docs/` stays self-contained and this item needs no file
outside `docs/`), **create-only-if-missing** (if a file is already there, diff and reconcile — never
clobber). Fill the placeholders, generalizing the single-package template to the three that ship
together — the step numbers below are the snapshot's own:
- `{{SOLUTION}}` → `Enigma.Icons.slnx`; `{{DEFAULT_BRANCH}}` → `main` (SPEC §1); tag form **bare
  `X.Y.Z`** (D0.2).
- `{{PACKAGE_ID}}` ×3 → `Enigma.Icons`, `Enigma.Icons.Phosphor`, `Enigma.Icons.Avalonia`;
  `{{LIB_CSPROJ}}` / `{{LIB_DIR}}` ×3 → the three `src/…` paths from Context.
- Intro: all three libraries publish together at the same version; `samples/` and `tools/` are
  `IsPackable=false` and ship as source only.
- Pre-release checklist: the three `<Version>` values, the three `<PackageReleaseNotes>`, the three
  `RELEASENOTES.md` sections, the root-README callout, and the TFM-policy check.
- Step 3: a **single shared tag** for the batch.
- Step 4: one `dotnet pack … -c Release -o ./artifacts` line **per library** (three lines).
- Step 5: one `dotnet nuget push` **per package** in dependency order — `Enigma.Icons` first, then
  `Enigma.Icons.Phosphor`, then `Enigma.Icons.Avalonia` — so each is indexed before its dependents.
  Keep the API-key-is-a-secret warning verbatim.
- Step 6: repeat the package-page / badge / `dotnet add package` / tag checks for all three ids.

### D9. Release verification (in-repo, run by the builder)
From the repo root:
1. `dotnet build Enigma.Icons.slnx -c Release` → **zero warnings** (`TreatWarningsAsErrors`,
   SPEC §2.1/§3.2; this is also where an IL2xxx/IL3xxx trim warning would surface, SPEC §10.4).
2. `dotnet test Enigma.Icons.slnx -c Release` → **whole suite green** (the three existing
   `*.UnitTests` projects, including the full-corpus integrity assertions of SPEC §12.2; no tests are
   added here).
3. `dotnet pack <each of the three csprojs> -c Release -o ./artifacts` → `Enigma.Icons.1.0.0.nupkg`,
   `Enigma.Icons.Phosphor.1.0.0.nupkg`, `Enigma.Icons.Avalonia.1.0.0.nupkg`, **plus one `.snupkg`
   each** (SPEC §10.4 mandates `IncludeSymbols`/`snupkg`).
   `./artifacts/` is a git-ignored scratch dir, never committed,
   and this pack is **for inspection only** — distinct from the publish pack the user runs from D10.
4. Inspect each nupkg (`unzip -l` + extract the `.nuspec`): `id`, `version` 1.0.0, `<releaseNotes>`
   present, `README.md` + `LICENSE.md` at the package root, the dependency groups from D3, and one
   lib folder per TFM.
5. **Phosphor-specific:** confirm `THIRD-PARTY-NOTICES.md` is present in
   `Enigma.Icons.Phosphor.1.0.0.nupkg` (and absent from the other two), that all six
   `phosphor.<weight>.dat` resources are present — **but not via `unzip -l`**: they are embedded
   *inside* the assembly, which is a single nupkg entry (SPEC §7.3), so the archive listing shows
   zero `.dat` entries on a correctly packed package. Extract `lib/<tfm>/Enigma.Icons.Phosphor.dll`
   and check the six SPEC §7.1 manifest resource names, or rely on FEATURE-3950's
   `ResourceManifestTests` as the gate and limit the archive check to one `lib/` folder per TFM plus
   `README.md` / `LICENSE.md` / `THIRD-PARTY-NOTICES.md`.
   Expected nupkg size: **≈3.4–3.5 MB** — SPEC §7.3's prediction for the normative three-TFM set
   (three `lib/` folders, each carrying the full ≈1.15 MB deflated resource blob; measured
   **1,150,587 B** for the blob itself). This is **larger** than the retired package's 2,121,835 B,
   and that is expected — see §7.3. A result near 1.15 MB would mean only **one** `lib/` folder was
   produced, which is itself the defect to investigate.

### D10. Printed runbook — print, never run
Final build output. Preface it with the explicit statement: *"The `dotnet-release` skill **prints**
these commands; the **user** runs them. Nothing outward-facing — the merge to `main`, `git tag`, the
publish `dotnet pack`, `dotnet nuget push` — is executed automatically. The NuGet API key is a secret:
never commit or echo it."*

```bash
# 1. Pre-flight (Release configuration)
dotnet build Enigma.Icons.slnx -c Release
dotnet test  Enigma.Icons.slnx -c Release

# 2. Merge the release branch into main, then locally:
git switch main && git pull

# 3. Tag — bare X.Y.Z (repo has no tags); all three packages ship as 1.0.0 under one tag
git tag 1.0.0
git push origin 1.0.0

# 4. Pack all three libraries (GeneratePackageOnBuild is off)
dotnet pack src/Enigma.Icons/Enigma.Icons.csproj                   -c Release -o ./artifacts
dotnet pack src/Enigma.Icons.Phosphor/Enigma.Icons.Phosphor.csproj -c Release -o ./artifacts
dotnet pack src/Enigma.Icons.Avalonia/Enigma.Icons.Avalonia.csproj -c Release -o ./artifacts

# 5. Push in dependency order (core first, then Phosphor, then Avalonia)
dotnet nuget push ./artifacts/Enigma.Icons.1.0.0.nupkg          --api-key <NUGET_API_KEY> --source https://api.nuget.org/v3/index.json
dotnet nuget push ./artifacts/Enigma.Icons.Phosphor.1.0.0.nupkg --api-key <NUGET_API_KEY> --source https://api.nuget.org/v3/index.json
dotnet nuget push ./artifacts/Enigma.Icons.Avalonia.1.0.0.nupkg --api-key <NUGET_API_KEY> --source https://api.nuget.org/v3/index.json
```

Follow with the `dotnet-release` post-publish verification, for **each** of the three ids: the
package page shows 1.0.0; the README NuGet badge resolves; `dotnet add package <id> --version 1.0.0`
restores; the `1.0.0` tag exists and its notes match `RELEASENOTES.md`. Same as `docs/RELEASE.md`
step 6.

## Dependencies & ordering

Must be built **after** every package-producing and documentation item is `DONE` and green (roadmap
sequencing 8; SPEC §16):

- **FEATURE-21C4** — solution, shared props/config, `LICENSE.md`, `THIRD-PARTY-NOTICES.md`, the
  `RELEASENOTES.md` skeleton this item fills.
- **FEATURE-24DD / FEATURE-3950 / FEATURE-3ADD** — the three packable csprojs (version, metadata,
  packed items) this item confirms and adds `<PackageReleaseNotes>` to; plus the test suites the
  Release test run exercises. FEATURE-2DDE's generated assets reach the nupkg through 3950.
- **FEATURE-469B** — the gallery; its packages are in the D7 refresh scope and its build must stay
  green in Release.
- **FEATURE-718F** — the root README this item adds the what's-new callout to, and the owner of the
  supported-target-frameworks table, the badges and the screenshot embed this item only confirms
  (SPEC §13.2). Placed before this item precisely so the documented API is the shipped API.

Rationale: build/test/pack verification and the runbook can only be produced once all three packages
compile, test green, pack, and are documented. This is the terminal 1.0.0 item.

Dependents: **FEATURE-6FA1** (`Enigma.Icons.Wpf`) is sequenced after it but is marked in
`docs/roadmap.md` as **DEFERRED post-1.0, do not build yet** — `/build` skips it, so FEATURE-74DC is
the **last buildable item** in this line. 6FA1 holds no code (SPEC §17) and nothing in this item may
be shaped for it beyond the `netstandard2.0` floor that already exists.

## Acceptance criteria

Documentation/packaging work with **no new product code and no new tests**: DoD criteria 1–2 are
satisfied by the Release build, the Release test run, and `dotnet pack` + nupkg inspection (D9),
stated explicitly below.

- [x] `<TargetFrameworks>` verified in all three packable csprojs: `netstandard2.0;net8.0;net10.0`
      (core, Phosphor) and `net8.0;net10.0` (Avalonia) — expected **unchanged**. Any change was
      proposed as `old → new`, **confirmed by the user before writing**, and logged in the
      `RELEASENOTES.md` *Compatibility* sub-section and the affected row of FEATURE-718F's
      supported-target-frameworks table. (D1)
- [x] `<Version>1.0.0</Version>` present exactly once in each of the three packable csprojs; absent
      from every non-packable project. (D2)
- [x] Prerequisite audit passes for all three csprojs: `PackageId`, `PackageReadmeFile` +
      packed `README.md`, `PackageLicenseFile` + packed `LICENSE.md`, `PackageReleaseNotes`,
      `GenerateDocumentationFile=true`, `GeneratePackageOnBuild` absent/false, `RepositoryUrl` +
      `PackageProjectUrl` both `https://github.com/josueclement/Enigma.Icons` with `RepositoryType`
      `git` and **no `PackageIcon`** (SPEC §10.5); the packed
      `THIRD-PARTY-NOTICES.md` item present in **`Enigma.Icons.Phosphor` only**. (D3)
- [x] `<PackageReleaseNotes>` in each of the three csprojs mirrors its `RELEASENOTES.md` section and
      ends with `See RELEASENOTES.md for the full details.`; the Phosphor one names the artwork as
      **Phosphor Icons 2.1.1 (MIT)**. (D4)
- [x] `RELEASENOTES.md` has exactly the three `## Enigma.Icons[.X] v1.0.0 Release Notes` sections,
      each with *New Features · Compatibility · Dependencies · Version*, and between them record:
      **6 weights including the new duotone**, **1,512 icons**, the **zero-third-party-dependency**
      posture with **FusionCache dropped**, the **`netstandard2.0` floor** and its rationale, and
      that this **supersedes the retired `PhosphorIconsAvalonia` under a new identity with no upgrade
      path**. (D5)
- [x] The **root README** carries the "What's new in 1.0" callout and the **three packed READMEs
      carry none** (SPEC §13.2). Packed-README links are audited, not rewritten: they link
      `RELEASENOTES.md` by **absolute** URL and `LICENSE.md` relatively. FEATURE-718F's
      supported-target-frameworks table was **confirmed** (edited only if D1 changed a TFM set), and
      the badge pair was confirmed in the **root README only** — none in the packed three
      (SPEC §13.1, §13.2). (D6)
- [x] `dotnet list package --outdated` was run; non-coupled bumps proposed as `old → new`, approved,
      and applied in `Directory.Packages.props`; the Avalonia coupled set held back — or, with the
      user's opt-in, the **six** Avalonia ids moved together to one identical version and
      `AvaloniaUI.DiagnosticsSupport` moved to its own current version, with the xunit.v3-native
      headless check re-verified; every `old → new` transition and any held-back set recorded in
      `RELEASENOTES.md`. (D7)
- [x] `docs/RELEASE.md` present, created from `docs/reference/release/RELEASE.template.md`,
      created-not-clobbered, filled for **three** packages
      (`Enigma.Icons.slnx`, `main`, bare `X.Y.Z` tag, three pack lines, three push lines in
      core → Phosphor → Avalonia order). (D8)
- [x] `dotnet build Enigma.Icons.slnx -c Release` completes with **zero warnings**. (DoD 1; D9.1)
- [x] `dotnet test Enigma.Icons.slnx -c Release` — **whole suite green**, all three
      `*.UnitTests` projects. (DoD 2; D9.2)
- [x] `dotnet pack -c Release -o ./artifacts` succeeds for all three projects, producing
      `Enigma.Icons.1.0.0.nupkg`, `Enigma.Icons.Phosphor.1.0.0.nupkg` and
      `Enigma.Icons.Avalonia.1.0.0.nupkg` with the expected names/version, plus one `.snupkg` each
      (SPEC §10.4); each nuspec inspected for
      id, version, release notes, packed `README.md`/`LICENSE.md`, and the D3 dependency groups. (D9.3–4)
- [x] `Enigma.Icons.Phosphor.1.0.0.nupkg` **contains `THIRD-PARTY-NOTICES.md`** (SPEC §14.2) and all
      the six `phosphor.<weight>.dat` **manifest resources inside the assembly** (not as archive
      entries — SPEC §7.3); the other two nupkgs do **not** contain the notices file. (D9.5)
- [x] Phosphor nupkg size sanity-checked against SPEC §7.3's three-TFM expectation of
      **≈3.4–3.5 MB** — deliberately **larger** than the retired package's 2,121,835 B, because the
      assembly ships once per TFM; the measured size is recorded in the completion doc. (D9.5)
- [x] **No size-reduction claim** appears in `RELEASENOTES.md`, any `<PackageReleaseNotes>`, or any
      README — and none of the retired figures ("36 MB", "≈3.74 MB", "≈4.05 MiB", "~45 % below",
      "1,153,846 B", "the real win is compression") survives anywhere. (D5, SPEC §7.3)
- [x] `Enigma.Icons.slnx` **unchanged** — FEATURE-74DC appends no `<Project>` entry (SPEC §3.4).
- [x] The pack/tag/push runbook was **printed, not run**, with the explicit "skill prints / user runs"
      statement, bare `1.0.0` tag, `main` branch, and no API key echoed. No `git tag`, publish
      `dotnet pack`, `dotnet nuget push`, or merge to `main` was executed. (D10)
- [x] All version surfaces agree at **1.0.0**: three csproj `<Version>`, three
      `<PackageReleaseNotes>`, three `RELEASENOTES.md` sections, one root-README callout,
      `docs/RELEASE.md`.
- [x] **Roadmap + this plan flipped to `DONE`** (`docs/roadmap.md` FEATURE-74DC row; this file's
      status header). (DoD criterion 4.)
- [x] **Completion doc `docs/done/FEATURE-74DC.md` written** — summary, files touched, deviations,
      build/test evidence. (DoD criterion 5.)

## Notes / risks

- **OPEN QUESTION 1 — the SPEC specifies the packable property set but not the descriptive metadata.**
  SPEC §10.4 now settles the shared packable property set (`GenerateDocumentationFile`,
  `IncludeSymbols` + `SymbolPackageFormat=snupkg`, `PackageReadmeFile`/`PackageLicenseFile`,
  `GeneratePackageOnBuild` off — **not** `ImplicitUsings`, which §10.4 explicitly excludes because
  §2 rule 2 / §3.2 require it in *every* csproj, packable or not) and §10.5 the canonical URLs and the
  no-`PackageIcon` rule, so none of those needs asking any more. What is still unlisted — unlike the
  sibling `Enigma.Logging` SPEC — is `<Title>`, `<Description>` and `<PackageTags>`. **Assumption to
  confirm:** whatever FEATURE-24DD/3950/3ADD set at creation is authoritative for those three, and
  this item only *confirms* them — it does not invent marketing copy. If one is missing entirely,
  **ask the user** rather than writing it. Either way, record what was found.
- **OPEN QUESTION 2 — is a publish `git remote` configured?** The URL itself is **settled**:
  `https://github.com/josueclement/Enigma.Icons` is canonical for `RepositoryUrl`,
  `PackageProjectUrl` (SPEC §10.5) and the XAML namespace URI (SPEC §10.3), so D6's absolute README
  links and D3's URL audit need no confirmation — use it. What is *not* established anywhere in the
  SPEC or the roadmap is that `git remote` is configured: FEATURE-21C4 only runs `git init -b main`,
  which per SPEC §16.1 both creates the repo and *is* its branch creation. If D0.4 finds no remote,
  the printed runbook's `git push` / `dotnet nuget push` steps are conditional on the user adding one
  — say so in the printed preface rather than silently assuming.
- **Dependency wording is settled — not an open question.** SPEC §0, §2 rule 10 and §9 are now
  explicit: `Enigma.Icons` has **zero dependencies at all**; `Enigma.Icons.Phosphor` has **zero
  third-party dependencies** and declares exactly one package dependency, the sibling `Enigma.Icons`
  (the `ProjectReference` that `dotnet pack` emits as `Enigma.Icons 1.0.0`); dependencies within the
  `Enigma.Icons` family are not third-party and do not breach the rule. The **prescribed**
  release-notes wording is therefore "no third-party runtime dependencies; the only package dependency
  is the sibling `Enigma.Icons` 1.0.0" (D5) — write it as-is, nothing to confirm before publishing.
- **Phosphor nupkg size is a signal, not a target.** If it lands materially outside ≈3.4–3.5 MB,
  **do not "fix" the assets.** Investigate the cause (raw `.svg` files accidentally packed, an
  uncompressed `.dat`, a stray `Content` item) and report it as a finding — the assets are
  byte-traceable to upstream (SPEC §8.3) and are not this item's to touch.
- **The Avalonia bump is a published-API decision.** `Enigma.Icons.Avalonia` is the only package with
  a third-party dependency, so bumping the coupled set raises the floor every consumer must satisfy.
  Hold it back by default; if the user opts in, move the **six** Avalonia ids together to one
  identical new version and `AvaloniaUI.DiagnosticsSupport` — same coupled set, **independent**
  version line — to its own current version, then re-verify the SPEC §3.3 xunit.v3-native headless
  condition; a v2-based `Avalonia.Headless.XUnit` would break the whole Avalonia test project. There
  is no `Avalonia.Diagnostics` for Avalonia 12 (SPEC §3.3, §11) — do not reintroduce that id.
- **Print boundary.** The D9 verification pack writes to a git-ignored `./artifacts/` for inspection
  only; it is *not* the publish pack. Nothing in D10 is executed. The API key never appears in the
  repo, in the printed output, or in the completion doc.
- **Clean break stays clean.** SPEC §17 rules out deprecating/unlisting `PhosphorIconsAvalonia`,
  writing a migration guide, and updating the `phosphor-icons-avalonia` skill. The known,
  accepted consequence — that skill will keep documenting the retired API — is not a defect to fix
  here.
- **No MSI profile.** All three projects have a `PackageId`, and `dotnet-release` makes profiles
  app-only. The gallery is an app but is `IsPackable=false` and is not a released artifact
  (SPEC §11), so it gets none either.
- **Tag prefix.** The repo has no tags, so bare `1.0.0` is the correct default. Do not introduce a
  `v` prefix — re-run `git tag` at build time to confirm rather than trusting this note.
- **Line endings.** If a touched doc or csproj shows CRLF/LF churn, add a one-line recommendation to
  the completion doc only; per `dev-workflow` normalization is never part of a dev.
