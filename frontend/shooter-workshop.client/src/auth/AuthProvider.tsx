import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import {
  ApiResponseError,
  getPublicAccountSession,
  getPublicAccountErrorMessage,
  loginPublicAccount,
  logoutPublicAccount,
} from "@morwalpizvideo/services";
import type { PublicAccountLoginRequest, PublicAccountSession } from "@morwalpizvideo/models";
import {
  clearAllOfflineData,
  clearOfflineSignedOutMarker,
  getRememberedOfflineAccount,
  isNetworkFailure,
  isOfflineSignedOut,
  rememberOfflineAccount,
} from "../services/offlineStore";

interface AuthContextValue {
  session: PublicAccountSession;
  loading: boolean;
  offline: boolean;
  error: string | null;
  refresh: () => Promise<boolean>;
  login: (request: PublicAccountLoginRequest) => Promise<boolean>;
  logout: () => Promise<void>;
  updateSession: (session: PublicAccountSession) => void;
}

const anonymous: PublicAccountSession = { authenticated: false };
const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState(anonymous);
  const [loading, setLoading] = useState(true);
  const [offline, setOffline] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const refresh = async () => {
    try {
      const next = await getPublicAccountSession();
      if (isOfflineSignedOut()) {
        if (next.authenticated) {
          try {
            await logoutPublicAccount();
            await clearOfflineSignedOutMarker();
          } catch (logoutError) {
            if (logoutError instanceof ApiResponseError && logoutError.status === 401) {
              await clearOfflineSignedOutMarker();
            } else {
              console.error("Workshop deferred logout could not be completed.", logoutError);
            }
          }
        } else {
          await clearOfflineSignedOutMarker();
        }
        setSession(anonymous);
        setOffline(false);
        setError(null);
        return false;
      }
      if (next.authenticated && next.account) {
        try {
          await rememberOfflineAccount(next.account);
        } catch (storageError) {
          console.error("Workshop account could not be saved for offline use.", storageError);
        }
      }
      setSession(next);
      setOffline(false);
      setError(null);
      return next.authenticated;
    } catch (requestError) {
      if (isNetworkFailure(requestError) && !isOfflineSignedOut()) {
        try {
          const account = await getRememberedOfflineAccount();
          if (account) {
            setSession({ authenticated: true, account });
            setOffline(true);
            setError(null);
            return true;
          }
        } catch (storageError) {
          console.error("Workshop offline session could not be restored.", storageError);
        }
      }
      setSession(anonymous);
      setOffline(false);
      setError(getPublicAccountErrorMessage(requestError));
      return false;
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void refresh();
  }, []);
  useEffect(() => {
    const handleOnline = () => { void refresh(); };
    const handleOffline = () => {
      if (session.authenticated) setOffline(true);
    };
    window.addEventListener("online", handleOnline);
    window.addEventListener("offline", handleOffline);
    return () => {
      window.removeEventListener("online", handleOnline);
      window.removeEventListener("offline", handleOffline);
    };
  }, [session.authenticated]);

  const value = useMemo<AuthContextValue>(
    () => ({
      session,
      loading,
      offline,
      error,
      refresh,
      login: async (request) => {
        try {
          const next = await loginPublicAccount(request);
          if (next.account) {
            try {
              await rememberOfflineAccount(next.account);
            } catch (storageError) {
              console.error("Workshop account could not be saved for offline use.", storageError);
              setError("You are signed in, but this device could not be prepared for offline use.");
            }
          }
          setSession(next);
          if (!next.account) setError(null);
          setOffline(false);
          setLoading(false);
          return next.authenticated;
        } catch (requestError) {
          setError(getPublicAccountErrorMessage(requestError));
          return false;
        }
      },
      logout: async () => {
        await clearAllOfflineData();
        setSession(anonymous);
        setOffline(false);
        if (typeof navigator !== "undefined" && navigator.onLine) {
          try {
            await logoutPublicAccount();
            await clearOfflineSignedOutMarker();
          } catch (logoutError) {
            console.error("Workshop sign-out could not be confirmed by the server.", logoutError);
          }
        }
      },
      updateSession: (nextSession) => setSession(nextSession),
    }),
    [error, loading, offline, session],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useWorkshopAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useWorkshopAuth must be used within AuthProvider");
  return context;
}
