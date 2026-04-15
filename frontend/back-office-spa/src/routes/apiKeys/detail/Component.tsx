import React, { useState } from 'react';
import { useLoaderData, Link, useFetcher, useNavigate } from 'react-router';
import { Card, Badge, Button, Modal, Alert } from 'react-bootstrap';
import { ApiKeyDto, RegenerateApiKeyResponse } from '../../../models';
import PageHeader from '@components/PageHeader';
import { post, ComposeUrl, endpoints } from '@morwalpizvideo/services';
import { useToast } from '@components/ToastNotification/ToastContext';

const ApiKeyDetail: React.FC = () => {
  const apiKey = useLoaderData<ApiKeyDto>();
  const navigate = useNavigate();
  const toast = useToast();
  const [showRegenerateModal, setShowRegenerateModal] = useState(false);
  const [regeneratedKey, setRegeneratedKey] = useState<string | null>(null);
  const [isRegenerating, setIsRegenerating] = useState(false);

  const handleRegenerate = async () => {
    setIsRegenerating(true);
    try {
      const response = await post(
        ComposeUrl(endpoints.APIKEYS_REGENERATE, { id: encodeURIComponent(apiKey.id) }),
        {}
      ) as RegenerateApiKeyResponse;

      if (response && response.key) {
        setRegeneratedKey(response.key);
        toast.show('Success', 'API key regenerated successfully', { variant: 'success' });
      }
    } catch (error) {
      toast.show('Error', 'Failed to regenerate API key', { variant: 'danger' });
    } finally {
      setIsRegenerating(false);
    }
  };

  const handleCopyKey = () => {
    if (regeneratedKey) {
      navigator.clipboard.writeText(regeneratedKey);
      toast.show('Success', 'API key copied to clipboard', { variant: 'success' });
    }
  };

  const handleCloseRegenerateModal = () => {
    setShowRegenerateModal(false);
    if (regeneratedKey) {
      // Refresh the page to get updated lastUsed date
      navigate(0);
    }
  };

  return (
    <>
      <PageHeader title="API Key Details" />

      <div className="mb-3">
        <Link to="/keys" className="btn btn-outline-secondary">
          ← Back to List
        </Link>
      </div>

      <Card className="mb-4">
        <Card.Header>
          <div className="d-flex justify-content-between align-items-center">
            <h5 className="mb-0">Information</h5>
            <Badge bg={apiKey.isActive ? 'success' : 'secondary'}>
              {apiKey.isActive ? 'Active' : 'Inactive'}
            </Badge>
          </div>
        </Card.Header>
        <Card.Body>
          <div className="row">
            <div className="col-md-6 mb-3">
              <label className="text-muted small">Name</label>
              <div className="fw-bold">{apiKey.name}</div>
            </div>

            {apiKey.description && (
              <div className="col-12 mb-3">
                <label className="text-muted small">Description</label>
                <div>{apiKey.description}</div>
              </div>
            )}

            <div className="col-md-6 mb-3">
              <label className="text-muted small">Created At</label>
              <div>{new Date(apiKey.createdAt).toLocaleString()}</div>
            </div>

            <div className="col-md-6 mb-3">
              <label className="text-muted small">Last Used</label>
              <div>
                {apiKey.lastUsedAt
                  ? new Date(apiKey.lastUsedAt).toLocaleString()
                  : 'Never'}
              </div>
            </div>

            <div className="col-md-6 mb-3">
              <label className="text-muted small">Expires At</label>
              <div>
                {apiKey.expiresAt
                  ? new Date(apiKey.expiresAt).toLocaleString()
                  : 'Never'}
              </div>
            </div>
          </div>

          <div className="mt-4">
            <Button
              variant="warning"
              onClick={() => setShowRegenerateModal(true)}
              className="me-2"
            >
              Regenerate Key
            </Button>
            <Link to={`/keys/${apiKey.id}/edit`} className="btn btn-primary">
              Edit
            </Link>
          </div>
        </Card.Body>
      </Card>

      <Modal
        show={showRegenerateModal}
        onHide={handleCloseRegenerateModal}
        centered
        size="lg"
      >
        <Modal.Header closeButton>
          <Modal.Title>Regenerate API Key</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          {!regeneratedKey ? (
            <>
              <Alert variant="warning">
                <strong>Warning:</strong> Regenerating this API key will invalidate the
                current key. Any applications using the old key will lose access immediately.
              </Alert>
              <p>Are you sure you want to regenerate the API key for "{apiKey.name}"?</p>
            </>
          ) : (
            <>
              <Alert variant="success">
                <strong>Success!</strong> Your new API key has been generated.
              </Alert>
              <Alert variant="danger">
                <strong>Important:</strong> This is the only time you will see this key.
                Please copy it now and store it securely.
              </Alert>
              <div className="mb-3">
                <label className="form-label fw-bold">New API Key</label>
                <div className="input-group">
                  <input
                    type="text"
                    className="form-control font-monospace"
                    value={regeneratedKey}
                    readOnly
                  />
                  <button
                    className="btn btn-outline-secondary"
                    type="button"
                    onClick={handleCopyKey}
                  >
                    Copy
                  </button>
                </div>
              </div>
            </>
          )}
        </Modal.Body>
        <Modal.Footer>
          {!regeneratedKey ? (
            <>
              <Button variant="secondary" onClick={handleCloseRegenerateModal}>
                Cancel
              </Button>
              <Button
                variant="warning"
                onClick={handleRegenerate}
                disabled={isRegenerating}
              >
                {isRegenerating ? 'Regenerating...' : 'Regenerate'}
              </Button>
            </>
          ) : (
            <Button variant="primary" onClick={handleCloseRegenerateModal}>
              Close
            </Button>
          )}
        </Modal.Footer>
      </Modal>
    </>
  );
};

export default ApiKeyDetail;