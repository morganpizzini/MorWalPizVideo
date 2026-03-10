# API Key Authentication System

## Overview

This document describes the API Key authentication system implemented for the MorWalPiz Video BackOffice API. The system enables secure communication between the VideoImporter WinForms application and the BackOffice ChatController endpoints.

## Architecture

### Components

1. **ApiKey Model** (`MorWalPizVideo.Models/Models/ApiKey.cs`)
   - Stores API key information with hashed keys (SHA256)
   - Supports rate limiting, IP whitelisting, and expiration
   - Tracks usage statistics (last used timestamp)

2. **ApiKey Repository** (`MorWalPizVideo.Domain/Interfaces/`)
   - `IApiKeyRepository` - Interface
   - `ApiKeyRepository` - MongoDB implementation
   - `ApiKeyMockRepository` - In-memory mock implementation

3. **Services** (`MorWalPizVideo.BackOffice/Services/`)
   - `IApiKeyService` / `ApiKeyService` - API key generation, validation, and management
   - `IApiKeyRateLimitingService` / `ApiKeyRateLimitingService` - Rate limiting enforcement

4. **Authentication Handler** (`MorWalPizVideo.BackOffice/Authentication/`)
   - `ApiKeyAuthenticationHandler` - ASP.NET Core authentication handler
   - `ApiKeyAuthAttribute` - Controller/action attribute for easy application

5. **Management Controller** (`MorWalPizVideo.BackOffice/Controllers/ApiKeysController.cs`)
   - CRUD operations for API keys (requires JWT authentication)
   - Endpoints for creating, listing, updating, toggling, regenerating, and deleting keys

## Features

### Security Features

- **Hashed Storage**: API keys are hashed using SHA256 before storage
- **One-Time Display**: Unhashed keys are only shown once upon creation
- **Rate Limiting**: Configurable per-key or default rate limits (requests per minute)
- **IP Whitelisting**: Optional IP address restrictions per key
- **Expiration**: Optional expiration dates for time-limited access
- **Active/Inactive States**: Keys can be deactivated without deletion

### Rate Limiting

- In-memory implementation using ConcurrentDictionary
- Sliding window of 60 seconds
- Configurable per API key
- Default limit: 60 requests per minute
- Returns HTTP 429 (Too Many Requests) when exceeded
- Includes `Retry-After` header (60 seconds)

### IP Whitelisting

- Optional feature (disabled by default)
- Supports multiple IP addresses per key
- Checks X-Forwarded-For and X-Real-IP headers for proxy support
- Returns HTTP 401 when IP is not whitelisted

## Configuration

### BackOffice API (appsettings.json)

```json
{
  "ApiKeySettings": {
    "HeaderName": "X-API-Key",
    "DefaultRateLimitPerMinute": 60,
    "EnableIpWhitelist": false
  }
}
```

### VideoImporter Application (app.config)

```xml
<configuration>
  <appSettings>
    <add key="ApiKey" value="YOUR_API_KEY_HERE" />
    <add key="BaseUrl" value="https://your-backoffice-api.com/" />
  </appSettings>
</configuration>
```

## Usage

### Creating an API Key

1. **Authenticate** with JWT to access the ApiKeys management endpoints
2. **POST** to `/api/apikeys` with the following payload:

```json
{
  "name": "VideoImporter-Production",
  "description": "API key for VideoImporter application",
  "rateLimitPerMinute": 100,
  "allowedIpAddresses": ["192.168.1.100"],
  "expiresAt": "2026-12-31T23:59:59Z"
}
```

3. **Save the returned key** - it will not be shown again:

```json
{
  "id": "507f1f77bcf86cd799439011",
  "name": "VideoImporter-Production",
  "key": "MWPV_a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0u1v2w3x4y5z6",
  "message": "IMPORTANT: Save this key securely. It will not be shown again.",
  ...
}
```

### Using an API Key (Client-Side)

#### C# HttpClient

```csharp
var apiService = new ApiService(
    apiEndpoint: "https://your-api.com/",
    apiKey: "MWPV_your_api_key_here"
);

// The ApiService automatically adds the X-API-Key header
var result = await apiService.SendVideosContextAsync(videoNames, context, languages);
```

#### Manual HTTP Request

```csharp
var client = new HttpClient();
client.DefaultRequestHeaders.Add("X-API-Key", "MWPV_your_api_key_here");
var response = await client.PostAsJsonAsync("https://your-api.com/api/chat", requestData);
```

### Protecting Endpoints

Add the `[ApiKeyAuth]` attribute to controllers or actions:

```csharp
[ApiKeyAuth]
public class ChatController : ApplicationControllerBase
{
    // All endpoints in this controller require API key authentication
}
```

Or apply to individual actions:

```csharp
public class MyController : ControllerBase
{
    [ApiKeyAuth]
    public IActionResult ProtectedEndpoint()
    {
        // Requires API key
    }

    public IActionResult PublicEndpoint()
    {
        // No authentication required
    }
}
```

## API Key Management Endpoints

All management endpoints require JWT authentication.

### Create API Key
```
POST /api/apikeys
Body: { "name": "...", "description": "...", ... }
```

### List All API Keys
```
GET /api/apikeys
```

### Get API Key Details
```
GET /api/apikeys/{id}
```

### Update API Key
```
PUT /api/apikeys/{id}
Body: { "name": "...", "rateLimitPerMinute": 100, ... }
```

### Toggle API Key (Activate/Deactivate)
```
POST /api/apikeys/{id}/toggle
```

### Regenerate API Key
```
POST /api/apikeys/{id}/regenerate
Returns: { "key": "NEW_KEY_HERE", ... }
```

### Delete API Key
```
DELETE /api/apikeys/{id}
```

## Security Best Practices

### For Administrators

1. **Generate Unique Keys**: Create separate keys for different applications/environments
2. **Use Descriptive Names**: Include application name and environment (e.g., "VideoImporter-Production")
3. **Set Appropriate Rate Limits**: Balance between functionality and DoS protection
4. **Use IP Whitelisting**: When clients have static IPs (production environments)
5. **Set Expiration Dates**: For temporary access or testing keys
6. **Monitor Usage**: Check LastUsedAt timestamps to identify unused keys
7. **Rotate Keys Regularly**: Use the regenerate endpoint to rotate keys periodically
8. **Revoke Immediately**: Deactivate or delete compromised keys immediately

### For Developers

1. **Never Commit Keys**: Keep API keys out of source control
2. **Use Configuration Files**: Store keys in app.config, appsettings.json, or environment variables
3. **Secure Storage**: In production, use Azure Key Vault or similar secret management
4. **Handle Errors**: Implement proper error handling for 401 (Unauthorized) and 429 (Rate Limit)
5. **Retry Logic**: Implement exponential backoff for rate limit errors
6. **HTTPS Only**: Always use HTTPS in production
7. **Log Securely**: Never log the actual API key value

## Error Responses

### 401 Unauthorized - Missing API M Key
```json
{
  "error": "Unauthorized",
  "message": "Valid API key required"
}
```

### 401 Unauthorized - Invalid Key
```json
{
  "error": "Unauthorized",
  "message": "Invalid or expired API Key"
}
```

### 401 Unauthorized - IP Not Whitelisted
```json
{
  "error": "Unauthorized",
  "message": "API Key not authorized from this IP address"
}
```

### 429 Too Many Requests - Rate Limit Exceeded
```json
{
  "error": "Too Many Requests",
  "message": "Rate limit exceeded"
}
```
Headers: `Retry-After: 60`

## Database Schema

### MongoDB Collection: `ApiKeys`

```json
{
  "_id": ObjectId("..."),
  "Name": "VideoImporter-Production",
  "Description": "Production API key for VideoImporter",
  "Key": "hashed_key_sha256...",
  "IsActive": true,
  "RateLimitPerMinute": 100,
  "AllowedIpAddresses": ["192.168.1.100"],
  "LastUsedAt": ISODate("2026-02-13T16:30:00Z"),
  "ExpiresAt": ISODate("2026-12-31T23:59:59Z"),
  "CreationDateTime": ISODate("2026-02-13T10:00:00Z")
}
```

## Testing

### Mock Mode

When `EnableMock` feature flag is true, the system uses `ApiKeyMockRepository` with in-memory storage from `MorWalPizVideo.BackOffice/Data/apiKeys.json`.

### Testing Authentication

1. Create a test API key via the management endpoint
2. Use the key in the `X-API-Key` header
3. Verify successful authentication
4. Test rate limiting by making rapid requests
5. Test IP whitelisting (if enabled)
6. Test with deactivated keys
7. Test with expired keys

## Monitoring and Logging

The system logs the following events:

- API key creation (with key name)
- API key updates
- API key regeneration
- API key deletion
- Authentication success (with key name)
- Authentication failures (invalid key, IP restriction, etc.)
- Rate limit violations

Log entries include the authenticated username or key name for audit trails.

## Migration Guide

### Updating Existing VideoImporter Instances

1. Update the VideoImporter application code to the latest version
2. Generate an API key through the BackOffice management interface
3. Add the API key to the VideoImporter's app.config
4. Test the connection
5. Deploy the updated configuration

### From No Authentication to API Key Authentication

Since the ChatController previously had no authentication, this is a breaking change. All existing VideoImporter instances must be updated with valid API keys before they can continue accessing the API.

## Troubleshooting

### "Missing X-API-Key header"
- Ensure the API key is being sent in the HTTP request headers
- Verify the header name matches the configuration (default: `X-API-Key`)

### "Invalid or expired API Key"
- Check that the API key is still active
- Verify the expiration date hasn't passed
- Ensure the correct key is being used

### "Rate limit exceeded"
- Wait 60 seconds before retrying
- Implement exponential backoff
- Request a higher rate limit if legitimate usage exceeds current limit

### "API Key not authorized from this IP address"
- Verify the client's IP address
- Check if using a proxy (X-Forwarded-For header)
- Update the  allowed IP addresses list for the key

## Future Enhancements

Potential improvements for future versions:

- Scoped permissions (different keys for different endpoints)
- Usage analytics and reporting
- Automatic key rotation
- Multiple authentication schemes (API Key OR JWT)
- Key usage quotas (monthly limits)
- Webhook notifications for security events
- Admin dashboard for key management