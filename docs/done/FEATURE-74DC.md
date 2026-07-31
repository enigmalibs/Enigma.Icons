# FEATURE-74DC — Release preparation & NuGet publish runbook (v1.0.0 ×3)

**Status:** DONE · Branch `feature/feature-74dc-release-prep` · Single-phase

## Summary

Brought all three packable libraries to a publishable **1.0.0** and produced the release collateral.
No product code, no public API change, no new test, no generated asset touched, no SPEC change,
`Enigma.Icons.slnx` untouched.

What was **confirmed** (already correct, nothing written):

- **D1 — target frameworks.** `netstandard2.0;net8.0;net10.0` for `Enigma.Icons` and
  `Enigma.Icons.Phosphor`, `net8.0;net10.0` for `Enigma.Icons.Avalonia`. Unchanged, exactly the
  normalized shape. `IsTrimmable`/`IsAotCompatible` verified in place — conditioned on
  `!= netstandard2.0` in the two multi-targeting packages, unconditional in the Avalonia one. That
  asymmetry is SPEC §10.4-sanctioned and was **not** "fixed".
- **D2 — versions.** `<Version>1.0.0</Version>` present exactly once in each of the three packable
  csprojs, and absent from all five non-packable projects.
- **D3 — prerequisite audit.** All three carry `PackageId`, `Title`, `Description`, `PackageTags`,
  `PackageReadmeFile` + packed `README.md`, `PackageLicenseFile` + packed `LICENSE.md`,
  `RepositoryUrl`/`PackageProjectUrl` = `https://github.com/josueclement/Enigma.Icons`,
  `RepositoryType` `git`, no `PackageIcon`, `IncludeSymbols` + `SymbolPackageFormat=snupkg`,
  `GenerateDocumentationFile=true`. `GeneratePackageOnBuild` absent everywhere.
  `THIRD-PARTY-NOTICES.md` packed by `Enigma.Icons.Phosphor` only. **Zero deviations found.**
- **D6b/c — FEATURE-718F's surfaces.** Supported-target-frameworks table confirmed (D1 changed no TFM
  set, so it was not edited). Badge pair present in the root README only; the three packed READMEs
  carry no badges and no callout — correct per SPEC §13.1/§13.2. Packed-README links audited, not
  rewritten: the only relative links are `LICENSE.md` (all three) and `THIRD-PARTY-NOTICES.md`
  (Phosphor) — precisely the files that ship inside the package. No packed README links
  `RELEASENOTES.md` at all, so no dead relative link exists on nuget.org.

What was **written**:

- **D4** — `<PackageReleaseNotes>` in each of the three csprojs, each ending
  `See RELEASENOTES.md for the full details.`; the Phosphor one names **Phosphor Icons 2.1.1 (MIT,
  © 2020 Phosphor Icons)**.
- **D5** — the three `RELEASENOTES.md` v1.0.0 bodies, each with *New Features · Compatibility ·
  Dependencies · Version*.
- **D6a** — the "What's new in 1.0" blockquote in the **root README only**, after the intro.
- **D7** — the dependency refresh (see below).
- **D8** — `docs/RELEASE.md`, created from `docs/reference/release/RELEASE.template.md` (file did not
  previously exist, so nothing was clobbered), generalized from the template's single package to the
  three that ship together.

### OPEN QUESTIONs resolved

- **OQ1 — descriptive metadata.** Resolved as *confirm only*. `<Title>`, `<Description>` and
  `<PackageTags>` were already set by FEATURE-24DD/3950/3ADD in all three csprojs. Nothing was
  missing, so no marketing copy was invented and the user was not asked.
- **OQ2 — publish remote.** `git remote -v` is **empty — no remote is configured.** The printed
  runbook's `git push origin`, and therefore the whole publish path, is explicitly conditional on the
  user adding one first. Flagged in the printed preface rather than silently assumed.

### D7 — dependency refresh (both decisions put to the user)

`dotnet list package --outdated` output, recorded verbatim as required:

```
Enigma.Icons.Avalonia.Gallery [net10.0]
  Avalonia                        12.0.4 -> 12.1.0
  Avalonia.Desktop                12.0.4 -> 12.1.0
  Avalonia.Fonts.Inter            12.0.4 -> 12.1.0
  Avalonia.Themes.Fluent          12.0.4 -> 12.1.0
  AvaloniaUI.DiagnosticsSupport   2.2.1  -> 2.2.3
  Microsoft.Extensions.Hosting    10.0.8 -> 10.0.10
Enigma.Icons.Avalonia [net8.0, net10.0]
  Avalonia                        12.0.4 -> 12.1.0
Enigma.Icons.Avalonia.UnitTests [net10.0]
  Avalonia.Headless.XUnit         12.0.4 -> 12.1.0
Enigma.Icons, Enigma.Icons.Phosphor, Enigma.Icons.UnitTests,
Enigma.Icons.Phosphor.UnitTests, Enigma.Icons.Generator — no updates.
```

- **Non-coupled — approved and applied:** `Microsoft.Extensions.Hosting` **10.0.8 → 10.0.10**
  (gallery-only, non-shipping). `CommunityToolkit.Mvvm` 8.4.2 and `xunit.v3` 3.2.2 were already
  current — no bump available, unchanged.
- **Avalonia coupled set — the user opted IN** (plan default was to hold back). All six Avalonia ids
  moved **together to one identical version 12.0.4 → 12.1.0**, and `AvaloniaUI.DiagnosticsSupport`
  moved **2.2.1 → 2.2.3** on its own independent version line. `Avalonia.Diagnostics` was not
  reintroduced.
  - **SPEC §3.3 re-verification performed before accepting**, as the plan requires: the
    `Avalonia.Headless.XUnit` 12.1.0 nuspec declares `xunit.v3.extensibility.core` **3.2.2** on both
    `net8.0` and `net10.0` — still xUnit v3-native. All 434 tests then passed on 12.1.0.
  - **This is the one change that raises a published dependency floor**: `Enigma.Icons.Avalonia`
    consumers now need **Avalonia 12.1.0**. Recorded as such in the Avalonia release notes; the
    packed nuspec confirms `<dependency id="Avalonia" version="12.1.0" />`.

## Files touched

**Modified**

- `src/Enigma.Icons/Enigma.Icons.csproj` — added `<PackageReleaseNotes>`.
- `src/Enigma.Icons.Phosphor/Enigma.Icons.Phosphor.csproj` — added `<PackageReleaseNotes>`.
- `src/Enigma.Icons.Avalonia/Enigma.Icons.Avalonia.csproj` — added `<PackageReleaseNotes>`.
- `RELEASENOTES.md` — the three v1.0.0 bodies filled in (headings and H1 kept from the skeleton).
- `README.md` — the "What's new in 1.0" callout, one blockquote after the intro. Nothing else.
- `Directory.Packages.props` — the seven Avalonia-set versions and `Microsoft.Extensions.Hosting`.
- `docs/roadmap.md` — FEATURE-74DC `TODO` → `IN PROGRESS` → `DONE`.
- `docs/plan/FEATURE-74DC.md` — status header, acceptance criteria ticked.

**Created**

- `docs/RELEASE.md`
- `docs/done/FEATURE-74DC.md` (this file)

**Deliberately not touched:** `Enigma.Icons.slnx`, `docs/SPEC.md`, the three packed READMEs,
`LICENSE.md`, `THIRD-PARTY-NOTICES.md`, `CLAUDE.md`, `global.json`, `Directory.Build.props`, all
product source, all tests, all generated assets, and the `/home/jo/Dev/PhosphorIconsAvalonia` repo.

## Build / test evidence

Per the plan, this item has no new product code and no new tests; DoD 1–2 are met by the Release
build, the Release test run, and pack + nupkg inspection.

- **`dotnet build Enigma.Icons.slnx -c Release`** → `Build succeeded. 0 Warning(s) 0 Error(s)`.
  Per the CLAUDE.md caveat, the `.axaml`-compiling projects (`Enigma.Icons.Avalonia`, the gallery)
  were additionally rebuilt with `--no-incremental` and grepped for `AVLN*` — **no output, no XAML
  diagnostics hiding behind a green exit code.**
- **`dotnet test --solution Enigma.Icons.slnx -c Release`** → **434 passed, 0 failed, 0 skipped**
  across all three `*.UnitTests` projects, on the bumped Avalonia 12.1.0.
- **`dotnet pack` ×3 into the git-ignored `./artifacts/`** (inspection only — *not* the publish pack):
  all three `.nupkg` **and all three `.snupkg`** produced.

  | Package | nupkg | snupkg |
  |---|---|---|
  | `Enigma.Icons.1.0.0` | 103,119 B | 44,770 B |
  | `Enigma.Icons.Phosphor.1.0.0` | **3,705,880 B** | 25,890 B |
  | `Enigma.Icons.Avalonia.1.0.0` | 30,783 B | 21,073 B |

- **Nuspec inspection** — all three: `<id>` correct, `<version>` `1.0.0`, `<releaseNotes>` present,
  `<license type="file">LICENSE.md`, `<readme>README.md`, `<repository type="git" url=…>` correct.
  Dependency groups exactly as SPEC requires:
  - `Enigma.Icons` — **empty groups on all three TFMs**, i.e. zero dependencies.
  - `Enigma.Icons.Phosphor` — `Enigma.Icons 1.0.0` and nothing else, on all three TFMs.
  - `Enigma.Icons.Avalonia` — `Enigma.Icons.Phosphor 1.0.0` + `Avalonia 12.1.0`, on both TFMs.

  One `lib/` folder per TFM in each package (3 / 3 / 2), each with its `.xml` doc file.
- **Phosphor specifics** — `THIRD-PARTY-NOTICES.md` present in `Enigma.Icons.Phosphor.1.0.0.nupkg`
  and **absent** from the other two. The archive lists **zero `.dat` entries**, which is the correct
  result: the resources are embedded *inside* the assembly. Extracting
  `lib/netstandard2.0/Enigma.Icons.Phosphor.dll` and reading its manifest confirms all six SPEC §7.1
  names — `Enigma.Icons.Phosphor.Assets.phosphor.{thin,light,regular,bold,fill,duotone}.dat`.
- **Phosphor nupkg size sanity check** — **3,705,880 B ≈ 3.53 MB**, against SPEC §7.3's ≈3.4–3.5 MB
  three-TFM prediction. Within expectation and, as §7.3 predicts, **larger** than the retired
  package's 2,121,835 B because the assembly ships once per TFM. Nowhere near the ~1.15 MB that would
  have indicated only one `lib/` folder was produced. No assets were touched.
- **No size-reduction claim** anywhere: `README.md`, `RELEASENOTES.md`, `docs/RELEASE.md`, the three
  packed READMEs and the three csprojs were grepped for `36 MB`, `≈3.74 MB`, `≈4.05 MiB`, `45 %`,
  `below`, `1,153,846` and `the real win is compression` — **zero hits**. The Phosphor notes state
  the four non-size reasons for the `.dat` format instead.
- **Runbook printed, never run.** No `git tag`, no publish `dotnet pack`, no `dotnet nuget push`, no
  merge to `main`, no commit. No API key appears in the repo, the console output, or this document.
- **Version surfaces all agree at 1.0.0**: three csproj `<Version>`, three `<PackageReleaseNotes>`,
  three `RELEASENOTES.md` sections, one root-README callout, `docs/RELEASE.md`.
- `git diff --stat Enigma.Icons.slnx` → **empty**, slnx unchanged as required.

## Deviations & follow-ups

- **Deviation from the plan default (user-approved):** D7.3 says hold the Avalonia coupled set back
  unless the user opts in. The user **opted in**, so 12.0.4 → 12.1.0 was applied with the mandated
  xunit.v3-native re-verification. This raises the published Avalonia floor for
  `Enigma.Icons.Avalonia` consumers — a deliberate compatibility decision, not housekeeping.
- **Documentation freshness sweep — three edits accepted by the user and folded into this commit.**
  All three were outside the plan's stated scope (which excludes SPEC and `CLAUDE.md` changes); they
  were surfaced as sweep candidates and approved rather than made silently.
  1. **`docs/SPEC.md` §3.3** — the `Directory.Packages.props` listing still showed Avalonia 12.0.4 /
     DiagnosticsSupport 2.2.1 / Hosting 10.0.8, contradicting the file after the approved bump. The
     pin block was updated to 12.1.0 / 2.2.3 / 10.0.10 and its "verify at restore time" note
     rewritten around the 1.0.0 state. The §1063+ **API coordinates were deliberately left alone** —
     they remain accurate on 12.1.0, as the green build and 434 passing tests confirm.
  2. **`CLAUDE.md`** — its closing note still described FEATURE-74DC as the remaining 1.0.0 item. Now
     records 1.0.0 prep as complete, points at the new `docs/RELEASE.md` (previously unmentioned),
     restates that FEATURE-6FA1 must not be built, and adds an explicit "releasing is the user's job"
     boundary including the missing-remote caveat.
  3. **`Directory.Packages.props` comment** — the claim "Version aligned with `/home/jo/Dev/Draw`"
     was **verified and found false**: Draw is still on Avalonia 12.0.4 / DiagnosticsSupport 2.2.1.
     The comment now records the 12.1.0 bump, states that Draw is a reference point rather than a
     constraint, and notes that this solution is deliberately ahead of it.

  Release build and the full test suite were **re-run after these edits**: `0 Warning(s)`,
  **434 passed**.
- **No `git remote` is configured** (OQ2). The publish path cannot run until the user adds one. This
  is stated in the printed runbook preface, not assumed away.
- **Line endings:** no CRLF churn observed. Every file written or modified was verified LF with a
  final newline. No recommendation needed.
- **`./artifacts/`** holds the six verification packages. It is git-ignored and was never committed;
  delete it freely, and note it is **not** the publish pack — the user re-packs from `main` after
  merging, per `docs/RELEASE.md` step 4.
- **No defects found** by the release verification, so no new work item was raised on that account.
