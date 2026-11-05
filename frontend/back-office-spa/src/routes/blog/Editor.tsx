import { createContext, useContext, useEffect, useId, useRef, useState } from 'react';
import { createUsePuck, Puck, type Config, type Overrides } from '@puckeditor/core';
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
  Settings,
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
import './Editor.css';
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

const blockLabels: Record<BlogBlockType, string> = {
  richText: 'Rich text',
  heading: 'Heading',
  image: 'Image',
  gallery: 'Gallery',
  carousel: 'Carousel',
  video: 'Video',
  columns: 'Columns',
};

const BlogImagesContext = createContext<BlogImage[] | null>(null);
const BlogSettingsContext = createContext<{
  openBlockId: string | null;
  setOpenBlockId: (id: string | null) => void;
} | null>(null);
type BlogPuckConfig = Config<BlogComponents>;
const useBlogPuck = createUsePuck<BlogPuckConfig>();

function useBlogImages(): BlogImage[] {
  const images = useContext(BlogImagesContext);
  if (!images) throw new Error('Blog editor image context is missing.');
  return images;
}

function useBlogSettings() {
  const settings = useContext(BlogSettingsContext);
  if (!settings) throw new Error('Blog editor settings context is missing.');
  return settings;
}

function TextEditor({
  value,
  onChange,
  onPointerDown,
  showToolbar = true,
  label = 'Article text',
}: {
  value: BlogText;
  onChange: (value: BlogText) => void;
  onPointerDown?: React.PointerEventHandler<HTMLDivElement>;
  showToolbar?: boolean;
  label?: string;
}) {
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
    <div className="blog-editor__text-editor" onPointerDown={onPointerDown}>
      {showToolbar && (
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
      )}
      <EditorContent editor={editor} className="blog-editor__rich-text" aria-label={label} />
      {showToolbar && (
        <div className="input-group input-group-sm mt-2">
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
      )}
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
            <strong className="me-auto">{blockLabels[block.type]}</strong>
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
              <option key={item} value={item}>
                {blockLabels[item]}
              </option>
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
  const imageRadioGroup = useId();
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
            aria-label="Number of columns"
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
        <label className="form-label">
          Level
          <select
            className="form-select"
            aria-label="Heading level"
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
        <label className="form-label">
          YouTube video ID
          <input
            className="form-control"
            aria-label="YouTube video ID"
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
              <span className="me-auto">
                {images.find(image => image.id === imageId)?.altText ?? 'Unavailable image'}
              </span>
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
                name={`${value.id}-${imageRadioGroup}`}
                aria-label={`Select image ${image.altText}`}
                checked={value.imageIds?.includes(image.id) ?? false}
                disabled={
                  value.type !== 'image' &&
                  !value.imageIds?.includes(image.id) &&
                  (value.imageIds?.length ?? 0) >= 20
                }
                onInput={
                  value.type === 'image'
                    ? () => onChange({ ...value, imageIds: [image.id] })
                    : undefined
                }
                onChange={
                  value.type !== 'image'
                    ? event =>
                        onChange({
                          ...value,
                          imageIds: event.target.checked
                          ? [...(value.imageIds ?? []), image.id]
                          : (value.imageIds ?? []).filter(id => id !== image.id),
                        })
                    : undefined
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

function BlockFieldsField({
  value,
  onChange,
}: {
  value: BlogBlock;
  onChange: (value: BlogBlock) => void;
}) {
  return <BlockFields value={value} images={useBlogImages()} onChange={onChange} />;
}

function useBlockUpdater(id: string) {
  const dispatch = useBlogPuck(state => state.dispatch);
  const getItemById = useBlogPuck(state => state.getItemById);
  const getSelectorForId = useBlogPuck(state => state.getSelectorForId);
  return (change: (block: BlogBlock) => BlogBlock) => {
    const current = getItemById(id);
    const selector = getSelectorForId(id);
    if (!current || !selector) return;
    const nextBlock = change(current.props.block);
    dispatch({
      type: 'replace',
      destinationIndex: selector.index,
      destinationZone: selector.zone ?? 'root',
      data: {
        ...current,
        props: {
          ...current.props,
          id,
          block: { ...nextBlock, id, type: current.type as BlogBlockType },
        },
      },
    });
  };
}

function useBlockSelection(id: string) {
  return useBlogPuck(state => {
    const selector = state.appState.ui.itemSelector;
    return selector ? state.getItemBySelector(selector)?.props.id === id : false;
  });
}

function useSelectBlock(id: string) {
  const dispatch = useBlogPuck(state => state.dispatch);
  const getSelectorForId = useBlogPuck(state => state.getSelectorForId);
  return () => {
    const selector = getSelectorForId(id);
    if (selector) dispatch({ type: 'setUi', ui: { itemSelector: selector } });
  };
}

function hasRichText(value?: BlogText): boolean {
  if (!value) return false;
  return Boolean(value.text || value.content?.some(hasRichText));
}

function incompleteMessage(block: BlogBlock, images: BlogImage[]): string | null {
  if (block.type === 'image' || block.type === 'gallery' || block.type === 'carousel') {
    const imageIds = block.imageIds ?? [];
    if (!imageIds.some(id => images.some(image => image.id === id))) {
      return block.type === 'image'
        ? 'Choose an image to add it to the article.'
        : 'Choose one or more images to add article media.';
    }
  }
  if (block.type === 'video' && !/^[a-zA-Z0-9_-]{11}$/.test(block.videoId ?? '')) {
    return 'Add a valid 11-character YouTube video ID.';
  }
  if (block.type === 'columns' && !(block.columns ?? []).some(column => column.length)) {
    return 'Add content to a column to build this section.';
  }
  return null;
}

function InlineColumnBlock({
  block,
  parentId,
  columnIndex,
  selected,
}: {
  block: BlogBlock;
  parentId: string;
  columnIndex: number;
  selected: boolean;
}) {
  const images = useBlogImages();
  const selectParent = useSelectBlock(parentId);
  const updateParent = useBlockUpdater(parentId);
  const update = (change: (current: BlogBlock) => BlogBlock) =>
    updateParent(parent => ({
      ...parent,
      columns: (parent.columns ?? []).map(column =>
        column.map(child => (child.id === block.id ? change(child) : child))
      ),
    }));
  if (block.type === 'richText') {
    return (
      <div className="blog-editor__column-child">
        <TextEditor
          label={`Column ${columnIndex + 1} text`}
          value={block.richText ?? { type: 'doc', content: [{ type: 'paragraph' }] }}
          onChange={richText => update(child => ({ ...child, richText }))}
          showToolbar={selected}
          onPointerDown={event => {
            event.stopPropagation();
            selectParent();
          }}
        />
      </div>
    );
  }
  if (block.type === 'heading') {
    return (
      <input
        className="blog-editor__heading-input"
        aria-label={`Column ${columnIndex + 1} heading`}
        placeholder="Write a heading..."
        maxLength={2000}
        value={block.text ?? ''}
        onPointerDown={event => {
          event.stopPropagation();
          selectParent();
        }}
        onChange={event => update(child => ({ ...child, text: event.target.value }))}
      />
    );
  }
  return (
    <div className="blog-editor__column-child">
      <BlogRenderer
        document={{ version: 1, blocks: [{ ...block, id: block.id }] }}
        images={images}
      />
      {incompleteMessage(block, images) && (
        <p className="blog-editor__empty-hint mb-0">{incompleteMessage(block, images)}</p>
      )}
    </div>
  );
}

function ColumnsCanvas({
  block,
  id,
  selected,
}: {
  block: BlogBlock;
  id: string;
  selected: boolean;
}) {
  const columns = block.columns ?? [[], []];
  return (
    <div className="row g-3">
      {columns.map((children, index) => (
        <div
          key={index}
          className={`col-12 ${columns.length === 3 ? 'col-md-4' : columns.length === 2 ? 'col-md-6' : ''}`}
          style={{ minWidth: 0 }}
        >
          {children.length ? (
            children.map(child => (
              <div key={child.id} className="mb-3">
                <InlineColumnBlock
                  block={child}
                  parentId={id}
                  columnIndex={index}
                  selected={selected}
                />
              </div>
            ))
          ) : (
            <p className="blog-editor__empty-hint mb-0">Add a block to column {index + 1}.</p>
          )}
        </div>
      ))}
    </div>
  );
}

function BlockCanvas({ block, id }: { block: BlogBlock; id: string }) {
  const images = useBlogImages();
  const updateBlock = useBlockUpdater(id);
  const selected = useBlockSelection(id);
  const select = useSelectBlock(id);
  const { openBlockId, setOpenBlockId } = useBlogSettings();
  const settingsOpen = openBlockId === id;
  const settingsButton = useRef<HTMLButtonElement>(null);
  const settingsPanel = useRef<HTMLDivElement>(null);
  const wasSettingsOpen = useRef(false);
  useEffect(() => {
    if (settingsOpen) settingsPanel.current?.querySelector<HTMLElement>('input, select, button')?.focus();
    else if (wasSettingsOpen.current) settingsButton.current?.focus();
    wasSettingsOpen.current = settingsOpen;
  }, [settingsOpen]);

  const closeSettings = () => {
    setOpenBlockId(null);
  };
  const message = incompleteMessage(block, images);
  const canEditSettings = block.type !== 'richText';

  let content;
  if (block.type === 'richText') {
    content = (
      <>
        {!hasRichText(block.richText) && (
          <p className="blog-editor__empty-hint mb-2">Click in the text area to start writing.</p>
        )}
        <TextEditor
          value={block.richText ?? { type: 'doc', content: [{ type: 'paragraph' }] }}
          onChange={richText => updateBlock(current => ({ ...current, richText }))}
          showToolbar={selected}
          onPointerDown={event => {
            event.stopPropagation();
            select();
          }}
        />
      </>
    );
  } else if (block.type === 'heading') {
    const Tag = block.level === 3 ? 'h3' : block.level === 4 ? 'h4' : 'h2';
    content = (
      <Tag className="blog-editor__heading">
        <input
          className="blog-editor__heading-input"
          aria-label="Article heading"
          placeholder="Write a heading..."
          maxLength={2000}
          value={block.text ?? ''}
          onPointerDown={event => {
            event.stopPropagation();
            select();
          }}
          onChange={event => updateBlock(current => ({ ...current, text: event.target.value }))}
        />
      </Tag>
    );
  } else if (block.type === 'columns') {
    content = (
      <>
        <ColumnsCanvas block={block} id={id} selected={selected} />
        {message && <p className="blog-editor__empty-hint mb-0">{message}</p>}
      </>
    );
  } else {
    content = (
      <>
        <BlogRenderer document={{ version: 1, blocks: [{ ...block, id }] }} images={images} />
        {message && <p className="blog-editor__empty-hint mb-0">{message}</p>}
      </>
    );
  }

  return (
    <section
      className={`blog-editor__frame${selected ? ' is-selected' : ''}`}
      aria-label={`${blockLabels[block.type]} block`}
      onPointerDown={select}
    >
      <header className="blog-editor__frame-header">
        <span className="blog-editor__frame-label">{blockLabels[block.type]}</span>
        {canEditSettings && (
          <button
            ref={settingsButton}
            type="button"
            className="btn btn-sm btn-outline-secondary blog-editor__settings-trigger"
            aria-label={`Settings for ${blockLabels[block.type]} block`}
            aria-expanded={settingsOpen}
            onPointerDown={event => {
              event.stopPropagation();
              setOpenBlockId(settingsOpen ? null : id);
              select();
            }}
            onClick={event => {
              event.stopPropagation();
              if (event.detail === 0) {
                setOpenBlockId(settingsOpen ? null : id);
                select();
              }
            }}
          >
            <Settings size={15} aria-hidden="true" />
            <span>Settings</span>
          </button>
        )}
      </header>
      <div className="blog-editor__frame-body">{content}</div>
      {settingsOpen && canEditSettings && (
        <div
          ref={settingsPanel}
          className="blog-editor__settings-panel"
          role="group"
          aria-label={`${blockLabels[block.type]} settings`}
          onPointerDown={event => event.stopPropagation()}
          onKeyDown={event => {
            if (event.key === 'Escape') {
              event.preventDefault();
              closeSettings();
            }
          }}
        >
          <BlockFieldsField
            value={block}
            onChange={value => updateBlock(() => value)}
          />
          <button type="button" className="btn btn-sm btn-outline-secondary" onClick={closeSettings}>
            Close settings
          </button>
        </div>
      )}
    </section>
  );
}

function component(type: BlogBlockType): Config<BlogComponents>['components']['heading'] {
  return {
    label: blockLabels[type],
    defaultProps: {
      block: { id: '', type, ...(type === 'columns' ? { columns: [[], []] } : {}) },
    },
    fields: {
      block: {
        type: 'custom',
        render: BlockFieldsField,
      },
    },
    render: ({ block, id }) => <BlockCanvas block={{ ...block, id }} id={id} />,
  };
}

const blogConfig: BlogPuckConfig = {
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

const blogOverrides: Partial<Overrides<BlogPuckConfig>> = {
  headerActions: () => <span className="text-secondary">Article composition</span>,
  preview: ({ children }) => <div className="blog-editor__canvas-gutter">{children}</div>,
};

const initialPuckUi = { rightSideBarVisible: false };

export function BlogEditor({
  document,
  images,
  onChange,
}: {
  document: BlogDocument;
  images: BlogImage[];
  onChange: (document: BlogDocument) => void;
}) {
  const [openBlockId, setOpenBlockId] = useState<string | null>(null);
  return (
    <BlogSettingsContext.Provider value={{ openBlockId, setOpenBlockId }}>
      <BlogImagesContext.Provider value={images}>
        <Puck
          config={blogConfig}
          data={toPuck(document)}
          onChange={value => onChange(fromPuck(value))}
          iframe={{ enabled: false }}
          ui={initialPuckUi}
          overrides={blogOverrides}
        />
      </BlogImagesContext.Provider>
    </BlogSettingsContext.Provider>
  );
}
