# Shooting ITA Implementation Plan

## 1. Architectural Overview

The new "Shooting ITA" ecosystem will seamlessly integrate with the existing `MorWalPizVideo.Backoffice` infrastructure. The system consists of two major components:
1.  **ASP.NET Core API Gateway (Backend):** Built upon the existing generic repository patterns and Web API infrastructure.
2.  **React 19 PWA (Frontend):** A localized frontend specifically for external users and competitors.

## 2. Data Models (MongoDB)

We will extend the `MorWalPizVideo.Models` project. Since the application uses MongoDB as its primary datastore through generic repositories (`BaseRepository<T>`), our models must derive from `BaseEntity`.

### 2.1 Identity and Organization
-   **`User` (Existing/Extended):** Needs properties referencing associations with particular matches or generic authentication parameters for the Web PWA.
-   **`YTChannel` (Existing):** Many-to-many references need optimization.
-   **`ChannelUserRole`:** A mapping entity to link users to channels with specific permissions.

### 2.2 Competition & Stage Hub
-   **`Competition` (New):** Represents a shooting match. Contains embedded metadata (dates, locations, rules).
-   **`Stage` (Embedded or New Document):** Detailed description of a match stage. Given MongoDB's document nature, stages could be an array within the `Competition` document (`List<Stage> Stages`) if bounded in size (e.g., maximum 20-30 stages per match).

## 3. Backend (MorWalPizVideo.ServerAPI)

### 3.1 Authentication
-   Implement a unified JWT-based Authentication scheme across both specific custom back-offices and the public PWA.
-   Leverage existing `ShopAuthController` architectural patterns to cleanly decouple user management.

### 3.2 Caching & Background Jobs
-   **Redis Cache:** Implement `IDistributedCache` for frequent queries, effectively using `MorWalPizVideo.Models.Constraints.CacheKeys`.
-   **Hangfire:** Incorporate Hangfire into `MorWalPizVideo.ServerAPI` for scheduling background jobs like asynchronous score processing, video generation alerts, or bulk email notifications.

### 3.3 Analytics via Aggregation Framework
-   Use MongoDB driver's Aggregation Framework inside dedicated API controllers (e.g., `CompetitionsController`) to calculate match stats (`BsonDocument` projections, grouping).

## 4. Frontend (React 19 PWA)

### 4.1 Technology Stack
-   **Framework:** React 19 using Vite.
-   **Styling:** Tailwind CSS.
-   **State/Data Fetching:** React Router loaders/actions patterns combined with `@morwalpizvideo/services`.
-   **PWA:** Establish a structured `manifest.json` and a Service Worker instance registered on application startup.

### 4.2 Web Push APIs
-   The Service worker must listen to standard `push` events.
-   Backend will emit push notifications formatted with unified payload structures indicating competition score updates or match time changes.

## 5. Development Phases

**Phase 1: Backend Domain Expansion**
- Create models (`Competition.cs`, `Stage.cs`) in `MorWalPizVideo.Models/Models`.
- Update `DbCollections.cs`.
- Create corresponding generic repositories.

**Phase 2: API Implementation**
- Expose CRUD endpoints in `MorWalPizVideo.ServerAPI` (e.g., `CompetitionsController`, `HangfireEndpoints`).
- Integrate Redis caching decorators.

**Phase 3: Frontend PWA Setup ("shooting-ita-frontend")**
- Scaffold React 19 UI with Tailwind.
- Connect existing shared service libraries.
- Construct the Service Worker code manually for caching and Push Notifications.

**Phase 4: Integrations**
- Link Hangfire job processing to Web Push trigger endpoints.
- Establish many-to-many YT Channel association UI.