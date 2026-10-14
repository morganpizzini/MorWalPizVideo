# cavecrew

Decision guide for repository task routing through `MorWalPiz Delivery Handoff`.

## Available routes

| Request | Route |
|---|---|
| Exact lookup | Read/search directly |
| Broad localization or unclear ownership | `MorWalPiz Repository Expert` |
| Bounded implementation | `MorWalPiz Senior Developer` |
| Architecture decision | `MorWalPiz Solution Architect` |
| Explicit multi-stream implementation after readiness | `MorWalPiz Delivery Architect` |
| Review-only request | `MorWalPiz Code Reviewer` |

`cavecrew-investigator`, `cavecrew-builder`, and `cavecrew-reviewer` are not
registered workspace agents. Do not invoke them or rely on their output
contracts. Skills may guide method or response style, but cannot bypass the
handoff's scope, approval, or validation gates.

See [`SKILL.md`](./SKILL.md) for the full routing policy.
