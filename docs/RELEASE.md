# Release runbook

Reusable checklist for publishing a new version of the **`Enigma.Icons`** umbrella to NuGet.

Three libraries publish **together, at the same version, under one tag**:

| Package | Project |
|---|---|
| `Enigma.Icons` | `src/Enigma.Icons/Enigma.Icons.csproj` |
| `Enigma.Icons.Phosphor` | `src/Enigma.Icons.Phosphor/Enigma.Icons.Phosphor.csproj` |
| `Enigma.Icons.Avalonia` | `src/Enigma.Icons.Avalonia/Enigma.Icons.Avalonia.csproj` |

Everything under `samples/` (the gallery) and `tools/` (the asset generator) is `IsPackable=false`
and ships as source only — it is never published.

Replace `X.Y.Z` with the version being released (e.g. `1.0.0`) throughout. The version lives in each
of the three library csprojs (`<Version>`), and all three must agree.

## 1. Pre-release checks

Run from the repository root, on the branch that will be merged:

- [ ] `<Version>X.Y.Z</Version>` set in **all three** library csprojs, and identical across them.
- [ ] `RELEASENOTES.md` has an `X.Y.Z` section for **each** of the three packages
      (`## Enigma.Icons[.X] vX.Y.Z Release Notes`, newest-first; any `(unreleased)` heading renamed
      to `X.Y.Z`).
- [ ] `<PackageReleaseNotes>` in **all three** csprojs summarizes the release and ends by pointing at
      `RELEASENOTES.md`.
- [ ] The root `README.md` "what's new" callout reflects `X.Y.Z`. (The three packed READMEs carry no
      callout and no badges — that is by design, not an omission.)
- [ ] `<TargetFrameworks>` reflect the policy — `netstandard2.0;net8.0;net10.0` for `Enigma.Icons`
      and `Enigma.Icons.Phosphor`, `net8.0;net10.0` for `Enigma.Icons.Avalonia` (Avalonia 12 is
      net8+). Any change was proposed, confirmed, and logged in `RELEASENOTES.md` *Compatibility* and
      in the root README's supported-target-frameworks table.
- [ ] Clean, warning-free build across all TFMs:
      ```bash
      dotnet build Enigma.Icons.slnx -c Release
      ```
      Avalonia's XAML compiler reports `AVLN*` diagnostics from an MSBuild task, and
      `TreatWarningsAsErrors` does **not** promote those — read the warning count, do not trust the
      exit code alone.
- [ ] Full test suite green:
      ```bash
      dotnet test --solution Enigma.Icons.slnx -c Release
      # If the test apphost can't find the runtime, prefix: DOTNET_ROOT=~/.dotnet
      ```
- [ ] README samples verified against the built version.

## 2. Merge to the default branch

Merge the release branch into `main` via a pull request (or fast-forward), then check it out
locally:

```bash
git switch main
git pull
```

## 3. Tag the release

All three packages ship under a **single shared tag**. Match the repo's existing tag convention — run
`git tag` to see how prior releases were tagged (bare `X.Y.Z` vs. `vX.Y.Z`). The repo had no tags at
1.0.0, so the convention is a **bare** `X.Y.Z` tag. Tag the merge commit and push the tag:

```bash
git tag X.Y.Z
git push origin X.Y.Z
```

## 4. Pack

`GeneratePackageOnBuild` is **off** for all three libraries, so no `.nupkg` is produced on an ordinary
build — pack explicitly in Release to get the artifacts you publish:

```bash
dotnet pack src/Enigma.Icons/Enigma.Icons.csproj                   -c Release -o ./artifacts
dotnet pack src/Enigma.Icons.Phosphor/Enigma.Icons.Phosphor.csproj -c Release -o ./artifacts
dotnet pack src/Enigma.Icons.Avalonia/Enigma.Icons.Avalonia.csproj -c Release -o ./artifacts
```

This writes `./artifacts/<PackageId>.X.Y.Z.nupkg` plus a matching `.snupkg` for each. `./artifacts/`
is git-ignored. Confirm the version in each filename matches the tag, and inspect the contents: each
package bundles `README.md` and `LICENSE.md`, `Enigma.Icons.Phosphor` **also** bundles
`THIRD-PARTY-NOTICES.md` (a licence obligation — the other two must not), and the dependency groups
read as expected: `Enigma.Icons` with no dependencies, `Enigma.Icons.Phosphor` on `Enigma.Icons`
X.Y.Z, `Enigma.Icons.Avalonia` on `Enigma.Icons.Phosphor` X.Y.Z plus `Avalonia`.

## 5. Push to NuGet

Publish **in dependency order** — core first, then Phosphor, then Avalonia — so each package is
indexed before the packages that depend on it. Use a NuGet API key with push rights for all three
ids:

```bash
dotnet nuget push ./artifacts/Enigma.Icons.X.Y.Z.nupkg \
  --api-key <NUGET_API_KEY> \
  --source https://api.nuget.org/v3/index.json

dotnet nuget push ./artifacts/Enigma.Icons.Phosphor.X.Y.Z.nupkg \
  --api-key <NUGET_API_KEY> \
  --source https://api.nuget.org/v3/index.json

dotnet nuget push ./artifacts/Enigma.Icons.Avalonia.X.Y.Z.nupkg \
  --api-key <NUGET_API_KEY> \
  --source https://api.nuget.org/v3/index.json
```

`dotnet pack` also emits a `.snupkg` symbols package alongside each `.nupkg`; pushing the `.nupkg`
uploads the matching symbols automatically. The API key is a secret — never commit or echo it.

## 6. Post-publish verification

For **each** of `Enigma.Icons`, `Enigma.Icons.Phosphor` and `Enigma.Icons.Avalonia`:

- [ ] The package page shows the new version (indexing can take a few minutes):
      <https://www.nuget.org/packages/Enigma.Icons>,
      <https://www.nuget.org/packages/Enigma.Icons.Phosphor>,
      <https://www.nuget.org/packages/Enigma.Icons.Avalonia>.
- [ ] The root README's NuGet badge for that package resolves to `X.Y.Z` (shields.io caches briefly).
- [ ] A scratch project can restore the new version:
      ```bash
      dotnet add package Enigma.Icons           --version X.Y.Z
      dotnet add package Enigma.Icons.Phosphor  --version X.Y.Z
      dotnet add package Enigma.Icons.Avalonia  --version X.Y.Z
      ```
- [ ] The GitHub release/tag `X.Y.Z` is present and its notes match `RELEASENOTES.md`.
