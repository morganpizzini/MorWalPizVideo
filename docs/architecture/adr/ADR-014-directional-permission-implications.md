# ADR-014: Domain-Owned Directional Permission Implications

- **Status:** Accepted
- **Date:** 2026-08-08

## Context

Permission checks previously used direct and active-group unions, with isolated special handling for `backoffice.manageall`. Repeating implication checks in authorization, projections, or the SPA would allow those surfaces to disagree.

## Decision

The Domain security layer owns normalized lowercase-invariant, cycle-safe, transitive permission expansion through `AuthorizationPermissionExpander`. Expansion preserves supplied parents and uses an explicit, reviewable mapping. It does not infer implications through reflection or permission-name prefixes, so a newly declared sensitive leaf is not silently granted by an existing parent.

Every declared resource `manage` permission implies the declared CRUD siblings for that resource. The explicit exceptions and specialized leaves are:

- `users.manage` also implies `users.permissions.manage`.
- `videos.manage` also implies `videos.import`, `videos.translate`, and `videos.publish`.
- `forms.manage` also implies `forms.responses.view`.
- `insights.manage` also implies `insights.scan`.
- `images.manage` implies `images.view`, `images.create`, and `images.delete`; no `images.update` permission exists.
- `diagnostics.view` remains standalone because no `diagnostics.manage` permission exists.

Implication is directional. Leaves do not imply parents, sibling capabilities, or permissions in another resource. `backoffice.manageall` remains the evaluator and frontend global bypass, but expansion materializes only `backoffice.access`; it does not add the permission catalog to effective permissions.

The resolver expands after direct and active-group union. Authorization expands after repository permissions and claims are merged. RBAC summaries reuse the same expander. The SPA consumes only server-returned effective permissions and does not encode implication rules.

`backoffice.access` authorizes login and BackOffice entry only. RBAC administration requires `users.permissions.manage`; user lifecycle endpoints require their corresponding granular leaves. A permission manager may grant any permission, including `backoffice.manageall`, without a separate self-management rule.

## Consequences

Existing persistence and JSON shapes remain unchanged and no migration is required. Persisted parent grants become effective immediately. Auth validation and RBAC summary responses only gain strings in their existing `effectivePermissions` arrays. Adding an implication is a backend security change and requires hierarchy, authorization, projection, and auth-validation tests.

## Migration And Rollback

No data migration is needed. Rollback removes the hierarchy entries and restores endpoint policies, but persisted direct and group grants remain compatible.

## Validation

Focused backend tests cover expansion direction, repository and claim merging, summaries, global bypass, and administration matrices. Frontend tests cover route guards and leaf-based lifecycle visibility.