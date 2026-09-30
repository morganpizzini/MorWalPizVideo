import React, { useState, useEffect, useRef } from 'react';
import { Form, Button, Row, Col } from 'react-bootstrap';
import { useFetcher, useNavigate } from 'react-router';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';
import MultiSelectWithBadges from '@components/MultiSelectWithBadges';
import type { CategoryRef } from '@morwalpizvideo/models';
import { fetchCalendarCategories } from '@morwalpizvideo/services';
import type { CalendarActionResult } from '../form';

const CreateCalendarEvent: React.FC = () => {
  const [categories, setCategories] = useState<CategoryRef[]>([]);
  const [selectedCategories, setSelectedCategories] = useState<CategoryRef[]>([]);
  const [loadingCategories, setLoadingCategories] = useState(true);
  const [categoryError, setCategoryError] = useState<string>();

  const fetcher = useFetcher<CalendarActionResult>();
  const navigate = useNavigate();
  const toast = useToast();

  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;
  const success = fetcher.data?.success;
  const lastResult = useRef<unknown>(undefined);

  useEffect(() => {
    let active = true;
    fetchCalendarCategories()
      .then(items => {
        if (active) setCategories(items);
      })
      .catch(error => {
        if (active)
          setCategoryError(error instanceof Error ? error.message : 'Unable to load categories.');
      })
      .finally(() => {
        if (active) setLoadingCategories(false);
      });
    return () => {
      active = false;
    };
  }, []);

  React.useEffect(() => {
    if (!busy && success && lastResult.current !== fetcher.data) {
      lastResult.current = fetcher.data;
      toast.show('Success', 'Calendar event created successfully', { variant: 'success' });
      navigate('/calendarEvents');
    }
  }, [busy, fetcher.data, success, navigate, toast]);

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (busy) return;

    const formData = new FormData(event.currentTarget);

    formData.set(
      'categories',
      JSON.stringify(
        selectedCategories.map(category => ({ id: category.id, title: category.title }))
      )
    );

    fetcher.submit(formData, { method: 'post' });
  };

  return (
    <>
      <PageHeader title="Create Calendar Event" backLink="/calendarEvents" />

      <GenericErrorList errors={errors?.generics} />
      <GenericErrorList errors={categoryError ? [categoryError] : undefined} />

      <fetcher.Form method="post" className="mb-3" onSubmit={handleSubmit}>
        <Row className="mb-3">
          <Col md={12}>
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
          disabled={loadingCategories}
        />

        <Form.Group className="mb-3" controlId="matchId">
          <Form.Label>Match ID</Form.Label>
          <Form.Control
            type="text"
            name="matchId"
            placeholder="Enter match ID"
            isInvalid={!!errors?.fields?.matchId}
          />
          {errors?.fields?.matchId && (
            <Form.Control.Feedback type="invalid">{errors.fields.matchId}</Form.Control.Feedback>
          )}
        </Form.Group>

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
