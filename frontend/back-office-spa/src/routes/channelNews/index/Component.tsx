import React, { useEffect, useState } from 'react';
import { Button, Modal } from 'react-bootstrap';
import { Link, useFetcher, useLoaderData } from 'react-router';
import type { ChannelNewsAdmin } from '@morwalpizvideo/models';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';
import GenericTable from '@components/Table';
import { type ColumnDef } from '@tanstack/react-table';

const statusLabels = ['Draft', 'Scheduled', 'Published', 'Archived'];

export default function ChannelNewsIndex(): React.ReactElement {
  const entities = useLoaderData() as ChannelNewsAdmin[];
  const fetcher = useFetcher();
  const toast = useToast();
  const [selected, setSelected] = useState<ChannelNewsAdmin | null>(null);
  const busy = fetcher.state !== 'idle';

  useEffect(() => {
    if (!busy && fetcher.data?.success) {
      setSelected(null);
      toast.show('Success', 'ChannelNews deleted successfully', { variant: 'success' });
    }
  }, [busy, fetcher.data, toast]);

  const columns: ColumnDef<ChannelNewsAdmin>[] = [
    { accessorKey: 'title', header: 'Title' },
    { accessorKey: 'slug', header: 'Slug' },
    {
      id: 'status',
      header: 'Status',
      cell: info => statusLabels[Number(info.row.original.status)] ?? 'Unknown',
    },
    {
      id: 'updated',
      header: 'Updated',
      cell: info => new Date(info.row.original.updatedDateTime).toLocaleString(),
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: info => (
        <div className="text-end">
          <Link className="btn btn-link" to={`/channelnews/${info.row.original.id}/edit`}>
            Edit
          </Link>
          <Button variant="link" onClick={() => setSelected(info.row.original)}>
            Delete
          </Button>
        </div>
      ),
    },
  ];

  return (
    <>
      <PageHeader title="ChannelNews" createLink="./create" />
      <GenericErrorList errors={fetcher.data?.errors} />
      <GenericTable
        data={entities}
        columns={columns}
        pageSize={10}
        searchPlaceholder="Search ChannelNews..."
        emptyMessage="No ChannelNews found"
      />
      <Modal show={selected !== null} onHide={() => setSelected(null)}>
        <Modal.Header closeButton>
          <Modal.Title>Delete ChannelNews</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          Delete <strong>{selected?.title}</strong>?
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setSelected(null)}>
            Cancel
          </Button>
          <Button
            variant="danger"
            disabled={busy}
            onClick={() =>
              selected?.id &&
              fetcher.submit({ id: selected.id }, { method: 'post', action: location.pathname })
            }
          >
            Delete
          </Button>
        </Modal.Footer>
      </Modal>
    </>
  );
}
