import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { createMemoryRouter, RouterProvider, useLoaderData } from 'react-router';
import PageDetail from './Component';

vi.mock('react-router', async () => {
  const actual = await vi.importActual<typeof import('react-router')>('react-router');
  return { ...actual, useLoaderData: vi.fn() };
});

describe('PageDetail', () => {
  it.each([[0, 'Draft'], [1, 'Published']])('renders %s status', (status, label) => {
    vi.mocked(useLoaderData).mockReturnValue({ id: 'page-1', title: 'About', url: 'about', status, content: '<p>Body</p>' } as never);

    const router = createMemoryRouter([{ path: '*', element: <PageDetail /> }], { initialEntries: ['/pages/page-1'] });
    render(<RouterProvider router={router} />);

    expect(screen.getByText(label)).toBeInTheDocument();
    expect(screen.getByText('/pages/about')).toBeInTheDocument();
  });
});