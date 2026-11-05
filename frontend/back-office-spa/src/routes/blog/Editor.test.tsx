import { act, cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent, { PointerEventsCheckLevel } from '@testing-library/user-event';
import { useState } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { BlogEditor } from './Editor';
import type { BlogDocument, BlogImage } from '@morwalpizvideo/models';

vi.hoisted(() => {
  vi.stubGlobal(
    'ResizeObserver',
    class {
      observe = vi.fn();
      unobserve = vi.fn();
      disconnect = vi.fn();
    }
  );
  vi.stubGlobal(
    'IntersectionObserver',
    class {
      observe = vi.fn();
      unobserve = vi.fn();
      disconnect = vi.fn();
    }
  );
  const storage = { getItem: vi.fn(() => null), setItem: vi.fn(), removeItem: vi.fn() };
  Object.defineProperty(window, 'localStorage', { configurable: true, value: storage });
  vi.stubGlobal('localStorage', storage);
  vi.stubGlobal('PointerEvent', MouseEvent);
  document.elementFromPoint = () => document.body;
  document.elementsFromPoint = () => [document.body];
  window.matchMedia = vi
    .fn()
    .mockReturnValue({
      matches: true,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      addListener: vi.fn(),
      removeListener: vi.fn(),
    });
});

beforeEach(() => vi.useFakeTimers({ shouldAdvanceTime: true }));
afterEach(async () => {
  cleanup();
  try {
    await act(async () => {
      await vi.runOnlyPendingTimersAsync();
    });
  } finally {
    vi.useRealTimers();
  }
});

const image: BlogImage = {
  id: 'image-1',
  publicUrl: 'https://images.example.test/photo.jpg',
  width: 640,
  height: 480,
  altText: 'A test image',
};

function ControlledEditor({
  initialDocument,
  images = [],
  onChange,
}: {
  initialDocument: BlogDocument;
  images?: BlogImage[];
  onChange?: (document: BlogDocument) => void;
}) {
  const [document, setDocument] = useState(initialDocument);
  return (
    <>
      <BlogEditor
        document={document}
        images={images}
        onChange={next => {
          onChange?.(next);
          setDocument(next);
        }}
      />
      <output data-testid="canonical-document">{JSON.stringify(document)}</output>
    </>
  );
}

describe('production Puck composition', () => {
  it('allows pointer interaction and typing in inline block editors', async () => {
    const user = userEvent.setup({
      advanceTimers: vi.advanceTimersByTime,
      // JSDOM sees Puck's injected drag styles but does not apply the editor stylesheet.
      pointerEventsCheck: PointerEventsCheckLevel.Never,
    });
    const document: BlogDocument = {
      version: 1,
      blocks: [
        { id: 'rich-empty', type: 'richText' },
        { id: 'heading-empty', type: 'heading' },
        { id: 'video-empty', type: 'video' },
      ],
    };
    const { container } = render(<ControlledEditor initialDocument={document} />);
    const heading = await waitFor(() =>
      container.querySelector<HTMLInputElement>('input[aria-label="Article heading"]')
    );
    const richText = container.querySelector<HTMLElement>(
      '.blog-editor__frame .tiptap[contenteditable="true"]'
    )!;

    await user.click(heading!);
    await user.type(heading!, 'Heading text');
    await user.click(richText);
    expect(richText).toHaveFocus();

    const settings = container.querySelector<HTMLButtonElement>(
      '[aria-label="Settings for Video block"]'
    )!;
    await user.click(settings);
    const videoId = await waitFor(() =>
      container.querySelector<HTMLInputElement>(
        '.blog-editor__settings-panel input[aria-label="YouTube video ID"]'
      )
    );
    await user.type(videoId!, 'dQw4w9WgXcQ');

    expect(heading).toHaveValue('Heading text');
    expect(screen.getByTestId('canonical-document')).toHaveTextContent('Heading text');
    expect(screen.getByTestId('canonical-document')).toHaveTextContent('dQw4w9WgXcQ');
  }, 10000);

  it('keeps canvas editor nodes mounted while the controlled parent receives edits', async () => {
    const document: BlogDocument = {
      version: 1,
      blocks: [
        {
          id: 'richText-1',
          type: 'richText',
          richText: {
            type: 'doc',
            content: [{ type: 'paragraph', content: [{ type: 'text', text: 'Editable article' }] }],
          },
        },
        { id: 'heading-1', type: 'heading', text: 'Article section', level: 2 },
        { id: 'columns-1', type: 'columns', columns: [[], [], []] },
      ],
    };
    const change = vi.fn();
    const { container } = render(
      <ControlledEditor initialDocument={document} onChange={change} />
    );
    const richText = await waitFor(() =>
      container.querySelector<HTMLElement>('.blog-editor__frame .tiptap[contenteditable="true"]')
    );
    const heading = container.querySelector<HTMLInputElement>(
      'input[aria-label="Article heading"]'
    );
    expect(richText).not.toBeNull();
    expect(heading).not.toBeNull();

    heading!.focus();
    fireEvent.change(heading!, { target: { value: 'Article section revised' } });
    expect(heading).toHaveFocus();
    expect(screen.getByTestId('canonical-document')).toHaveTextContent(
      'Article section revised'
    );
    expect(change).toHaveBeenCalled();
    expect(
      container.querySelector('.blog-editor__frame .tiptap[contenteditable="true"]')
    ).toBe(richText);

    const richFrame = container.querySelector<HTMLElement>(
      '[aria-label="Rich text block"]'
    )!;
    fireEvent.pointerDown(richText!);
    expect(richFrame.querySelector('[role="toolbar"][aria-label="Text formatting"]')).toBeTruthy();
    const linkUrl = richFrame.querySelector<HTMLInputElement>('input[aria-label="Link URL"]')!;
    fireEvent.change(linkUrl, { target: { value: 'javascript:alert(1)' } });
    expect(richFrame.querySelector('[aria-label="Apply link"]')).toBeDisabled();
    fireEvent.change(linkUrl, { target: { value: 'https://example.test/article' } });
    expect(richFrame.querySelector('[aria-label="Apply link"]')).toBeEnabled();
  });

  it('renders all empty block frames and keeps media settings local and selectable', async () => {
    const document: BlogDocument = {
      version: 1,
      blocks: [
        { id: 'rich-empty', type: 'richText' },
        { id: 'heading-empty', type: 'heading' },
        { id: 'image-empty', type: 'image' },
        { id: 'gallery-empty', type: 'gallery' },
        { id: 'carousel-empty', type: 'carousel' },
        { id: 'video-empty', type: 'video' },
        { id: 'columns-empty', type: 'columns', columns: [[], []] },
      ],
    };
    const change = vi.fn();
    const { container } = render(
      <ControlledEditor initialDocument={document} images={[image]} onChange={change} />
    );
    await waitFor(() =>
      expect(container.querySelectorAll('.blog-editor__frame')).toHaveLength(7)
    );
    expect(container.querySelector('.blog-editor__canvas-gutter')).toBeInTheDocument();
    expect(container.querySelectorAll('.blog-editor__frame')).toHaveLength(7);
    expect(screen.getByText('Add a valid 11-character YouTube video ID.')).toBeInTheDocument();
    expect(screen.getByText('Add content to a column to build this section.')).toBeInTheDocument();

    const imageFrame = container.querySelector<HTMLElement>('[aria-label="Image block"]')!;
    const settings = imageFrame.querySelector<HTMLButtonElement>(
      '[aria-label="Settings for Image block"]'
    )!;
    fireEvent.pointerDown(settings);
    const activeImageFrame = container.querySelector<HTMLElement>('[aria-label="Image block"]')!;
    const activeSettings = activeImageFrame.querySelector<HTMLButtonElement>(
      '[aria-label="Settings for Image block"]'
    )!;
    expect(activeSettings).toHaveAttribute('aria-expanded', 'true');
    const panel = activeImageFrame.querySelector<HTMLElement>('.blog-editor__settings-panel');
    expect(panel).toBeInTheDocument();
    const caption = panel!.querySelector<HTMLInputElement>('input')!;
    expect(caption).toHaveFocus();
    fireEvent.change(caption, { target: { value: 'Inline caption' } });
    expect(screen.getByTestId('canonical-document')).toHaveTextContent('"text":"Inline caption"');
    const refreshedFrame = container.querySelector<HTMLElement>('[aria-label="Image block"]')!;
    fireEvent.pointerDown(
      refreshedFrame.querySelector('[aria-label="Settings for Image block"]')!
    );
    await waitFor(() =>
      expect(container.querySelector('.blog-editor__settings-panel')).not.toBeInTheDocument()
    );
    await waitFor(() =>
      expect(
        container
          .querySelector<HTMLElement>('[aria-label="Image block"]')!
          .querySelector('[aria-label="Settings for Image block"]')
      ).toHaveFocus()
    );

    const closedFrame = container.querySelector<HTMLElement>('[aria-label="Image block"]')!;
    fireEvent.pointerDown(
      closedFrame.querySelector('[aria-label="Settings for Image block"]')!
    );
    const activePanel = container
      .querySelector<HTMLElement>('[aria-label="Image block"]')!
      .querySelector<HTMLElement>('.blog-editor__settings-panel')!;
    const imageChoice = activePanel.querySelector<HTMLInputElement>(
      'input[type="radio"][aria-label="Select image A test image"]'
    )!;
    const changesBeforeSelection = change.mock.calls.length;
    fireEvent.click(imageChoice);
    expect(change).toHaveBeenCalledTimes(changesBeforeSelection + 1);
    expect(change).toHaveBeenCalled();
    expect(screen.getByTestId('canonical-document')).toHaveTextContent('"imageIds":["image-1"]');
    expect(
      container.querySelector('[aria-label="Image block"]')
    ).toHaveClass('is-selected');
  });

  it('updates a column text child by ID without changing its sibling', async () => {
    const document: BlogDocument = {
      version: 1,
      blocks: [
        {
          id: 'columns-parent',
          type: 'columns',
          columns: [
            [
              { id: 'child-one', type: 'heading', text: 'First' },
              { id: 'child-two', type: 'heading', text: 'Second' },
            ],
            [],
          ],
        },
      ],
    };
    const { container } = render(<ControlledEditor initialDocument={document} />);
    const first = container.querySelector<HTMLInputElement>(
      'input[aria-label="Column 1 heading"]'
    )!;
    first.focus();
    fireEvent.change(first, { target: { value: 'First changed' } });
    expect(first).toHaveFocus();
    const canonical = screen.getByTestId('canonical-document').textContent ?? '';
    expect(canonical).toContain('"id":"child-one","type":"heading","text":"First changed"');
    expect(canonical).toContain('"id":"child-two","type":"heading","text":"Second"');
  });

  it('keeps invalid video data out of embeds and exposes safe settings', async () => {
    const document: BlogDocument = {
      version: 1,
      blocks: [{ id: 'unsafe-video', type: 'video', videoId: 'https://evil.test' }],
    };
    const { container } = render(<ControlledEditor initialDocument={document} />);
    await waitFor(() =>
      expect(container.querySelector('.blog-editor__frame')).toBeInTheDocument()
    );
    expect(container.querySelector('iframe')).toBeNull();
    const frame = container.querySelector<HTMLElement>('[aria-label="Video block"]')!;
    fireEvent.pointerDown(frame.querySelector('[aria-label="Settings for Video block"]')!);
    const activeFrame = container.querySelector<HTMLElement>('[aria-label="Video block"]')!;
    expect(
      activeFrame.querySelector<HTMLInputElement>(
        '.blog-editor__settings-panel input[aria-label="YouTube video ID"]'
      )
    ).toHaveValue('https://evil.test');
  });
});
