# cavecrew

Decision guide for selecting a repository agent directly.

## Available routes

| Request | Route |
|---|---|
| Exact lookup | Read/search directly |
| Broad localization or unclear ownership | `MorWalPiz Repository Expert` |
| Bounded implementation | `MorWalPiz Senior Developer` |
| Architecture decision | `MorWalPiz Solution Architect` |
| Explicit multi-stream implementation after readiness | `MorWalPiz Delivery Architect` |
| Review-only request | `MorWalPiz Code Reviewer` |

Users may select an agent directly from the agent picker; no additional
routing or approval agent is required. `cavecrew-investigator`,
`cavecrew-builder`, and `cavecrew-reviewer` are not registered workspace
agents. Do not invoke them or rely on their output contracts. Skills may guide
method or response style, but cannot widen the user's scope or bypass approval
and validation requirements.

See [`SKILL.md`](./SKILL.md) for the full routing policy.
