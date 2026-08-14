# SEpedia

SEpedia is an in-game encyclopedia for Space Engineers. It indexes the definitions registered in the current session, including enabled mods, and presents searchable details and relationships through Rich HUD Master.

## Requirements and controls

- Space Engineers with [Rich HUD Master](https://steamcommunity.com/sharedfiles/filedetails/?id=1965654081) enabled on the client.
- Open or close SEpedia with `Ctrl+F1` by default. The binding can be changed on SEpedia's Rich HUD settings page.
- Rich HUD is optional at runtime: if it is absent, indexing remains available internally and the client UI stays disabled safely.

SEpedia covers physical items, components, ores, ingots, ammunition, tools and weapons, consumables, gas bottles, cube blocks, production-menu recipes, planet and asteroid generators, and spawned planets. Relationships link recipes to inputs, outputs, and production blocks; components to blocks; and items to reachable recipes.

## Current limitations

- Base-game definition icons render, including when a modded definition reuses a base-game icon. Arbitrary mod-owned icon textures currently fall back to a text-only header while [research continues](docs/ICON_RESEARCH.md).
- Runtime acceptance still requires an interactive game client. Builds cannot verify Rich HUD input focus, HUD restoration, layout, or heavily modded performance.
- The frontend does not run on dedicated servers.

## Build and deploy

The project targets .NET Framework 4.8 and C# 6 through MDK². Machine-local MDK settings belong in the ignored `mdk.local.ini` file.

```bash
./scripts/verify.sh
./scripts/deploy.sh
```

`verify.sh` performs a headless compile-only build and the script-sandbox guard. `deploy.sh` performs a non-interactive Release package/deploy using the locally configured MDK destination. Do not invoke a bare MDK build in automation because its default behavior may open an interactive window.

Regenerate the tracked base-game icon aliases from a local Space Engineers Content directory with:

```bash
python3 scripts/generate-vanilla-icon-aliases.py "/path/to/SpaceEngineers/Content" Content/Data/SEpediaVanillaIconAliases.sbc
```

Architecture and runtime ownership are described in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md). Third-party provenance is recorded in [THIRD_PARTY.md](THIRD_PARTY.md).
