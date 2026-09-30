import { act, cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { BlogEditor } from './Editor';
import type { BlogDocument } from '@morwalpizvideo/models';

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

describe('production Puck composition', () => {
  it('mounts the canonical rich text, heading and columns with a Tiptap field', async () => {
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
    const { container } = render(<BlogEditor document={document} images={[]} onChange={change} />);
    expect(screen.getByText('Article section')).toBeInTheDocument();
    fireEvent.click(screen.getByText('Editable article'));
    const desktopFields = container.querySelector<HTMLElement>('[class*="Sidebar--right_"]');
    expect(desktopFields).not.toBeNull();
    await waitFor(() =>
      expect(desktopFields!.querySelector('.tiptap[contenteditable="true"]')).not.toBeNull()
    );
    const textField = within(desktopFields!);
    expect(textField.getByTitle('Bold')).toBeInTheDocument();
    expect(textField.getByTitle('Apply link')).toBeDisabled();
    fireEvent.change(textField.getByLabelText('Link URL'), {
      target: { value: 'javascript:alert(1)' },
    });
    expect(textField.getByTitle('Apply link')).toBeDisabled();
    fireEvent.change(textField.getByLabelText('Link URL'), {
      target: { value: 'https://example.test/article' },
    });
    expect(textField.getByTitle('Apply link')).toBeEnabled();
  });
});
