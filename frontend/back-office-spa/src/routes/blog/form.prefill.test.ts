import { describe, expect, it } from 'vitest';
import { buildInsightBlogSnapshot } from './prefill';

describe('buildInsightBlogSnapshot', () => {
  it('prefills basic information and keeps the result as an editable document', () => {
    const snapshot = buildInsightBlogSnapshot({
      title: 'Insight title',
      summary: 'Insight summary',
      body: 'Generated body',
      topicId: 'topic-1',
      contentPlanId: 'plan-1',
    });

    expect(snapshot.title).toBe('Insight title');
    expect(snapshot.summary).toBe('Insight summary');
    expect(snapshot.document.blocks[0].richText?.content?.[0].content?.[0].text).toBe(
      'Generated body'
    );
    expect(snapshot.coverImageId).toBeNull();
  });
});
