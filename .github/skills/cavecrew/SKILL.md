---
name: cavecrew
description: >
  Use when deciding how repository work should be delegated. In MorWalPizVideo,
  route repository tasks through MorWalPiz Delivery Handoff and its allowed
  MorWalPiz agents.
---

# Repository delegation

`MorWalPiz Delivery Handoff` is the entry point for repository work. Do not
invoke `cavecrew-investigator`, `cavecrew-builder`, or `cavecrew-reviewer`:
those subagents are not registered for this workspace.

The handoff agent selects the smallest valid route:

- Exact lookup: answer with read/search tools.
- Broad localization or unclear ownership: `MorWalPiz Repository Expert`.
- Bounded implementation: `MorWalPiz Senior Developer`.
- Architecture decision: `MorWalPiz Solution Architect`, only when needed.
- Explicit multi-stream implementation after readiness: `MorWalPiz Delivery Architect`.
- Review-only request: `MorWalPiz Code Reviewer`.

Use one agent for one bounded task. Parallelize only genuinely independent
workstreams or hard dependency boundaries. Keep the handoff's scope, approval,
and validation gates intact.

Skills are method and style overlays, not permission to bypass `MorWalPiz
Delivery Handoff`. Never invent an available agent or widen a bounded request
to justify delegation.
