import React, { useState } from 'react';
import { Button, Card, Modal } from 'react-bootstrap';
import { Link, useLoaderData, useNavigate, useFetcher } from 'react-router';
import { MorWalPizConfiguration } from '@/models/configuration';
import { useToast } from '@components/ToastNotification/ToastContext';
import PageHeader from '@components/PageHeader';
import GenericErrorList from '@components/GenericErrorList';

const ConfigurationDetail: React.FC = () => {
  const [showModal, setShowModal] = useState(false);
  const configuration = useLoaderData<MorWalPizConfiguration>();
  const navigate = useNavigate();
  const toast = useToast();

  const fetcher = useFetcher();
  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;

  const handleDelete = () => {
    setShowModal(true);
  };

  const confirmDelete = () => {
    fetcher.submit(
      { id: configuration.id },
      { method: 'post', action: '/morwalpizconfigurations' }
    );
  };

  // Handle successful delete
  if (fetcher.data && fetcher.data.success) {
    toast.show('Success', 'Configuration deleted successfully', { variant: 'success' });
    navigate('/morwalpizconfigurations');
  }

  return (
    <>
      <PageHeader
        title={`Configuration: ${configuration.key}`}
        backLink="/morwalpizconfigurations"
      />

      <GenericErrorList errors={errors?.generics} />

      <Card className="mb-4">
        <Card.Header>Configuration Details</Card.Header>
        <Card.Body>
          <dl className="row">
            <dt className="col-sm-3">Key</dt>
            <dd className="col-sm-9">{configuration.key}</dd>

            <dt className="col-sm-3">Value</dt>
            <dd className="col-sm-9">{configuration.value}</dd>

            <dt className="col-sm-3">Type</dt>
            <dd className="col-sm-9">{configuration.type}</dd>

            <dt className="col-sm-3">Description</dt>
            <dd className="col-sm-9">{configuration.description}</dd>
          </dl>
        </Card.Body>
        <Card.Footer className="d-flex justify-content-between">
          <Button as={Link} to={`/morwalpizconfigurations/${configuration.id}/edit`} variant="primary">
            Edit
          </Button>
          <Button variant="danger" onClick={handleDelete}>
            Delete
          </Button>
        </Card.Footer>
      </Card>

      <Modal show={showModal} onHide={() => setShowModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>Confirm Delete</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <p>
            Are you sure you want to delete the configuration <strong>{configuration.key}</strong>?
            This action cannot be undone.
          </p>
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

export default ConfigurationDetail;
