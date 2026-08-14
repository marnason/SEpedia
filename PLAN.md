# SEpedia Roadmap

SEpedia is a mod-agnostic in-game encyclopedia built entirely from the definitions registered in the current Space Engineers session. It uses only the public ModAPI and keeps its definition/indexing layer independent of Rich HUD Master.

## Invariants

- Never ship or maintain a hardcoded vanilla definition database.
- Preserve definition and relationship identity with `MyDefinitionId`.
- Keep Rich HUD types at the UI boundary and keep client-only code off dedicated servers.
- Treat third-party definition data and Rich HUD availability as fallible; log failures and continue where possible.

## Milestones

1. **Initial vertical slice** — Runtime registry snapshot; searchable definition, recipe, and block-usage indexes; Rich HUD search/list/detail window; linked navigation; persisted toggle bind. Implemented.
2. **Catalog and advanced filters** — Focused physical-item/block/celestial categories; G-menu-aware block browsing; runtime source, grid-size, block-type, and flag facets; spawned-planet tracking; safe local-HUD suppression. Completed, with performance and final container-based layout accepted in game.
3. **Broader definition detail** — Production-menu recipe browsing; linked input, output, production-block, and item-usage views; bounded relationship paging; and a layered production icon header. Implemented and verified in game. Base-game icons render for both vanilla definitions and modded definitions that reuse them. Custom mod-supplied icons remain text-only and continue under the next milestone.
4. **Universal mod-supplied icons** — Keep investigating until SEpedia can dynamically render custom icons supplied by arbitrary enabled mods. Treat a working route as discoverable: inspect source from Rich HUD mods that render custom resources, Rich HUD Master and client internals, and Space Engineers' native G-menu/inventory icon lookup and texture-resolution code. Track each hypothesis against vanilla icons, modded definitions reusing base resources, and genuinely mod-owned textures so failure in one route is not generalized to every route. Preserve bounded rendering and clean fallbacks during experiments. Complete only when custom icons can be resolved automatically without per-mod edits, or when the user explicitly accepts a different compatibility architecture.
5. **Navigation and performance** — Add history and larger-result navigation, profile heavily modded sessions, and chunk or cache work only where measurements justify it.
6. **Release hardening** — Expand multiplayer/dedicated-server coverage, localization behavior, diagnostics, compatibility checks, and Workshop release documentation.

## Current Delivery

The catalog/filter/celestial iteration and Broader Definition Detail are implemented and verified in game. Components remain the default browse category. Menu-reachable recipe browsing and its linked input, output, production-block, and item-usage views are accepted. The bounded layered header resolves packaged base-game path aliases for vanilla definitions and mods that reuse those resources, while unresolved custom mod textures fall back cleanly to text.

The maintenance baseline following that milestone is implemented: definition construction, extraction, diagnostics, and relationship discovery have explicit ownership; catalog queries are read-only; detail composition is separated from Rich HUD rendering; dynamic controls share bounded paging; lifecycle cleanup is idempotent; and vendor/generated content has documented provenance. Universal custom mod-icon resolution remains milestone 4 and is not redefined or closed by this cleanup. Missing-dependency, dedicated-server, and release-hardening coverage remain later milestones.
