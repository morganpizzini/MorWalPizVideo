import { useEffect, useState, type FormEvent } from 'react';
import { Link, useLocation, useNavigate } from 'react-router';
import { AccountForm } from '@morwalpiz/layout';
import {
  getPublicProfile,
  getPublicAccountErrorMessage,
  loginPublicAccount,
  registerPublicAccount,
  updatePublicProfile,
} from '@morwalpizvideo/services';
import { getApiFieldErrors } from '@morwalpizvideo/services';
import type { PublicAccountSummary } from '@morwalpizvideo/models';
import { usePublicAccount } from '../layout/account';
import {
  getAccountLoginPath,
  getAccountRegisterPath,
  getPostLoginDestination,
  getSafeReturnUrl,
} from './returnUrl';
import './style.scss';

function errorMessage(error: unknown): string {
  return getPublicAccountErrorMessage(error);
}

function fieldError(fieldErrors: Record<string, string[]>, field: string): string[] {
  const entry = Object.entries(fieldErrors).find(
    ([key]) => key.toLowerCase() === field.toLowerCase()
  );
  return entry?.[1] ?? [];
}

function FieldErrors({ id, errors }: { id: string; errors: string[] }) {
  if (!errors.length) return null;
  return (
    <div id={id} className="invalid-feedback d-block">
      {errors.map((message) => (
        <div key={message}>{message}</div>
      ))}
    </div>
  );
}

export function Login() {
  const navigate = useNavigate();
  const location = useLocation();
  const { refresh } = usePublicAccount();
  const [identifier, setIdentifier] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});
  const [pending, setPending] = useState(false);
  const [registrationSuccess] = useState(() => location.state?.registrationSuccess === true);
  const [sessionWarning] = useState(() => location.state?.sessionRestoreFailed === true);
  const returnUrl = getSafeReturnUrl(location.search);

  useEffect(() => {
    if (registrationSuccess || sessionWarning) {
      void navigate(getAccountLoginPath(returnUrl), { replace: true, state: null });
    }
  }, [navigate, registrationSuccess, returnUrl, sessionWarning]);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setError('');
    setFieldErrors({});
    setPending(true);
    try {
      const result = await loginPublicAccount({ identifier, password, site: 'public-site' });
      if (result.emailConfirmationRequired && !result.authenticated) {
        void navigate('/account/check-email', { state: { email: identifier.includes('@') ? identifier : '' } });
        return;
      }
      if (!(await refresh(result.confirmationEmailStatus))) {
        throw new Error('La sessione autenticata non è stata verificata.');
      }
      void navigate(getPostLoginDestination(location.search), { replace: true });
    } catch (requestError) {
      const nextFieldErrors = getApiFieldErrors(requestError);
      setFieldErrors(nextFieldErrors);
      setError(Object.keys(nextFieldErrors).length ? '' : errorMessage(requestError));
    } finally {
      setPending(false);
    }
  };

  return (
    <AccountForm
      title="Accedi"
      submitLabel="Accedi"
      onSubmit={submit}
      error={error}
      pending={pending}
      message={registrationSuccess ? 'Registrazione completata. Ora puoi accedere.' : undefined}
      warning={
        sessionWarning
          ? 'Account creato, ma non è stato possibile verificare la sessione. Accedi per continuare.'
          : undefined
      }
    >
      <label className="form-label" htmlFor="identifier">
        Username o email
      </label>
      <input
        id="identifier"
        className="form-control"
        value={identifier}
        onChange={(event) => setIdentifier(event.target.value)}
        required
        aria-invalid={fieldError(fieldErrors, 'Identifier').length > 0}
        aria-describedby="identifier-errors"
      />
      <FieldErrors id="identifier-errors" errors={fieldError(fieldErrors, 'Identifier')} />
      <label className="form-label mt-3" htmlFor="password">
        Password
      </label>
      <input
        id="password"
        type="password"
        className="form-control"
        value={password}
        onChange={(event) => setPassword(event.target.value)}
        required
        maxLength={128}
        aria-invalid={fieldError(fieldErrors, 'Password').length > 0}
        aria-describedby="login-password-errors"
      />
      <FieldErrors id="login-password-errors" errors={fieldError(fieldErrors, 'Password')} />
      <p className="mt-3"><Link to="/account/forgot-password">Password dimenticata?</Link></p>
      <p className="mt-3 mb-0">
        Non hai un account? <Link to={getAccountRegisterPath(returnUrl)}>Registrati</Link>
      </p>
    </AccountForm>
  );
}

export function Register() {
  const navigate = useNavigate();
  const [form, setForm] = useState({
    site: 'public-site' as const,
    username: '',
    email: '',
    password: '',
    displayName: '',
    termsAccepted: false,
    privacyNoticeAcknowledged: false,
  });
  const [error, setError] = useState('');
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});
  const [pending, setPending] = useState(false);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setError('');
    setFieldErrors({});
    setPending(true);
    try {
      const result = await registerPublicAccount(form);
      setForm((current) => ({ ...current, password: '' }));
      void navigate('/account/check-email', {
        replace: true,
        state: { email: form.email, confirmationEmailStatus: result.confirmationEmailStatus },
      });
    } catch (requestError) {
      const nextFieldErrors = getApiFieldErrors(requestError);
      setFieldErrors(nextFieldErrors);
      setError(Object.keys(nextFieldErrors).length ? '' : errorMessage(requestError));
    } finally {
      setPending(false);
    }
  };

  return (
    <AccountForm
      title="Crea account"
      submitLabel="Registrati"
      onSubmit={submit}
      error={error}
      pending={pending}
    >
      <label className="form-label" htmlFor="username">
        Username
      </label>
      <input
        id="username"
        className="form-control"
        value={form.username}
        onChange={(event) => setForm({ ...form, username: event.target.value })}
        required
        minLength={3}
        maxLength={40}
        aria-invalid={fieldError(fieldErrors, 'Username').length > 0}
        aria-describedby="username-errors"
      />
      <FieldErrors id="username-errors" errors={fieldError(fieldErrors, 'Username')} />
      <label className="form-label mt-3" htmlFor="email">
        Email
      </label>
      <input
        id="email"
        type="email"
        className="form-control"
        value={form.email}
        onChange={(event) => setForm({ ...form, email: event.target.value })}
        required
        maxLength={256}
        aria-invalid={fieldError(fieldErrors, 'Email').length > 0}
        aria-describedby="register-email-errors"
      />
      <FieldErrors id="register-email-errors" errors={fieldError(fieldErrors, 'Email')} />
      <label className="form-label mt-3" htmlFor="displayName">
        Nome visualizzato
      </label>
      <input
        id="displayName"
        className="form-control"
        value={form.displayName}
        onChange={(event) => setForm({ ...form, displayName: event.target.value })}
        maxLength={120}
        aria-invalid={fieldError(fieldErrors, 'DisplayName').length > 0}
        aria-describedby="display-name-errors"
      />
      <FieldErrors id="display-name-errors" errors={fieldError(fieldErrors, 'DisplayName')} />
      <label className="form-label mt-3" htmlFor="password">
        Password
      </label>
      <input
        id="password"
        type="password"
        minLength={8}
        maxLength={128}
        className="form-control"
        value={form.password}
        onChange={(event) => setForm({ ...form, password: event.target.value })}
        required
        aria-invalid={fieldError(fieldErrors, 'Password').length > 0}
        aria-describedby="register-password-errors"
      />
      <FieldErrors id="register-password-errors" errors={fieldError(fieldErrors, 'Password')} />
      <div className="form-check mt-3">
        <input
          id="termsAccepted"
          type="checkbox"
          className="form-check-input"
          checked={form.termsAccepted}
          onChange={(event) =>
            setForm({
              ...form,
              termsAccepted: event.target.checked,
              privacyNoticeAcknowledged: event.target.checked,
            })
          }
          required
          aria-invalid={fieldError(fieldErrors, 'TermsAccepted').length > 0}
          aria-describedby="legal-errors"
        />
        <label className="form-check-label" htmlFor="termsAccepted">
          Ho letto l'<Link to="/privacy">Informativa Privacy</Link> e accetto i{' '}
          <Link to="/terms">Termini di utilizzo</Link>.
        </label>
      </div>
      <FieldErrors id="legal-errors" errors={fieldError(fieldErrors, 'TermsAccepted')} />
      <FieldErrors
        id="legal-privacy-errors"
        errors={fieldError(fieldErrors, 'PrivacyNoticeAcknowledged')}
      />
    </AccountForm>
  );
}

export function Profile() {
  const navigate = useNavigate();
  const { session, loading, sessionError, logout, refresh } = usePublicAccount();
  const [profile, setProfile] = useState<PublicAccountSummary | null>(session.account ?? null);
  const [displayName, setDisplayName] = useState(session.account?.displayName ?? '');
  const [email, setEmail] = useState(session.account?.email ?? '');
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});

  useEffect(() => {
    if (!session.authenticated) return;
    void getPublicProfile()
      .then((value) => {
        setProfile(value);
        setDisplayName(value.displayName);
        setEmail(value.email);
      })
      .catch((requestError) => setError(errorMessage(requestError)));
  }, [session.authenticated]);

  if (loading)
    return (
      <main className="account-page">
        <h1 className="visually-hidden">Profilo</h1>
        <p role="status">Caricamento...</p>
      </main>
    );
  if (!session.authenticated || !profile)
    return (
      <main className="account-page">
        <h1 className="visually-hidden">Profilo</h1>
        {sessionError ? (
          <p role="alert">Impossibile verificare l'accesso all'account. Riprova tra poco.</p>
        ) : (
          <p>
            Accedi per visualizzare il profilo. <Link to="/account/login">Accedi</Link>
          </p>
        )}
      </main>
    );

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setError('');
    setMessage('');
    setFieldErrors({});
    try {
      const updated = await updatePublicProfile({ displayName, email });
      setProfile(updated);
      if (updated.emailConfirmationRequired && !updated.emailConfirmed) {
        void navigate('/account/check-email', { state: { email: updated.email } });
        return;
      }
      await refresh();
      setMessage('Profilo aggiornato.');
    } catch (requestError) {
      const nextFieldErrors = getApiFieldErrors(requestError);
      setFieldErrors(nextFieldErrors);
      setError(Object.keys(nextFieldErrors).length ? '' : errorMessage(requestError));
    }
  };

  return (
    <AccountForm
      title={`Profilo di ${profile.username}`}
      submitLabel="Salva"
      onSubmit={submit}
      error={error}
      message={message}
    >
      <label className="form-label" htmlFor="displayName">
        Nome visualizzato
      </label>
      <input
        id="displayName"
        className="form-control"
        value={displayName}
        onChange={(event) => setDisplayName(event.target.value)}
        required
        maxLength={120}
        aria-invalid={fieldError(fieldErrors, 'DisplayName').length > 0}
        aria-describedby="profile-display-name-errors"
      />
      <FieldErrors
        id="profile-display-name-errors"
        errors={fieldError(fieldErrors, 'DisplayName')}
      />
      <label className="form-label mt-3" htmlFor="email">
        Email
      </label>
      <input
        id="email"
        type="email"
        className="form-control"
        value={email}
        onChange={(event) => setEmail(event.target.value)}
        required
        maxLength={256}
        aria-invalid={fieldError(fieldErrors, 'Email').length > 0}
        aria-describedby="profile-email-errors"
      />
      <FieldErrors id="profile-email-errors" errors={fieldError(fieldErrors, 'Email')} />
      {profile.emailConfirmationRequired && <p>Se cambi email, dovrai confermare il nuovo indirizzo prima di accedere nuovamente.</p>}
      <button
        type="button"
        className="btn btn-outline-secondary me-2 mt-4"
        onClick={() => void logout()}
      >
        Esci
      </button>
    </AccountForm>
  );
}

