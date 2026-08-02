# Solution Structure

## .NET Projects

| Project | Type | References | Responsibility |
|---|---|---|---|
| `MorWalPizVideo.Models` | .NET 10 library | None | Mongo entities, embedded records, enums, serializers, configuration POCOs, collection names, cache keys |
| `MorWalPizVideo.Domain` | .NET 10 library | Models | Repository interfaces and Mongo/mock implementations, data and external-service abstractions |
| `MorWalPiz.Contracts` | .NET 10 library | Models | Cross-process request/response DTOs and conversion helpers |
| `MorWalPizVideo.MvcHelpers` | .NET 10 library | Domain, Models | Shared ASP.NET controller helpers, cache services, Mongo setup, feature utilities, test auth |
| `MorWalPizVideo.ServiceDefaults` | .NET 10 library | Aspire packages | Service discovery, HTTP resilience, OpenTelemetry, health endpoints |
| `MorWalPizVideo.BackOffice` | ASP.NET API | Contracts, Domain, Models, MvcHelpers, ServiceDefaults | Administrative system and business-management center |
| `MorWalPizVideo.ServerAPI` | ASP.NET API | Domain, Models, MvcHelpers, ServiceDefaults | Public projections and approved public interactions |
| `MorWalPizVideo.ShortLinks` | ASP.NET API | Domain, MvcHelpers, ServiceDefaults | Branded redirects and usage tracking |
| `MorWalPizVideo.AppHost` | Aspire host | Three service projects | Local orchestration for APIs and selected frontends |
| `MorWalPizVideo.BackOffice.Tests` | xUnit/Reqnroll | BackOffice, ServerAPI | HTTP behavior, authentication, repositories, cache and contract tests |
| `MorWalPiz.VideoImporter` | .NET 10 WPF | Contracts | Local upload, scheduling, tenant, SQLite, and BackOffice integration |
| `MorWalPiz.InsightScanner` | .NET 10 WPF | Contracts | Local web scanning and insight submission |

`MorWalPizVideo.Operations` is not a project and must not be treated as a deployable component.

## Frontend Workspace

The Yarn Classic workspace in `frontend/package.json` contains:

| Package | Responsibility |
|---|---|
| `@morwalpizvideo/models` | Shared strict TypeScript models and DTO shapes |
| `@morwalpizvideo/services` | Endpoint constants, URL composition, Fetch client, token/credential injection, domain APIs |
| `@morwalpiz/layout` | Shared React navigation, content, category, video, and presentation components |
| `back-office-spa` | Authenticated administration UI using React Router data routes |
| `morwalpizvideo.client` | Public SSR/PWA application hosted at `morwalpiz.com` |
| `morwalpiz-shop.client` | Free digital-artifact catalog, cart, acquisition, and download UI |
| `shooting-ita-frontend` | Focused PWA using shared content and layout packages |

Shared packages build in this order: models, services, layout. Consumers build afterward.

## Source Ownership Rules

| Concern | Owner |
|---|---|
| Persistence entities and storage constraints | Models |
| Cross-process .NET DTOs | Contracts |
| Repository and reusable service behavior | Domain |
| Shared ASP.NET host behavior | MvcHelpers |
| Telemetry, resilience, service discovery, health conventions | ServiceDefaults |
| Administrative composition and workflows | BackOffice |
| Public API composition and projections | ServerAPI |
| Redirect resolution | ShortLinks |
| Shared TypeScript DTOs | Frontend models package |
| Shared endpoint/network behavior | Frontend services package |
| Truly shared presentation | Frontend layout package |

## Namespace Debt

Models and Domain currently retain `MorWalPizVideo.Server.*` namespaces, while shared controller bases retain BackOffice namespaces. These names do not reflect project ownership and contribute to accidental authorization coupling. Namespace correction is a staged compatibility refactor, not a reason to merge projects.

## Startup Roots

- `MorWalPizVideo.BackOffice/Program.cs`
- `MorWalPizVideo.ServerAPI/Program.cs`
- `MorWalPizVideo.ShortLinks/Program.cs`
- `MorWalPizVideo.AppHost/Program.cs`
- `MorWalPiz.VideoImporter/App.xaml.cs`
- `MorWalPiz.InsightScanner/App.xaml.cs`
- Each React application's `main.tsx` or server entrypoint

## Exclusions

`frontend/TelePrompter` and `frontend/stage-designer` are explicitly outside this guide. Their presence does not imply shared-package or deployment support.