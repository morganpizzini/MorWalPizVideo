import { Link } from 'react-router';
import { ShieldX } from 'lucide-react';

export default function ForbiddenPage() {
  return (
    <main className="container py-5 text-center" aria-labelledby="forbidden-title">
      <ShieldX size={42} className="text-danger mb-3" aria-hidden="true" />
      <h1 id="forbidden-title" className="h3">Access denied</h1>
      <p className="text-muted">Your account does not have permission to open this area.</p>
      <Link to="/" className="btn btn-primary">Back to dashboard</Link>
    </main>
  );
}