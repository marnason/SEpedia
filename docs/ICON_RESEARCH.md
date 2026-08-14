# Custom Icon Research

SEpedia displays definition icons through Rich HUD `TexturedBox` controls. Definition
`Icons[]` values are GUI texture paths, while Rich HUD expects registered
transparent-material subtype IDs. A path and a material ID can contain similar text,
but they are different resources and are resolved at different stages.

## Established runtime results

- Passing a raw definition texture path to Rich HUD produces a white or
  missing-material square.
- Load-time aliases packaged by SEpedia render base-game GUI icons correctly.
- Those aliases also render mod definitions that reuse base-game GUI icon paths.
- Raw custom mod paths and same-origin transparent-material matching have not
  produced a general custom-icon solution.
- Calling the normal definition-loading pass again during a live session corrupts
  definition state and is rejected as an icon-registration route.
- A definition icon stack with an unresolved layer, more than eight layers, or no
  icon data remains text-only.

These results describe the implemented resolver boundary. They do not establish
that the engine has no other public registration route.

## Rich HUD mod precedents

### Applied Logistics

[Applied Logistics](https://steamcommunity.com/sharedfiles/filedetails/?id=3737187200)
uses a deterministic material name:

```text
Icon_{definition type without MyObjectBuilder_}_{subtype}
```

For example, a component definition can be addressed as
`Icon_Component_SteelPlate`. Its shipped transparent-material catalogue contains
1,688 unique aliases. The inspected release points those aliases at Colorful Icons
assets through cross-mod paths such as `..\801185519\...`.

Applied Logistics also has an out-of-game patcher. It scans the user's installed
Workshop and local mods and generates transparent-material definitions whose
textures use `..\{published mod ID}\{icon path}`. This is why it can regenerate a
catalogue for the particular installed mod set: the patcher performs discovery and
writes load-time definitions before Space Engineers starts. That approach is useful
evidence for naming and cross-mod texture resolution, but it is outside SEpedia's
in-game-only constraint.

### Build Cost Calculator

[Build Cost Calculator](https://steamcommunity.com/sharedfiles/filedetails/?id=3611550636)
extracts the filename without extension from `blockDef.Icons[0]` and looks for a
transparent material with that subtype. If no material matches, it falls back to a
grid-size icon.

Its static cross-mod catalogue contains 1,176 entries (1,169 unique
case-insensitive subtype IDs). In the inspected release, 149 entries explicitly
refer to Industrial Overhaul (`..\2344068716\Icons\...`); the remainder use
base-game or same-mod resources. This works for the finite set anticipated by the
catalogue author, not arbitrary enabled mods.

Both mods therefore confirm that Rich HUD can display another mod's texture after a
transparent material has been registered. Neither obtains a new renderer material
merely by passing an arbitrary `Icons[]` path to Rich HUD.

## Official API and content findings

- The official [transparent-material definition reference](https://spaceengineers.wiki.gg/wiki/Modding/Reference/SBC/TransparentMaterial_Definition)
  describes the SBC definition that binds a material subtype to a texture. The
  normal definition-loading pass converts those definitions into renderer
  materials.
- The official [cross-mod asset-path guide](https://spaceengineers.wiki.gg/wiki/Modding/Tutorials/Modifying_Mods_by_Other_Creators)
  documents `..\workshop-id\...` paths. This permits one loaded definition to name
  another installed mod's asset; it does not itself create a transparent material.
- The official [script whitelist](https://spaceengineers.wiki.gg/wiki/Modding/Reference/Programming/Whitelist)
  is the runtime boundary for scripted mods. The public definition-manager route
  compiles, while the direct renderer and native GUI drawing routes do not.
- The broader [materials reference](https://spaceengineers.wiki.gg/wiki/Modding/Reference/Materials#Transparent_Materials)
  confirms that transparent materials are the resource type used for these
  billboard textures.

`MyDefinitionManager.GetAllDefinitions()` is not a complete blueprint source, so
the experiment enumerates `GetBlueprintDefinitions()` separately.

## Routes investigated

| Route | Compile | Package | Load/render result |
| --- | --- | --- | --- |
| Packaged aliases for base GUI paths | Pass | Pass | Vanilla and mod definitions reusing vanilla paths render. |
| Raw `Icons[]` path supplied to Rich HUD | Pass | Pass | White/missing material for custom paths. |
| Static same-origin transparent-material matching | Pass | Pass | Useful for known catalogues; no general arbitrary-path mapping. |
| Native GUI sprite drawing | Rejected | Not attempted | The game can draw GUI paths natively, but the required GUI drawing member is not available to a scripted mod. |
| Direct renderer material registration | Rejected | Not attempted | `VRageRender`/`MyTransparentMaterials` registration members are outside the scripted-mod whitelist. |
| Rich HUD runtime font/atlas construction | Pass | Not adopted | Atlas pages still resolve through Rich HUD materials, so this does not bypass transparent-material registration. |
| Runtime definitions added only to the merged definition set | Pass | Not adopted | A later merge clears that set, and adding a definition alone does not invoke renderer registration. |
| Persistent loading-set aliases followed by `LoadData(session.Mods)` | Pass | Pass | Rejected in game: duplicate definitions, a post-process exception, missing definitions, and loss of production-menu reachability. |
| External installed-mod catalogue generator | Demonstrated by Applied Logistics | N/A | Viable prelaunch technique, explicitly out of scope for SEpedia. |

## Rejected runtime reload probe

The temporary `RuntimeIconMaterialExperiment` tested the remaining public in-game
route. It was restricted to a non-dedicated, offline session whose world name was
exactly `SEpedia Icon Probe`.

Before SEpedia's first index build, the probe:

1. Captured the IDs and counts returned by `GetAllDefinitions()` and
   `GetBlueprintDefinitions()`.
2. Enumerated those two collections explicitly and gathered each mod-owned icon
   layer from otherwise eligible one-to-eight-layer stacks.
3. Deduplicated resolved texture paths, assigned collision-resistant
   `SEpedia_RuntimeIcon_{hash}` IDs, initialized transparent-material definitions
   with the owning mod context, and added them to `GetLoadingSet()`.
4. Called `MyDefinitionManager.LoadData(session.Mods)` once, allowing the normal
   transparent-material registration pass to run.
5. Recorded alias/layer/definition counts, before/after definition counts, reload
   duration, registration count, and any exception in the Space Engineers log. It
   also compares the complete before/after ID sets after excluding the expected
   injected aliases.
6. Would enable its context-and-path mapping only when all aliases appeared and the
   structural comparison passed. It did not reach that point.

The experiment compiled and packaged with zero MDK diagnostics. In the copied,
heavily modded test save, the live reload then produced the following outcome:

- `MyDefinitionManager.LoadData(session.Mods)` emitted 593 duplicate-definition and
  8,069 duplicate-entry diagnostics.
- Definition post-processing threw `NullReferenceException` from
  `MyBlockVariantGroup.ResolveBlocks()`, through `InitBlockGroups()` and
  `LoadPostProcess()`.
- The same running game provided a pre-probe baseline of 15,732 indexed definitions,
  4,357 recipes, and 596 production-menu-reachable recipes. After the failed reload,
  SEpedia found 14,766 definitions, 4,358 recipes, and **zero**
  production-menu-reachable recipes.
- The Escape screen rendered its background and branding but none of its action
  buttons, confirming visible GUI damage after the definition reload.

The route therefore failed before renderer validation. Per the rejection criteria,
there was no reason to continue into G-menu, inventory, production, placement,
save/reload, or Rich HUD interaction checks: the definition mismatch, duplicates,
and exception independently reject the hypothesis. The temporary class and all
integration hooks were removed after this result.

The intended rendering fixtures were:

1. A vanilla definition icon.
2. A mod definition that reuses a vanilla GUI icon.
3. A genuinely custom icon. The known fixture is Industrial Overhaul's
   `BlueprintDefinition/FSSolarCell`, display name **Full-Spectrum Solar Cell**,
   whose icon is `Icons\FSSolarCell.dds`.

Because integrity failed first, no claim is made about whether the generated alias
could have rendered any of these textures. Production behavior remains limited to
the existing base-game and base-resource-reusing aliases.
