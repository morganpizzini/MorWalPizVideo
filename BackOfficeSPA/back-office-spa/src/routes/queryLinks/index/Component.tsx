import React, { useState, useEffect, useMemo } from 'react';
import { Button, Modal } from 'react-bootstrap';
import { Link, useLoaderData, useFetcher } from 'react-router';
import { QueryLink } from '@models';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';
import GenericTable from '@components/Table';
import { ColumnDef } from '@tanstack/react-table';

const QueryLinks: React.FC = () => {
  const [showModal, setShowModal] = useState(false);
  const [selectedLink, setSelectedLink] = useState<QueryLink | null>(null);
  const toast = useToast();

  const entities = useLoaderData<QueryLink[]>();

  const fetcher = useFetcher();
  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;
  const result =
    fetcher.data != undefined &&
    (fetcher.data.errors == undefined || fetcher.data.errors.length == 0)
      ? fetcher.data
      : null;

  useEffect(() => {
    if (!result) return;
    setShowModal(false);

    if (result.success) {
      toast.show('Success', 'Query link deleted successfully', { variant: 'success' });
    }
  }, [result, toast]);

  const handleDelete = (link: QueryLink) => {
    setSelectedLink(link);
    setShowModal(true);
  };

  const confirmDelete = () => {
    if (!selectedLink) return;
    fetcher.submit(
      {
        id: selectedLink.queryLinkId,
      },
      {
        method: 'post',
        action: location.pathname,
      }
    );
  };

  // Definizione delle colonne
  const columns = useMemo<ColumnDef<QueryLink>[]>(
    () => [
      {
        accessorKey: 'title',
        header: 'Title',
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
          const link = props.row.original;
          return (
            <div className="text-end">
              <Link className="btn btn-link px-1" to={`/querylinks/${link.queryLinkId}`}>
                Detail
              </Link>
              <Link className="btn btn-link px-1" to={`/querylinks/${link.queryLinkId}/edit`}>
                Edit
              </Link>
              <Button variant="link" className="px-1" onClick={() => handleDelete(link)}>
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
      <PageHeader title="Query Links" createLink={`./create`} />
      <GenericErrorList errors={errors?.generics} />

      <GenericTable
        data={entities}
        columns={columns}
        pageSize={10}
        searchPlaceholder="Search query links..."
        emptyMessage="No query links found"
      />

      <Modal show={showModal} onHide={() => setShowModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>Confirm Delete</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <p>Are you sure you want to delete the following query link?</p>
          <p>
            <strong>Title:</strong> {selectedLink?.title}
          </p>
          <p>
            <strong>Description:</strong> {selectedLink?.description}
          </p>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowModal(false)}>
            Cancel
          </Button>
          <Button
            variant="danger"
            disabled={busy}
            onClick={confirmDelete}
            data-testid="delete-modal-confirm"
          >
            Delete
          </Button>
        </Modal.Footer>
      </Modal>
    </>
  );
};

export default QueryLinks;
