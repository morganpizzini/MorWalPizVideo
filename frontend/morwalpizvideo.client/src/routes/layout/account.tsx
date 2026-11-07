import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import {
  getPublicAccountErrorMessage,
  getPublicAccountSession,
  logoutPublicAccount,
} from '@morwalpizvideo/services';
import type { PublicAccountSession } from '@morwalpizvideo/models';

interface PublicAccountContextValue {
  session: PublicAccountSession;
  loading: boolean;
  sessionError: string | null;
  refresh: () => Promise<boolean>;
  logout: () => Promise<void>;
}

const anonymousSession: PublicAccountSession = { authenticated: false };
const PublicAccountContext = createContext<PublicAccountContextValue | null>(null);

export function PublicAccountProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<PublicAccountSession>(anonymousSession);
  const [loading, setLoading] = useState(true);
  const [sessionError, setSessionError] = useState<string | null>(null);

  const refresh = async () => {
    try {
      const nextSession = await getPublicAccountSession();
      setSession(nextSession);
      setSessionError(null);
      return nextSession.authenticated;
    } catch (error) {
      setSession(anonymousSession);
      setSessionError(getPublicAccountErrorMessage(error));
      return false;
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void refresh();
  }, []);

  const logout = async () => {
    await logoutPublicAccount();
    setSession(anonymousSession);
    setSessionError(null);
  };

  return (
    <PublicAccountContext.Provider value={{ session, loading, sessionError, refresh, logout }}>
      {children}
    </PublicAccountContext.Provider>
  );
}

export function usePublicAccount(): PublicAccountContextValue {
  const context = useContext(PublicAccountContext);
  if (!context) throw new Error('usePublicAccount must be used within PublicAccountProvider');
  return context;
}
