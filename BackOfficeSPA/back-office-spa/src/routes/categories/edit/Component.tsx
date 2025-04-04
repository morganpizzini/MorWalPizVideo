import React, { useState, useEffect } from 'react';
import { Form, Button, Modal } from 'react-bootstrap';
import { useLoaderData, useNavigate, useFetcher } from 'react-router';
import { Category } from '@models';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';
import { useToast } from '@components/ToastNotification/ToastContext';
import PageHeader from '@components/PageHeader';

const EditCategory: React.FC = () => {
  const entity = useLoaderData<Category>();
  const [model, setModel] = useState<Category | null>(null);

  const [showModal, setShowModal] = useState(false);
  const navigate = useNavigate();
  const toast = useToast();

  const fetcher = useFetcher();
  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;
  const result =
    fetcher.data != undefined &&
      (fetcher.data.errors == undefined || fetcher.data.errors.length == 0)
      ? fetcher.data
      : null;

  useEffect(() => {
    setModel(entity);
  }, [entity]);

  useEffect(() => {
    if (!result) return;
    setShowModal(false);

    if (result.success) {
      toast.show('Success', 'Category updated successfully', { variant: 'success' });
      navigate('..');
    }
  }, [result, navigate, toast]);

  const isDisabled = () =>
    !model ||
    model.title.length === 0 ||
    model.description.length === 0 ||
    (model.title === entity.title && model.description === entity.description) ||
    busy;

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    setShowModal(true);
  };

  const confirmEdit = () => {
    fetcher.submit(
      {
        ...model
      },
      {
        method: 'post',
        action: location.pathname,
      }
    );
  };

  if (!model) {
    return <div>Loading...</div>;
  }

  return (
    <>
      <PageHeader title={`Edit Category: ${entity.title}`} />
      <GenericErrorList errors={errors?.generics} />
      <Form onSubmit={handleSubmit}>
        <Form.Group controlId="formTitle">
          <Form.Label>
            Title <span className="text-danger">*</span>
          </Form.Label>
          <Form.Control
            type="text"
            value={model.title}
            onChange={e => setModel({ ...model, title: e.target.value })}
          />
          <Form.Control type="text" value={model.title}
            onChange={e => setModel({ ...model, title: e.target.value })}
          />
          <FieldError error={errors?.title} />
        </Form.Group>
        <Form.Group controlId="formDescription">
          <Form.Label>
            Description <span className="text-danger">*</span>
          </Form.Label>
          <Form.Control
            type="text"
            value={model.description}
            onChange={e => setModel({ ...model, description: e.target.value })}
          />
          <FieldError error={errors?.description} />
        </Form.Group>
        <Button variant="primary" disabled={isDisabled()} type="submit" className="mt-2">
          Update
        </Button>
      </Form>

      <Modal show={showModal} onHide={() => setShowModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>Confirm Edit</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <p>Are you sure you want to update this category with the following details?</p>
          <p>
            <strong>Title:</strong> {model.title}
            {model.title != entity.title && (
              <>
                (<s>{entity.title}</s>)
              </>
            )}
          </p>
          <p>
            <strong>Description:</strong> {model.description}
            {model.description != entity.description && (
              <>
                (<s>{entity.description}</s>)
              </>
            )}
          </p>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowModal(false)}>
            Cancel
          </Button>
          <Button variant="primary" onClick={confirmEdit} data-testid="edit-modal-confirm">
            Update
          </Button>
        </Modal.Footer>
      </Modal>
    </>
  );
};

export default EditCategory;
