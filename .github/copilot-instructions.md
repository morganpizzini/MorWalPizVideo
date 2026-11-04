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

## .NET Build/Test Concurrency

- Do not overlap .NET build or test commands that write the same project's `bin/` or `obj/` outputs. After a timeout, verify the previous `dotnet`, `testhost`, or `vstest` process has exited before retrying.
- For focused backend validation, build once and run tests without an implicit rebuild:

	```bash
	dotnet build <project>.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false
	dotnet test <project>.csproj --configuration Release --no-restore --no-build --filter "FullyQualifiedName~<TestName>"
	```

- When validating multiple backend test projects, run the build commands serially and keep every later discovery/test command on `--no-build`.

## Repository work:
- The user may select a custom agent directly from the agent picker; that agent should work on the request without requiring a separate routing agent.
- Skills define methods or style and do not override the user's requested scope, approval, or validation requirements.
- Caveman Cloud operations are separate from repository work.
- When architecture planning is needed before implementation, save the user-editable summary as a Markdown file in `docs/feature-handoffs/` (or a feature-equivalent folder). After implementation, update it with actual changes, validation, and residual risks.
