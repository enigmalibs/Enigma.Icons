**Status:** DONE · Single-phase · Built on branch `feature/feature-718f-documentation`

# FEATURE-718F — Documentation (root + 3 packed READMEs)

## Objective

Bring the solution's four READMEs to their **shipped** state: write the root landing page in full
per SPEC §13, and polish the three packed per-package READMEs from their first form into the text
that will appear on nuget.org. Every code sample in all four files is verified against the API as
actually shipped by FEATURE-24DD / FEATURE-3950 / FEATURE-3ADD — not written from memory. No source
code changes, no version work.

## Context

- Documentation ownership and per-file content are fixed by the SPEC §13 table; this item is the
  "full" column for the root README and the shipped state for all three packed READMEs.
- The item sits **after** FEATURE-469B deliberately (roadmap "Sequencing & dependencies" §7): by now
  the public surface is frozen, the gallery exists, and the screenshot material is available, so the
  documented API is the shipped API.
- **The files already exist.** FEATURE-21C4 wrote the root README as a skeleton landing page with
  *forward links* to the three per-package READMEs. Each packable project's own item
  (24DD / 3950 / 3ADD) wrote its `README.md` in a correct first form — a packable csproj needs a
  README on disk to pack (SPEC §13 "packed"). This item therefore mostly **rewrites and completes**
  rather than creates — SPEC §13.2: sole ownership governs the **shipped** state. If any packed README
  is missing or is a stub, this item writes it in full.
- The house `dotnet-release` skill also touches the README at release time, but SPEC §13.2 fixes the
  ownership: the **badges** (root README only, SPEC §13.1), the **supported-target-frameworks table**
  and the **gallery screenshot embed** are this item's; the **what's-new callout** is FEATURE-74DC's
  and is **root README only** (SPEC §13.2 — the three packed READMEs get none), and 74DC only
  *confirms* the TFM table. This item writes its own sections and leaves **one** obvious slot, in the
  root README, for 74DC's callout.

## Scope

### In scope
- `README.md` (root) — full landing page per SPEC §13, replacing the 21C4 skeleton.
- `src/Enigma.Icons/README.md` — final, shipped form.
- `src/Enigma.Icons.Phosphor/README.md` — final, shipped form.
- `src/Enigma.Icons.Avalonia/README.md` — final, shipped form.
- Resolving the forward links FEATURE-21C4 left pointing at the per-package READMEs.
- `docs/img/gallery.png` — the gallery screenshot captured by FEATURE-469B (SPEC §1, §13), embedded
  from the root README only.
- A consistency sweep: every C# and XAML sample in all four files checked against the shipped
  signatures, plus a `dotnet pack` check that each nupkg really carries its README.

### Out of scope
- **`RELEASENOTES.md` bodies, `<PackageReleaseNotes>`, `docs/RELEASE.md`, and the version bump** —
  all FEATURE-74DC (SPEC §13, §16). Do **not** add a what's-new callout or a version number to any
  README here; leave the shape for 74DC to fill.
- **Any migration guide, deprecation notice, or compatibility shim for `PhosphorIconsAvalonia`**, and
  **any update to the `phosphor-icons-avalonia` skill** — clean break, SPEC §17. That skill keeps
  documenting the retired API; that is known and accepted.
- `LICENSE.md`, `THIRD-PARTY-NOTICES.md`, `CLAUDE.md` — owned by FEATURE-21C4 (SPEC §13). This item
  links/points at them, never rewrites them.
- Any `.cs`, `.axaml`, or XML-doc change. If a SPEC-mandated behaviour turns out to be missing from
  the shipped code, that is a defect against the owning item — record it as a follow-up, do **not**
  document it as if it shipped and do **not** fix it here.
- WPF documentation (SPEC §17 / FEATURE-6FA1) beyond a single "post-1.0, not part of 1.0.0" line.
- Any new docs page (no usage guide, no icon catalog listing 1,512 names — the gallery's search
  covers that, SPEC §11).
- **slnx: appends nothing** (SPEC §3.4 incremental-growth table, FEATURE-718F row). The file is not
  touched at all.

All four files are LF with a final newline; the zero-warning / explicit-usings / CPM rules of
SPEC §2 continue to apply unchanged (samples show their `using` directives because
`ImplicitUsings` is disabled).

## Design

Build order. Step 1 is a hard prerequisite for steps 2–5: **no sample is written before the
signature ledger exists.**

### 1. Harvest the shipped signature ledger (no files written in the repo)

Read the real public surface and record it in a scratch note (scratchpad, never in the repo):

- `src/Enigma.Icons/*.cs` — `IconViewBox`, `IconLayer`, `IconGlyph`, `IconFillRule`, `IconLineCap`,
  `IconLineJoin`, `IIconSet`, `SvgIconSet` factory signatures, `SvgIconParser`, both exceptions.
- `src/Enigma.Icons.Phosphor/PhosphorWeight.cs`, `PhosphorIconSet.cs`,
  `PhosphorIconNames.g.cs` (member names of the three helpers), and `PhosphorIcon.g.cs` (spot-check
  a few enum member spellings actually used in samples).
- `src/Enigma.Icons.Avalonia/IconGlyphExtensions.cs`, `Icon.cs`,
  `Markup/IconGeometryExtension.cs`, `Markup/IconImageExtension.cs` — property names, defaults, and a
  confirmation that `Properties/AssemblyInfo.cs` carries the **two** `XmlnsDefinition` attributes of
  SPEC §10.3 (one for `Enigma.Icons.Avalonia`, one for `Enigma.Icons.Avalonia.Markup`), which is why
  every sample shows a single `ei:` prefix.
- The **three packable csprojs'** `<TargetFrameworks>` values (the root README's TFM table must match
  reality, not SPEC §0 from memory — they should agree; if they do not, the csproj wins for the README
  and the mismatch is reported). SPEC §0's fourth TFM set belongs to the deferred `Enigma.Icons.Wpf`,
  which has no csproj and is not documented here.
- `samples/Enigma.Icons.Avalonia.Gallery/**.axaml` and the three test projects — the best source of
  *known-compiling* usage for both XAML and C# samples.

Ledger entries carry parameter order, default values, and return types. Every sample in steps 2–5
is written **from this ledger**.

### 2. `README.md` (root) — full landing page (SPEC §13 row 1)

Rewrite in full, in this order:

1. **H1 + intro paragraph** — one umbrella, three sibling packages; what each is for in a clause
   (framework-agnostic model + SVG parser · the Phosphor artwork as an interchangeable pack ·
   the Avalonia renderer). Mention the two hard selling points from SPEC §0: **zero third-party
   dependencies** on the base and Phosphor packages (`Enigma.Icons` has none at all;
   `Enigma.Icons.Phosphor` declares exactly one — the sibling `Enigma.Icons`; SPEC §2 rule 10), and
   six weights including duotone. Immediately after this paragraph, leave the **slot** for
   FEATURE-74DC's what's-new blockquote — a place, not a placeholder line: this item writes **no**
   callout text, no version number, and no `RELEASENOTES.md` link there, because SPEC §13.2 gives
   74DC sole ownership of that blockquote.
2. **Packages** — the three, one line each, **each with the SPEC §13.1 badge pair** with
   `<PackageId>` substituted (NuGet version badge + MIT badge linking `LICENSE.md`). Root README only
   — the packed READMEs carry no badges (SPEC §13.1). Do not add a Downloads badge.
3. **Which one do I need?** — Avalonia app → `Enigma.Icons.Avalonia` (it brings the other two);
   non-Avalonia / your own `.svg` files / a renderer of your own → `Enigma.Icons`;
   `Enigma.Icons.Phosphor` alone → the artwork with no renderer. Plus one line: a WPF sibling is
   planned post-1.0 and is **not** part of 1.0.0 (SPEC §0 table, §17) — no timeline, no API.
4. **Quick start** — `dotnet add package Enigma.Icons.Avalonia`, then **both** paths:
   - **XAML** — the single `xmlns:ei="https://github.com/josueclement/Enigma.Icons"` declaration
     (SPEC §10.3), an `<ei:Icon Kind=… Weight=… Size=… Foreground=…/>`,
     and **both** markup extensions (`{ei:IconGeometry …}` on `Path.Data`,
     `{ei:IconImage …}` on `Image.Source`) — shape per SPEC §10.3, the single `ei:` prefix confirmed
     by the step-1 ledger.
   - **C#** — the two `using` directives and
     `PhosphorIconSet.Instance.GetGlyph(PhosphorIcon.Acorn, PhosphorWeight.Bold).ToGeometry()`
     (SPEC §10.3 C# block).
5. **Supported target frameworks** — a single short table, three rows, taken from the three packable
   csprojs' `<TargetFrameworks>`. **This item is the table's sole owner** (SPEC §13.2); FEATURE-74DC
   only *confirms* it and edits it only if a TFM set changed. Keep this the **only** place in the
   README where TFMs appear, so that confirmation is one-line surgical.
6. **Gallery** — the FEATURE-469B screenshot with alt text, one line describing the gallery
   (search across all 1,512 icons, six weights, size and colour controls) and the
   `dotnet run --project samples/Enigma.Icons.Avalonia.Gallery` command. Image path:
   **`docs/img/gallery.png`**, the single canonical path (SPEC §1, §13). Root README **only** — never
   in a packed README.
7. **Bring your own SVGs** — two lines (point at `SvgIconSet`) linking
   `src/Enigma.Icons/README.md`.
8. **Documentation** — the three resolved per-package README links (this discharges the 21C4
   forward references) plus a pointer to `docs/SPEC.md` for the full design.
9. **Credits** — the Phosphor credit line **verbatim** from SPEC §14.2.
10. **Licence** — MIT, linking `LICENSE.md`; one line noting the artwork is MIT too and its notice
    travels in `THIRD-PARTY-NOTICES.md` (SPEC §14.2).

### 3. `src/Enigma.Icons/README.md` — final form (SPEC §13 row 2)

Packed; assume the reader is on nuget.org, which resolves neither relative *images* nor links to files
that are not in the package. **Link rule — settled, and it applies to all three packed READMEs:**

- Files that **ship inside the package** — `LICENSE.md`, plus `THIRD-PARTY-NOTICES.md` in the Phosphor
  package (SPEC §14.1, §14.2) — are linked **relatively** (`[MIT](LICENSE.md)`), because they sit
  beside the README in the nupkg.
- Everything **not** packed — `RELEASENOTES.md`, anything under `docs/`, a sibling package's README —
  is linked **absolutely** against `https://github.com/josueclement/Enigma.Icons` (SPEC §10.5), e.g.
  `…/blob/main/docs/reference/phosphor/README.md`.
- No images at all in a packed README.

This is the same rule FEATURE-74DC states in its D6a and hardens into an acceptance criterion, so this
item must produce exactly that state. The file carries **no badges** (SPEC §13.1). Sections:

1. **What it is** — the framework-agnostic layer: model + parser + icon-set abstraction, zero
   dependencies, TFM set; one line that renderers ship separately (`Enigma.Icons.Avalonia`).
2. **The model** — `IconViewBox` / `IconLayer` / `IconGlyph` (SPEC §4): layers are in paint order,
   index 0 bottom-most; `IsSingleLayer` is the collapsible common case; and the short version of the
   SPEC §4.2 rationale for **raw string paint values** (`"currentColor"` → `null` so "inherit the
   consumer's brush" is the default).
3. **`IIconSet`** — the interface plus the four contract bullets of SPEC §6.1: case-insensitive and
   kebab/snake/Pascal-tolerant names, `null` variant means `DefaultVariant`, **a variant the set
   lacks is a miss and never a silent fallback**, repeated lookups return a reference-equal cached
   glyph, all members thread-safe.
4. **Parsing SVG** — `SvgIconParser.Parse` (both overloads, `MaxDocumentBytes`), a compact table of
   the supported subset (one row per SPEC §5.1 construct, short handling phrase) and two lines on
   hardening: DTD processing prohibited, no external resolution, size cap (SPEC §5.2).
5. **Not supported** — the SPEC §5.1 exclusion list named explicitly (gradients, `clipPath`, `mask`,
   `<defs>`/`<use>`/`<symbol>`, `style=`/`<style>` CSS, `<text>`, `<image>`, filters, animation),
   what happens (`SvgParseException` naming the construct), and the pointer: **`Avalonia.Svg` /
   `Svg.Skia`** for arbitrary artwork — this library is about icons.
6. **Bring your own SVG folder** — the worked example: a small directory tree
   (`my-icons/{bold,regular}/…svg`), the `SvgIconSet.FromDirectory(...)` call with its real
   parameters, a `GetGlyph` call, and the two behaviours that make an extracted icon tree work
   unchanged — subfolder-as-variant and `-<variant>` filename-suffix stripping (SPEC §6.2). Then the
   other three factories, one line each (`FromAssembly`, `FromFiles`, `FromSvgSources`), and the
   eager-discovery / lazy-parse note.
7. **Errors** — `SvgParseException`, `IconNotFoundException` (message names icon, variant, set),
   `DirectoryNotFoundException` at construction; and the `TryGetGlyph` precedence of SPEC §6.1 stated
   as the SPEC states it — a **miss** returns `false` (and `GetGlyph` throws `IconNotFoundException`),
   while a **broken source** (malformed SVG, I/O error) **propagates from both methods**. Do not write
   the looser "`TryGetGlyph` never throws".
8. **Licence** — MIT; `LICENSE.md` ships in the package, so link it **relatively**.

### 4. `src/Enigma.Icons.Phosphor/README.md` — final form (SPEC §13 row 3)

1. **What it is** — 1,512 icons × 6 weights as an interchangeable asset pack behind `IIconSet`;
   zero third-party dependencies (exactly one package dependency: the sibling `Enigma.Icons`,
   SPEC §2 rule 10); the artwork is embedded as six compact tables, not 9,072 `.svg` files — that is
   why there is no XML parsing at runtime. Pinned upstream version **Phosphor 2.1.1**.
   **No size claim of any kind** in this (or any) README, and no byte figure: per SPEC §7.3 the format
   saves **no** package size — compressed it is a wash, and the three-TFM nupkg will be *larger* than
   the retired package. The old "≈3.74 MB of path data" phrasing was a unit error and must not be
   published; if a figure is ever wanted anyway, the only permitted values are the measured ones —
   `.dat` total 3,951,294 B (3.77 MiB), path-data subtotal 3,820,927 B (3.64 MiB). The size story
   belongs to FEATURE-74DC's release notes (SPEC §7.3), not here.
2. **The six weights** — a table giving a *visual* sense of each: `Thin`/`Light` progressively finer
   outlines-converted-to-fill, `Regular` the default, `Bold` heavy, `Fill` solid silhouette, and
   `Duotone` **two layers** — a 20 %-opacity backing shape under the foreground shape. Add the one
   consequence a caller must know: duotone needs the layer-aware render path
   (`ToDrawing` / the `Icon` control), not `ToGeometry` (SPEC §10.1) — link the Avalonia package.
   No image: relative images do not render on nuget.org; point at the gallery instead.
3. **`PhosphorIcon` and `PhosphorIconNames`** — the generated enum (PascalCase, one member per
   upstream kebab name) and the three helpers `ToKebabCase`, `TryParse`, `All`, with the reason they
   exist: lookup tables instead of `Enum.ToString()`/`Enum.Parse`, which keeps hot paths fast and
   trim/AOT-clean (SPEC §2.11, §8.4). Then the **enum-ordinal caveat**, mandatory per SPEC §8.4 and
   delegated to this item by FEATURE-2DDE: `PhosphorIcon` ordinals are **positional, not a stable
   ABI** — an artwork refresh renumbers the members, so **never persist or transmit the numeric
   value**; persist `PhosphorIconNames.ToKebabCase(icon)` and read it back with `TryParse`. Note in
   the same breath that an artwork refresh is therefore at least a **minor** version bump for this
   package. The same caveat sits on the enum's own XML doc summary, written by FEATURE-2DDE — this
   item documents it, it does not edit `.g.cs`.
4. **Using it** — `PhosphorIconSet.Instance`; the typed pair (`GetGlyph(icon, weight)` defaulting to
   `Regular`, `TryGetGlyph`); the string-keyed `IIconSet` surface with `Variants` /
   `DefaultVariant == "regular"` / `IconNames`; then the caching + thread-safety paragraph
   (per-weight table built on first touch, glyphs cached and reference-equal, safe from any
   thread — SPEC §9.1).
5. **Attribution** — the SPEC §14.2 credit line **verbatim**, plus: the full Phosphor MIT notice
   ships as `THIRD-PARTY-NOTICES.md` in this package (and in this package only) — packed alongside
   this README, so link it **relatively** (step 3's link rule).
6. **Refreshing the artwork** — 4–6 lines, no restatement: extract the pinned snapshot, run the
   generator (`--input` / `--output` per SPEC §8.1), review `git diff`, re-run the full-corpus
   integrity tests, and cross-reference the ordinal caveat above (a refresh renumbers the enum and is
   at least a minor bump, SPEC §8.4); then point at `docs/reference/phosphor/README.md` in the repository for the
   snapshot provenance, sha256, and the newer-release procedure — linked absolutely as
   `https://github.com/josueclement/Enigma.Icons/blob/main/docs/reference/phosphor/README.md`
   (SPEC §10.5), because `docs/` is never packed (SPEC §1).
7. **Licence** — MIT for the code, MIT for the artwork; two lines, both files (`LICENSE.md`,
   `THIRD-PARTY-NOTICES.md`) linked **relatively** because both are packed here.

### 5. `src/Enigma.Icons.Avalonia/README.md` — final form (SPEC §13 row 4)

1. **What it is** — the Avalonia renderer; TFMs `net8.0;net10.0`; depends on `Avalonia` and
   `Enigma.Icons.Phosphor` (which brings `Enigma.Icons` transitively) and nothing else.
2. **Quick start** — install, the single `xmlns:ei="https://github.com/josueclement/Enigma.Icons"`
   line, one `<ei:Icon …/>`, and one sentence naming the per-namespace `using:` forms as the
   documented fallback (SPEC §10.3). Immediately after it, the
   callout: **no `<StyleInclude>` in `App.axaml`, no theme resources, no XAML shipped in the
   package** — because `Icon` derives from `Control` and renders itself (SPEC §10.2).
3. **The `Icon` control** — a property table (`Kind`, `Weight`, `IconSet`, `IconName`, `Variant`,
   `Foreground`, `Size` default 16, `Stretch` default `Uniform`; `IconSet` wins over `Kind`/`Weight`
   when set), then the three behaviours worth stating: `Foreground` is an `AddOwner` of
   `TextElement.Foreground`, so an icon inside a `Button`/`MenuItem`/`TextBlock` scope **inherits
   that scope's brush and follows theme switches with no binding written by the consumer**; an
   explicit `Width`/`Height` wins over `Size`; the control **never throws** from render or a property
   change (a missing glyph paints nothing — the previewer stays alive). One line on accessibility:
   decorative by default, not focusable, set `AutomationProperties.Name` if a screen reader should
   announce it (SPEC §10.2, §15).
4. **Control vs markup extension — which to use** — the load-time-vs-live distinction, stated as the
   reason and not as a preference: a markup extension is evaluated **once at load time**, so it can
   never follow a bound brush, a `DynamicResource`, or a theme switch; the control can, and that is
   the capability the retired design structurally could not have (SPEC §10.2). Rule of thumb: use
   `ei:Icon` for anything themed, bound, or interactive; use the extensions for a static
   `Path.Data` / `Image.Source`.
5. **Markup extensions** — `IconGeometryExtension` and `IconImageExtension` with the SPEC §10.3
   usage lines (positional icon, `Weight`, `Brush` defaulting to black) and the naming note: it is
   `IconImage`, not `IconSource`, because it returns a `DrawingImage` and Avalonia has its own
   `IconSource` concept.
6. **Extension methods** — `ToGeometry`, `ToDrawing(brush)`, `ToDrawingImage(brush)`, each one line;
   then the **bold caveat**: `ToGeometry` collapses a multi-layer glyph into a `GeometryGroup` and
   **per-layer opacity is lost — use `ToDrawing` (or the `Icon` control) for duotone** (SPEC §10.1).
7. **Works with any `IIconSet`** — the extensions and the control's `IconSet`/`IconName`/`Variant`
   path take *any* set, including a `SvgIconSet` over your own folder: a short C# + XAML pair
   (build the set once, bind it to `IconSet`). Link `Enigma.Icons` for the details.
8. **Trimming / AOT** — one line: the package is `IsTrimmable`/`IsAotCompatible` on the modern TFMs
   and the build is free of IL2xxx/IL3xxx warnings (SPEC §10.4).
9. **Gallery + licence** — pointer to the gallery sample (an **absolute** repo link — `samples/` is
   not packed); MIT via the **relative** `LICENSE.md`, which is; artwork attribution lives in the
   Phosphor package (absolute link to that sibling README).

### 6. Consistency sweep — the verification pass

Mechanical, and its result is recorded in the completion doc. **State explicitly in the completion
doc that samples were verified against the real signatures, not written from memory.**

1. **C# samples** — every sample checked against the step-1 ledger for type names, namespaces,
   member names, parameter order, default values, and return type; every sample carries the `using`
   directives it needs (SPEC §2.2). Then a real compile: create a throwaway console project **in the
   scratchpad, never in the repo**, `ProjectReference` the three `src` projects, paste every C#
   sample into it, `dotnet build`, discard the project. Any sample that does not compile is fixed in
   the README — never by touching library code.
2. **XAML samples** — checked against the shipped control and extensions (prefix declarations,
   property names, extension names, positional-argument form). Cross-check against the gallery's own
   `.axaml`, which is known to load, and against the headless tests of SPEC §12.3.
3. **Cross-file consistency** — the root README's quick start and each packed README's quick start
   must not disagree; the TFM facts appear once (root README, step 2.5); the Phosphor credit line is
   byte-identical in the root and Phosphor READMEs (SPEC §14.2).
4. **Links** — every relative link in the root README resolves in-repo. In each packed README, audit
   against the step-3 rule: `LICENSE.md` (and `THIRD-PARTY-NOTICES.md` in the Phosphor package) is
   **relative**, because it is packed alongside the README — confirm it with the step-6.5 nupkg
   listing; every link to something **not** packed (`RELEASENOTES.md`, `docs/**`, `samples/**`, a
   sibling package README) is **absolute** against `https://github.com/josueclement/Enigma.Icons`
   (SPEC §10.5). No relative images anywhere in a packed README, and no packed README carries a badge
   (SPEC §13.1). This is the state FEATURE-74DC's D6a audits, so a divergence here is a defect in this
   item, not in 74DC.
5. **Pack wiring** — `dotnet pack -c Release` each of the three `src` projects and list the nupkg
   contents: `README.md` at the package root in all three, `LICENSE.md` in all three,
   `THIRD-PARTY-NOTICES.md` in **`Enigma.Icons.Phosphor` only** (SPEC §14). The nupkgs are build
   output, are not committed, and no version is bumped (74DC owns versions). If a
   `<PackageReadmeFile>` / `<None … Pack="true">` entry is missing, adding that one line is the
   minimal correct fix here — record it as a deviation in the completion doc.
6. **Text hygiene** — all four READMEs LF with a final newline (SPEC §2).

### 7. Close out

Flip the FEATURE-718F row in `docs/roadmap.md` and this file's status header to `DONE`, then write
`docs/done/FEATURE-718F.md`.

## Dependencies & ordering

- **Depends on FEATURE-469B**, and transitively on 21C4 → 24DD → 2DDE → 3950 → 3ADD (roadmap
  "Sequencing & dependencies" §7, SPEC §16). It needs the frozen public surface to document and the
  gallery screenshot to embed. Running it earlier would document an API that can still move.
- **Blocks FEATURE-74DC**, which prepends the release-notes bodies, sets `<PackageReleaseNotes>`,
  writes `docs/RELEASE.md`, and adds the **what's-new callout** — its section, root README only, per
  SPEC §13.2 — on top
  of the text this item finalizes. 74DC only *confirms* the supported-TFM table this item owns.
- **slnx: appends nothing** — SPEC §3.4's growth table lists FEATURE-718F as adding no `<Project>`
  entries; FEATURE-469B already brought the file to its §3.4 end state. `Enigma.Icons.slnx` must be
  byte-unchanged by this item.

## Acceptance criteria

There is **no code change in this item**, so DoD criteria 1–2 are satisfied by *"the solution still
builds clean and the whole suite still passes, and every README code sample is verified against the
shipped signatures"* — the build/test criteria below are regression checks, not new work. Criteria
3–5 apply normally.

- [x] Root `README.md` is a complete landing page: title + intro (umbrella, three packages, what
      each is for), the three packages one line each **with the SPEC §13.1 NuGet + MIT badge pair**
      (root README only; no packed README carries a badge), when-to-use-which guidance, and the MIT
      licence link.
- [x] Root quick start shows **both** paths: XAML (the single
      `xmlns:ei="https://github.com/josueclement/Enigma.Icons"` declaration, `ei:Icon`, and **both**
      markup extensions under that one prefix) and C#
      (`PhosphorIconSet.Instance.GetGlyph(...).ToGeometry()`).
- [x] Root README carries a **single** supported-target-frameworks table — this item's sole section
      (SPEC §13.2) — whose three rows match the three packable csprojs' `<TargetFrameworks>` values,
      plus the SPEC §14.2 Phosphor credit line **verbatim**.
- [x] Root README embeds the gallery screenshot from **`docs/img/gallery.png`** (SPEC §1, §13) with
      alt text, the one-line gallery description, and the `dotnet run` command. Only if
      `docs/done/FEATURE-469B.md` records that no capture was produced does the section fall back to
      the prose description plus the run command, with the missing capture logged as a follow-up —
      either way no broken image link ships.
- [x] The three forward links FEATURE-21C4 left (`src/*/README.md`) resolve to the real files; the
      root README contains no unresolved forward reference and no version number or what's-new
      callout (74DC's, and **root README only** — no packed README carries one, SPEC §13.2). In each
      packed README, `LICENSE.md` (plus `THIRD-PARTY-NOTICES.md` in the Phosphor package) is linked
      **relatively** because it is packed alongside, while every unpacked target (`RELEASENOTES.md`,
      `docs/**`, `samples/**`, sibling package READMEs) is **absolute** against
      `https://github.com/josueclement/Enigma.Icons` — the exact state FEATURE-74DC's D6a audits.
- [x] `src/Enigma.Icons/README.md` documents the model, `IIconSet` (including *variant miss, never a
      silent fallback*), the parser's supported subset **and** the SPEC §5.1 exclusions named
      explicitly with the `Avalonia.Svg` / `Svg.Skia` pointer, and a worked
      `SvgIconSet.FromDirectory` bring-your-own-SVG example.
- [x] `src/Enigma.Icons.Phosphor/README.md` documents the six weights with a visual sense of the
      difference (incl. duotone's 20 %-opacity backing layer), `PhosphorIcon` +
      `PhosphorIconNames`, `PhosphorIconSet` (typed and string surfaces, caching, thread safety),
      the credit line + `THIRD-PARTY-NOTICES.md` pointer, and the artwork-refresh procedure pointing
      at `docs/reference/phosphor/README.md` and the SPEC §8.1 generator invocation — and carries
      **no byte figure and no size-reduction claim** (SPEC §7.3: the format saves no package size).
- [x] `src/Enigma.Icons.Phosphor/README.md` carries the **enum-ordinal caveat** mandated by
      SPEC §8.4: `PhosphorIcon` ordinals are positional and shift on an artwork refresh — **never
      persist the numeric value**; persist `PhosphorIconNames.ToKebabCase(icon)` and read it back with
      `TryParse`; and an artwork refresh is at least a minor version bump for this package.
- [x] `src/Enigma.Icons.Avalonia/README.md` documents the XAML quick start, the **no
      `<StyleInclude>` needed** note with its reason, `Icon` vs the markup extensions **with the
      load-time-evaluation reason the control is the one that follows bound/themed brushes**
      (SPEC §10.2), the `ToGeometry` opacity-loss caveat (SPEC §10.1), and that the extension methods
      work with any `IIconSet`.
- [x] **Every code sample is verified against the shipped signatures, not written from memory:** the
      C# samples compile in a throwaway scratchpad project referencing the three `src` projects, and
      the XAML samples match the shipped control/extension surface (cross-checked against the
      gallery `.axaml`). Evidence recorded in the completion doc.
- [x] Nothing documented that is not shipped, and nothing contradicting SPEC; the only forward-
      looking statement is the single "WPF sibling is post-1.0" line (SPEC §17).
- [x] `dotnet pack -c Release` for each of the three `src` projects yields a nupkg containing
      `README.md` at its root and `LICENSE.md`, with `THIRD-PARTY-NOTICES.md` in
      `Enigma.Icons.Phosphor` only (SPEC §14).
- [x] All four READMEs are LF with a final newline; `Enigma.Icons.slnx` is unchanged (zero
      `<Project>` entries appended, SPEC §3.4); no `.cs`/`.axaml` file is modified.
- [x] **`dotnet build Enigma.Icons.slnx` succeeds with zero warnings** (`TreatWarningsAsErrors`,
      SPEC §2) — unchanged from FEATURE-469B, captured as regression evidence.
- [x] **`dotnet test Enigma.Icons.slnx` — the whole suite green**, no test added or removed by this
      item.
- [x] **Roadmap + this plan flipped to `DONE`** (`docs/roadmap.md` FEATURE-718F row; this file's
      status header). (DoD criterion 4.)
- [x] **Completion doc `docs/done/FEATURE-718F.md` written** — summary, files touched, deviations,
      build/test evidence. (DoD criterion 5.)

## Notes / risks

- **XAML namespace — one `ei:` prefix, settled (SPEC §10.3).**
  `src/Enigma.Icons.Avalonia/Properties/AssemblyInfo.cs` carries two
  `[assembly: XmlnsDefinition("https://github.com/josueclement/Enigma.Icons", …)]` attributes — one
  for `Enigma.Icons.Avalonia`, one for `Enigma.Icons.Avalonia.Markup` — so a single
  `xmlns:ei="https://github.com/josueclement/Enigma.Icons"` reaches the `Icon` control **and** both
  markup extensions. Every XAML sample in all four READMEs uses that one prefix, and the
  per-namespace `using:` forms are named once as the documented fallback. Step 1 confirms both
  attributes shipped; if either is missing that is a defect against FEATURE-3ADD, recorded as a
  follow-up — this item documents the shipped state, it does not add attributes.
- **Gallery screenshot — settled path.** `docs/img/gallery.png` is the single canonical path
  (SPEC §1, §13): FEATURE-469B captures it under a hard criterion of its own, this item embeds it
  from the root README only, never from a packed README, and it is never packed into a nupkg.
- **Canonical URLs and the packed-README link rule — settled (SPEC §10.5, §14; FEATURE-74DC D6a).**
  `RepositoryUrl` and `PackageProjectUrl` are both `https://github.com/josueclement/Enigma.Icons`,
  `RepositoryType` is `git`, and no package carries a `PackageIcon`; the three packable csprojs already
  set them. Link form in a packed README depends on whether the target **ships in the package**:
  `LICENSE.md` (all three packages) and `THIRD-PARTY-NOTICES.md` (Phosphor only) are packed beside the
  README, so they stay **relative** — this is exactly what FEATURE-74DC's D6a requires and audits, and
  an absolute `…/blob/main/LICENSE.md` here would fail that criterion. Everything not packed is
  **absolute** against the canonical URL (`…/blob/main/RELEASENOTES.md`,
  `…/blob/main/docs/reference/phosphor/README.md`, sibling package READMEs). Relative *images* never
  render on nuget.org, so a packed README has none. Packed READMEs carry **no badges** (SPEC §13.1);
  the root README keeps one NuGet + MIT badge pair per package, its MIT badge linking the relative
  `LICENSE.md`, which resolves on GitHub.
- **Skeleton-vs-shipped ownership — settled, no action needed.** SPEC §13's table now reads
  "<owning item> first cut, FEATURE-718F full" on all three packed-README rows, and §13.2 says sole
  ownership governs the **shipped** state. Nothing to argue or reconcile here.
- **Do not paper over a gap.** The sweep in step 6 is the first time the SPEC-described API is read
  end-to-end as a consumer would. If a documented-by-SPEC behaviour is absent or differs (a default,
  a property name, a message), the README follows the **shipped** code and the divergence is written
  up as a follow-up against the owning item — never smoothed over in prose, and never fixed by
  editing library code in this item.
- **Doc-drift seam with FEATURE-74DC.** Keep the TFM table in exactly one place and leave a natural
  slot for the what's-new blockquote right after the intro, so 74DC's release edits are surgical and
  cannot fork the quick start.
- **Packed-README length.** Each packed README is a standalone product page — self-sufficient, no
  dependency on the root README being visible. Some duplication between the root and packed quick
  starts is intended; step 6.3 keeps them from disagreeing.
- **Clean break stays clean.** No README mentions `PhosphorIconsAvalonia`, `IconService`,
  `IconType`, or the old enum, and none offers a mapping table (SPEC §17). A reader coming from the
  retired package gets the new API, not a migration path.
