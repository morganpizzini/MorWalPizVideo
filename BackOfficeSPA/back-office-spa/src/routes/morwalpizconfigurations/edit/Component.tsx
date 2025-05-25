import React, { useState, useEffect } from 'react';
import { Form, Button, Card, Row, Col } from 'react-bootstrap';
import { useLoaderData, useFetcher, useNavigate } from 'react-router';
import { MorWalPizConfiguration } from '@/models/configuration';
import { useToast } from '@components/ToastNotification/ToastContext';
import PageHeader from '@components/PageHeader';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';

const EditConfiguration: React.FC = () => {
  const configuration = useLoaderData<MorWalPizConfiguration>();
  const [model, setModel] = useState<MorWalPizConfiguration>(configuration);
  const navigate = useNavigate();
  const toast = useToast();

  const fetcher = useFetcher();
  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;
  const isSuccess = fetcher.data?.success;

  // Reset form when data is loaded
  useEffect(() => {
    if (configuration) {
      setModel(configuration);
    }
  }, [configuration]);

  // Handle redirect after successful update
  useEffect(() => {
    if (isSuccess) {
      toast.show('Success', 'Configuration updated successfully', { variant: 'success' });
      navigate(`/morwalpizconfigurations/${configuration.id}`);
    }
  }, [isSuccess, toast, navigate, configuration.id]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    fetcher.submit(
      {
        key: model.key,
        value: model.value,
        type: model.type,
        description: model.description,
      },
      { method: 'post' }
    );
  };

  return (
    <>
      <PageHeader
        title={`Edit Configuration: ${configuration.key}`}
        backLink={`/morwalpizconfigurations/${configuration.id}`}
      />

      <Card>
        <Card.Body>
          <GenericErrorList errors={errors?.generics} />
          <Form onSubmit={handleSubmit}>
            <Row className="mb-3">
              <Form.Group as={Col} controlId="formKey">
                <Form.Label>
                  Key <span className="text-danger">*</span>
                </Form.Label>
                <Form.Control
                  type="text"
                  value={model.key}
                  onChange={e => setModel({ ...model, key: e.target.value })}
                  required
                />
                <FieldError error={errors?.key} />
              </Form.Group>
            </Row>

            <Row className="mb-3">
              <Form.Group as={Col} controlId="formValue">
                <Form.Label>
                  Value <span className="text-danger">*</span>
                </Form.Label>
                <Form.Control
                  type="text"
                  value={model.value}
                  onChange={e => setModel({ ...model, value: e.target.value })}
                  required
                />
                <FieldError error={errors?.value} />
              </Form.Group>
            </Row>

            <Row className="mb-3">
              <Form.Group as={Col} controlId="formType">
                <Form.Label>
                  Type <span className="text-danger">*</span>
                </Form.Label>
                <Form.Control
                  type="text"
                  value={model.type}
                  onChange={e => setModel({ ...model, type: e.target.value })}
                  required
                />
                <FieldError error={errors?.type} />
              </Form.Group>
            </Row>

            <Row className="mb-3">
              <Form.Group as={Col} controlId="formDescription">
                <Form.Label>
                  Description <span className="text-danger">*</span>
                </Form.Label>
                <Form.Control
                  as="textarea"
                  rows={3}
                  value={model.description}
                  onChange={e => setModel({ ...model, description: e.target.value })}
                  required
                />
                <FieldError error={errors?.description} />
              </Form.Group>
            </Row>

            <div className="d-flex justify-content-end">
              <Button
                variant="secondary"
                className="me-2"
                onClick={() => navigate(`/morwalpizconfigurations/${configuration.id}`)}
              >
                Cancel
              </Button>
              <Button type="submit" variant="primary" disabled={busy}>
                {busy ? 'Saving...' : 'Save Changes'}
              </Button>
            </div>
          </Form>
        </Card.Body>
      </Card>
    </>
  );
};

export default EditConfiguration;
