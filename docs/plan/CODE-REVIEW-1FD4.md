**Status:** TODO · Multi-phase (4 phases) · Branches `review/code-review-1fd4-phaseNN-<slug>`

# CODE-REVIEW-1FD4 — Post-1.0 review fixes

## Objective

Act on the six accepted findings of the full-solution code review of 2026-07-27, in four
severity-ordered phases. The review covered all eight projects, `docs/SPEC.md`, and the shipped
configuration; it found **no correctness defect** in the parser, the transformer or the generator.
Every item below is a refinement of already-working code.

Verified state at the time of review — this is the baseline every phase must preserve:

| Check | Result |
|---|---|
| `dotnet build Enigma.Icons.slnx -c Release` | Build succeeded · **0 warnings**, 0 errors |
| `dotnet test --solution Enigma.Icons.slnx` | **434 passed**, 0 failed, 0 skipped |
| Generator `--check` | 8 artifacts match the committed bytes · exit 0 |
| `.dat` sizes vs SPEC §7.3 | Exact to the byte (3,951,294 B total) |

## Context

- The 1.0.0 line is **feature-complete**. This item changes no public API shape, adds no dependency,
  and touches no generated asset.
- **Versioning: fold into 1.0.0, no bump.** All three packages are stamped `1.0.0` and packed into
  `artifacts/`, but the repo has **no git remote** and nothing is on nuget.org — 1.0.0 was never
  published, so there is no released artifact to stay compatible with and no consumer to notify.
  Leave `<Version>`, `<PackageReleaseNotes>`, `RELEASENOTES.md` and the root README what's-new
  callout **exactly as FEATURE-74DC wrote them**. The locally packed `artifacts/*.nupkg` become
  stale and are simply re-packed when the user publishes, per `docs/RELEASE.md`.
- **`FEATURE-6FA1` (WPF) stays deferred** and is not touched by any phase.

### Findings considered and deliberately NOT actioned

Record both in each relevant completion doc, so a future reviewer does not re-raise them:

- **`SvgIconParser.MaxDocumentBytes` is process-wide mutable static config** (`SvgIconParser.cs:41-61`).
  Explicitly SPEC §5-sanctioned, and the XML doc already warns that it is process-wide and that a
  test lowering it must restore it. Changing it now would grow the shipped public API of a 1.0
  package to solve a problem nobody has hit. **Left as-is by decision.**
- **`Icon.Render`'s blanket `catch (Exception)`** (`Icon.cs:282`). Mandated verbatim by SPEC §10.2 and
  §15: a throwing render pass takes down the Avalonia previewer for the whole window, and
  observability is deliberately *none* (§2.10 — logging would breach the zero-dependency rule). The
  silence is the decision, not an oversight. A `#if DEBUG` → `Debug.WriteLine` was offered and
  **declined**. **Left as-is by decision.**

## Scope

### In scope
- `src/Enigma.Icons.Avalonia/Icon.cs`, `src/Enigma.Icons.Avalonia/IconGlyphExtensions.cs` (PHASE01)
- `src/Enigma.Icons.Phosphor/PhosphorIconSet.cs`, possibly `src/Enigma.Icons/Enigma.Icons.csproj` (PHASE02)
- `src/Enigma.Icons/SvgIconSet.cs` (PHASE03, PHASE04)
- `src/Enigma.Icons/SvgIconParser.cs` (PHASE04)
- New unit tests in the corresponding `*.UnitTests` projects (PHASE01-03)

### Out of scope
- **Any version bump, `RELEASENOTES.md` edit, `PackageReleaseNotes` edit, or README callout change** —
  see *Versioning* above. Explicitly forbidden in every phase.
- **`Assets/*.dat`, `PhosphorIcon.g.cs`, `PhosphorIconNames.g.cs`** and anything under
  `tools/Enigma.Icons.Generator`. The generator's `--check` must still exit 0 after every phase.
- `docs/SPEC.md`. Where a fix makes the code match the SPEC more closely, the SPEC is already right.
  If a phase genuinely needs a SPEC amendment, stop and raise it rather than editing quietly.
- The gallery sample, the READMEs, and `docs/internals.html` (that is `FEATURE-1608`).
- The two findings listed under *deliberately NOT actioned*.

## Cross-phase constraints

Every phase must hold all of these, not just its own acceptance criteria:

1. **Zero-warning Release build.** SPEC §2.1. Avalonia's `AVLN*` diagnostics are **not** promoted by
   `TreatWarningsAsErrors` — read the warning *count* on any project compiling `.axaml`, do not trust
   the exit code alone.
2. **`netstandard2.0` compiles.** `Enigma.Icons` and `Enigma.Icons.Phosphor` multi-target
   `netstandard2.0;net8.0;net10.0`. No `System.HashCode`, no `Math.Clamp`, no `Dictionary.TryAdd`, no
   `Span<T>` in public API, no default interface members. **No new `PackageReference` on any project**
   (SPEC §2.10) — if a polyfill seems necessary, rewrite the code instead.
3. **XML docs on every public member** (SPEC §2.8 — CS1591 is a build error), `ImplicitUsings`
   disabled so every file declares its own `using` directives, LF with a final newline.
4. **Full suite green**, including the new tests. xUnit v3 / MTP; every Avalonia test method carries
   `[AvaloniaFact]` or `[AvaloniaTheory]`, never a bare `[Fact]`/`[Theory]` — `Geometry.Parse` needs
   the platform render interface even in a pure geometry test.
5. **Generator `--check` exits 0.** Extract the pinned snapshot outside the working tree
   (`.gitignore` has no rule for loose `.svg` files — 9,072 strays would land in the next commit).

---

## PHASE01 — [Medium] `Icon.Render` re-parses geometry on every render pass

**Status:** TODO · Branch `review/code-review-1fd4-phase01-geometry-cache`

**Location:** `src/Enigma.Icons.Avalonia/Icon.cs:309`, `src/Enigma.Icons.Avalonia/IconGlyphExtensions.cs:152`

### Finding

`Icon.DrawLayers` calls `Geometry.Parse(layer.PathData)` for every layer on **every** render pass.
Phosphor path data runs to several hundred bytes per layer, and a window resize, a theme switch or a
scroll through the gallery's virtualized grid re-parses every visible icon each frame.

The existing comment declines a cache:

> No geometry cache: glyph instances are already cached and reference-equal, and a cache keyed by
> path string would only add an invalidation surface.

That objection is sound **for the cache it describes** — but not for a cache keyed on the
`IconGlyph` itself. `IconGlyph` is immutable, and both `PhosphorIconSet` and `SvgIconSet` guarantee a
cached, reference-equal instance per `(icon, variant)`. A `ConditionalWeakTable<IconGlyph, Geometry[]>`
therefore has **no invalidation surface at all**, and its weak keys let a dropped custom `IIconSet`
and its glyphs be collected normally.

### Design

1. Add an internal geometry cache to `Enigma.Icons.Avalonia` —
   `ConditionalWeakTable<IconGlyph, Geometry[]>`, one entry per glyph holding one parsed `Geometry`
   per layer in layer order. Available on `net8.0`/`net10.0`, which is this project's whole TFM set.
2. `Icon.DrawLayers` resolves its geometry through the cache instead of calling `Geometry.Parse`
   directly. The `try`/`catch` in `Render` still covers a parse failure on third-party path data.
3. **Verify before extending the cache to `ToGeometry`/`ToDrawing`.** A `Geometry` instance handed to
   `GeometryGroup.Children` or to `GeometryDrawing.Geometry` may acquire framework parent/owner
   state; sharing one instance across two groups, or between a group and a direct `DrawGeometry`
   call, could misbehave. **Confirm this against Avalonia 12.1.0 with a test before sharing.** If it
   is not provably safe, scope the cache to `Icon.Render` only and say so in the completion doc —
   that is where the per-frame cost is, and it is a perfectly acceptable outcome for this phase.
4. **The `Pen` allocation is a secondary, likely negligible concern — do not over-engineer it.**
   `TryGetLayerPaint` only allocates a `Pen` when `layer.IsStroked`, and the `.dat` resources carry
   no stroke information at all, so the built-in Phosphor path never hits it. It matters only for a
   custom `SvgIconSet` with stroked artwork, where the pen also depends on the caller's brush.
   Hoist it only if it can be done cleanly; otherwise leave it and record why.

### Acceptance criteria

- Two renders of the same glyph use a reference-equal `Geometry` per layer (assert via the internal
  cache, which `InternalsVisibleTo` already exposes to `Enigma.Icons.Avalonia.UnitTests`).
- Behaviour is unchanged for single-layer, multi-layer, duotone (per-layer opacity), stroked and
  `fill="none"` spacer layers — the existing `IconControlTests`, `LayerPaintTests`,
  `IconGlyphExtensionsDrawingTests` and `IconGlyphExtensionsGeometryTests` all still pass unmodified.
- A glyph from a custom `IIconSet` that becomes unreachable is not retained by the cache (weak-key
  behaviour asserted, or the weak-key choice justified in the completion doc if a deterministic test
  proves impractical).
- `Icon.Render` still never throws, including on malformed third-party path data.

---

## PHASE02 — [Medium] `PhosphorIconSet` variant lookup diverges from the `IIconSet` contract

**Status:** TODO · Branch `review/code-review-1fd4-phase02-variant-parity`

**Location:** `src/Enigma.Icons.Phosphor/PhosphorIconSet.cs:239-248` (`TryResolveWeight`), against
`src/Enigma.Icons/IIconSet.cs:13-15` and `src/Enigma.Icons/SvgIconSet.cs:371-385`

### Finding

`IIconSet`'s implementer contract states:

> Name lookup is **case-insensitive** and accepts kebab-case, `snake_case` or PascalCase input,
> normalized internally to kebab-case. **The same applies to variants.**

`SvgIconSet` honours this for variants — it runs them through `IconNameNormalizer.TryNormalize`,
which trims, maps `_`/space/tab to `-`, splits PascalCase and collapses hyphen runs.
`PhosphorIconSet` does a bare `OrdinalIgnoreCase` dictionary probe on the raw string. So
`GetGlyph("acorn", " duotone ")` misses on Phosphor and hits on the equivalent `SvgIconSet`: two
implementations of one documented contract, behaving differently.

Practical impact is small — all six weight names are single lower-case words, so casing already
works and only whitespace and separator forms differ. It is fixed because a contract that two
in-house implementations disagree about will not survive a third one.

### Design

Route the variant through the same normalizer, then probe the existing `OrdinalIgnoreCase` lookup.
`IconNameNormalizer` is `internal` to `Enigma.Icons`, so pick one of:

- **Preferred — `InternalsVisibleTo`.** Add `<InternalsVisibleTo Include="Enigma.Icons.Phosphor" />`
  to `src/Enigma.Icons/Enigma.Icons.csproj`, beside the existing `Enigma.Icons.UnitTests` entry.
  One implementation, no public-API growth, no trim/AOT impact, and a family reference is not a
  third-party dependency (SPEC §2.10). Note the assembly is unsigned, so no public key is needed.
- **Fallback — inline.** If exposing internals across the package boundary is unwanted, normalize
  inline in `TryResolveWeight`. Cheaper to reason about, but it forks the normalization rule; if
  chosen, add a comment in **both** places pointing at the other.

Whichever is chosen, state it and why in the completion doc.

Do **not** change the typed `GetGlyph(PhosphorIcon, PhosphorWeight)` surface, and do **not** relax
the "a variant the set does not have is a miss, never a fallback" rule.

### Acceptance criteria

- `TryGetGlyph("acorn", " Duotone ", out _)`, `"DUOTONE"`, and `"duotone"` all resolve to the same
  reference-equal glyph.
- An unknown variant (`"heavy"`) is still a **miss** — `false` from `TryGetGlyph`,
  `IconNotFoundException` from `GetGlyph` — never a silent fall back to `regular`.
- `variant: null` still means `regular`; a null `icon` is still a miss for `TryGetGlyph` and an
  `ArgumentNullException` from `GetGlyph`.
- A test asserts `PhosphorIconSet` and an equivalent `SvgIconSet` agree on the same set of variant
  spellings — the parity this phase exists to establish.
- `Enigma.Icons.Phosphor` still declares **zero** `PackageReference` entries on every TFM.

---

## PHASE03 — [Low] `SvgIconSet.FromDirectory` follows symlinked `.svg` files out of its root

**Status:** TODO · Branch `review/code-review-1fd4-phase03-symlinked-files`

**Location:** `src/Enigma.Icons/SvgIconSet.cs:465-508` (`AddDirectoryFiles`), against the guarantee in
its own `<remarks>` at `SvgIconSet.cs:97-101`

### Finding

`FromDirectory` tests `FileAttributes.ReparsePoint` on each **variant subdirectory** and skips
symlinked ones — but applies no such test to the **files** it then enumerates.
`Path.GetFullPath` does not resolve symlinks, so `root/evil.svg → /etc/hosts` produces a path that is
literally inside the root, passes `PathSafety.IsWithin`, and is opened and read.

Impact is bounded — a non-SVG target surfaces as an `SvgParseException` rather than as returned
content — but the XML doc promises more than the code delivers:

> Directory symlinks are not followed, and **any resolved path that escapes the root is dropped**.

Related, same location: because enumeration is `TopDirectoryOnly` and every candidate path comes
straight from the enumerator rooted at `root`, `PathSafety.IsWithin` cannot currently reject
anything. It is genuine belt-and-braces and should stay, but the doc should not present it as the
mechanism that stops traversal.

### Design

1. In `AddDirectoryFiles`, apply the same portable `FileAttributes.ReparsePoint` test to each file
   before indexing it, and skip on match — silently, matching the existing "one hostile entry cannot
   deny service on an otherwise valid directory" policy for non-contained paths.
   `FileSystemInfo.LinkTarget` is .NET 6+, so `netstandard2.0` rules out that API — stay on
   `FileAttributes`, exactly as the directory check already does.
2. Update the `<remarks>` to state precisely what is guaranteed: symlinked directories **and**
   symlinked files are skipped; enumeration is top-directory-only; resolved paths are constrained to
   the root.
3. Leave `PathSafety.IsWithin` in place; add a one-line comment noting it is defence-in-depth against
   a future change to the enumeration, not the primary containment mechanism.

### Acceptance criteria

- A symlinked `.svg` inside the root pointing outside it is **not** present in `IconNames` and is a
  miss for `TryGetGlyph`.
- A symlinked variant **subdirectory** is still skipped (existing behaviour, unchanged).
- Ordinary files, and a set with `variantsFromSubfolders: false`, are unaffected — the existing
  `SvgIconSetTests` and `SvgIconSetPathSafetyTests` pass unmodified.
- **The new test guards itself on Windows**, where creating a symlink requires elevation or Developer
  Mode: create the link inside a `try`/`catch` (or probe first) and skip rather than fail. Use the
  existing `TestSupport/TempDirectory` helper.
- The updated `<remarks>` matches the code exactly — no residual overpromise.

---

## PHASE04 — [Low] Documentation and cosmetic sweep

**Status:** TODO · Branch `review/code-review-1fd4-phase04-doc-cosmetic-sweep`

Three unrelated one-line items, grouped so they cost one branch rather than three. No behaviour
change beyond the corrected diagnostic text; **no new tests required**.

### Finding #5 — over-limit message reports UTF-16 code units as bytes

`src/Enigma.Icons/SvgIconParser.cs:81-89`. The size check short-circuits:

```csharp
int size = svg.Length > limit ? svg.Length : Encoding.UTF8.GetByteCount(svg);
```

The short-circuit itself is correct and worth keeping — a UTF-8 byte count is always ≥ the UTF-16
code-unit count, so a string longer than the limit is over it without measuring. But the same value
is then passed to `TooLarge`, which formats it as *"The SVG document is {0} bytes"*. For a document
over the limit that number is a **code-unit count**, and it understates the real size for any
non-ASCII input.

Fix: measure the true byte count for the message when the document is being rejected (the exact
count is only needed on the failure path, so the fast path keeps its short-circuit), or reword the
message so it does not claim to be a byte count. Prefer the former — an accurate number is more
useful to whoever is tuning `MaxDocumentBytes`.

### Finding #6 — `SvgIconSet.IconNames` is the union across variants, undocumented

`src/Enigma.Icons/SvgIconSet.cs:49-63, 76`. The constructor unions every bucket's keys, so for a set
with variants a name in `IconNames` need not resolve in `DefaultVariant`: a Phosphor-style tree
missing one icon in one weight lists that name, yet `TryGetGlyph(name, null, …)` may return `false`.
This is the right behaviour — `IconNames` answers "what icons does this set know about" — but it is
not stated anywhere, and it contrasts with `PhosphorIconSet`, where all 1,512 names exist in all six
weights.

Fix: add an `<inheritdoc/>`-compatible `<remarks>` on the property saying `IconNames` is the union
across all variants and that a listed name is not guaranteed to resolve in every variant, including
the default. Documentation only — do not change the union semantics.

### Finding #7 — `new string[0]`

`src/Enigma.Icons/SvgIconSet.cs:31-32`. Replace with `Array.Empty<string>()`, which is present in
`netstandard2.0` and avoids the (admittedly one-off, static) allocation. Purely cosmetic; included
so the sweep is complete.

### Acceptance criteria

- The over-limit `SvgParseException` message states a true UTF-8 byte count (or no longer claims to
  state one), and the existing over-limit tests in `SvgIconParserErrorTests` still pass — **update an
  assertion only if it asserts the wrong number**, never to paper over a regression.
- `SvgIconSet.IconNames` carries the union caveat in its XML documentation.
- No `new string[0]` remains in `src/`.
- Zero-warning build, 434+ tests green, generator `--check` exit 0.

---

## Definition of Done (per phase)

Per `dev-workflow` v3, each phase is its own dev:

1. Release build succeeds with **zero warnings** (Avalonia `AVLN*` counts read explicitly).
2. The entire suite passes, including the phase's new tests.
3. Every acceptance criterion of that phase is met.
4. `docs/roadmap.md` and this plan's phase status are updated.
5. `docs/done/CODE-REVIEW-1FD4-PHASENN.md` is written and lands in the **same commit** as the code.

Then print the four-row phase progress table and the suggested commit message
(`fix(CODE-REVIEW-1FD4): … (PHASENN)`), and **stop** — the user commits before the next phase's
branch is cut.
