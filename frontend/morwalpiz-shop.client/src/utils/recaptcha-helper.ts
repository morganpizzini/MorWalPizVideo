/**
 * reCAPTCHA Helper Utilities
 * 
 * Provides React hooks and utilities for integrating Google reCAPTCHA v3
 * into authentication and form submission flows.
 */

import { useGoogleReCaptcha } from 'react-google-recaptcha-v3';
import { useCallback } from 'react';

/**
 * Hook to execute reCAPTCHA verification
 * Returns a function to get reCAPTCHA token for a specific action
 */
export function useRecaptcha() {
  const { executeRecaptcha } = useGoogleReCaptcha();

  const getRecaptchaToken = useCallback(
    async (action: string = 'submit'): Promise<string> => {
      if (!executeRecaptcha) {
        console.warn('reCAPTCHA not available yet');
        throw new Error('reCAPTCHA non disponibile. Riprova tra poco.');
      }

      try {
        const token = await executeRecaptcha(action);
        return token;
      } catch (error) {
        console.error('reCAPTCHA execution failed:', error);
        throw new Error('Verifica reCAPTCHA fallita. Riprova.');
      }
    },
    [executeRecaptcha]
  );

  return {
    getRecaptchaToken,
    isReady: !!executeRecaptcha,
  };
}

/**
 * Common reCAPTCHA action names for consistent usage
 */
export const RecaptchaActions = {
  LOGIN: 'email_login',
  REGISTER: 'register',
  CHECKOUT: 'checkout',
  CONTACT: 'contact_form',
  NEWSLETTER: 'newsletter_subscribe',
} as const;

/**
 * Validate reCAPTCHA token format
 * Tokens should be non-empty strings
 */
export function isValidRecaptchaToken(token: string): boolean {
  return typeof token === 'string' && token.length > 0;
}

/**
 * Error types for reCAPTCHA failures
 */
export class RecaptchaError extends Error {
  constructor(
    message: string,
    public readonly code: 'NOT_READY' | 'EXECUTION_FAILED' | 'INVALID_TOKEN'
  ) {
    super(message);
    this.name = 'RecaptchaError';
  }
}

/**
 * Execute reCAPTCHA with error handling and retry logic
 */
export async function executeRecaptchaWithRetry(
  executeRecaptcha: ((action?: string) => Promise<string>) | undefined,
  action: string,
  maxRetries: number = 2
): Promise<string> {
  if (!executeRecaptcha) {
    throw new RecaptchaError(
      'reCAPTCHA non disponibile',
      'NOT_READY'
    );
  }

  let lastError: Error | null = null;

  for (let attempt = 0; attempt <= maxRetries; attempt++) {
    try {
      const token = await executeRecaptcha(action);
      
      if (!isValidRecaptchaToken(token)) {
        throw new RecaptchaError(
          'Token reCAPTCHA non valido',
          'INVALID_TOKEN'
        );
      }

      return token;
    } catch (error) {
      lastError = error as Error;
      
      // Wait before retry (exponential backoff)
      if (attempt < maxRetries) {
        await new Promise(resolve => setTimeout(resolve, 1000 * (attempt + 1)));
      }
    }
  }

  throw new RecaptchaError(
    lastError?.message || 'Verifica reCAPTCHA fallita dopo diversi tentativi',
    'EXECUTION_FAILED'
  );
}