import { describe, expect, it } from 'vitest';
import type { BlogDocument, BlogText } from '@morwalpizvideo/models';
import { fromPuck, fromTiptap, resizeColumns, safeBlogLink, toPuck, toTiptap } from './adapter';

describe('canonical blog adapter', () => {
  const text: BlogText = {
    type: 'doc',
    content: [
      {
        type: 'paragraph',
        content: [
          {
            type: 'text',
            text: 'Article',
            marks: [{ type: 'bold' }, { type: 'link', href: '/blog/article' }],
          },
        ],
      },
    ],
  };
  const document: BlogDocument = {
    version: 1,
    blocks: [
      { id: 'heading-1', type: 'heading', level: 3, text: 'Title' },
      {
        id: 'columns-1',
        type: 'columns',
        columns: [
          [{ id: 'text-1', type: 'richText', richText: text }],
          [{ id: 'video-1', type: 'video', videoId: 'abcdefghijk' }],
        ],
      },
      { id: 'gallery-1', type: 'carousel', imageIds: ['image-2', 'image-1'] },
    ],
  };
  it('roundtrips the canonical body without editor-specific fields', () => {
    expect(fromPuck(toPuck(document))).toEqual(document);
    expect(fromTiptap(toTiptap(text))).toEqual(text);
  });
  it('preserves reordered blocks and media order', () => {
    const data = toPuck(document);
    data.content.reverse();
    expect(fromPuck(data).blocks.map(block => block.id)).toEqual([
      'gallery-1',
      'columns-1',
      'heading-1',
    ]);
    expect(fromPuck(data).blocks[0].imageIds).toEqual(['image-2', 'image-1']);
  });
  it('resizes one to three columns without losing blocks', () => {
    const blocks = document.blocks;
    expect(resizeColumns([[blocks[0]], [blocks[1]], [blocks[2]]], 1)).toEqual([blocks]);
    expect(resizeColumns([[blocks[0]]], 3)).toEqual([[blocks[0]], [], []]);
  });
  it('rejects executable URLs and unsupported rich text', () => {
    for (const href of [
      'javascript:alert(1)',
      '//evil.test',
      '/\\evil',
      'https://user:pass@example.test',
    ])
      expect(safeBlogLink(href)).toBe(false);
    expect(safeBlogLink('https://example.test/article')).toBe(true);
    expect(() => fromTiptap({ type: 'iframe' })).toThrow();
    expect(() =>
      fromTiptap({
        type: 'text',
        text: 'bad',
        marks: [{ type: 'link', attrs: { href: 'javascript:alert(1)' } }],
      })
    ).toThrow();
  });
});
