---
name: cavecrew
description: >
  Use when choosing a suitable MorWalPizVideo repository agent. The user may
  select an agent directly; do not require an additional routing agent.
---

# Repository agent selection

The user may select a repository agent directly from the agent picker. Do not
require another agent to route or approve a task before the selected agent
starts work. Do not invoke `cavecrew-investigator`, `cavecrew-builder`, or
`cavecrew-reviewer`: those subagents are not registered for this workspace.

When the user asks which agent to select, use this guide:

- Exact lookup: answer with read/search tools.
- Broad localization or unclear ownership: `MorWalPiz Repository Expert`.
- Bounded implementation: `MorWalPiz Senior Developer`.
- Architecture decision: `MorWalPiz Solution Architect`, only when needed.
- Explicit multi-stream implementation: `MorWalPiz Delivery Architect`.
- Review-only request: `MorWalPiz Code Reviewer`.

Use one agent for one bounded task. Parallelize only genuinely independent
workstreams or hard dependency boundaries. Keep the user's scope, approval,
and validation requirements intact.

Skills are method and style overlays, not permission to widen a bounded
request. Never invent an available agent to justify delegation.
