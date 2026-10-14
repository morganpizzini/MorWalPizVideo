# Copilot Instructions

- Follow the conventions and architecture of the project being changed. Keep changes focused, use clear names, and avoid unnecessary abstractions or comments.
- Before changing an in-scope application, read the relevant architecture guide and owning feature document. The guide distinguishes current behavior from approved targets; verify behavior in source, project manifests, workflows, and tests. These are authoritative if documentation conflicts with them.
- When a change affects architectural boundaries, contracts, persistence, authentication, deployment, or operational policy, update the relevant architecture documentation and ADR as appropriate.
- Never add credentials, connection strings, API keys, or deployed secret values to source or documentation.

## Project References

- [Architecture guide](../docs/architecture/README.md)
- [Development guide](../docs/architecture/development.md)
- [Test execution runbook](../docs/architecture/testing.md)

Consult the relevant backend, frontend, Windows application, security, and feature documents linked from the architecture guide for implementation-specific rules. In particular, follow its cache-tag and HTTP-client guidance rather than duplicating those rules here.
