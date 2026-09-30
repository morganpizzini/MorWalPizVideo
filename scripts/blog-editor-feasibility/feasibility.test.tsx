import type { Config, Data } from '@puckeditor/core';
import { Render } from '@puckeditor/core';
import type { JSONContent } from '@tiptap/core';
import { generateHTML, generateJSON } from '@tiptap/html/server';
import { EditorContent, useEditor } from '@tiptap/react';
import StarterKit from '@tiptap/starter-kit';
import { renderToString } from 'react-dom/server';
import { describe, expect, it } from 'vitest';

interface CanonicalDocument {
  version: 1;
  blocks: CanonicalBlock[];
}

type CanonicalBlock =
  | { id: string; type: 'heading'; text: string }
  | { id: string; type: 'richText'; document: JSONContent };

type Components = {
  Heading: { text: string };
  RichText: { document: JSONContent };
};

const config: Config<Components> = {
  components: {
    Heading: {
      fields: { text: { type: 'text' } },
      render: ({ text }) => <h2>{text}</h2>,
    },
    RichText: {
      fields: { document: { type: 'custom', render: () => <></> } },
      render: ({ document }) => (
        <div dangerouslySetInnerHTML={{ __html: generateHTML(document, [StarterKit]) }} />
      ),
    },
  },
};

function toPuck(document: CanonicalDocument): Data<Components> {
  return {
    root: { props: {} },
    content: document.blocks.map((block) =>
      block.type === 'heading'
        ? { type: 'Heading', props: { id: block.id, text: block.text } }
        : { type: 'RichText', props: { id: block.id, document: block.document } },
    ),
  };
}

function fromPuck(data: Data<Components>): CanonicalDocument {
  return {
    version: 1,
    blocks: data.content.map((block) =>
      block.type === 'Heading'
        ? { id: block.props.id, type: 'heading', text: block.props.text }
        : { id: block.props.id, type: 'richText', document: block.props.document },
    ),
  };
}

const document: CanonicalDocument = {
  version: 1,
  blocks: [
    { id: 'heading-stable', type: 'heading', text: 'Article title' },
    {
      id: 'text-stable',
      type: 'richText',
      document: {
        type: 'doc',
        content: [{ type: 'paragraph', content: [{ type: 'text', text: 'Article body' }] }],
      },
    },
  ],
};

function EditorSsrProbe() {
  const editor = useEditor({
    extensions: [StarterKit],
    content: document.blocks[1].type === 'richText' ? document.blocks[1].document : undefined,
    immediatelyRender: false,
  });
  return <EditorContent editor={editor} />;
}

describe('isolated blog editor compatibility gate', () => {
  it('roundtrips canonical JSON through Puck without losing IDs or order', () => {
    const stored = JSON.parse(JSON.stringify(toPuck(document))) as Data<Components>;
    expect(fromPuck(stored)).toEqual(document);
    stored.content.reverse();
    expect(fromPuck(stored).blocks.map((block) => block.id)).toEqual([
      'text-stable',
      'heading-stable',
    ]);
  });

  it('renders deterministic Puck content on the server with React 19', () => {
    const render = () => renderToString(<Render config={config} data={toPuck(document)} />);
    expect(render()).toBe(render());
    expect(render()).toContain('<h2>Article title</h2>');
    expect(render()).toContain('<p>Article body</p>');
  });

  it('roundtrips Tiptap rich text through server HTML without a browser', () => {
    const block = document.blocks[1];
    if (block.type !== 'richText') throw new Error('Expected a rich text fixture');
    expect(generateJSON(generateHTML(block.document, [StarterKit]), [StarterKit])).toEqual(
      block.document,
    );
  });

  it('allows an SSR-safe Tiptap editor when immediatelyRender is false', () => {
    expect(() => renderToString(<EditorSsrProbe />)).not.toThrow();
  });
});