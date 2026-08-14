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
3. **Broader definition detail** — Production-menu recipe browsing; linked input, output, production-block, and item-usage views; bounded relationship paging; and a text-only definition header. Implemented and verified in game.
4. **Navigation and performance** — Add history and larger-result navigation, profile heavily modded sessions, and chunk or cache work only where measurements justify it.
5. **Release hardening** — Expand multiplayer/dedicated-server coverage, localization behavior, diagnostics, compatibility checks, and Workshop release documentation.

## Current Delivery

The catalog/filter/celestial iteration and Broader Definition Detail are implemented and verified in game. Components remain the default browse category. Menu-reachable recipe browsing and its linked input, output, production-block, and item-usage views are accepted. Definition and celestial detail headers are intentionally text-only.

The maintenance baseline following that milestone is implemented: definition construction, extraction, diagnostics, and relationship discovery have explicit ownership; catalog queries are read-only; detail composition is separated from Rich HUD rendering; dynamic controls share bounded paging; lifecycle cleanup is idempotent; and vendor/generated content has documented provenance. Missing-dependency, dedicated-server, and release-hardening coverage remain later milestones.
