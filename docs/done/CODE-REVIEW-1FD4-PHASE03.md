# CODE-REVIEW-1FD4-PHASE03 — [Low] `SvgIconSet.FromDirectory` follows symlinked `.svg` files out of its root

**Branch:** `review/code-review-1fd4-phase03-symlinked-files` · **Plan:** `docs/plan/CODE-REVIEW-1FD4.md`

## Summary

`FromDirectory` tested `FileAttributes.ReparsePoint` on each variant **subdirectory** and skipped
symlinked ones, but applied no such test to the **files** it then enumerated. Since
`Path.GetFullPath` does not resolve symlinks, `root/evil.svg → /etc/hosts` produced a path that is
literally inside the root, passed `PathSafety.IsWithin`, and was opened and read — while the XML doc
promised "any resolved path that escapes the root is dropped".

`AddDirectoryFiles` now applies the same portable `FileAttributes.ReparsePoint` test to each file
before indexing it and skips on match, silently, matching the existing "one hostile entry cannot deny
service on an otherwise valid directory" policy. The check runs **before** `PathSafety.IsWithin`,
because it — not `IsWithin` — is the mechanism that actually stops a symlinked file.

`FileAttributes` was kept rather than `FileSystemInfo.LinkTarget` (.NET 6+, ruled out by
`netstandard2.0`), exactly as the directory check already does. The skip is therefore on the
attribute, not on where the link leads: a symlink pointing *inside* the root is skipped too. That is
the conservative reading and it is now what the documentation says.

The `<remarks>` were rewritten to state precisely what is guaranteed — top-directory-only
enumeration, symlinked directories **and** symlinked files skipped, read paths additionally
constrained to the root — with no residual overpromise. `PathSafety.IsWithin` stays, with a comment
recording that it is defence-in-depth against a future change to how files are discovered rather than
the primary containment mechanism.

## Files/modules touched

**Modified**

- `src/Enigma.Icons/SvgIconSet.cs` — the file-level `ReparsePoint` skip in `AddDirectoryFiles`
  (ahead of `IsWithin`), the comment demoting `IsWithin` to defence-in-depth, and the corrected
  *Path safety* `<remarks>` paragraph on `FromDirectory`.
- `tests/Enigma.Icons.UnitTests/SvgIconSetPathSafetyTests.cs` — four new tests appended plus the
  `TryCreateFileSymlink` helper; every pre-existing test in the file is untouched.
- `docs/roadmap.md`, `docs/plan/CODE-REVIEW-1FD4.md` — statuses.

**Modified by the documentation freshness sweep** (both accepted by the user)

- `src/Enigma.Icons/README.md` — the *Hardening* bullet said `FromDirectory` "never follows a
  directory symlink out of the root"; it now names symlinked `.svg` files too.
- `docs/SPEC.md` §6 *Path safety* — same understatement. The plan puts `docs/SPEC.md` out of scope
  and the SPEC was not *wrong* (the code became stricter than it promised, not looser), so this edit
  was raised rather than made quietly, and the user chose to take it. The wording now records the
  file-symlink skip, why `Path.GetFullPath` makes it necessary, and the silent-skip policy.

**Created** — none. **Deleted** — none.

## Deviations & follow-ups

1. **No deviation from the plan.** All three design steps were implemented as written.
2. **The new tests were verified to actually catch the defect.** With the `ReparsePoint` block
   temporarily removed, all four fail (`FromDirectory_DoesNotFollowAFileSymlinkWhenVariantsAreOff`
   reports `Expected: "square" / Actual: "escaped"`); with it restored, all ten tests in the class
   pass. The guard was restored before any of the evidence below was gathered.
3. **A fourth test beyond the plan's criteria:**
   `FromDirectory_SkipsASymlinkedFileThatPointsInsideTheRoot` pins the consequence of testing the
   attribute rather than the target — an in-root symlink is skipped as well. It is a deliberate
   documented behaviour, not an accident, so it is worth a test.
4. **One `FileInfo` stat per candidate file is added to discovery.** Irrelevant in practice: the
   built-in Phosphor set is served from embedded `.dat` resources and never goes through
   `FromDirectory`, and discovery already opened nothing — it is one attribute read per `.svg`, once,
   at construction.
5. **A dangling symlink does not throw.** Verified on this machine (.NET 10): `FileInfo.Attributes`
   returns `ReparsePoint` for a broken link rather than throwing `FileNotFoundException`, so the
   check needs no `try`/`catch` and `FromDirectory`'s documented exception set is unchanged.
6. **Windows is guarded, not asserted.** Creating a file symlink needs elevation or Developer Mode
   there, so `TryCreateFileSymlink` returns `false` on `IOException`,
   `UnauthorizedAccessException` or `PlatformNotSupportedException` and the test calls `Assert.Skip`
   — mirroring the existing directory-symlink test. On Linux all four ran and passed (0 skipped).
7. **Not actioned, by decision of the plan** — recorded so a future reviewer does not re-raise them:
   `SvgIconParser.MaxDocumentBytes` stays process-wide mutable static config (SPEC §5-sanctioned and
   already documented as such), and `Icon.Render`'s blanket `catch (Exception)` stays silent
   (mandated verbatim by SPEC §10.2 and §15; a `#if DEBUG` trace was offered and declined).
8. **No line-ending issues observed** in either touched file — both LF with a final newline.
9. **No follow-ups.** Nothing here constrains PHASE04, which also touches `SvgIconSet.cs` (the
   `IconNames` `<remarks>` and `new string[0]`) but not `AddDirectoryFiles`.

## Build/test evidence

| Check | Result |
|---|---|
| `dotnet build Enigma.Icons.slnx -c Release` | Build succeeded · **0 Warning(s)**, 0 Error(s) — the count read explicitly, per the Avalonia `AVLN*` caveat |
| `dotnet test --solution Enigma.Icons.slnx` (Debug) | **478 passed**, 0 failed, 0 skipped |
| `dotnet test --solution Enigma.Icons.slnx -c Release` | **478 passed**, 0 failed, 0 skipped (474 before, **+4** new) |
| Generator `--check` | `8 artifacts match the committed bytes` · exit **0** (snapshot extracted to `/tmp/phosphor-flat`, outside the working tree, and removed afterwards) |
| `netstandard2.0` | Compiles — `Enigma.Icons` built all three TFMs; `FileInfo`/`FileAttributes` are `netstandard2.0` surface, no polyfill, no new `PackageReference` |
| Public API | Unchanged — the fix is inside a `private static` method |

### Acceptance criteria

| Criterion | Evidence |
|---|---|
| A symlinked `.svg` inside the root pointing outside it is not in `IconNames` and is a miss for `TryGetGlyph` | `FromDirectory_DoesNotFollowAFileSymlinkOutOfTheRoot` (variant subdirectory) and `FromDirectory_DoesNotFollowAFileSymlinkWhenVariantsAreOff` (root) — both assert `IconNames` is exactly `["square"]` **and** the `TryGetGlyph` miss |
| A symlinked variant subdirectory is still skipped | `FromDirectory_DoesNotFollowADirectorySymlinkOutOfTheRoot`, unmodified and passing |
| Ordinary files, and `variantsFromSubfolders: false`, are unaffected | `SvgIconSetTests` and the five pre-existing `SvgIconSetPathSafetyTests` pass unmodified; `FromDirectory_SkipsASymlinkedFileThatPointsInsideTheRoot` additionally asserts the real `square.svg` beside a skipped link still resolves via `GetGlyph` |
| The new test guards itself on Windows, using `TestSupport/TempDirectory` | `TryCreateFileSymlink` + `Assert.Skip`; both `TempDirectory` (root and outside-root) used throughout |
| The updated `<remarks>` matches the code exactly | The *Path safety* paragraph now claims only top-directory-only enumeration, skipped symlinked directories *and* files, additional root containment, and silent skipping — each one implemented in `AddDirectoryFiles`/`FromDirectory` |
| Bonus — a symlink to a non-SVG target no longer surfaces as `SvgParseException` | `FromDirectory_DoesNotFollowAFileSymlinkToANonSvgTarget`: the entry never enters the index, so the lookup is a plain miss |
