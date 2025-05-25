import React, { useState, useEffect, useMemo } from 'react';
import { Button, Modal } from 'react-bootstrap';
import { Link, useLoaderData, useFetcher } from 'react-router';
import { MorWalPizConfiguration } from '@/models/configuration';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';
import GenericTable from '@components/Table';
import { ColumnDef } from '@tanstack/react-table';

const ConfigurationsIndex: React.FC = () => {
  const [showModal, setShowModal] = useState(false);
  const [selectedConfiguration, setSelectedConfiguration] = useState<MorWalPizConfiguration | null>(null);
  const toast = useToast();

  const configurations = useLoaderData<MorWalPizConfiguration[]>();

  const fetcher = useFetcher();
  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;
  const result =
    fetcher.data != undefined &&
      (fetcher.data.errors === undefined || fetcher.data.errors.length === 0)
      ? fetcher.data
      : null;

  useEffect(() => {
    if (!result) return;
    setShowModal(false);

    if (result.success) {
      toast.show('Success', 'Configuration deleted successfully', { variant: 'success' });
    }
  }, [result, toast]);

  const handleDelete = (configuration: MorWalPizConfiguration) => {
    setSelectedConfiguration(configuration);
    setShowModal(true);
  };

  const confirmDelete = () => {
    if (!selectedConfiguration) return;
    fetcher.submit(
      {
        id: selectedConfiguration.id,
      },
      {
        method: 'post',
        action: location.pathname,
      }
    );
  };

  // Column definitions
  const columns = useMemo<ColumnDef<MorWalPizConfiguration>[]>(
    () => [
      {
        accessorKey: 'key',
        header: 'Key',
        cell: info => info.getValue(),
      },
      {
        accessorKey: 'value',
        header: 'Value',
        cell: info => info.getValue(),
      },
      {
        accessorKey: 'type',
        header: 'Type',
        cell: info => info.getValue(),
      },
      {
        accessorKey: 'description',
        header: 'Description',
        cell: info => info.getValue(),
      },
      {
        id: 'actions',
        header: () => <div className="text-end">Actions</div>,
        cell: props => {
          const configuration = props.row.original;
          return (
            <div className="text-end">
              <Link className="btn btn-link px-1" to={`/morwalpizconfigurations/${configuration.id}`}>
                Detail
              </Link>
              <Link className="btn btn-link px-1" to={`/morwalpizconfigurations/${configuration.id}/edit`}>
                Edit
              </Link>
              <Button variant="link" className="px-1" onClick={() => handleDelete(configuration)}>
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
      <PageHeader title="Configurations" createLink="./create" />
      <GenericErrorList errors={errors?.generics} />

      <GenericTable
        data={configurations}
        columns={columns}
        pageSize={10}
        searchPlaceholder="Search configurations..."
        emptyMessage="No configurations found"
      />

      <Modal show={showModal} onHide={() => setShowModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>Confirm Delete</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          {selectedConfiguration && (
            <p>
              Are you sure you want to delete the configuration <strong>{selectedConfiguration.key}</strong>?
              This action cannot be undone.
            </p>
          )}
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowModal(false)} disabled={busy}>
            Cancel
          </Button>
          <Button variant="danger" onClick={confirmDelete} disabled={busy}>
            {busy ? 'Deleting...' : 'Delete'}
          </Button>
        </Modal.Footer>
      </Modal>
    </>
  );
};

export default ConfigurationsIndex;
