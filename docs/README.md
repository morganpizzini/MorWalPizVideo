# Documentation

Start with the [architecture guide](architecture/README.md) for ownership,
system boundaries, and accepted decisions. Source, manifests, workflows, and
executable tests take precedence over documentation.

## Feature records

These compact records consolidate the former task-by-task handoffs. They retain
decisions, reported delivery evidence, unresolved work, and release gates without
repeating repository-wide architecture. A recorded test result is historical
evidence, not a new verification or deployment approval.

| Topic | Record |
| --- | --- |
| Blog editor, covers, media, and publication | [Blog authoring](features/blog-authoring.md) |
| Public blog, layout, gallery, and metadata | [Public experience](features/public-experience.md) |
| Accounts, push, channel transport, and public links | [Accounts and channels](features/accounts-and-channels.md) |
| Editorial workflow, AI, and partnerships | [Editorial and AI](features/editorial-and-ai.md) |
| Multi-video import and reference classification | [Content import](features/content-import.md) |
| FreezerAnalysis delivery and production gates | [FreezerAnalysis](features/freezer-analysis.md) |
| CI, deployment diagnostics, build cache, and test runner | [Delivery and tooling](features/delivery-and-tooling.md) |
| Desktop channel configuration synchronization | [VideoImporter settings](features/video-importer-settings.md) |
| Short-link traffic classification proposal | [Short-link classification](features/shortlink-classification.md) |

For a former filename, consult the
[handoff migration map](feature-handoffs/README.md#former-handoff-map).

## Working documents

- [Handoff intake and template](feature-handoffs/README.md): new, bounded work
  awaiting implementation or review.
- [Architecture decisions](architecture/architecture-decisions.md): durable
  boundary decisions; feature records do not supersede accepted ADRs.
- [Technical debt](architecture/technical-debt.md) and
  [roadmap](architecture/refactoring-roadmap.md): cross-feature priorities.
- [Test execution](architecture/testing.md),
  [deployment](architecture/deployment.md), and
  [MongoDB operations](architecture/mongo-operations.md): operating procedures.
- [Operations records](architecture/operations/): environment-specific release
  and recovery guidance.

## Maintenance

Keep one feature record per coherent ownership boundary. Update its delivery
status and remaining gates rather than appending another complete implementation
plan. Keep proposed work explicitly separate from delivered behavior. Link to
architecture and runbooks instead of duplicating them.

New handoffs remain user-editable. After delivery, merge enduring decisions,
actual validation, and residual risks into the owning feature record, update
incoming links, and retire the redundant handoff. Do not remove pending work or
turn incomplete checks into passing results during consolidation.
