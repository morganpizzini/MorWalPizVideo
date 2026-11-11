import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, screen, waitFor } from '@testing-library/react';
import { useFetcher, useNavigate, useParams, useSearchParams } from 'react-router';
import { get } from '@morwalpizvideo/services';
import { render } from '../../../test/test-utils';
import Component from './Component';

const { fetcher, navigate, entity, toast } = vi.hoisted(() => ({
  fetcher: {
    state: 'idle',
    data: undefined as
      | { success?: boolean; errors?: { generics?: string[] } }
      | undefined,
    submit: vi.fn(),
  },
  navigate: vi.fn(),
  toast: { show: vi.fn() },
  entity: {
    target: 'https://example.test',
    code: 'example',
    linkType: 5,
    queryLinkIds: [],
  },
}));

vi.mock('react-router', async () => {
  const actual = await vi.importActual<typeof import('react-router')>('react-router');
  return {
    ...actual,
    useFetcher: vi.fn(() => fetcher),
    useNavigate: vi.fn(() => navigate),
    useParams: vi.fn(() => ({})),
    useSearchParams: vi.fn(() => [new URLSearchParams(), vi.fn()]),
  };
});

vi.mock('@/router/asyncData', () => ({
  useResolvedLoaderData: () => entity,
}));

vi.mock('@components/ToastNotification/ToastContext', async importOriginal => ({
  ...(await importOriginal<typeof import('@components/ToastNotification/ToastContext')>()),
  useToast: () => toast,
}));

vi.mock('@morwalpizvideo/services', () => ({
  get: vi.fn().mockResolvedValue([]),
  endpoints: { QUERYLINKS: '/api/querylinks' },
}));

vi.mock('@/services/matchesService', () => ({
  fetchMatches: vi.fn().mockResolvedValue([]),
}));

describe('short-link form', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    fetcher.state = 'idle';
    fetcher.data = undefined;
  });

  it('displays API errors without showing success or leaving the confirmation modal open', async () => {
    const { container, rerender } = render(<Component />);
    fireEvent.submit(container.querySelector('form')!);
    expect(screen.getByText('Confirm Create')).toBeInTheDocument();

    fetcher.data = {
      success: false,
      errors: { generics: ['Short link could not be saved.'] },
    };
    rerender(<Component />);

    expect(await screen.findByText('Short link could not be saved.')).toBeInTheDocument();
    await waitFor(() => expect(screen.queryByText('Confirm Create')).not.toBeInTheDocument());
    expect(navigate).not.toHaveBeenCalled();
    expect(toast.show).not.toHaveBeenCalled();
  });
});
