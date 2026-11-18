import { useCallback, useEffect, useState } from "react";
import { NavLink, Outlet, useLocation, useNavigate } from "react-router";
import { markWorkshopCapabilityGuideSeen } from "@morwalpizvideo/services";
import { useWorkshopAuth } from "../auth/AuthProvider";
import { LoadingState } from "../components/States";
import CapabilityWizard from "../components/CapabilityWizard";
import { LanguageSelector } from "../components/LanguageSelector";
import InstallPrompt from "../components/InstallPrompt";
import { useI18n } from "../i18n";
import { useInstallPrompt } from "../services/installPrompt";

const navigation = [
  ["Dashboard", "/"], ["Equipment", "/firearms"],
  ["Ammunition", "/ammunition"], ["Recordings", "/sessions/shooting"], ["History", "/history"],
] as const;

type HeaderIcon = "globe" | "download" | "logout";

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
  const { loading, session, error, logout, updateSession } = useWorkshopAuth();
  const { t } = useI18n();
  const { canInstall, installed, reopen } = useInstallPrompt();
  const navigate = useNavigate();
  const location = useLocation();
  const [wizardOpen, setWizardOpen] = useState(false);
  const [wizardError, setWizardError] = useState<string | null>(null);
  const guideVersion = session.account?.workshopCapabilityGuideSeenVersion ?? 1;
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
    if (!loading && !session.authenticated) {
      const returnUrl = `${location.pathname}${location.search}${location.hash}`;
      void navigate(`/account/login?returnUrl=${encodeURIComponent(returnUrl)}`, { replace: true });
    }
  }, [loading, location.hash, location.pathname, location.search, navigate, session.authenticated]);
  if (loading) return <LoadingState fullScreen label={t("Restoring your account session…")} />;
  if (!session.authenticated) {
    return <LoadingState fullScreen label={error ?? t("Redirecting to account login…")} />;
  }
  return (
    <div className="workshop-shell">
      <header className="workshop-header">
        <div className="workshop-header-top">
          <NavLink to="/" className="brand"><span className="brand-mark">M</span>orWalPiz <span>Workshop</span></NavLink>
          <div className="header-account">
            <LanguageSelector />
            <NavLink className="header-account-link" to="/account/profile">{session.account?.displayName ?? session.account?.username ?? t("Account")}</NavLink>
            <button type="button" className="button button-quiet header-guide-button" onClick={() => { setWizardError(null); setWizardOpen(true); }}>
              {t("Guide")}
            </button>
            {canInstall && !installed ? (
              <button className="button button-quiet icon-button" onClick={reopen} aria-label={t("Install app")} title={t("Install app")}>
                <HeaderIcon name="download" />
              </button>
            ) : null}
            <button className="button button-quiet icon-button" onClick={() => void logout().then(() => navigate("/account/login"))} aria-label={t("Sign out")} title={t("Sign out")}>
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
        <Outlet />
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
