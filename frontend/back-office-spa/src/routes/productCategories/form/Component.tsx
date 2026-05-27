import React, { useState, useEffect } from 'react';
import { Form, Button, Modal } from 'react-bootstrap';
import { useFetcher, useLoaderData, useNavigate, useParams } from 'react-router';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';
import PageHeader from '@components/PageHeader';
import type { VideoProductCategory } from '@morwalpizvideo/models';

const ProductCategoryForm: React.FC = () => {
  const { productCategory } = useLoaderData() as {
    productCategory: VideoProductCategory | null;
  };
  const params = useParams();
  const isEditMode = !!params.categoryId;

  const [title, setTitle] = useState(productCategory?.title || '');
  const [description, setDescription] = useState(productCategory?.description || '');
  const [showModal, setShowModal] = useState(false);

  const fetcher = useFetcher();
  const navigate = useNavigate();
  const toast = useToast();

  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;
  const result = fetcher.data != undefined && !fetcher.data.errors ? fetcher.data : null;

  useEffect(() => {
    if (productCategory) {
      setTitle(productCategory.title);
      setDescription(productCategory.description);
    }
  }, [productCategory]);

  useEffect(() => {
    if (!result || busy) return;
    if (result.success) {
      toast.show(
        'Success',
        isEditMode ? 'Product category updated successfully' : 'Product category created successfully',
        { variant: 'success' }
      );
      navigate('/product-categories');
    }
  }, [result, busy, navigate, isEditMode]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setShowModal(true);
  };

  const confirmSubmit = () => {
    fetcher.submit(
      { title, description },
      { method: 'post', action: location.pathname }
    );
  };

  return (
    <>
      <PageHeader title={isEditMode ? 'Edit Product Category' : 'Create Product Category'} />
      <GenericErrorList errors={errors?.generics} />

      <Form onSubmit={handleSubmit}>
        <Form.Group className="mb-3">
          <Form.Label>
            Title <span className="text-danger">*</span>
          </Form.Label>
          <Form.Control
            type="text"
            value={title}
            onChange={e => setTitle(e.target.value)}
            required
          />
          <FieldError error={errors?.title} />
        </Form.Group>

        <Form.Group className="mb-3">
          <Form.Label>
            Description <span className="text-danger">*</span>
          </Form.Label>
          <Form.Control
            as="textarea"
            rows={3}
            value={description}
            onChange={e => setDescription(e.target.value)}
            required
          />
          <FieldError error={errors?.description} />
        </Form.Group>

        <div className="d-flex gap-2 mt-2">
          <Button type="submit" variant="primary" disabled={busy}>
            {isEditMode ? 'Save Changes' : 'Create Category'}
          </Button>
          <Button
            type="button"
            variant="secondary"
            onClick={() => navigate('/product-categories')}
            disabled={busy}
          >
            Cancel
          </Button>
        </div>
      </Form>

      <Modal show={showModal} onHide={() => setShowModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>{isEditMode ? 'Confirm Edit' : 'Confirm Create'}</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <p>
            Are you sure you want to{' '}
            {isEditMode ? 'save the changes to' : 'create'} the following category?
          </p>
          <p>
            <strong>Title:</strong> {title}
            {isEditMode && productCategory && title !== productCategory.title && (
              <>
                {' '}
                (<s>{productCategory.title}</s>)
              </>
            )}
          </p>
          <p>
            <strong>Description:</strong> {description}
            {isEditMode && productCategory && description !== productCategory.description && (
              <>
                {' '}
                (<s>{productCategory.description}</s>)
              </>
            )}
          </p>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowModal(false)}>
            Cancel
          </Button>
          <Button
            variant={isEditMode ? 'primary' : 'success'}
            onClick={confirmSubmit}
            disabled={busy}
            data-testid={isEditMode ? 'edit-modal-confirm' : 'create-modal-confirm'}
          >
            {isEditMode ? 'Save Changes' : 'Create Category'}
          </Button>
        </Modal.Footer>
      </Modal>
    </>
  );
};

export default ProductCategoryForm;
