import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router';
import { HelmetProvider } from 'react-helmet-async';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiResponseError, getPublicBlog, getPublicBlogPost } from '@morwalpizvideo/services';
import type { BlogPage, BlogPostPublic } from '@morwalpizvideo/models';
import { BlogArticle, BlogList, articleLoader, blogLoader } from './index';

const mocks = vi.hoisted(() => ({ loaderData: {} as BlogPage | BlogPostPublic }));
vi.mock('react-router', async (importOriginal) => ({
  ...(await importOriginal<typeof import('react-router')>()),
  useLoaderData: () => mocks.loaderData,
}));
vi.mock('@morwalpizvideo/services', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@morwalpizvideo/services')>()),
  getPublicBlog: vi.fn(),
  getPublicBlogPost: vi.fn(),
}));
const post: BlogPostPublic = {
  metadata: {
    slug: 'article',
    title: 'Public article',
    summary: 'Published summary',
    author: 'Author',
    tags: ['Training'],
    coverUrl: 'https://cdn.test/cover.jpg',
    coverAlt: 'Cover image',
    publishedAt: '2026-09-30T00:00:00Z',
  },
  document: {
    version: 1,
    blocks: [
      {
        id: 'text',
        type: 'richText',
        richText: {
          type: 'doc',
          content: [{ type: 'paragraph', content: [{ type: 'text', text: 'Published body' }] }],
        },
      },
    ],
  },
  seoTitle: 'Article SEO',
  seoDescription: 'Description',
  updatedAt: '2026-09-30T01:00:00Z',
  images: [],
};
beforeEach(() => vi.clearAllMocks());
describe('public blog routes', () => {
  it('validates pagination and uses only the public published API', async () => {
    vi.mocked(getPublicBlog).mockResolvedValue({
      items: [post.metadata],
      page: 2,
      pageSize: 12,
      hasMore: false,
    });
    const args: Parameters<typeof blogLoader>[0] = {
      request: new Request('https://public.test/blog?page=2'),
      url: new URL('https://public.test/blog?page=2'),
      pattern: '/blog',
      params: {},
      context: {},
    };
    expect((await blogLoader(args)).items).toEqual([post.metadata]);
    expect(getPublicBlog).toHaveBeenCalledWith(2);
    for (const page of ['0', '1.5', '10001', 'NaN'])
      await expect(
        blogLoader({ ...args, request: new Request(`https://public.test/blog?page=${page}`) })
      ).rejects.toMatchObject({ status: 400 });
    expect(getPublicBlog).toHaveBeenCalledTimes(1);
  });
  it('maps missing/unpublished articles to a route 404', async () => {
    vi.mocked(getPublicBlogPost).mockRejectedValue(
      new ApiResponseError({ status: 404, errors: ['Not found'] })
    );
    const args: Parameters<typeof articleLoader>[0] = {
      request: new Request('https://public.test/blog/draft'),
      url: new URL('https://public.test/blog/draft'),
      pattern: '/blog/:slug',
      params: { slug: 'draft' },
      context: {},
    };
    await expect(articleLoader(args)).rejects.toMatchObject({ status: 404 });
    expect(getPublicBlogPost).toHaveBeenCalledWith('draft');
  });
  it('renders the list empty state and stable public links on the server', () => {
    mocks.loaderData = { items: [], page: 1, pageSize: 12, hasMore: false };
    expect(
      renderToStaticMarkup(
        <HelmetProvider>
          <MemoryRouter>
            <BlogList />
          </MemoryRouter>
        </HelmetProvider>
      )
    ).toContain('Nessun articolo pubblicato.');
    mocks.loaderData = { items: [post.metadata], page: 2, pageSize: 12, hasMore: true };
    const html = renderToStaticMarkup(
      <HelmetProvider>
        <MemoryRouter initialEntries={['/blog?page=2']}>
          <BlogList />
        </MemoryRouter>
      </HelmetProvider>
    );
    expect(html).toContain('href="/blog/article"');
    expect(html).toContain('href="/blog?page=3"');
  });
  it('server-renders published body with canonical and absolute image SEO', () => {
    mocks.loaderData = post;
    const html = renderToStaticMarkup(
      <HelmetProvider>
        <MemoryRouter initialEntries={['/blog/article']}>
          <BlogArticle />
        </MemoryRouter>
      </HelmetProvider>
    );
    expect(html).toContain('Published body');
    expect(html).not.toContain('draft');
    expect(html).toContain('<title>Article SEO</title>');
    expect(html).toContain('content="https://cdn.test/cover.jpg"');
    expect(html).toContain('href="https://morwalpiz.it/blog/article"');
    expect(html).toContain('"@type":"Article"');
  });
});
