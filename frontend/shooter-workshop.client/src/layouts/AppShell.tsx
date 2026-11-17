import { useEffect } from "react";
import { NavLink, Outlet, useLocation, useNavigate } from "react-router";
import { useWorkshopAuth } from "../auth/AuthProvider";
import { LoadingState } from "../components/States";
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
  const { loading, session, error, logout } = useWorkshopAuth();
  const { t } = useI18n();
  const { canInstall, installed, reopen } = useInstallPrompt();
  const navigate = useNavigate();
  const location = useLocation();
  useEffect(() => {
    if (!loading && !session.authenticated) {
      const returnUrl = `${location.pathname}${location.search}${location.hash}`;
      void navigate(`/account/login?returnUrl=${encodeURIComponent(returnUrl)}`, { replace: true });
    }
  }, [loading, location.hash, location.pathname, location.search, navigate, session.authenticated]);
  if (loading) return <LoadingState label={t("Restoring your account session…")} />;
  if (!session.authenticated) {
    return <LoadingState label={error ?? t("Redirecting to account login…")} />;
  }
  return (
    <div className="workshop-shell">
      <header className="workshop-header">
        <div className="workshop-header-top">
          <NavLink to="/" className="brand"><span className="brand-mark">M</span>orWalPiz <span>Workshop</span></NavLink>
          <div className="header-account">
            <LanguageSelector />
            <NavLink className="header-account-link" to="/account/profile">{session.account?.displayName ?? session.account?.username ?? t("Account")}</NavLink>
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
      <main className="workshop-main">
        <InstallPrompt />
        <Outlet />
      </main>
    </div>
  );
}
