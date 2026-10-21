import { useEffect, useMemo, useState } from 'react';
import { Button, Modal } from 'react-bootstrap';
import { Link, useFetcher } from 'react-router';
import { ColumnDef } from '@tanstack/react-table';
import { Partner, PartnerStatus } from '@/models';
import { useResolvedLoaderData } from '@/router/asyncData';
import GenericErrorList from '@components/GenericErrorList';
import GenericTable from '@components/Table';
import PageHeader from '@components/PageHeader';
import { useToast } from '@components/ToastNotification/ToastContext';

const statusLabel = (status: PartnerStatus) => PartnerStatus[status] ?? String(status);

export default function PartnersIndex() {
  const partners = useResolvedLoaderData<Partner[]>();
  const fetcher = useFetcher();
  const toast = useToast();
  const [selected, setSelected] = useState<Partner | null>(null);
  const busy = fetcher.state !== 'idle';
  const result = fetcher.data;

  useEffect(() => {
    if (result?.success) {
      setSelected(null);
      toast.show('Success', 'Partner deleted successfully.', { variant: 'success' });
    }
  }, [result, toast]);

  const columns = useMemo<ColumnDef<Partner>[]>(
    () => [
      { accessorKey: 'name', header: 'Name' },
      { accessorKey: 'contactName', header: 'Contact' },
      { accessorKey: 'contactEmail', header: 'Email' },
      {
        accessorKey: 'status',
        header: 'Status',
        cell: info => statusLabel(info.getValue<PartnerStatus>()),
      },
      {
        id: 'actions',
        header: () => <div className="text-end">Actions</div>,
        cell: info => {
          const partner = info.row.original;
          return (
            <div className="text-end">
              <Link className="btn btn-link px-1" to={partner.id}>
                Detail
              </Link>
              <Link className="btn btn-link px-1" to={`${partner.id}/edit`}>
                Edit
              </Link>
              <Button variant="link" className="px-1" onClick={() => setSelected(partner)}>
                Delete
              </Button>
            </div>
          );
        },
      },
    ],
    []
  );

  return (
    <>
      <PageHeader title="Partners" createLink="./create" />
      <GenericErrorList errors={fetcher.data?.errors?.generics} />
      <GenericTable
        data={partners}
        columns={columns}
        getRowId={partner => partner.id}
        pageSize={10}
        searchPlaceholder="Search partners..."
        emptyMessage="No partners found"
      />
      <Modal show={selected !== null} onHide={() => setSelected(null)}>
        <Modal.Header closeButton>
          <Modal.Title>Confirm Delete</Modal.Title>
        </Modal.Header>
        <Modal.Body>Are you sure you want to delete {selected?.name}?</Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setSelected(null)}>
            Cancel
          </Button>
          <Button
            variant="danger"
            disabled={busy}
            onClick={() => selected && fetcher.submit({ id: selected.id }, { method: 'post' })}
          >
            Delete
          </Button>
        </Modal.Footer>
      </Modal>
    </>
  );
}
