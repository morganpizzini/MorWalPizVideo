import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import {
  getPublicAccountSession,
  getPublicAccountErrorMessage,
  loginPublicAccount,
  logoutPublicAccount,
} from "@morwalpizvideo/services";
import type { PublicAccountLoginRequest, PublicAccountSession } from "@morwalpizvideo/models";

interface AuthContextValue {
  session: PublicAccountSession;
  loading: boolean;
  error: string | null;
  refresh: () => Promise<boolean>;
  login: (request: PublicAccountLoginRequest) => Promise<boolean>;
  logout: () => Promise<void>;
}

const anonymous: PublicAccountSession = { authenticated: false };
const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState(anonymous);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refresh = async () => {
    try {
      const next = await getPublicAccountSession();
      setSession(next);
      setError(null);
      return next.authenticated;
    } catch (requestError) {
      setSession(anonymous);
      setError(getPublicAccountErrorMessage(requestError));
      return false;
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void refresh();
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      session,
      loading,
      error,
      refresh,
      login: async (request) => {
        try {
          await loginPublicAccount(request);
          return await refresh();
        } catch (requestError) {
          setError(getPublicAccountErrorMessage(requestError));
          return false;
        }
      },
      logout: async () => {
        await logoutPublicAccount();
        setSession(anonymous);
      },
    }),
    [error, loading, session],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useWorkshopAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useWorkshopAuth must be used within AuthProvider");
  return context;
}
