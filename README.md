# SEpedia

SEpedia is an in-game encyclopedia for Space Engineers. It indexes the definitions registered in the current session, including enabled mods, and presents searchable details and relationships through Rich HUD Master.

## Requirements and controls

- Space Engineers with [Rich HUD Master](https://steamcommunity.com/sharedfiles/filedetails/?id=1965654081) enabled on the client.
- Open or close SEpedia with `Ctrl+F1` by default. The binding can be changed on SEpedia's Rich HUD settings page.
- Rich HUD is optional at runtime: if it is absent, indexing remains available internally and the client UI stays disabled safely.

SEpedia covers physical items, components, ores, ingots, ammunition, tools and weapons, consumables, gas bottles, cube blocks, production-menu recipes, planet and asteroid generators, and spawned planets. Relationships link recipes to inputs, outputs, and production blocks; components to blocks; and items to reachable recipes.

## Current limitations

- Definition details are intentionally text-only; SEpedia does not collect or render definition-supplied icons.
- Runtime acceptance still requires an interactive game client. Builds cannot verify Rich HUD input focus, HUD restoration, layout, or heavily modded performance.
- The frontend does not run on dedicated servers.

## Build, package, and deploy

The project targets .NET Framework 4.8 and C# 6 through MDK². The repository pins the .NET SDK and NuGet dependency graph; packaging also requires Go so it can regenerate the Workshop thumbnail. Machine-local MDK settings belong in the ignored `mdk.local.ini` file.

```bash
./scripts/verify.sh
./scripts/deploy.sh
```

`verify.sh` runs the script-sandbox guard, a locked restore, and a Debug build with MDK packaging disabled. `deploy.sh` calls the shared packaging entry point in local MDK Release mode and deploys through the locally configured MDK destination. GitHub Actions calls the same entry point in source-only mode because Space Engineers compiles scripted mod source at runtime. Every package build regenerates `thumb.png` from the pinned thumbnail generator and packages that fresh output. Do not invoke a bare MDK build in automation because its default behavior may open an interactive window.

For an isolated CI-style package, provide explicit reference, staging, and archive paths:

```bash
./scripts/package.sh \
  --stage /tmp/sepedia-package \
  --archive /tmp/SEpedia-1.0.0.zip
```

The resulting ZIP contains one top-level `SEpedia/` folder ready to extract into a Space Engineers local Mods directory. Its adjacent `.sha256` file verifies the archive. Fresh packages intentionally exclude Steam Workshop ownership metadata and all compiled binaries.

## Releases

The repository has two release paths, both backed by `scripts/package.sh`:

- Every push to `main` creates a timestamped GitHub pre-release. Nightlies are retained on GitHub only and are never uploaded to Steam.
- The **Stable release** workflow is started manually from GitHub Actions with a SemVer version and optional maintainer notes. It always packages the latest `main` commit and creates a normal GitHub Release marked as latest.

Before the first stable release, configure the repository Actions variable `STEAM_WORKSHOP_URL` with the full public SEpedia Workshop item URL. Stable publishing fails early when that variable is absent or invalid, and its release description begins with the Workshop installation link. Steam publication itself remains manual; the workflows neither request nor store Steam credentials.

To bootstrap the Workshop listing, download a nightly archive, extract its `SEpedia/` folder into the local Mods directory, and upload it with Space Engineers. Once the item exists, set `STEAM_WORKSHOP_URL` and dispatch the stable workflow.

## Roadmap

- Improve navigation and larger-result browsing, then optimize heavily modded sessions where profiling justifies it.
- Harden multiplayer, dedicated-server, localization, compatibility, and diagnostics.

Architecture and runtime ownership are described in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md). Third-party provenance is recorded in [THIRD_PARTY.md](THIRD_PARTY.md).
