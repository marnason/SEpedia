# Custom Icon Research

SEpedia displays definition icons through Rich HUD `TexturedBox` controls. Definition `Icons[]` values are texture paths, while Rich HUD expects registered transparent-material subtype IDs. These identifiers are not interchangeable.

## Accepted runtime results

- Passing a raw definition texture path to Rich HUD produces a white or missing-material square.
- Load-time aliases packaged by SEpedia render base-game GUI icons correctly.
- The packaged aliases also work for modded definitions that reuse base-game GUI icon paths.
- Custom textures owned by enabled mods do not currently resolve automatically and therefore use the text-only header.
- Static material experiments demonstrated that Rich HUD can render several registered materials, but none produced a general mapping from an arbitrary mod-owned `Icons[]` path to a usable runtime material.

## Current production boundary

The production resolver accepts only complete icon stacks whose normalized paths match SEpedia's packaged base-game aliases. A stack with an unresolved layer, more than eight layers, or no icon data does not create an icon control. Raw paths remain in the definition model for further research.

## Next investigation

The Universal mod-supplied icons milestone remains open. Investigation should examine:

1. Open-source Rich HUD mods that render custom or cross-mod resources.
2. Rich HUD Client and Master material lookup and billboard submission.
3. Space Engineers' G-menu, inventory, and production-menu icon resolution.
4. The content-context and resource-name transformations applied before renderer submission.

Record compile, package, load, and render outcomes separately for vanilla paths, base-resource-reusing mods, and genuinely mod-owned textures. A failed route is evidence only about that route.
