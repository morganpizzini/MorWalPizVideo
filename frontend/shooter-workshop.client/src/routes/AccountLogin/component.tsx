import { useState } from "react";
import { useLocation, useNavigate } from "react-router";
import { useWorkshopAuth } from "../../auth/AuthProvider";
import { LanguageSelector } from "../../components/LanguageSelector";
import { useI18n } from "../../i18n";

const workshopOrigin = "https://workshop.local";

export function getSafeReturnPath(value: string | null): string {
  if (!value || !value.startsWith("/") || value.startsWith("//") || value.includes("\\")) {
    return "/";
  }

  try {
    const url = new URL(value, workshopOrigin);
    return `${url.pathname}${url.search}${url.hash}`;
  } catch {
    return "/";
  }
}

export default function AccountLogin() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const location = useLocation();
  const [identifier, setIdentifier] = useState("");
  const [password, setPassword] = useState("");
  const [message, setMessage] = useState("");
  const { login } = useWorkshopAuth();
  return (
    <main className="account-page">
      <div className="account-card">
        <LanguageSelector />
        <p className="eyebrow">{t("Private workspace")}</p>
        <h1>{t("Sign in to Workshop")}</h1>
        <p>{t("Use your existing MorWalPiz account. Workshop requests stay on the account cookie transport.")}</p>
        <form
          onSubmit={(event) => {
            event.preventDefault();
            void login({ identifier, password }).then((ok) => {
              if (ok) {
                const returnUrl = new URLSearchParams(location.search).get("returnUrl");
                void navigate(getSafeReturnPath(returnUrl), { replace: true });
              }
              else setMessage(t("Sign-in was not accepted."));
            });
          }}
        >
          <label>
            {t("Username or email")}
            <input
              required
              value={identifier}
              onChange={(event) => setIdentifier(event.target.value)}
            />
          </label>
          <label>
            {t("Password")}
            <input
              required
              type="password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
            />
          </label>
          {message && (
            <p className="form-error" role="alert">
              {message}
            </p>
          )}
          <button className="button button-primary">{t("Sign in")}</button>
        </form>
      </div>
    </main>
  );
}
