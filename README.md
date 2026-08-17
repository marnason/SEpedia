# SEpedia
[![badge](https://shieldcn.dev/badge/vibecoded%20with-Codex.svg?mode=light&logo=ri%3ABsOpenai&logoColor=ffffff&brand=openai)](https://superintelligence-statement.org/) ![built in Faroe Islands](https://shieldcn.dev/flag/fo.svg)

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

`verify.sh` runs the script-sandbox guard, a locked restore, and a Debug build with MDK packaging disabled. `deploy.sh` calls the shared packaging entry point in local MDK Release mode and deploys through the locally configured MDK destination. GitHub Actions calls the same entry point in source-only mode because Space Engineers compiles scripted mod source at runtime. Every package build regenerates `thumb.png` from the thumbnail generator's current `main` branch and packages that fresh output. Do not invoke a bare MDK build in automation because its default behavior may open an interactive window.

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
- The **Stable release** workflow is started manually from GitHub Actions with a SemVer version and a required changelog. It packages the latest `main`, uploads that source package to the existing Steam Workshop item after attended mobile approval, and only then creates a normal GitHub Release marked as latest. The same changelog is published to Steam and GitHub.

Configure these repository-level GitHub Actions settings before the first stable release:

- Variable `STEAM_WORKSHOP_URL`: `https://steamcommunity.com/sharedfiles/filedetails/?id=3784965557`
- Secret `STEAM_USERNAME`: the username of the Steam account allowed to update the item
- Secret `STEAM_PASSWORD`: that account's password

Do not configure an authenticator shared secret or a reusable Steam session. Steam Guard remains in the official mobile app. Before enabling live publication, manually run **Steam mobile approval check** on a GitHub-hosted runner and approve its expected login notification from the phone. This login-only workflow has closed terminal input, waits up to ten minutes, modifies no Workshop content, and retains no session after its ephemeral runner is discarded.

For a stable release, start **Stable release** while available to approve the new hosted-runner login from the Steam mobile app. SteamCMD is explicitly non-interactive: it cannot fall back to asking for a password, QR scan, or Steam Guard code. Declined, missing, or timed-out approval stops the workflow before it creates a GitHub Release.

Workshop uploads are never retried automatically. If SteamCMD reports an ambiguous timeout or network failure after showing upload progress, inspect the [SEpedia Workshop item](https://steamcommunity.com/sharedfiles/filedetails/?id=3784965557) before rerunning anything. If Steam succeeds but GitHub Release creation fails, use GitHub's **Re-run failed jobs** operation so only the GitHub Release job is retried; rerunning all jobs would submit the Workshop update again.

## Roadmap

- Improve navigation and larger-result browsing, then optimize heavily modded sessions where profiling justifies it.
- Harden multiplayer, dedicated-server, localization, compatibility, and diagnostics.

Architecture and runtime ownership are described in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md). Third-party provenance is recorded in [THIRD_PARTY.md](THIRD_PARTY.md).
