import { fireEvent, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { BlogDocument, BlogPostAdmin } from '@morwalpizvideo/models';
import {
  createBlogPost,
  publishBlogPost,
  saveBlogPost,
  uploadBlogImage,
} from '@morwalpizvideo/services';
import { requirePermissions } from '@/router/guards';
import { appStore } from '@/state/appStore';
import { action, Component } from './form';

const mocks = vi.hoisted(() => ({
  initial: null as BlogPostAdmin | null,
  fetcher: { state: 'idle', data: undefined as unknown, submit: vi.fn() },
}));
vi.hoisted(() => {
  const storage = { getItem: vi.fn(() => null), setItem: vi.fn(), removeItem: vi.fn() };
  Object.defineProperty(window, 'localStorage', { configurable: true, value: storage });
  vi.stubGlobal('localStorage', storage);
});
vi.mock('@/router/asyncData', () => ({ useResolvedLoaderData: () => mocks.initial }));
vi.mock('@/router/guards', () => ({ requirePermissions: vi.fn().mockResolvedValue(null) }));
vi.mock('./Editor', () => ({
  BlogEditor: ({
    document,
    onChange,
  }: {
    document: BlogDocument;
    onChange: (document: BlogDocument) => void;
  }) => (
    <div>
      <span>Composition</span>
      <button
        type="button"
        onClick={() =>
          onChange({
            version: 1,
            blocks: [
              ...document.blocks,
              { id: 'unsaved-edit', type: 'heading', text: 'Latest unsaved edit' },
            ],
          })
        }
      >
        Update composition
      </button>
    </div>
  ),
}));
vi.mock('@morwalpizvideo/services', async importOriginal => ({
  ...(await importOriginal<typeof import('@morwalpizvideo/services')>()),
  createBlogPost: vi.fn(),
  saveBlogPost: vi.fn(),
  publishBlogPost: vi.fn(),
  uploadBlogImage: vi.fn(),
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
  const form = new URLSearchParams();
  Object.entries(values).forEach(([key, value]) => form.set(key, value));
  return new Request('https://admin.test/blogposts/post-1/edit', { method: 'POST', body: form });
}
function encodedForm(form: HTMLFormElement) {
  const values = new URLSearchParams();
  new FormData(form).forEach((value, key) => {
    if (typeof value === 'string') values.append(key, value);
  });
  return values;
}
beforeEach(() => {
  vi.clearAllMocks();
  mocks.initial = structuredClone(post);
  mocks.fetcher.state = 'idle';
  mocks.fetcher.data = undefined;
  mocks.fetcher.submit.mockReset();
  appStore.setState({ effectivePermissions: ['pages.manage'], selectedChannelId: null });
});
describe('blog draft workflow', () => {
  it('uses removable tag pills and keeps the blog tag limit', () => {
    render(
      <MemoryRouter>
        <Component />
      </MemoryRouter>
    );
    const tags = screen.getByRole('combobox', { name: 'Tags' });
    expect(tags).toHaveAttribute('maxLength', '60');
    fireEvent.change(tags, { target: { value: 'Gear,' } });
    expect(screen.getByRole('button', { name: 'Remove tag Gear' })).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Remove tag Gear' }));
    expect(screen.queryByRole('button', { name: 'Remove tag Gear' })).not.toBeInTheDocument();
  });
  it('includes a tag committed on blur in the current save payload', async () => {
    const user = userEvent.setup();
    render(
      <MemoryRouter>
        <Component />
      </MemoryRouter>
    );
    await user.type(screen.getByRole('combobox', { name: 'Tags' }), 'Last minute');
    await user.click(screen.getByRole('button', { name: 'Save' }));
    const payload = JSON.parse(
      document
        .querySelector<HTMLFormElement>('form')!
        .querySelector<HTMLInputElement>('input[name="payload"]')!.value
    );
    expect(payload.draft.tags).toEqual(['Last minute']);
  });
  it('shows article media controls before the first article save', () => {
    mocks.initial = null;
    render(
      <MemoryRouter>
        <Component />
      </MemoryRouter>
    );
    expect(screen.getByRole('heading', { name: 'Cover image' })).toBeInTheDocument();
    expect(screen.getByLabelText('Upload image')).toBeInTheDocument();
    expect(screen.getByText('No images uploaded for this article yet.')).toBeInTheDocument();
  });
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
    const form = document.querySelector('form')!;
    await action({
      request: new Request('https://admin.test/blogposts/post-1/edit', {
        method: 'POST',
        body: encodedForm(form),
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
    const form = document.querySelector('form')!;
    await action({
      request: new Request('https://admin.test/blogposts/post-1/edit', {
        method: 'POST',
        body: encodedForm(form),
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
  it('saves dirty content before publishing and keeps unpublish disabled while dirty', () => {
    render(
      <MemoryRouter>
        <Component />
      </MemoryRouter>
    );
    expect(screen.getByLabelText('Slug')).toHaveAttribute('readonly');
    expect(screen.getByRole('button', { name: 'Save' })).toBeEnabled();
    fireEvent.change(screen.getByLabelText('Title'), { target: { value: 'Changed draft' } });
    expect(screen.getByRole('button', { name: 'Save' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Unpublish' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Upload' })).toBeDisabled();
  });
  it('previews the latest unsaved composition and restores edit mode without saving', () => {
    render(
      <MemoryRouter>
        <Component />
      </MemoryRouter>
    );
    fireEvent.click(screen.getByRole('button', { name: 'Update composition' }));
    fireEvent.click(screen.getByRole('button', { name: 'Preview' }));
    expect(screen.getByRole('region', { name: 'Article preview' })).toBeInTheDocument();
    expect(screen.getByText('Latest unsaved edit')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Update composition' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Preview' }));
    expect(screen.getByText('Composition')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Update composition' })).toBeInTheDocument();
    expect(saveBlogPost).not.toHaveBeenCalled();
    expect(publishBlogPost).not.toHaveBeenCalled();
  });
  it('keeps publish controls out of the update-only UI', () => {
    appStore.setState({ effectivePermissions: ['pages.update'] });
    render(
      <MemoryRouter>
        <Component />
      </MemoryRouter>
    );
    expect(screen.queryByRole('button', { name: 'Save' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Save as draft' })).toBeEnabled();
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
  it('keeps the published snapshot unchanged when saving a draft', async () => {
    const updated = { ...post, revision: 5, isPublished: true };
    vi.mocked(saveBlogPost).mockResolvedValue(updated);
    const result = await action({
      request: request({
        intent: 'save',
        payload: JSON.stringify({ slug: post.slug, revision: 4, draft: post.draft }),
      }),
      params: { id: post.id },
    });
    expect(publishBlogPost).not.toHaveBeenCalled();
    expect(result).toMatchObject({
      post: updated,
      message: 'Draft saved. The live article is unchanged.',
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
  it('saves the current revision before publishing and uses the returned revision', async () => {
    const saved = { ...post, revision: 5 };
    const published = { ...post, revision: 6, isPublished: true };
    vi.mocked(saveBlogPost).mockResolvedValue(saved);
    vi.mocked(publishBlogPost).mockResolvedValue(published);
    await action({
      request: request({
        intent: 'publish',
        revision: '4',
        payload: JSON.stringify({ slug: post.slug, revision: 4, draft: post.draft }),
      }),
      params: { id: post.id },
    });
    expect(requirePermissions).toHaveBeenCalledWith(['pages.manage']);
    expect(saveBlogPost).toHaveBeenCalledWith(post.id, {
      slug: post.slug,
      revision: 4,
      draft: post.draft,
    });
    expect(publishBlogPost).toHaveBeenCalledWith(post.id, 5, true);
    expect(published.revision).toBe(6);
  });
  it('creates a draft and publishes it using the created identity and revision', async () => {
    const created = { ...post, revision: 1, isPublished: false };
    const published = { ...post, revision: 2, isPublished: true };
    vi.mocked(createBlogPost).mockResolvedValue(created);
    vi.mocked(publishBlogPost).mockResolvedValue(published);
    const result = await action({
      request: request({
        intent: 'publish',
        revision: '1',
        payload: JSON.stringify({ slug: post.slug, revision: 1, draft: post.draft }),
      }),
      params: {},
    });
    expect(createBlogPost).toHaveBeenCalledOnce();
    expect(publishBlogPost).toHaveBeenCalledWith(created.id, created.revision, true);
    expect(result).toMatchObject({ post: published, message: 'Article published.' });
  });
  it('saves dirty work before uploading and uses the returned revision', async () => {
    const saved = { ...post, revision: 5 };
    const uploaded = {
      ...saved,
      revision: 6,
      images: [{ id: 'media-1', publicUrl: '/media', width: 1, height: 1, altText: 'Cover' }],
    };
    vi.mocked(saveBlogPost).mockResolvedValue(saved);
    vi.mocked(uploadBlogImage).mockResolvedValue(uploaded);
    const form = new FormData();
    form.set('intent', 'upload');
    form.set('revision', '4');
    form.set('postId', post.id);
    form.set('saveBeforeUpload', 'true');
    form.set('payload', JSON.stringify({ slug: post.slug, revision: 4, draft: post.draft }));
    form.set('file', new File(['image'], 'cover.png', { type: 'image/png' }));
    form.set('altText', 'Cover');
    const uploadRequest = new Request('https://admin.test/blogposts/post-1/edit', {
      method: 'POST',
    });
    vi.spyOn(uploadRequest, 'formData').mockResolvedValue(form);
    const result = await action({
      request: uploadRequest,
      params: { id: post.id },
    });
    expect(saveBlogPost).toHaveBeenCalledOnce();
    expect(uploadBlogImage).toHaveBeenCalledWith(
      post.id,
      saved.revision,
      expect.any(File),
      'Cover'
    );
    expect(result).toMatchObject({
      post: uploaded,
      uploadedImageId: 'media-1',
      uploadedImageAlt: 'Cover',
    });
  });
  it('creates a draft before uploading media for a new article', async () => {
    const created = { ...post, revision: 1, isPublished: false };
    const uploaded = {
      ...created,
      revision: 2,
      images: [{ id: 'media-new', publicUrl: '/media', width: 1, height: 1, altText: 'Cover' }],
    };
    vi.mocked(createBlogPost).mockResolvedValue(created);
    vi.mocked(uploadBlogImage).mockResolvedValue(uploaded);
    const form = new FormData();
    form.set('intent', 'upload');
    form.set('revision', '1');
    form.set('saveBeforeUpload', 'true');
    form.set('payload', JSON.stringify({ slug: post.slug, revision: 1, draft: post.draft }));
    form.set('file', new File(['image'], 'cover.png', { type: 'image/png' }));
    form.set('altText', 'Cover');
    const uploadRequest = new Request('https://admin.test/blogposts/create', { method: 'POST' });
    vi.spyOn(uploadRequest, 'formData').mockResolvedValue(form);
    const result = await action({ request: uploadRequest, params: {} });

    expect(createBlogPost).toHaveBeenCalledOnce();
    expect(uploadBlogImage).toHaveBeenCalledWith(
      created.id,
      created.revision,
      expect.any(File),
      'Cover'
    );
    expect(result).toMatchObject({ post: uploaded, uploadedImageId: 'media-new' });
  });
  it('returns the saved post when publication fails after saving', async () => {
    const saved = { ...post, revision: 5, isPublished: false };
    vi.mocked(saveBlogPost).mockResolvedValue(saved);
    vi.mocked(publishBlogPost).mockRejectedValue(new Error('offline'));
    const result = await action({
      request: request({
        intent: 'publish',
        revision: '4',
        payload: JSON.stringify({ slug: post.slug, revision: 4, draft: post.draft }),
      }),
      params: { id: post.id },
    });
    expect(result).toMatchObject({
      data: {
        post: saved,
        error:
          'Draft saved, but publishing failed: Unable to complete the request. Check the connection and retry.',
      },
    });
  });
  it('offers an explicit retry after a save succeeds but publication fails', () => {
    mocks.fetcher.data = {
      post: { ...post, revision: 5, isPublished: false },
      error: 'Draft saved, but publishing failed: offline',
    };
    render(
      <MemoryRouter>
        <Component />
      </MemoryRouter>
    );
    expect(screen.getByRole('button', { name: 'Retry publish' })).toBeEnabled();
  });
  it('checks manage permission before publishing', async () => {
    vi.mocked(publishBlogPost).mockResolvedValue(post);
    vi.mocked(saveBlogPost).mockResolvedValue(post);
    vi.mocked(requirePermissions).mockResolvedValueOnce(new Response(null, { status: 403 }));
    await action({
      request: request({
        intent: 'publish',
        revision: '4',
        payload: JSON.stringify({ slug: post.slug, revision: 4, draft: post.draft }),
      }),
      params: { id: post.id },
    });
    expect(saveBlogPost).not.toHaveBeenCalled();
    vi.mocked(requirePermissions).mockResolvedValueOnce(new Response(null, { status: 403 }));
    await action({
      request: request({ intent: 'unpublish', revision: '4' }),
      params: { id: post.id },
    });
    expect(publishBlogPost).not.toHaveBeenCalled();
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
