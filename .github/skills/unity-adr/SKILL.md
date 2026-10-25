---
name: unity-adr
description: "Compare and record a Unity architecture decision with context, alternatives, consequences, and validation criteria. Use for DI, async/reactive, package API, networking, presentation, pooling, or content lifetime choices; create an ADR file only when requested."
---

# Unity architecture decisions

## Workflow

1. Identify one decision that changes implementation or compatibility, not a catalogue of patterns.
   Inspect the relevant code, versions, user-added dependencies, constraints, and available evidence.
2. Compare the simplest viable baseline with one or two real alternatives. Include ownership,
   performance evidence, testability, public API exposure, support burden, and platform constraints.
3. Recommend a choice and state what evidence would change it. An unresolved network stack or
   unmeasured optimization should not become an accepted decision by assumption.
4. Present status, context, decision, alternatives, consequences, and verification/rollback criteria
   in chat. Obtain approval before calling it accepted or changing code.
5. Only if explicitly requested, write to the repository's existing ADR convention; otherwise
   keep the decision in the conversation. Do not invent new documentation folders for a routine edit.
6. Verify that the implementation checklist and consumer compatibility obligations follow the
   accepted decision. Supersede prior records deliberately instead of silently rewriting history.

## DO NOT

- Create Markdown files automatically, confuse a plan with an accepted decision, or record unsupported
  claims such as guaranteed zero allocation/hitch-free loading.
- Prescribe DI, netcode, rendering, or package dependencies without context and approval.
- Mix per-task logs/dates into always-on AGENTS.md or claim the decision was tested without evidence.

## Sources

- [Architecture](../unity-clean-architecture/SKILL.md)
- [Unity-Skills advisory ADR module](https://github.com/Besty0728/Unity-Skills/tree/main/SkillsForUnity/unity-skills~/skills/adr)
