import { beforeEach, describe, expect, it, vi } from 'vitest';
import { frontendEndpoints, get } from '@morwalpizvideo/services';
import { loadMatchesWithChannels } from '../services/shootingItaVideoService';

vi.mock('@morwalpizvideo/services', async importOriginal => ({
    ...(await importOriginal<typeof import('@morwalpizvideo/services')>()),
    get: vi.fn(),
}));

const mockedGet = vi.mocked(get);

describe('shootingItaVideoService', () => {
    beforeEach(() => {
        mockedGet.mockReset();
    });

    it('keeps a match when any VideoRef belongs to a returned Shooting ITA channel', async () => {
        const videoRefs = [
            { youtubeId: 'not-a-shooting-video', channelIds: ['other-channel'] },
            { youtubeId: 'shooting-video', channelIds: ['shooting-channel'] },
        ];
        mockedGet.mockImplementation((url: string) => {
            if (url === frontendEndpoints.SHIT_MATCHES) {
                return Promise.resolve({ data: [{ matchId: 'match-1', videoRefs }] });
            }

            return Promise.resolve([{ channelId: 'shooting-channel', channelName: 'Shooting ITA' }]);
        });

        const result = await loadMatchesWithChannels();

        expect(result.matches).toHaveLength(1);
        expect(result.matches[0].owner.channelId).toBe('shooting-channel');
        expect(result.matches[0].videoRefs).toEqual(videoRefs);
    });
});