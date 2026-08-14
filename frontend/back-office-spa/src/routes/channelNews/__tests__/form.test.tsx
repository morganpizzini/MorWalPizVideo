import { describe, it, expect, vi, beforeEach } from 'vitest';
import React from 'react';
import { fireEvent, screen, waitFor } from '@testing-library/react';
import { render } from '../../../test/test-utils';
import { useFetcher, useLoaderData, useNavigate } from 'react-router';
import type { ChannelNewsAdmin } from '@morwalpizvideo/models';
import { deleteChannelNewsImage, uploadChannelNewsImages } from '@morwalpizvideo/services';

vi.mock('react-router', async () => {
  const actual = await vi.importActual<typeof import('react-router')>('react-router');
  return {
    ...actual,
    useFetcher: vi.fn(),
    useLoaderData: vi.fn(),
    useNavigate: vi.fn(),
    useParams: vi.fn(() => ({ id: 'news-1' })),
  };
});

vi.mock('@morwalpizvideo/services', async () => {
  const actual = await vi.importActual<typeof import('@morwalpizvideo/services')>('@morwalpizvideo/services');
  return {
    ...actual,
    deleteChannelNewsImage: vi.fn(),
    uploadChannelNewsImages: vi.fn(),
  };
});

const mockEntity: ChannelNewsAdmin = {
  id: 'news-1',
  channelId: 'channel-1',
  title: 'Match report',
  subtitle: 'Final score',
  descriptionHtml: '<p>Existing body</p>',
  images: [
    {
      publicUrl: '/images/news.jpg',
      contentType: 'image/jpeg',
      width: 1200,
      height: 800,
      altText: 'Match',
      displayOrder: 0,
    },
  ],
  slug: 'match-report',
  status: 0,
  displayOrder: 0,
  creationDateTime: '2026-01-01T00:00:00Z',
  updatedDateTime: '2026-01-01T00:00:00Z',
};

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(useLoaderData).mockReturnValue(mockEntity);
  vi.mocked(useNavigate).mockReturnValue(vi.fn());
  vi.mocked(useFetcher).mockReturnValue({
    state: 'idle',
    data: undefined,
    Form: ({ children, ...props }: React.PropsWithChildren<Record<string, unknown>>) => (
      <form {...props}>{children}</form>
    ),
  } as unknown as ReturnType<typeof useFetcher>);
});

describe('ChannelNews form', () => {
  it('renders the WYSIWYG editor and ordered image metadata', async () => {
    const { default: Component } = await import('../form/Component');
    render(<Component />);

    expect(screen.getByTestId('channelnews-editor')).toHaveTextContent('Existing body');
    expect(screen.getByRole('img', { name: 'Match' })).toBeInTheDocument();
    expect(screen.getByText(/1200 x 800/)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Delete image' })).toBeInTheDocument();
  });

  it('keeps edited HTML in the submitted hidden field', async () => {
    const { default: Component } = await import('../form/Component');
    render(<Component />);
    const editor = screen.getByTestId('channelnews-editor');

    fireEvent.input(editor, { target: { innerHTML: '<p><strong>Updated</strong></p>' } });

    expect(screen.getByDisplayValue('<p><strong>Updated</strong></p>')).toBeInTheDocument();
  });

  it('uploads selected images and deletes an image through the service actions', async () => {
    const newImage = {
      publicUrl: '/images/news-second.jpg',
      contentType: 'image/jpeg',
      width: 800,
      height: 600,
      altText: 'Second image',
      displayOrder: 1,
    };
    vi.mocked(uploadChannelNewsImages).mockResolvedValue({ ...mockEntity, images: [...mockEntity.images, newImage] });
    vi.mocked(deleteChannelNewsImage).mockResolvedValue({ ...mockEntity, images: [] });

    const { default: Component } = await import('../form/Component');
    render(<Component />);
    const imageInput = document.querySelector('input[type="file"]') as HTMLInputElement;
    const file = new File(['image'], 'second.jpg', { type: 'image/jpeg' });

    fireEvent.change(imageInput, { target: { files: [file] } });
    expect(screen.getByText('Selected: second.jpg')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Upload selected images' }));
    await waitFor(() => expect(uploadChannelNewsImages).toHaveBeenCalledWith('news-1', [file]));
    expect(screen.getByRole('img', { name: 'Second image' })).toBeInTheDocument();

    fireEvent.click(screen.getAllByRole('button', { name: 'Delete image' })[0]);
    await waitFor(() => expect(deleteChannelNewsImage).toHaveBeenCalledWith('news-1', 0));
    expect(screen.queryByRole('img', { name: 'Match' })).not.toBeInTheDocument();
  });
});
