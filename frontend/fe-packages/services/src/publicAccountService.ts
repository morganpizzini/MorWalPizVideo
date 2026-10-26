import type {
  PublicAccountCsrf,
  PublicAccountLoginRequest,
  PublicAccountProfileUpdateRequest,
  PublicAccountRegistrationRequest,
  PublicAccountSession,
  PublicAccountSummary,
} from "@morwalpizvideo/models";
import frontendEndpoints from "./endpoints-frontend";
import { accountApiService } from "./apiTransport";

async function getCsrfToken(): Promise<string> {
  const response = (await accountApiService.get(
    frontendEndpoints.PUBLIC_ACCOUNT_CSRF,
  )) as PublicAccountCsrf;
  if (!response?.token) throw new Error("Unable to acquire account CSRF token");
  return response.token;
}

function requireAuthenticatedSession(response: unknown): PublicAccountSession {
  if (
    !response ||
    typeof response !== "object" ||
    !(response as PublicAccountSession).authenticated
  ) {
    throw new Error("The account request was not accepted");
  }
  return response as PublicAccountSession;
}

export const getPublicAccountSession = (): Promise<PublicAccountSession> =>
  accountApiService.get(frontendEndpoints.PUBLIC_ACCOUNT_SESSION);

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
