import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router';
import { describe, expect, it, vi } from 'vitest';
import { Login } from './Components';

const refresh = vi.fn();

vi.mock('../layout/account', () => ({
  usePublicAccount: () => ({ refresh }),
}));

vi.mock('@morwalpizvideo/services', () => ({
  getApiFieldErrors: () => ({}),
  getPublicAccountErrorMessage: () => 'Impossibile completare la richiesta.',
  loginPublicAccount: vi.fn().mockResolvedValue({ authenticated: true }),
  getPublicProfile: vi.fn(),
  registerPublicAccount: vi.fn(),
  updatePublicProfile: vi.fn(),
}));

describe('public account login', () => {
  it('shows a registration success banner from navigation state', () => {
    const markup = renderToStaticMarkup(
      <MemoryRouter
        initialEntries={[
          {
            pathname: '/account/login',
            state: { registrationSuccess: true },
          },
        ]}
      >
        <Login />
      </MemoryRouter>
    );

    expect(markup).toContain('role="status">Registrazione completata. Ora puoi accedere.');
  });

  it('preserves returnUrl when moving from login to registration', () => {
    const markup = renderToStaticMarkup(
      <MemoryRouter
        initialEntries={['/account/login?returnUrl=%2Fblog%2Fstory%3Fpage%3D2%23comments']}
      >
        <Login />
      </MemoryRouter>
    );

    expect(markup).toContain(
      'href="/account/register?returnUrl=%2Fblog%2Fstory%3Fpage%3D2%23comments"'
    );
  });
});
