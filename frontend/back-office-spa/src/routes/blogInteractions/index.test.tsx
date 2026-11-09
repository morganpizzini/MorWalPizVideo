import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { appStore } from '@/state/appStore';
import BlogInteractionsPage from './index';

vi.mock('@morwalpizvideo/services', () => ({
  getBlogArticleInteractions: vi.fn().mockResolvedValue({
    items: [
      {
        postId: 'article/1',
        title: 'Article one',
        slug: 'article-one',
        published: true,
        likes: 1,
        dislikes: 0,
        comments: 0,
      },
    ],
    page: 1,
    hasMore: false,
  }),
  getBlogAdminComments: vi.fn().mockResolvedValue({ items: [], page: 1, hasMore: false }),
  moderateBlogComment: vi.fn(),
}));

describe('blog interactions article links', () => {
  beforeEach(() => {
    appStore.setState({ effectivePermissions: ['pages.update'] });
  });

  it('uses the canonical editor route for permitted users', async () => {
    render(
      <MemoryRouter>
        <BlogInteractionsPage />
      </MemoryRouter>
    );

    expect(await screen.findByRole('link', { name: 'Article one' })).toHaveAttribute(
      'href',
      '/blogposts/article%2F1/edit'
    );
  });

  it('does not present an editor link without page edit permission', async () => {
    appStore.setState({ effectivePermissions: [] });
    render(
      <MemoryRouter>
        <BlogInteractionsPage />
      </MemoryRouter>
    );

    const articleCell = (await screen.findAllByText('Article one'))[0];
    expect(articleCell.closest('a')).toBeNull();
  });
});
