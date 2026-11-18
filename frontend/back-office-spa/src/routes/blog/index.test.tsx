import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { BlogPostAdmin } from '@morwalpizvideo/models';
import { appStore } from '@/state/appStore';
import { Component } from './index';

const mocks = vi.hoisted(() => ({ posts: [] as BlogPostAdmin[] }));

vi.mock('@/router/asyncData', () => ({
  useResolvedLoaderData: () => ({ posts: mocks.posts, page: 1 }),
}));

describe('blog index', () => {
  beforeEach(() => {
    mocks.posts = [];
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

  it('shows editorial revision rather than the aggregate concurrency revision', () => {
    mocks.posts = [
      {
        id: 'post-1',
        slug: 'article',
        revision: 8,
        editorialRevision: 1,
        draft: {
          title: 'Article',
          summary: '',
          author: '',
          tags: [],
          coverImageId: null,
          coverAlt: '',
          seoTitle: '',
          seoDescription: '',
          document: { version: 1, blocks: [] },
        },
        isPublished: false,
        firstPublishedAt: null,
        publishedAt: null,
        images: [],
      },
    ];
    appStore.setState({ effectivePermissions: ['pages.update'] });

    render(
      <MemoryRouter>
        <Component />
      </MemoryRouter>
    );

    expect(screen.getByRole('columnheader', { name: 'Revision' })).toBeInTheDocument();
    const row = screen.getByRole('row', { name: /Article article Draft 1 Edit Article/ });
    expect(row).toHaveTextContent('Article');
    expect(row).toHaveTextContent('article');
    expect(row).toHaveTextContent('Draft');
    expect(row).toHaveTextContent('1');
    expect(row).not.toHaveTextContent('8');
  });
});
