import { beforeEach, describe, expect, it, vi } from 'vitest';
import { getMatch, getMatchImages } from '@services/matches';
import loader from './loader';

vi.mock('@services/matches', () => ({
  getMatch: vi.fn(),
  getMatchImages: vi.fn(),
}));

const match = {
  id: 'match-1',
  title: 'Match title',
  images: [
    {
      publicUrl: 'https://cdn.test/first.jpg',
      contentType: 'image/jpeg',
      width: 1200,
      height: 800,
      altText: 'First image',
    },
    {
      publicUrl: 'https://cdn.test/second.jpg',
      contentType: 'image/jpeg',
      width: 1200,
      height: 800,
      altText: 'Second image',
    },
  ],
  videoRefs: [{ youtubeId: 'video-1' }],
};

describe('match detail loader', () => {
  beforeEach(() => vi.clearAllMocks());

  it('keeps contract image order and does not mix in the legacy image endpoint', async () => {
    vi.mocked(getMatch).mockResolvedValue(match as never);

    const result = await loader({ params: { matchId: 'match-1' } } as never);

    expect(result.images.map((image) => image.source)).toEqual([
      'https://cdn.test/first.jpg',
      'https://cdn.test/second.jpg',
    ]);
    expect(result.match.videoRefs[0].youtubeId).toBe('video-1');
    expect(getMatchImages).not.toHaveBeenCalled();
  });

  it('uses legacy image URLs when the contract has no images field', async () => {
    vi.mocked(getMatch).mockResolvedValue({ ...match, images: undefined } as never);
    vi.mocked(getMatchImages).mockResolvedValue(['https://legacy.test/image.jpg'] as never);

    const result = await loader({ params: { matchId: 'match-1' } } as never);

    expect(result.images[0].source).toBe('https://legacy.test/image.jpg');
    expect(getMatchImages).toHaveBeenCalledWith('match-1');
  });
});
