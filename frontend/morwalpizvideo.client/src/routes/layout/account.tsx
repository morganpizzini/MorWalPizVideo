import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import { getPublicAccountSession, logoutPublicAccount } from '@morwalpizvideo/services';
import type { PublicAccountSession } from '@morwalpizvideo/models';

interface PublicAccountContextValue {
  session: PublicAccountSession;
  loading: boolean;
  refresh: () => Promise<void>;
  logout: () => Promise<void>;
}

const anonymousSession: PublicAccountSession = { authenticated: false };
const PublicAccountContext = createContext<PublicAccountContextValue | null>(null);

export function PublicAccountProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<PublicAccountSession>(anonymousSession);
  const [loading, setLoading] = useState(true);

  const refresh = async () => {
    try {
      setSession(await getPublicAccountSession());
    } catch {
      setSession(anonymousSession);
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
  };

  return (
    <PublicAccountContext.Provider value={{ session, loading, refresh, logout }}>
      {children}
    </PublicAccountContext.Provider>
  );
}

export function usePublicAccount(): PublicAccountContextValue {
  const context = useContext(PublicAccountContext);
  if (!context) throw new Error('usePublicAccount must be used within PublicAccountProvider');
  return context;
}
