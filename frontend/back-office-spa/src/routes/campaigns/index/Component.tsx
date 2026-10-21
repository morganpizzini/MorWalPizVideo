import { useEffect, useMemo, useState } from 'react';
import { Button, Modal } from 'react-bootstrap';
import { Link, useFetcher } from 'react-router';
import { ColumnDef } from '@tanstack/react-table';
import { Campaign, CampaignStatus } from '@/models';
import { useResolvedLoaderData } from '@/router/asyncData';
import GenericErrorList from '@components/GenericErrorList';
import GenericTable from '@components/Table';
import PageHeader from '@components/PageHeader';
import { useToast } from '@components/ToastNotification/ToastContext';

const statusLabel = (status: CampaignStatus) => CampaignStatus[status] ?? String(status);

export default function CampaignsIndex() {
  const campaigns = useResolvedLoaderData<Campaign[]>();
  const fetcher = useFetcher();
  const toast = useToast();
  const [selected, setSelected] = useState<Campaign | null>(null);
  const busy = fetcher.state !== 'idle';
  useEffect(() => {
    if (fetcher.data?.success) {
      setSelected(null);
      toast.show('Success', 'Campaign deleted successfully.', { variant: 'success' });
    }
  }, [fetcher.data, toast]);
  const columns = useMemo<ColumnDef<Campaign>[]>(
    () => [
      { accessorKey: 'title', header: 'Title' },
      { accessorKey: 'partnerId', header: 'Partner' },
      {
        accessorKey: 'status',
        header: 'Status',
        cell: info => statusLabel(info.getValue<CampaignStatus>()),
      },
      {
        accessorKey: 'startAt',
        header: 'Starts',
        cell: info => info.getValue<string | null>()?.slice(0, 10) ?? 'Not scheduled',
      },
      {
        id: 'actions',
        header: () => <div className="text-end">Actions</div>,
        cell: info => {
          const campaign = info.row.original;
          return (
            <div className="text-end">
              <Link className="btn btn-link px-1" to={campaign.id}>
                Detail
              </Link>
              <Link className="btn btn-link px-1" to={`${campaign.id}/edit`}>
                Edit
              </Link>
              <Button variant="link" className="px-1" onClick={() => setSelected(campaign)}>
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
      <PageHeader title="Campaigns" createLink="./create" />
      <GenericErrorList errors={fetcher.data?.errors?.generics} />
      <GenericTable
        data={campaigns}
        columns={columns}
        getRowId={campaign => campaign.id}
        pageSize={10}
        searchPlaceholder="Search campaigns..."
        emptyMessage="No campaigns found"
      />
      <Modal show={selected !== null} onHide={() => setSelected(null)}>
        <Modal.Header closeButton>
          <Modal.Title>Confirm Delete</Modal.Title>
        </Modal.Header>
        <Modal.Body>Are you sure you want to delete {selected?.title}?</Modal.Body>
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
