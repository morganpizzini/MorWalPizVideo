import React, { useEffect, useState } from 'react';
import { Button, Modal } from 'react-bootstrap';
import { Link, useFetcher, useLoaderData } from 'react-router';
import type { PageAdmin } from '@morwalpizvideo/models';
import { PageStatus } from '@morwalpizvideo/models';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';
import GenericTable from '@components/Table';
import type { ColumnDef } from '@tanstack/react-table';

export default function PagesIndex(): React.ReactElement {
  const pages = useLoaderData() as PageAdmin[];
  const fetcher = useFetcher();
  const toast = useToast();
  const [selected, setSelected] = useState<PageAdmin | null>(null);
  const busy = fetcher.state !== 'idle';

  useEffect(() => {
    if (!busy && fetcher.data?.success) {
      setSelected(null);
      toast.show('Success', 'Page deleted successfully', { variant: 'success' });
    }
  }, [busy, fetcher.data, toast]);

  const columns: ColumnDef<PageAdmin>[] = [
    { accessorKey: 'title', header: 'Title' },
    { accessorKey: 'url', header: 'Public URL' },
    { id: 'status', header: 'Status', cell: info => info.row.original.status === PageStatus.Published ? 'Published' : 'Draft' },
    { id: 'actions', header: 'Actions', cell: info => <div className="text-end"><Link className="btn btn-link" to={`/pages/${info.row.original.id}`}>View</Link><Link className="btn btn-link" to={`/pages/${info.row.original.id}/edit`}>Edit</Link><Button variant="link" onClick={() => setSelected(info.row.original)}>Delete</Button></div> },
  ];

  return <>
    <PageHeader title="Pages" createLink="./create" />
    <GenericErrorList errors={fetcher.data?.errors} />
    <GenericTable data={pages} columns={columns} pageSize={10} searchPlaceholder="Search pages..." emptyMessage="No pages found" />
    <Modal show={selected !== null} onHide={() => setSelected(null)}>
      <Modal.Header closeButton><Modal.Title>Delete page</Modal.Title></Modal.Header>
      <Modal.Body>Delete <strong>{selected?.title}</strong>? Linked navigation items and inline images will be removed.</Modal.Body>
      <Modal.Footer><Button variant="secondary" onClick={() => setSelected(null)}>Cancel</Button><Button variant="danger" disabled={busy} onClick={() => selected?.id && fetcher.submit({ id: selected.id }, { method: 'post', action: location.pathname })}>Delete</Button></Modal.Footer>
    </Modal>
  </>;
}