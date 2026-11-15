import { useEffect, useState, type FormEvent } from "react";
import { useLocation, useNavigate } from "react-router";
import { AccountForm } from "@morwalpiz/layout";
import { getApiFieldErrors, getPublicAccountErrorMessage } from "@morwalpizvideo/services";
import { useWorkshopAuth } from "../../auth/AuthProvider";
import { LanguageSelector } from "../../components/LanguageSelector";
import { useI18n } from "../../i18n";

export function getSafeReturnPath(value: string | null): string {
  if (
    !value ||
    !value.startsWith("/") ||
    value.startsWith("//") ||
    value.includes("\\")
  ) {
    return "/";
  }

  try {
    const url = new URL(value, "https://workshop.local");
    if (url.pathname === "/account/login" || url.pathname === "/account/register") {
      return "/";
    }
    return `${url.pathname}${url.search}${url.hash}`;
  } catch {
    return "/";
  }
}

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

export default function AccountLogin() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const location = useLocation();
  const [identifier, setIdentifier] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [fieldErrorMap, setFieldErrorMap] = useState<Record<string, string[]>>({});
  const [pending, setPending] = useState(false);
  const [registrationSuccess] = useState(
    () => location.state?.registrationSuccess === true,
  );
  const [sessionWarning] = useState(
    () => location.state?.sessionRestoreFailed === true,
  );
  const { login } = useWorkshopAuth();
  const returnPath = getSafeReturnPath(
    new URLSearchParams(location.search).get("returnUrl"),
  );

  useEffect(() => {
    if (registrationSuccess || sessionWarning) {
      void navigate(location.pathname + location.search, {
        replace: true,
        state: null,
      });
    }
  }, [location.pathname, location.search, navigate, registrationSuccess, sessionWarning]);

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError("");
    setFieldErrorMap({});
    setPending(true);
    try {
      if (await login({ identifier, password })) {
        void navigate(returnPath, { replace: true });
      } else {
        setError(t("Sign-in was not accepted."));
      }
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

  return (
    <AccountForm
      title={t("Sign in to Workshop")}
      submitLabel={t("Sign in")}
      pendingLabel={t("Signing in…")}
      submitClassName="button button-primary"
      onSubmit={submit}
      error={error}
      message={
        registrationSuccess
          ? t("Registration completed. You can now sign in.")
          : undefined
      }
      warning={
        sessionWarning && !registrationSuccess
          ? t("Account created, but the session could not be verified. Sign in to continue.")
          : undefined
      }
      pending={pending}
      header={
        <div className="account-header">
          <p className="eyebrow">{t("Private workspace")}</p>
          <LanguageSelector />
          <p className="account-subtitle">
            {t(
              "Use your existing MorWalPiz account. Workshop requests stay on the account cookie transport.",
            )}
          </p>
        </div>
      }
      footer={
        <p>
          {t("Need an account?")}{" "}
          <a
            href={`/account/register?returnUrl=${encodeURIComponent(returnPath)}`}
            onClick={(event) => {
              event.preventDefault();
              void navigate(
                `/account/register?returnUrl=${encodeURIComponent(returnPath)}`,
              );
            }}
          >
            {t("Create one")}
          </a>
        </p>
      }
    >
      <label>
        {t("Username or email")}
        <input
          required
          value={identifier}
          onChange={(event) => setIdentifier(event.target.value)}
          aria-invalid={fieldErrors(fieldErrorMap, "Identifier").length > 0}
          aria-describedby="workshop-identifier-errors"
        />
        <FieldErrors
          id="workshop-identifier-errors"
          errors={fieldErrors(fieldErrorMap, "Identifier")}
        />
      </label>
      <label>
        {t("Password")}
        <input
          required
          type="password"
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          aria-invalid={fieldErrors(fieldErrorMap, "Password").length > 0}
          aria-describedby="workshop-password-errors"
        />
        <FieldErrors
          id="workshop-password-errors"
          errors={fieldErrors(fieldErrorMap, "Password")}
        />
      </label>
    </AccountForm>
  );
}
