# SEpedia Roadmap

SEpedia is a mod-agnostic in-game encyclopedia built entirely from the definitions registered in the current Space Engineers session. It uses only the public ModAPI and keeps its definition/indexing layer independent of Rich HUD Master.

## Invariants

- Never ship or maintain a hardcoded vanilla definition database.
- Preserve definition and relationship identity with `MyDefinitionId`.
- Keep Rich HUD types at the UI boundary and keep client-only code off dedicated servers.
- Treat third-party definition data and Rich HUD availability as fallible; log failures and continue where possible.

## Milestones

1. **Initial vertical slice** — Runtime registry snapshot; searchable definition, recipe, and block-usage indexes; Rich HUD search/list/detail window; linked navigation; persisted toggle bind.
2. **Broader definition detail** — Add useful type-specific views, category/origin filters, icons, and richer production metadata based on real usage.
3. **Navigation and performance** — Add history and larger-result navigation, profile heavily modded sessions, and chunk or cache work only where measurements justify it.
4. **Release hardening** — Expand multiplayer/dedicated-server coverage, localization behavior, diagnostics, compatibility checks, and Workshop release documentation.

## Current Delivery

Milestone 1 has an implemented compile/package proof of concept. The remaining acceptance work is the manual in-game, missing-dependency, and dedicated-server validation described in the delivery handoff; runtime behavior must not be inferred from compilation alone.
