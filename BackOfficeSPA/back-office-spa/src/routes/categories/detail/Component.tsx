import React, { useState, useEffect } from 'react';
import { useLoaderData, useFetcher, useNavigate } from 'react-router';
import { Button, Modal } from 'react-bootstrap';
import { useToast } from '@components/ToastNotification/ToastContext';
import DetailPanel from '@components/DetailPanel';
import PageHeader from '@components/PageHeader';
import { Category } from '@models';

const CategoryDetail: React.FC = () => {
  const category = useLoaderData<Category>();
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
      toast.show('Success', 'Category deleted successfully', { variant: 'success' });
      navigate('..');
    }
  }, [result, navigate, toast]);

  const handleDelete = () => {
    setShowModal(true);
  };

  const confirmDelete = () => {
    const actionPath = location.pathname.substring(0, location.pathname.lastIndexOf('/'));
    fetcher.submit(
      { id: category.categoryId },
      {
        method: 'post',
        action: actionPath,
      }
    );
  };

  if (!category) {
    return <div>Loading...</div>;
  }

  return (
    <>
      <PageHeader
        title={category.title}
        editLink={`/categories/${category.categoryId}/edit`}
        deleteCallback={handleDelete}
      />
      <DetailPanel title="Category Details">
        <p>
          <strong>Title:</strong> {category.title}
        </p>
        <p>
          <strong>Description:</strong> {category.description}
        </p>
      </DetailPanel>

      <Modal show={showModal} onHide={() => setShowModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>Confirm Delete</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <p>Are you sure you want to delete the following category?</p>
          <p>
            <strong>Title:</strong> {category.title}
          </p>
          <p>
            <strong>Description:</strong> {category.description}
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

export default CategoryDetail;
