# Authentication Security Improvements

This document outlines the security enhancements implemented for the MorWalPizVideo back-office authentication system.

## Overview

The authentication system has been upgraded with three priority levels of security improvements to protect against common web vulnerabilities and follow industry best practices.

## Implemented Improvements

### Priority 1: HttpOnly Cookie-Based Authentication

**Problem**: Storing JWT tokens in `localStorage` makes them vulnerable to XSS (Cross-Site Scripting) attacks. Any malicious JavaScript code injected into the page can access and exfiltrate the token.

**Solution**: Implemented HttpOnly Secure SameSite cookies for token storage.

#### Backend Changes

**File: `MorWalPizVideo.BackOffice/Controllers/AuthController.cs`**
- Login endpoint now sets an `auth_token` cookie with security flags:
  - `HttpOnly: true` - Prevents JavaScript access
  - `Secure: true` - Requires HTTPS
  - `SameSite: Strict` - Prevents CSRF attacks
  - `Expires: 24 hours` - Matches JWT expiry
- Added `/api/auth/logout` endpoint to clear the cookie
- Token still returned in response body for backward compatibility during transition

**File: `MorWalPizVideo.BackOffice/Program.cs`**
- JWT Bearer authentication now checks cookies if Authorization header is missing
- CORS configured to support credentials (`AllowCredentials()`)
- HSTS (HTTP Strict Transport Security) enabled in production
- Development CORS allows any origin with credentials

#### Frontend Changes

**File: `frontend/fe-packages/services/src/apiService.ts`**
- All API calls now include `credentials: 'include'` to send cookies

**File: `frontend/back-office-spa/src/services/authService.ts`**
- `logout()` method now calls `/api/auth/logout` endpoint
- localStorage cleanup retained as fallback

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

**File: `MorWalPizVideo.Domain/Interfaces/Repository.cs`**
- `VerifyPassword()`: Updated from 10,000 to 100,000 iterations
- `HashPassword()`: Updated from 10,000 to 100,000 iterations
- Added explicit `HashAlgorithmName.SHA256` parameter
- Both methods now use:
  ```csharp
  new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256)
  ```

## Security Benefits

### XSS Protection
- HttpOnly cookies cannot be accessed by JavaScript
- Even if XSS vulnerability exists, attacker cannot steal authentication token

### CSRF Protection
- SameSite=Strict prevents cookie from being sent in cross-site requests
- Cookies only sent when navigating directly to the application

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
1. Login endpoint returns JWT in response body (will be removed in future version)
2. Frontend still stores token in localStorage temporarily
3. Both cookie and Authorization header accepted by backend

### Testing Checklist

- [ ] Login successfully creates auth cookie
- [ ] Subsequent API calls work without Authorization header
- [ ] Logout clears the cookie
- [ ] Cookie not sent in cross-origin requests (CORS verification)
- [ ] HTTPS enforced in production
- [ ] Password verification works with new iteration count

### Future Recommendations

1. **Remove localStorage token storage**: Once cookie authentication is verified working, remove localStorage token storage completely
2. **Password migration**: Consider migrating all stored passwords to new hash parameters
3. **Consider Argon2id**: For new implementations, Argon2id is recommended over PBKDF2
4. **Implement refresh tokens**: Add refresh token mechanism with shorter JWT expiry
5. **Add CSP headers**: Implement Content Security Policy to further prevent XSS
6. **Monitor failed login attempts**: Alert on suspicious patterns

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