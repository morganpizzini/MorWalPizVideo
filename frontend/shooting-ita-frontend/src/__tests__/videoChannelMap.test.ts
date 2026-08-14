import { describe, it, expect } from 'vitest';
import { buildOwnerMap, resolveOwner } from '@morwalpizvideo/services';

const channels = [
    { channelId: 'C1', channelName: 'One', videos: [{ videoId: 'v1' }, { videoId: 'v2' }] },
    { channelId: 'C2', channelName: 'Two', videos: [{ videoId: 'v3' }] },
];

describe('videoChannelMap', () => {
    it('buildOwnerMap merges YTChannel.videos[] (legacy)', () => {
        const map = buildOwnerMap([], channels);
        expect(map.get('v1')?.channelName).toBe('One');
        expect(map.get('v2')?.channelName).toBe('One');
        expect(map.get('v3')?.channelName).toBe('Two');
    });

    it('buildOwnerMap overlays Video.channelId from match projections', () => {
        const matches = [
            { matchId: 'm1', videoRefs: [{ youtubeId: 'v4' }], videos: [{ youtubeId: 'v4', channelId: 'C2' }] },
        ];
        const map = buildOwnerMap(matches, channels);
        expect(map.get('v4')?.channelName).toBe('Two');
    });

    it('uses VideoRef.channelIds as the authoritative ownership relationship', () => {
        const matches = [
            { matchId: 'm1', videoRefs: [{ youtubeId: 'v5', channelIds: ['C1'] }] },
        ];
        const map = buildOwnerMap(matches, channels);

        expect(map.get('v5')?.channelName).toBe('One');
    });

    it('resolveOwner returns undefined for matches whose first videoRef is unowned', () => {
        const map = buildOwnerMap([], channels);
        const match = { matchId: 'm2', videoRefs: [{ youtubeId: 'unknown' }] };
        expect(resolveOwner(match, map)).toBeUndefined();
    });

    it('resolveOwner returns the badge for the first videoRef', () => {
        const map = buildOwnerMap([], channels);
        const match = { matchId: 'm3', videoRefs: [{ youtubeId: 'v3' }] };
        expect(resolveOwner(match, map)?.channelName).toBe('Two');
    });
});
