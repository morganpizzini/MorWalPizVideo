import { beforeEach, describe, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render as renderDom, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { render } from '../../test/test-utils';
import RbacManagementPage from './index';
import RbacUserCreatePage from './UserCreatePage';
import RbacUserEditPage from './UserEditPage';
import RbacUserDetailPage from './UsersDetailPage';
import RbacGroupCreatePage from './GroupCreatePage';
import RbacGroupEditPage from './GroupEditPage';
import RbacGroupDetailPage from './GroupDetailPage';
import { Delete, endpoints, get, post, put } from '@morwalpizvideo/services';

vi.mock('../../services/authService', () => ({
  authService: { getPermissions: () => ['backoffice.manageall'] },
}));

vi.mock('@morwalpizvideo/services', () => ({
  endpoints: { USERS: 'api/user', USER_DETAIL: 'api/user/{id}', USER_STATUS: 'api/user/{id}/status', USER_PASSWORD_RESET: 'api/user/{id}/password/reset', USER_PASSWORD_SET: 'api/user/{id}/password/set', RBAC_USER_DETAIL: 'api/rbac/users/{id}', RBAC_USERS: 'api/rbac/users', RBAC_USER_PERMISSIONS: 'api/rbac/users/{id}/permissions', RBAC_USER_GROUPS: 'api/rbac/users/{id}/groups', RBAC_USER_CHANNELS: 'api/rbac/users/{id}/channels', RBAC_GROUPS: 'api/rbac/groups', RBAC_GROUPS_DETAIL: 'api/rbac/groups/{id}', CHANNELS: 'api/channels' },
  ComposeUrl: (template: string, replacements: Record<string, string>) => template.replace(/\{(.*?)\}/g, (_, key: string) => replacements[key] ?? `{${key}}`),
  get: vi.fn(), post: vi.fn(), put: vi.fn(), Delete: vi.fn(),
}));

const user = { id: 'u1', username: 'mario', email: 'mario@example.test', isActive: true, lastLogin: null, groupIds: ['g1'], groupCodes: ['admin'], directPermissions: ['videos.view'], effectivePermissions: ['backoffice.access', 'videos.view'], canAccessBackoffice: true, channelIds: ['c1'] };
const group = { id: 'g1', code: 'admin', name: 'Administrators', description: 'Admin group', isActive: true, permissions: ['backoffice.access'], memberCount: 1 };
const channel = { channelId: 'c1', channelName: 'Channel One', yTChannelId: 'yt1', mine: true };

function renderRoute(path: string, element: React.ReactNode) {
  const router = createMemoryRouter([
    { path: path.replace(/u1|g1/, ':id'), element },
  ], { initialEntries: [path] });
  return renderDom(<RouterProvider router={router} />);
}

describe('RBAC workflows', () => {
  beforeEach(() => { vi.clearAllMocks(); vi.mocked(get).mockImplementation(async url => url.includes('/users/') ? user : url === endpoints.RBAC_USERS ? [user] : url === endpoints.RBAC_GROUPS ? [group] : url === endpoints.CHANNELS ? [channel] : group); vi.mocked(post).mockResolvedValue({}); vi.mocked(put).mockResolvedValue({}); vi.mocked(Delete).mockResolvedValue({}); });

  it('keeps /rbac as a workflow hub', () => {
    render(<RbacManagementPage />);
    expect(screen.getByRole('link', { name: /users accounts/i })).toHaveAttribute('href', '/rbac/users');
    expect(screen.getByRole('link', { name: /groups reusable/i })).toHaveAttribute('href', '/rbac/groups');
  });

  it('creates and edits a user through existing lifecycle endpoints', async () => {
    renderRoute('/rbac/users/create', <RbacUserCreatePage />);
    fireEvent.change(screen.getByLabelText('Username'), { target: { value: 'luigi' } }); fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'luigi@example.test' } }); fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'TempPass!42' } }); fireEvent.click(screen.getByRole('button', { name: 'Create user' }));
    await waitFor(() => expect(post).toHaveBeenCalledWith(endpoints.USERS, { username: 'luigi', email: 'luigi@example.test', password: 'TempPass!42', isActive: true }));
    cleanup();
    renderRoute('/rbac/users/u1/edit', <RbacUserEditPage />);
    fireEvent.change(await screen.findByLabelText('Username'), { target: { value: 'mario-admin' } }); fireEvent.click(screen.getByRole('button', { name: 'Save user' }));
    await waitFor(() => expect(put).toHaveBeenCalledWith('api/user/u1', { username: 'mario-admin', email: 'mario@example.test', isActive: true }));
  });

  it('updates user groups and direct permissions from user detail', async () => {
    renderRoute('/rbac/users/u1', <RbacUserDetailPage />);
    const groups = await screen.findByLabelText('Groups'); await userEvent.selectOptions(groups, 'g1'); fireEvent.click(screen.getByRole('button', { name: 'Save groups' }));
    await waitFor(() => expect(put).toHaveBeenCalledWith('api/rbac/users/u1/groups', { groupIds: ['g1'] }));
    fireEvent.change(screen.getByLabelText('Direct permissions'), { target: { value: 'VIDEOS.VIEW, diagnostics.view' } }); fireEvent.click(screen.getByRole('button', { name: 'Save permissions' }));
    await waitFor(() => expect(put).toHaveBeenCalledWith('api/rbac/users/u1/permissions', { permissions: ['videos.view', 'diagnostics.view'] }));
  });

  it('updates channel assignments from user detail as a backoffice.manageall holder', async () => {
    renderRoute('/rbac/users/u1', <RbacUserDetailPage />);
    const channels = await screen.findByLabelText('Channels'); await userEvent.selectOptions(channels, 'c1'); fireEvent.click(screen.getByRole('button', { name: 'Save channel assignments' }));
    await waitFor(() => expect(put).toHaveBeenCalledWith('api/rbac/users/u1/channels', { channelIds: ['c1'] }));
  });

  it('creates, edits and deletes groups with normalized permissions', async () => {
    renderRoute('/rbac/groups/create', <RbacGroupCreatePage />);
    fireEvent.change(screen.getByLabelText('Code'), { target: { value: 'Editors' } }); fireEvent.change(screen.getByLabelText('Name'), { target: { value: 'Editors' } }); fireEvent.change(screen.getByLabelText('Permissions'), { target: { value: 'VIDEOS.VIEW, videos.view' } }); fireEvent.click(screen.getByRole('button', { name: 'Create group' }));
    await waitFor(() => expect(post).toHaveBeenCalledWith(endpoints.RBAC_GROUPS, expect.objectContaining({ code: 'editors', permissions: ['videos.view'] })));
    cleanup();
    renderRoute('/rbac/groups/g1/edit', <RbacGroupEditPage />); fireEvent.change(await screen.findByLabelText('Name'), { target: { value: 'Platform admins' } }); fireEvent.click(screen.getByRole('button', { name: 'Save group' }));
    await waitFor(() => expect(put).toHaveBeenCalledWith('api/rbac/groups/g1', expect.objectContaining({ name: 'Platform admins', permissions: ['backoffice.access'] })));
    cleanup();
    vi.spyOn(window, 'confirm').mockReturnValue(true); renderRoute('/rbac/groups/g1', <RbacGroupDetailPage />); fireEvent.click(await screen.findByRole('button', { name: 'Delete group' }));
    await waitFor(() => expect(Delete).toHaveBeenCalledWith('api/rbac/groups/g1'));
  });

});