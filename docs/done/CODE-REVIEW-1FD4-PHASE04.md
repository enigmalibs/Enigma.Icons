# CODE-REVIEW-1FD4-PHASE04 — [Low] Documentation and cosmetic sweep

**Branch:** `review/code-review-1fd4-phase04-doc-cosmetic-sweep` · **Plan:** `docs/plan/CODE-REVIEW-1FD4.md`

## Summary

The three unrelated one-line findings the plan grouped into a single branch, all in
`src/Enigma.Icons`:

**Finding #5 — the over-limit message reported UTF-16 code units as bytes.** `SvgIconParser.Parse(string)`
short-circuited with `int size = svg.Length > limit ? svg.Length : Encoding.UTF8.GetByteCount(svg)`
and passed that value to `TooLarge`, which formats it as *"The SVG document is {0} bytes"*. For a
rejected document the number was a code-unit count, understating the true size of any non-ASCII
input. The short-circuit is kept — it is what stops the accept path paying for an exact count — but
the rejection now measures: an oversized string throws `TooLarge(Encoding.UTF8.GetByteCount(svg), limit)`.
The exact count is computed only on the failure path, so the fast path is unchanged.

**Finding #6 — `SvgIconSet.IconNames` is the union across variants, undocumented.** The constructor
unions every bucket's keys, so a set whose tree is missing one icon in one variant still lists that
name while `TryGetGlyph(name, null, …)` may return `false`. That is the right behaviour and it is
unchanged; it is now stated in a `<remarks>` beside the property's `<inheritdoc/>`, contrasting it
with `PhosphorIconSet`, where all 1,512 names exist in all six weights.

**Finding #7 — `new string[0]`.** Replaced with `Array.Empty<string>()` in the `_noVariants`
initializer. Present in `netstandard2.0`; purely cosmetic.

## Files/modules touched

**Modified**

- `src/Enigma.Icons/SvgIconParser.cs` — `Parse(string)`'s size gate split into an over-limit branch
  that measures the true UTF-8 byte count for the message and an exact-count branch for the accept
  path (finding #5).
- `src/Enigma.Icons/SvgIconSet.cs` — `<remarks>` on `IconNames` recording the union semantics
  (finding #6); `new string[0]` → `Array.Empty<string>()` (finding #7).
- `tests/Enigma.Icons.UnitTests/SvgIconParserErrorTests.cs` — one new test (below).
- `docs/roadmap.md`, `docs/plan/CODE-REVIEW-1FD4.md` — PHASE04 status; the item's own row flips to
  `DONE` with this, the final phase.

**Created** — this file.

**Not touched:** every `Assets/*.dat`, `PhosphorIcon.g.cs`, `PhosphorIconNames.g.cs`,
`tools/Enigma.Icons.Generator`, `docs/SPEC.md`, any `<Version>`/`<PackageReleaseNotes>`,
`RELEASENOTES.md`, the READMEs, and the gallery — all out of scope per the plan.

## Deviations & follow-ups

1. **One test added although the plan says "no new tests required".** `Parse_ReportsTheTrueUtf8ByteCountOfAnOverLimitString`
   pins finding #5 with a 200-character `'é'` document (1 code unit, 2 UTF-8 bytes each): the message
   must contain the true byte count `424` and must **not** contain the code-unit count `224`.
   Verified to fail against the pre-fix line and pass after it. The plan's wording is a floor, not a
   prohibition, and findings #6 and #7 are genuinely untestable (documentation and an allocation).
2. **No existing assertion was changed.** The three pre-existing over-limit tests
   (`Parse_RejectsAStringLargerThanTheLimit`, `Parse_RejectsAStreamLargerThanTheLimit`,
   `Parse_AcceptsADocumentExactlyAtTheLimit`) assert `Contains("MaxDocumentBytes")` and a successful
   parse, never a number, so none asserted the wrong value — nothing needed papering over.
3. **Out-of-scope observation, left alone: the stream path's number is a lower bound, not a size.**
   `ReadBounded` deliberately stops at `limit + 1` bytes, so `Parse(Stream)` rejects with
   *"The SVG document is 65 bytes"* even for a 10 MiB stream. That is a different defect from
   finding #5 (an honest byte count that happens to be a floor, rather than the wrong unit), it was
   not raised in the review, and fixing it properly means either reading the whole stream — defeating
   the bounded read that is the DoS protection — or rewording the shared `TooLarge` message.
   **Recorded here as a follow-up candidate; no change made.**
4. **Overflow note on the new measurement.** `Encoding.UTF8.GetByteCount` is now called on strings
   that previously short-circuited past it. A string whose UTF-8 length overflows `int` would throw
   `ArgumentOutOfRangeException` instead of `SvgParseException`; that needs >715 million non-ASCII
   characters (a ~1.4 GiB string), so no guard was added.
5. **Not actioned, by decision of the plan** — recorded so a future reviewer does not re-raise them:
   `SvgIconParser.MaxDocumentBytes` stays process-wide mutable static config (SPEC §5-sanctioned and
   already documented as such), and `Icon.Render`'s blanket `catch (Exception)` stays silent
   (mandated verbatim by SPEC §10.2 and §15; a `#if DEBUG` trace was offered and declined).
6. **No line-ending issues observed** in any touched file — all LF with a final newline.
7. **`CODE-REVIEW-1FD4` is complete with this phase.** All six accepted findings are actioned; the
   two deliberately-declined ones are recorded in three completion docs. The next roadmap item is
   `FEATURE-1608` (`docs/internals.html`); `FEATURE-6FA1` stays deferred.

## Build/test evidence

| Check | Result |
|---|---|
| `dotnet build Enigma.Icons.slnx -c Release` | Build succeeded · **0 Warning(s)**, 0 Error(s) — the count read explicitly, per the Avalonia `AVLN*` caveat |
| `dotnet test --solution Enigma.Icons.slnx` (Debug) | **479 passed**, 0 failed, 0 skipped |
| `dotnet test --solution Enigma.Icons.slnx -c Release` | **479 passed**, 0 failed, 0 skipped (478 before, **+1** new) |
| Generator `--check` | `8 artifacts match the committed bytes` · exit **0** (snapshot extracted to the session scratchpad, outside the working tree) |
| `netstandard2.0` | Compiles — `Enigma.Icons` built all three TFMs; `Array.Empty<T>()` and `Encoding.UTF8.GetByteCount` are `netstandard2.0` surface, no polyfill, no new `PackageReference` |
| Public API | Shape unchanged — one exception *message* differs; the only doc addition is `<remarks>` on an existing property |

### Acceptance criteria

| Criterion | Evidence |
|---|---|
| The over-limit `SvgParseException` message states a true UTF-8 byte count | `Parse(string)` measures with `Encoding.UTF8.GetByteCount` on the rejection branch; asserted by `Parse_ReportsTheTrueUtf8ByteCountOfAnOverLimitString`, which fails against the pre-fix line |
| The existing over-limit tests in `SvgIconParserErrorTests` still pass, unmodified | `Parse_RejectsAStringLargerThanTheLimit`, `Parse_RejectsAStreamLargerThanTheLimit` and `Parse_AcceptsADocumentExactlyAtTheLimit` are untouched and green — no assertion was updated |
| `SvgIconSet.IconNames` carries the union caveat in its XML documentation | `<remarks>` beside the property's `<inheritdoc/>`: the union across variants, not guaranteed to resolve in every variant including `DefaultVariant`, with the `TryGetGlyph` consequence spelled out |
| No `new string[0]` remains in `src/` | `grep -rn "new string\[0\]" src/ tools/ samples/ tests/` → no match |
| Zero-warning build, 434+ tests green, generator `--check` exit 0 | See the evidence table above — 0 warnings, 479 green, `--check` exit 0 |
