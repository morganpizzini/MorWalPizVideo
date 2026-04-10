export interface Customer {
  id: string;
  email: string;
  name?: string;
  createdAt: Date;
  lastLoginAt?: Date;
  newsletterAccepted: boolean;
  termsAccepted: boolean;
  termsAcceptedAt?: Date;
}

export interface EmailLoginRequest {
  email: string;
  termsAccepted: boolean;
  newsletterAccepted?: boolean;
  recaptchaToken: string;
}

export interface EmailLoginResponse {
  customerId: string;
  email: string;
  sessionToken: string;
}

export interface EmailVerificationRequest {
  email: string;
  verificationCode: string;
}

export interface LoginResponse {
  customerId: string;
  email: string;
  sessionToken: string;
  expiresAt: Date;
}
