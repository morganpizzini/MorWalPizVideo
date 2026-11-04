import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router';
import { describe, expect, it } from 'vitest';
import type { BlogSummary } from '@morwalpizvideo/models';
import { ContentCard, createEditorialFeed, filterEditorialFeed, getMatchDestination, type IndexMatch } from './Component';

const video: IndexMatch = {
  contentId: 'video-id',
  title: 'Gara IPSC',
  description: 'Sul campo',
  categories: [{ title: 'Gare' }, { title: 'IPSC' }],
  tags: ['Training', 'Gear'],
  videoRefs: [{ youtubeId: 'video-id' }],
  shortLinks: [],
  creationDateTime: '2026-10-01T10:00:00Z',
};
const series: IndexMatch = {
  ...video,
  contentId: 'series-id',
  title: 'La serie',
  url: 'la-serie',
  videoRefs: [{ youtubeId: 'one' }, { youtubeId: 'two' }],
  tags: ['Training'],
};
const article: BlogSummary = {
  slug: 'sul-campo', title: 'Diario dal campo', summary: 'Un articolo', author: 'Morgan',
  tags: ['Training'], coverUrl: 'https://cdn.test/article.jpg', coverAlt: 'Campo di gara',
  publishedAt: '2026-10-02T10:00:00Z',
};

describe('editorial homepage', () => {
  it('mixes real articles and matches by publication date without mutating input', () => {
    const matches = [video, series];
    const feed = createEditorialFeed(matches, [article]);
    expect(feed.map((item) => item.type)).toEqual(['article', 'video', 'collection']);
    expect(matches).toEqual([video, series]);
  });

  it('preserves AND semantics across categories and tags, with type and text filters', () => {
    const feed = createEditorialFeed([video, series], [article]);
    expect(filterEditorialFeed(feed, 'all', ['Gare', 'IPSC'], ['Training', 'Gear'], '')).toEqual([{ type: 'video', match: video }]);
    expect(filterEditorialFeed(feed, 'article', [], ['Training'], 'DIARIO')).toEqual([{ type: 'article', article }]);
    expect(filterEditorialFeed(feed, 'all', ['Missing'], [], '')).toEqual([]);
    expect(filterEditorialFeed(feed, 'collection', [], [], '')).toEqual([{ type: 'collection', match: series }]);
  });

  it('preserves branded and absolute short links and video fallback URLs', () => {
    expect(getMatchDestination(video)).toBe('https://youtu.be/video-id');
    expect(getMatchDestination({ ...video, shortLinks: [{ target: 'video-id', code: 'tracked' }] })).toBe('https://shorts.morwalpiz.com/tracked');
    expect(getMatchDestination({ ...video, shortLinks: [{ target: 'video-id', code: 'https://short.test/tracked' }] })).toBe('https://short.test/tracked');
    expect(getMatchDestination(series)).toBe('/matches/la-serie');
  });

  it('server-renders series episode badges and an internal CTA, without external targets', () => {
    const html = renderToStaticMarkup(<MemoryRouter><ContentCard item={{ type: 'collection', match: series }} /></MemoryRouter>);
    expect(html).toContain('href="/matches/la-serie"');
    expect(html).toContain('2 episodi');
    expect(html).toContain('Esplora la serie');
    expect(html).toContain('episode-count');
    expect(html).not.toContain('target="_blank"');
    expect(html).toContain('aria-expanded="false"');
  });

  it('server-renders clickable videos and articles with the correct destinations', () => {
    const html = renderToStaticMarkup(<MemoryRouter><ContentCard item={{ type: 'video', match: video }} /><ContentCard item={{ type: 'article', article }} /></MemoryRouter>);
    expect(html).toContain('href="https://youtu.be/video-id"');
    expect(html).toContain('target="_blank"');
    expect(html).toContain('href="/blog/sul-campo"');
    expect(html).toContain('alt="Campo di gara"');
    expect(html).toContain('loading="lazy"');
  });
});