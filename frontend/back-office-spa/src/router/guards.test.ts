import { beforeEach, describe, expect, it, vi } from 'vitest';
import { redirect } from 'react-router';
import { authLoader } from '../router';
import { authService } from '../services/authService';
import { requireBackOfficeAccess } from './guards';

describe('BackOffice route guards', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    localStorage.clear();
  });

  it('allows an authenticated session with the canonical permission', async () => {
    localStorage.setItem('auth_user', JSON.stringify({ id: 'user-with-access' }));
    vi.spyOn(authService, 'validateSession').mockResolvedValue({
      userId: 'user-with-access',
      effectivePermissions: ['backoffice.access'],
    });

    await expect(requireBackOfficeAccess()).resolves.toBeNull();
  });

  it('redirects an authenticated session without the canonical permission', async () => {
    localStorage.setItem('auth_user', JSON.stringify({ id: 'user-without-access' }));
    vi.spyOn(authService, 'validateSession').mockResolvedValue({
      userId: 'user-without-access',
      effectivePermissions: [],
    });

    await expect(requireBackOfficeAccess()).resolves.toEqual(redirect('/'));
  });

  it('redirects the shell route away from users without the canonical permission', async () => {
    localStorage.setItem('auth_user', JSON.stringify({ id: 'user-without-access' }));
    vi.spyOn(authService, 'validateSession').mockResolvedValue({
      userId: 'user-without-access',
      effectivePermissions: [],
    });

    await expect(authLoader()).resolves.toEqual(redirect('/'));
  });
});