import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, screen, waitFor } from '@testing-library/react';
import { useFetcher, useRevalidator } from 'react-router';
import { useResolvedLoaderData } from '@/router/asyncData';
import { createProductsBulk } from '@morwalpizvideo/services';
import { render } from '../../../test/test-utils';
import type { Product, VideoProductCategory } from '@morwalpizvideo/models';
import { useToast } from '@components/ToastNotification/ToastContext';
import Component from './Component';

const mockToastShow = vi.hoisted(() => vi.fn());
const mockRevalidate = vi.hoisted(() => vi.fn());

vi.mock('@/router/asyncData', () => ({
  useResolvedLoaderData: vi.fn(),
}));

vi.mock('@components/ToastNotification/ToastContext', async importOriginal => {
  const actual =
    await importOriginal<typeof import('@components/ToastNotification/ToastContext')>();
  return {
    ...actual,
    useToast: vi.fn(),
  };
});

vi.mock('@morwalpizvideo/services', () => ({
  createProductsBulk: vi.fn(),
  assignProductCategoriesBulk: vi.fn(),
}));

vi.mock('react-router', async () => {
  const actual = await vi.importActual<typeof import('react-router')>('react-router');
  return {
    ...actual,
    useFetcher: vi.fn(),
    useRevalidator: vi.fn(),
  };
});

const product: Product = {
  id: 'product-1',
  title: 'Product 1',
  description: 'Description',
  url: 'https://example.com/product-1',
  categories: [],
} as Product;

const categories: VideoProductCategory[] = [
  { id: 'category-1', title: 'News', description: '' } as VideoProductCategory,
];

const fetcher = {
  state: 'idle' as const,
  data: { success: true },
  submit: vi.fn(),
};

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(useResolvedLoaderData).mockReturnValue({ products: [product], categories });
  vi.mocked(useToast).mockReturnValue({ show: mockToastShow });
  vi.mocked(useFetcher).mockReturnValue(fetcher as never);
  vi.mocked(useRevalidator).mockImplementation(() => ({
    state: 'idle',
    revalidate: mockRevalidate,
  }));
});

describe('Products index refresh behavior', () => {
  it('does not revalidate again when a successful delete response is retained', async () => {
    render(<Component />);

    await waitFor(() => {
      expect(mockToastShow).toHaveBeenCalledTimes(1);
    });
    expect(mockRevalidate).not.toHaveBeenCalled();

    fireEvent.click(screen.getByRole('button', { name: /select/i }));

    expect(mockToastShow).toHaveBeenCalledTimes(1);
    expect(mockRevalidate).not.toHaveBeenCalled();
  });

  it('revalidates once after a successful CSV import', async () => {
    vi.mocked(useFetcher).mockReturnValue({
      state: 'idle',
      data: undefined,
      submit: vi.fn(),
    } as never);
    vi.mocked(createProductsBulk).mockResolvedValue({
      results: [{ rowNumber: 2, success: true }],
    });

    render(<Component />);
    fireEvent.click(screen.getByRole('button', { name: /import/i }));
    fireEvent.change(screen.getByLabelText('CSV file'), {
      target: {
        files: [
          {
            text: () =>
              Promise.resolve(
                'Title,Description,Url\nImported,Description,https://example.com/imported'
              ),
          } as File,
        ],
      },
    });
    fireEvent.click(screen.getAllByRole('button', { name: /^import$/i })[1]);

    await waitFor(() => expect(createProductsBulk).toHaveBeenCalledTimes(1));
    expect(mockRevalidate).toHaveBeenCalledTimes(1);
  });
});
