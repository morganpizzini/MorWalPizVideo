// @vitest-environment jsdom
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { afterEach, expect, it, vi } from 'vitest';
import AccountMail from './Mail';

const requests = vi.hoisted(() => ({ reset: vi.fn().mockResolvedValue(undefined) }));
vi.mock('@morwalpizvideo/services', () => ({
  requestPublicPasswordReset: requests.reset,
  requestPublicEmailConfirmation: vi.fn(),
  resetPublicPassword: vi.fn(),
  confirmPublicEmail: vi.fn(),
  getPublicAccountMailErrorMessage: () => 'Mail unavailable',
}));
afterEach(cleanup);
it('the public site requests recovery for its own origin using shared localized forms', async () => {
  render(<MemoryRouter initialEntries={['/account/forgot-password']}><AccountMail /></MemoryRouter>);
  expect(screen.getByRole('heading').textContent).toBe('Recupera la password');
  fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'mail@example.test' } });
  fireEvent.submit(screen.getByRole('button', { name: 'Invia email' }).closest('form')!);
  await waitFor(() => expect(requests.reset).toHaveBeenCalledWith({ email: 'mail@example.test', site: 'public-site' }));
});
