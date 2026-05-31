import type { LoaderFunction } from 'react-router-dom';
import { get, frontendEndpoints } from '@morwalpizvideo/services';
import { loadMatchesWithChannels } from '../../services/shootingItaVideoService';
import { derivePopular } from '../../utils/deriveCategories';

interface VideoViewRecord { youtubeId: string; views?: number }

export const popularLoader: LoaderFunction = async () => {
    const { matches } = await loadMatchesWithChannels();
    let viewsMap = new Map<string, number>();
    try {
        const videos = (await get('api/videos')) as VideoViewRecord[] | undefined;
        for (const v of videos ?? []) {
            if (v?.youtubeId) viewsMap.set(v.youtubeId, v.views ?? 0);
        }
    } catch {
        // views endpoint may not be available in dev; fall back to empty map
        viewsMap = new Map();
    }
    void frontendEndpoints; // silence unused import if endpoint is read indirectly
    return { items: derivePopular(matches, viewsMap) };
};
