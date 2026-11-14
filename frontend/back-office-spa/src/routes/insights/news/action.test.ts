import { describe, expect, it, vi } from 'vitest';
import { createFormRequest } from '@/test/request-fixtures';

const review = vi.hoisted(() => vi.fn());
vi.mock('@morwalpizvideo/services', () => ({ insightsNewsApi: { review } }));

import action from './action';

describe('insight news review action', () => {
  it('returns a failure when the API resolves an error envelope', async () => {
    review.mockRejectedValueOnce(new Error('Review was rejected'));
    const result = await action({
      request: createFormRequest('http://localhost/insights/news/news-1', {
        status: '1',
        starRating: '4',
      }),
      params: { newsId: 'news-1' },
    } as never);

    expect(result).toEqual({ success: false, errors: { generics: ['Review was rejected'] } });
  });
});