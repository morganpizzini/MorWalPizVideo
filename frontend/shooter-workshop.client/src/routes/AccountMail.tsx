import { useState } from "react";
import { useLocation } from "react-router";
import { AccountMailPage, AppearanceSelector } from "@morwalpiz/layout";
import { confirmPublicEmail, requestPublicEmailConfirmation, requestPublicPasswordReset, resetPublicPassword, getPublicAccountMailErrorMessage } from "@morwalpizvideo/services";
import { getAccountMailLink, clearAccountMailLink } from "@morwalpizvideo/services/account-mail-link";
import { LanguageSelector } from "../components/LanguageSelector";
import { useI18n } from "../i18n";

export default function AccountMail() {
  const { t } = useI18n();
  const location = useLocation();
  const [link] = useState(getAccountMailLink);
  const mode = location.pathname === "/account/forgot-password" ? "request-reset" : location.pathname === "/account/reset-password"
    ? "reset" : location.pathname === "/account/check-email" ? "check-email" : "confirm";
  return <AccountMailPage key={mode} mode={mode} t={t} link={link} initialEmail={typeof location.state?.email === "string" ? location.state.email : ""}
    initiallyRequested={location.state?.confirmationEmailStatus === "requested"}
    requestPath={mode === "confirm" ? "/account/check-email" : "/account/forgot-password"}
    submitClassName="button button-primary"
    header={<div className="account-header-controls"><LanguageSelector /><AppearanceSelector label={t("Appearance")}
      systemLabel={t("System")} lightLabel={t("Light")} darkLabel={t("Dark")}
      persistenceErrorMessage={t("Your appearance choice will last only for this visit because browser storage is unavailable.")} /></div>}
    errorMessage={(error) => t(getPublicAccountMailErrorMessage(error))}
    onRequest={(email) => mode === "request-reset"
      ? requestPublicPasswordReset({ email, site: "workshop" })
      : requestPublicEmailConfirmation({ email, site: "workshop" })}
    onConsume={async (token, password) => {
      if (mode === "reset") await resetPublicPassword({ ...token, password });
      else await confirmPublicEmail(token);
      clearAccountMailLink();
    }} />;
}
