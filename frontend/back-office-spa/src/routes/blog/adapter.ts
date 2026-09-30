import type { Data } from '@puckeditor/core';
import type { JSONContent } from '@tiptap/core';
import type { BlogBlock, BlogBlockType, BlogDocument, BlogText } from '@morwalpizvideo/models';

export type BlogComponents = Record<BlogBlockType, { block: BlogBlock }>;
const textTypes = new Set([
  'doc',
  'paragraph',
  'text',
  'bulletList',
  'orderedList',
  'listItem',
  'blockquote',
  'hardBreak',
]);
const markTypes = new Set(['bold', 'italic', 'strike', 'code', 'link']);
export function safeBlogLink(value: string): boolean {
  if (Array.from(value).some(character => character === '\\' || character.charCodeAt(0) <= 32))
    return false;
  if (value.startsWith('/') && !value.startsWith('//')) return true;
  try {
    const url = new URL(value);
    return url.protocol === 'https:' && !url.username && !url.password;
  } catch {
    return false;
  }
}
export function resizeColumns(columns: BlogBlock[][], count: number): BlogBlock[][] {
  const next = Array.from({ length: count }, (_, index) => columns[index] ?? []);
  if (columns.length > count)
    next[count - 1] = [...next[count - 1], ...columns.slice(count).flat()];
  return next;
}
export function toPuck(document: BlogDocument): Data<BlogComponents> {
  return {
    root: { props: {} },
    content: document.blocks.map(block => ({ type: block.type, props: { id: block.id, block } })),
  };
}
export function fromPuck(data: Data<BlogComponents>): BlogDocument {
  return {
    version: 1,
    blocks: data.content.map(item => ({ ...item.props.block, type: item.type, id: item.props.id })),
  };
}
export function toTiptap(node: BlogText): JSONContent {
  return {
    type: node.type,
    ...(node.text ? { text: node.text } : {}),
    ...(node.marks?.length
      ? {
          marks: node.marks.map(mark => ({
            type: mark.type,
            ...(mark.type === 'link' ? { attrs: { href: mark.href } } : {}),
          })),
        }
      : {}),
    ...(node.content?.length ? { content: node.content.map(toTiptap) } : {}),
  };
}
export function fromTiptap(node: JSONContent): BlogText {
  if (!node.type || !textTypes.has(node.type)) throw new Error('Unsupported rich text node.');
  if (
    node.marks?.some(
      mark =>
        !markTypes.has(mark.type) ||
        (mark.type === 'link' && !safeBlogLink(String(mark.attrs?.href ?? '')))
    )
  )
    throw new Error('Unsupported rich text mark or link.');
  return {
    type: node.type as BlogText['type'],
    ...(node.text ? { text: node.text } : {}),
    ...(node.marks?.length
      ? {
          marks: node.marks.map(mark => ({
            type: mark.type as NonNullable<BlogText['marks']>[number]['type'],
            ...(mark.type === 'link' ? { href: String(mark.attrs?.href ?? '') } : {}),
          })),
        }
      : {}),
    ...(node.content?.length ? { content: node.content.map(fromTiptap) } : {}),
  };
}
