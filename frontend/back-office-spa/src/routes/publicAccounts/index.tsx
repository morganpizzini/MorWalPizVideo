import { useEffect, useState, type FormEvent } from 'react';
import { Alert, Button, Form, Table } from 'react-bootstrap';
import { Link } from 'react-router';
import { getPublicAccountDirectory } from '@morwalpizvideo/services';
import type {
  PublicAccountDirectoryItem,
  PublicAccountDirectoryPage,
} from '@morwalpizvideo/models';

export default function PublicAccountsPage() {
  const [page, setPage] = useState<PublicAccountDirectoryPage | null>(null);
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const [pageNumber, setPageNumber] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError('');
    void getPublicAccountDirectory(query, pageNumber)
      .then(result => {
        if (active) setPage(result);
      })
      .catch(() => {
        if (active) setError('Unable to load public accounts.');
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [pageNumber, query]);

  const submitSearch = (event: FormEvent) => {
    event.preventDefault();
    setPageNumber(1);
    setQuery(search.trim());
  };

  return (
    <main>
      <h1 className="h3 mb-3">Public accounts</h1>
      <p className="text-secondary">Shared accounts used across MorWalPiz public portals.</p>
      <Form className="d-flex gap-2 mb-3" role="search" onSubmit={submitSearch}>
        <Form.Label htmlFor="public-account-search" className="visually-hidden">
          Search accounts
        </Form.Label>
        <Form.Control
          id="public-account-search"
          value={search}
          onChange={event => setSearch(event.target.value)}
          placeholder="Username or display name"
        />
        <Button type="submit" variant="outline-primary">
          Search
        </Button>
      </Form>
      {error && (
        <Alert variant="danger" role="alert">
          {error}
        </Alert>
      )}
      {loading ? <p role="status">Loading accounts...</p> : null}
      {!loading && !error && !page?.items.length ? <p>No public accounts found.</p> : null}
      {page?.items.length ? (
        <>
          <Table responsive hover className="align-middle">
            <thead>
              <tr>
                <th>Username</th>
                <th>Name</th>
                <th>Status</th>
                <th>Registered</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {page.items.map((account: PublicAccountDirectoryItem) => (
                <tr key={account.id}>
                  <td>{account.username}</td>
                  <td>{account.displayName}</td>
                  <td>{account.isActive ? 'Active' : 'Inactive'}</td>
                  <td>{new Date(account.createdAt).toLocaleDateString()}</td>
                  <td className="text-end">
                    <Link
                      className="btn btn-sm btn-outline-primary"
                      to={`/public-accounts/${encodeURIComponent(account.id)}`}
                    >
                      Interactions & access
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </Table>
          <div className="d-flex justify-content-between align-items-center">
            <span>{page.totalCount} accounts</span>
            <div className="d-flex gap-2">
              <Button
                variant="outline-secondary"
                disabled={pageNumber <= 1}
                onClick={() => setPageNumber(value => value - 1)}
              >
                Previous
              </Button>
              <span className="align-self-center">Page {page.page}</span>
              <Button
                variant="outline-secondary"
                disabled={pageNumber * page.pageSize >= page.totalCount}
                onClick={() => setPageNumber(value => value + 1)}
              >
                Next
              </Button>
            </div>
          </div>
        </>
      ) : null}
    </main>
  );
}
