import { useResolvedLoaderData } from '@/router/asyncData';
import React, { useEffect, useState } from 'react';
import { Button, Modal } from 'react-bootstrap';
import { Link, useFetcher } from 'react-router';
import type { Gallery } from '@morwalpizvideo/models';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';
import GenericTable from '@components/Table';
import type { ColumnDef } from '@tanstack/react-table';

export default function GalleriesIndex(): React.ReactElement {
  const rawGalleries = useResolvedLoaderData<Gallery[] | { data?: Gallery[] }>();
  const galleries = Array.isArray(rawGalleries) ? rawGalleries : (rawGalleries.data ?? []);
  const fetcher = useFetcher();
  const toast = useToast();
  const [selected, setSelected] = useState<Gallery | null>(null);
  const busy = fetcher.state !== 'idle';

  useEffect(() => {
    if (!busy && fetcher.data?.success) {
      setSelected(null);
      toast.show('Success', 'Gallery deleted successfully', { variant: 'success' });
    }
  }, [busy, fetcher.data, toast]);

  const columns: ColumnDef<Gallery>[] = [
    { accessorKey: 'name', header: 'Name' },
    { accessorKey: 'slug', header: 'Public slug' },
    { id: 'images', header: 'Images', cell: info => info.row.original.images.length },
    { accessorKey: 'downloadCount', header: 'Downloads' },
    {
      id: 'actions',
      header: () => <div className="text-end">Actions</div>,
      cell: info => (
        <div className="text-end">
          <Link className="btn btn-link px-1" to={`/galleries/${info.row.original.id}`}>
            View
          </Link>
          <Link className="btn btn-link px-1" to={`/galleries/${info.row.original.id}/edit`}>
            Edit
          </Link>
          <Button variant="link" className="px-1" onClick={() => setSelected(info.row.original)}>
            Delete
          </Button>
        </div>
      ),
    },
  ];

  return (
    <>
      <PageHeader title="Galleries" createLink="./create" />
      <GenericErrorList errors={fetcher.data?.errors} />
      <GenericTable
        data={galleries}
        columns={columns}
        getRowId={gallery => gallery.id}
        pageSize={10}
        searchPlaceholder="Search galleries..."
        emptyMessage="No galleries found"
      />
      <Modal show={selected !== null} onHide={() => setSelected(null)}>
        <Modal.Header closeButton>
          <Modal.Title>Delete gallery</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          Delete <strong>{selected?.name}</strong>?
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
