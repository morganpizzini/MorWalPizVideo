import { beforeEach, describe, expect, it } from 'vitest';
import { useAppStore } from './appStore';

describe('app store', () => {
  beforeEach(() => useAppStore.getState().reset());

  it('hydrates shell state and resets it completely', () => {
    useAppStore.getState().hydrate({
      user: { id: 'user-1', username: 'Ada', email: 'ada@example.test' },
      effectivePermissions: ['backoffice.access'],
      featureFlags: { videoBulkImportEnabled: true },
      accessibleChannels: [{ channelId: 'channel-1', channelName: 'Main', yTChannelId: 'yt-1' }],
      selectedChannelId: 'channel-1',
      sessionStatus: 'authenticated',
    });

    expect(useAppStore.getState().hydrated).toBe(true);
    expect(useAppStore.getState().user?.username).toBe('Ada');
    expect(useAppStore.getState().featureFlags.videoBulkImportEnabled).toBe(true);

    useAppStore.getState().reset();

    expect(useAppStore.getState()).toMatchObject({
      user: null,
      effectivePermissions: [],
      sessionStatus: 'unknown',
      accessibleChannels: [],
      selectedChannelId: null,
      hydrated: false,
    });
  });
});