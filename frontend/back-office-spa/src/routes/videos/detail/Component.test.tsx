import { fireEvent, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { useResolvedLoaderData } from '@/router/asyncData';
import { deleteVideoImage, uploadVideoImages } from '@morwalpizvideo/services';
import { render } from '../../../test/test-utils';
import Component from './Component';

const mockToastShow = vi.hoisted(() => vi.fn());

vi.mock('@/router/asyncData', () => ({
  useResolvedLoaderData: vi.fn(),
}));
vi.mock('@components/ToastNotification/ToastContext', async importOriginal => ({
  ...(await importOriginal<typeof import('@components/ToastNotification/ToastContext')>()),
  useToast: () => ({ show: mockToastShow }),
}));
vi.mock('@morwalpizvideo/services', () => ({
  deleteVideoImage: vi.fn(),
  uploadVideoImages: vi.fn(),
}));

const match = {
  id: 'match-1',
  title: 'Video title',
  description: 'Description',
  url: '',
  thumbnailVideoId: 'video-1',
  videoRefs: [],
  categories: [],
  contentType: 0,
  isLink: false,
  creationDateTime: '2026-01-01T00:00:00.000Z',
  shortLinks: [],
  images: [
    {
      publicUrl: 'https://cdn.test/old.jpg',
      contentType: 'image/jpeg',
      width: 800,
      height: 600,
      altText: 'Existing image',
    },
  ],
};

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(useResolvedLoaderData).mockReturnValue({ match, channels: [], categories: [] });
});

describe('video detail images', () => {
  it('uploads selected files and deletes an image using the updated contract', async () => {
    const uploadedImage = {
      publicUrl: 'https://cdn.test/new.jpg',
      contentType: 'image/jpeg',
      width: 1000,
      height: 700,
      altText: 'New image',
    };
    vi.mocked(uploadVideoImages).mockResolvedValue({ ...match, images: [uploadedImage] } as never);
    vi.mocked(deleteVideoImage).mockResolvedValue({ ...match, images: [] } as never);

    render(<Component />);
    const file = new File(['image'], 'new.jpg', { type: 'image/jpeg' });
    fireEvent.change(screen.getByLabelText('Select video images'), { target: { files: [file] } });
    fireEvent.click(screen.getByRole('button', { name: 'Upload selected images' }));

    await waitFor(() => expect(uploadVideoImages).toHaveBeenCalledWith('match-1', [file]));
    expect(screen.getByAltText('New image')).toBeInTheDocument();
    expect(mockToastShow).toHaveBeenCalledWith('Success', 'Video images uploaded successfully', {
      variant: 'success',
    });

    fireEvent.click(screen.getByRole('button', { name: 'Delete' }));
    await waitFor(() => expect(deleteVideoImage).toHaveBeenCalledWith('match-1', 0));
    await waitFor(() => expect(screen.queryByAltText('New image')).not.toBeInTheDocument());
  });
});
