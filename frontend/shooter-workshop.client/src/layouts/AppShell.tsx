import { useEffect } from "react";
import { NavLink, Outlet, useNavigate } from "react-router";
import { useWorkshopAuth } from "../auth/AuthProvider";
import { LoadingState } from "../components/States";
import { LanguageSelector } from "../components/LanguageSelector";
import { useI18n } from "../i18n";

const navigation = [
  ["Dashboard", "/"], ["Firearms", "/firearms"], ["Components", "/components"],
  ["Maintenance", "/maintenance"], ["Ammunition", "/ammunition"], ["Recipes", "/recipes"],
  ["Production", "/production"], ["Shooting sessions", "/sessions/shooting"], ["History", "/history"],
] as const;

export default function AppShell() {
  const { loading, session, error, logout } = useWorkshopAuth();
  const { t } = useI18n();
  const navigate = useNavigate();
  useEffect(() => {
    if (!loading && !session.authenticated) {
      void navigate(`/account/login?returnUrl=${encodeURIComponent(location.pathname)}`, { replace: true });
    }
  }, [loading, navigate, session.authenticated]);
  if (loading) return <LoadingState label={t("Restoring your account session…")} />;
  if (!session.authenticated) {
    return <LoadingState label={error ?? t("Redirecting to account login…")} />;
  }
  return (
    <div className="workshop-shell">
      <header className="workshop-header">
        <NavLink to="/" className="brand"><span className="brand-mark">M</span>orWalPiz <span>Workshop</span></NavLink>
        <nav className="desktop-nav" aria-label={t("Workshop navigation")}>
          {navigation.map(([label, path]) => <NavLink key={path} to={path} end={path === "/"}>{t(label)}</NavLink>)}
        </nav>
        <div className="header-account">
          <LanguageSelector />
          <NavLink to="/account/profile">{session.account?.displayName ?? session.account?.username ?? t("Account")}</NavLink>
          <button className="button button-quiet" onClick={() => void logout().then(() => navigate("/account/login"))}>{t("Sign out")}</button>
        </div>
      </header>
      <main className="workshop-main"><Outlet /></main>
    </div>
  );
}
