import { useCallback, useEffect, useRef, useState } from 'react';
import { data, redirect, useFetcher, useLocation } from 'react-router';
import type {
  BlogPostAdmin,
  BlogSnapshot,
  SaveBlogPost,
  InsightArticleDraft,
} from '@morwalpizvideo/models';
import {
  ApiResponseError,
  createBlogPost,
  getBlogPost,
  saveBlogPost,
  publishBlogPost,
  uploadBlogImage,
  generateScriptStudio,
} from '@morwalpizvideo/services';
import { BlogRenderer } from '@morwalpiz/layout';
import { ButtonGroup, Dropdown } from 'react-bootstrap';
import { Save, Upload, Eye, Send, EyeOff, WandSparkles, X, Trash2 } from 'lucide-react';
import { useResolvedLoaderData } from '@/router/asyncData';
import PageHeader from '@components/PageHeader';
import { hasPermission, permissions } from '@/authorization/permissions';
import { appStore, useAppStore } from '@/state/appStore';
import { useChannelContext } from '@/contexts/ChannelContext';
import { requirePermissions } from '@/router/guards';
import TagInput from '@components/TagInput';
import { normalizeTags } from '@components/TagInput/tagRules';
import { BlogEditor } from './Editor';
import { BlogCoverCropper } from './BlogCoverCropper';
import { buildInsightBlogSnapshot } from './prefill';
import { applyPrettifiedContent, extractBlogContent } from './prettify';

const emptyDraft = (): BlogSnapshot => ({
  title: '',
  summary: '',
  author: '',
  tags: [],
  coverImageId: null,
  coverAlt: '',
  seoTitle: '',
  seoDescription: '',
  document: { version: 1, blocks: [] },
});

export const loader = ({ params }: { params: { id?: string } }) =>
  params.id ? getBlogPost(params.id) : null;

export async function action({ request, params }: { request: Request; params: { id?: string } }) {
  const form = await request.formData();
  const intent = String(form.get('intent') ?? 'save');
  const postId = params.id ?? (String(form.get('postId') ?? '') || undefined);
  const revision = Number(form.get('revision'));
  const channelId = String(form.get('channelId') ?? '');
  let savedPost: BlogPostAdmin | undefined;
  try {
    if (!['save', 'upload', 'publish', 'unpublish'].includes(intent))
      return data({ error: 'Unknown action.' }, { status: 400 });
    if (intent !== 'save' && (!Number.isSafeInteger(revision) || revision < 1))
      return data({ error: 'Invalid article revision.' }, { status: 400 });

    if (intent === 'publish' || intent === 'unpublish') {
      const denial = await requirePermissions([permissions.pages.manage]);
      if (denial) return denial;
    }
    if (channelId && appStore.getState().selectedChannelId !== channelId)
      return data(
        { error: 'The selected channel changed. Switch back and retry.' },
        { status: 409 }
      );

    if (intent === 'upload') {
      const denial = await requirePermissions([permissions.pages.update, permissions.pages.manage]);
      if (denial) return denial;
      const file = form.get('file');
      if (!(file instanceof File) || !file.size)
        return data({ error: 'Choose an image.' }, { status: 400 });
      const payload = JSON.parse(String(form.get('payload'))) as SaveBlogPost;
      if (!payload.draft.title.trim() || !payload.slug.trim())
        return data({ error: 'Save a title and slug before uploading an image.' }, { status: 400 });
      payload.draft.tags = normalizeTags(payload.draft.tags);
      if (!postId || String(form.get('saveBeforeUpload')) === 'true') {
        savedPost = postId ? await saveBlogPost(postId, payload) : await createBlogPost(payload);
      }
      const uploadPostId = savedPost?.id ?? postId;
      const uploadRevision = savedPost?.revision ?? revision;
      if (!uploadPostId)
        return data({ error: 'Unable to identify the article for this upload.' }, { status: 400 });
      if (channelId && appStore.getState().selectedChannelId !== channelId) {
        return data(
          { post: savedPost, error: 'The selected channel changed. Switch back before uploading.' },
          { status: 409 }
        );
      }
      const uploaded = await uploadBlogImage(
        uploadPostId,
        uploadRevision,
        file,
        String(form.get('altText') ?? ''),
        'cover'
      );
      return {
        post: uploaded,
        uploadedImageId: uploaded.images[uploaded.images.length - 1]?.id,
        uploadedImageAlt: String(form.get('altText') ?? '').trim(),
        message: 'Image uploaded. Save the draft to apply the cover selection.',
      };
    }

    if (intent === 'publish') {
      const payload = JSON.parse(String(form.get('payload'))) as SaveBlogPost;
      if (!payload.draft.title.trim() || !payload.slug.trim())
        return data({ error: 'Title and slug are required.' }, { status: 400 });
      payload.draft.tags = normalizeTags(payload.draft.tags);
      savedPost = postId ? await saveBlogPost(postId, payload) : await createBlogPost(payload);
      if (channelId && appStore.getState().selectedChannelId !== channelId) {
        return data(
          {
            post: savedPost,
            error: 'Draft saved, but the selected channel changed. Switch back to publish.',
          },
          { status: 409 }
        );
      }
      const published = await publishBlogPost(savedPost.id, savedPost.revision, true);
      return { post: published, message: 'Article published.' };
    }

    if (intent === 'unpublish') {
      if (!postId) return data({ error: 'Save the article before unpublishing.' }, { status: 400 });
      return {
        post: await publishBlogPost(postId, revision, false),
        message: 'Article unpublished.',
      };
    }

    const payload = JSON.parse(String(form.get('payload'))) as SaveBlogPost;
    if (!payload.draft.title.trim() || !payload.slug.trim())
      return data({ error: 'Title and slug are required.' }, { status: 400 });
    payload.draft.tags = normalizeTags(payload.draft.tags);
    const post = postId ? await saveBlogPost(postId, payload) : await createBlogPost(payload);
    return postId
      ? {
          post,
          message: post.isPublished
            ? 'Draft saved. The live article is unchanged.'
            : 'Draft saved.',
        }
      : redirect(`/blogposts/${post.id}/edit`);
  } catch (error) {
    const message =
      error instanceof ApiResponseError
        ? error.message
        : 'Unable to complete the request. Check the connection and retry.';
    if (savedPost && intent === 'publish')
      return data(
        { post: savedPost, error: `Draft saved, but publishing failed: ${message}` },
        { status: error instanceof ApiResponseError ? error.status : 400 }
      );
    if (savedPost && intent === 'upload')
      return data(
        { post: savedPost, error: `Draft saved, but image upload failed: ${message}` },
        { status: error instanceof ApiResponseError ? error.status : 400 }
      );
    return data(
      { error: message },
      { status: error instanceof ApiResponseError ? error.status : 400 }
    );
  }
}

export function Component() {
  const initial = useResolvedLoaderData() as BlogPostAdmin | null;
  const { selectedChannelId } = useChannelContext();
  const location = useLocation();
  const insightDraft = (location.state as { insightDraft?: InsightArticleDraft } | null)
    ?.insightDraft;
  return (
    <BlogForm
      key={`${selectedChannelId}-${initial?.id ?? 'new'}-${insightDraft?.topicId ?? ''}`}
      initial={initial}
      insightDraft={insightDraft}
    />
  );
}

function BlogForm({
  initial,
  insightDraft,
}: {
  initial: BlogPostAdmin | null;
  insightDraft?: InsightArticleDraft;
}) {
  const fetcher = useFetcher<{
    post?: BlogPostAdmin;
    error?: string;
    message?: string;
    uploadedImageId?: string;
    uploadedImageAlt?: string;
  }>();
  const effectivePermissions = useAppStore(state => state.effectivePermissions);
  const { selectedChannelId } = useChannelContext();
  const [post, setPost] = useState(initial);
  const canSave = hasPermission(effectivePermissions, [
    post ? permissions.pages.update : permissions.pages.create,
    permissions.pages.manage,
  ]);
  const canUpload = post
    ? hasPermission(effectivePermissions, [permissions.pages.update, permissions.pages.manage])
    : hasPermission(effectivePermissions, [permissions.pages.manage]) ||
      (hasPermission(effectivePermissions, [permissions.pages.create]) &&
        hasPermission(effectivePermissions, [permissions.pages.update]));
  const canPublish = hasPermission(effectivePermissions, [permissions.pages.manage]);
  const canPrettify = hasPermission(effectivePermissions, [permissions.scripts.studio]);
  const [draft, setDraft] = useState<BlogSnapshot>(
    () => initial?.draft ?? (insightDraft ? buildInsightBlogSnapshot(insightDraft) : emptyDraft())
  );
  const [slug, setSlug] = useState(initial?.slug ?? '');
  const [uploadAltText, setUploadAltText] = useState('');
  const [uploadSource, setUploadSource] = useState<File | null>(null);
  const [preparedUpload, setPreparedUpload] = useState<File | null>(null);
  const [cropSession, setCropSession] = useState(0);
  const setPreparedCover = useCallback((file: File | null) => setPreparedUpload(file), []);
  const uploadInput = useRef<HTMLInputElement>(null);
  const cancelCoverUpload = useCallback(() => {
    setUploadSource(null);
    setPreparedUpload(null);
    if (uploadInput.current) uploadInput.current.value = '';
  }, []);
  const [preview, setPreview] = useState(false);
  const [prettifyOpen, setPrettifyOpen] = useState(false);
  const [prettifyInstructions, setPrettifyInstructions] = useState('');
  const [prettifyBusy, setPrettifyBusy] = useState(false);
  const [prettifyError, setPrettifyError] = useState('');
  const [prettifySource, setPrettifySource] = useState('');
  const prettifyRun = useRef(0);
  const slugLocked = post?.firstPublishedAt != null;
  const busy = fetcher.state !== 'idle';
  const content = extractBlogContent(draft.document);
  const dirty =
    JSON.stringify(draft) !== JSON.stringify(post?.draft ?? emptyDraft()) ||
    slug !== (post?.slug ?? '');
  useEffect(() => {
    if (fetcher.data?.post && !busy) {
      setPost(fetcher.data.post);
      setDraft(
        fetcher.data.uploadedImageId
          ? {
              ...fetcher.data.post.draft,
              coverImageId: fetcher.data.uploadedImageId,
              coverAlt: fetcher.data.uploadedImageAlt ?? '',
            }
          : fetcher.data.post.draft
      );
      setSlug(fetcher.data.post.slug);
      if (fetcher.data.uploadedImageId) {
        setUploadSource(null);
        setPreparedUpload(null);
        if (uploadInput.current) uploadInput.current.value = '';
      }
    }
  }, [fetcher.data, busy]);
  const field = (key: keyof BlogSnapshot, value: string) =>
    setDraft(current => ({ ...current, [key]: value }));
  const openPrettify = () => {
    prettifyRun.current += 1;
    setPrettifySource(content);
    setPrettifyError('');
    setPrettifyOpen(true);
  };
  const prettify = async () => {
    if (!prettifySource.trim()) {
      setPrettifyError('Add article content before prettifying.');
      return;
    }
    setPrettifyBusy(true);
    setPrettifyError('');
    const runId = prettifyRun.current;
    try {
      const response = await generateScriptStudio({
        operation: 'prettify',
        script: prettifySource,
        prompt: prettifyInstructions,
        examples: '',
        style: '',
        generalContext: '',
        format: 'markdown',
      });
      if (prettifyRun.current !== runId) return;
      if (extractBlogContent(draft.document) !== prettifySource) {
        setPrettifyError(
          'The article changed while generation was running. Review it and try again.'
        );
        return;
      }
      setDraft(current => ({
        ...current,
        document: applyPrettifiedContent(current.document, response.result),
      }));
      setPrettifyOpen(false);
    } catch (error) {
      setPrettifyError(error instanceof Error ? error.message : 'Unable to prettify the article.');
    } finally {
      setPrettifyBusy(false);
    }
  };
  const cancelPrettify = () => {
    prettifyRun.current += 1;
    setPrettifyOpen(false);
  };
  return (
    <>
      <PageHeader title={post ? 'Edit article' : 'Create article'} backLink="/blogposts" />
      {fetcher.data?.error && (
        <div className="alert alert-danger" role="alert">
          {fetcher.data.error}
        </div>
      )}
      {fetcher.data?.message && !busy && (
        <p role="status" className="text-success">
          {fetcher.data.message} Revision {post?.revision}
        </p>
      )}
      <fetcher.Form method="post" aria-busy={busy}>
        <input
          type="hidden"
          name="payload"
          value={JSON.stringify({ slug, revision: post?.revision ?? 1, draft })}
        />
        <input type="hidden" name="revision" value={post?.revision ?? 1} />
        <input type="hidden" name="postId" value={post?.id ?? ''} />
        <input type="hidden" name="channelId" value={selectedChannelId ?? ''} />
        <fieldset disabled={busy}>
          <div className="row g-3 mb-3">
            <label className="col-md-8">
              Title
              <input
                className="form-control"
                value={draft.title}
                maxLength={200}
                required
                onChange={event => field('title', event.target.value)}
              />
            </label>
            <label className="col-md-4">
              Slug
              <input
                className="form-control"
                value={slug}
                maxLength={120}
                pattern="[a-z0-9](?:[a-z0-9-]{0,118}[a-z0-9])?"
                required
                readOnly={slugLocked}
                onChange={event => setSlug(event.target.value)}
              />
            </label>
            <label className="col-12">
              Summary
              <textarea
                className="form-control"
                value={draft.summary}
                maxLength={1000}
                rows={3}
                onChange={event => field('summary', event.target.value)}
              />
            </label>
            <label className="col-md-6">
              Author
              <input
                className="form-control"
                value={draft.author}
                maxLength={120}
                onChange={event => field('author', event.target.value)}
              />
            </label>
            <div className="col-12">
              <TagInput
                label="Tags"
                value={draft.tags}
                onChange={tags => setDraft(current => ({ ...current, tags }))}
                maxTags={20}
                maxTagLength={60}
                disabled={busy}
              />
            </div>
            <label className="col-md-6">
              SEO title
              <input
                className="form-control"
                value={draft.seoTitle}
                maxLength={200}
                onChange={event => field('seoTitle', event.target.value)}
              />
            </label>
            <label className="col-md-6">
              SEO description
              <input
                className="form-control"
                value={draft.seoDescription}
                maxLength={320}
                onChange={event => field('seoDescription', event.target.value)}
              />
            </label>
          </div>
          <div className="d-flex flex-wrap gap-2 align-items-center mb-3">
            {canPublish ? (
              <Dropdown as={ButtonGroup}>
                <button className="btn btn-primary" type="submit" name="intent" value="publish">
                  <Save size={16} className="me-2" />
                  {busy ? 'Saving...' : 'Save'}
                </button>
                <Dropdown.Toggle
                  split
                  variant="primary"
                  aria-label="More save options"
                  disabled={busy || !canSave}
                />
                <Dropdown.Menu align="end">
                  <Dropdown.Item
                    as="button"
                    type="submit"
                    name="intent"
                    value="save"
                    disabled={!canSave}
                  >
                    Save as draft
                  </Dropdown.Item>
                </Dropdown.Menu>
              </Dropdown>
            ) : (
              <button
                className="btn btn-primary"
                type="submit"
                name="intent"
                value="save"
                disabled={!canSave}
              >
                <Save size={16} className="me-2" />
                {busy ? 'Saving...' : 'Save as draft'}
              </button>
            )}
            {canPublish && (
              <span className="small text-secondary">Save publishes this article.</span>
            )}
            {canPublish &&
              fetcher.data?.error?.startsWith('Draft saved, but publishing failed:') && (
                <button
                  className="btn btn-outline-primary"
                  type="submit"
                  name="intent"
                  value="publish"
                  disabled={busy}
                >
                  <Send size={16} className="me-2" />
                  Retry publish
                </button>
              )}
            <button
              className="btn btn-outline-primary"
              type="button"
              onClick={openPrettify}
              disabled={busy || prettifyBusy || !canPrettify}
            >
              <WandSparkles size={16} className="me-2" />
              Prettify article
            </button>
            <button
              className="btn btn-outline-secondary"
              type="button"
              onClick={() => setPreview(value => !value)}
              aria-pressed={preview}
            >
              <Eye size={16} className="me-2" />
              Preview
            </button>
            {post?.isPublished && canPublish && (
              <button
                className="btn btn-outline-danger"
                type="submit"
                name="intent"
                value="unpublish"
                disabled={dirty}
                formNoValidate
              >
                <EyeOff size={16} className="me-2" />
                Unpublish
              </button>
            )}
            {post && (
              <span className="text-secondary">
                {post.isPublished ? 'Published' : 'Draft'} · Revision {post.revision}
              </span>
            )}
          </div>
        </fieldset>
      </fetcher.Form>
      <section className="border rounded p-3 mb-3" aria-labelledby="cover-media-title">
        <div className="d-flex flex-wrap justify-content-between align-items-start gap-2 mb-3">
          <div>
            <h2 id="cover-media-title" className="h5 mb-1">
              Cover image
            </h2>
            <p className="text-secondary mb-0">Upload or choose an image from this article.</p>
          </div>
          {draft.coverImageId && (
            <button
              type="button"
              className="btn btn-sm btn-outline-secondary"
              onClick={() =>
                setDraft(current => ({ ...current, coverImageId: null, coverAlt: '' }))
              }
              disabled={busy}
            >
              <Trash2 size={16} className="me-1" />
              Remove cover
            </button>
          )}
        </div>
        {draft.coverImageId && post?.images.find(image => image.id === draft.coverImageId) && (
          <figure className="mb-3">
            <img
              src={post.images.find(image => image.id === draft.coverImageId)!.publicUrl}
              alt={draft.coverAlt}
              className="img-fluid rounded"
              style={{ maxHeight: '16rem', objectFit: 'contain' }}
            />
            <figcaption className="small text-secondary mt-1">
              {dirty ? 'Cover selection is not saved yet.' : 'Selected cover'}
            </figcaption>
          </figure>
        )}
        {post?.images.length ? (
          <div className="d-flex flex-wrap gap-2 mb-3" aria-label="Article media">
            {post.images.map(image => (
              <button
                key={image.id}
                type="button"
                className={`btn p-1 ${draft.coverImageId === image.id ? 'btn-primary' : 'btn-outline-secondary'}`}
                aria-pressed={draft.coverImageId === image.id}
                aria-label={`Use ${image.altText || 'image'} as cover`}
                onClick={() =>
                  setDraft(current => ({
                    ...current,
                    coverImageId: image.id,
                    coverAlt: image.altText,
                  }))
                }
                disabled={busy}
              >
                <img
                  src={image.publicUrl}
                  alt=""
                  loading="lazy"
                  className="rounded"
                  style={{ width: '7rem', height: '5rem', objectFit: 'cover' }}
                />
              </button>
            ))}
          </div>
        ) : (
          <p className="text-secondary">No images uploaded for this article yet.</p>
        )}
        <label className="form-label" htmlFor="cover-alt">
          Cover alt text
        </label>
        <input
          id="cover-alt"
          className="form-control mb-3"
          value={draft.coverAlt}
          maxLength={300}
          required={!!draft.coverImageId}
          disabled={busy}
          onChange={event => field('coverAlt', event.target.value)}
        />
        <fetcher.Form
          method="post"
          encType="multipart/form-data"
          className="row g-2 align-items-end"
          onSubmit={event => {
            event.preventDefault();
            if (!preparedUpload) return;
            const formData = new FormData(event.currentTarget);
            formData.set('file', preparedUpload);
            fetcher.submit(formData, { method: 'post', encType: 'multipart/form-data' });
          }}
        >
          <input type="hidden" name="intent" value="upload" />
          <input type="hidden" name="revision" value={post?.revision ?? 1} />
          <input type="hidden" name="postId" value={post?.id ?? ''} />
          <input type="hidden" name="channelId" value={selectedChannelId ?? ''} />
          <input
            type="hidden"
            name="payload"
            value={JSON.stringify({ slug, revision: post?.revision ?? 1, draft })}
          />
          <input type="hidden" name="saveBeforeUpload" value={dirty || !post ? 'true' : 'false'} />
          <fieldset disabled={busy || !canUpload} className="row g-2 align-items-end">
            <label className="col-md-5">
              Upload image
              <input
                ref={uploadInput}
                name="file"
                type="file"
                className="form-control"
                accept="image/jpeg,image/png,image/webp"
                required
                onChange={event => {
                  const file = event.target.files?.[0] ?? null;
                  setUploadSource(file);
                  setPreparedUpload(null);
                  if (file) setCropSession(current => current + 1);
                }}
              />
              {uploadSource && <span className="form-text">{uploadSource.name}</span>}
            </label>
            <label className="col-md-5">
              Image alt text
              <input
                name="altText"
                className="form-control"
                maxLength={300}
                required
                value={uploadAltText}
                onChange={event => setUploadAltText(event.target.value)}
              />
            </label>
            <div className="col-md-2">
              <button
                className="btn btn-outline-primary"
                type="submit"
                disabled={!preparedUpload || !uploadAltText.trim()}
              >
                <Upload size={16} className="me-2" />
                Upload
              </button>
            </div>
          </fieldset>
          {uploadSource && (
            <div className="col-12">
              <BlogCoverCropper
                key={cropSession}
                file={uploadSource}
                preparedFile={preparedUpload}
                disabled={busy || !canUpload}
                onPrepared={setPreparedCover}
                onCancel={cancelCoverUpload}
              />
            </div>
          )}
          <p className="small text-secondary mb-0">
            JPEG, PNG, or WebP; maximum 10 MB and 25 megapixels. Confirm a 16:9 crop before
            uploading. If the article is new or has unsaved changes, uploading saves a draft first.
            The uploaded image is selected as cover; save again to apply it.
          </p>
          {!canUpload && (
            <p className="small text-secondary mb-0">
              Uploading to a new article requires create and update permission; existing articles
              require update permission.
            </p>
          )}
        </fetcher.Form>
      </section>
      {preview ? (
        <section aria-label="Article preview" className="bg-white p-3">
          <h1 className="h2">{draft.title}</h1>
          <p>{draft.summary}</p>
          <BlogRenderer document={draft.document} images={post?.images ?? []} />
        </section>
      ) : (
        <div aria-busy={busy} inert={busy} style={{ minWidth: 0 }}>
          <BlogEditor
            key={`${post?.id ?? 'new'}-${post?.revision ?? 1}`}
            document={draft.document}
            images={post?.images ?? []}
            onChange={document => setDraft(current => ({ ...current, document }))}
          />
        </div>
      )}
      {prettifyOpen && (
        <div
          className="modal d-block"
          role="dialog"
          aria-modal="true"
          aria-labelledby="prettify-title"
          onKeyDown={event => {
            if (event.key === 'Escape') cancelPrettify();
          }}
        >
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content">
              <div className="modal-header">
                <h2 id="prettify-title" className="modal-title h5">
                  Prettify article
                </h2>
                <button
                  type="button"
                  className="btn-close"
                  aria-label="Cancel"
                  onClick={cancelPrettify}
                />
              </div>
              <div className="modal-body">
                <p>Improve the current unsaved article without saving or publishing it.</p>
                <label htmlFor="prettify-instructions" className="form-label">
                  Optional instructions
                </label>
                <textarea
                  id="prettify-instructions"
                  className="form-control"
                  rows={4}
                  maxLength={2000}
                  autoFocus
                  value={prettifyInstructions}
                  onChange={event => setPrettifyInstructions(event.target.value)}
                />
                {prettifyError && (
                  <div className="alert alert-danger mt-3" role="alert">
                    {prettifyError}
                  </div>
                )}
              </div>
              <div className="modal-footer">
                <button
                  type="button"
                  className="btn btn-outline-secondary"
                  onClick={cancelPrettify}
                >
                  <X size={16} className="me-2" />
                  Cancel
                </button>
                <button
                  type="button"
                  className="btn btn-primary"
                  onClick={prettify}
                  disabled={prettifyBusy || !prettifySource.trim()}
                >
                  <WandSparkles size={16} className="me-2" />
                  {prettifyBusy ? 'Prettifying...' : 'Prettify'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
