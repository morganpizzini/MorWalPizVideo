import { useEffect, useRef, useState } from 'react';
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
import { Save, Upload, Eye, Send, EyeOff, WandSparkles, X } from 'lucide-react';
import { useResolvedLoaderData } from '@/router/asyncData';
import PageHeader from '@components/PageHeader';
import { hasPermission, permissions } from '@/authorization/permissions';
import { useAppStore } from '@/state/appStore';
import { useChannelContext } from '@/contexts/ChannelContext';
import { requirePermissions } from '@/router/guards';
import { BlogEditor } from './Editor';
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
  const revision = Number(form.get('revision'));
  try {
    if (!['save', 'upload', 'publish', 'unpublish'].includes(intent))
      return data({ error: 'Unknown action.' }, { status: 400 });
    if (intent !== 'save' && (!params.id || !Number.isSafeInteger(revision) || revision < 1))
      return data({ error: 'Invalid article revision.' }, { status: 400 });
    if (params.id && intent === 'upload') {
      const file = form.get('file');
      if (!(file instanceof File) || !file.size)
        return data({ error: 'Choose an image.' }, { status: 400 });
      return {
        post: await uploadBlogImage(params.id, revision, file, String(form.get('altText') ?? '')),
      };
    }
    if (params.id && (intent === 'publish' || intent === 'unpublish')) {
      const denial = await requirePermissions([permissions.pages.manage]);
      if (denial) return denial;
      return { post: await publishBlogPost(params.id, revision, intent === 'publish') };
    }
    const payload = JSON.parse(String(form.get('payload'))) as SaveBlogPost;
    if (!payload.draft.title.trim() || !payload.slug.trim())
      return data({ error: 'Title and slug are required.' }, { status: 400 });
    payload.draft.tags = payload.draft.tags.map(tag => tag.trim()).filter(Boolean);
    const post = params.id ? await saveBlogPost(params.id, payload) : await createBlogPost(payload);
    return params.id ? { post } : redirect(`/blogposts/${post.id}/edit`);
  } catch (error) {
    return data(
      {
        error:
          error instanceof ApiResponseError
            ? error.message
            : 'Unable to save. Check the connection and retry.',
      },
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
  const fetcher = useFetcher<{ post?: BlogPostAdmin; error?: string }>();
  const effectivePermissions = useAppStore(state => state.effectivePermissions);
  const canSave = hasPermission(effectivePermissions, [
    initial ? permissions.pages.update : permissions.pages.create,
    permissions.pages.manage,
  ]);
  const canPublish = hasPermission(effectivePermissions, [permissions.pages.manage]);
  const canPrettify = hasPermission(effectivePermissions, [permissions.scripts.studio]);
  const [post, setPost] = useState(initial);
  const [draft, setDraft] = useState<BlogSnapshot>(
    () => initial?.draft ?? (insightDraft ? buildInsightBlogSnapshot(insightDraft) : emptyDraft())
  );
  const [slug, setSlug] = useState(initial?.slug ?? '');
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
      setDraft(fetcher.data.post.draft);
      setSlug(fetcher.data.post.slug);
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
      {fetcher.data?.post && !busy && (
        <p role="status" className="text-success">
          Saved. Revision {post?.revision}
        </p>
      )}
      <fetcher.Form method="post" aria-busy={busy}>
        <input
          type="hidden"
          name="payload"
          value={JSON.stringify({ slug, revision: post?.revision ?? 1, draft })}
        />
        <input type="hidden" name="revision" value={post?.revision ?? 1} />
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
            <label className="col-md-6">
              Tags
              <input
                className="form-control"
                value={draft.tags.join(',')}
                onChange={event =>
                  setDraft(current => ({ ...current, tags: event.target.value.split(',') }))
                }
              />
            </label>
            <label className="col-md-6">
              Cover
              <select
                className="form-select"
                value={draft.coverImageId ?? ''}
                onChange={event =>
                  setDraft(current => ({ ...current, coverImageId: event.target.value || null }))
                }
              >
                <option value="">None</option>
                {post?.images.map(image => (
                  <option key={image.id} value={image.id}>
                    {image.altText}
                  </option>
                ))}
              </select>
            </label>
            <label className="col-md-6">
              Cover alt text
              <input
                className="form-control"
                value={draft.coverAlt}
                maxLength={300}
                required={!!draft.coverImageId}
                onChange={event => field('coverAlt', event.target.value)}
              />
            </label>
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
            <button
              className="btn btn-primary"
              type="submit"
              name="intent"
              value="save"
              disabled={!canSave}
            >
              <Save size={16} className="me-2" />
              {busy ? 'Saving...' : 'Save draft'}
            </button>
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
            {post && canPublish && (
              <button
                className="btn btn-success"
                type="submit"
                name="intent"
                value="publish"
                disabled={dirty}
              >
                <Send size={16} className="me-2" />
                {post.isPublished ? 'Publish revision' : 'Publish'}
              </button>
            )}
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
      {post && (
        <fetcher.Form
          method="post"
          encType="multipart/form-data"
          className="border-top border-bottom py-3 mb-3"
        >
          <input type="hidden" name="intent" value="upload" />
          <input type="hidden" name="revision" value={post.revision} />
          <fieldset disabled={busy || dirty || !canSave} className="row g-2 align-items-end">
            <label className="col-md-5">
              Image
              <input
                name="file"
                type="file"
                className="form-control"
                accept="image/jpeg,image/png,image/webp"
                required
              />
            </label>
            <label className="col-md-5">
              Alt text
              <input name="altText" className="form-control" maxLength={300} required />
            </label>
            <div className="col-md-2">
              <button className="btn btn-outline-primary" type="submit">
                <Upload size={16} className="me-2" />
                Upload
              </button>
            </div>
          </fieldset>
        </fetcher.Form>
      )}
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
