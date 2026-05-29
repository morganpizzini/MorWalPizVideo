# MorWalPizVideo.BackOffice — Project Documentation

The admin Web API powering the MorWalPizVideo platform. It exposes the management surface used by the `back-office-spa` (React 19) SPA and by the WPF `MorWalPiz.VideoImporter` desktop tool. It owns YouTube content lifecycle, translations, the digital shop, social distribution (Discord / Telegram / Pinterest), insights (AI content planning), custom forms, sponsor management and the Shooting ITA vertical (competitions / user requests / push notifications).

> Companion docs already in the repo: [API_KEY_AUTHENTICATION.md](API_KEY_AUTHENTICATION.md) · [HEALTH_CHECKS.md](HEALTH_CHECKS.md) · [docs/AUTHENTICATION_SECURITY_IMPROVEMENTS.md](../docs/AUTHENTICATION_SECURITY_IMPROVEMENTS.md) · [docs/GITHUB_PRODUCTION_DEPLOYMENT.md](../docs/GITHUB_PRODUCTION_DEPLOYMENT.md) · [memory-bank/](../memory-bank/)

---

## 1. Architecture at a Glance

```
┌─────────────────────┐   JWT cookie / Bearer    ┌──────────────────────────┐
│  back-office-spa    │ ───────────────────────► │                          │
│  (React 19)         │                          │   MorWalPizVideo         │
└─────────────────────┘                          │   .BackOffice            │
                                                 │   (ASP.NET Core 8)       │
┌─────────────────────┐    X-API-Key header      │                          │
│  VideoImporter WPF  │ ───────────────────────► │  ─ Controllers (33)      │
└─────────────────────┘                          │  ─ Services              │
                                                 │  ─ Hangfire jobs         │
                                                 │  ─ Health checks         │
                                                 └────────────┬─────────────┘
                                                              │
       ┌──────────────────┬──────────────────┬────────────────┼────────────────┐
       ▼                  ▼                  ▼                ▼                ▼
   MongoDB         Azure OpenAI       YouTube Data API   Azure Translator  Discord/Telegram/
   (or Mocks)      (Semantic Kernel)  v3                                   Pinterest/Facebook
```

### Project layout
| Folder | Purpose |
| ------ | ------- |
| [Program.cs](Program.cs) | Composition root: DI, auth, feature flags, Hangfire, health checks, Swagger, CORS. |
| [Authentication/](Authentication/) | `ApiKeyAuthenticationHandler`, `[ApiKeyAuth]` attribute, Swagger security filter. |
| [Configuration/](Configuration/) | Strongly-typed `IOptions<T>` POCOs (`AzureConfig`, `PinterestSettings`, …). |
| [Controllers/](Controllers/) | 33 controllers — see §3. |
| [Data/](Data/) | Seed JSON files used by the **Mock** repositories (`EnableMock=true`). |
| [Jobs/](Jobs/) | Hangfire recurring background jobs — see §5. |
| [Services/](Services/) | Application services (auth, chat AI, rate-limiting, image generation, Telegram/Discord/Facebook clients, factories). |
| [fonts/](fonts/) | Bundled fonts used by `UtilityController.GenerateTextImage`. |

### Cross-project dependencies
- **MorWalPizVideo.Domain** — repository interfaces + MongoDB implementations + `DataService` / `ExternalDataService` / `BlobService` / `AzureTranslatorService`.
- **MorWalPizVideo.Models** — domain entities, enums, JSON converters, constraint constants.
- **MorWalPizVideo.MvcHelpers** — `ApplicationControllerBase`, `BaseRequest<T>` / `BaseRequestId<T>` wrappers.
- **MorWalPizVideo.ServiceDefaults** — Aspire defaults (`AddServiceDefaults`, `MapDefaultEndpoints`).
- **MorWalPiz.Contracts** — DTO contracts shared with the public ServerAPI.

---

## 2. Cross-cutting Concerns

### 2.1 Authentication & Authorization
| Scheme | Where it applies | Notes |
| ------ | ---------------- | ----- |
| **JWT Bearer** (default) | Every controller inheriting [`ApplicationControllerBase`](../MorWalPizVideo.MvcHelpers/Controllers/ApplicationControllerBase.cs) — it carries `[Authorize]` at the class level. | Token read from `Authorization: Bearer …` **or** from the HttpOnly cookie `auth_token` (set by `/api/auth/login`). |
| **API Key** | Controllers/actions decorated `[ApiKeyAuth]` (notably `ChatController`). | Header `X-API-Key`. Hashed (SHA-256) at rest, rate-limited, optional IP whitelist + expiry. Managed via `/api/apikeys`. |
| **Anonymous** | Endpoints explicitly tagged `[AllowAnonymous]` (login, public form submission, user request submission, `/api/user/init/{username}` bootstrap). | |

See [API_KEY_AUTHENTICATION.md](API_KEY_AUTHENTICATION.md) and [docs/AUTHENTICATION_SECURITY_IMPROVEMENTS.md](../docs/AUTHENTICATION_SECURITY_IMPROVEMENTS.md) (HttpOnly cookies, PBKDF2-SHA256 100k iterations, HSTS, CORS with credentials).

### 2.2 Feature Flags (`Microsoft.FeatureManagement`)
Configured under `FeatureManagement` in `appsettings*.json`.

| Flag | Effect when enabled |
| ---- | ------------------- |
| `EnableMock` | Swaps every repository + most external services for in-memory mocks reading from [Data/*.json](Data/). Lets the API run with no MongoDB / no third-party creds. |
| `EnableHangFire` | Registers Hangfire server + dashboard at `/hangfire` and schedules the recurring jobs (§5). Storage = SQL Server in prod, in-memory in dev. |
| `EnableSwagger` | Mounts Swagger UI at `/swagger` with both `Bearer` and `ApiKey` security schemes. |
| `EnableKeyVault` | Loads secrets from Azure Key Vault (URL in `KeyVaultUrl`) using `DefaultAzureCredential`. Falls back gracefully if unreachable. |
| `EnableCors` | Uses the strict `MorWalPizPolicy` (admin SPA origin only, credentials enabled) instead of `AllowAllOrigins`. |

### 2.3 Health Checks
Exposed at `/health`, `/health/live`, `/health/ready`, `/health/startup` and (dev) `/alive`. Probes: `self`, `mongodb`, `azure-openai`, `youtube-api`, `morwalpiz-api`, `hangfire-sqlserver`, `hangfire-processing`, `feature-management`, `mock-services`. Full details in [HEALTH_CHECKS.md](HEALTH_CHECKS.md).

### 2.4 Request/Response Conventions
- Routes follow `api/[controller]` by convention (provided by `ApplicationControllerBase`).
- Request bodies are typically wrapped in either a plain DTO or one of the helpers:
  - `BaseRequest<T>` — body only.
  - `BaseRequestId<T>` — id from route + body.
- JSON serializer registers custom converters for `CustomFormQuestion` / `CustomFormAnswer` (polymorphic by `QuestionType` / `AnswerType`).
- AI / Chat endpoints use Semantic Kernel + Azure OpenAI (`AzureConfig.OpenAi`).

---

## 3. Controllers Reference

> Notation: routes are shown relative to the host. Unless stated otherwise, the controller inherits `ApplicationControllerBase` ⇒ base route `api/[controller]` and class-level `[Authorize]` (JWT required).

### 3.1 Authentication & Identity

#### `AuthController` — `api/auth` *(anonymous)*
Login/logout/validate. Sets `auth_token` HttpOnly Secure SameSite=Strict cookie on success; also returns the JWT in body for transitional clients.
| Verb | Route | Method | Purpose |
| ---- | ----- | ------ | ------- |
| POST | `/login` | `Login(LoginRequest)` | Username + password. Rate-limited via `IRateLimitingService`; logs attempt to `LoginAttempt`. |
| POST | `/logout` | `Logout()` | Clears the `auth_token` cookie. |
| POST | `/validate` | `ValidateToken(ValidateTokenRequest)` | Validates a token's signature & expiry. |

#### `UserController` — `api/user`
Admin user CRUD + bootstrap. `InitUsers` is `[AllowAnonymous]` and used **once** to seed the first admin account; do not expose in production once seeded.
| Verb | Route | Purpose |
| ---- | ----- | ------- |
| GET | `/` | List all admin users. |
| GET | `/{id}` | Get user by id. |
| GET | `/init/{username}` *(anon)* | Bootstrap default admin user. |
| POST | `/` | Create user (`CreateUserRequest`). |
| PUT | `/{id}` | Update user. |
| PUT | `/{id}/status` | Activate/deactivate. |
| DELETE | `/{id}` | Delete user. |

#### `ApiKeysController` — `api/apikeys`
CRUD for service-to-service API keys (model: [`ApiKey`](#52-apikey)). The plaintext key is returned **only at create/regenerate** time. See [API_KEY_AUTHENTICATION.md](API_KEY_AUTHENTICATION.md).
| Verb | Route | Purpose |
| ---- | ----- | ------- |
| POST | `/` | Create key (returns plaintext once). |
| GET | `/` | List metadata for all keys (no plaintext). |
| GET | `/{id}` | Get key metadata. |
| PUT | `/{id}` | Update name/limits/IPs/expiry. |
| POST | `/{id}/toggle` | Enable/disable without deleting. |
| POST | `/{id}/regenerate` | Replace the secret (returns new plaintext). |
| DELETE | `/{id}` | Remove key. |

### 3.2 Video Domain

#### `VideosController` — `api/videos`
Core surface for managing the YouTube content tree (root collections + sub-videos).
| Verb | Route | Purpose |
| ---- | ----- | ------- |
| GET | `/` | List every `YouTubeContent`. |
| GET | `/{id}` | Fetch a single content document. |
| PUT | `/{id}` | Update editable metadata. |
| POST | `/Translate` | Batch-translate the titles/descriptions of the given video ids via Azure Translator. |
| POST | `/ImportVideo` | Import a single YouTube video by id (fetches metadata + thumbnail). |
| POST | `/ConvertIntoRoot` | Promote an existing video to a root `YouTubeContent` collection. |
| POST | `/SwapThumbnailId` | Change the `ThumbnailVideoId` driving a collection's poster. |
| POST | `/RootCreation` | Create a new root collection from scratch. |
| POST | `/ImportSubCreation` | Attach a new sub-video to an existing root. |
| POST | `/{id}/refresh-youtube` | Re-pull stats from the YouTube Data API for one item. |
| POST | `/{id}/publish-social` | Push the video to the configured social channels (`PublishSocialRequest`). |

#### `ChannelsController` — `api/channels`
Manage tracked YouTube channels ([`YTChannel`](#510-channels-and-creator-tracking)).

#### `CategoriesController` — `api/categories`
CRUD for content categories used to tag videos / events / collections.

#### `CompilationsController` — `api/compilations`
Curated playlist-like groupings of `VideoRef` (model: [`Compilation`](#54-content-grouping)).

#### `YouTubeVideoLinksController` — `api/youtubevideolinks`
Cross-promotion links from a `match` (sponsored/contracted video) to other creators' YouTube videos. Also serves the creator profile image.

#### `PublishScheduleController` — `api/publishschedule`
Scheduled publication entries (a `VideoId` × date × message × query-link list).

### 3.3 Sponsorships, Pages & Bio Links

#### `SponsorsController` — `api/sponsors`
CRUD for `Sponsor`. Accepts `multipart/form-data` (`[FromForm]`) so a logo file can be uploaded inline.

#### `SponsorAppliesController` — `api/sponsorapplies`
Read-only listing of sponsor application submissions (`SponsorApply`). Submissions are created from the public ServerAPI; this endpoint only surfaces them to admins.

#### `PagesController` — `api/pages`
Manage CMS-style pages that bundle Reel/Short ids (`Page.VideoReelIds`, `Page.ShortReelIds`).

#### `BioLinksController` — `api/biolinks`
Linktree-style entries. Add / update / **toggle** enabled state / delete by title.

#### `ShortLinksController` — `api/shortlinks`
Branded URL shortener entries (`ShortLink`, `LinkType` discriminator) — used by Discord/Telegram/Pinterest publishing flows.

#### `QueryLinksController` — `api/querylinks`
Templated query-string snippets attached to a `PublishSchedule` (used for UTM / tracking).

### 3.4 Calendar & Custom Forms

#### `CalendarEventsController` — `api/calendarevents`
CRUD for `CalendarEvent` plus `GET /by-title/{title}` lookup. Events can link to a related match (`MatchId`, `MatchUrl`).

#### `CustomFormsController` — `api/customforms`
Dynamic form designer (text / select / checkbox / etc. — see `CustomFormEnums`). Public form submission is open via `POST /{id}/responses` (`[AllowAnonymous]`); listing the responses requires auth.

#### `ConfigurationController` — `api/configuration`
Key/value store for `MorWalPizConfiguration`. Supports lookup by id or by key (`/key/{key}`).

### 3.5 Shop (Digital Products)

The shop has its own URL namespace (`api/shop/...`) and **does not** inherit `ApplicationControllerBase`, so its endpoints are by default anonymous unless explicitly secured.

#### `ShopCatalogController` — `api/shop/catalog` *(anonymous)*
Browseable storefront — list products, get product, list categories.

#### `ShopCartController` — `api/shop/cart` *(anonymous, customer scoped)*
Cart operations keyed by `customerId` query string: get, add item, remove item, checkout.

#### `ShopAuthController` — `api/shop/auth` *(anonymous)*
Customer (not admin) login by email (`EmailLoginRequest` → `LoginResponse`). Returns a customer-facing token used by the shop SPA.

#### `ProductsController` — `api/products`
Legacy/admin CRUD for the catalog `Product` model (distinct from `DigitalProduct`).

#### `ProductCategoriesController` — `api/productcategories`
Admin CRUD for product categories.

### 3.6 AI / Insights / Utilities

#### `ChatController` — `api/chat` *(API key)*
AI helpers backed by Azure OpenAI via Semantic Kernel. Designed for the WPF importer.
| Verb | Route | Purpose |
| ---- | ----- | ------- |
| POST | `/` | `GetReviewDetails(ReviewRequest)` — generate review/summary for a video. |
| POST | `/translate` | `TranslateVideoContent(VideoTranslationRequest)` — high-quality translation. |
| POST | `/transcript-analysis` | `AnalyzeTranscript(TranscriptAnalysisRequest)` — extract topics / chapters. |

#### `InsightsController` — `api/insights`
Content-planning suite: AI agent–driven topic monitoring → news scanning → content plan generation.
| Resource | Endpoints |
| -------- | --------- |
| Topics (`InsightTopic`) | `GET /topics`, `GET /topics/{id}`, `POST /topics`, `PUT /topics/{id}`, `DELETE /topics/{id}`, `POST /topics/{id}/scan-news` |
| News items (`InsightNewsItem`) | `GET /news`, `GET /topics/{id}/news?status=`, `GET /news/{id}`, `PUT /news/{id}/review`, `DELETE /news/{id}` |
| Content plans (`InsightContentPlan`) | `POST /content-plans`, `GET /content-plans`, `GET /topics/{id}/content-plans`, `GET /content-plans/{id}`, `PUT /content-plans/{id}`, `DELETE /content-plans/{id}` |

Implemented through `IInsightAgentService` (`InsightAgentService` in prod, `MockInsightAgentService` in mock).

#### `UtilityController` — `api/utility`
| Verb | Route | Purpose |
| ---- | ----- | ------- |
| POST | `/generate-text-image` | Render a text-on-background PNG (uses bundled fonts). |
| GET | `/fonts` | List available font families packaged under [fonts/](fonts/). |

#### `QRCodeController` — `api/qrcode`
Generates a QR code for arbitrary `data`, with an optional center logo `IFormFile`.

#### `ImageUploadController` — `api/imageupload`
Uploads to blob storage (Azure Blob in prod, `BlobServiceMock` in mock). Supports single (`/upload`) or batch (`/upload-multiple`) uploads, scoped to a `folderName` and optionally placed under a per-match folder.

#### `ScraperController` — `api/scraper`
Quick utility to scrape recent videos + comments for a given `channelName` (used to seed insights / video ideas).

### 3.7 Social Distribution

#### `DiscordController` — `api/discord`
`GET /{shortLink}?message=...` — publishes a post to the configured Discord channel using the URL backed by the given short link. HttpClient is created on demand via `IDiscordHttpClientFactory` so missing tokens don't crash startup.

#### `TelegramController` — `api/telegram`
Twin of Discord but for Telegram (uses `ITelegramHttpClientFactory`).

#### `PinterestController` — `api/pinterest`
| Verb | Route | Purpose |
| ---- | ----- | ------- |
| GET | `/` | `Login()` — start OAuth flow, redirects to Pinterest. |
| GET | `/callback?code=` | OAuth callback; exchanges code for tokens. |
| POST | `/` | `CreatePin(CreatePinterestPinRequest)` — publish a pin. |

### 3.8 Shooting ITA Vertical

Sub-domain for the shooting-competition platform (see `memory-bank/progress.md`).

#### `UserRequestsController` — `api/userrequests`
| Verb | Route | Auth | Purpose |
| ---- | ----- | ---- | ------- |
| POST | `/` | Anonymous | Public submission of a feature request (`SubmitUserRequestDto`). |
| GET | `/?status=` | JWT | List requests, optionally filtered by `UserRequestStatus`. |
| GET | `/{id}` | JWT | Get one. |
| PATCH | `/{id}/status` | JWT | Moderate (`ModerateUserRequestDto` — status + admin note). |
| DELETE | `/{id}` | JWT | Remove. |

Related models live in Domain: `Competition`, `Stage`, `StageEvaluation`, `UserChannel`, `UserChannelOwner`, `PushSubscriptionInfo`. The competitions & push-subscription endpoints are served by other projects (`MorWalPizVideo.ServerAPI`) but share the same MongoDB collections.

---

## 4. Models Reference (`MorWalPizVideo.Models/Models`)

All persisted entities derive from [`BaseEntity`](../MorWalPizVideo.Models/Models/BaseEntity.cs) which provides `string Id` (Mongo `_id`) and `DateTime CreationDateTime`.

### 4.1 Core video graph
| Model | Role |
| ----- | ---- |
| [`YouTubeContent`](../MorWalPizVideo.Models/Models/YouTubeContent.cs) | **Aggregate root.** A collection (root video + sub-videos). Fields: `ContentId`, `Title`, `Description`, `Url`, `ThumbnailVideoId` (id of the video used as poster), `VideoRefs[]`, `Categories[]`, `ContentType` (enum `YoutubeContentType`), `YouTubeVideoLinks[]?`, `ShortLinks[]`, `IsPrivate` (gates access for Shooting ITA). |
| [`Video`](../MorWalPizVideo.Models/Models/Video.cs) | Full video metadata: `YoutubeId`, `Title`, `Description`, view/like/comment counters, `PublishedAt`, `Thumbnail`, `Duration`, `Categories[]`, `IsPrivate`. |
| [`VideoRef`](../MorWalPizVideo.Models/Models/VideoRef.cs) | Lightweight reference embedded inside a `YouTubeContent.VideoRefs[]` — avoids duplicating a full `Video`. |
| [`VideoContent`](../MorWalPizVideo.Models/Models/VideoContent.cs) | Projection/DTO used for cross-API responses. |
| [`VideoDisplayItem`](../MorWalPizVideo.Models/Models/VideoDisplayItem.cs) | Display polymorphism: single video vs. collection (`enum ContentType`). |
| [`YouTubeVideoLink`](../MorWalPizVideo.Models/Models/YouTubeVideoLink.cs) | External creator cross-promo entry (creator name, video id, profile image, optional `ShortLink`). |

### 4.2 Categorization & cross-cutting
| Model | Role |
| ----- | ---- |
| [`Category`](../MorWalPizVideo.Models/Models/Category.cs) | Top-level category (id, title, description). |
| [`CategoryRef`](../MorWalPizVideo.Models/Models/CategoryRef.cs) | Lightweight embedded reference. |
| [`ShortLink`](../MorWalPizVideo.Models/Models/ShortLink.cs) | Branded shortener entry with `ClicksCount` + `LinkType` enum (target type). |
| [`QueryLink`](../MorWalPizVideo.Models/Models/QueryLink.cs) | Reusable query-string template (UTM/tracking) attached to a `PublishSchedule`. |
| [`Page`](../MorWalPizVideo.Models/Models/Page.cs) | CMS page bundling `VideoReelIds` and `ShortReelIds`. |
| [`Compilation`](../MorWalPizVideo.Models/Models/Compilation.cs) | Editor-curated grouping of `VideoRef[]` with title/description/url. |
| [`BioLink`](../MorWalPizVideo.Models/Models/BioLink.cs) | Linktree entry — extends base with `Enable` toggle (other fields in base/derived class). |
| [`PublishSchedule`](../MorWalPizVideo.Models/Models/PublishSchedule.cs) | Scheduled publication: `VideoId`, `QueryStringIds[]`, `Message`, `Date`. |
| [`MorWalPizConfiguration`](../MorWalPizVideo.Models/Models/MorWalPizConfiguration.cs) | Generic key/value runtime configuration. |

### 4.3 Sponsors & calendar
| Model | Role |
| ----- | ---- |
| [`Sponsor`](../MorWalPizVideo.Models/Models/Sponsor.cs) | Sponsor record (logo, name, links, contract metadata). |
| [`SponsorApply`](../MorWalPizVideo.Models/Models/SponsorApply.cs) | Inbound sponsor application form (submitted via ServerAPI). |
| [`CalendarEvent`](../MorWalPizVideo.Models/Models/CalendarEvent.cs) | Scheduled event: title, description, start/end, categories, optional `MatchId`/`MatchUrl`. |

### 4.4 Custom forms (dynamic schema)
| Model | Role |
| ----- | ---- |
| [`CustomForm`](../MorWalPizVideo.Models/Models/CustomForm.cs) | Form definition + `Active` flag + `Questions[]` + collected `Responses[]`. |
| [`CustomFormQuestion`](../MorWalPizVideo.Models/Models/CustomFormQuestion.cs) | Single question — typed by `QuestionType`. |
| [`QuestionOption`](../MorWalPizVideo.Models/Models/QuestionOption.cs) | Option for choice-type questions. |
| [`CustomFormResponse`](../MorWalPizVideo.Models/Models/CustomFormResponse.cs) | A submission (response id + `SubmittedAt` + `Answers[]`). |
| `CustomFormAnswer` (in `CustomFormResponse.cs`) | Polymorphic answer (text / single id / multiple ids) discriminated by `AnswerType`. |
| [`CustomFormEnums`](../MorWalPizVideo.Models/Models/CustomFormEnums.cs) | `QuestionType` & `AnswerType` enums; deserialization handled by custom JSON converters registered in `Program.cs`. |

### 4.5 Shop
| Model | Role |
| ----- | ---- |
| [`DigitalProduct`](../MorWalPizVideo.Models/Models/DigitalProduct.cs) | Sellable item: `Name`, `Description`, `PreviewImageUrl`, `ContentStorageKey` (blob storage key for the deliverable), `CategoryIds[]`, `Price`, `IsActive`, `UpdatedAt`. |
| [`Product`](../MorWalPizVideo.Models/Models/Product.cs) | Legacy product entity (still used by `ProductsController`). |
| [`Customer`](../MorWalPizVideo.Models/Models/Customer.cs) | Shop user (`Email`, `Name`, `LastLoginAt`, `NewsletterAccepted`, `TermsAccepted`, `TermsAcceptedAt`). |
| [`Cart`](../MorWalPizVideo.Models/Models/Cart.cs) | Customer cart with `Items[]` (`CartItem`: `ProductId`, `ProductName`, `Quantity`, `Price`) + `IsCompleted` + `CompletedAt`. |

### 4.6 Insights (AI content planning)
| Model | Role |
| ----- | ---- |
| [`InsightTopic`](../MorWalPizVideo.Models/Models/InsightTopic.cs) | Topic the agent monitors. `Title`, `Description`, `SeedArguments[]`, `PreferredSources[]`. |
| [`InsightNewsItem`](../MorWalPizVideo.Models/Models/InsightNewsItem.cs) | A scanned article: `TopicId`, `Title`, `Summary`, `SourceUrl`, `SourceName`, `Status` (`InsightNewsStatus`), `StarRating`, `AIRelevanceScore`, `DiscoveredAt`. |
| [`InsightContentPlan`](../MorWalPizVideo.Models/Models/InsightContentPlan.cs) | Generated plan: `TopicId`, `Title`, `Type` (`ContentPlanType`), `Outline`, `GeneratedFromNewsItemIds[]`, `TargetPlatforms[]`, `GeneratedAt`. |

### 4.7 Identity, auth & security
| Model | Role |
| ----- | ---- |
| [`User`](../MorWalPizVideo.Models/Models/User.cs) | Admin user — `Username`, `Email`, `PasswordHash`, `Salt`, `LastLogin`, `IsActive`, `Role`, `CanAccessBackoffice`, `PushSubscriptions[]`. |
| `PushSubscriptionInfo` (in `User.cs`) | Web Push subscription (`Endpoint`, `P256dh`, `Auth`, `CreatedAt`). |
| [`LoginAttempt`](../MorWalPizVideo.Models/Models/LoginAttempt.cs) | Audit row: `IpAddress`, `Username`, `IsSuccessful`, `AttemptTime`, `UserAgent`, `FailureReason`. |
| [`ApiKey`](../MorWalPizVideo.Models/Models/ApiKey.cs) | Service key — `Key` (SHA-256 hash), `Name`, `Description`, `IsActive`, `LastUsedAt`, `ExpiresAt`, `RateLimitPerMinute`, `AllowedIpAddresses[]`. |

### 4.8 Shooting ITA vertical
| Model | Role |
| ----- | ---- |
| [`Competition`](../MorWalPizVideo.Models/Models/Competition.cs) | Top-level competition (`Name`, dates, `OrganizerId`, `Status` enum, `Type` enum, `MaxParticipants`, `RegistrationDeadline`, `Rules`, `Stages[]`, `ImageUrl`, `WebsiteUrl`). |
| `Stage` (same file) | Stage within a competition (`StageNumber`, `TargetCount`, `RoundCount`, `MinScore`, `MaxScore`, `TimeLimitSeconds`, `Briefing`, `Order`, `Images[]`, `Evaluations[]`, `Stats`). |
| `StageEvaluation` (same file) | Public rating: `UserId`, `Username`, `Rating`, `Comment`, `CreatedAt`. |
| `StageStats` (same file) | Aggregate: `AverageRating`, `TotalReviews`. |
| [`UserChannel`](../MorWalPizVideo.Models/Models/UserChannel.cs) | Link between an admin user and a competition channel (`UserId`, `ChannelId`, `IsActive`). |
| [`UserRequest`](../MorWalPizVideo.Models/Models/UserRequest.cs) | Public feature/idea request: `Name`, `Email`, `Topic`, `Description`, `Status`, `AdminNote`, `Votes`. |

### 4.9 Channels & creator tracking
| Model | Role |
| ----- | ---- |
| [`YTChannel`](../MorWalPizVideo.Models/Models/YTChannel.cs) | Tracked YouTube channel with `Videos[]` + `ShortLinks[]`. |
| `YouTubeVideo` (same file) | Snapshot used by scraper: `VideoId`, `Title`, `LastCommentDate`, `VideoIdeas[]`. |
| `VideoIdea` (same file) | AI-extracted idea from comments: `Idea`, `CommentExcerpt`, `CreationDate`, `Sentiment`. |
| [`Competition` enums](../MorWalPizVideo.Models/Models/Competition.cs) | `CompetitionType`, `CompetitionStatus`. |

---

## 5. Background Jobs (Hangfire)

Registered in `Program.cs` only when `EnableHangFire = true`. Dashboard at `/hangfire`.

| Job | File | Schedule | What it does | What it could do later |
| --- | ---- | -------- | ------------ | ---------------------- |
| `news-job` | [Jobs/NewsJobs.cs](Jobs/NewsJobs.cs) | `0 18 * * 0` (Sunday 18:00) | **Stub.** Intended to roll up the week's shorts + videos, compose a CMS page, populate `VideoReelIds`/`ShortReelIds`, and broadcast a Telegram + Discord post. | Implement the actual pipeline; persist a `Page`; emit via `ITelegramService`/`IDiscordService`. |
| `youtube-sync-job` | [Jobs/YouTubeSyncJob.cs](Jobs/YouTubeSyncJob.cs) | `0 3 * * *` (daily 03:00 UTC, overridable via `YouTubeSyncCron`) | Iterates every `YouTubeContent`, refreshes title/description/thumbnail/stats from the YouTube Data API via `IExternalDataService.FetchMatches()`. | Add per-content quota tracking; partial sync filters; failure notifications. |

---

## 6. Services Layer

Under [Services/](Services/) — registered in `Program.cs`. Most have a matching `*Mock` implementation in `MorWalPizVideo.Domain` used when `EnableMock` is on.

| Service | Purpose |
| ------- | ------- |
| `IJwtService` / `JwtService` | Token issuance & validation (HS256, settings under `JwtSettings`). |
| `IRateLimitingService` / `RateLimitingService` | Per-IP login rate limiting (config: `SecuritySettings.MaxLoginAttempts`, `LockoutDurationMinutes`). |
| `IApiKeyService` / `ApiKeyService` | Hash/verify/generate API keys; manage CRUD via repository. |
| `IApiKeyRateLimitingService` / `ApiKeyRateLimitingService` | Sliding-window per-key rate limiter (`ConcurrentDictionary`). |
| `IDiscordService` / `DiscordService` | Discord posting (uses `IDiscordHttpClientFactory` + `IDiscordConfigurationService`). |
| `ITelegramService` / `TelegramService` | Telegram posting (twin of Discord). |
| `IFacebookService` / `FacebookService` | Facebook page posting (auto-falls back to `FacebookServiceMock` if `FacebookSettings.PageId` is blank). |
| `IImageGenerationService` / `ImageGenerationService` | PNG generation (text-on-image, QR composition). |
| `IInsightAgentService` / `InsightAgentService` | AI agent for the Insights pipeline (Azure OpenAI via Semantic Kernel). |
| `ICrossApiService` / `CrossApiService` | Calls the named HttpClient `MorWalPiz` (internal API base = `SiteUrl + "api/"`). |
| `HealthCheckService` | Programmatic health-check registration (see [HEALTH_CHECKS.md](HEALTH_CHECKS.md)). |
| Factories (`IDiscordHttpClientFactory`, `ITelegramHttpClientFactory`) | Build HttpClient instances **lazily** so missing tokens don't block startup. |

### Repositories (provided by `MorWalPizVideo.Domain`)
Mock vs MongoDB registration is performed in `Program.cs` based on `EnableMock`. Each entity has its own `IxxxRepository` + `xxxRepository` + `xxxMockRepository` triad — e.g. `IProductRepository`, `IYouTubeContentRepository`, `IApiKeyRepository`, `IUserRequestRepository`, etc. Mock implementations seed from [Data/*.json](Data/).

---

## 7. Running Locally

```pwsh
# Restore + build the whole solution
dotnet build MorWalPizVideo.sln

# Run only the BackOffice API
dotnet run --project MorWalPizVideo.BackOffice

# Run via Aspire AppHost (recommended — wires service defaults & dependencies)
dotnet run --project MorWalPizVideo.AppHost
```

### Recommended dev configuration
Enable mocks + swagger to avoid needing MongoDB and external credentials:
```json
{
  "FeatureManagement": {
    "EnableMock": true,
    "EnableSwagger": true,
    "EnableHangFire": false,
    "EnableKeyVault": false,
    "EnableCors": false
  }
}
```

### Tests
```pwsh
dotnet test MorWalPizVideo.BackOffice.Tests\MorWalPizVideo.BackOffice.Tests.csproj
```
Uses Reqnroll (SpecFlow successor) with `Features/` + `StepDefinitions/` + `Infrastructure/` (custom `WebApplicationFactory`).

---

## 8. Roadmap — "What Can Still Be Built"

Items collected from `memory-bank/progress.md`, in-code TODOs, and stub jobs:

### Functional
- **Finish `NewsJobs.ExecuteAsync`** — currently logs only. Needs: weekly content aggregation → `Page` creation → Telegram/Discord publishing.
- **Products & Sponsors admin UI** — backend complete, SPA wiring in progress (see Progress doc).
- **Bulk operations UI** — extend `VideosController` batch endpoints with progress/streaming responses.
- **Refresh tokens / shorter JWT expiry** — currently single 24 h JWT (see [docs/AUTHENTICATION_SECURITY_IMPROVEMENTS.md §Future](../docs/AUTHENTICATION_SECURITY_IMPROVEMENTS.md)).
- **Argon2id password hashing** for new users (currently PBKDF2-SHA256 100k).
- **Per-key quota analytics** on top of `IApiKeyRateLimitingService` (persist usage to MongoDB).
- **Configurable cron for `news-job`** (mirror what `youtube-sync-job` does with `YouTubeSyncCron`).
- **Migrate `Product` → `DigitalProduct`** and retire the legacy `ProductsController`.

### Operational
- **Wire `VITE_VAPID_PUBLIC_KEY`** in SPA `.env` for Web Push.
- **Comprehensive API docs** — Swagger is enabled but lacks XML comments on most controllers; add `<GenerateDocumentationFile>` and `IncludeXmlComments(...)`.
- **CSP headers + tighter CORS** in production.
- **Test coverage uplift** from ~70 % → 85 % (target tracked in `memory-bank/progress.md`).
- **Database query optimization** + indexes audit on MongoDB collections.
- **Monitor failed login attempts** (`LoginAttempt` data is captured — surface alerts).

### Architectural
- Externalize the mock dataset into a fixture project so seeds aren't shipped with the API binary.
- Split the Shooting ITA vertical into its own ASP.NET project once the surface grows.
- Replace `[ApiKeyAuth]` polling with proper ASP.NET `AuthorizationPolicy` (already partially done — handler exists, just needs uniform `[Authorize(AuthenticationSchemes = "ApiKey")]`).

---

## 9. Appendix — Files Cheat Sheet

| Need to… | Look at |
| -------- | ------- |
| Add a new endpoint | Create a controller in [Controllers/](Controllers/) extending `ApplicationControllerBase`. |
| Add a new entity | Add POCO in `MorWalPizVideo.Models/Models/`, define `IxxxRepository` + Mongo + Mock impls in `MorWalPizVideo.Domain`, register both in `Program.cs`. |
| Add a recurring job | Drop a class in [Jobs/](Jobs/) and wire `RecurringJob.AddOrUpdate<T>(...)` in `Program.cs` inside the `enableHangFire` block. |
| Protect with an API key | Decorate the action or controller with `[ApiKeyAuth]`. |
| Allow anonymous access | Add `[AllowAnonymous]` on the action (the base class enforces `[Authorize]`). |
| Add a health probe | Extend `HealthCheckService.ConfigureHealthChecks` and tag with `live` / `ready` / `startup`. |
| Add a feature flag | Add to `FeatureManagement` in appsettings and the `MyFeatureFlags` constants. |
