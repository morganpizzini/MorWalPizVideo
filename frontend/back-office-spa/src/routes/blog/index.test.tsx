import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { appStore } from '@/state/appStore';
import { Component } from './index';

vi.mock('@/router/asyncData', () => ({
  useResolvedLoaderData: () => ({ posts: [], page: 1 }),
}));

describe('blog index', () => {
  beforeEach(() => {
    appStore.setState({ effectivePermissions: ['pages.create'] });
  });

  it('places the create action in the page header', () => {
    render(
      <MemoryRouter>
        <Component />
      </MemoryRouter>
    );

    const createArticle = screen.getByRole('link', { name: 'Create article' });
    expect(createArticle).toHaveAttribute('href', '/create');
    expect(createArticle.closest('.bg-light')).toContainElement(createArticle);
    expect(screen.getByRole('heading', { name: 'Blog' }).closest('.bg-light')).toContainElement(
      createArticle
    );
  });

  it('keeps the create action permission-gated', () => {
    appStore.setState({ effectivePermissions: [] });
    render(
      <MemoryRouter>
        <Component />
      </MemoryRouter>
    );

    expect(screen.queryByRole('link', { name: 'Create article' })).not.toBeInTheDocument();
  });
});
