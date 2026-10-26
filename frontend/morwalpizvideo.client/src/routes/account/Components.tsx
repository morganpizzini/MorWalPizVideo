import { useEffect, useState, type FormEvent, type ReactNode } from 'react';
import { Link, useNavigate } from 'react-router';
import {
  getPublicProfile,
  loginPublicAccount,
  registerPublicAccount,
  updatePublicProfile,
} from '@morwalpizvideo/services';
import type { PublicAccountSummary } from '@morwalpizvideo/models';
import { usePublicAccount } from '../layout/account';

function errorMessage(error: unknown): string {
  return error instanceof Error ? error.message : 'Unable to complete the request.';
}

export function Login() {
  const navigate = useNavigate();
  const { refresh } = usePublicAccount();
  const [identifier, setIdentifier] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setError('');
    try {
      await loginPublicAccount({ identifier, password });
      await refresh();
      void navigate('/account/profile');
    } catch (requestError) {
      setError(errorMessage(requestError));
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
      />
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
      />
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

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setError('');
    try {
      await registerPublicAccount(form);
      await refresh();
      void navigate('/account/profile');
    } catch (requestError) {
      setError(errorMessage(requestError));
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
      />
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
      />
      <label className="form-label mt-3" htmlFor="displayName">
        Nome visualizzato
      </label>
      <input
        id="displayName"
        className="form-control"
        value={form.displayName}
        onChange={(event) => setForm({ ...form, displayName: event.target.value })}
        required
      />
      <label className="form-label mt-3" htmlFor="password">
        Password
      </label>
      <input
        id="password"
        type="password"
        minLength={8}
        className="form-control"
        value={form.password}
        onChange={(event) => setForm({ ...form, password: event.target.value })}
        required
      />
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
    try {
      const updated = await updatePublicProfile({ displayName, email });
      setProfile(updated);
      setMessage('Profilo aggiornato.');
    } catch (requestError) {
      setError(errorMessage(requestError));
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
      />
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
    <section className="mx-auto" style={{ maxWidth: 480 }}>
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
    </section>
  );
}
