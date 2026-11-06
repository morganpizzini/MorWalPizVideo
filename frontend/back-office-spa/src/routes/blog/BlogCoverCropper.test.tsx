import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { BlogCoverCropper } from './BlogCoverCropper';

const cropMocks = vi.hoisted(() => ({
  prepareCoverImage: vi.fn(),
}));
vi.mock('./coverImageCrop', async importOriginal => ({
  ...(await importOriginal<typeof import('./coverImageCrop')>()),
  prepareCoverImage: cropMocks.prepareCoverImage,
}));

class MockImage {
  naturalWidth = 2400;
  naturalHeight = 1600;
  onload: (() => void) | null = null;
  onerror: (() => void) | null = null;
  src = '';

  constructor() {
    Object.defineProperty(this, 'src', {
      set: () => queueMicrotask(() => this.onload?.()),
    });
  }
}

beforeEach(() => {
  vi.stubGlobal('Image', MockImage);
  Object.defineProperty(URL, 'createObjectURL', {
    configurable: true,
    value: vi.fn(() => 'blob:cover-preview'),
  });
  Object.defineProperty(URL, 'revokeObjectURL', { configurable: true, value: vi.fn() });
  cropMocks.prepareCoverImage.mockReset();
});

describe('BlogCoverCropper', () => {
  it('requires an explicit crop confirmation and allows adjustment before upload', async () => {
    const source = new File(['source'], 'mountains.png', { type: 'image/png' });
    const prepared = new File(['prepared'], 'mountains.jpg', { type: 'image/jpeg' });
    const onPrepared = vi.fn();
    const onCancel = vi.fn();
    cropMocks.prepareCoverImage.mockResolvedValue(prepared);

    const cropView = render(
      <BlogCoverCropper
        file={source}
        preparedFile={null}
        disabled={false}
        onPrepared={onPrepared}
        onCancel={onCancel}
      />
    );

    expect(await screen.findByRole('group', { name: '16 by 9 cover crop preview' })).toBeVisible();
    expect(screen.getByRole('slider', { name: 'Horizontal crop position' })).toBeInTheDocument();
    expect(screen.getByRole('slider', { name: 'Vertical crop position' })).toBeInTheDocument();
    expect(screen.getByRole('slider', { name: 'Crop zoom' })).toBeInTheDocument();
    expect(onPrepared).toHaveBeenCalledWith(null);

    fireEvent.change(screen.getByRole('slider', { name: 'Crop zoom' }), {
      target: { value: '2' },
    });
    expect(screen.getByRole('status')).toHaveTextContent('will be enlarged');

    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: 'Use this crop' }));
    });
    await waitFor(() => expect(onPrepared).toHaveBeenCalledWith(prepared));
    expect(cropMocks.prepareCoverImage).toHaveBeenCalledWith(
      source,
      expect.objectContaining({ naturalWidth: 2400, naturalHeight: 1600 }),
      expect.objectContaining({ width: 1200, height: 675 })
    );

    cropView.unmount();
    const preparedView = render(
      <BlogCoverCropper
        file={source}
        preparedFile={prepared}
        disabled={false}
        onPrepared={onPrepared}
        onCancel={onCancel}
      />
    );
    expect(await screen.findByAltText('Prepared cover preview, 1920 by 1080 pixels')).toBeVisible();
    fireEvent.click(screen.getByRole('button', { name: 'Adjust crop' }));
    expect(onPrepared).toHaveBeenLastCalledWith(null);
    preparedView.rerender(
      <BlogCoverCropper
        file={source}
        preparedFile={null}
        disabled={false}
        onPrepared={onPrepared}
        onCancel={onCancel}
      />
    );
    expect(screen.getByRole('button', { name: 'Use this crop' })).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Cancel image' }));
    expect(onCancel).toHaveBeenCalledOnce();
  });
});
