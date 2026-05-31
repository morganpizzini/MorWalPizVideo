/**
 * Pure derivation helpers for the home page rails and Discover category pages.
 *
 * Sort key for "latest" and "featured" is ALWAYS max(videoRefs[].publishedAt OR videos[].publishedAt)
 * coerced through Date. Missing values sort last.
 *
 * `deriveExclusives` MUST return [] when `exclusiveCategoryId` is empty.
 * `derivePopular` sorts by sum(views) across the match's video refs;
 * missing views are treated as 0.
 */

export interface DeriveCategoryRef {
    id: string;
    title?: string;
}

export interface DeriveVideoRef {
    youtubeId: string;
    categories?: DeriveCategoryRef[];
    publishedAt?: string;
}

export interface DeriveVideo {
    youtubeId: string;
    publishedAt?: string;
}

export interface DeriveMatch {
    matchId?: string;
    id?: string;
    videoRefs?: DeriveVideoRef[];
    videos?: DeriveVideo[];
    categories?: DeriveCategoryRef[];
    creationDateTime?: string;
}

function maxPublishedAt(match: DeriveMatch): number {
    let max = 0;
    const considered: Array<string | undefined> = [];
    for (const r of match.videoRefs ?? []) considered.push(r.publishedAt);
    for (const v of match.videos ?? []) considered.push(v.publishedAt);
    considered.push(match.creationDateTime);
    for (const value of considered) {
        if (!value) continue;
        const t = Date.parse(value);
        if (!Number.isNaN(t) && t > max) max = t;
    }
    return max;
}

export function deriveLatest<T extends DeriveMatch>(matches: T[]): T[] {
    return [...matches].sort((a, b) => maxPublishedAt(b) - maxPublishedAt(a));
}

export function deriveFeatured<T extends DeriveMatch>(matches: T[], count = 5): T[] {
    return deriveLatest(matches).slice(0, count);
}

export function deriveExclusives<T extends DeriveMatch>(matches: T[], exclusiveCategoryId: string | undefined | null): T[] {
    if (!exclusiveCategoryId) return [];
    const id = exclusiveCategoryId.trim();
    if (!id) return [];
    const isExclusive = (cats?: DeriveCategoryRef[]) => cats?.some(c => c?.id === id) ?? false;
    return matches.filter(m => isExclusive(m.categories) || m.videoRefs?.some(r => isExclusive(r.categories)));
}

export function derivePopular<T extends DeriveMatch>(
    matches: T[],
    videoViewsById: Map<string, number> | Record<string, number>,
): T[] {
    const lookup = (id: string): number => {
        if (videoViewsById instanceof Map) return videoViewsById.get(id) ?? 0;
        return videoViewsById[id] ?? 0;
    };
    const score = (m: DeriveMatch) => (m.videoRefs ?? []).reduce((acc, r) => acc + lookup(r.youtubeId), 0);
    return [...matches].sort((a, b) => score(b) - score(a));
}
