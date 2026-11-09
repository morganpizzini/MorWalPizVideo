import { Link } from 'react-router';

export default function NotFoundPage() {
  return (
    <section className="card">
      <div className="card-body">
        <h1 className="h3">Not found</h1>
        <p className="text-muted">The page you requested could not be found.</p>
        <Link className="btn btn-primary" to="/">
          Go home
        </Link>
      </div>
    </section>
  );
}
