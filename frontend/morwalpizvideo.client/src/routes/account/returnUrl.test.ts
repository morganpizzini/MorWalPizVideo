import { describe, expect, it } from 'vitest';
import {
  getAccountLoginLink,
  getPostLoginDestination,
  getSafeReturnUrl,
} from './returnUrl';

describe('account return URLs', () => {
  it('preserves local path, query, and hash after login', () => {
    const search = '?returnUrl=%2Fblog%2Fstory%3Fpage%3D2%23comments';

    expect(getSafeReturnUrl(search)).toBe('/blog/story?page=2#comments');
    expect(getPostLoginDestination(search)).toBe('/blog/story?page=2#comments');
  });

  it('rejects external destinations and authentication loops', () => {
    expect(getSafeReturnUrl('?returnUrl=https%3A%2F%2Fexample.com')).toBeUndefined();
    expect(getSafeReturnUrl('?returnUrl=%2F%2Fexample.com')).toBeUndefined();
    expect(getSafeReturnUrl('?returnUrl=%2Faccount%2Flogin')).toBeUndefined();
    expect(getPostLoginDestination('?returnUrl=%2F%2Fexample.com')).toBe('/account/profile');
  });

  it('uses the current route as returnUrl outside authentication pages', () => {
    expect(
      getAccountLoginLink({
        pathname: '/blog/story',
        search: '?page=2',
        hash: '#comments',
      })
    ).toBe('/account/login?returnUrl=%2Fblog%2Fstory%3Fpage%3D2%23comments');
  });
});
