# Workspace Instructions

MUST and MUST NOT are requirements. SHOULD is the default unless repository evidence justifies an exception. These rules apply to the workspace unless a closer `AGENTS.md` overrides them.

## Toolchain

- SEpedia is a scripted C# Space Engineers mod built with MDK². Preserve the scaffold, analyzers, packager, references, and package versions unless the task requires changing them.
- Space Engineers Workshop packages distribute scripted mod source, which the game compiles at runtime; compiled assemblies MUST NOT be released. Treat off-game compilation as an additional validation step that requires current Keen reference assemblies, not as a packaging requirement. Any CI acquisition of those proprietary references MUST be validated on a clean hosted runner and tolerate documented transient provider failures without caching or releasing the assemblies.
- First-party code MUST remain compatible with .NET Framework 4.8 and C# 6 and MUST pass `scripts/check-script-sandbox.sh`. Do not suppress MDK diagnostics or use `#pragma warning`; the in-game compiler rejects prohibited members independently of local builds.
- Use `SEpedia.sln`. Run routine verification through `scripts/verify.sh`, then finish every implementation change by running the single deployment entry point, `scripts/deploy.sh`. Do not automate a bare `dotnet build` because MDK may become interactive.
- Portable code and guidance MUST NOT contain machine-specific paths, users, Steam roots, or deployment destinations.
- GitHub immutable releases are enabled. Nightly automation MUST publish with a unique tag and may prune older nightlies only after the replacement succeeds; do not delete and recreate a fixed tag. Recheck the repository setting and [GitHub's release API documentation](https://docs.github.com/en/rest/releases/releases) when changing this flow.
- Stable Workshop publishing MUST keep Steam Guard in the official mobile app and use an operator-attended SteamCMD login approved from the phone. Do not request or store the authenticator shared secret, transfer or remove the authenticator for automation, or persist a reusable Steam session in CI. Keep Steam credentials confined to the upload job, validate mobile approval with a login-only hosted-runner test before enabling publication, and fail before modifying the Workshop item or GitHub Release if authentication times out. Recheck [Valve's SteamCMD Workshop documentation](https://partner.steamgames.com/doc/features/workshop/implementation#SteamCmd) and [Steam Support's mobile approval guidance](https://help.steampowered.com/en/faqs/view/6891-E071-C9D9-0134) when changing this flow.

## Architecture and frontend

- Keep backend state independent of Rich HUD types. All game integration MUST use the public Space Engineers ModAPI; do not add plugins, patching, reflection into internals, external services, or custom infrastructure without approval.
- Recipe indexing MUST enumerate `MyDefinitionManager.GetBlueprintDefinitions()` explicitly because `GetAllDefinitions()` omits blueprints.
- Generated assembler block-component bundles are runtime composite blueprints whose IDs mirror cube-block definition IDs; they are not `MyBlockBlueprintDefinition` instances. Exclude them from recipe browsing and relationships by matching loaded cube-block IDs rather than guessing from the runtime type name.
- Player-facing UI MUST use Rich HUD Master. Treat it as an optional client dependency, never initialize it on dedicated servers, and release registrations on reset and unload.
- Check the maintained [Rich HUD client documentation](https://zachhembree.github.io/RichHudFramework.Client/index.html), [official example](https://github.com/ZachHembree/TextEditorExample), and matching vendored implementation before changing version-sensitive integration.
- Definition-supplied icons MUST NOT be collected, resolved, packaged, or rendered. Headers remain text-only unless the user approves a new direction.
- Bound or page runtime-sized Rich HUD collections. A `HudChain`-managed axis MUST have one layout owner; do not overwrite chain allocation with `DimAlignment` or manual compensation.
- After rebuilding a Rich HUD scroll collection, apply a requested `Start`, `End`, or pixel offset only from a post-layout hook on a later frame, such as `HandleInput`. The retained-mode layout recalculates scrollbar bounds after the rebuild, so same-stack offsets can clamp against stale bounds; recheck the maintained [Rich HUD client documentation](https://zachhembree.github.io/RichHudFramework.Client/index.html) and matching vendored `ScrollBox` implementation when changing this flow.
- `RichHudFramework/` is vendored source. Do not reformat, reorganize, or otherwise mix vendor changes into normal first-party work.

## Code organization

- Use descriptive `#region` blocks in first-party classes with multiple functional areas. Regions SHOULD identify behavior such as lifecycle, querying, layout, persistence, or event handling; do not wrap tiny classes or individual methods without navigational value.
- Prefer a concrete vertical slice over speculative layers or extension points. Keep repository-specific skills under `.agents/skills/`, never in user-level Codex storage.

## Verification

- Begin code or project verification with `scripts/verify.sh` and treat MDK warnings and errors as build failures. Confirm generated output and machine-local `mdk.local.ini` variants remain ignored and untracked.
- Use `scripts/deploy.sh` after every implementation change. Do not inspect deployed contents unless the user explicitly requests it or deployment reports a failure. Report runtime checks that cannot be performed.
- When testing the locally deployed mod, keep Rich HUD Master explicitly enabled in the save; a local mod does not inherit the Workshop item's dependency list. Before reporting UI runtime results, confirm the log records `[SEpedia] Rich HUD interface initialized` rather than the unavailable-dependency warning.
- UI, lifecycle, multiplayer, dedicated-server, or runtime-indexing changes require proportionate in-game verification. For Rich HUD changes, test text entry, gameplay-input suppression, vanilla-HUD overlap/restoration, reset, unload, and minimum/default/large layouts.
- Before an experiment that can mutate live definition or session state, disable autosave or use a disposable save. Stop without saving on any integrity failure and restore the save before retrying.
- When mirroring a vanilla menu, use its actual runtime eligibility semantics and confirm every browse category receives a vanilla entry.
- Player-targeted APIs require verified identifier and default-argument semantics. Temporary state changes must target the local identity, preserve the prior value, and restore only state still owned by the mod.

## Skills

- Use `$learn-from-my-mistakes` immediately when a preventable Codex mistake reveals missing project knowledge, routing, or runtime verification.
