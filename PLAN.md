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
3. **Broader definition detail** — Add icons and further type-specific views and production metadata based on real player usage. In progress: production-menu recipe browsing and bounded relationship views are implemented. The production icon header supports base-game GUI icons regardless of definition origin and same-origin transparent materials supplied by cooperating mods. Custom icons from non-cooperating mods remain text-only under the Workshop-only architecture.
4. **Navigation and performance** — Add history and larger-result navigation, profile heavily modded sessions, and chunk or cache work only where measurements justify it.
5. **Release hardening** — Expand multiplayer/dedicated-server coverage, localization behavior, diagnostics, compatibility checks, and Workshop release documentation.

## Current Delivery

The catalog/filter/celestial iteration is complete; its advanced-filter performance and final responsive layout have been accepted in game. Components remain the default browse category. Menu-reachable recipe browsing and its linked input, output, production-block, and item usage views are implemented and awaiting final runtime acceptance. The icon experiment matrix has been removed: one bounded layered header now resolves packaged base-game path aliases for vanilla or modded definitions and same-context materials for cooperating mods, with text-only fallback for unsupported custom paths. Missing-dependency, dedicated-server, and release-hardening coverage remain later milestones.
