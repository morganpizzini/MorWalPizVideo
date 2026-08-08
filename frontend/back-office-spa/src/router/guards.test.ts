import { beforeEach, describe, expect, it, vi } from 'vitest';
import { redirect } from 'react-router';
import { authLoader } from '../router';
import { authService } from '../services/authService';
import { getRoutePermissions, permissions } from '../authorization/permissions';
import { requireBackOfficeAccess, requirePermissions, withPermission } from './guards';

describe('BackOffice route guards', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    localStorage.clear();
  });

  it('allows an authenticated session with the canonical permission', async () => {
    vi.spyOn(authService, 'validateSession').mockResolvedValue({
      userId: 'user-with-access',
      effectivePermissions: ['backoffice.access'],
    });

    await expect(requireBackOfficeAccess()).resolves.toBeNull();
    expect(localStorage.getItem('auth_user')).toBeNull();
  });

  it('redirects an authenticated session without the canonical permission', async () => {
    localStorage.setItem('auth_user', JSON.stringify({ id: 'user-without-access' }));
    vi.spyOn(authService, 'validateSession').mockResolvedValue({
      userId: 'user-without-access',
      effectivePermissions: [],
    });

    await expect(requireBackOfficeAccess()).resolves.toEqual(redirect('/forbidden'));
  });

  it('redirects the shell route away from users without the canonical permission', async () => {
    localStorage.setItem('auth_user', JSON.stringify({ id: 'user-without-access' }));
    vi.spyOn(authService, 'validateSession').mockResolvedValue({
      userId: 'user-without-access',
      effectivePermissions: [],
    });

    await expect(authLoader()).resolves.toEqual(redirect('/forbidden'));
    expect(authService.validateSession).toHaveBeenCalledOnce();
  });

  it('does not invoke a module loader when a direct URL is denied', async () => {
    vi.spyOn(authService, 'validateSession').mockResolvedValue({
      userId: 'user-without-videos',
      effectivePermissions: ['backoffice.access'],
    });
    const loader = vi.fn();
    const guardedLoader = withPermission([permissions.videos.view], loader);

    const result = await guardedLoader({} as never);

    expect(result).toEqual(redirect('/forbidden'));
    expect(loader).not.toHaveBeenCalled();
  });

  it('denies Insights before its loader runs without view or manage permission', async () => {
    vi.spyOn(authService, 'validateSession').mockResolvedValue({
      userId: 'user-without-insights',
      effectivePermissions: [permissions.backoffice.access],
    });
    const loader = vi.fn();
    const guardedLoader = withPermission(getRoutePermissions('insights', false), loader);

    const result = await guardedLoader({} as never);

    expect(result).toEqual(redirect('/forbidden'));
    expect(loader).not.toHaveBeenCalled();
  });

  it('uses server-returned effective permissions without expanding parent permissions', async () => {
    vi.spyOn(authService, 'validateSession').mockResolvedValue({
      userId: 'user-with-unexpanded-parent',
      effectivePermissions: [permissions.users.manage],
    });

    await expect(requirePermissions([permissions.users.permissionsManage]))
      .resolves.toEqual(redirect('/forbidden'));
  });

  it('accepts manage-all as the frontend global override', async () => {
    vi.spyOn(authService, 'validateSession').mockResolvedValue({
      userId: 'global-manager',
      effectivePermissions: [permissions.backoffice.manageAll],
    });

    await expect(requirePermissions([permissions.users.permissionsManage])).resolves.toBeNull();
  });

  it('maps RBAC lifecycle reads separately from permission administration', () => {
    expect(getRoutePermissions('rbac/users', false)).toContain(permissions.users.view);
    expect(getRoutePermissions('rbac/users/:id', false)).toContain(permissions.users.view);
    expect(getRoutePermissions('rbac/groups', false)).toEqual([permissions.users.permissionsManage]);
  });

  it('fails closed when a protected route module has no permission mapping', () => {
    expect(() => getRoutePermissions('future-module', false)).toThrow(/no permission mapping/i);
  });
});