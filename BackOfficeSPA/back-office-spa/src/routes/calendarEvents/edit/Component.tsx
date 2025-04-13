import React, { useEffect } from 'react';
import { Form, Button, Row, Col } from 'react-bootstrap';
import { useFetcher, useLoaderData, useNavigate } from 'react-router';
import { CalendarEvent } from '@models/CalendarEvent';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';

const EditCalendarEvent: React.FC = () => {
  const calendarEvent = useLoaderData() as CalendarEvent;
  const fetcher = useFetcher();
  const navigate = useNavigate();
  const toast = useToast();

  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;
  const success = fetcher.data?.success;

  useEffect(() => {
    if (success) {
      toast.show('Success', 'Calendar event updated successfully', { variant: 'success' });
      navigate(`/calendarEvents/${encodeURIComponent(fetcher.data.updatedTitle || calendarEvent.title)}`);
    }
  }, [success, navigate, toast, calendarEvent.title, fetcher.data?.updatedTitle]);

  // Format date for input
  const formatDateForInput = (dateString: string) => {
    const date = new Date(dateString);
    return date.toISOString().split('T')[0];
  };

  return (
    <>
      <PageHeader
        title="Edit Calendar Event"
        backLink={`/calendarEvents/${encodeURIComponent(calendarEvent.title)}`}
      />

      <GenericErrorList errors={errors?.generics} />

      <fetcher.Form method="post" className="mb-3">
        {/* Original title (hidden) for identifying the event */}
        <input type="hidden" name="title" value={calendarEvent.title} />

        <Row className="mb-3">
          <Col md={6}>
            <Form.Group controlId="newTitle">
              <Form.Label>Title*</Form.Label>
              <Form.Control
                type="text"
                name="newTitle"
                placeholder="Enter title"
                defaultValue={calendarEvent.title}
                isInvalid={!!errors?.fields?.newTitle}
                required
              />
              {errors?.fields?.newTitle && (
                <Form.Control.Feedback type="invalid">
                  {errors.fields.newTitle}
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
                defaultValue={formatDateForInput(calendarEvent.date)}
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
            defaultValue={calendarEvent.description}
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
                defaultValue={calendarEvent.category}
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
                defaultValue={calendarEvent.matchId}
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
          <Button
            variant="secondary"
            onClick={() => navigate(`/calendarEvents/${encodeURIComponent(calendarEvent.title)}`)}
            disabled={busy}
          >
            Cancel
          </Button>
          <Button type="submit" disabled={busy}>
            {busy ? 'Updating...' : 'Update'}
          </Button>
        </div>
      </fetcher.Form>
    </>
  );
};

export default EditCalendarEvent;
