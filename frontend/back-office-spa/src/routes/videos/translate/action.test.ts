import { describe, expect, it, vi } from 'vitest';
import { endpoints, post } from '@morwalpizvideo/services';
import action from './action';

vi.mock('@morwalpizvideo/services', () => ({
  endpoints: { VIDEOS_TRANSLATE: '/api/Videos/Translate' },
  post: vi.fn(),
}));

describe('translate video action', () => {
  it('posts the API string-list contract', async () => {
    const formData = new FormData();
    formData.append('videoIds', 'video-one');
    formData.append('videoIds', ' video-two ');
    const request = new Request('http://localhost/videos/translate', { method: 'POST', body: formData });

    await action({ request });

    expect(post).toHaveBeenCalledWith(endpoints.VIDEOS_TRANSLATE, ['video-one', 'video-two']);
  });
});