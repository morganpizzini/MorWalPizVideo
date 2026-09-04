import { describe, expect, it } from 'vitest';
import { getRoutePermissions, permissions } from './permissions';

describe('Ask permissions', () => {
  it('protects campaign reads and forms', () => {
    expect(getRoutePermissions('ask', false)).toContain(permissions.ask.view);
    expect(getRoutePermissions('ask/create', false)).toContain(permissions.ask.create);
    expect(getRoutePermissions('ask/:id/edit', true)).toContain(permissions.ask.update);
  });

  it('uses moderation permission for submission actions', () => {
    expect(getRoutePermissions('ask/:id', true)).toContain(permissions.ask.moderate);
  });
});