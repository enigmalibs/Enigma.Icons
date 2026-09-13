# FEATURE-4E1F-PHASE03 — ICO container writer & exporter

**Branch:** `feature/feature-4e1f-phase03-ico-export` · **Plan:** `docs/plan/FEATURE-4E1F.md`

## Summary

The studio can now write its output. Two layers:

`Export/IcoWriter` assembles frames into the bytes of a Windows `.ico` — pure byte assembly, no
Avalonia, no file system, which is what lets the container format be tested to the offset. Frames of
256 px are stored as **PNG files**, smaller ones as **32-bpp BMP DIBs** with a zeroed AND mask;
`IcoFrame` carries a payload in its input form (straight, top-down BGRA) and the writer does the
bottom-up flip and the `BITMAPINFOHEADER`.

`Export/IconExporter` turns an `ExportRequest` into files: `<base>.ico` and `<base>-<size>.png`, the
exact names an Avalonia app consumes. It **rasterizes everything before opening a single file**, so a
rendering failure leaves the output directory untouched rather than half-written. `ExportRequest`
normalizes and validates on construction — sizes de-duplicated and sorted, base name required to be a
plain file name — so a request that exists is a request that can be run.

The whole chain was verified end to end outside the test suite: the studio's own `.ico` was generated,
pointed at by a throwaway project's `<ApplicationIcon>`, built, and the resulting `.exe`'s icon
extracted and looked at. It is the blue plate with the white glyph.

## Files/modules touched

**Created**

- `tools/…/Export/IcoFrameFormat.cs` — `Dib` / `Png`.
- `tools/…/Export/IcoFrame.cs` — one validated frame; `FromPixels` / `FromPng`.
- `tools/…/Export/IcoWriter.cs` — `Write`, `UsesPngFrame`, `AndMaskStride`, `PngFrameThreshold`.
- `tools/…/Export/ExportRequest.cs` — validated, normalized request; `IsValidBaseName`.
- `tools/…/Export/ExportResult.cs` — the paths actually written.
- `tools/…/Export/IconExporter.cs` — `ExportAsync`.
- `tests/…/TestSupport/TempDirectory.cs` — the house helper, copied.
- `tests/…/TestSupport/IcoReader.cs` — an **independent** ICO parser for the assertions.
- `tests/…/TestSupport/FakeRasterizer.cs` — records what it was asked for; no platform needed.
- `tests/…/IcoWriterTests.cs` (31 tests) · `tests/…/ExportRequestTests.cs` (31) ·
  `tests/…/IconExporterTests.cs` (11) — 73 new, taking the studio assembly from 87 to 160.

**Modified**

- `tools/…/App.axaml.cs` — registers `IconExporter`.
- `docs/roadmap.md`, `docs/plan/FEATURE-4E1F.md` — statuses.

**Documentation freshness sweep:** nothing to do. No README, `CLAUDE.md` or `docs/SPEC.md` statement
describes the export path yet — SPEC §18 is PHASE06's planned work — so this phase falsified nothing.

Untouched, as the plan requires: everything under `src/`, every `<Version>`, `RELEASENOTES.md`,
`THIRD-PARTY-NOTICES.md`, the root README, the gallery sample, and every generated Phosphor asset. No
package reference was added.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Where does the row flip live? | `IcoWriter`, not the rasterizer or `IcoFrame` | Bottom-up rows are a *BMP* fact. Keeping them in the container writer leaves the rasterizer unaware of ICO and `IcoFrame` unaware of Avalonia. |
| AND mask contents | All zeros | With 32 bits per pixel the alpha channel is the authority. A redundant 1-bit mask that disagreed would fringe exactly the rounded corners this tool exists to draw. |
| Payload validation | A DIB payload must be the exact pixel length; a PNG payload must start with the PNG signature | Both mistakes are silent otherwise — the file still "writes", and only a reader discovers it. Two cheap checks at the boundary. |
| Frame ordering | Ascending by size, whatever order was given | Order carries no meaning to a reader (each entry names its own offset), so the tie-break went to the one that makes a hex dump and an icon editor's list readable. |
| Does the exporter create the output directory? | No — missing is a `DirectoryNotFoundException` | Silently creating a mistyped path is how generated output ends up somewhere nobody looks. |
| Rasterize/write ordering | Render every frame first, then write | A rendering failure leaves nothing behind. The tests assert the empty directory. |
| Path containment | Re-checked with `Path.GetFullPath` + an ordinal prefix test, even though `IsValidBaseName` already rejects separators | One string comparison against a failure mode — writing outside the chosen folder — that is only ever noticed afterwards. |
| Test double | A `FakeRasterizer` for the orchestration tests, the real one for a single end-to-end test | The exporter's job is *which* sizes in *which* encoding under *which* names; none of that needs pixels, and the fake makes "was 256 rendered as a PNG rather than as pixels?" directly assertable. |
| ICO reader for tests | Written independently rather than reusing the writer's constants | A reader derived from the writer would agree with it whatever the field order was. |

## Build/test evidence

| Check | Result |
|---|---|
| `dotnet build Enigma.Icons.slnx` | Build succeeded · **0 Warning(s)**, 0 Error(s), first try |
| `dotnet test --solution Enigma.Icons.slnx` | **639 total · 634 passed · 0 failed · 5 skipped** (up from 566/561 — 73 new tests, all green on the first run) |
| Pre-existing skips | The same 5 `SvgIconSetPathSafetyTests` symlink cases. No new skip. |

**End-to-end verification, outside the suite.** A throwaway `[AvaloniaFact]` (deleted; not in the
commit) exported a real design at the full default size set into the scratchpad, then:

| Step | Result |
|---|---|
| Files written | `app.ico` 107,580 B · `app-256.png` 5,430 B · `app-512.png` 11,023 B · `app-1024.png` 23,704 B |
| A throwaway project with `<ApplicationIcon>` pointed at `app.ico` | Build succeeded, 0 warnings |
| `Icon.ExtractAssociatedIcon` on the built `.exe` | 32×32, and the 48 px frame rendered out of the executable shows the blue plate and white glyph correctly |
| `new Icon(path, n, n)` for n = 16, 24, 32, 48, 64, 128 | Each returns exactly that size — every BMP frame is readable |
| `new Icon(path, 256, 256)` | Returns 128×128 — see *Deviations* 2 |

## Deviations & follow-ups

**1. The plan's size estimate for a hybrid icon was wrong, and the code comment is corrected rather
than the format.** The plan said a PNG-at-256 icon would come to "10–15 KB". Measured, the full
seven-frame set is **107 KB** — the saving is real (all-BMP is ~285 KB, which is what the existing
`Enigma.MarkdownEditor` icon weighs) but it is 2.7×, not 20×, because the 128 px BMP frame alone is
64 KB. `IcoWriter`'s XML docs now carry the measured numbers. The threshold was **not** changed: 256
is the conventional boundary, lowering it to 64 would reach ~27 KB but would trade away the one
compatibility property the BMP frames are there for, and 107 KB inside an executable is not a problem
worth that. `PngFrameThreshold` is a single constant if the trade ever needs revisiting.

**2. Measured limitation, now documented: GDI+ cannot read the PNG frame.** `System.Drawing.Icon`
asked for 256 px returns the 128 px BMP frame instead — it has no PNG decoder for ICO payloads. This
affects only that legacy, Windows-only API: the Windows shell, WIC, Skia (so Avalonia) and the .NET
SDK's resource embedder all read the 256 frame, which the `<ApplicationIcon>` probe above confirms
end to end. Recorded in `IcoWriter`'s `<remarks>` so it is not rediscovered as a bug. An app wanting a
large bitmap should use the standalone PNGs, which is what the studio emits them for.

**3. The `<ApplicationIcon>` acceptance check was done here, not in PHASE05 where the plan lists it.**
The format is decided in this phase, so this is where a failure would have mattered; deferring it
would have meant two phases built on an unverified container. PHASE05 will still confirm the same
thing through the UI, which is the part that phase actually owns.

**4. `ExportRequest` gained `IsValidBaseName` as public API, which the plan did not name.** PHASE05's
Generate button has to disable itself on the same rule the constructor enforces, and discovering that
by catching an exception per keystroke would be both slow and wrong. One predicate, one rule, two
callers.

**5. Line endings (recommendation only).** All new files are LF with a final newline; `.gitattributes`
already governs this. No action taken.
