import { describe, expect, it, vi } from 'vitest';

const review = vi.hoisted(() => vi.fn());
vi.mock('@morwalpizvideo/services', () => ({ insightsNewsApi: { review } }));

import action from './action';

describe('insight news review action', () => {
  it('returns a failure when the API resolves an error envelope', async () => {
    review.mockRejectedValueOnce(new Error('Review was rejected'));
    const formData = new FormData();
    formData.set('status', '1');
    formData.set('starRating', '4');

    const result = await action({
      request: new Request('http://localhost/insights/news/news-1', { method: 'POST', body: formData }),
      params: { newsId: 'news-1' },
    } as never);

    expect(result).toEqual({ success: false, errors: { generics: ['Review was rejected'] } });
  });
});