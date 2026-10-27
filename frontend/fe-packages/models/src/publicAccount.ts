export interface PublicAccountRegistrationRequest {
  username: string;
  email: string;
  password: string;
  displayName: string;
  termsAccepted: boolean;
  privacyNoticeAcknowledged: boolean;
}

export interface PublicAccountLoginRequest {
  identifier: string;
  password: string;
}

export interface PublicAccountProfileUpdateRequest {
  displayName: string;
  email: string;
}

export interface PublicAccountSummary {
  id: string;
  username: string;
  email: string;
  displayName: string;
}

export interface PublicAccountSession {
  authenticated: boolean;
  account?: PublicAccountSummary;
}

export interface PublicAccountCsrf {
  token: string;
}
