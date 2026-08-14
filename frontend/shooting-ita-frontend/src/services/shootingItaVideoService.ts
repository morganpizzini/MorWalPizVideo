/**
 * FR-016 composition wrapper: fetches matches + channels, builds the owner map,
 * and drops matches with no videoRef owned by a returned Shooting ITA channel.
 */

import {
    get,
    frontendEndpoints,
    buildOwnerMap,
    type ChannelBadge,
    type ChannelWithVideos,
    type MatchLike,
} from '@morwalpizvideo/services';

export interface MatchWithChannel extends MatchLike {
    owner: ChannelBadge;
}

export interface LoadMatchesWithChannelsResult {
    matches: MatchWithChannel[];
    ownerMap: Map<string, ChannelBadge>;
}

export async function loadMatchesWithChannels(): Promise<LoadMatchesWithChannelsResult> {
    const [matches, channels] = await Promise.all([
        get(frontendEndpoints.SHIT_MATCHES) as Promise<{ data?: MatchLike[] } | MatchLike[] | undefined>,
        get(frontendEndpoints.SHIT_CHANNELS) as Promise<ChannelWithVideos[] | undefined>,
    ]);

    const safeMatches = Array.isArray(matches) ? matches : matches?.data ?? [];
    const safeChannels = channels ?? [];
    const ownerMap = buildOwnerMap(safeMatches, safeChannels);

    const withChannel: MatchWithChannel[] = [];
    for (const match of safeMatches) {
        const owner = (match.videoRefs ?? [])
            .map(videoRef => ownerMap.get(videoRef.youtubeId))
            .find(Boolean);
        if (!owner) continue;
        withChannel.push({ ...match, owner });
    }

    return { matches: withChannel, ownerMap };
}
