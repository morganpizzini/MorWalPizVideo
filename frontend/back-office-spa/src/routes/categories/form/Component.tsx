import React, { useEffect, useState } from 'react';
import { Form, Button, Modal } from 'react-bootstrap';
import { useFetcher, useLoaderData, useNavigate, useParams } from 'react-router';
import type { Category } from '@morwalpizvideo/models';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';
import { useToast } from '@components/ToastNotification/ToastContext';
import PageHeader from '@components/PageHeader';

const CategoryForm: React.FC = () => {
  const category = useLoaderData<Category | null>();
  const isEditMode = !!useParams().id;
  const [title, setTitle] = useState(category?.title ?? '');
  const [description, setDescription] = useState(category?.description ?? '');
  const [showModal, setShowModal] = useState(false);
  const fetcher = useFetcher();
  const navigate = useNavigate();
  const toast = useToast();
  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;

  useEffect(() => {
    setTitle(category?.title ?? '');
    setDescription(category?.description ?? '');
  }, [category]);

  useEffect(() => {
    if (busy || !fetcher.data) return;
    if (fetcher.data.success) {
      toast.show(
        'Success',
        isEditMode ? 'Category updated successfully' : 'Category created successfully',
        {
          variant: 'success',
        }
      );
      navigate(isEditMode ? '..' : '/categories');
    }
  }, [busy, fetcher.data, isEditMode, navigate, toast]);

  const unchanged = isEditMode && category?.title === title && category.description === description;
  const invalid = title.trim().length === 0 || description.trim().length === 0;

  return (
    <>
      <PageHeader
        title={isEditMode ? `Edit Category: ${category?.title ?? ''}` : 'Create Category'}
      />
      <GenericErrorList errors={errors?.generics} />
      <Form
        onSubmit={event => {
          event.preventDefault();
          setShowModal(true);
        }}
      >
        <Form.Group className="mb-3" controlId="formTitle">
          <Form.Label>
            Title <span className="text-danger">*</span>
          </Form.Label>
          <Form.Control value={title} onChange={event => setTitle(event.target.value)} required />
          <FieldError error={errors?.title} />
        </Form.Group>
        <Form.Group className="mb-3" controlId="formDescription">
          <Form.Label>
            Description <span className="text-danger">*</span>
          </Form.Label>
          <Form.Control
            value={description}
            onChange={event => setDescription(event.target.value)}
            required
          />
          <FieldError error={errors?.description} />
        </Form.Group>
        <Button variant="primary" disabled={busy || invalid || unchanged} type="submit">
          {isEditMode ? 'Update' : 'Create'}
        </Button>
      </Form>

      <Modal show={showModal} onHide={() => setShowModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>{isEditMode ? 'Confirm Edit' : 'Confirm Create'}</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <p>Are you sure you want to {isEditMode ? 'update' : 'create'} this category?</p>
          <p>
            <strong>Title:</strong> {title}
          </p>
          <p>
            <strong>Description:</strong> {description}
          </p>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowModal(false)}>
            Cancel
          </Button>
          <Button
            variant="primary"
            onClick={() => fetcher.submit({ title, description }, { method: 'post' })}
            disabled={busy}
            data-testid={isEditMode ? 'edit-modal-confirm' : 'create-modal-confirm'}
          >
            {isEditMode ? 'Update' : 'Create'}
          </Button>
        </Modal.Footer>
      </Modal>
    </>
  );
};

export default CategoryForm;
