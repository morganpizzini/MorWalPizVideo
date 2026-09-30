import { useResolvedLoaderData } from '@/router/asyncData';
import React, { useState, useEffect, useRef } from 'react';
import { Alert, Form, Button, Row, Col } from 'react-bootstrap';
import { RefreshCw } from 'lucide-react';
import { useFetcher, useNavigate } from 'react-router';
import type { AdminCalendarEvent, CategoryRef } from '@morwalpizvideo/models';
import { getCalendarEventById } from '@morwalpizvideo/services';
import type { CalendarActionResult } from '../form';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';
import MultiSelectWithBadges from '@components/MultiSelectWithBadges';

const EditCalendarEvent: React.FC = () => {
  const { calendarEvent: loadedEvent, categories } = useResolvedLoaderData() as {
    calendarEvent: AdminCalendarEvent;
    categories: CategoryRef[];
  };

  const [calendarEvent, setCalendarEvent] = useState(loadedEvent);
  const [selectedCategories, setSelectedCategories] = useState<CategoryRef[]>(
    calendarEvent.categories || []
  );
  const [reloading, setReloading] = useState(false);
  const [reloadError, setReloadError] = useState<string>();
  const [recoveredResult, setRecoveredResult] = useState<CalendarActionResult>();

  const fetcher = useFetcher<CalendarActionResult>();
  const navigate = useNavigate();
  const toast = useToast();

  const busy = fetcher.state !== 'idle' || reloading;
  const conflict = fetcher.data?.conflict && fetcher.data !== recoveredResult;
  const errors = fetcher.data !== recoveredResult ? fetcher.data?.errors : undefined;
  const success = fetcher.data?.success;
  const lastResult = useRef<unknown>(undefined);

  useEffect(() => {
    if (!busy && success && lastResult.current !== fetcher.data) {
      lastResult.current = fetcher.data;
      toast.show('Success', 'Calendar event updated successfully', { variant: 'success' });
      navigate(
        `/calendarEvents/${encodeURIComponent(fetcher.data?.updatedTitle || calendarEvent.title)}`
      );
    }
  }, [busy, fetcher.data, success, navigate, toast, calendarEvent.title]);

  const reloadLatest = async () => {
    setReloading(true);
    setReloadError(undefined);
    try {
      const latest = await getCalendarEventById(calendarEvent.id);
      setCalendarEvent(latest);
      setSelectedCategories(latest.categories || []);
      setRecoveredResult(fetcher.data);
    } catch (error) {
      setReloadError(
        error instanceof Error ? error.message : 'Unable to reload the Calendar event.'
      );
    } finally {
      setReloading(false);
    }
  };

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (busy || conflict) return;

    const formData = new FormData(event.currentTarget);
    formData.set(
      'categories',
      JSON.stringify(
        selectedCategories.map(category => ({ id: category.id, title: category.title }))
      )
    );

    fetcher.submit(formData, { method: 'post' });
  };

  // Format date for input
  const formatDateForInput = (dateString: string) => {
    return dateString.slice(0, 10);
  };

  return (
    <>
      <PageHeader
        title="Edit Calendar Event"
        backLink={`/calendarEvents/${encodeURIComponent(calendarEvent.title)}`}
      />

      <GenericErrorList errors={errors?.generics} />
      <GenericErrorList errors={reloadError ? [reloadError] : undefined} />
      {conflict && (
        <Alert variant="warning" role="alert">
          <p>This event changed after you opened it. Your draft has not been saved.</p>
          <Button variant="outline-secondary" onClick={reloadLatest} disabled={busy}>
            <RefreshCw size={16} aria-hidden="true" className="me-2" />
            {reloading ? 'Reloading...' : 'Reload Latest and Discard Draft'}
          </Button>
        </Alert>
      )}

      <fetcher.Form
        key={`${calendarEvent.id}:${calendarEvent.revision}`}
        method="post"
        className="mb-3"
        onSubmit={handleSubmit}
      >
        {/* Original title (hidden) for identifying the event */}
        <input type="hidden" name="title" value={calendarEvent.title} />
        <input type="hidden" name="id" value={calendarEvent.id} />
        <input type="hidden" name="revision" value={calendarEvent.revision} />

        <Row className="mb-3">
          <Col md={12}>
            <Form.Group controlId="newTitle">
              <Form.Label>Title*</Form.Label>
              <Form.Control
                type="text"
                name="newTitle"
                placeholder="Enter title"
                defaultValue={calendarEvent.title}
                isInvalid={!!errors?.fields?.title}
                required
              />
              {errors?.fields?.title && (
                <Form.Control.Feedback type="invalid">{errors.fields.title}</Form.Control.Feedback>
              )}
            </Form.Group>
          </Col>
        </Row>

        <Row className="mb-3">
          <Col md={6}>
            <Form.Group controlId="startDate">
              <Form.Label>Start Date*</Form.Label>
              <Form.Control
                type="date"
                name="startDate"
                defaultValue={formatDateForInput(calendarEvent.startDate)}
                isInvalid={!!errors?.fields?.startDate}
                required
              />
              {errors?.fields?.startDate && (
                <Form.Control.Feedback type="invalid">
                  {errors.fields.startDate}
                </Form.Control.Feedback>
              )}
            </Form.Group>
          </Col>
          <Col md={6}>
            <Form.Group controlId="endDate">
              <Form.Label>End Date*</Form.Label>
              <Form.Control
                type="date"
                name="endDate"
                defaultValue={formatDateForInput(calendarEvent.endDate)}
                isInvalid={!!errors?.fields?.endDate}
                required
              />
              {errors?.fields?.endDate && (
                <Form.Control.Feedback type="invalid">
                  {errors.fields.endDate}
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
            required
          />
          {errors?.fields?.description && (
            <Form.Control.Feedback type="invalid">
              {errors.fields.description}
            </Form.Control.Feedback>
          )}
        </Form.Group>

        <MultiSelectWithBadges
          label="Categories"
          items={categories}
          selectedItems={selectedCategories}
          onSelectionChange={setSelectedCategories}
          getItemId={cat => cat.id}
          getItemDisplay={cat => cat.title}
          placeholder="Select a category"
          disabled={busy}
          error={errors?.fields?.categories}
        />

        <Form.Group className="mb-3" controlId="matchId">
          <Form.Label>Match ID</Form.Label>
          <Form.Control
            type="text"
            name="matchId"
            placeholder="Enter match ID"
            defaultValue={calendarEvent.matchId}
            isInvalid={!!errors?.fields?.matchId}
          />
          {errors?.fields?.matchId && (
            <Form.Control.Feedback type="invalid">{errors.fields.matchId}</Form.Control.Feedback>
          )}
        </Form.Group>

        <div className="d-flex justify-content-end gap-2">
          <Button
            variant="secondary"
            onClick={() => navigate(`/calendarEvents/${encodeURIComponent(calendarEvent.title)}`)}
            disabled={busy}
          >
            Cancel
          </Button>
          <Button type="submit" disabled={busy || !!conflict}>
            {busy ? 'Updating...' : 'Update'}
          </Button>
        </div>
      </fetcher.Form>
    </>
  );
};

export default EditCalendarEvent;
