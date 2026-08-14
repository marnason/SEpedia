# Definition Icon Compatibility

SEpedia renders definition icons through Rich HUD `TexturedBox` controls. Rich HUD materials refer to Space Engineers `TransparentMaterialDefinition` subtype IDs, not directly to definition `Icons[]` texture paths. See the maintained [Rich HUD custom-texture documentation](https://zachhembree.github.io/RichHudFramework.Client/articles/Custom-Textures.html).

SEpedia supports these cases automatically:

- base-game GUI icon paths, including when a modded definition reuses them;
- custom icon paths whose source mod registers a transparent material for the same texture.

Custom paths from a mod that does not register transparent materials remain text-only. SEpedia deliberately avoids submitting unresolved material IDs because Space Engineers displays those as white or missing-material squares.

## Mod-author integration

Add one transparent material for every layer in a definition's `Icons[]` array. The `.sbc` must be loaded from the same source mod as the texture so its relative path resolves in the correct mod context. Material subtype IDs must be globally unique, but SEpedia does not require a particular naming scheme.

```xml
<Definitions xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
             xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <TransparentMaterials>
    <TransparentMaterial>
      <Id>
        <TypeId>TransparentMaterialDefinition</TypeId>
        <SubtypeId>YourMod_UniqueIconMaterial</SubtypeId>
      </Id>
      <AlphaMistingEnable>false</AlphaMistingEnable>
      <CanBeAffectedByOtherLights>false</CanBeAffectedByOtherLights>
      <SoftParticleDistanceScale>0</SoftParticleDistanceScale>
      <Texture>Icons\YourIcon.dds</Texture>
      <Reflectivity>0</Reflectivity>
    </TransparentMaterial>
  </TransparentMaterials>
</Definitions>
```

The repository includes a generator for existing mods:

```text
python3 scripts/generate-mod-icon-materials.py MOD_ROOT MOD_NAMESPACE OUTPUT.sbc
```

Place the resulting file under the source mod's `Data` directory. The generator reads definition files and references existing icon assets; it neither copies nor modifies textures.

A separate compatibility mod has a different content context. It cannot use another mod's relative texture paths unless it also carries authorized copies of those assets, so source-mod integration is the preferred Workshop-compatible route.
