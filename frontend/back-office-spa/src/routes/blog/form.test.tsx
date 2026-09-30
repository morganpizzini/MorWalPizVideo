import { fireEvent, render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { BlogPostAdmin } from '@morwalpizvideo/models';
import { createBlogPost, publishBlogPost, saveBlogPost } from '@morwalpizvideo/services';
import { requirePermissions } from '@/router/guards';
import { appStore } from '@/state/appStore';
import { action, Component } from './form';

const mocks = vi.hoisted(() => ({
  initial: null as BlogPostAdmin | null,
  fetcher: { state: 'idle', data: undefined as unknown },
}));
vi.mock('@/router/asyncData', () => ({ useResolvedLoaderData: () => mocks.initial }));
vi.mock('@/router/guards', () => ({ requirePermissions: vi.fn().mockResolvedValue(null) }));
vi.mock('./Editor', () => ({ BlogEditor: () => <div>Composition</div> }));
vi.mock('@morwalpizvideo/services', async importOriginal => ({
  ...(await importOriginal<typeof import('@morwalpizvideo/services')>()),
  createBlogPost: vi.fn(),
  saveBlogPost: vi.fn(),
  publishBlogPost: vi.fn(),
}));
vi.mock('react-router', async importOriginal => ({
  ...(await importOriginal<typeof import('react-router')>()),
  useFetcher: () => ({
    ...mocks.fetcher,
    Form: ({ children, ...props }: React.ComponentProps<'form'>) => (
      <form {...props}>{children}</form>
    ),
  }),
}));

const post: BlogPostAdmin = {
  id: 'post-1',
  slug: 'article',
  revision: 4,
  isPublished: true,
  firstPublishedAt: '2026-09-30T00:00:00Z',
  publishedAt: '2026-09-30T00:00:00Z',
  images: [],
  draft: {
    title: 'Article',
    summary: 'Summary',
    author: '',
    tags: [],
    coverImageId: null,
    coverAlt: '',
    seoTitle: '',
    seoDescription: '',
    document: { version: 1, blocks: [] },
  },
};
function request(values: Record<string, string>) {
  const form = new FormData();
  Object.entries(values).forEach(([key, value]) => form.set(key, value));
  return new Request('https://admin.test/blogposts/post-1/edit', { method: 'POST', body: form });
}
beforeEach(() => {
  vi.clearAllMocks();
  mocks.initial = structuredClone(post);
  mocks.fetcher.state = 'idle';
  mocks.fetcher.data = undefined;
  appStore.setState({ effectivePermissions: ['pages.manage'] });
});
describe('blog draft workflow', () => {
  it('locks a previously published slug after unpublish and reopen and saves it unchanged', async () => {
    const reopened: BlogPostAdmin = { ...post, isPublished: false, publishedAt: null };
    mocks.initial = reopened;
    vi.mocked(saveBlogPost).mockResolvedValue(reopened);
    render(
      <MemoryRouter>
        <Component />
      </MemoryRouter>
    );
    expect(screen.getByLabelText('Slug')).toHaveAttribute('readonly');
    expect(screen.getByLabelText('Slug')).toHaveValue(post.slug);
    fireEvent.change(screen.getByLabelText('Title'), { target: { value: 'Reopened draft' } });
    const saveButton = screen.getByRole('button', { name: 'Save draft' });
    expect(saveButton).toBeEnabled();
    const form = saveButton.closest('form')!;
    await action({
      request: new Request('https://admin.test/blogposts/post-1/edit', {
        method: 'POST',
        body: new FormData(form),
      }),
      params: { id: post.id },
    });
    expect(saveBlogPost).toHaveBeenCalledWith(post.id, {
      slug: post.slug,
      revision: post.revision,
      draft: { ...post.draft, title: 'Reopened draft' },
    });
  });
  it('lets a never-published draft edit and save its slug', async () => {
    const unpublished: BlogPostAdmin = {
      ...post,
      isPublished: false,
      firstPublishedAt: null,
      publishedAt: null,
    };
    mocks.initial = unpublished;
    vi.mocked(saveBlogPost).mockResolvedValue(unpublished);
    render(
      <MemoryRouter>
        <Component />
      </MemoryRouter>
    );
    expect(screen.getByLabelText('Slug')).not.toHaveAttribute('readonly');
    expect(screen.getByLabelText('Slug')).toBeEnabled();
    fireEvent.change(screen.getByLabelText('Slug'), { target: { value: 'new-slug' } });
    const form = screen.getByRole('button', { name: 'Save draft' }).closest('form')!;
    await action({
      request: new Request('https://admin.test/blogposts/post-1/edit', {
        method: 'POST',
        body: new FormData(form),
      }),
      params: { id: post.id },
    });
    expect(saveBlogPost).toHaveBeenCalledWith(post.id, {
      slug: 'new-slug',
      revision: post.revision,
      draft: post.draft,
    });
  });
  it('locks on the publish response and stays locked on the unpublish response', () => {
    mocks.initial = { ...post, isPublished: false, firstPublishedAt: null, publishedAt: null };
    const { rerender } = render(
      <MemoryRouter>
        <Component />
      </MemoryRouter>
    );
    expect(screen.getByLabelText('Slug')).not.toHaveAttribute('readonly');
    mocks.fetcher.data = { post: { ...post, revision: 5 } };
    rerender(
      <MemoryRouter>
        <Component />
      </MemoryRouter>
    );
    expect(screen.getByLabelText('Slug')).toHaveAttribute('readonly');
    mocks.fetcher.data = { post: { ...post, revision: 6, isPublished: false, publishedAt: null } };
    rerender(
      <MemoryRouter>
        <Component />
      </MemoryRouter>
    );
    expect(screen.getByLabelText('Slug')).toHaveAttribute('readonly');
  });
  it('requires saving before publication, unpublication and upload', () => {
    render(
      <MemoryRouter>
        <Component />
      </MemoryRouter>
    );
    expect(screen.getByLabelText('Slug')).toHaveAttribute('readonly');
    expect(screen.getByRole('button', { name: 'Publish revision' })).toBeEnabled();
    fireEvent.change(screen.getByLabelText('Title'), { target: { value: 'Changed draft' } });
    expect(screen.getByRole('button', { name: 'Publish revision' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Unpublish' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Upload' })).toBeDisabled();
  });
  it('keeps publish controls out of the update-only UI', () => {
    appStore.setState({ effectivePermissions: ['pages.update'] });
    render(
      <MemoryRouter>
        <Component />
      </MemoryRouter>
    );
    expect(screen.queryByRole('button', { name: 'Publish revision' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Save draft' })).toBeEnabled();
  });
  it('saves canonical JSON with normalized tags and expected revision', async () => {
    vi.mocked(saveBlogPost).mockResolvedValue(post);
    const payload = { slug: post.slug, revision: 4, draft: { ...post.draft, tags: [' tag ', ''] } };
    await action({
      request: request({ payload: JSON.stringify(payload) }),
      params: { id: post.id },
    });
    expect(saveBlogPost).toHaveBeenCalledWith(post.id, {
      ...payload,
      draft: { ...payload.draft, tags: ['tag'] },
    });
  });
  it('creates a draft and redirects to its owned editor', async () => {
    vi.mocked(createBlogPost).mockResolvedValue(post);
    const result = await action({
      request: request({
        payload: JSON.stringify({ slug: post.slug, revision: 1, draft: post.draft }),
      }),
      params: {},
    });
    expect((result as Response).headers.get('Location')).toBe('/blogposts/post-1/edit');
  });
  it('checks manage permission before publishing the stored revision', async () => {
    vi.mocked(publishBlogPost).mockResolvedValue(post);
    await action({
      request: request({ intent: 'publish', revision: '4' }),
      params: { id: post.id },
    });
    expect(requirePermissions).toHaveBeenCalledWith(['pages.manage']);
    expect(publishBlogPost).toHaveBeenCalledWith(post.id, 4, true);
    vi.mocked(requirePermissions).mockResolvedValueOnce(new Response(null, { status: 403 }));
    await action({
      request: request({ intent: 'unpublish', revision: '4' }),
      params: { id: post.id },
    });
    expect(publishBlogPost).toHaveBeenCalledTimes(1);
  });
  it('does not mutate on malformed intent, revision or payload', async () => {
    const invalidRequests: Record<string, string>[] = [
      { intent: 'destroy' },
      { intent: 'publish', revision: 'NaN' },
      { payload: 'invalid JSON' },
    ];
    for (const values of invalidRequests) {
      const result = await action({ request: request(values), params: { id: post.id } });
      expect((result as { init: { status: number } }).init.status).toBe(400);
    }
    expect(saveBlogPost).not.toHaveBeenCalled();
    expect(publishBlogPost).not.toHaveBeenCalled();
  });
});
