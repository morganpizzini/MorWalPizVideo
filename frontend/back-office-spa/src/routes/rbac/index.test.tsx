import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { render } from '../../test/test-utils';
import RbacManagementPage from './index';
import { ComposeUrl, endpoints, get, post, put } from '@morwalpizvideo/services';

vi.mock('@morwalpizvideo/services', () => ({
  endpoints: {
    USERS: 'api/user',
    USER_DETAIL: 'api/user/{id}',
    USER_STATUS: 'api/user/{id}/status',
    USER_PASSWORD_RESET: 'api/user/{id}/password/reset',
    USER_PASSWORD_SET: 'api/user/{id}/password/set',
    RBAC_USERS: 'api/rbac/users',
    RBAC_GROUPS: 'api/rbac/groups',
    RBAC_USER_PERMISSIONS: 'api/rbac/users/{id}/permissions',
    RBAC_USER_GROUPS: 'api/rbac/users/{id}/groups',
    RBAC_GROUPS_DETAIL: 'api/rbac/groups/{id}',
  },
  ComposeUrl: vi.fn((template: string, replacements: Record<string, string>) =>
    template.replace(/\{(.*?)\}/g, (_, key: string) => replacements[key] ?? `{${key}}`)
  ),
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
  Delete: vi.fn(),
}));

const rbacUsers = [
  {
    id: 'u1',
    username: 'mario',
    email: 'mario@example.test',
    role: 'Editor',
    isActive: true,
    lastLogin: null,
    groupIds: ['g1'],
    groupCodes: ['admin'],
    directPermissions: ['videos.write'],
    effectivePermissions: ['canaccessbackoffice', 'videos.write'],
    canAccessBackoffice: true,
  },
];

const rbacGroups = [
  {
    id: 'g1',
    code: 'admin',
    name: 'Administrators',
    description: 'Admin group',
    isActive: true,
    permissions: ['canaccessbackoffice'],
    memberCount: 1,
  },
  {
    id: 'g2',
    code: 'editors',
    name: 'Editors',
    description: 'Editors group',
    isActive: true,
    permissions: ['videos.write'],
    memberCount: 0,
  },
];

describe('RBAC management page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(get).mockImplementation(async (url: string) => {
      if (url === endpoints.RBAC_USERS) {
        return rbacUsers;
      }

      if (url === endpoints.RBAC_GROUPS) {
        return rbacGroups;
      }

      throw new Error(`Unexpected GET ${url}`);
    });
    vi.mocked(post).mockResolvedValue({});
    vi.mocked(put).mockResolvedValue({});
  });

  it('creates a user via the admin lifecycle endpoint', async () => {
    render(<RbacManagementPage />);

    await screen.findByDisplayValue('mario');
    fireEvent.change(screen.getByLabelText('Username'), { target: { value: 'luigi' } });
    fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'luigi@example.test' } });
    fireEvent.change(screen.getByLabelText('Role'), { target: { value: 'Reviewer' } });
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'TempPass!42' } });
    fireEvent.click(screen.getByRole('button', { name: 'Create user' }));

    await waitFor(() => {
      expect(post).toHaveBeenCalledWith(endpoints.USERS, {
        username: 'luigi',
        email: 'luigi@example.test',
        password: 'TempPass!42',
        role: 'Reviewer',
        isActive: true,
      });
    });
  });

  it('updates user details and active status with admin endpoints', async () => {
    render(<RbacManagementPage />);

    const row = (await screen.findByText('canaccessbackoffice')).closest('tr');
    expect(row).not.toBeNull();
    const scoped = within(row as HTMLElement);

    fireEvent.change(scoped.getByLabelText(/username for mario/i), { target: { value: 'mario-admin' } });
    fireEvent.change(scoped.getByLabelText(/email for mario/i), { target: { value: 'mario-admin@example.test' } });
    fireEvent.change(scoped.getByLabelText(/role for mario/i), { target: { value: 'Admin' } });
    fireEvent.click(scoped.getByRole('button', { name: 'Save user' }));

    await waitFor(() => {
      expect(put).toHaveBeenCalledWith(ComposeUrl(endpoints.USER_DETAIL, { id: 'u1' }), {
        username: 'mario-admin',
        email: 'mario-admin@example.test',
        role: 'Admin',
        isActive: true,
      });
    });

    fireEvent.click(scoped.getByLabelText(/active status for mario/i));
    fireEvent.click(scoped.getByRole('button', { name: 'Save status' }));

    await waitFor(() => {
      expect(put).toHaveBeenCalledWith(ComposeUrl(endpoints.USER_STATUS, { id: 'u1' }), {
        isActive: false,
      });
    });
  });

  it('invokes both admin password actions', async () => {
    render(<RbacManagementPage />);

    const row = (await screen.findByText('canaccessbackoffice')).closest('tr');
    expect(row).not.toBeNull();
    const scoped = within(row as HTMLElement);

    fireEvent.change(scoped.getByLabelText(/new password for mario/i), { target: { value: 'ResetPass!42' } });
    fireEvent.click(scoped.getByRole('button', { name: 'Reset password' }));

    await waitFor(() => {
      expect(put).toHaveBeenCalledWith(ComposeUrl(endpoints.USER_PASSWORD_RESET, { id: 'u1' }), {
        newPassword: 'ResetPass!42',
      });
    });

    fireEvent.change(scoped.getByLabelText(/new password for mario/i), { target: { value: 'SetPass!42' } });
    fireEvent.click(scoped.getByRole('button', { name: 'Set password' }));

    await waitFor(() => {
      expect(put).toHaveBeenCalledWith(ComposeUrl(endpoints.USER_PASSWORD_SET, { id: 'u1' }), {
        newPassword: 'SetPass!42',
      });
    });
  });

  it('preserves RBAC group and direct-permission saves', async () => {
    render(<RbacManagementPage />);

    const row = (await screen.findByText('canaccessbackoffice')).closest('tr');
    expect(row).not.toBeNull();
    const scoped = within(row as HTMLElement);

    const groupSelect = scoped.getByLabelText(/groups for mario/i) as HTMLSelectElement;
    Array.from(groupSelect.options).forEach((option) => {
      option.selected = option.value === 'g1' || option.value === 'g2';
    });
    fireEvent.change(groupSelect);
    fireEvent.click(scoped.getByRole('button', { name: 'Save groups' }));

    await waitFor(() => {
      expect(put).toHaveBeenCalledWith(ComposeUrl(endpoints.RBAC_USER_GROUPS, { id: 'u1' }), {
        groupIds: ['g1', 'g2'],
      });
    });

    fireEvent.change(scoped.getByLabelText(/direct permissions for mario/i), {
      target: { value: 'videos.write, diagnostics.read' },
    });
    fireEvent.click(scoped.getByRole('button', { name: 'Save permissions' }));

    await waitFor(() => {
      expect(put).toHaveBeenCalledWith(ComposeUrl(endpoints.RBAC_USER_PERMISSIONS, { id: 'u1' }), {
        permissions: ['videos.write', 'diagnostics.read'],
      });
    });
  });
});