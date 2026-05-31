/**
 * FR-016 composition wrapper: fetches matches + channels, builds the owner map,
 * and drops matches whose first videoRef has no owning channel.
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
        get(frontendEndpoints.MATCHES) as Promise<MatchLike[] | undefined>,
        get(frontendEndpoints.CHANNELS) as Promise<ChannelWithVideos[] | undefined>,
    ]);

    const safeMatches = matches ?? [];
    const safeChannels = channels ?? [];
    const ownerMap = buildOwnerMap(safeMatches, safeChannels);

    const withChannel: MatchWithChannel[] = [];
    for (const match of safeMatches) {
        const firstRef = match.videoRefs?.[0]?.youtubeId;
        if (!firstRef) continue;
        const owner = ownerMap.get(firstRef);
        if (!owner) continue;
        withChannel.push({ ...match, owner });
    }

    return { matches: withChannel, ownerMap };
}
