# Third-party provenance

## Rich HUD Framework client snapshot

SEpedia vendors a source snapshot under `RichHudFramework/` so the mod can compile against the Rich HUD client API without a runtime assembly dependency.

- Upstream projects: [RichHudFramework.Client](https://github.com/ZachHembree/RichHudFramework.Client) and [RichHudFramework.Shared](https://github.com/ZachHembree/RichHudFramework.Shared)
- License: MIT; preserved in `RichHudFramework/LICENSE` and `RichHudFramework/Shared/LICENSE`
- Shared upstream commit recorded by the vendored subrepo metadata: `92acc3b644219e8308beea0750bdd8e2153dfe73`
- Vendored repository tree IDs:
  - `RichHudFramework/`: `cc63ff832c8f8caa55a9c976b2f4ea81a80b1c29`
  - `RichHudFramework/Client/`: `ed5546f2e9fdf877483804e5562ae5acffa4a697`
  - `RichHudFramework/Shared/`: `42b95fd100c93afef2dd8c2e02642bacbb02d470`

The snapshot is vendor code, not first-party SEpedia code. Do not reformat or reorganize it during ordinary feature work.

### Refresh procedure

1. Review the current Rich HUD client documentation, official example mod, upstream changes, and licenses.
2. Refresh the snapshot from the upstream repositories while preserving its provenance metadata; do not mix a vendor upgrade with unrelated feature work.
3. Record the new upstream commit and repository tree IDs here.
4. Run `scripts/verify.sh` and `scripts/deploy.sh`.
5. In game, verify Rich HUD readiness/reset, text entry, gameplay-input suppression, vanilla-HUD overlap/restoration, settings-page behavior, unload, and all accepted SEpedia layouts.
