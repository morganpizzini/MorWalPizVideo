import React, { useState, useEffect } from 'react';
import { useLoaderData, useFetcher, useNavigate } from 'react-router';
import { Button, Modal } from 'react-bootstrap';
import { useToast } from '@components/ToastNotification/ToastContext';
import DetailPanel from '@components/DetailPanel';
import PageHeader from '@components/PageHeader';

const QueryLinkDetail: React.FC = () => {
  const entity = useLoaderData();
  const [showModal, setShowModal] = useState(false);
  const navigate = useNavigate();
  const toast = useToast();

  const fetcher = useFetcher();
  const busy = fetcher.state !== 'idle';
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
      navigate('..');
    }
  }, [result, navigate, toast]);

  const handleDelete = () => {
    setShowModal(true);
  };

  const confirmDelete = () => {
    const actionPath = location.pathname.substring(0, location.pathname.lastIndexOf('/'));
    fetcher.submit(
      { id: entity.queryLinkId },
      {
        method: 'post',
        action: actionPath,
      }
    );
  };

  if (!entity) {
    return <div>Loading...</div>;
  }

  return (
    <>
      <PageHeader
        title="Query Link Detail"
        editLink={`/querylinks/${entity.queryLinkId}/edit`}
        deleteCallback={handleDelete}
      />
      <DetailPanel title="Dettagli dell'entità">
        <p>
          <strong>Title:</strong> {entity.title}
        </p>
        <p>
          <strong>Value:</strong> {entity.value}
        </p>
      </DetailPanel>

      <Modal show={showModal} onHide={() => setShowModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>Confirm Delete</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <p>Are you sure you want to delete the following query link?</p>
          <p>
            <strong>Title:</strong> {entity.title}
          </p>
          <p>
            <strong>Value:</strong> {entity.value}
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

export default QueryLinkDetail;
