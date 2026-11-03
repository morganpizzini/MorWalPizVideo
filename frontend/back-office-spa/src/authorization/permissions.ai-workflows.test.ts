import { describe, expect, it } from 'vitest';
import { getRoutePermissions, permissions } from './permissions';
import { protectedRoutes } from '../router/routes';
import { adminMenuGroups } from '../layouts/adminMenu';

describe('AI workflow administration access', () => {
  it('registers a protected route with split view, edit, evaluation, and activation permissions', () => {
    expect(protectedRoutes.some(route => route.path === 'ai-workflows')).toBe(true);
    expect(getRoutePermissions('ai-workflows', false)).toEqual([
      permissions.aiPrompts.view,
      permissions.aiPrompts.edit,
      permissions.aiPrompts.evaluate,
      permissions.aiPrompts.activate,
    ]);

    const item = adminMenuGroups
      .flatMap(group => group.items)
      .find(entry => entry.path === '/ai-workflows');
    expect(item).toMatchObject({
      label: 'AI workflows',
      permissions: [
        permissions.aiPrompts.view,
        permissions.aiPrompts.edit,
        permissions.aiPrompts.evaluate,
        permissions.aiPrompts.activate,
      ],
    });
  });
});
