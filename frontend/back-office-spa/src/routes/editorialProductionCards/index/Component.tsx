import React, { useEffect, useState } from 'react';
import { Button, Modal } from 'react-bootstrap';
import { Link, useFetcher } from 'react-router';
import { useResolvedLoaderData } from '@/router/asyncData';
import type { EditorialProductionCard } from '@morwalpizvideo/models';
import GenericErrorList from '@components/GenericErrorList';
import GenericTable from '@components/Table';
import PageHeader from '@components/PageHeader';
import { useToast } from '@components/ToastNotification/ToastContext';
import { type ColumnDef } from '@tanstack/react-table';

const statuses = ['Idea', 'Planned', 'In progress', 'Ready', 'Published', 'Archived'];

export default function EditorialProductionCardsIndex(): React.ReactElement {
  const cards = useResolvedLoaderData() as EditorialProductionCard[];
  const fetcher = useFetcher();
  const toast = useToast();
  const [selected, setSelected] = useState<EditorialProductionCard | null>(null);
  const busy = fetcher.state !== 'idle';

  useEffect(() => {
    if (!busy && fetcher.data?.success) {
      setSelected(null);
      toast.show('Success', 'Editorial production card deleted', { variant: 'success' });
    }
  }, [busy, fetcher.data, toast]);

  const columns: ColumnDef<EditorialProductionCard>[] = [
    { accessorKey: 'title', header: 'Title' },
    { accessorKey: 'editorialAngle', header: 'Angle' },
    {
      id: 'status',
      header: 'Status',
      cell: info => statuses[Number(info.row.original.status)] ?? 'Unknown',
    },
    {
      id: 'dueDate',
      header: 'Due',
      cell: info =>
        info.row.original.dueDateUtc
          ? new Date(info.row.original.dueDateUtc).toLocaleDateString()
          : 'No date',
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: info => (
        <div className="text-end">
          <Link className="btn btn-link" to={`/editorialproductioncards/${info.row.original.id}`}>
            Open
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
      <PageHeader title="Editorial production" createLink="./create" />
      <GenericErrorList errors={fetcher.data?.errors} />
      <GenericTable
        data={cards}
        columns={columns}
        pageSize={10}
        searchPlaceholder="Search production cards..."
        emptyMessage="No editorial production cards found"
      />
      <Modal show={selected !== null} onHide={() => setSelected(null)}>
        <Modal.Header closeButton>
          <Modal.Title>Delete production card</Modal.Title>
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
              selected &&
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
