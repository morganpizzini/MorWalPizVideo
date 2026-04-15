import React, { useState, useEffect, useMemo } from 'react';
import { Button, Modal, Badge } from 'react-bootstrap';
import { Link, useLoaderData, useFetcher, useLocation } from 'react-router';
import { ApiKeyDto } from '../../../models';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';
import GenericTable from '@components/Table';
import { ColumnDef } from '@tanstack/react-table';

const ApiKeys: React.FC = () => {
  const [showModal, setShowModal] = useState(false);
  const [selectedApiKey, setSelectedApiKey] = useState<ApiKeyDto | null>(null);
  const [modalAction, setModalAction] = useState<'delete' | 'toggle'>('delete');
  const toast = useToast();
  const location = useLocation();

  const apiKeys = useLoaderData<ApiKeyDto[]>();

  const fetcher = useFetcher();
  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;
  const result =
    fetcher.data != undefined &&
      (fetcher.data.errors == undefined || fetcher.data.errors.length == 0)
      ? fetcher.data
      : null;

  // Define columns
  const columns = useMemo<ColumnDef<ApiKeyDto>[]>(
    () => [
      {
        accessorKey: 'name',
        header: 'Name',
        cell: info => info.getValue(),
      },
      {
        accessorKey: 'description',
        header: 'Description',
        cell: info => {
          const desc = info.getValue() as string;
          return desc ? (desc.length > 100 ? desc.substring(0, 100) + '...' : desc) : '-';
        },
      },
      {
        accessorKey: 'isActive',
        header: 'Status',
        cell: info => {
          const isActive = info.getValue() as boolean;
          return (
            <Badge bg={isActive ? 'success' : 'secondary'}>
              {isActive ? 'Active' : 'Inactive'}
            </Badge>
          );
        },
      },
      {
        accessorKey: 'expiresAt',
        header: 'Expires At',
        cell: info => {
          const dateValue = info.getValue() as string | null;
          return dateValue ? new Date(dateValue).toLocaleDateString() : 'Never';
        },
      },
      {
        accessorKey: 'createdAt',
        header: 'Created',
        cell: info => {
          const dateValue = info.getValue() as string;
          return new Date(dateValue).toLocaleDateString();
        },
      },
      {
        id: 'actions',
        header: () => <div className="text-end">Actions</div>,
        cell: props => {
          const apiKey = props.row.original;
          return (
            <div className="text-end">
              <Link className="btn btn-link px-1" to={`/keys/${apiKey.id}`}>
                Detail
              </Link>
              <Link className="btn btn-link px-1" to={`/keys/${apiKey.id}/edit`}>
                Edit
              </Link>
              <Button
                variant="link"
                className="px-1"
                onClick={() => handleToggle(apiKey)}
              >
                {apiKey.isActive ? 'Deactivate' : 'Activate'}
              </Button>
              <Button variant="link" className="px-1" onClick={() => handleDelete(apiKey)}>
                Delete
              </Button>
            </div>
          );
        },
      },
    ],
    []
  );

  useEffect(() => {
    if (!result) return;
    setShowModal(false);

    if (result.success) {
      if (modalAction === 'delete') {
        toast.show('Success', 'API key deleted successfully', { variant: 'success' });
      } else if (modalAction === 'toggle') {
        const message = result.toggleResult?.isActive
          ? 'API key activated successfully'
          : 'API key deactivated successfully';
        toast.show('Success', message, { variant: 'success' });
      }
    }
  }, [result]);

  const handleDelete = (apiKey: ApiKeyDto) => {
    setSelectedApiKey(apiKey);
    setModalAction('delete');
    setShowModal(true);
  };

  const handleToggle = (apiKey: ApiKeyDto) => {
    setSelectedApiKey(apiKey);
    setModalAction('toggle');
    setShowModal(true);
  };

  const confirmAction = () => {
    if (!selectedApiKey) return;
    fetcher.submit(
      {
        id: selectedApiKey.id,
        intent: modalAction,
      },
      {
        method: 'post',
        action: location.pathname,
      }
    );
  };

  return (
    <>
      <PageHeader title="API Keys" createLink="./create" />
      <GenericErrorList errors={errors?.generics} />

      <GenericTable
        data={apiKeys}
        columns={columns}
        pageSize={10}
        searchPlaceholder="Search API keys..."
      />

      <Modal show={showModal} onHide={() => setShowModal(false)} centered>
        <Modal.Header closeButton>
          <Modal.Title>
            {modalAction === 'delete' ? 'Confirm Delete' : 'Confirm Toggle Status'}
          </Modal.Title>
        </Modal.Header>
        <Modal.Body>
          {modalAction === 'delete' ? (
            <>
              Are you sure you want to delete the API key "{selectedApiKey?.name}"?
              <div className="alert alert-warning mt-3">
                <strong>Warning:</strong> This action cannot be undone. Any applications using
                this API key will lose access immediately.
              </div>
            </>
          ) : (
            <>
              Are you sure you want to {selectedApiKey?.isActive ? 'deactivate' : 'activate'} the
              API key "{selectedApiKey?.name}"?
              {selectedApiKey?.isActive && (
                <div className="alert alert-warning mt-3">
                  <strong>Warning:</strong> Deactivating this key will immediately prevent any
                  applications from using it.
                </div>
              )}
            </>
          )}
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowModal(false)}>
            Cancel
          </Button>
          <Button
            variant={modalAction === 'delete' ? 'danger' : 'primary'}
            onClick={confirmAction}
            disabled={busy}
          >
            {busy
              ? modalAction === 'delete'
                ? 'Deleting...'
                : 'Processing...'
              : modalAction === 'delete'
                ? 'Delete'
                : selectedApiKey?.isActive
                  ? 'Deactivate'
                  : 'Activate'}
          </Button>
        </Modal.Footer>
      </Modal>
    </>
  );
};

export default ApiKeys;