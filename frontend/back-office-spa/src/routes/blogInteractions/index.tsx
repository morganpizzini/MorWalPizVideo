import { useEffect, useState } from 'react';
import { Alert, Button, Form, Table } from 'react-bootstrap';
import { Link } from 'react-router';
import {
  getBlogAdminComments,
  getBlogArticleInteractions,
  moderateBlogComment,
} from '@morwalpizvideo/services';
import {
  BlogCommentStatus,
  type BlogAdminComment,
  type BlogAdminCommentPage,
  type BlogArticleInteractionsPage,
} from '@morwalpizvideo/models';
import { hasPermission, permissions } from '@/authorization/permissions';
import { useAppStore } from '@/state/appStore';

export default function BlogInteractionsPage() {
  const [articles, setArticles] = useState<BlogArticleInteractionsPage | null>(null);
  const [comments, setComments] = useState<BlogAdminCommentPage | null>(null);
  const [status, setStatus] = useState<number | undefined>(BlogCommentStatus.Pending);
  const [postId, setPostId] = useState('');
  const [articlePage, setArticlePage] = useState(1);
  const [commentPage, setCommentPage] = useState(1);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  const [pendingId, setPendingId] = useState('');
  const effectivePermissions = useAppStore(state => state.effectivePermissions);
  const canEditArticles = hasPermission(effectivePermissions, [
    permissions.pages.update,
    permissions.pages.manage,
  ]);

  async function load() {
    setLoading(true);
    setError('');
    try {
      const [articleResult, commentResult] = await Promise.all([
        getBlogArticleInteractions(articlePage),
        getBlogAdminComments(status, postId || undefined, commentPage),
      ]);
      setArticles(articleResult);
      setComments(commentResult);
    } catch {
      setError('Unable to load blog interactions for the selected channel.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, [articlePage, commentPage, status, postId]);

  const moderate = async (comment: BlogAdminComment, nextStatus: BlogCommentStatus) => {
    setPendingId(comment.id);
    setError('');
    try {
      await moderateBlogComment(comment, nextStatus);
      await load();
    } catch {
      setError('The comment changed or could not be moderated. Reload and try again.');
    } finally {
      setPendingId('');
    }
  };

  return (
    <main>
      <h1 className="h3 mb-3">Blog interactions</h1>
      {error && (
        <Alert variant="danger" role="alert">
          {error}
        </Alert>
      )}
      {loading && <p role="status">Loading blog interactions...</p>}
      <section>
        {!articles?.items.length && !loading ? <p>No articles found.</p> : null}
        {articles?.items.length ? (
          <>
            <Table responsive hover className="align-middle">
              <thead>
                <tr>
                  <th>Article</th>
                  <th>Status</th>
                  <th>Likes</th>
                  <th>Dislikes</th>
                  <th>Comments</th>
                </tr>
              </thead>
              <tbody>
                {articles.items.map(article => (
                  <tr key={article.postId}>
                    <td>
                      {canEditArticles ? (
                        <Link to={`/blogposts/${encodeURIComponent(article.postId)}/edit`}>
                          {article.title || article.slug}
                        </Link>
                      ) : (
                        article.title || article.slug
                      )}
                    </td>
                    <td>{article.published ? 'Published' : 'Draft'}</td>
                    <td>{article.likes}</td>
                    <td>{article.dislikes}</td>
                    <td>{article.comments}</td>
                  </tr>
                ))}
              </tbody>
            </Table>
            <div className="d-flex justify-content-between align-items-center">
              <Button
                variant="outline-secondary"
                disabled={articles.page <= 1}
                onClick={() => setArticlePage(value => value - 1)}
              >
                Previous articles
              </Button>
              <span>Page {articles.page}</span>
              <Button
                variant="outline-secondary"
                disabled={!articles.hasMore}
                onClick={() => setArticlePage(value => value + 1)}
              >
                Next articles
              </Button>
            </div>
          </>
        ) : null}
      </section>

      <section className="mt-4">
        <h2 className="h5">Comment moderation</h2>
        <div className="row g-2 mb-3">
          <div className="col-md-4">
            <Form.Label htmlFor="moderation-status">Status</Form.Label>
            <Form.Select
              id="moderation-status"
              value={status ?? 'all'}
              onChange={event => {
                setCommentPage(1);
                setStatus(event.target.value === 'all' ? undefined : Number(event.target.value));
              }}
            >
              <option value="all">All statuses</option>
              <option value={BlogCommentStatus.Pending}>Pending</option>
              <option value={BlogCommentStatus.Approved}>Approved</option>
              <option value={BlogCommentStatus.Rejected}>Rejected</option>
              <option value={BlogCommentStatus.Hidden}>Hidden</option>
            </Form.Select>
          </div>
          <div className="col-md-8">
            <Form.Label htmlFor="moderation-article">Article</Form.Label>
            <Form.Select
              id="moderation-article"
              value={postId}
              onChange={event => {
                setCommentPage(1);
                setPostId(event.target.value);
              }}
            >
              <option value="">All articles</option>
              {articles?.items.map(article => (
                <option key={article.postId} value={article.postId}>
                  {article.title || article.slug}
                </option>
              ))}
            </Form.Select>
          </div>
        </div>
        {!comments?.items.length && !loading ? <p>No comments match these filters.</p> : null}
        {comments?.items.length ? (
          <>
            <Table responsive hover>
              <thead>
                <tr>
                  <th>Article</th>
                  <th>Account</th>
                  <th>Comment</th>
                  <th>Status</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {comments.items.map(comment => (
                  <tr key={comment.id}>
                    <td>{comment.slug}</td>
                    <td>
                      <Link to={`/public-accounts/${encodeURIComponent(comment.accountId)}`}>
                        {comment.displayName}
                      </Link>
                    </td>
                    <td>{comment.body || 'Withdrawn'}</td>
                    <td>
                      {['Pending', 'Approved', 'Rejected', 'Hidden', 'Withdrawn'][comment.status]}
                    </td>
                    <td className="text-nowrap">
                      {comment.status !== BlogCommentStatus.Approved &&
                      comment.status !== BlogCommentStatus.Withdrawn ? (
                        <Button
                          size="sm"
                          className="me-1"
                          disabled={pendingId === comment.id}
                          onClick={() => void moderate(comment, BlogCommentStatus.Approved)}
                        >
                          Approve
                        </Button>
                      ) : null}
                      {comment.status !== BlogCommentStatus.Rejected &&
                      comment.status !== BlogCommentStatus.Withdrawn ? (
                        <Button
                          size="sm"
                          variant="outline-danger"
                          disabled={pendingId === comment.id}
                          onClick={() => void moderate(comment, BlogCommentStatus.Rejected)}
                        >
                          Reject
                        </Button>
                      ) : null}
                      {comment.status === BlogCommentStatus.Approved ? (
                        <Button
                          size="sm"
                          variant="outline-secondary"
                          className="ms-1"
                          disabled={pendingId === comment.id}
                          onClick={() => void moderate(comment, BlogCommentStatus.Hidden)}
                        >
                          Hide
                        </Button>
                      ) : null}
                    </td>
                  </tr>
                ))}
              </tbody>
            </Table>
            <div className="d-flex justify-content-between align-items-center">
              <Button
                variant="outline-secondary"
                disabled={comments.page <= 1}
                onClick={() => setCommentPage(value => value - 1)}
              >
                Previous comments
              </Button>
              <span>
                Page {comments.page} of{' '}
                {Math.max(1, Math.ceil(comments.totalCount / comments.pageSize))}
              </span>
              <Button
                variant="outline-secondary"
                disabled={comments.page * comments.pageSize >= comments.totalCount}
                onClick={() => setCommentPage(value => value + 1)}
              >
                Next comments
              </Button>
            </div>
          </>
        ) : null}
      </section>
    </main>
  );
}
