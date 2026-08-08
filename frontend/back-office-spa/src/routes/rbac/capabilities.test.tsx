import { beforeEach, describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { endpoints, get } from '@morwalpizvideo/services';
import { render } from '../../test/test-utils';
import { authService } from '../../services/authService';
import RbacUserDetailPage from './UsersDetailPage';
import RbacUsersPage from './UsersPage';

vi.mock('@morwalpizvideo/services', () => ({
  endpoints: {
    RBAC_USERS: 'api/rbac/users',
    RBAC_USER_DETAIL: 'api/rbac/users/{id}',
    RBAC_GROUPS: 'api/rbac/groups',
    RBAC_USER_GROUPS: 'api/rbac/users/{id}/groups',
    RBAC_USER_PERMISSIONS: 'api/rbac/users/{id}/permissions',
    USER_DETAIL: 'api/user/{id}',
    USER_STATUS: 'api/user/{id}/status',
    USER_PASSWORD_RESET: 'api/user/{id}/password/reset',
    USER_PASSWORD_SET: 'api/user/{id}/password/set',
  },
  ComposeUrl: (template: string, replacements: Record<string, string>) =>
    template.replace(/\{(.*?)\}/g, (_, key: string) => replacements[key] ?? ''),
  Delete: vi.fn(),
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
  resetCsrfToken: vi.fn(),
  setUnauthorizedHandler: vi.fn(),
}));

const user = {
  id: 'u1',
  username: 'mario',
  email: 'mario@example.test',
  isActive: true,
  groupIds: [],
  groupCodes: [],
  directPermissions: [],
  effectivePermissions: ['backoffice.access'],
  canAccessBackoffice: true,
};

describe('RBAC lifecycle capabilities', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.spyOn(authService, 'getPermissions').mockReturnValue(['users.permissions.manage']);
    vi.mocked(get).mockImplementation(async url => url === endpoints.RBAC_GROUPS ? [] : [user]);
  });

  it('hides lifecycle creation from a permissions-only manager', async () => {
    render(<RbacUsersPage />);

    expect(await screen.findByText('mario')).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Create user' })).not.toBeInTheDocument();
  });

  it('keeps grant controls but hides unrelated lifecycle controls', async () => {
    vi.mocked(get).mockImplementation(async url => url === endpoints.RBAC_GROUPS ? [] : user);
    render(<RbacUserDetailPage />);

    expect(await screen.findByRole('button', { name: 'Save groups' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Save permissions' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Save user' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Save status' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Reset password' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Delete user' })).not.toBeInTheDocument();
  });

  it('renders lifecycle detail read-only for users.view without calling group administration', async () => {
    vi.spyOn(authService, 'getPermissions').mockReturnValue(['users.view']);
    vi.mocked(get).mockResolvedValue(user);

    render(<RbacUserDetailPage />);

    expect(await screen.findByText('mario')).toBeInTheDocument();
    expect(get).toHaveBeenCalledOnce();
    expect(get).not.toHaveBeenCalledWith(endpoints.RBAC_GROUPS);
    expect(screen.queryByRole('button')).not.toBeInTheDocument();
  });

  it('shows lifecycle controls when the server returns expanded leaves', async () => {
    vi.spyOn(authService, 'getPermissions').mockReturnValue([
      'users.permissions.manage',
      'users.create',
      'users.update',
      'users.delete',
    ]);
    vi.mocked(get).mockImplementation(async url => url === endpoints.RBAC_GROUPS ? [] : user);

    render(<RbacUserDetailPage />);

    expect(await screen.findByRole('button', { name: 'Save user' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Save status' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Reset password' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Delete user' })).toBeInTheDocument();
  });
});