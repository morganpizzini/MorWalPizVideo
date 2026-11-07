import { useEffect, useState, type FormEvent } from 'react';
import { Link } from 'react-router';
import {
  clearBlogReaction,
  getBlogComments,
  getBlogInteractionSummary,
  getBlogViewerState,
  setBlogReaction,
  submitBlogComment,
} from '@morwalpizvideo/services';
import {
  BlogReactionValue,
  type BlogComment,
  type BlogInteractionSummary,
  type BlogViewerState,
} from '@morwalpizvideo/models';
import { usePublicAccount } from '../layout/account';

const emptySummary: BlogInteractionSummary = { likes: 0, dislikes: 0, approvedComments: 0 };

export default function BlogInteractions({ slug }: { slug: string }) {
  const { session, loading: accountLoading, sessionError } = usePublicAccount();
  const [summary, setSummary] = useState(emptySummary);
  const [comments, setComments] = useState<BlogComment[]>([]);
  const [viewer, setViewer] = useState<BlogViewerState | null>(null);
  const [body, setBody] = useState('');
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError('');
    void Promise.all([
      getBlogInteractionSummary(slug),
      getBlogComments(slug, page),
      session.authenticated ? getBlogViewerState(slug) : Promise.resolve(null),
    ])
      .then(([nextSummary, commentPage, nextViewer]) => {
        if (!active) return;
        setSummary(nextSummary);
        setComments((current) =>
          page === 1
            ? commentPage.items
            : [
                ...current,
                ...commentPage.items.filter(
                  (item) => !current.some((existing) => existing.id === item.id)
                ),
              ]
        );
        setViewer(nextViewer);
      })
      .catch(() => {
        if (active) setError('Impossibile caricare le interazioni. Riprova.');
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [page, session.authenticated, slug]);

  const changeReaction = async (value: BlogReactionValue) => {
    if (!session.authenticated || !viewer || pending) return;
    setPending(true);
    setError('');
    try {
      if (viewer.reaction === value) await clearBlogReaction(slug);
      else
        await setBlogReaction(slug, {
          value: value === BlogReactionValue.Like ? 'Like' : 'Dislike',
        });
      const [nextSummary, nextViewer] = await Promise.all([
        getBlogInteractionSummary(slug),
        getBlogViewerState(slug),
      ]);
      setSummary(nextSummary);
      setViewer(nextViewer);
    } catch {
      setError('Non è stato possibile aggiornare la reazione. Riprova.');
    } finally {
      setPending(false);
    }
  };

  const addComment = async (event: FormEvent) => {
    event.preventDefault();
    if (pending) return;
    setPending(true);
    setError('');
    setMessage('');
    try {
      await submitBlogComment(slug, { body, requestId: crypto.randomUUID() });
      setBody('');
      setMessage('Commento inviato. Sarà visibile dopo la moderazione.');
      const [nextSummary, commentPage] = await Promise.all([
        getBlogInteractionSummary(slug),
        getBlogComments(slug, page),
      ]);
      setSummary(nextSummary);
      setComments((current) =>
        page === 1
          ? commentPage.items
          : [
              ...current,
              ...commentPage.items.filter(
                (item) => !current.some((existing) => existing.id === item.id)
              ),
            ]
      );
    } catch {
      setError('Impossibile inviare il commento. Verifica la connessione e riprova.');
    } finally {
      setPending(false);
    }
  };

  return (
    <section className="blog-interactions" aria-labelledby="blog-interactions-title">
      <h2 id="blog-interactions-title">Partecipa alla conversazione</h2>
      <div className="blog-reactions" aria-label="Reazioni all'articolo">
        <button
          type="button"
          className={`btn btn-outline-primary${viewer?.reaction === BlogReactionValue.Like ? ' active' : ''}`}
          onClick={() => void changeReaction(BlogReactionValue.Like)}
          disabled={
            !session.authenticated ||
            accountLoading ||
            !viewer ||
            viewer.portalAccessRevoked ||
            pending
          }
          aria-pressed={viewer?.reaction === BlogReactionValue.Like}
        >
          Mi piace <span>{summary.likes}</span>
        </button>
        <button
          type="button"
          className={`btn btn-outline-secondary${viewer?.reaction === BlogReactionValue.Dislike ? ' active' : ''}`}
          onClick={() => void changeReaction(BlogReactionValue.Dislike)}
          disabled={
            !session.authenticated ||
            accountLoading ||
            !viewer ||
            viewer.portalAccessRevoked ||
            pending
          }
          aria-pressed={viewer?.reaction === BlogReactionValue.Dislike}
        >
          Non mi piace <span>{summary.dislikes}</span>
        </button>
      </div>
      {!session.authenticated &&
        !accountLoading &&
        (sessionError ? (
          <p role="alert">Accesso account non verificabile: {sessionError}</p>
        ) : (
          <p className="text-secondary">
            <Link to="/account/login">Accedi</Link> per reagire o lasciare un commento.
          </p>
        ))}
      {viewer?.portalAccessRevoked && (
        <p role="status">
          Le interazioni non sono disponibili per questo account su questo portale.
        </p>
      )}
      {error && (
        <p className="alert alert-danger" role="alert">
          {error}
        </p>
      )}
      {message && (
        <p className="alert alert-success" role="status">
          {message}
        </p>
      )}
      {session.authenticated && !accountLoading && !viewer && !error && (
        <p role="status">Verifica delle autorizzazioni...</p>
      )}
      {session.authenticated &&
        !accountLoading &&
        viewer?.commentsBlocked &&
        !viewer.portalAccessRevoked && (
          <p role="status">Non puoi pubblicare commenti su questo portale.</p>
        )}
      {session.authenticated &&
        !accountLoading &&
        viewer &&
        !viewer.commentsBlocked &&
        !viewer.portalAccessRevoked && (
          <form className="blog-comment-form" onSubmit={(event) => void addComment(event)}>
            <label htmlFor="blog-comment">Il tuo commento</label>
            <textarea
              id="blog-comment"
              className="form-control"
              value={body}
              onChange={(event) => setBody(event.target.value)}
              minLength={1}
              maxLength={2000}
              rows={4}
              required
              disabled={pending}
            />
            <div className="d-flex justify-content-between align-items-center mt-2">
              <span className="text-secondary">{body.length}/2000</span>
              <button type="submit" className="btn btn-primary" disabled={pending || !body.trim()}>
                {pending ? 'Invio...' : 'Invia commento'}
              </button>
            </div>
          </form>
        )}
      <div className="blog-comments">
        <h3>
          Commenti <span>{summary.approvedComments}</span>
        </h3>
        {loading ? (
          <p role="status">Caricamento commenti...</p>
        ) : comments.length ? (
          <ul className="list-unstyled">
            {comments.map((comment) => (
              <li key={comment.id} className="blog-comment">
                <p className="mb-1">{comment.body}</p>
                <p className="text-secondary mb-0">
                  {comment.displayName} ·{' '}
                  <time dateTime={comment.createdAt}>
                    {new Date(comment.createdAt).toLocaleDateString()}
                  </time>
                </p>
              </li>
            ))}
          </ul>
        ) : (
          <p>Nessun commento ancora. La conversazione inizia da qui.</p>
        )}
        {summary.approvedComments > comments.length && (
          <button
            type="button"
            className="btn btn-outline-secondary"
            onClick={() => setPage((current) => current + 1)}
          >
            Mostra altri commenti
          </button>
        )}
      </div>
    </section>
  );
}
