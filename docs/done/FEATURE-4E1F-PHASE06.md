# FEATURE-4E1F-PHASE06 — Documentation

**Branch:** `feature/feature-4e1f-phase06-docs` · **Plan:** `docs/plan/FEATURE-4E1F.md`

## Summary

The studio is now documented where the solution keeps its documentation.

**`docs/SPEC.md` §18** is the new authoritative section: why an app needs both an `.ico` and
standalone PNGs, the composition model and its ranges, the rendering rules (including the two that
are easy to get wrong — premultiplied alpha and the obsolete `Save` overload), the `.ico` container
format byte by byte with the `PngFrameThreshold` rationale and its one measured cost, the UI's
load-bearing decisions, and the dependency and testing split. The §1 repository tree grew the two new
projects; §3.4 was already corrected in PHASE01.

**`tools/Enigma.Icons.AppIconStudio/README.md`** is the user-facing page: what each control does and
what it defaults to, what gets written, and — the part that makes the tool useful — the four XML
snippets that wire the output into an Avalonia app's csproj, window, About view and Linux packaging,
with `Enigma.MarkdownEditor` named as the worked example.

The root `README.md` gained an *App icon studio* section beside the existing *Gallery* one and a
fourth bullet under *Documentation*. `CLAUDE.md` gained the run command and the architecture entry.

## Files/modules touched

**Created**

- `tools/Enigma.Icons.AppIconStudio/README.md` — not packed; the project packs nothing.

**Modified**

- `docs/SPEC.md` — new **§18** (six subsections), and the §1 tree.
- `README.md` — an *App icon studio* section after *Gallery*, and a *Documentation* bullet. The
  badges, the what's-new callout and every package section are untouched (SPEC §13.2).
- `CLAUDE.md` — the run command beside the gallery's, the studio's entry in the architecture block
  with its `Avalonia.Controls.ColorPicker` note, the restated "`dotnet pack` still applies to exactly
  three projects", and the status paragraph now naming `FEATURE-4E1F` as `DONE`.
- `tools/…/Export/IcoWriter.cs` — **comment only**, see *Deviations* 1.
- `docs/roadmap.md`, `docs/plan/FEATURE-4E1F.md` — statuses; the item's own row flips to `DONE` with
  this, its final phase.

Untouched, as the plan requires: everything under `src/`, every `<Version>` and
`<PackageReleaseNotes>`, `RELEASENOTES.md`, `THIRD-PARTY-NOTICES.md`, the gallery sample, and every
generated Phosphor asset.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Where does the studio go in the root README? | Its own section after *Gallery*, plus a *Documentation* bullet | There is no "repository layout" section to add to — the plan's wording assumed one. *Gallery* is the existing precedent for "a tool in this repo, here is how to run it", so the studio sits beside it in the same shape. |
| How much goes in SPEC §18 versus the tool README? | §18 carries the rules and the reasons a maintainer must not break; the README carries what a user does and what they get | The same division §11 and the packed READMEs already use. The one deliberate overlap is the GDI+ limitation, which belongs in both: it constrains the format *and* surprises a user. |
| Does §18 restate the measured probe results? | Yes, with the numbers | "PNG frames work" is a claim; "the SDK copies the payload verbatim and GDI+ falls back to 128 px" is a finding. Written so nobody re-derives either. |

## Build/test evidence

Per `dev-workflow`, criteria 1 and 2 are satisfied by the applicable equivalent for a documentation
dev — but this phase touched a source file's comments, so both were run in full anyway:

| Check | Result |
|---|---|
| `dotnet build Enigma.Icons.slnx` | Build succeeded · **0 Warning(s)**, 0 Error(s) |
| `dotnet test --solution Enigma.Icons.slnx` | **667 total · 662 passed · 0 failed · 5 skipped** — unchanged from PHASE05, as a comment-only code change should leave it |
| Every constant quoted in the new docs | Cross-checked against the source, not assumed: `PreviewSizePx` 256 · `PreviewDelay` 150 ms · `ThumbnailSizes` 64/48/32/16 · `OfferedIcoSizes` 16/24/32/48/64/128/256 · `OfferedPngSizes` 16/32/64/128/256/512/1024 · `DefaultPngSizes` 256/512/1024 · `PngFrameThreshold` 256 · defaults 0.22 / 0.60 / 45° / `#3B72F0` / `#1E3A8A` / white / Fill / `markdown-logo` |
| Line endings | **0 carriage-return bytes** in every file this item created or edited, each ending in a newline (SPEC §2.7) — verified with a byte count, and `git ls-files --eol` reports `i/lf w/lf` throughout |

## Deviations & follow-ups

**1. A measured figure was wrong and is corrected here — the plan said this phase changes no code, and
it changed one comment.** PHASE03 recorded that the seven-size all-BMP icon "runs to about 285 KB".
285,478 bytes is the size of `Enigma.MarkdownEditor`'s **four**-frame icon (16/32/48/256), not of the
studio's seven-frame set. Computed and cross-checked while writing §18:

| Icon | Bytes |
|---|---|
| Seven frames (16/24/32/48/64/128/256), all BMP | 372,526 |
| The same seven, hybrid — PNG at 256 | **107,580** (measured on a real export) |
| Four frames (16/32/48/256), all BMP | 285,478 — which the same arithmetic reproduces **exactly**, matching `Enigma.MarkdownEditor`'s committed `app.ico` byte for byte |

That last row is a genuine check on the DIB layout: an independent formula landing on the exact byte
count of an icon this repository did not produce says the header, stride and AND-mask arithmetic are
right. The corrected numbers are now in `IcoWriter`'s XML docs, SPEC §18.4 and the tool README. The
saving is 3.5×, better than the 2.7× PHASE03 claimed — the conclusion did not change, only the
arithmetic behind it.

`docs/done/FEATURE-4E1F-PHASE03.md` **keeps the superseded figure**: it is the record of what that dev
concluded, and rewriting a merged completion doc falsifies the history rather than correcting it. This
entry is the correction.

**2. The root README has no "repository layout" section**, which the plan's step 3 assumed. Handled as
above — a section in the shape of the existing *Gallery* one. Nothing SPEC §13.2 reserves was touched.

**3. No screenshot was committed.** SPEC §1 makes `docs/img/gallery.png` the single permitted
screenshot path, and the studio's verification captures stayed in the scratchpad. If a studio
screenshot is ever wanted in the README, amending §1 is the prerequisite, not adding the file.

**4. Line endings (recommendation only).** Every file is LF with a final newline and `.gitattributes`
already enforces it. No action taken, and none needed.
