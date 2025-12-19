import React, { useState, useEffect } from 'react';
import { Form, Button, Card, Row, Col } from 'react-bootstrap';
import { useFetcher, useNavigate } from 'react-router';
import { useToast } from '@components/ToastNotification/ToastContext';
import PageHeader from '@components/PageHeader';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';
import { CreateConfigurationDTO } from '@/models/configuration';

const CreateConfiguration: React.FC = () => {
  const [model, setModel] = useState<CreateConfigurationDTO>({
    key: '',
    value: '',
    type: '',
    description: '',
  });

  const navigate = useNavigate();
  const toast = useToast();

  const fetcher = useFetcher();
  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;
  const isSuccess = fetcher.data?.success;

  // Handle redirect after successful creation
  useEffect(() => {
    if (isSuccess) {
      toast.show('Success', 'Configuration created successfully', { variant: 'success' });
      navigate('/morwalpizconfigurations');
    }
  }, [isSuccess, toast, navigate]);

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
        title="Create New Configuration"
        backLink="/morwalpizconfigurations"
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
                onClick={() => navigate('/morwalpizconfigurations')}
              >
                Cancel
              </Button>
              <Button type="submit" variant="primary" disabled={busy}>
                {busy ? 'Creating...' : 'Create Configuration'}
              </Button>
            </div>
          </Form>
        </Card.Body>
      </Card>
    </>
  );
};

export default CreateConfiguration;
