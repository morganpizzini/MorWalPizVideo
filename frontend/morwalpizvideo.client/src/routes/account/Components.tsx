import { useEffect, useState, type FormEvent, type ReactNode } from 'react';
import { Link, useNavigate } from 'react-router';
import {
  getPublicProfile,
  loginPublicAccount,
  registerPublicAccount,
  updatePublicProfile,
} from '@morwalpizvideo/services';
import { getApiFieldErrors } from '@morwalpizvideo/services';
import type { PublicAccountSummary } from '@morwalpizvideo/models';
import { usePublicAccount } from '../layout/account';
import './style.scss';

function errorMessage(error: unknown): string {
  return error instanceof Error ? error.message : 'Unable to complete the request.';
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
  const { refresh } = usePublicAccount();
  const [identifier, setIdentifier] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setError('');
    setFieldErrors({});
    try {
      await loginPublicAccount({ identifier, password });
      await refresh();
      void navigate('/account/profile');
    } catch (requestError) {
      const nextFieldErrors = getApiFieldErrors(requestError);
      setFieldErrors(nextFieldErrors);
      setError(Object.keys(nextFieldErrors).length ? '' : errorMessage(requestError));
    }
  };

  return (
    <AccountForm title="Accedi" submitLabel="Accedi" onSubmit={submit} error={error}>
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
      <p className="mt-3 mb-0">
        Non hai un account? <Link to="/account/register">Registrati</Link>
      </p>
    </AccountForm>
  );
}

export function Register() {
  const navigate = useNavigate();
  const { refresh } = usePublicAccount();
  const [form, setForm] = useState({ username: '', email: '', password: '', displayName: '' });
  const [error, setError] = useState('');
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setError('');
    setFieldErrors({});
    try {
      await registerPublicAccount(form);
      await refresh();
      void navigate('/account/profile');
    } catch (requestError) {
      const nextFieldErrors = getApiFieldErrors(requestError);
      setFieldErrors(nextFieldErrors);
      setError(Object.keys(nextFieldErrors).length ? '' : errorMessage(requestError));
    }
  };

  return (
    <AccountForm title="Crea account" submitLabel="Registrati" onSubmit={submit} error={error}>
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
        minLength={12}
        maxLength={128}
        className="form-control"
        value={form.password}
        onChange={(event) => setForm({ ...form, password: event.target.value })}
        required
        aria-invalid={fieldError(fieldErrors, 'Password').length > 0}
        aria-describedby="register-password-errors"
      />
      <FieldErrors id="register-password-errors" errors={fieldError(fieldErrors, 'Password')} />
    </AccountForm>
  );
}

export function Profile() {
  const { session, loading, logout } = usePublicAccount();
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

  if (loading) return <p role="status">Caricamento...</p>;
  if (!session.authenticated || !profile)
    return (
      <p>
        Accedi per visualizzare il profilo. <Link to="/account/login">Accedi</Link>
      </p>
    );

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setError('');
    setMessage('');
    setFieldErrors({});
    try {
      const updated = await updatePublicProfile({ displayName, email });
      setProfile(updated);
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
      <button
        type="button"
        className="btn btn-outline-secondary ms-2"
        onClick={() => void logout()}
      >
        Esci
      </button>
    </AccountForm>
  );
}

function AccountForm({
  title,
  submitLabel,
  onSubmit,
  error,
  message,
  children,
}: {
  title: string;
  submitLabel: string;
  onSubmit: (event: FormEvent) => void;
  error: string;
  message?: string;
  children: ReactNode;
}) {
  return (
    <section className="account-card card card-no-float mx-auto" style={{ maxWidth: 480 }}>
      <div className="card-body">
        <h2>{title}</h2>
        <form onSubmit={onSubmit}>
          {children}
          {error && (
            <div className="alert alert-danger mt-3" role="alert">
              {error}
            </div>
          )}
          {message && (
            <div className="alert alert-success mt-3" role="status">
              {message}
            </div>
          )}
          <button type="submit" className="btn btn-primary mt-4">
            {submitLabel}
          </button>
        </form>
      </div>
    </section>
  );
}
