import { fireEvent, render, screen } from '@testing-library/react';
import { renderToStaticMarkup } from 'react-dom/server';
import { describe, expect, it } from 'vitest';
import { BlogRenderer } from '@morwalpiz/layout';
import type { BlogDocument, BlogImage } from '@morwalpizvideo/models';
import { fromPuck, toPuck } from './adapter';

const images: BlogImage[] = [
  {
    id: 'first',
    publicUrl: 'https://cdn.test/first.jpg',
    altText: 'First image',
    width: 1200,
    height: 800,
  },
  {
    id: 'second',
    publicUrl: 'https://cdn.test/second.jpg',
    altText: 'Second image',
    width: 800,
    height: 1200,
  },
];
const document: BlogDocument = {
  version: 1,
  blocks: [
    { id: 'title', type: 'heading', level: 3, text: 'Section' },
    {
      id: 'text',
      type: 'richText',
      richText: {
        type: 'doc',
        content: [
          {
            type: 'paragraph',
            content: [
              {
                type: 'text',
                text: '<script>alert(1)</script>',
                marks: [{ type: 'bold' }, { type: 'link', href: 'javascript:alert(1)' }],
              },
            ],
          },
        ],
      },
    },
    { id: 'image', type: 'image', imageIds: ['first'], text: 'Caption' },
    { id: 'gallery', type: 'gallery', imageIds: ['second', 'first'] },
    { id: 'carousel', type: 'carousel', imageIds: ['second', 'first'] },
    { id: 'video', type: 'video', videoId: 'abcdefghijk', text: 'Approved video' },
    {
      id: 'columns',
      type: 'columns',
      columns: [[{ id: 'column-heading', type: 'heading', text: 'Column one', level: 2 }], [], []],
    },
  ],
};
describe('shared preview and public renderer', () => {
  it('produces identical deterministic SSR before and after the editor adapter', () => {
    const canonical = renderToStaticMarkup(<BlogRenderer document={document} images={images} />);
    expect(
      renderToStaticMarkup(<BlogRenderer document={fromPuck(toPuck(document))} images={images} />)
    ).toBe(canonical);
    expect(canonical).toContain('&lt;script&gt;');
    expect(canonical).not.toContain('href="javascript:');
    expect(canonical).toContain('youtube-nocookie.com/embed/abcdefghijk');
    expect(canonical).toContain('col-12 col-md-4');
  });
  it('supports previous/next and arrow-key carousel navigation without autoplay', () => {
    render(
      <BlogRenderer document={{ version: 1, blocks: [document.blocks[4]] }} images={images} />
    );
    expect(screen.getByRole('img')).toHaveAttribute('alt', 'Second image');
    fireEvent.click(screen.getByRole('button', { name: 'Next image' }));
    expect(screen.getByRole('img')).toHaveAttribute('alt', 'First image');
    fireEvent.keyDown(screen.getByRole('region'), { key: 'ArrowLeft' });
    expect(screen.getByRole('img')).toHaveAttribute('alt', 'Second image');
  });
  it('renders one full-width column and suppresses unsupported videos and versions', () => {
    const html = renderToStaticMarkup(
      <BlogRenderer
        images={[]}
        document={{
          version: 1,
          blocks: [
            { id: 'single', type: 'columns', columns: [[]] },
            { id: 'bad', type: 'video', videoId: 'https://evil.test' },
          ],
        }}
      />
    );
    expect(html).not.toContain('col-md-6');
    expect(html).not.toContain('<iframe');
    expect(
      renderToStaticMarkup(
        <BlogRenderer
          images={[]}
          document={{ version: 2, blocks: [] } as unknown as BlogDocument}
        />
      )
    ).toBe('');
  });
});
