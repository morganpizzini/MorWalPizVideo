- BackOffice API calls use `credentials: 'include'` to send cookies and enable the explicit cookie-only client mode
- The BackOffice SPA no longer reads `localStorage.authToken` or emits browser Bearer headers. Non-browser bearer/API-key callers and public clients configured with `credentials: 'omit'` retain their existing behavior.
# Authentication Security Improvements

This document outlines the security enhancements implemented for the MorWalPizVideo back-office authentication system.

## Overview

The authentication system has been upgraded with three priority levels of security improvements to protect against common web vulnerabilities and follow industry best practices.

## Implemented Improvements

### 2026-08 Security Follow-up

- User mutations under `/api/user` are admin-only (`group:admin`) while authenticated BackOffice users retain self-service profile endpoints:
  - `GET /api/user/me`
  - `PUT /api/user/me`
  - `PUT /api/user/me/password` (requires current password validation)
- The BackOffice SPA `/rbac` screen now owns the admin user-management UI for create/update, activation, and password reset/set flows, reusing the existing cookie + CSRF client behavior.
- Admin-managed password operations are available at:
  - `PUT /api/user/{id}/password/reset`
  - `PUT /api/user/{id}/password/set` (alias with the same behavior)
- Authorization grants are group/permission-based; role claims are no longer emitted in BackOffice JWT tokens.
- Password hashing now standardizes new hashes to PBKDF2-SHA256 (100k iterations, 32-byte output) while verification remains compatible with legacy 256-byte PBKDF2 hashes.

### Priority 1: HttpOnly Cookie-Based Authentication

**Problem**: Storing JWT tokens in `localStorage` makes them vulnerable to XSS (Cross-Site Scripting) attacks. Any malicious JavaScript code injected into the page can access and exfiltrate the token.

**Solution**: Implemented an HttpOnly, Secure cookie for the browser session plus antiforgery validation for unsafe cookie-authenticated requests.

#### Backend Changes

**File: `MorWalPizVideo.BackOffice/Controllers/AuthController.cs`**
- Login endpoint now sets an `auth_token` cookie with security flags:
  - `HttpOnly: true` - Prevents JavaScript access
  - `Secure: true` - Requires HTTPS
  - `SameSite: None` - Allows the accepted HTTPS SPA origin to call the separate API origin
  - `Path: /` - Makes the session available to all BackOffice API routes
  - `Expires` - Matches the configured JWT expiration
- Added `/api/auth/logout` endpoint to clear the cookie
- Login, validate, and logout response bodies remain unchanged; the JWT is not exposed to browser JavaScript

**File: `MorWalPizVideo.BackOffice/Program.cs`**
- JWT Bearer authentication now checks cookies if Authorization header is missing
- Production CORS accepts exactly `https://morwalpiz-admin-spa.azurewebsites.net` with credentials
- ASP.NET Core antiforgery uses the `X-CSRF-TOKEN` header and a Secure, HttpOnly, `SameSite=None` `__Host-morwalpiz-csrf` cookie
- Unsafe requests carrying `auth_token`, including logout and validate, require a valid antiforgery token
- Bearer-only, API-key-only, anonymous, safe-method, and health-probe requests do not acquire a CSRF requirement
- HSTS (HTTP Strict Transport Security) enabled in production
- Development CORS allows any origin with credentials

#### Frontend Changes

**File: `frontend/fe-packages/services/src/apiService.ts`**
- All API calls now include `credentials: 'include'` to send cookies
- Unsafe cookie requests acquire `/api/auth/csrf`, cache its token, and send it as `X-CSRF-TOKEN`
- Bearer requests and public clients configured with `credentials: 'omit'` retain their existing behavior

**File: `frontend/back-office-spa/src/services/authService.ts`**
- `logout()` method now calls `/api/auth/logout` endpoint
- Cached CSRF state is reset after login, logout, and unauthorized-session handling
- `localStorage` contains display-only user information, not the JWT

### Priority 2: Remove Debug Logging

**Problem**: Console logging of authentication responses can expose sensitive data in browser DevTools.

**Solution**: Removed all `console.log()` statements containing authentication data.

#### Changes

**File: `frontend/back-office-spa/src/services/authService.ts`**
- Removed `console.log(response)` from login method (line 54)
- Removed `console.log("validate token:", response)` from validateToken method

### Priority 3: Strengthen Password Hashing

**Problem**: PBKDF2 with 10,000 iterations is below current NIST recommendations and vulnerable to modern password cracking techniques.

**Solution**: Upgraded to PBKDF2-SHA256 with 100,000 iterations.

#### Changes

**File: `MorWalPizVideo.Domain/Security/PasswordHashing.cs`**
- Uses PBKDF2-SHA256 with 100,000 iterations for all new hashes
- Verifies against the stored hash length to preserve compatibility with legacy records
- Standardized new hash output length to 32 bytes
- Maintains compatibility with legacy 256-byte PBKDF2 hashes
- Uses:
  ```csharp
  new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256)
  ```

## Security Benefits

### XSS Protection
- HttpOnly cookies cannot be accessed by JavaScript
- Even if XSS vulnerability exists, attacker cannot steal authentication token

### CSRF Protection
- Cross-site cookie transport is limited to the exact credentialed CORS origin
- Unsafe requests carrying the auth cookie require the matching server-issued antiforgery token
- Supplying a forged bearer or API-key header does not bypass cookie CSRF validation

### HTTPS Enforcement
- Secure flag ensures cookies only transmitted over HTTPS
- HSTS header forces browsers to use HTTPS

### Password Security
- 100,000 iterations significantly increases time to crack passwords
- SHA256 provides stronger cryptographic protection
- Compatible with existing password hashes (users not required to reset passwords)

## Migration Notes

### Backward Compatibility

The implementation maintains backward compatibility:
1. Login, validate, and logout response shapes are unchanged
2. `auth_token` remains the cookie name
3. JWT bearer headers and API-key clients remain supported without browser CSRF token handling
4. Anonymous endpoints and health probes retain their existing access behavior

### Testing Checklist

- [x] Login creates an HttpOnly, Secure, `SameSite=None` auth cookie
- [x] Exact production SPA origin receives credentialed CORS headers; unsupported origins do not
- [x] Missing and forged CSRF tokens fail on unsafe cookie requests
- [x] Valid CSRF permits validate and logout, and logout clears the cookie
- [x] Bearer-only and API-key-only requests remain exempt from cookie CSRF
- [ ] Verify the accepted SPA/API origins and HTTPS-only cookie flow after Azure deployment
- [ ] Password verification works with new iteration count

### Future Recommendations

1. **Password migration**: Consider migrating all stored passwords to new hash parameters
2. **Consider Argon2id**: For new implementations, Argon2id is recommended over PBKDF2
3. **Implement refresh tokens**: Add refresh token mechanism with shorter JWT expiry
4. **Add CSP headers**: Implement Content Security Policy to further prevent XSS
5. **Monitor failed login attempts**: Alert on suspicious patterns

## Configuration

### Backend (appsettings.json)

```json
{
  "JwtSettings": {
    "Secret": "your-super-secret-key-with-at-least-32-characters",
    "Issuer": "MorWalPizVideo.BackOffice",
    "Audience": "MorWalPizVideo.BackOffice",
    "ExpiryHours": 24
  },
  "SecuritySettings": {
    "MaxLoginAttempts": 5,
    "LockoutDurationMinutes": 15
  }
}
```

### Production CORS

In production, CORS is restricted to specific origins with credentials support:
```csharp
builder.WithOrigins("https://morwalpiz-admin-spa.azurewebsites.net")
       .AllowAnyMethod()
       .AllowAnyHeader()
       .AllowCredentials();
```

## References

- [OWASP Authentication Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html)
- [NIST Password Guidelines](https://pages.nist.gov/800-63-3/sp800-63b.html)
- [OWASP Session Management](https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html)

## Change Log

**Date**: 2026-04-09

**Changes**:
- Implemented HttpOnly cookie authentication
- Removed debug logging
- Upgraded password hashing to 100,000 iterations with SHA256
- Added HSTS support
- Updated CORS for credentials
- Added logout endpoint

**Impact**: Significantly improved security posture with minimal breaking changes.