import { useState } from "react";
import { useNavigate } from "react-router";
import { useWorkshopAuth } from "../../auth/AuthProvider";
import { LanguageSelector } from "../../components/LanguageSelector";
import { useI18n } from "../../i18n";
export default function AccountLogin() {
  const { t } = useI18n();
  const navigate = useNavigate();
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
              if (ok) void navigate("/");
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
