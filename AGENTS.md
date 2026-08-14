# Workspace Instructions

## Normative language

MUST and MUST NOT are requirements. SHOULD is the default unless repository evidence justifies an exception. MAY is optional. These rules apply to the whole workspace unless a closer `AGENTS.md` overrides them.

## Product and toolchain

- SEpedia is a scripted C# mod for Space Engineers, built with MDK².
- First-party script source MUST pass `scripts/check-script-sandbox.sh`. It MUST NOT suppress MDK diagnostics or use `#pragma warning`; the in-game compiler independently rejects prohibited members and the pragma itself even when a local build succeeds. Renderer experiments requiring prohibited types need a separately compiled dependency or an exposed framework API rather than suppressed script source.
- The project MUST remain compatible with its configured .NET Framework 4.8 target and C# 6 language version. Do not use newer language features merely because the installed SDK accepts them elsewhere.
- Use `SEpedia.sln` as the solution. Routine automated compilation MUST run through `scripts/verify.sh`; a raw `dotnet build` may invoke the MDK packager and MUST NOT be used as an automated verification shortcut. Explicitly requested local package/deploy builds MUST run through `scripts/deploy.sh`, which pins MDK to non-interactive mode.
- Preserve the existing MDK² scaffold, analyzer, packager, reference packages, and package versions unless the active task requires changing them.
- Do not hardcode WSL paths, Windows user names, Steam library roots, local deployment directories, or other machine-specific values in portable source or guidance.

## Architecture

- Keep the backend deliberately small. It SHOULD contain only mod state, public Space Engineers game-event integration, and the data or operations needed by the frontend.
- All backend interaction with Space Engineers MUST use the public Space Engineers ModAPI. Torch, server plugins, Harmony or other runtime patching, reflection into game internals, private APIs, external services, and custom server infrastructure MUST NOT be introduced without explicit user approval.
- Rich HUD-specific types and lifecycle concerns SHOULD remain at the frontend boundary rather than leaking into backend state or domain interfaces.
- Recipe indexing MUST enumerate `MyDefinitionManager.GetBlueprintDefinitions()` explicitly; `GetAllDefinitions()` does not include blueprint definitions.
- Client-only frontend code MUST NOT execute on dedicated servers. Session, event, and UI registrations MUST be released when their owning mod lifecycle ends.
- Prefer a useful vertical slice over speculative scaffolding. Do not create architectural layers, directories, abstractions, or extension points until concrete behavior justifies them.

## Frontend contract

- Every player-facing interface MUST use Rich HUD Master unless the user explicitly approves a specific exception. This includes interactive controls, informational views, settings, prompts, and feedback.
- Alternative HUD or UI frameworks, vanilla notification or chat UI, terminal-control UI, and bespoke billboard interfaces MUST NOT be used as frontend substitutes without explicit user approval.
- Treat Rich HUD Master as an optional runtime dependency while it is loading or unavailable. Frontend initialization MUST wait for a ready client, fail safely, and avoid breaking backend behavior.
- Consult the maintained [Rich HUD Framework client documentation](https://zachhembree.github.io/RichHudFramework.Client/index.html) and [official example mod](https://github.com/ZachHembree/TextEditorExample) before implementing its integration. Verify version-sensitive behavior from current primary sources instead of relying on memory or copied third-party snippets.
- Definition `Icons[]` values are GUI texture paths, not Rich HUD transparent-material identifiers. A failed resource-resolution strategy proves only that strategy failed. Before declaring dynamic resources infeasible, inspect current Rich HUD client and master implementations, representative open-source Rich HUD mods using analogous resources, and the Space Engineers implementation that resolves the native UI resource. Record each tested route and its runtime outcome across base-game, base-resource-reusing mod, and custom-resource mod cases.
- The verified icon pipeline uses load-time aliases for base-game GUI paths. Raw custom mod paths and same-origin transparent-material matching have not produced a general solution; treat this as the boundary of the implemented pipeline, not proof that no alternative engine or framework resolution route exists.
- Runtime-sized data MUST NOT create an unbounded number of compound Rich HUD controls. Bound, page, or virtualize dynamic collections; inspect their per-frame layout and measurement work; and validate representative heavily modded counts in game before handoff.
- Use nested `HudChain` containers for row-and-column layout. A chain-managed axis MUST have one layout owner: a weighted or member-fitted chain child MUST NOT also copy that axis with `DimAlignment`, because parent alignment runs after chain layout and can overwrite the allocated size. Do not compensate for layout conflicts with manual offsets.

## Working discipline

- Inspect the repository and relevant primary documentation before asking questions or proposing architecture. Ask only when an answer materially changes behavior, scope, or an established contract; state and proceed with low-risk assumptions otherwise.
- Keep changes narrowly scoped. Do not refactor unrelated code or replace established project configuration as incidental cleanup.
- A production dependency MAY be added when it solves a concrete requirement. Explain why existing capabilities are insufficient and verify the dependency against its primary documentation.
- Preserve unrelated and pre-existing user changes. Never overwrite local configuration or generated assets merely to make a clean diff.
- Keep repository-specific skills under `.agents/skills/`. MUST NOT create or install SEpedia skills in user-level Codex skill storage.

## Verification

- Run the cheapest checks that meaningfully cover the change, beginning with `scripts/verify.sh` for code or project changes. The script deliberately pins a non-packaging configuration and non-interactive MDK behavior; do not duplicate or improvise its command inline.
- Before running or handing off an MDK² build, confirm that generated output and machine-local configuration are ignored and untracked while preserving required local copies. This includes `mdk.local.ini` and `<project>.mdk.local.ini`. Use `-p:MdkBuildConfiguration=CompileOnly` for compile-only iteration and `-p:MdkInteractive=no` for headless packaging. If a GUI action is unavoidable, tell the user exactly what will open and what they need to do before invoking it.
- Treat MDK analyzer and packager diagnostics as part of the build result; do not report success while relevant warnings or errors remain unexplained.
- Confirm packaged output when a change affects content layout, packaging, or deployment.
- For runtime resource or renderer experiments, track compile, package, load, and render outcomes per strategy. Validate base-game resources, third-party definitions reusing base resources, and third-party custom resources separately; success in one origin class does not validate another. Remove or debug-gate experimental controls, labels, and assets before normal handoff.
- Before a runtime experiment that can corrupt live definition or session state, disable autosave or take a restorable copy of the disposable save. When any rejection criterion fires, stop the game without saving before further UI inspection; pausing or backgrounding the game does not guarantee that autosave is inactive. Confirm save timestamps afterward and restore the disposable copy before another load.
- Runtime, lifecycle, multiplayer, dedicated-server, or Rich HUD behavior MUST receive a proportionate in-game check when the environment permits it. Report any verification that remains manual or unavailable.
- When mirroring a vanilla menu, eligibility MUST follow that menu's actual runtime collection and visibility semantics rather than substituting generic `MyDefinitionBase` flags. Confirm that at least one vanilla entry reaches every new browse category before handoff.
- For Rich HUD interaction or layering changes, inspect the matching Rich HUD Master implementation as well as the client API, and explicitly test text entry, gameplay-input suppression, vanilla-HUD overlap, and state restoration in game before treating the behavior as verified.
- For nested Rich HUD layout changes, resize the window through its minimum, default, and representative larger sizes in game and check every row and column for overlap, clipping, and unused space before treating the layout as verified.
- Before using player-targeted APIs, verify the required identifier and default-argument semantics from primary documentation; do not assume `0` means the local player. Temporary player state changes MUST preserve the previous value, target the local identity explicitly, and restore only state the mod still owns on every close, reset, failure, and unload path.

## Skills

Use the matching repo skill whenever its trigger applies:

- `$learn-from-my-mistakes` immediately when Codex discovers that its own preventable mistake resulted from missing project knowledge, incomplete skill guidance or references, a skill trigger or routing gap, or verification that missed the real runtime boundary.
