import { afterEach, describe, expect, it } from 'vitest';

describe('browser storage test environment', () => {
  afterEach(() => {
    window.localStorage.clear();
    window.sessionStorage.clear();
  });

  it('exposes jsdom local and session storage on recent Node versions', () => {
    window.localStorage.setItem('test-key', 'local');
    window.sessionStorage.setItem('test-key', 'session');

    expect(window.localStorage.getItem('test-key')).toBe('local');
    expect(window.sessionStorage.getItem('test-key')).toBe('session');
  });

  it('keeps router request signals compatible with the Request implementation', () => {
    expect(() =>
      new Request(window.location.href, { signal: new AbortController().signal })
    ).not.toThrow();
  });
});
