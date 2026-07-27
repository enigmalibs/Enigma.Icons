# CODE-REVIEW-1FD4-PHASE02 — [Medium] `PhosphorIconSet` variant lookup diverges from the `IIconSet` contract

**Branch:** `review/code-review-1fd4-phase02-variant-parity` · **Plan:** `docs/plan/CODE-REVIEW-1FD4.md`

## Summary

`IIconSet` documents **one** normalization rule and applies it to icon names *and* variants:
case-insensitive, accepting kebab-case, `snake_case` or PascalCase, normalized internally to
kebab-case. `SvgIconSet` honoured that for variants by routing them through
`IconNameNormalizer.TryNormalize`; `PhosphorIconSet.TryResolveWeight` did a bare `OrdinalIgnoreCase`
probe of the raw string. So `GetGlyph("acorn", " duotone ")` missed on Phosphor and hit on the
equivalent `SvgIconSet` — two in-house implementations of one documented contract, disagreeing.

`TryResolveWeight` now normalizes first and then probes the existing (still `OrdinalIgnoreCase`)
weight table. Practical impact was always small — all six weight names are single lower-case words,
so casing already worked and only whitespace and separator forms differed — but the contract is now
the same on both sides, which is what a third implementation will be held to.

**The plan's preferred option was taken:** `<InternalsVisibleTo Include="Enigma.Icons.Phosphor" />`
on `Enigma.Icons`, beside the existing `Enigma.Icons.UnitTests` entry. One implementation of the
rule, no public-API growth, no trim/AOT impact, no `PackageReference`; the assembly is unsigned so
no public key is needed, and a family reference is not a third-party dependency (SPEC §2.10). The
inline fallback was rejected precisely because forking the rule is the defect being fixed — a
duplicated normalizer would drift again the next time `IconNameNormalizer` changes.

Nothing else moved: the typed `GetGlyph(PhosphorIcon, PhosphorWeight)` surface is untouched, and a
variant the set does not have is still a **miss**, never a fallback to `regular`.

## Files/modules touched

**Created**

- `tests/Enigma.Icons.Phosphor.UnitTests/VariantParityTests.cs` — 24 tests: a 22-case theory
  asserting `PhosphorIconSet` and an equivalent on-disk `SvgIconSet` agree hit-for-hit and
  miss-for-miss on every variant spelling, plus reference-equality across spellings and the
  no-fallback rule under a normalized-but-unknown variant.
- `tests/Enigma.Icons.Phosphor.UnitTests/TestSupport/TempDirectory.cs` — a deliberate twin of the
  helper in `Enigma.Icons.UnitTests`; the two test projects share no assembly and neither is
  packable, so a copy beats a fourth project created only to hold it. Noted in its `<remarks>`.

**Modified**

- `src/Enigma.Icons/Enigma.Icons.csproj` — the second `InternalsVisibleTo`, with a comment naming
  this phase and the reason.
- `src/Enigma.Icons.Phosphor/PhosphorIconSet.cs` — `using Enigma.Icons.Internal;`;
  `TryResolveWeight` normalizes before probing (a `null` normalization result is a miss, matching
  the `SvgIconSet` path); the `variant` parameter docs on both string-surface overloads now state
  the normalization rather than only "case-insensitively".
- `tests/Enigma.Icons.Phosphor.UnitTests/PhosphorIconSetTests.cs` — see *Deviations*.
- `docs/roadmap.md`, `docs/plan/CODE-REVIEW-1FD4.md` — statuses.

**Modified by the documentation freshness sweep** (accepted by the user)

- `src/Enigma.Icons.Phosphor/README.md` — the "Lookup is case-insensitive and accepts kebab-case,
  `snake_case` or PascalCase" sentence was true only for icon names until this phase; it now says
  the same applies to variant names, matching how `src/Enigma.Icons/README.md` already words it.

## Deviations & follow-ups

1. **One existing assertion had to move — deliberately, not to paper over a regression.**
   `AVariantTheSetLacks_IsAMissAndNeverFallsBackToRegular` carried `[InlineData("regular ")]`, a
   trailing-space variant pinned as a **miss**. That is exactly the behaviour this phase corrects, so
   the case was removed and replaced with spellings that still miss *after* normalization:
   `"   "` (whitespace-only → normalizes to nothing), `"duo-tone"` and `"DuoTone"` (both normalize to
   `duo-tone`, which is not a weight). The empty-string case and the `heavy`/`outline` cases are
   unchanged. Every other test in the file passes untouched.
2. **`NameLookup_MissesOnARunOfUpperCaseLetters` is unaffected and stays.** That gap is on the icon
   *name* path (`PhosphorIconNames.TryParse`, generated code), not the variant path, so this phase
   does not close it. It remains the one documented divergence between the two sets, recorded in
   `docs/done/FEATURE-3950.md`.
3. **Not actioned, by decision of the plan** — recorded here so a future reviewer does not re-raise
   them: `SvgIconParser.MaxDocumentBytes` stays process-wide mutable static config (SPEC §5-sanctioned,
   already documented as such), and `Icon.Render`'s blanket `catch (Exception)` stays silent
   (mandated verbatim by SPEC §10.2 and §15; a `#if DEBUG` trace was offered and declined).
4. **No line-ending issues observed** in any touched or created file — all LF with a final newline.
5. **No follow-ups.** Nothing in this phase constrains PHASE03 or PHASE04.

## Build/test evidence

| Check | Result |
|---|---|
| `dotnet build Enigma.Icons.slnx -c Release` | Build succeeded · **0 Warning(s)**, 0 Error(s) — the count read explicitly, per the Avalonia `AVLN*` caveat |
| `dotnet test --solution Enigma.Icons.slnx` (Debug) | **474 passed**, 0 failed, 0 skipped |
| `dotnet test --solution Enigma.Icons.slnx -c Release` | **474 passed**, 0 failed, 0 skipped (444 before, **+30** new) |
| Generator `--check` | `8 artifacts match the committed bytes` · exit **0** |
| `netstandard2.0` | Compiles — `Enigma.Icons` and `Enigma.Icons.Phosphor` both built all three TFMs; `IconNameNormalizer` uses only `StringBuilder` and `char`, no polyfill |
| `Enigma.Icons.Phosphor` `PackageReference` count | **zero** on every TFM — the csproj was not touched |

### Acceptance criteria

| Criterion | Evidence |
|---|---|
| `" Duotone "`, `"DUOTONE"`, `"duotone"` all resolve to the same reference-equal glyph | `VariantParityTests.EverySpellingOfAWeightResolvesToTheSameGlyphInstance`; `PhosphorIconSetTests.VariantLookup_NormalizesTheSameWayAnIconNameDoes` |
| An unknown variant (`"heavy"`) is still a miss, never a fallback | `AVariantTheSetLacks_IsAMissAndNeverFallsBackToRegular`; `VariantParityTests.ANormalizedVariantStillDoesNotFallBackToRegular` (also covers `" Heavy "`, so normalization does not soften the rule) |
| `variant: null` means `regular`; a null `icon` is a miss for `TryGetGlyph` and `ArgumentNullException` from `GetGlyph` | `ANullVariant_MeansTheDefaultVariant`, `ANullIconName_IsAMissForTryGetGlyphAndThrowsFromGetGlyph` — both unmodified and passing |
| A test asserts `PhosphorIconSet` and an equivalent `SvgIconSet` agree on the same set of variant spellings | `VariantParityTests.BothSetsAgreeOnAVariantSpelling` — 22 spellings, 14 that must hit and 8 that must miss |
| `Enigma.Icons.Phosphor` still declares zero `PackageReference` entries on every TFM | Unchanged csproj; three-TFM Release build clean |
