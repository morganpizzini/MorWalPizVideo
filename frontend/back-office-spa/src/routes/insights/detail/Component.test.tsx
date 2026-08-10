import { describe, expect, it } from 'vitest';
import { InsightNewsStatus } from '@morwalpizvideo/models';
import { getCommentPreview, getCommentTextParts, orderInsightNewsItems } from './Component';

describe('insight detail helpers', () => {
  it('limits collapsed source comments to 200 characters and preserves full expanded text', () => {
    const text = 'x'.repeat(240);
    expect(getCommentPreview(text, false)).toHaveLength(200);
    expect(getCommentPreview(text, true)).toBe(text);
  });

  it('returns the generating excerpt as a boldable expanded-text segment', () => {
    expect(getCommentTextParts('before excerpt after', 'excerpt', true)).toEqual(['before ', 'excerpt', ' after']);
    expect(getCommentTextParts('before excerpt after', 'excerpt', false)).toBeUndefined();
  });

  it('orders accepted insights before pending insights', () => {
    const base = { id: '', topicId: 'topic', title: '', summary: '', sourceUrl: '', sourceName: '', starRating: 0, aiRelevanceScore: 0, rankingScore: 0, discoveredAt: '2026-08-10T00:00:00Z', creationDateTime: '', platformSource: '', postId: '', videoId: '', analysisReason: '', reviewReason: '', sourceKind: 0, commentExcerpt: '', sentiment: '' };
    const ordered = orderInsightNewsItems([
      { ...base, id: 'pending', status: InsightNewsStatus.Pending },
      { ...base, id: 'accepted', status: InsightNewsStatus.Accepted },
    ]);

    expect(ordered.map(item => item.id)).toEqual(['accepted', 'pending']);
  });
});