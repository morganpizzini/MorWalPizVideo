# Feature handoffs

This directory is an intake area for user-editable implementation handoffs,
not a second architecture library. Start at the
[documentation index](../README.md) for compact feature records and at the
[architecture guide](../architecture/README.md) for system boundaries.

## New work

Use the [handoff template](feature-handoff-template.md) for a bounded plan.
Distinguish source evidence, proposed behavior, actual validation, and deployment
approval. After implementation, update the handoff, merge enduring details and
remaining gates into the owning feature record, update incoming links, and retire
the redundant plan. Pending proposals remain pending after consolidation.

## Preserved fixtures

`freezer-analysis-samples/` remains in place. It contains test fixtures, not
obsolete planning documents:
[FreezerPdfPairSampleTests.cs](../../FreezerAnalysis.Api.Tests/FreezerPdfPairSampleTests.cs)
uses this exact directory for both repository discovery and PDF inputs.
Documentation cleanup must not move or delete it.

## Former handoff map

The task records below were consolidated in documentation-cleanup batches on
2026-11-09 and 2026-11-17. Their filenames are retained here for discovery; use
the linked topic as the continuing record.
Reported implementation and test results were not rerun during this
documentation-only change. Earlier plans do not override newer delivery records,
and no pending rollout gate is closed by this move.

| Former filename | Continuing record |
| --- | --- |
| `ai-prompt-orchestration-handoff.md` | [Editorial and AI](../features/editorial-and-ai.md) |
| `back-office-spa-deployment-diagnostics-handoff.md` | [Delivery and tooling](../features/delivery-and-tooling.md) |
| `backoffice-blog-routing-shell-remediation-handoff.md` | [Blog authoring](../features/blog-authoring.md) |
| `backoffice-blog-save-media-remediation-handoff.md` | [Blog authoring](../features/blog-authoring.md) |
| `backoffice-channel-transport-remediation-handoff.md` | [Accounts and channels](../features/accounts-and-channels.md) |
| `backoffice-public-account-sort-remediation-handoff.md` | [Accounts and channels](../features/accounts-and-channels.md) |
| `backoffice-spa-installazione-pwa-handoff.md` | [Delivery and tooling](../features/delivery-and-tooling.md) |
| `backoffice-test-runner-timeout-handoff.md` | [Delivery and tooling](../features/delivery-and-tooling.md) |
| `blog-authoring-controls-handoff.md` | [Blog authoring](../features/blog-authoring.md) |
| `blog-cover-explicit-upload-remediation-handoff.md` | [Blog authoring](../features/blog-authoring.md) |
| `blog-cover-image-crop-handoff.md` | [Blog authoring](../features/blog-authoring.md) |
| `blog-reader-interactions-handoff.md` | [Public experience](../features/public-experience.md) |
| `blog-social-preview-remediation-handoff.md` | [Public experience](../features/public-experience.md) |
| `channel-public-social-links-handoff.md` | [Accounts and channels](../features/accounts-and-channels.md) |
| `channel-terminology-null-compatibility-handoff.md` | [Accounts and channels](../features/accounts-and-channels.md) |
| `creator-partnership-campaign-workspace-handoff.md` | [Editorial and AI](../features/editorial-and-ai.md) |
| `docker-image-ci-gate-handoff.md` | [Delivery and tooling](../features/delivery-and-tooling.md) |
| `editorial-production-workflow-handoff.md` | [Editorial and AI](../features/editorial-and-ai.md) |
| `freezer-analysis-handoff.md` | [FreezerAnalysis](../features/freezer-analysis.md) |
| `freezer-analysis-route-field-refinement-handoff.md` | [FreezerAnalysis](../features/freezer-analysis.md) |
| `gallery-build-fix.md` | [Public experience](../features/public-experience.md) |
| `gallery-direct-link-handoff.md` | [Public experience](../features/public-experience.md) |
| `gallery-feedback-plan-update.md` | [Public experience](../features/public-experience.md) |
| `morwalpizvideo-client-buildkit-cache-handoff.md` | [Delivery and tooling](../features/delivery-and-tooling.md) |
| `multi-video-import-handoff.md` | [Content import](../features/content-import.md) |
| `new-ui-design-handoff.md` | [Public experience](../features/public-experience.md) |
| `public-account-registration-login-remediation-handoff.md` | [Accounts and channels](../features/accounts-and-channels.md) |
| `public-accounts-anonymous-push-handoff.md` | [Accounts and channels](../features/accounts-and-channels.md) |
| `public-client-admin-remediation-handoff.md` | [Public experience](../features/public-experience.md) |
| `public-cookie-article-layout-remediation-handoff.md` | [Public experience](../features/public-experience.md) |
| `puck-editor-layout-feedback-handoff.md` | [Blog authoring](../features/blog-authoring.md) |
| `serverapi-buildkit-cache-handoff.md` | [Delivery and tooling](../features/delivery-and-tooling.md) |
| `shortlink-request-classification-plan.md` | [Short-link classification](../features/shortlink-classification.md) |
| `site-wide-layout-alignment-handoff.md` | [Public experience](../features/public-experience.md) |
| `video-importer-channel-settings-handoff.md` | [VideoImporter settings](../features/video-importer-settings.md) |
| `video-reference-classification-handoff.md` | [Content import](../features/content-import.md) |
| `backoffice-navigation-home-handoff.md` | [BackOffice admin dashboard](../architecture/admin-dashboard.md) |
| `backoffice-personal-quick-actions-handoff.md` | [BackOffice admin dashboard](../architecture/admin-dashboard.md) |
| `backoffice-video-fetch-performance-handoff.md` | [Content import](../features/content-import.md) |
| `channel-editing-and-response-contracts-handoff.md` | [Accounts and channels](../features/accounts-and-channels.md) |
| `channel-public-base-url-handoff.md` | [Accounts and channels](../features/accounts-and-channels.md) |
| `global-feedback-handoff.md` | [Global feedback](../features/global-feedback.md) |
| `quick-links-minimal-layout-handoff.md` | [Public experience](../features/public-experience.md) |
| `shared-account-registration-handoff.md` | [Accounts and channels](../features/accounts-and-channels.md) |
| `shooter-workshop-allocation-removal-handoff.md` | [Shooter Workshop](../architecture/shooter-workshop.md) |
| `shooter-workshop-ammunition-container-presentation-handoff.md` | [Shooter Workshop](../architecture/shooter-workshop.md) |
| `shooter-workshop-ammunition-production-merge-handoff.md` | [Shooter Workshop](../architecture/shooter-workshop.md) |
| `shooter-workshop-component-feedback-handoff.md` | [Shooter Workshop](../architecture/shooter-workshop.md) |
| `shooter-workshop-consolidated-workspaces-handoff.md` | [Shooter Workshop](../architecture/shooter-workshop.md) |
| `shooter-workshop-container-test-dependency-handoff.md` | [Delivery and tooling](../features/delivery-and-tooling.md) |
| `shooter-workshop-equipment-workspace-handoff.md` | [Shooter Workshop](../architecture/shooter-workshop.md) |
| `shooter-workshop-localization-handoff.md` | [Shooter Workshop](../architecture/shooter-workshop.md) |
| `shooter-workshop-production-route-handoff.md` | [Shooter Workshop](../architecture/shooter-workshop.md) |
| `shooter-workshop-pwa-handoff.md` | [Shooter Workshop](../architecture/shooter-workshop.md) |
| `shooter-workshop-recipes-route-edit-handoff.md` | [Shooter Workshop](../architecture/shooter-workshop.md) |
| `shooter-workshop-shooting-session-fifo-inline-correction-handoff.md` | [Shooter Workshop](../architecture/shooter-workshop.md) |
| `shooter-workshop-ui-feedback-handoff.md` | [Shooter Workshop](../architecture/shooter-workshop.md) |
| `video-importer-startup-schema-gate-handoff.md` | [Windows applications](../architecture/windows-apps.md) |
