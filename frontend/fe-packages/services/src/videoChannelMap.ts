/**
 * Client-side video↔channel ownership join (FR-016 / FR-017).
 *
 * Builds a Map<youtubeId, ChannelBadge> from the union of:
 *   (a) `Video.channelId` projected onto each match's `videoRefs[*]` (post-backfill)
 *   (b) `YTChannel.videos[*].videoId` (legacy fallback, source of truth today)
 */

import { get } from './apiService';
import frontendEndpoints from './endpoints-frontend';

export const MORWALPIZ_CHANNEL_ID = 'UCQRMDE-gxoD43w2q-QCVmIA';

export interface ChannelBadge {
    channelId: string;
    channelName: string;
    avatarUrl?: string;
}

export interface ChannelWithVideos {
    channelId: string;
    channelName: string;
    yTChannelId?: string;
    avatarUrl?: string;
    mine?: boolean;
    isSHIT?: boolean;
    videos?: Array<{ videoId: string }>;
}

export interface VideoLike {
    youtubeId: string;
    channelId?: string;
    title?: string;
    description?: string;
    publishedAt?: string;
    thumbnailUrl?: string;
}

export interface VideoRefLike {
    youtubeId: string;
    title?: string;
    description?: string;
    publishedAt?: string;
    thumbnailUrl?: string;
    channelIds?: string[];
}

export interface MatchLike {
    matchId?: string;
    id?: string;
    videoRefs?: VideoRefLike[];
    videos?: VideoLike[];
}

export async function loadChannelMap(): Promise<Map<string, ChannelBadge>> {
    const channels = (await get(frontendEndpoints.CHANNELS)) as ChannelWithVideos[] | undefined;
    const map = new Map<string, ChannelBadge>();
    for (const channel of channels ?? []) {
        if (!channel.mine || channel.channelId !== MORWALPIZ_CHANNEL_ID) continue;
        const badge: ChannelBadge = {
            channelId: channel.channelId,
            channelName: channel.channelName,
            avatarUrl: channel.avatarUrl,
        };
        map.set(channel.channelId, badge);
    }
    return map;
}

export function buildOwnerMap(
    matches: MatchLike[],
    channels: ChannelWithVideos[]
): Map<string, ChannelBadge> {
    const owners = new Map<string, ChannelBadge>();

    // Legacy channel video arrays remain a fallback for older records.
    for (const channel of channels ?? []) {
        const badge: ChannelBadge = {
            channelId: channel.channelId,
            channelName: channel.channelName,
            avatarUrl: channel.avatarUrl,
        };
        for (const v of channel.videos ?? []) {
            if (v?.videoId) owners.set(v.videoId, badge);
        }
    }

    // (a) Post-backfill: Video.channelId on match projections — overrides legacy when set
    const byChannelId = new Map<string, ChannelBadge>();
    for (const channel of channels ?? []) {
        byChannelId.set(channel.channelId, {
            channelId: channel.channelId,
            channelName: channel.channelName,
            avatarUrl: channel.avatarUrl,
        });
    }
    for (const match of matches ?? []) {
        for (const video of match.videoRefs ?? []) {
            for (const channelId of video.channelIds ?? []) {
                const badge = byChannelId.get(channelId);
                if (badge) owners.set(video.youtubeId, badge);
            }
        }
        for (const v of match.videos ?? []) {
            if (v?.channelId) {
                const badge = byChannelId.get(v.channelId);
                if (badge) owners.set(v.youtubeId, badge);
            }
        }
    }

    return owners;
}

export function resolveOwner(
    match: MatchLike,
    ownerMap: Map<string, ChannelBadge>
): ChannelBadge | undefined {
    const firstRef = match.videoRefs?.[0]?.youtubeId;
    if (!firstRef) return undefined;
    return ownerMap.get(firstRef);
}
