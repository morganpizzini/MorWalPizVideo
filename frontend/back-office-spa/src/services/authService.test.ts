import { beforeEach, describe, expect, it, vi } from 'vitest';

const { post } = vi.hoisted(() => ({ post: vi.fn() }));

vi.mock('@morwalpizvideo/services', () => ({
  post,
  resetCsrfToken: vi.fn(),
  setUnauthorizedHandler: vi.fn(),
}));

import { authService } from './authService';

describe('authService', () => {
  beforeEach(async () => {
    post.mockReset();
    await authService.logout();
    post.mockReset();
  });

  it('shares one session validation request across concurrent callers', async () => {
    post.mockResolvedValue({
      userId: 'user-1',
      effectivePermissions: ['backoffice.access'],
    });

    const sessions = await Promise.all([
      authService.validateSession(),
      authService.validateSession(),
      authService.validateSession(),
    ]);

    expect(post).toHaveBeenCalledTimes(1);
    expect(sessions).toHaveLength(3);
    expect(sessions[0]).toEqual({
      userId: 'user-1',
      effectivePermissions: ['backoffice.access'],
    });
  });
});