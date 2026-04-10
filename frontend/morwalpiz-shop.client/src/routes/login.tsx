/**
 * Login Page Component
 * 
 * Email-only authentication with reCAPTCHA v3 and terms acceptance.
 * No password required - authentication via email verification.
 */

import { useState, FormEvent } from 'react';
import { useNavigate, useSearchParams } from 'react-router';
import { Helmet } from 'react-helmet-async';
import { useAuth } from '../contexts/AuthContext';
import { useRecaptcha, RecaptchaActions } from '../utils/recaptcha-helper';
import {
  validateEmailForRegistration,
  normalizeEmail,
} from '../utils/email-validator';
import { shopLogin } from '@morwalpizvideo/services';
import type { EmailLoginRequest } from '@morwalpizvideo/models';

export default function Login() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { login } = useAuth();
  const { getRecaptchaToken, isReady } = useRecaptcha();

  const [email, setEmail] = useState('');
  const [termsAccepted, setTermsAccepted] = useState(false);
  const [newsletterAccepted, setNewsletterAccepted] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [validationError, setValidationError] = useState<string | null>(null);

  const redirectTo = searchParams.get('redirect') || '/';

  const handleEmailChange = (value: string) => {
    setEmail(value);
    setValidationError(null);
    setError(null);
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    setValidationError(null);

    // Validate email
    const normalized = normalizeEmail(email);
    const validation = validateEmailForRegistration(normalized);

    if (!validation.valid) {
      setValidationError(validation.error || 'Email non valida');
      return;
    }

    if (!termsAccepted) {
      setValidationError('Devi accettare i Termini e Condizioni');
      return;
    }

    if (!isReady) {
      setError('Sistema di sicurezza non pronto. Riprova tra poco.');
      return;
    }

    setIsSubmitting(true);

    try {
      // Get reCAPTCHA token
      const recaptchaToken = await getRecaptchaToken(RecaptchaActions.LOGIN);

      // Create login request
      const loginRequest: EmailLoginRequest = {
        email: normalized,
        termsAccepted: true,
        newsletterAccepted,
        recaptchaToken,
      };

      // Call login API
      const response = await shopLogin(loginRequest);

      // Save session
      login({
        customerId: response.customerId,
        email: response.email,
        sessionToken: response.sessionToken,
      });

      // Redirect to original destination or home
      navigate(redirectTo, { replace: true });
    } catch (err: any) {
      console.error('Login error:', err);

      if (err.message?.includes('reCAPTCHA')) {
        setError('Verifica di sicurezza fallita. Riprova.');
      } else if (err.message?.includes('Email temporanea')) {
        setError('Email temporanea non consentita. Utilizza un indirizzo permanente.');
      } else if (err.message?.includes('404') || err.message?.includes('429')) {
        setError('Servizio temporaneamente non disponibile. Riprova più tardi.');
      } else {
        setError('Errore durante il login. Riprova più tardi.');
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <>
      <Helmet>
        <title>Login - MorWalPiz Shop</title>
        <meta
          name="description"
          content="Accedi al tuo account MorWalPiz Shop"
        />
      </Helmet>

      <div className="container my-5">
        <div className="row justify-content-center">
          <div className="col-md-6 col-lg-5">
            <div className="card shadow">
              <div className="card-body p-4">
                <h1 className="card-title text-center mb-4">Accedi</h1>

                <p className="text-muted text-center mb-4">
                  Inserisci la tua email per accedere o registrarti.
                  <br />
                  <small>Non è richiesta alcuna password.</small>
                </p>

                {(error || validationError) && (
                  <div className="alert alert-danger" role="alert">
                    {error || validationError}
                  </div>
                )}

                <form onSubmit={handleSubmit}>
                  <div className="mb-3">
                    <label htmlFor="email" className="form-label">
                      Email <span className="text-danger">*</span>
                    </label>
                    <input
                      type="email"
                      className={`form-control ${validationError ? 'is-invalid' : ''}`}
                      id="email"
                      value={email}
                      onChange={(e) => handleEmailChange(e.target.value)}
                      placeholder="tua@email.com"
                      required
                      disabled={isSubmitting}
                      autoComplete="email"
                      autoFocus
                    />
                    {validationError && (
                      <div className="invalid-feedback">{validationError}</div>
                    )}
                  </div>

                  <div className="mb-3 form-check">
                    <input
                      type="checkbox"
                      className="form-check-input"
                      id="termsCheck"
                      checked={termsAccepted}
                      onChange={(e) => setTermsAccepted(e.target.checked)}
                      required
                      disabled={isSubmitting}
                    />
                    <label className="form-check-label" htmlFor="termsCheck">
                      Accetto i{' '}
                      <a
                        href="/terms"
                        target="_blank"
                        rel="noopener noreferrer"
                      >
                        Termini e Condizioni
                      </a>{' '}
                      <span className="text-danger">*</span>
                    </label>
                  </div>

                  <div className="mb-3 form-check">
                    <input
                      type="checkbox"
                      className="form-check-input"
                      id="newsletterCheck"
                      checked={newsletterAccepted}
                      onChange={(e) => setNewsletterAccepted(e.target.checked)}
                      disabled={isSubmitting}
                    />
                    <label className="form-check-label" htmlFor="newsletterCheck">
                      Voglio ricevere la newsletter con offerte e aggiornamenti
                    </label>
                  </div>

                  <div className="d-grid">
                    <button
                      type="submit"
                      className="btn btn-primary"
                      disabled={isSubmitting || !termsAccepted || !email || !isReady}
                    >
                      {isSubmitting ? (
                        <>
                          <span
                            className="spinner-border spinner-border-sm me-2"
                            role="status"
                            aria-hidden="true"
                          ></span>
                          Accesso in corso...
                        </>
                      ) : (
                        'Accedi'
                      )}
                    </button>
                  </div>
                </form>

                <div className="mt-4 text-center">
                  <small className="text-muted">
                    Questa pagina è protetta da reCAPTCHA e si applicano la{' '}
                    <a
                      href="https://policies.google.com/privacy"
                      target="_blank"
                      rel="noopener noreferrer"
                    >
                      Privacy Policy
                    </a>{' '}
                    e i{' '}
                    <a
                      href="https://policies.google.com/terms"
                      target="_blank"
                      rel="noopener noreferrer"
                    >
                      Termini di Servizio
                    </a>{' '}
                    di Google.
                  </small>
                </div>

                <div className="mt-3 text-center">
                  <small className="text-muted">
                    <strong>Nota sulla privacy:</strong> La tua email NON verrà
                    mai ceduta a terzi e sarà utilizzata solo per la gestione
                    dell'account.
                  </small>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}