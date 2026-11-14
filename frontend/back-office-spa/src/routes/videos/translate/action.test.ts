import { describe, expect, it, vi } from 'vitest';
import { endpoints, post } from '@morwalpizvideo/services';
import { createFormRequest } from '@/test/request-fixtures';
import action from './action';

vi.mock('@morwalpizvideo/services', () => ({
  endpoints: { VIDEOS_TRANSLATE: '/api/Videos/Translate' },
  post: vi.fn(),
}));

describe('translate video action', () => {
  it('posts the API string-list contract', async () => {
    const request = createFormRequest('http://localhost/videos/translate', {
      videoIds: ['video-one', ' video-two '],
    });

    await action({ request });

    expect(post).toHaveBeenCalledWith(endpoints.VIDEOS_TRANSLATE, ['video-one', 'video-two']);
  });
});