import { describe, expect, it } from 'vitest';
import { composeShortLinkUrl } from './VideoList';

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