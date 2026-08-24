# Architecture

## Runtime ownership

`SEpediaSession` is the only game-discovered entry point. It waits for definitions, builds the backend index, and tracks spawned planets. Clients then wait for Rich HUD Master before creating the UI; dedicated servers never create frontend objects.

Cleanup runs in reverse ownership order and is idempotent. The window closes text input and restores the vanilla HUD before releasing navigation, controls, settings, and entity subscriptions.

## Definition and catalog flow

`DefinitionIndexBuilder` enumerates the primary and blueprint registries. `DefinitionRelationships` derives G-menu reachability from both the All Blocks view and eligible block categories, then applies variant and block-pair relationships before `DefinitionExtractors` creates Rich-HUD-independent documents. `DefinitionIndex` then sorts documents and builds the recipe and component-usage lookups.

Failures at game or mod-data boundaries are isolated and summarized by `DefinitionBuildDiagnostics`. Extractors do not mutate relationship indexes.

`CatalogSchema` supplies ordered category and facet descriptors using stable keys. Built-in categories use the same registry contract intended for future adapters. `CatalogIndex` owns search scoring, the 500-result bound, sorting, and dynamic facet counts. `DefinitionList` normalizes category state, reconciles unavailable selections once, and renders the result. Shared labels and recipe search text live in `CatalogText`.

`CatalogEntryVisibility` is the single policy boundary for Enabled, Public, Survival, and Source. Catalog queries add category, search, block availability, grid size, and registered facets; detail relationships apply only the common policy.

## Detail and layout flow

`DetailPageComposer` orchestrates ordered providers over strongly typed definition and celestial data. Providers emit relationship candidates, and the central relationship builder applies common visibility before producing bounded detail rows. Filtered candidates contribute exact hidden counts while unresolved non-link metadata remains visible. `DefinitionView` renders that model and recomposes the current history entry when common visibility changes. Dynamic detail and facet collections reuse eight live slots plus a shared pager.

Nested `HudChain` containers own row-and-column placement. Dynamic controls remain bounded because Rich HUD traverses retained nodes every frame.

## Runtime acceptance

Compilation cannot prove Rich HUD behavior. Relevant changes require in-game checks for category population, search and filters, linked navigation, paging, window resizing, text input and gameplay suppression, vanilla-HUD restoration, Rich HUD reset/unavailability, unload, and heavily modded performance.
