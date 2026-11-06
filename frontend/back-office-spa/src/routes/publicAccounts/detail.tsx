import { useEffect, useState, type FormEvent } from 'react';
import { Alert, Button, Form, Table } from 'react-bootstrap';
import { Link, useParams } from 'react-router';
import {
  getPublicAccountInteractions,
  updatePublicAccountPortalAccess,
} from '@morwalpizvideo/services';
import type { PublicAccountInteractionDetail } from '@morwalpizvideo/models';

export default function PublicAccountDetailPage() {
  const { id = '' } = useParams();
  const [detail, setDetail] = useState<PublicAccountInteractionDetail | null>(null);
  const [pageNumber, setPageNumber] = useState(1);
  const [commentsBlocked, setCommentsBlocked] = useState(false);
  const [portalAccessRevoked, setPortalAccessRevoked] = useState(false);
  const [reason, setReason] = useState('');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');

  async function load() {
    setLoading(true);
    setError('');
    try {
      const loaded = await getPublicAccountInteractions(id, pageNumber);
      setDetail(loaded);
      setCommentsBlocked(loaded.account.portalAccess.commentsBlocked);
      setPortalAccessRevoked(loaded.account.portalAccess.portalAccessRevoked);
      setReason(loaded.account.portalAccess.reason);
    } catch {
      setError('Unable to load account interactions for the selected channel.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, [id, pageNumber]);

  const updateAccess = async (event: FormEvent) => {
    event.preventDefault();
    if (!detail) return;
    setSaving(true);
    setError('');
    setMessage('');
    try {
      await updatePublicAccountPortalAccess(id, {
        revision: detail.account.portalAccess.revision,
        commentsBlocked,
        portalAccessRevoked,
        reason,
      });
      setMessage('Portal access updated.');
      await load();
    } catch {
      setError('Portal access could not be updated. Reload the page and try again.');
    } finally {
      setSaving(false);
    }
  };

  if (loading && !detail) return <p role="status">Loading account...</p>;
  if (!detail)
    return (
      <main>
        <Alert variant="danger" role="alert">
          {error || 'Account unavailable.'}
        </Alert>
        <Link to="/public-accounts">Back to public accounts</Link>
      </main>
    );
  const { account } = detail;

  return (
    <main>
      <p>
        <Link to="/public-accounts">Public accounts</Link>
      </p>
      <h1 className="h3">{account.displayName || account.username}</h1>
      {error && (
        <Alert variant="danger" role="alert">
          {error}
        </Alert>
      )}
      {message && (
        <Alert variant="success" role="status">
          {message}
        </Alert>
      )}
      <dl className="row">
        <dt className="col-sm-3">Username</dt>
        <dd className="col-sm-9">{account.username}</dd>
        <dt className="col-sm-3">Account status</dt>
        <dd className="col-sm-9">{account.isActive ? 'Active' : 'Inactive'}</dd>
        <dt className="col-sm-3">Interactions on this channel</dt>
        <dd className="col-sm-9">
          {account.commentCount} comments · {account.likeCount} likes · {account.dislikeCount}{' '}
          dislikes
        </dd>
      </dl>

      <section className="mt-4">
        <h2 className="h5">Portal access</h2>
        <p className="text-secondary">
          Restrictions apply only to this channel. Approved comments remain publicly readable.
        </p>
        <Form onSubmit={event => void updateAccess(event)}>
          <Form.Check
            id="comments-blocked"
            className="mb-2"
            label="Block this account from writing comments"
            checked={commentsBlocked}
            onChange={event => setCommentsBlocked(event.target.checked)}
          />
          <Form.Check
            id="portal-access-revoked"
            className="mb-3"
            label="Revoke authenticated interactions on this portal"
            checked={portalAccessRevoked}
            onChange={event => setPortalAccessRevoked(event.target.checked)}
          />
          <Form.Group className="mb-3">
            <Form.Label htmlFor="access-reason">Internal reason</Form.Label>
            <Form.Control
              id="access-reason"
              as="textarea"
              rows={2}
              maxLength={500}
              value={reason}
              onChange={event => setReason(event.target.value)}
              required
            />
          </Form.Group>
          <Button type="submit" disabled={saving}>
            {saving ? 'Saving...' : 'Save restrictions'}
          </Button>
        </Form>
      </section>

      <section className="mt-4">
        <h2 className="h5">Comments left</h2>
        {!detail.comments.length ? (
          <p>No comments from this account on this channel.</p>
        ) : (
          <Table responsive hover>
            <thead>
              <tr>
                <th>Article</th>
                <th>Comment</th>
                <th>Status</th>
                <th>Created</th>
              </tr>
            </thead>
            <tbody>
              {detail.comments.map(comment => (
                <tr key={comment.id}>
                  <td>
                    <Link to={`/blogposts/${encodeURIComponent(comment.postId)}`}>
                      {comment.slug}
                    </Link>
                  </td>
                  <td>{comment.body || 'Withdrawn'}</td>
                  <td>
                    {['Pending', 'Approved', 'Rejected', 'Hidden', 'Withdrawn'][comment.status]}
                  </td>
                  <td>{new Date(comment.createdAt).toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </Table>
        )}
        {account.commentCount > pageNumber * detail.pageSize ? (
          <Button variant="outline-secondary" onClick={() => setPageNumber(value => value + 1)}>
            More comments
          </Button>
        ) : null}
      </section>

      <section className="mt-4">
        <h2 className="h5">Article reactions</h2>
        {!detail.reactions.length ? (
          <p>No article reactions from this account on this channel.</p>
        ) : (
          <Table responsive hover>
            <thead>
              <tr>
                <th>Article</th>
                <th>Reaction</th>
                <th>Date</th>
              </tr>
            </thead>
            <tbody>
              {detail.reactions.map(reaction => (
                <tr key={`${reaction.postId}-${reaction.createdAt}`}>
                  <td>{reaction.slug}</td>
                  <td>{reaction.value === 0 ? 'Like' : 'Dislike'}</td>
                  <td>{new Date(reaction.createdAt).toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </Table>
        )}
      </section>
    </main>
  );
}
