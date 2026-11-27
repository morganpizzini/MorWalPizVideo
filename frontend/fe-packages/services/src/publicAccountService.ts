import type {
  PublicAccountCsrf,
  PublicAccountLoginRequest,
  PublicAccountProfileUpdateRequest,
  PublicAccountRegistrationRequest,
  PublicAccountSession,
  PublicAccountSummary,
  PublicAccountMailRequest,
  PublicAccountTokenRequest,
  PublicAccountPasswordResetRequest,
} from "@morwalpizvideo/models";
import frontendEndpoints from "./endpoints-frontend";
import {
  ApiResponseError,
  accountApiService,
  requireSuccessfulResponse,
} from "./apiTransport";

async function getCsrfToken(): Promise<string> {
  const response = (await accountApiService.get(
    frontendEndpoints.PUBLIC_ACCOUNT_CSRF,
    undefined,
    "",
    false,
    "no-store",
  )) as PublicAccountCsrf;
  if (!response?.token) throw new Error("Unable to acquire account CSRF token");
  return response.token;
}

function requireAuthenticatedSession(response: unknown): PublicAccountSession {
  requireSuccessfulResponse(response);
  if (
    !response ||
    typeof response !== "object" ||
    typeof (response as PublicAccountSession).authenticated !== "boolean"
  ) {
    throw new Error("The account request was not accepted");
  }
  return response as PublicAccountSession;
}

export async function getPublicAccountSession(): Promise<PublicAccountSession> {
  const response = await accountApiService.get(
    frontendEndpoints.PUBLIC_ACCOUNT_SESSION,
    undefined,
    "",
    false,
    "no-store",
  );
  requireSuccessfulResponse(response);
  if (
    !response ||
    typeof response !== "object" ||
    typeof (response as PublicAccountSession).authenticated !== "boolean"
  ) {
    throw new Error("The account session response was invalid");
  }
  return response as PublicAccountSession;
}

export function getPublicAccountErrorMessage(error: unknown): string {
  if (!(error instanceof ApiResponseError)) {
    return "Impossibile completare la richiesta. Riprova tra poco.";
  }

  switch (error.status) {
    case 401:
      return "Credenziali non valide o account non disponibile.";
    case 403:
      return "La richiesta di sicurezza è scaduta. Ricarica la pagina e riprova.";
    case 409:
      return "Non è possibile completare la richiesta con questi dati.";
    case 429:
      return "Troppe richieste. Attendi qualche minuto e riprova.";
    default:
      return error.status && error.status >= 500
        ? "Il servizio account non è disponibile. Riprova tra poco."
        : "Impossibile completare la richiesta. Riprova tra poco.";
  }
}

export async function registerPublicAccount(
  request: PublicAccountRegistrationRequest,
): Promise<PublicAccountSession> {
  const csrfToken = await getCsrfToken();
  return requireAuthenticatedSession(
    await accountApiService.post(
      frontendEndpoints.PUBLIC_ACCOUNT_REGISTER,
      request,
      "",
      { "X-CSRF-TOKEN": csrfToken },
    ),
  );
}

export async function loginPublicAccount(
  request: PublicAccountLoginRequest,
): Promise<PublicAccountSession> {
  const csrfToken = await getCsrfToken();
  return requireAuthenticatedSession(
    await accountApiService.post(
      frontendEndpoints.PUBLIC_ACCOUNT_LOGIN,
      request,
      "",
      { "X-CSRF-TOKEN": csrfToken },
    ),
  );
}

export const getPublicProfile = (): Promise<PublicAccountSummary> =>
  accountApiService.get(frontendEndpoints.PUBLIC_ACCOUNT_PROFILE);

export const updatePublicProfile = async (
  request: PublicAccountProfileUpdateRequest,
): Promise<PublicAccountSummary> => {
  const response = await accountApiService.put(
    frontendEndpoints.PUBLIC_ACCOUNT_PROFILE,
    request,
  );
  if (!response?.id) throw new Error("The profile request was not accepted");
  return response as PublicAccountSummary;
};

export const logoutPublicAccount = (): Promise<unknown> =>
  accountApiService.Delete(frontendEndpoints.PUBLIC_ACCOUNT_LOGOUT);

async function submitAccountMail(endpoint: string, request: PublicAccountMailRequest | PublicAccountTokenRequest): Promise<void> {
  const csrfToken = await getCsrfToken();
  requireSuccessfulResponse(await accountApiService.post(endpoint, request, "", { "X-CSRF-TOKEN": csrfToken }));
}

export const requestPublicPasswordReset = (request: PublicAccountMailRequest): Promise<void> =>
  submitAccountMail(frontendEndpoints.PUBLIC_ACCOUNT_RESET_REQUEST, request);
export const resetPublicPassword = (request: PublicAccountPasswordResetRequest): Promise<void> =>
  submitAccountMail(frontendEndpoints.PUBLIC_ACCOUNT_RESET, request);
export const requestPublicEmailConfirmation = (request: PublicAccountMailRequest): Promise<void> =>
  submitAccountMail(frontendEndpoints.PUBLIC_ACCOUNT_CONFIRM_REQUEST, request);
export const confirmPublicEmail = (request: PublicAccountTokenRequest): Promise<void> =>
  submitAccountMail(frontendEndpoints.PUBLIC_ACCOUNT_CONFIRM, request);

export function getPublicAccountMailErrorMessage(error: unknown): string {
  if (error instanceof ApiResponseError) {
    if (error.status === 400) return "The request or link is invalid or expired. Check the details or request a new email.";
    if (error.status === 403) return "The security request expired. Reload the page and try again.";
    if (error.status === 429) return "Too many requests. Wait a few minutes and try again.";
  }
  return "Account mail is unavailable. Check your connection and try again.";
}
