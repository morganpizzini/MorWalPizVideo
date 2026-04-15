import React, { useState, useEffect } from 'react';
import { Form, Button, Card, Row, Col, Alert, Modal } from 'react-bootstrap';
import { useFetcher, useNavigate, useLoaderData, useParams } from 'react-router';
import { ApiKeyDto, CreateApiKeyResponse } from '../../../models/apiKey';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';

const ApiKeyForm: React.FC = () => {
  const existingApiKey = useLoaderData() as ApiKeyDto | null;
  const params = useParams();
  const isEditMode = !!params.id;
  
  const [name, setName] = useState(existingApiKey?.name || '');
  const [description, setDescription] = useState(existingApiKey?.description || '');
  const [rateLimitPerMinute, setRateLimitPerMinute] = useState(existingApiKey?.rateLimitPerMinute?.toString() || '60');
  const [expiresAt, setExpiresAt] = useState(existingApiKey?.expiresAt ? existingApiKey.expiresAt.split('T')[0] : '');
  const [allowedIpAddresses, setAllowedIpAddresses] = useState(
    existingApiKey?.allowedIpAddresses?.join('\n') || ''
  );
  
  const [showKeyModal, setShowKeyModal] = useState(false);
  const [newApiKey, setNewApiKey] = useState<string>('');
  
  const fetcher = useFetcher();
  const navigate = useNavigate();
  const toast = useToast();

  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;

  useEffect(() => {
    if (fetcher.data?.success && fetcher.data?.data) {
      const response = fetcher.data.data as CreateApiKeyResponse;
      setNewApiKey(response.key);
      setShowKeyModal(true);
    }
  }, [fetcher.data]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    const formData = new FormData();
    formData.append('name', name);
    formData.append('description', description);
    formData.append('rateLimitPerMinute', rateLimitPerMinute);
    if (expiresAt) {
      formData.append('expiresAt', expiresAt);
    }
    formData.append('allowedIpAddresses', allowedIpAddresses);

    fetcher.submit(formData, { method: 'post' });
  };

  const handleCloseModal = () => {
    setShowKeyModal(false);
    navigate('/keys');
  };

  const copyToClipboard = () => {
    navigator.clipboard.writeText(newApiKey);
    toast.show('Success', 'API key copied to clipboard', { variant: 'success' });
  };

  return (
    <>
      <PageHeader title={isEditMode ? 'Edit API Key' : 'Create API Key'} />
      
      <Card>
        <Card.Body>
          {errors?.generics && errors.generics.length > 0 && (
            <GenericErrorList errors={errors.generics} />
          )}

          <Form onSubmit={handleSubmit}>
            <Row>
              <Col md={6}>
                <Form.Group className="mb-3">
                  <Form.Label>Name *</Form.Label>
                  <Form.Control
                    type="text"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    isInvalid={!!errors?.fields?.name}
                    disabled={busy}
                    placeholder="Enter API key name"
                  />
                  {errors?.fields?.name && (
                    <Form.Control.Feedback type="invalid">
                      {errors.fields.name}
                    </Form.Control.Feedback>
                  )}
                </Form.Group>
              </Col>

              <Col md={6}>
                <Form.Group className="mb-3">
                  <Form.Label>Rate Limit (per minute) *</Form.Label>
                  <Form.Control
                    type="number"
                    min="1"
                    value={rateLimitPerMinute}
                    onChange={(e) => setRateLimitPerMinute(e.target.value)}
                    isInvalid={!!errors?.fields?.rateLimitPerMinute}
                    disabled={busy}
                    placeholder="60"
                  />
                  {errors?.fields?.rateLimitPerMinute && (
                    <Form.Control.Feedback type="invalid">
                      {errors.fields.rateLimitPerMinute}
                    </Form.Control.Feedback>
                  )}
                  <Form.Text className="text-muted">
                    Maximum number of requests allowed per minute
                  </Form.Text>
                </Form.Group>
              </Col>
            </Row>

            <Form.Group className="mb-3">
              <Form.Label>Description</Form.Label>
              <Form.Control
                as="textarea"
                rows={3}
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                disabled={busy}
                placeholder="Enter description (optional)"
              />
            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Label>Expiration Date</Form.Label>
              <Form.Control
                type="date"
                value={expiresAt}
                onChange={(e) => setExpiresAt(e.target.value)}
                disabled={busy}
              />
              <Form.Text className="text-muted">
                Leave empty for no expiration
              </Form.Text>
            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Label>Allowed IP Addresses</Form.Label>
              <Form.Control
                as="textarea"
                rows={4}
                value={allowedIpAddresses}
                onChange={(e) => setAllowedIpAddresses(e.target.value)}
                disabled={busy}
                placeholder="Enter one IP address per line (optional)&#10;Example:&#10;192.168.1.1&#10;10.0.0.1"
              />
              <Form.Text className="text-muted">
                Leave empty to allow all IP addresses. Enter one IP address per line.
              </Form.Text>
            </Form.Group>

            <div className="d-flex justify-content-between">
              <Button
                variant="secondary"
                onClick={() => navigate('/keys')}
                disabled={busy}
              >
                Cancel
              </Button>
              <Button
                variant="primary"
                type="submit"
                disabled={busy}
              >
                {busy ? 'Saving...' : isEditMode ? 'Update API Key' : 'Create API Key'}
              </Button>
            </div>
          </Form>
        </Card.Body>
      </Card>

      {/* Modal to display the newly created API key */}
      <Modal show={showKeyModal} onHide={handleCloseModal} backdrop="static" keyboard={false}>
        <Modal.Header>
          <Modal.Title>API Key Created Successfully</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <Alert variant="warning">
            <strong>Important:</strong> This is the only time you will see this key. 
            Please copy it now and store it securely.
          </Alert>
          
          <Form.Group className="mb-3">
            <Form.Label><strong>Your API Key:</strong></Form.Label>
            <div className="d-flex gap-2">
              <Form.Control
                type="text"
                value={newApiKey}
                readOnly
                className="font-monospace"
              />
              <Button variant="outline-primary" onClick={copyToClipboard}>
                Copy
              </Button>
            </div>
          </Form.Group>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="primary" onClick={handleCloseModal}>
            I've Saved the Key
          </Button>
        </Modal.Footer>
      </Modal>
    </>
  );
};

export default ApiKeyForm;