---
name: learn-from-my-mistakes
description: Convert a newly discovered, preventable Codex mistake into durable workspace guidance. Use immediately when Codex realizes, or the user reports, that Codex's earlier implementation, diagnosis, advice, or process introduced or failed to prevent a defect because a project fact was not discovered, primary documentation was not consulted, an existing skill lacked a requirement or reference, a skill description or workspace routing rule failed to trigger, or verification missed the real runtime boundary. Do not use for ordinary pre-existing bugs, expected iteration failures caught before handoff, or one-off slips with no reusable lesson.
---

# Learn From My Mistakes

Turn a preventable mistake into the smallest reusable guardrail that would have changed the original decision. Improve the workspace's future behavior rather than keeping an incident diary.

## Establish the lesson

1. Gather enough repository, runtime, test, or user evidence to explain the failure causally.
2. Identify the specific fact or check that would have prevented it. Do not encode a lesson while the cause remains speculative.
3. Distinguish the immediate fix from the learning change. Follow the active request's authority for product or code changes; this skill authorizes only durable workspace guidance and its validation.
4. Consult current primary documentation when framework, dependency, protocol, security, or provider behavior contributed to the mistake.

Classify the prevention gap:

- Missing project knowledge or an undiscovered local convention.
- Missing or ambiguous instructions in a relevant skill body or reference.
- A skill description that did not encompass the task and therefore failed to trigger.
- A missing workspace routing rule for a skill that should have been mandatory.
- Verification that exercised an internal layer but missed the user-visible runtime boundary.
- Version-sensitive knowledge that was assumed instead of checked against primary sources.

## Put knowledge in the smallest durable home

- Add a broadly applicable workspace invariant or mandatory skill route to the closest `AGENTS.md`.
- Expand a skill's frontmatter `description` when its trigger vocabulary or scope was too narrow.
- Add a concise repeatable rule or workflow gate to the owning skill body.
- Add a skill reference when the necessary domain detail is too large, conditional, or version-specific for the body.
- Add or strengthen a deterministic verification step when a test, script, or runtime smoke check can catch the class of failure more reliably than prose.
- Create a new skill only when no existing skill clearly owns the recurring workflow.

Prefer one authoritative location and link to it where routing is needed. Do not copy the same lesson into several skills.

## Apply the learning

1. Read every target `AGENTS.md` and `SKILL.md` completely before editing it, including directly required references.
2. Use `$skill-creator` for skill package changes. Preserve valid frontmatter, concise imperative wording, progressive disclosure, and matching `agents/openai.yaml` metadata.
3. State the generalized rule without names, blame, timestamps, or incident-specific history. Include a primary-source link when future agents may need to recheck evolving behavior.
4. If the correct instruction already existed but was missed, do not duplicate it. Improve routing, triggering, mandatory wording, or verification so it becomes actionable.
5. Preserve unrelated and pre-existing user changes.

## Validate the guardrail

- Run the skill validator for every created or changed skill.
- Check metadata still describes the skill accurately and permits the intended implicit invocation.
- Run formatting, validation, or targeted tests for any executable guardrail added.
- Review the diff for contradictions, unnecessary duplication, overbroad triggers, and incident-specific wording.
- Forward-test a substantial or ambiguous skill change when an isolated evaluation is available and low-risk.

Report the cause, the durable lesson, where it was recorded, and what validated it. Describe reduced risk rather than promising that recurrence is impossible.

## Keep the loop disciplined

- Do not record secrets, personal data, transient environment values, or speculative conclusions.
- Do not turn every failed test or corrected typo into permanent policy. The lesson must generalize to a realistic future task.
- Do not weaken security, architecture, accessibility, or business invariants merely to remove a symptom.
- Do not change product scope or architecture without the user's authority, even when a broader change could prevent recurrence.
- Do not modify this skill after every incident unless its learning workflow or trigger itself proved insufficient.
