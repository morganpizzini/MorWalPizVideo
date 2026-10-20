import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, useFetcher, useLoaderData, useRevalidator } from 'react-router';
import { ToastContext } from '@components/ToastNotification/ToastContext';
import { useResolvedLoaderData } from '@/router/asyncData';

vi.mock('@/router/asyncData', () => ({
  useResolvedLoaderData: vi.fn(),
}));

vi.mock('react-router', async () => {
  const actual = await vi.importActual<typeof import('react-router')>('react-router');
  return {
    ...actual,
    useFetcher: vi.fn(),
    useLoaderData: vi.fn(),
    useRevalidator: vi.fn(),
  };
});

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(useLoaderData).mockReturnValue({ data: [] });
  vi.mocked(useResolvedLoaderData).mockReturnValue({ data: [] });
  vi.mocked(useFetcher).mockReturnValue({
    state: 'idle',
    data: undefined,
    submit: vi.fn(),
  } as never);
  vi.mocked(useRevalidator).mockReturnValue({ revalidate: vi.fn(), state: 'idle' });
});

describe('Galleries', () => {
  it('renders when the loader returns the API response envelope', async () => {
    const { default: Component } = await import('./Component');

    expect(() =>
      render(
        <MemoryRouter>
          <ToastContext.Provider value={{ show: vi.fn() }}>
            <Component />
          </ToastContext.Provider>
        </MemoryRouter>
      )
    ).not.toThrow();
    expect(screen.getByRole('heading', { name: 'Galleries' })).toBeInTheDocument();
  });
});
