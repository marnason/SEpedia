# SEpedia Roadmap

SEpedia is a mod-agnostic in-game encyclopedia built entirely from the definitions registered in the current Space Engineers session. It uses only the public ModAPI and keeps its definition/indexing layer independent of Rich HUD Master.

## Invariants

- Never ship or maintain a hardcoded vanilla definition database.
- Preserve definition and relationship identity with `MyDefinitionId`.
- Keep Rich HUD types at the UI boundary and keep client-only code off dedicated servers.
- Treat third-party definition data and Rich HUD availability as fallible; log failures and continue where possible.

## Milestones

1. **Initial vertical slice** — Runtime registry snapshot; searchable definition, recipe, and block-usage indexes; Rich HUD search/list/detail window; linked navigation; persisted toggle bind. Implemented.
2. **Catalog and advanced filters** — Focused physical-item/block/celestial categories; G-menu-aware block browsing; runtime source, grid-size, block-type, and flag facets; spawned-planet tracking; safe local-HUD suppression. Implemented pending in-game acceptance.
3. **Broader definition detail** — Add icons and further type-specific views and production metadata based on real player usage.
4. **Navigation and performance** — Add history and larger-result navigation, profile heavily modded sessions, and chunk or cache work only where measurements justify it.
5. **Release hardening** — Expand multiplayer/dedicated-server coverage, localization behavior, diagnostics, compatibility checks, and Workshop release documentation.

## Current Delivery

The catalog/filter/celestial iteration is the current delivery. Components are the default browse category; recipes and generic definitions remain indexed for links without appearing as browse categories. The remaining acceptance work is the manual in-game, missing-dependency, large-save, and dedicated-server validation described in the delivery handoff; runtime behavior must not be inferred from compilation alone.
