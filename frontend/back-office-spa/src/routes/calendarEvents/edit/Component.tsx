import React, { useState, useEffect } from 'react';
import { Form, Button, Row, Col } from 'react-bootstrap';
import { useFetcher, useLoaderData, useNavigate } from 'react-router';
import { CalendarEvent, VideoProductCategory } from '@morwalpizvideo/models';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';
import MultiSelectWithBadges from '@components/MultiSelectWithBadges';

const EditCalendarEvent: React.FC = () => {
  const { calendarEvent, categories } = useLoaderData() as { calendarEvent: CalendarEvent; categories: VideoProductCategory[] };
  
  const [selectedCategories, setSelectedCategories] = useState<VideoProductCategory[]>(
    (calendarEvent.categories || []).map(cat => categories.find(c => c.id === cat.id)).filter(Boolean) as VideoProductCategory[]
  );
  
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

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    const formData = new FormData(e.target as HTMLFormElement);
    const categoryIds = selectedCategories.map(cat => cat.id);
    
    formData.append('categoryIds', JSON.stringify(categoryIds));
    
    fetcher.submit(formData, { method: 'post' });
  };

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

      <fetcher.Form method="post" className="mb-3" onSubmit={handleSubmit}>
        {/* Original title (hidden) for identifying the event */}
        <input type="hidden" name="title" value={calendarEvent.title} />

        <Row className="mb-3">
          <Col md={12}>
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
          getItemId={(cat) => cat.id}
          getItemDisplay={(cat) => cat.title}
          placeholder="Select a category"
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
            <Form.Control.Feedback type="invalid">
              {errors.fields.matchId}
            </Form.Control.Feedback>
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
          <Button type="submit" disabled={busy}>
            {busy ? 'Updating...' : 'Update'}
          </Button>
        </div>
      </fetcher.Form>
    </>
  );
};

export default EditCalendarEvent;
