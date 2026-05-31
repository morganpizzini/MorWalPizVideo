import { describe, it, expect } from 'vitest';
import {
    deriveLatest,
    deriveFeatured,
    deriveExclusives,
    derivePopular,
} from '../utils/deriveCategories';

const matches = [
    { matchId: 'a', categories: [{ id: 'cat1' }], videoRefs: [{ youtubeId: 'a1', publishedAt: '2025-01-10T00:00:00Z' }] },
    { matchId: 'b', categories: [{ id: 'cat2' }], videoRefs: [{ youtubeId: 'b1', publishedAt: '2025-03-10T00:00:00Z' }] },
    { matchId: 'c', categories: [{ id: 'cat3' }], videoRefs: [{ youtubeId: 'c1', publishedAt: '2025-02-10T00:00:00Z' }] },
];

describe('deriveCategories', () => {
    it('deriveLatest sorts by max(publishedAt) desc', () => {
        const result = deriveLatest(matches).map(m => m.matchId);
        expect(result).toEqual(['b', 'c', 'a']);
    });

    it('deriveFeatured caps at the requested count', () => {
        expect(deriveFeatured(matches, 2).map(m => m.matchId)).toEqual(['b', 'c']);
    });

    it('deriveExclusives returns [] when exclusiveCategoryId is empty', () => {
        expect(deriveExclusives(matches, '')).toEqual([]);
        expect(deriveExclusives(matches, undefined)).toEqual([]);
        expect(deriveExclusives(matches, '   ')).toEqual([]);
    });

    it('deriveExclusives filters by category id (match-level OR videoRef-level)', () => {
        const out = deriveExclusives(matches, 'cat2');
        expect(out.map(m => m.matchId)).toEqual(['b']);
    });

    it('derivePopular sorts by sum(views) desc with missing as zero', () => {
        const views = new Map<string, number>([['a1', 100], ['b1', 10]]);
        const out = derivePopular(matches, views).map(m => m.matchId);
        expect(out[0]).toBe('a');
    });

    it('derivePopular accepts a plain Record', () => {
        const out = derivePopular(matches, { c1: 999 }).map(m => m.matchId);
        expect(out[0]).toBe('c');
    });
});
