import { describe, expect, it } from 'vitest';
import { composeShortLinkUrl, getSocialPublishingVideoId, getVideoContentLabel, shouldShowMainUrl } from './VideoList';
import type { Match } from '@morwalpizvideo/models';

describe('composeShortLinkUrl', () => {
  it('normalizes slashes around the channel base and code', () => {
    expect(composeShortLinkUrl('https://morwalpiz.com/sl/', '/abc')).toBe('https://morwalpiz.com/sl/abc');
  });

  it('does not compose an incomplete short link', () => {
    expect(composeShortLinkUrl('', 'abc')).toBeUndefined();
    expect(composeShortLinkUrl('https://morwalpiz.com/sl/', '')).toBeUndefined();
  });

  it('produces the same display and copy URL used by video detail', () => {
    const channel = { shortLinkUrl: 'https://morwalpiz.com/sl/' };
    const shortLink = { code: 'video-1' };
    const url = composeShortLinkUrl(channel.shortLinkUrl, shortLink.code);

    expect(url).toBe('https://morwalpiz.com/sl/video-1');
  });
});

describe('shouldShowMainUrl', () => {
  it('keeps the canonical content URL for empty collections', () => {
    expect(shouldShowMainUrl(0)).toBe(true);
  });
  it('hides the canonical content URL for a single video reference', () => {
    expect(shouldShowMainUrl(1)).toBe(false);
  });

  describe('getVideoContentLabel', () => {
    it.each([
      [0, 'Empty Collection (0 videos)'],
      [1, 'Single Video (1 video)'],
      [2, 'Collection (2 videos)'],
      [10, 'Collection (10 videos)'],
    ])('acknowledges %i attachments as %s', (count, label) => {
      expect(getVideoContentLabel(count)).toBe(label);
    });
  });

  it('keeps the canonical content URL for multiple references', () => {
    expect(shouldShowMainUrl(2)).toBe(true);
  });
});

describe('getSocialPublishingVideoId', () => {
  it('uses the thumbnail YouTube id instead of the aggregate database id', () => {
    const match = {
      id: 'mongo-id',
      thumbnailVideoId: 'youtube-thumbnail',
      videoRefs: [{ youtubeId: 'youtube-first' }],
    } as Match;

    expect(getSocialPublishingVideoId(match)).toBe('youtube-thumbnail');
  });

  it('falls back to the first video reference', () => {
    const match = {
      id: 'mongo-id',
      thumbnailVideoId: '',
      videoRefs: [{ youtubeId: 'youtube-first' }],
    } as Match;

    expect(getSocialPublishingVideoId(match)).toBe('youtube-first');
  });
});