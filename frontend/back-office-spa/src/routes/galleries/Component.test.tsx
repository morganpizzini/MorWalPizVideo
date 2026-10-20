import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { useLoaderData, useRevalidator } from 'react-router';

vi.mock('react-router', async () => {
  const actual = await vi.importActual<typeof import('react-router')>('react-router');
  return {
    ...actual,
    useLoaderData: vi.fn(),
    useRevalidator: vi.fn(),
  };
});

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(useLoaderData).mockReturnValue({ data: [] });
  vi.mocked(useRevalidator).mockReturnValue({ revalidate: vi.fn(), state: 'idle' });
});

describe('Galleries', () => {
  it('renders when the loader returns the API response envelope', async () => {
    const { default: Component } = await import('./Component');

    expect(() => render(<Component />)).not.toThrow();
    expect(screen.getByRole('heading', { name: 'Galleries' })).toBeInTheDocument();
  });
});
