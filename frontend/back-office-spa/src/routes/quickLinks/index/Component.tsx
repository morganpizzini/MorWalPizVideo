import React, { useEffect, useState } from 'react';
import { Button, Modal } from 'react-bootstrap';
import { Link, useFetcher, useLoaderData } from 'react-router';
import type { QuickLinks } from '@morwalpizvideo/models';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';
import GenericTable from '@components/Table';
import { type ColumnDef } from '@tanstack/react-table';

const QuickLinksIndex: React.FC = () => {
  const entities = useLoaderData() as QuickLinks[];
  const fetcher = useFetcher();
  const toast = useToast();
  const [selected, setSelected] = useState<QuickLinks | null>(null);
  const busy = fetcher.state !== 'idle';

  useEffect(() => {
    if (!busy && fetcher.data?.success) {
      setSelected(null);
      toast.show('Success', 'QuickLinks deleted successfully', { variant: 'success' });
    }
  }, [busy, fetcher.data, toast]);

  const columns: ColumnDef<QuickLinks>[] = [
    { accessorKey: 'title', header: 'Title' },
    { accessorKey: 'url', header: 'Public slug' },
    { id: 'links', header: 'Links', cell: info => info.row.original.links.length },
    {
      id: 'actions',
      header: 'Actions',
      cell: info => <div className="text-end"><Link className="btn btn-link" to={`/quicklinks/${info.row.original.id}`}>View</Link><Link className="btn btn-link" to={`/quicklinks/${info.row.original.id}/edit`}>Edit</Link><Button variant="link" onClick={() => setSelected(info.row.original)}>Delete</Button></div>,
    },
  ];

  return <>
    <PageHeader title="QuickLinks" createLink="./create" />
    <GenericErrorList errors={fetcher.data?.errors} />
    <GenericTable data={entities} columns={columns} pageSize={10} searchPlaceholder="Search QuickLinks..." emptyMessage="No QuickLinks found" />
    <Modal show={selected !== null} onHide={() => setSelected(null)}>
      <Modal.Header closeButton><Modal.Title>Delete QuickLinks</Modal.Title></Modal.Header>
      <Modal.Body>Delete <strong>{selected?.title}</strong> and its public links?</Modal.Body>
      <Modal.Footer>
        <Button variant="secondary" onClick={() => setSelected(null)}>Cancel</Button>
        <Button variant="danger" disabled={busy} onClick={() => selected?.id && fetcher.submit({ id: selected.id }, { method: 'post', action: location.pathname })}>Delete</Button>
      </Modal.Footer>
    </Modal>
  </>;
};

export default QuickLinksIndex;
