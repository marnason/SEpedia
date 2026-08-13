# Workspace Instructions

## Normative language

MUST and MUST NOT are requirements. SHOULD is the default unless repository evidence justifies an exception. MAY is optional. These rules apply to the whole workspace unless a closer `AGENTS.md` overrides them.

## Product and toolchain

- SEpedia is a scripted C# mod for Space Engineers, built with MDK².
- The project MUST remain compatible with its configured .NET Framework 4.8 target and C# 6 language version. Do not use newer language features merely because the installed SDK accepts them elsewhere.
- Use `SEpedia.sln` as the solution and `dotnet build SEpedia.sln` as the canonical build entry point. An MDK build may package and deploy the mod to the configured Windows Space Engineers Mods directory.
- Preserve the existing MDK² scaffold, analyzer, packager, reference packages, and package versions unless the active task requires changing them.
- Do not hardcode WSL paths, Windows user names, Steam library roots, local deployment directories, or other machine-specific values in portable source or guidance.

## Architecture

- Keep the backend deliberately small. It SHOULD contain only mod state, public Space Engineers game-event integration, and the data or operations needed by the frontend.
- All backend interaction with Space Engineers MUST use the public Space Engineers ModAPI. Torch, server plugins, Harmony or other runtime patching, reflection into game internals, private APIs, external services, and custom server infrastructure MUST NOT be introduced without explicit user approval.
- Rich HUD-specific types and lifecycle concerns SHOULD remain at the frontend boundary rather than leaking into backend state or domain interfaces.
- Client-only frontend code MUST NOT execute on dedicated servers. Session, event, and UI registrations MUST be released when their owning mod lifecycle ends.
- Prefer a useful vertical slice over speculative scaffolding. Do not create architectural layers, directories, abstractions, or extension points until concrete behavior justifies them.

## Frontend contract

- Every player-facing interface MUST use Rich HUD Master unless the user explicitly approves a specific exception. This includes interactive controls, informational views, settings, prompts, and feedback.
- Alternative HUD or UI frameworks, vanilla notification or chat UI, terminal-control UI, and bespoke billboard interfaces MUST NOT be used as frontend substitutes without explicit user approval.
- Treat Rich HUD Master as an optional runtime dependency while it is loading or unavailable. Frontend initialization MUST wait for a ready client, fail safely, and avoid breaking backend behavior.
- Consult the maintained [Rich HUD Framework client documentation](https://zachhembree.github.io/RichHudFramework.Client/index.html) and [official example mod](https://github.com/ZachHembree/TextEditorExample) before implementing its integration. Verify version-sensitive behavior from current primary sources instead of relying on memory or copied third-party snippets.

## Working discipline

- Inspect the repository and relevant primary documentation before asking questions or proposing architecture. Ask only when an answer materially changes behavior, scope, or an established contract; state and proceed with low-risk assumptions otherwise.
- Keep changes narrowly scoped. Do not refactor unrelated code or replace established project configuration as incidental cleanup.
- A production dependency MAY be added when it solves a concrete requirement. Explain why existing capabilities are insufficient and verify the dependency against its primary documentation.
- Preserve unrelated and pre-existing user changes. Never overwrite local configuration or generated assets merely to make a clean diff.
- Keep repository-specific skills under `.agents/skills/`. MUST NOT create or install SEpedia skills in user-level Codex skill storage.

## Verification

- Run the cheapest checks that meaningfully cover the change, beginning with `dotnet build SEpedia.sln` for code or project changes.
- Treat MDK analyzer and packager diagnostics as part of the build result; do not report success while relevant warnings or errors remain unexplained.
- Confirm packaged output when a change affects content layout, packaging, or deployment.
- Runtime, lifecycle, multiplayer, dedicated-server, or Rich HUD behavior MUST receive a proportionate in-game check when the environment permits it. Report any verification that remains manual or unavailable.

## Skills

Use the matching repo skill whenever its trigger applies:

- `$learn-from-my-mistakes` immediately when Codex discovers that its own preventable mistake resulted from missing project knowledge, incomplete skill guidance or references, a skill trigger or routing gap, or verification that missed the real runtime boundary.
