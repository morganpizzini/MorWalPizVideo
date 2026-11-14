# Shooter Workshop Container Test Dependency Handoff

### Outcome

Make the Shooter Workshop container build deterministic by declaring the test tooling that its source-owned test file imports. Assume the current build contract remains `tsc -b && vite build`; do not hide tests from TypeScript compilation merely to bypass the missing dependency.

### Current Evidence

- `frontend/shooter-workshop.client/package.json` runs `tsc -b && vite build`.
- `frontend/shooter-workshop.client/tsconfig.json` references `tsconfig.app.json`.
- `frontend/shooter-workshop.client/tsconfig.app.json` includes the entire `src` directory, so `src/components/feedback/FeedbackProvider.test.tsx` is part of TypeScript compilation.
- That test imports `@testing-library/react`, but the Shooter Workshop manifest does not declare it.
- `vitest.config.ts` uses the `jsdom` environment, but the Shooter Workshop manifest does not declare `jsdom`.
- The Dockerfile intentionally copies only the root manifest plus the shared-package and Shooter Workshop manifests before `yarn install`. Dependencies hoisted because of unrelated workspaces in a full local install are therefore unavailable in this reduced Docker workspace.
- Other repository React test owners, including BackOffice, FreezerAnalysis, Shooting ITA, and Shoot Recorder, declare `@testing-library/react` directly.

### Proposed Design

Keep production compilation behavior unchanged and make the owning workspace self-contained:

1. Add `@testing-library/react` to the Shooter Workshop development dependencies, aligned with the existing React 19-compatible repository version.
2. Add `jsdom` to the Shooter Workshop development dependencies because Vitest names that environment.
3. Regenerate the committed Yarn Classic lockfile through the workspace package manager.

Excluding `*.test.tsx` from `tsconfig.app.json` is not recommended for this incident. It would make the image build pass without repairing the undeclared test dependency and would reduce build-time type checking of tests.

### Impacted Projects

- `frontend/shooter-workshop.client`: owns the imported test dependency and Vitest environment.
- Frontend Yarn workspace root: owns the shared lockfile.
- Shooter Workshop container image: consumes the reduced set of workspace manifests during install.
- No backend, database, API, deployment-topology, or shared-package behavior changes.

### Impacted Files

#### Modify

- `frontend/shooter-workshop.client/package.json`: declare Testing Library and jsdom as development dependencies.
- `frontend/yarn.lock`: record the package-manager-resolved dependency graph.

#### Add

None identified.

#### Generated/Migration

None. The lockfile is package-manager maintained but remains a modified tracked file, not a runtime migration.

#### Documentation

- `docs/feature-handoffs/shooter-workshop-container-test-dependency-handoff.md`: root-cause and implementation handoff.

#### Verify Only

- `frontend/shooter-workshop.client/Dockerfile`: confirm the existing reduced-workspace install succeeds without changing image stages.
- `frontend/shooter-workshop.client/tsconfig.app.json`: confirm tests remain included.
- `frontend/shooter-workshop.client/vitest.config.ts`: confirm jsdom remains the selected environment.
- `.github/workflows/deploy-shooter-workshop-client.yml`: use its existing test, build, and image-build gates.
- `.github/workflows/ci.yml`: use its existing workspace build and container matrix gates.

### Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | Not applicable. |
| Frontend/UI | Dependency metadata only; no rendered behavior or Impeccable workflow is needed. |
| Database/Data | Not applicable. |
| Contracts/Models | Not applicable. |
| Authentication/Authorization | Not applicable. |
| Validation | Restores deterministic test type-checking and Vitest environment ownership. |
| Cache | Not applicable. |
| Configuration/Secrets | No configuration or secret change. |
| Logging/Telemetry | Not applicable. |
| Background Jobs | Not applicable. |
| Deployment/Operations | Container dependency installation becomes independent of unrelated workspace manifests. |
| Backward Compatibility | Additive development dependencies; runtime bundle behavior remains unchanged. |

### Reuse Opportunities

- Reuse the repository versions already resolved for `@testing-library/react` and `jsdom`.
- Reuse the existing Yarn Classic workspace install, test, build, and Buildx workflow gates.

### Risks

- **Low — lockfile drift:** manually editing the lockfile could produce an invalid graph. Mitigation: update it only through Yarn Classic with the committed manifest. Signal: frozen-lockfile install succeeds.
- **Low — hidden dependency remains:** adding only Testing Library would fix TypeScript compilation while leaving Vitest's jsdom environment undeclared. Mitigation: declare both direct test requirements. Signal: tests pass in a reduced/fresh install.
- **Low — scope expansion:** changing tsconfig or Docker stages could alter validation or image behavior unnecessarily. Mitigation: restrict implementation to manifest and lockfile ownership. Signal: Dockerfile and tsconfigs remain unchanged.

### Technical Debt

The full monorepo install can mask undeclared workspace dependencies through Yarn Classic hoisting. This incident does not justify repository-wide dependency isolation work. Fix the owning workspace now and defer any broader workspace audit.

### Implementation Roadmap

1. Update the Shooter Workshop development dependencies with the repository-aligned Testing Library and jsdom versions.
2. Refresh the frontend lockfile using the repository's Yarn Classic workspace flow.
3. Run the Shooter Workshop test suite from the frontend workspace root.
4. Run the Shooter Workshop application build.
5. Build the Shooter Workshop container from the same frontend context used by CI and confirm the `RUN yarn build` layer completes.
6. Stop after these dependency and lockfile changes; do not alter tsconfig inclusion, Docker stages, application code, or unrelated workspace dependencies.

### Testing Strategy

- **Component:** run the existing `FeedbackProvider.test.tsx` through the Shooter Workshop Vitest suite; this proves Testing Library resolution and jsdom startup.
- **Build:** run the existing workspace build; this proves `tsc -b` resolves the test imports and Vite still emits the application.
- **Container:** build the existing Dockerfile using `frontend` as context; this reproduces the reduced-manifest dependency installation that failed in CI.
- **Negative/edge:** use a frozen-lockfile install so an uncommitted or inconsistent lockfile fails explicitly.
- Backend, contract, security, migration, and end-to-end tests are not applicable because runtime behavior is unchanged.

### Migration And Rollback

No data, API, auth, cache, or configuration migration is required. Deploy after the test, workspace build, and container build gates pass. Rollback consists of reverting the two dependency metadata changes; there is no persistent runtime state or compatibility window.

An independent post-validation code review is not warranted for this bounded dependency correction unless the implementation expands into tsconfig, Docker, or workflow changes.

### Open Questions

None identified.

### Implementation Record

Implemented on 2026-11-14:

- Added `@testing-library/react` (`^16.3.2`) to the Shooter Workshop development dependencies.
- Added `jsdom` (`^26.0.0`) to the Shooter Workshop development dependencies.
- Refreshed `frontend/yarn.lock` with Yarn Classic.
- Left the Dockerfile and TypeScript inclusion rules unchanged.

Validation:

- `yarn --cwd frontend workspace shooter-workshop.client test --run src/components/feedback/FeedbackProvider.test.tsx` — passed, 2 tests.
- `yarn --cwd frontend workspace shooter-workshop.client build` — passed; TypeScript compilation and Vite production build completed.
- `docker build --file shooter-workshop.client/Dockerfile --tag shooter-workshop-client:dependency-fix .` from `frontend` — not executed because the Docker daemon is unavailable in the environment (`Cannot connect to the Docker daemon at unix:///var/run/docker.sock`).

Residual risk: the CI/container gate still needs to run with an available Docker daemon. The dependency-resolution and application build portions of the failing path are verified locally.
