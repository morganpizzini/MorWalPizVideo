import { useCallback, useEffect, useRef, useState } from "react";
import { NavLink, Outlet, useFetcher, useLocation, useNavigate } from "react-router";
import { markWorkshopCapabilityGuideSeen } from "@morwalpizvideo/services";
import { useToast } from "@morwalpiz/layout/notifications";
import { useWorkshopAuth } from "../auth/AuthProvider";
import { LoadingState } from "../components/States";
import CapabilityWizard from "../components/CapabilityWizard";
import { LanguageSelector } from "../components/LanguageSelector";
import InstallPrompt from "../components/InstallPrompt";
import { useI18n } from "../i18n";
import { useInstallPrompt } from "../services/installPrompt";
import { AppearanceSelector } from "@morwalpiz/layout";
import { getOfflineState, hasOfflineData, subscribeToOfflineChanges } from "../services/offlineStore";
import type { action as offlineAction } from "../routes/offlineAction";

const navigation = [
  ["Dashboard", "/"], ["Equipment", "/firearms"],
  ["Ammunition", "/ammunition"], ["Inventory", "/inventory"],
  ["Recordings", "/sessions/shooting"], ["History", "/history"],
] as const;

type HeaderIcon = "globe" | "download" | "logout";

export interface WorkshopShellContext {
  offline: {
    effectivelyOffline: boolean;
    preparedAt: string | null;
    pending: number;
    busy: boolean;
    preparing: boolean;
    prepare: () => void;
    sync: () => void;
  };
}

function HeaderIcon({ name }: { name: HeaderIcon }) {
  if (name === "globe") {
    return <svg aria-hidden="true" viewBox="0 0 24 24" focusable="false"><circle cx="12" cy="12" r="8.5" /><path d="M3.8 12h16.4M12 3.5c2.2 2.3 3.3 5.1 3.3 8.5s-1.1 6.2-3.3 8.5c-2.2-2.3-3.3-5.1-3.3-8.5S9.8 5.8 12 3.5Z" /></svg>;
  }
  if (name === "download") {
    return <svg aria-hidden="true" viewBox="0 0 24 24" focusable="false"><path d="M12 3v11m0 0 4-4m-4 4-4-4M5 18.5h14" /></svg>;
  }
  return <svg aria-hidden="true" viewBox="0 0 24 24" focusable="false"><path d="M9 4.5h9v15H9M13 12H3m0 0 3.5-3.5M3 12l3.5 3.5" /></svg>;
}

export default function AppShell() {
  const { loading, session, error, logout, refresh, updateSession, offline } = useWorkshopAuth();
  const { t } = useI18n();
  const toast = useToast();
  const { canInstall, installed, reopen } = useInstallPrompt();
  const navigate = useNavigate();
  const location = useLocation();
  const [wizardOpen, setWizardOpen] = useState(false);
  const [wizardError, setWizardError] = useState<string | null>(null);
  const [logoutError, setLogoutError] = useState<string | null>(null);
  const [signingOut, setSigningOut] = useState(false);
  const [offlineState, setOfflineState] = useState({ pending: 0, blocked: 0, preparedAt: null as string | null, storagePersisted: false, commands: [] as Awaited<ReturnType<typeof getOfflineState>>["commands"] });
  const [browserOnline, setBrowserOnline] = useState(() => navigator.onLine);
  const effectivelyOffline = offline || !browserOnline;
  const offlineFetcher = useFetcher<typeof offlineAction>();
  const syncAttempted = useRef(false);
  const authRefreshAttempted = useRef<unknown>(null);
  const activeOfflineIntent = useRef<"prepare" | "sync" | null>(null);
  const lastOfflineIntent = useRef<"prepare" | "sync" | null>(null);
  const preparationDataBeforeSubmit = useRef(offlineFetcher.data);
  const guideVersion = session.account?.workshopCapabilityGuideSeenVersion ?? 1;
  const handleSignOut = async () => {
    setLogoutError(null);
    setSigningOut(true);
    try {
      const hasLocalData = await hasOfflineData();
      if ((hasLocalData || effectivelyOffline) &&
          !window.confirm(t("Signing out deletes all saved offline data and pending changes. Reconnect and sync first if you want to keep them. Continue?"))) {
        return;
      }
      await logout();
      await navigate("/account/login");
    } catch (signOutError) {
      setLogoutError(signOutError instanceof Error ? signOutError.message : t("Offline data could not be deleted."));
    } finally {
      setSigningOut(false);
    }
  };
  const persistGuideSeen = useCallback(async () => {
    try {
      const result = await markWorkshopCapabilityGuideSeen(1);
      updateSession({
        ...session,
        account: session.account
          ? { ...session.account, workshopCapabilityGuideSeenVersion: result.effectiveVersion }
          : session.account,
      });
      setWizardError(null);
      setWizardOpen(false);
    } catch {
      setWizardError(t("The guide could not be saved. You can retry or close it."));
    }
  }, [session, t, updateSession]);
  useEffect(() => {
    if (!loading && session.authenticated && guideVersion < 1) {
      setWizardOpen(true);
    }
  }, [guideVersion, loading, session.authenticated]);
  useEffect(() => {
    const accountId = session.account?.id;
    if (!accountId) return;
    let active = true;
    const refreshOfflineState = () => {
      void getOfflineState(accountId).then(next => {
        if (active) setOfflineState(next);
      }).catch(storageError => {
        console.error("Workshop offline status could not be loaded.", storageError);
      });
    };
    refreshOfflineState();
    return subscribeToOfflineChanges(refreshOfflineState);
  }, [session.account?.id]);
  useEffect(() => {
    if (offlineFetcher.state !== "idle" || activeOfflineIntent.current !== "prepare" ||
        !offlineFetcher.data || offlineFetcher.data === preparationDataBeforeSubmit.current) return;
    if ("preparedAt" in offlineFetcher.data && offlineFetcher.data.ok) {
      if (offlineFetcher.data.storagePersisted) {
        toast.show(t("Offline readiness"), t("Offline data prepared."), { variant: "success" });
      } else {
        toast.show(t("Offline readiness"), t("Offline data prepared, but the browser may clear it."), {
          variant: "warning",
          autohide: false,
        });
      }
    } else if ("error" in offlineFetcher.data) {
      toast.show(t("Offline preparation failed"), offlineFetcher.data.error, {
        variant: "danger",
        autohide: false,
      });
    } else {
      toast.show(t("Offline preparation failed"), t("The request could not be completed."), {
        variant: "danger",
        autohide: false,
      });
    }
    activeOfflineIntent.current = null;
    preparationDataBeforeSubmit.current = offlineFetcher.data;
  }, [offlineFetcher.data, offlineFetcher.state, t, toast]);
  useEffect(() => {
    if (effectivelyOffline || offlineState.pending === 0 || syncAttempted.current ||
      offlineFetcher.state !== "idle") return;
    syncAttempted.current = true;
    activeOfflineIntent.current = "sync";
    lastOfflineIntent.current = "sync";
    offlineFetcher.submit({ intent: "sync" }, { method: "post", action: "/" });
  }, [effectivelyOffline, offlineFetcher, offlineState.pending]);
  useEffect(() => {
    if (offlineFetcher.state !== "idle" || !offlineFetcher.data ||
        !("authRequired" in offlineFetcher.data) || !offlineFetcher.data.authRequired ||
        authRefreshAttempted.current === offlineFetcher.data) return;
    authRefreshAttempted.current = offlineFetcher.data;
    void refresh();
  }, [offlineFetcher.data, offlineFetcher.state, refresh]);
  useEffect(() => {
    const handleOnline = () => {
      setBrowserOnline(true);
      syncAttempted.current = false;
    };
    const handleOffline = () => {
      setBrowserOnline(false);
      syncAttempted.current = false;
    };
    window.addEventListener("online", handleOnline);
    window.addEventListener("offline", handleOffline);
    return () => {
      window.removeEventListener("online", handleOnline);
      window.removeEventListener("offline", handleOffline);
    };
  }, []);
  useEffect(() => {
    if (!loading && !session.authenticated) {
      const returnUrl = `${location.pathname}${location.search}${location.hash}`;
      void navigate(`/account/login?returnUrl=${encodeURIComponent(returnUrl)}`, { replace: true });
    }
  }, [loading, location.hash, location.pathname, location.search, navigate, session.authenticated]);
  if (loading) return <LoadingState fullScreen label={t("Restoring your account session…")} />;
  if (!session.authenticated) {
    return <LoadingState fullScreen label={error ?? t("Redirecting to account login…")} />;
  }
  const prepareOffline = () => {
    preparationDataBeforeSubmit.current = offlineFetcher.data;
    activeOfflineIntent.current = "prepare";
    lastOfflineIntent.current = "prepare";
    offlineFetcher.submit({ intent: "prepare" }, { method: "post", action: "/" });
  };
  const syncOffline = () => {
    syncAttempted.current = true;
    activeOfflineIntent.current = "sync";
    lastOfflineIntent.current = "sync";
    offlineFetcher.submit({ intent: "sync" }, { method: "post", action: "/" });
  };
  const shellContext: WorkshopShellContext = {
    offline: {
      effectivelyOffline,
      preparedAt: offlineState.preparedAt,
      pending: offlineState.pending,
      busy: offlineFetcher.state !== "idle",
      preparing: offlineFetcher.state !== "idle" && activeOfflineIntent.current === "prepare",
      prepare: prepareOffline,
      sync: syncOffline,
    },
  };
  const showSyncError = offlineFetcher.state === "idle" &&
    lastOfflineIntent.current === "sync" &&
    !!offlineFetcher.data &&
    "error" in offlineFetcher.data;
  const showSyncAuthError = offlineFetcher.state === "idle" &&
    lastOfflineIntent.current === "sync" &&
    !!offlineFetcher.data &&
    "authRequired" in offlineFetcher.data &&
    offlineFetcher.data.authRequired;
  return (
    <div className="workshop-shell">
      <header className="workshop-header">
        <div className="workshop-header-top">
          <NavLink to="/" className="brand"><span className="brand-mark">M</span>orWalPiz <span>Workshop</span></NavLink>
          <div className="header-account">
            <LanguageSelector />
            <AppearanceSelector
              label={t("Appearance")}
              systemLabel={t("System")}
              lightLabel={t("Light")}
              darkLabel={t("Dark")}
              persistenceErrorMessage={t(
                "Your appearance choice will last only for this visit because browser storage is unavailable.",
              )}
            />
            <NavLink className="header-account-link" to="/account/profile">{session.account?.displayName ?? session.account?.username ?? t("Account")}</NavLink>
            <button type="button" className="button button-quiet header-guide-button" onClick={() => { setWizardError(null); setWizardOpen(true); }}>
              {t("Guide")}
            </button>
            {canInstall && !installed ? (
              <button className="button button-quiet icon-button" onClick={reopen} aria-label={t("Install app")} title={t("Install app")}>
                <HeaderIcon name="download" />
              </button>
            ) : null}
            <button className="button button-quiet icon-button" onClick={() => void handleSignOut()}
              disabled={signingOut} aria-label={t("Sign out")} title={t("Sign out")}>
              <HeaderIcon name="logout" />
            </button>
          </div>
        </div>
        <div className="workshop-header-navigation">
          <nav className="desktop-nav" aria-label={t("Workshop navigation")}>
            {navigation.map(([label, path]) => <NavLink key={path} to={path} end={path === "/"}>{t(label)}</NavLink>)}
          </nav>
        </div>
      </header>
      <main className="workshop-main" data-feedback-app-content>
        <InstallPrompt />
        {(offlineState.blocked > 0 || showSyncError || showSyncAuthError || logoutError) && (
          <div className="offline-shell-alerts">
          {offlineState.blocked > 0 && (
            <div className="offline-blocked" role="alert">
              {offlineState.commands.filter(command => command.status === "blocked").map(command => (
                <p key={command.id}>{command.error ?? t("A saved operation was aborted because of a conflict.")}</p>
              ))}
            </div>
          )}
          {showSyncError && offlineFetcher.data && "error" in offlineFetcher.data && (
            <p className="form-error" role="alert">{offlineFetcher.data.error}</p>
          )}
          {showSyncAuthError && (
            <p className="form-error" role="alert">{t("Your session expired. Sign in again to sync saved changes.")}</p>
          )}
          {logoutError && <p className="form-error" role="alert">{logoutError}</p>}
          </div>
        )}
        <Outlet context={shellContext} />
      </main>
      {wizardOpen ? (
        <CapabilityWizard
          onClose={() => setWizardOpen(false)}
          onSkip={() => void persistGuideSeen()}
          onComplete={() => void persistGuideSeen()}
          persistenceError={wizardError}
          onRetryPersistence={() => void persistGuideSeen()}
        />
      ) : null}
    </div>
  );
}
