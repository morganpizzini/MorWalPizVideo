import { useState, type FormEvent } from "react";
import { useLocation, useNavigate } from "react-router";
import { AccountForm, AppearanceSelector } from "@morwalpiz/layout";
import {
  getApiFieldErrors,
  getPublicAccountErrorMessage,
  registerPublicAccount,
} from "@morwalpizvideo/services";
import type { PublicAccountRegistrationRequest } from "@morwalpizvideo/models";
import { useWorkshopAuth } from "../../auth/AuthProvider";
import { LanguageSelector } from "../../components/LanguageSelector";
import { useI18n } from "../../i18n";
import { getSafeReturnPath } from "../AccountLogin/component";

function fieldErrors(
  errors: Record<string, string[]>,
  field: string,
): string[] {
  const entry = Object.entries(errors).find(
    ([key]) => key.toLowerCase() === field.toLowerCase(),
  );
  return entry?.[1] ?? [];
}

function FieldErrors({ id, errors }: { id: string; errors: string[] }) {
  if (!errors.length) return null;
  return (
    <div id={id} className="form-error" role="alert">
      {errors.map((error) => (
        <div key={error}>{error}</div>
      ))}
    </div>
  );
}

export default function AccountRegister() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const location = useLocation();
  const { refresh } = useWorkshopAuth();
  const returnPath = getSafeReturnPath(
    new URLSearchParams(location.search).get("returnUrl"),
  );
  const [form, setForm] = useState<PublicAccountRegistrationRequest>({
    username: "",
    email: "",
    password: "",
    displayName: "",
    termsAccepted: false,
    privacyNoticeAcknowledged: false,
  });
  const [confirmation, setConfirmation] = useState("");
  const [error, setError] = useState("");
  const [fieldErrorMap, setFieldErrorMap] = useState<Record<string, string[]>>({});
  const [pending, setPending] = useState(false);

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError("");
    setFieldErrorMap({});
    if (form.password !== confirmation) {
      setError(t("Passwords do not match."));
      return;
    }

    setPending(true);
    try {
      await registerPublicAccount(form);
      const sessionRestored = await refresh();
      void navigate(
        `/account/login?returnUrl=${encodeURIComponent(returnPath)}`,
        {
          replace: true,
          state: {
            registrationSuccess: true,
            sessionRestoreFailed: !sessionRestored,
          },
        },
      );
    } catch (requestError) {
      const nextFieldErrors = getApiFieldErrors(requestError);
      setFieldErrorMap(nextFieldErrors);
      setError(
        Object.keys(nextFieldErrors).length
          ? ""
          : getPublicAccountErrorMessage(requestError),
      );
    } finally {
      setPending(false);
    }
  };

  const updateForm = <K extends keyof PublicAccountRegistrationRequest>(
    key: K,
    value: PublicAccountRegistrationRequest[K],
  ) => {
    setForm((current) => ({ ...current, [key]: value }));
  };

  return (
    <AccountForm
      title={t("Create your Workshop account")}
      submitLabel={t("Create account")}
      pendingLabel={t("Creating account…")}
      submitClassName="button button-primary"
      onSubmit={submit}
      error={error}
      pending={pending}
      header={
        <div className="account-header">
          <p className="eyebrow">{t("Private workspace")}</p>
          <div className="account-header-controls">
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
          </div>
          <p className="account-subtitle">
            {t(
              "Create one MorWalPiz account to use Workshop and keep your equipment history connected.",
            )}
          </p>
        </div>
      }
      footer={
        <p>
          {t("Already have an account?")}{" "}
          <a
            href={`/account/login?returnUrl=${encodeURIComponent(returnPath)}`}
            onClick={(event) => {
              event.preventDefault();
              void navigate(
                `/account/login?returnUrl=${encodeURIComponent(returnPath)}`,
              );
            }}
          >
            {t("Sign in")}
          </a>
        </p>
      }
    >
      <label>
        {t("Username")}
        <input
          required
          minLength={3}
          maxLength={40}
          value={form.username}
          onChange={(event) => updateForm("username", event.target.value)}
          aria-invalid={fieldErrors(fieldErrorMap, "Username").length > 0}
          aria-describedby="workshop-username-errors"
        />
        <FieldErrors
          id="workshop-username-errors"
          errors={fieldErrors(fieldErrorMap, "Username")}
        />
      </label>
      <label>
        {t("Email")}
        <input
          required
          type="email"
          maxLength={256}
          value={form.email}
          onChange={(event) => updateForm("email", event.target.value)}
          aria-invalid={fieldErrors(fieldErrorMap, "Email").length > 0}
          aria-describedby="workshop-email-errors"
        />
        <FieldErrors
          id="workshop-email-errors"
          errors={fieldErrors(fieldErrorMap, "Email")}
        />
      </label>
      <label>
        {t("Display name")}
        <input
          maxLength={120}
          value={form.displayName}
          onChange={(event) => updateForm("displayName", event.target.value)}
          aria-invalid={fieldErrors(fieldErrorMap, "DisplayName").length > 0}
          aria-describedby="workshop-display-name-errors"
        />
        <FieldErrors
          id="workshop-display-name-errors"
          errors={fieldErrors(fieldErrorMap, "DisplayName")}
        />
      </label>
      <label>
        {t("Password")}
        <input
          required
          type="password"
          minLength={8}
          maxLength={128}
          value={form.password}
          onChange={(event) => updateForm("password", event.target.value)}
          aria-invalid={fieldErrors(fieldErrorMap, "Password").length > 0}
          aria-describedby="workshop-register-password-errors"
        />
        <FieldErrors
          id="workshop-register-password-errors"
          errors={fieldErrors(fieldErrorMap, "Password")}
        />
      </label>
      <label>
        {t("Confirm password")}
        <input
          required
          type="password"
          minLength={8}
          maxLength={128}
          value={confirmation}
          onChange={(event) => setConfirmation(event.target.value)}
          aria-invalid={confirmation.length > 0 && confirmation !== form.password}
        />
      </label>
      <label className="account-legal-check">
        <span>
          <input
            required
            type="checkbox"
            checked={form.termsAccepted}
            onChange={(event) => {
              const accepted = event.target.checked;
              setForm((current) => ({
                ...current,
                termsAccepted: accepted,
                privacyNoticeAcknowledged: accepted,
              }));
            }}
            aria-invalid={fieldErrors(fieldErrorMap, "TermsAccepted").length > 0}
            aria-describedby="workshop-legal-errors"
          />{" "}
          {t("I have read the privacy notice and accept the terms of use.")}
        </span>
        <FieldErrors
          id="workshop-legal-errors"
          errors={[
            ...fieldErrors(fieldErrorMap, "TermsAccepted"),
            ...fieldErrors(fieldErrorMap, "PrivacyNoticeAcknowledged"),
          ]}
        />
      </label>
    </AccountForm>
  );
}
