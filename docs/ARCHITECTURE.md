# Architecture

## Session lifecycle

`SEpediaSession` is the only game-discovered entry point. It waits for the Space Engineers definition manager, builds the immutable backend index, and starts spawned-planet tracking. On clients it also waits for Rich HUD Master before creating the frontend. Dedicated servers never create frontend objects.

Registrations are released in reverse ownership order. The window closes text input, restores the vanilla HUD, releases navigation and control subscriptions, then unregisters. The binding controller disables its Rich HUD settings page when Rich HUD offers no removal API. Close and reset paths are idempotent.

## Definition-build pipeline

1. `DefinitionIndexBuilder` enumerates the primary registry and the separate blueprint registry.
2. `DefinitionRelationships` discovers build-menu reachability, production-menu reachability, and block variant/pair relationships.
3. `DefinitionExtractors` converts each game definition into immutable, Rich-HUD-independent domain data.
4. `DefinitionIndex` sorts and freezes documents, then derives recipe lookup and reverse block usage in explicit passes.

`DefinitionBuildDiagnostics` isolates failures at game/mod-data boundaries. It labels failures by operation and definition, limits repeated samples, and emits aggregate suppressed counts. Extractors do not mutate relationship indexes as a side effect.

## Catalog flow

`CatalogIndex` is a read-only searchable projection. `DefinitionList` owns category normalization and unavailable-facet reconciliation, allowing at most one stabilization query. Search scoring, the 500-row display bound, source and block facets, and sorting all live in the catalog layer; labels and recipe summaries live in `CatalogText` so list and detail wording share one source.

## Detail and UI ownership

`DetailPageComposer` converts a definition or spawned planet into a presentation model containing a header, fields, links, and bounded sections. `DefinitionView` only renders that model and rebuilds rows when navigation changes.

Rich HUD layout containers own placement and final sizing. `CategoryBar` reflows existing buttons from their declared minimum widths. `PagedFacetSection` and `PagedDetailSection` share `PagerRow` behavior and reuse eight live slots per dynamic collection.

## Bounded-node rule

Runtime-sized Rich HUD data must not create an unbounded compound control tree. Dynamic collections are paged or otherwise bounded; layout complexity must be inspected because Rich HUD traverses retained nodes every frame. Representative heavily modded sessions are the performance acceptance boundary.

## Manual runtime boundary

Compilation and packaging do not prove Rich HUD behavior. Changes affecting UI, input, lifecycle, or runtime indexing must be checked in game for category wrapping, text entry and gameplay-input suppression, vanilla-HUD overlap/restoration, reset/unload behavior, dense relationship paging, heavily modded counts, and sustained FPS/allocation behavior.
