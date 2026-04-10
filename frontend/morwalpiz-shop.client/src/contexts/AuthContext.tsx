/**
 * Authentication Context
 * 
 * Provides authentication state and methods throughout the application.
 * Manages customer login/logout and session persistence.
 */

import { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import {
  saveAuthSession,
  getAuthSession,
  clearAuthSession,
  isAuthenticated as checkIsAuthenticated,
  type AuthSession,
} from '../store/auth-storage';

interface AuthContextType {
  session: AuthSession | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (session: Omit<AuthSession, 'expiresAt'>) => void;
  logout: () => void;
  refreshSession: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

interface AuthProviderProps {
  children: ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [session, setSession] = useState<AuthSession | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // Load session on mount
  useEffect(() => {
    const loadSession = () => {
      try {
        const storedSession = getAuthSession();
        setSession(storedSession);
      } catch (error) {
        console.error('Failed to load auth session:', error);
        setSession(null);
      } finally {
        setIsLoading(false);
      }
    };

    loadSession();
  }, []);

  // Check session expiration periodically
  useEffect(() => {
    if (!session) return;

    const checkExpiration = () => {
      if (!checkIsAuthenticated()) {
        setSession(null);
      }
    };

    // Check every minute
    const interval = setInterval(checkExpiration, 60000);

    return () => clearInterval(interval);
  }, [session]);

  const login = (newSession: Omit<AuthSession, 'expiresAt'>) => {
    saveAuthSession(newSession);
    const fullSession = getAuthSession();
    setSession(fullSession);
  };

  const logout = () => {
    clearAuthSession();
    setSession(null);
  };

  const refreshSession = () => {
    const currentSession = getAuthSession();
    setSession(currentSession);
  };

  const value: AuthContextType = {
    session,
    isAuthenticated: !!session,
    isLoading,
    login,
    logout,
    refreshSession,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

/**
 * Hook to access authentication context
 * Must be used within AuthProvider
 */
export function useAuth(): AuthContextType {
  const context = useContext(AuthContext);

  if (context === undefined) {
    throw new Error('useAuth must be used within AuthProvider');
  }

  return context;
}

/**
 * Hook to get current authenticated user email
 */
export function useCurrentUser(): string | null {
  const { session } = useAuth();
  return session?.email ?? null;
}

/**
 * Hook to check if user is authenticated
 */
export function useIsAuthenticated(): boolean {
  const { isAuthenticated } = useAuth();
  return isAuthenticated;
}

/**
 * Hook to get session token for API calls
 */
export function useSessionToken(): string | null {
  const { session } = useAuth();
  return session?.sessionToken ?? null;
}