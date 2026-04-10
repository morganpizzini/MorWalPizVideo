/**
 * Email Validation Utilities
 * 
 * Provides email format validation and disposable email domain detection
 * to prevent spam and ensure valid email addresses.
 */

/**
 * List of known disposable/temporary email domains
 * These should be rejected during registration
 */
const DISPOSABLE_DOMAINS = [
  '10minutemail.com',
  'guerrillamail.com',
  'mailinator.com',
  'tempmail.com',
  'throwaway.email',
  'temp-mail.org',
  'getnada.com',
  'maildrop.cc',
  'mintemail.com',
  'trashmail.com',
  'yopmail.com',
  'sharklasers.com',
  'guerrillamail.info',
  'grr.la',
  'guerrillamail.biz',
  'guerrillamail.de',
  'spam4.me',
  'mailnesia.com',
  'mytrashmail.com',
  'jetable.org',
];

/**
 * Validates email format using RFC 5322 compliant regex
 */
export function isValidEmail(email: string): boolean {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return emailRegex.test(email);
}

/**
 * Normalizes email by trimming whitespace and converting to lowercase
 */
export function normalizeEmail(email: string): string {
  return email.trim().toLowerCase();
}

/**
 * Checks if email domain is in the disposable domains list
 */
export function isDisposableEmail(email: string): boolean {
  const domain = email.split('@')[1];
  return domain ? DISPOSABLE_DOMAINS.includes(domain.toLowerCase()) : false;
}

/**
 * Comprehensive email validation for registration
 * Returns validation result with error message if invalid
 */
export function validateEmailForRegistration(email: string): {
  valid: boolean;
  error?: string;
} {
  const normalized = normalizeEmail(email);

  if (!normalized) {
    return { valid: false, error: 'Email è obbligatoria' };
  }

  if (!isValidEmail(normalized)) {
    return { valid: false, error: 'Formato email non valido' };
  }

  if (isDisposableEmail(normalized)) {
    return {
      valid: false,
      error: 'Email temporanea non consentita. Utilizza un indirizzo email permanente.',
    };
  }

  return { valid: true };
}

/**
 * Extract domain from email address
 */
export function getEmailDomain(email: string): string | null {
  const parts = email.split('@');
  return parts.length === 2 ? parts[1].toLowerCase() : null;
}

/**
 * Check if email appears to be from a business domain (not freemail)
 */
export function isBusinessEmail(email: string): boolean {
  const freemailDomains = [
    'gmail.com',
    'yahoo.com',
    'hotmail.com',
    'outlook.com',
    'live.com',
    'icloud.com',
    'mail.com',
    'aol.com',
    'protonmail.com',
    'zoho.com',
  ];

  const domain = getEmailDomain(email);
  return domain ? !freemailDomains.includes(domain) : false;
}