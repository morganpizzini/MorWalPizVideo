export type PublicAccountSite = "workshop" | "public-site";
export interface PublicAccountMailRequest {
  email: string;
  site: PublicAccountSite;
}
export interface PublicAccountTokenRequest {
  accountId: string;
  token: string;
}
export interface PublicAccountPasswordResetRequest extends PublicAccountTokenRequest {
  password: string;
}

export interface PublicAccountRegistrationRequest {
  site: PublicAccountSite;
  username: string;
  email: string;
  password: string;
  displayName: string;
  termsAccepted: boolean;
  privacyNoticeAcknowledged: boolean;
}

export interface PublicAccountLoginRequest {
  site?: PublicAccountSite;
  identifier: string;
  password: string;
  rememberMe?: boolean;
}

export interface PublicAccountProfileUpdateRequest {
  displayName: string;
  email: string;
}

export interface PublicAccountSummary {
  emailConfirmed?: boolean;
  emailConfirmationRequired?: boolean;
  id: string;
  username: string;
  email: string;
  displayName: string;
  workshopCapabilityGuideSeenVersion?: number;
}

export interface PublicAccountSession {
  authenticated: boolean;
  emailConfirmationRequired?: boolean;
  confirmationEmailStatus?: "requested" | "unavailable";
  account?: PublicAccountSummary;
}

export interface PublicAccountCsrf {
  token: string;
}
