import React from 'react';
import { Form, Button, Row, Col } from 'react-bootstrap';
import { useFetcher, useNavigate } from 'react-router';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';

const CreateCalendarEvent: React.FC = () => {
  const fetcher = useFetcher();
  const navigate = useNavigate();
  const toast = useToast();

  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;
  const success = fetcher.data?.success;

  React.useEffect(() => {
    if (success) {
      toast.show('Success', 'Calendar event created successfully', { variant: 'success' });
      navigate('/calendarEvents');
    }
  }, [success, navigate, toast]);

  return (
    <>
      <PageHeader title="Create Calendar Event" backLink="/calendarEvents" />

      <GenericErrorList errors={errors?.generics} />

      <fetcher.Form method="post" className="mb-3">
        <Row className="mb-3">
          <Col md={6}>
            <Form.Group controlId="title">
              <Form.Label>Title*</Form.Label>
              <Form.Control
                type="text"
                name="title"
                placeholder="Enter title"
                isInvalid={!!errors?.fields?.title}
                required
              />
              {errors?.fields?.title && (
                <Form.Control.Feedback type="invalid">
                  {errors.fields.title}
                </Form.Control.Feedback>
              )}
            </Form.Group>
          </Col>
          <Col md={6}>
            <Form.Group controlId="date">
              <Form.Label>Date*</Form.Label>
              <Form.Control
                type="date"
                name="date"
                isInvalid={!!errors?.fields?.date}
                required
              />
              {errors?.fields?.date && (
                <Form.Control.Feedback type="invalid">
                  {errors.fields.date}
                </Form.Control.Feedback>
              )}
            </Form.Group>
          </Col>
        </Row>

        <Form.Group className="mb-3" controlId="description">
          <Form.Label>Description</Form.Label>
          <Form.Control
            as="textarea"
            name="description"
            placeholder="Enter description"
            isInvalid={!!errors?.fields?.description}
            rows={3}
          />
          {errors?.fields?.description && (
            <Form.Control.Feedback type="invalid">
              {errors.fields.description}
            </Form.Control.Feedback>
          )}
        </Form.Group>

        <Row className="mb-3">
          <Col md={6}>
            <Form.Group controlId="category">
              <Form.Label>Category</Form.Label>
              <Form.Control
                type="text"
                name="category"
                placeholder="Enter category"
                isInvalid={!!errors?.fields?.category}
              />
              {errors?.fields?.category && (
                <Form.Control.Feedback type="invalid">
                  {errors.fields.category}
                </Form.Control.Feedback>
              )}
            </Form.Group>
          </Col>
          <Col md={6}>
            <Form.Group controlId="matchId">
              <Form.Label>Match ID</Form.Label>
              <Form.Control
                type="text"
                name="matchId"
                placeholder="Enter match ID"
                isInvalid={!!errors?.fields?.matchId}
              />
              {errors?.fields?.matchId && (
                <Form.Control.Feedback type="invalid">
                  {errors.fields.matchId}
                </Form.Control.Feedback>
              )}
            </Form.Group>
          </Col>
        </Row>

        <div className="d-flex justify-content-end gap-2">
          <Button variant="secondary" onClick={() => navigate('/calendarEvents')} disabled={busy}>
            Cancel
          </Button>
          <Button type="submit" disabled={busy}>
            {busy ? 'Creating...' : 'Create'}
          </Button>
        </div>
      </fetcher.Form>
    </>
  );
};

export default CreateCalendarEvent;
