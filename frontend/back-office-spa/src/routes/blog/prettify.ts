import type { BlogBlock, BlogDocument, BlogText } from '@morwalpizvideo/models';

function textFromNode(node: BlogText): string {
  if (node.type === 'hardBreak') return '\n';
  return node.text ?? (node.content ?? []).map(textFromNode).join('');
}

function textFromBlock(block: BlogBlock): string {
  if (block.type === 'richText') return textFromNode(block.richText ?? { type: 'doc' });
  if (block.type === 'columns')
    return (block.columns ?? []).flatMap(column => column.map(textFromBlock)).join('\n');
  return block.text ?? '';
}

export function extractBlogContent(document: BlogDocument): string {
  return document.blocks.map(textFromBlock).filter(Boolean).join('\n\n');
}

export function applyPrettifiedContent(document: BlogDocument, result: string): BlogDocument {
  const paragraphs = result
    .split(/\r?\n/)
    .reduce<string[][]>((groups, line) => {
      if (line.trim()) {
        if (!groups.length) groups.push([]);
        groups.at(-1)!.push(line);
      } else if (groups.at(-1)?.length) {
        groups.push([]);
      }
      return groups;
    }, [])
    .filter(group => group.length);
  const richText = {
    type: 'doc' as const,
    content: paragraphs.map(lines => ({
      type: 'paragraph' as const,
      content: [{ type: 'text' as const, text: lines.join('\n') }],
    })),
  };
  const firstRichText = document.blocks.findIndex(block => block.type === 'richText');
  const block = {
    id: firstRichText >= 0 ? document.blocks[firstRichText].id : crypto.randomUUID(),
    type: 'richText' as const,
    richText,
  };
  const blocks =
    firstRichText >= 0
      ? document.blocks.map((item, index) => (index === firstRichText ? block : item))
      : [block, ...document.blocks];
  return { ...document, blocks };
}
