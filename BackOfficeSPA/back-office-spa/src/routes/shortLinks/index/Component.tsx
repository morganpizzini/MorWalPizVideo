import React, { useState, useEffect, useMemo } from 'react';
import { Button, Modal } from 'react-bootstrap';
import { Link, useLoaderData, useFetcher } from 'react-router';
import { ShortLink, LinkType } from '@models';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';
import GenericTable from '@components/Table';
import { ColumnDef } from '@tanstack/react-table';

const ShortLinks: React.FC = () => {
  const [showModal, setShowModal] = useState(false);
  const [selectedLink, setSelectedLink] = useState<ShortLink | null>(null);
  const toast = useToast();

  const entities = useLoaderData<ShortLink[]>();

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
      toast.show('Success', 'Short link deleted successfully', { variant: 'success' });
    }
  }, [result, toast]);

  const handleDelete = (link: ShortLink) => {
    setSelectedLink(link);
    setShowModal(true);
  };

  const confirmDelete = () => {
    if (!selectedLink) return;
    const actionPath = location.pathname.substring(0, location.pathname.lastIndexOf('/'));
    fetcher.submit(
      {
        id: selectedLink.shortLinkId,
      },
      {
        method: 'post',
        action: actionPath,
      }
    );
  };
  // Definizione delle colonne
  const columns = useMemo<ColumnDef<ShortLink>[]>(
    () => [
      {
        accessorKey: 'target',
        header: 'Target',
        cell: info => info.getValue(),
      },      {
        accessorKey: 'linkType',
        header: 'Type',
        cell: info => {
          const linkTypeValue = info.getValue() as number;
          return LinkType[linkTypeValue];
        },
      },
      {
        accessorKey: 'queryString',
        header: 'Query String',
        cell: info => info.getValue(),
      },
      {
        accessorKey: 'clicksCount',
        header: 'Clicks Count',
        cell: info => info.getValue(),
      },
      {
        id: 'actions',
        header: () => <div className="text-end">Actions</div>,
        cell: props => {
          const link = props.row.original;
          return (
            <div className="text-end">
              <Link className="btn btn-link px-1" to={`/shortlinks/${link.shortLinkId}`}>
                Detail
              </Link>
              <Link className="btn btn-link px-1" to={`/shortlinks/${link.shortLinkId}/edit`}>
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
      <PageHeader title="Short Links" createLink={`./create`} />
      <GenericErrorList errors={errors?.generics} />

      <GenericTable
        data={entities}
        columns={columns}
        pageSize={10}
        searchPlaceholder="Search short links..."
        emptyMessage="No short links found"
      />

      <Modal show={showModal} onHide={() => setShowModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>Confirm Delete</Modal.Title>
        </Modal.Header>        <Modal.Body>
          <p>Are you sure you want to delete the following short link?</p>
          <p>
            <strong>Link Type:</strong> {selectedLink ? LinkType[selectedLink.linkType] : ''}
          </p>
          <p>
            <strong>Target:</strong> {selectedLink?.target}
          </p>
          <p>
            <strong>Query String:</strong> {selectedLink?.queryString}
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

export default ShortLinks;
