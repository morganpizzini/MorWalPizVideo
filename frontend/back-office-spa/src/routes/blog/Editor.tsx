import { useEffect, useState } from 'react';
import { Puck, type Config } from '@puckeditor/core';
import { EditorContent, useEditor } from '@tiptap/react';
import StarterKit from '@tiptap/starter-kit';
import {
  ArrowDown,
  ArrowUp,
  Bold,
  Italic,
  List,
  ListOrdered,
  Link as LinkIcon,
  Undo2,
  Redo2,
  Trash2,
  Plus,
} from 'lucide-react';
import type {
  BlogBlock,
  BlogBlockType,
  BlogDocument,
  BlogImage,
  BlogText,
} from '@morwalpizvideo/models';
import { BlogRenderer } from '@morwalpiz/layout';
import {
  fromPuck,
  toPuck,
  fromTiptap,
  toTiptap,
  resizeColumns,
  safeBlogLink,
  type BlogComponents,
} from './adapter';
import '@puckeditor/core/puck.css';

const blockTypes: BlogBlockType[] = [
  'richText',
  'heading',
  'image',
  'gallery',
  'carousel',
  'video',
  'columns',
];

function TextEditor({ value, onChange }: { value: BlogText; onChange: (value: BlogText) => void }) {
  const [href, setHref] = useState('');
  const editor = useEditor({
    extensions: [
      StarterKit.configure({
        heading: false,
        codeBlock: false,
        horizontalRule: false,
        underline: false,
        link: {
          openOnClick: false,
          protocols: ['https'],
          defaultProtocol: 'https',
          isAllowedUri: url => safeBlogLink(url),
        },
      }),
    ],
    content: toTiptap(value),
    immediatelyRender: false,
    onUpdate: ({ editor: current }) => onChange(fromTiptap(current.getJSON())),
  });
  useEffect(() => {
    if (editor && JSON.stringify(fromTiptap(editor.getJSON())) !== JSON.stringify(value))
      editor.commands.setContent(toTiptap(value), { emitUpdate: false });
  }, [editor, value]);
  if (!editor) return <div role="status">Loading editor...</div>;
  return (
    <div>
      <div className="d-flex flex-wrap gap-1 mb-2" role="toolbar" aria-label="Text formatting">
        <button
          type="button"
          className="btn btn-sm btn-outline-secondary"
          title="Bold"
          aria-label="Bold"
          aria-pressed={editor.isActive('bold')}
          onClick={() => editor.chain().focus().toggleBold().run()}
        >
          <Bold size={16} />
        </button>
        <button
          type="button"
          className="btn btn-sm btn-outline-secondary"
          title="Italic"
          aria-label="Italic"
          aria-pressed={editor.isActive('italic')}
          onClick={() => editor.chain().focus().toggleItalic().run()}
        >
          <Italic size={16} />
        </button>
        <button
          type="button"
          className="btn btn-sm btn-outline-secondary"
          title="Bullet list"
          aria-label="Bullet list"
          onClick={() => editor.chain().focus().toggleBulletList().run()}
        >
          <List size={16} />
        </button>
        <button
          type="button"
          className="btn btn-sm btn-outline-secondary"
          title="Ordered list"
          aria-label="Ordered list"
          onClick={() => editor.chain().focus().toggleOrderedList().run()}
        >
          <ListOrdered size={16} />
        </button>
        <button
          type="button"
          className="btn btn-sm btn-outline-secondary"
          title="Undo"
          aria-label="Undo"
          onClick={() => editor.chain().focus().undo().run()}
        >
          <Undo2 size={16} />
        </button>
        <button
          type="button"
          className="btn btn-sm btn-outline-secondary"
          title="Redo"
          aria-label="Redo"
          onClick={() => editor.chain().focus().redo().run()}
        >
          <Redo2 size={16} />
        </button>
      </div>
      <EditorContent editor={editor} className="border rounded p-2 bg-white" />
      <div className="input-group mt-2">
        <input
          className="form-control"
          type="url"
          aria-label="Link URL"
          value={href}
          onChange={event => setHref(event.target.value)}
        />
        <button
          type="button"
          className="btn btn-outline-secondary"
          title="Apply link"
          aria-label="Apply link"
          disabled={!safeBlogLink(href)}
          onClick={() => editor.chain().focus().setLink({ href }).run()}
        >
          <LinkIcon size={16} />
        </button>
        <button
          type="button"
          className="btn btn-outline-secondary"
          title="Remove link"
          aria-label="Remove link"
          onClick={() => editor.chain().focus().unsetLink().run()}
        >
          <Trash2 size={16} />
        </button>
      </div>
    </div>
  );
}

function ColumnEditor({
  blocks,
  images,
  onChange,
}: {
  blocks: BlogBlock[];
  images: BlogImage[];
  onChange: (blocks: BlogBlock[]) => void;
}) {
  const [type, setType] = useState<BlogBlockType>('richText');
  const move = (index: number, direction: number) => {
    const next = [...blocks];
    const target = index + direction;
    [next[index], next[target]] = [next[target], next[index]];
    onChange(next);
  };
  return (
    <div>
      {blocks.map((block, index) => (
        <div key={block.id} className="border-bottom pb-3 mb-3">
          <div className="d-flex align-items-center gap-1 mb-2">
            <strong className="me-auto">{block.type}</strong>
            <button
              type="button"
              className="btn btn-sm"
              title="Move up"
              aria-label="Move up"
              disabled={!index}
              onClick={() => move(index, -1)}
            >
              <ArrowUp size={16} />
            </button>
            <button
              type="button"
              className="btn btn-sm"
              title="Move down"
              aria-label="Move down"
              disabled={index === blocks.length - 1}
              onClick={() => move(index, 1)}
            >
              <ArrowDown size={16} />
            </button>
            <button
              type="button"
              className="btn btn-sm"
              title="Remove block"
              aria-label="Remove block"
              onClick={() => onChange(blocks.filter(item => item.id !== block.id))}
            >
              <Trash2 size={16} />
            </button>
          </div>
          <BlockFields
            value={block}
            images={images}
            onChange={value => onChange(blocks.map(item => (item.id === block.id ? value : item)))}
          />
        </div>
      ))}
      <div className="input-group">
        <select
          className="form-select"
          aria-label="New column block"
          value={type}
          onChange={event => setType(event.target.value as BlogBlockType)}
        >
          {blockTypes
            .filter(item => item !== 'columns')
            .map(item => (
              <option key={item}>{item}</option>
            ))}
        </select>
        <button
          type="button"
          className="btn btn-outline-primary"
          title="Add block"
          aria-label="Add block"
          onClick={() => onChange([...blocks, { id: crypto.randomUUID(), type }])}
        >
          <Plus size={16} />
        </button>
      </div>
    </div>
  );
}

function BlockFields({
  value,
  images,
  onChange,
}: {
  value: BlogBlock;
  images: BlogImage[];
  onChange: (value: BlogBlock) => void;
}) {
  if (value.type === 'richText')
    return (
      <TextEditor
        value={value.richText ?? { type: 'doc', content: [{ type: 'paragraph' }] }}
        onChange={richText => onChange({ ...value, richText })}
      />
    );
  if (value.type === 'columns') {
    const columns = value.columns ?? [[], []];
    return (
      <div>
        <label className="form-label">
          Columns
          <select
            className="form-select"
            value={columns.length}
            onChange={event => {
              const count = Number(event.target.value);
              onChange({ ...value, columns: resizeColumns(columns, count) });
            }}
          >
            <option value={1}>1</option>
            <option value={2}>2</option>
            <option value={3}>3</option>
          </select>
        </label>
        {columns.map((blocks, index) => (
          <fieldset key={index} className="mb-3">
            <legend className="h6">Column {index + 1}</legend>
            <ColumnEditor
              blocks={blocks}
              images={images}
              onChange={next =>
                onChange({
                  ...value,
                  columns: columns.map((column, position) => (position === index ? next : column)),
                })
              }
            />
          </fieldset>
        ))}
      </div>
    );
  }
  return (
    <div className="d-grid gap-2">
      <label className="form-label">
        {value.type === 'heading' ? 'Heading' : 'Caption / video title'}
        <input
          className="form-control"
          value={value.text ?? ''}
          maxLength={2000}
          onChange={event => onChange({ ...value, text: event.target.value })}
        />
      </label>
      {value.type === 'heading' && (
        <label>
          Level
          <select
            className="form-select"
            value={value.level ?? 2}
            onChange={event => onChange({ ...value, level: Number(event.target.value) })}
          >
            <option value={2}>H2</option>
            <option value={3}>H3</option>
            <option value={4}>H4</option>
          </select>
        </label>
      )}
      {value.type === 'video' && (
        <label>
          YouTube video ID
          <input
            className="form-control"
            value={value.videoId ?? ''}
            maxLength={11}
            onChange={event => onChange({ ...value, videoId: event.target.value })}
          />
        </label>
      )}
      {['image', 'gallery', 'carousel'].includes(value.type) && (
        <fieldset>
          <legend className="h6">Images</legend>
          {!images.length && <p>No images uploaded.</p>}
          {(value.imageIds ?? []).map((imageId, index, selected) => (
            <div key={imageId} className="d-flex gap-2 align-items-center mb-2">
              <span className="me-auto">{images.find(image => image.id === imageId)?.altText}</span>
              <button
                type="button"
                className="btn btn-sm btn-outline-secondary"
                title="Move image up"
                aria-label="Move image up"
                disabled={index === 0}
                onClick={() => {
                  const next = [...selected];
                  [next[index - 1], next[index]] = [next[index], next[index - 1]];
                  onChange({ ...value, imageIds: next });
                }}
              >
                <ArrowUp size={16} />
              </button>
              <button
                type="button"
                className="btn btn-sm btn-outline-secondary"
                title="Move image down"
                aria-label="Move image down"
                disabled={index === selected.length - 1}
                onClick={() => {
                  const next = [...selected];
                  [next[index + 1], next[index]] = [next[index], next[index + 1]];
                  onChange({ ...value, imageIds: next });
                }}
              >
                <ArrowDown size={16} />
              </button>
            </div>
          ))}
          {images.map(image => (
            <label key={image.id} className="d-flex align-items-center gap-2 mb-2">
              <input
                type={value.type === 'image' ? 'radio' : 'checkbox'}
                name={value.id}
                checked={value.imageIds?.includes(image.id) ?? false}
                disabled={
                  value.type !== 'image' &&
                  !value.imageIds?.includes(image.id) &&
                  (value.imageIds?.length ?? 0) >= 20
                }
                onChange={event =>
                  onChange({
                    ...value,
                    imageIds:
                      value.type === 'image'
                        ? [image.id]
                        : event.target.checked
                          ? [...(value.imageIds ?? []), image.id]
                          : (value.imageIds ?? []).filter(id => id !== image.id),
                  })
                }
              />
              <img
                src={image.publicUrl}
                alt=""
                width={48}
                height={36}
                style={{ objectFit: 'cover' }}
              />
              {image.altText}
            </label>
          ))}
        </fieldset>
      )}
    </div>
  );
}

export function BlogEditor({
  document,
  images,
  onChange,
}: {
  document: BlogDocument;
  images: BlogImage[];
  onChange: (document: BlogDocument) => void;
}) {
  const component = (type: BlogBlockType): Config<BlogComponents>['components']['heading'] => ({
    label: type,
    defaultProps: { block: { id: '', type, ...(type === 'columns' ? { columns: [[], []] } : {}) } },
    fields: {
      block: {
        type: 'custom',
        render: ({ value, onChange: change }) => (
          <BlockFields value={value} images={images} onChange={change} />
        ),
      },
    },
    render: ({ block, id }) => (
      <BlogRenderer document={{ version: 1, blocks: [{ ...block, id }] }} images={images} />
    ),
  });
  const config: Config<BlogComponents> = {
    components: {
      richText: component('richText'),
      heading: component('heading'),
      image: component('image'),
      gallery: component('gallery'),
      carousel: component('carousel'),
      video: component('video'),
      columns: component('columns'),
    },
  };
  return (
    <Puck
      config={config}
      data={toPuck(document)}
      onChange={value => onChange(fromPuck(value))}
      iframe={{ enabled: false }}
      overrides={{
        headerActions: () => <span className="text-secondary">Article composition</span>,
      }}
    />
  );
}
