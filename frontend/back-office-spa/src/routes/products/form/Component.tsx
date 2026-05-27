import React, { useState, useEffect } from 'react';
import { Form, Button, Modal } from 'react-bootstrap';
import { useFetcher, useLoaderData, useNavigate, useParams } from 'react-router';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';
import MultiSelectWithBadges from '@components/MultiSelectWithBadges';
import PageHeader from '@components/PageHeader';
import type { Product, VideoProductCategory } from '@morwalpizvideo/models';

const ProductForm: React.FC = () => {
  const { product, categories } = useLoaderData() as {
    product: Product | null;
    categories: VideoProductCategory[];
  };
  const params = useParams();
  const isEditMode = !!params.productId;

  const [title, setTitle] = useState(product?.title || '');
  const [description, setDescription] = useState(product?.description || '');
  const [url, setUrl] = useState(product?.url || '');
  const [selectedCategories, setSelectedCategories] = useState<VideoProductCategory[]>([]);
  const [showModal, setShowModal] = useState(false);

  const fetcher = useFetcher();
  const navigate = useNavigate();
  const toast = useToast();

  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;
  const result = fetcher.data != undefined && !fetcher.data.errors ? fetcher.data : null;

  useEffect(() => {
    if (product && categories) {
      setTitle(product.title);
      setDescription(product.description);
      setUrl(product.url);
      setSelectedCategories(
        (product.categories || [])
          .map(cat => categories.find(c => c.id === cat.id))
          .filter(Boolean) as VideoProductCategory[]
      );
    }
  }, [product, categories]);

  useEffect(() => {
    if (!result || busy) return;
    if (result.success) {
      toast.show(
        'Success',
        isEditMode ? 'Product updated successfully' : 'Product created successfully',
        { variant: 'success' }
      );
      navigate('/products');
    }
  }, [result, busy, navigate, isEditMode]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setShowModal(true);
  };

  const confirmSubmit = () => {
    const categoryIds = selectedCategories.map(cat => cat.id);
    fetcher.submit(
      { title, description, url, categoryIds: JSON.stringify(categoryIds) },
      { method: 'post', action: location.pathname }
    );
  };

  return (
    <>
      <PageHeader title={isEditMode ? 'Edit Product' : 'Create Product'} />
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

        <Form.Group className="mb-3">
          <Form.Label>
            URL <span className="text-danger">*</span>
          </Form.Label>
          <Form.Control
            type="url"
            value={url}
            onChange={e => setUrl(e.target.value)}
            required
          />
          <FieldError error={errors?.url} />
        </Form.Group>

        <MultiSelectWithBadges
          label="Categories"
          items={categories}
          selectedItems={selectedCategories}
          onSelectionChange={setSelectedCategories}
          getItemId={cat => cat.id}
          getItemDisplay={cat => cat.title}
          placeholder="Select a category"
        />

        <div className="d-flex gap-2 mt-3">
          <Button type="submit" variant="primary" disabled={busy}>
            {isEditMode ? 'Save Changes' : 'Create Product'}
          </Button>
          <Button
            type="button"
            variant="secondary"
            onClick={() => navigate('/products')}
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
            {isEditMode ? 'save the changes to' : 'create'} the following product?
          </p>
          <p>
            <strong>Title:</strong> {title}
          </p>
          <p>
            <strong>URL:</strong> {url}
          </p>
          {selectedCategories.length > 0 && (
            <p>
              <strong>Categories:</strong>{' '}
              {selectedCategories.map(c => c.title).join(', ')}
            </p>
          )}
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
            {isEditMode ? 'Save Changes' : 'Create Product'}
          </Button>
        </Modal.Footer>
      </Modal>
    </>
  );
};

export default ProductForm;
