import { describe, expect, it } from 'vitest';
import { applyPrettifiedContent, extractBlogContent } from './prettify';

describe('blog prettify helpers', () => {
  it('extracts rich text and preserves special content as text', () => {
    const document = {
      version: 1 as const,
      blocks: [
        {
          id: 'body',
          type: 'richText' as const,
          richText: {
            type: 'doc' as const,
            content: [
              {
                type: 'paragraph' as const,
                content: [
                  { type: 'text' as const, text: 'https://example.test {{name}} <code>x</code>' },
                ],
              },
            ],
          },
        },
      ],
    };
    expect(extractBlogContent(document)).toContain('https://example.test {{name}} <code>x</code>');
  });

  it('replaces the existing rich text block without changing other blocks', () => {
    const document = {
      version: 1 as const,
      blocks: [
        { id: 'body', type: 'richText' as const },
        { id: 'image', type: 'image' as const, imageIds: ['one'] },
      ],
    };
    const result = applyPrettifiedContent(document, 'First paragraph\n\nSecond paragraph');
    expect(result.blocks).toHaveLength(2);
    expect(result.blocks[1]).toEqual(document.blocks[1]);
    expect(result.blocks[0].richText?.content).toHaveLength(2);
  });
});
