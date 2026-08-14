# Architecture

## Runtime ownership

`SEpediaSession` is the only game-discovered entry point. It waits for definitions, builds the backend index, and tracks spawned planets. Clients then wait for Rich HUD Master before creating the UI; dedicated servers never create frontend objects.

Cleanup runs in reverse ownership order and is idempotent. The window closes text input and restores the vanilla HUD before releasing navigation, controls, settings, and entity subscriptions.

## Definition and catalog flow

`DefinitionIndexBuilder` enumerates the primary and blueprint registries. `DefinitionRelationships` derives build-menu, production-menu, variant, and block-pair relationships before `DefinitionExtractors` creates Rich-HUD-independent documents. `DefinitionIndex` then sorts documents and builds the recipe and component-usage lookups.

Failures at game or mod-data boundaries are isolated and summarized by `DefinitionBuildDiagnostics`. Extractors do not mutate relationship indexes.

`CatalogIndex` owns search scoring, the 500-result bound, sorting, and facet counts. `DefinitionList` normalizes category state, reconciles unavailable selections once, and renders the result. Shared labels and recipe search text live in `CatalogText`.

## Detail and layout flow

`DetailPageComposer` produces headings, fields, and bounded sections. `DefinitionView` renders that model and rebuilds rows only when navigation changes. Dynamic detail and facet collections reuse eight live slots plus a shared pager.

Nested `HudChain` containers own row-and-column placement. Dynamic controls remain bounded because Rich HUD traverses retained nodes every frame.

## Runtime acceptance

Compilation cannot prove Rich HUD behavior. Relevant changes require in-game checks for category population, search and filters, linked navigation, paging, window resizing, text input and gameplay suppression, vanilla-HUD restoration, Rich HUD reset/unavailability, unload, and heavily modded performance.
