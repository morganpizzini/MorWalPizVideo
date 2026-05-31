# Contract: HttpClient Lifetime in `MorWalPizVideo.BackOffice`

**Feature**: `001-cache-invalidation-fixes`
**Owner**: `MorWalPizVideo.BackOffice` (process-wide)
**Status**: Coding convention enforced by audit step.

---

## Rules

1. **Single source of HttpClient**: every outbound `HttpClient` in the BackOffice MUST be obtained via `IHttpClientFactory.CreateClient(string name)`.
   - **Forbidden**: `new HttpClient()`, `new HttpClient(handler)`, `new HttpClient(handler, disposeHandler: ...)`.
2. **No `using` on factory clients**: code obtaining a client from the factory MUST NOT wrap it in `using`/`await using`.
3. **No `IDisposable` for the sole purpose of disposing factory clients**: services that depend on `IHttpClientFactory` MUST NOT implement `IDisposable` solely to release factory-issued clients.
4. **Named client registration**: every distinct outbound base URL MUST have a named registration in `Program.cs` (e.g., `HttpClientNames.MorWalPiz`, `HttpClientNames.YouTube`, `HttpClientNames.Facebook`, `HttpClientNames.Pinterest` (NEW)).

---

## Audit

The implementation MUST add a CI-grade check (script or test) that fails when either of the following regex matches occurs anywhere under `MorWalPizVideo.BackOffice/`:

- `new HttpClient\(`
- `using\s+var\s+\w+\s*=\s*[^;]*\.CreateClient\(`

Manual one-time audit at implementation time uses `grep_search` with the same patterns to verify zero matches before merge.

---

## Migration mapping (informational)

| File | Before | After |
|------|--------|-------|
| `Services/CrossApiService.cs` | `using var client = this.client.CreateClient(...)` | `var client = _factory.CreateClient(...)` |
| `Controllers/PinterestController.cs` | `new HttpClient()` | inject `IHttpClientFactory`, `CreateClient(HttpClientNames.Pinterest)` |
| `Services/TelegramService.cs` | implements `IDisposable`, disposes `_httpClient` | remove `IDisposable` + `Dispose()` |
| `Services/DiscordService.cs` | implements `IDisposable`, disposes `_httpClient` | remove `IDisposable` + `Dispose()` |
| `Services/FacebookService.cs` | implements `IDisposable`, disposes `_httpClient` | remove `IDisposable` + `Dispose()` |

A new constant `HttpClientNames.Pinterest` is added to `MorWalPizVideo.Models.Constraints.HttpClientNames` (or equivalent existing location) and registered in `Program.cs` with the Pinterest API base URL currently hard-coded in `PinterestController`.
