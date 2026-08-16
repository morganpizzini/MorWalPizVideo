import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { createMemoryRouter, RouterProvider, useFetcher, useLoaderData, useNavigate, useParams } from 'react-router';
import { deletePageImage, uploadPageImages } from '@morwalpizvideo/services';
import PageForm from './Component';

vi.mock('react-router', async () => {
  const actual = await vi.importActual<typeof import('react-router')>('react-router');
  return { ...actual, useFetcher: vi.fn(), useLoaderData: vi.fn(), useNavigate: vi.fn(), useParams: vi.fn() };
});
vi.mock('@morwalpizvideo/services', () => ({ deletePageImage: vi.fn(), uploadPageImages: vi.fn() }));
vi.mock('@components/ToastNotification/ToastContext', () => ({ useToast: () => ({ show: vi.fn() }) }));

const mockFetcher = {
  state: 'idle',
  data: undefined as unknown,
  Form: ({ children }: { children: React.ReactNode }) => <form>{children}</form>,
  submit: vi.fn(),
};

const page = {
  id: 'page-1', channelId: 'channel-1', title: 'About', url: 'about', content: '<p>Body</p>',
  thumbnailUrl: '', videoId: '', status: 1, inlineImages: [{ publicUrl: 'https://cdn.example.test/page.png', contentType: 'image/png', width: 100, height: 50, altText: 'page' }],
  creationDateTime: '', updatedDateTime: '',
};

function renderPageForm() {
  const router = createMemoryRouter([{ path: '*', element: <PageForm /> }], { initialEntries: ['/pages/page-1/edit'] });
  return render(<RouterProvider router={router} />);
}

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(useFetcher).mockReturnValue(mockFetcher as never);
  vi.mocked(useLoaderData).mockReturnValue(page as never);
  vi.mocked(useParams).mockReturnValue({ id: 'page-1' });
  vi.mocked(useNavigate).mockReturnValue(vi.fn());
  vi.mocked(uploadPageImages).mockRejectedValue(new Error('upload failed'));
  vi.mocked(deletePageImage).mockRejectedValue(new Error('delete failed'));
});

describe('PageForm', () => {
  it('renders the persisted Published field and reports image upload errors', async () => {
    const { container } = renderPageForm();
    expect(screen.getByLabelText('Status')).toHaveValue('1');
    const input = container.querySelector('input[type="file"]') as HTMLInputElement;
    fireEvent.change(input, { target: { files: [new File(['image'], 'page.png', { type: 'image/png' })] } });
    fireEvent.click(screen.getByRole('button', { name: 'Upload and insert' }));

    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('Unable to upload page images.'));
  });

  it('reports image deletion errors without losing the editor', async () => {
    renderPageForm();
    fireEvent.click(screen.getByRole('button', { name: 'Delete image' }));

    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('Unable to delete the page image.'));
    expect(screen.getByTestId('page-editor')).toBeInTheDocument();
  });
});