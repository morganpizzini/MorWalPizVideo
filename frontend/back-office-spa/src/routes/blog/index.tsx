import { Link } from 'react-router';
import type { BlogPostAdmin } from '@morwalpizvideo/models';
import { fetchBlogPosts } from '@morwalpizvideo/services';
import { Plus, Pencil } from 'lucide-react';
import { useResolvedLoaderData } from '@/router/asyncData';
import PageHeader from '@components/PageHeader';
import { hasPermission, permissions } from '@/authorization/permissions';
import { useAppStore } from '@/state/appStore';

export function loader({ request }: { request: Request }) {
  const page = Number(new URL(request.url).searchParams.get('page') ?? 1);
  if (!Number.isInteger(page) || page < 1 || page > 10000)
    throw new Response('Invalid page', { status: 400 });
  return fetchBlogPosts(page).then(posts => ({ posts, page }));
}
export function Component() {
  const { posts, page } = useResolvedLoaderData() as { posts: BlogPostAdmin[]; page: number };
  const effectivePermissions = useAppStore(state => state.effectivePermissions);
  const canCreate = hasPermission(effectivePermissions, [
    permissions.pages.create,
    permissions.pages.manage,
  ]);
  const canEdit = hasPermission(effectivePermissions, [
    permissions.pages.update,
    permissions.pages.manage,
  ]);
  return (
    <>
      <PageHeader title="Blog" />
      {canCreate && (
        <Link to="create" className="btn btn-primary mb-3">
          <Plus size={16} className="me-2" />
          Create article
        </Link>
      )}
      {!posts.length ? (
        <p role="status">No articles.</p>
      ) : (
        <div className="table-responsive">
          <table className="table align-middle">
            <thead>
              <tr>
                <th>Title</th>
                <th>Slug</th>
                <th>Status</th>
                <th>Revision</th>
                <th>
                  <span className="visually-hidden">Actions</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {posts.map(post => (
                <tr key={post.id}>
                  <td>{post.draft.title}</td>
                  <td>{post.slug}</td>
                  <td>{post.isPublished ? 'Published' : 'Draft'}</td>
                  <td>{post.revision}</td>
                  <td>
                    {canEdit && (
                      <Link
                        to={`${post.id}/edit`}
                        className="btn btn-sm btn-outline-secondary"
                        title="Edit article"
                        aria-label={`Edit ${post.draft.title}`}
                      >
                        <Pencil size={16} />
                      </Link>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      <nav aria-label="Blog pages" className="d-flex justify-content-between mt-3">
        {page > 1 ? <Link to={`?page=${page - 1}`}>Previous</Link> : <span />}
        {posts.length === 50 && <Link to={`?page=${page + 1}`}>Next</Link>}
      </nav>
    </>
  );
}
