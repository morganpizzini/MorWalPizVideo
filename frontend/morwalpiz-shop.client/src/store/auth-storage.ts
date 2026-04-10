/**
 * Authentication Session Storage
 * 
 * Manages customer authentication session data in localStorage.
 * Sessions expire after 24 hours.
 */

const AUTH_STORAGE_KEY = 'morwalpiz_shop_auth';

export interface AuthSession {
  customerId: string;
  email: string;
  sessionToken: string;
  expiresAt: number;
}

/**
 * Save authentication session to localStorage
 * Automatically sets expiration to 24 hours from now
 */
export function saveAuthSession(session: Omit<AuthSession, 'expiresAt'>): void {
  const sessionWithExpiry: AuthSession = {
    ...session,
    expiresAt: Date.now() + (24 * 60 * 60 * 1000), // 24 hours
  };
  localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(sessionWithExpiry));
}

/**
 * Retrieve authentication session from localStorage
 * Returns null if session doesn't exist or has expired
 */
export function getAuthSession(): AuthSession | null {
  const stored = localStorage.getItem(AUTH_STORAGE_KEY);
  if (!stored) return null;

  try {
    const session = JSON.parse(stored) as AuthSession;

    // Check expiration
    if (session.expiresAt < Date.now()) {
      clearAuthSession();
      return null;
    }

    return session;
  } catch (error) {
    console.error('Failed to parse auth session:', error);
    clearAuthSession();
    return null;
  }
}

/**
 * Clear authentication session from localStorage
 */
export function clearAuthSession(): void {
  localStorage.removeItem(AUTH_STORAGE_KEY);
}

/**
 * Check if user is currently authenticated
 */
export function isAuthenticated(): boolean {
  return getAuthSession() !== null;
}

/**
 * Get the current session token for API requests
 * Returns null if no valid session exists
 */
export function getSessionToken(): string | null {
  const session = getAuthSession();
  return session?.sessionToken ?? null;
}

/**
 * Get the current authenticated customer's email
 * Returns null if no valid session exists
 */
export function getCurrentUserEmail(): string | null {
  const session = getAuthSession();
  return session?.email ?? null;
}