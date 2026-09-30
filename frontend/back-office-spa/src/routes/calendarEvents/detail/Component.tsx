import { useResolvedLoaderData } from '@/router/asyncData';
import React, { useState, useRef } from 'react';
import { Button, Card, Modal, Badge } from 'react-bootstrap';
import { Link, useFetcher, useNavigate } from 'react-router';
import type { AdminCalendarEvent } from '@morwalpizvideo/models';
import type { CalendarActionResult } from '../form';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';

const CalendarEventDetail: React.FC = () => {
  const calendarEvent = useResolvedLoaderData() as AdminCalendarEvent;
  const [showModal, setShowModal] = useState(false);
  const fetcher = useFetcher<CalendarActionResult>();
  const navigate = useNavigate();
  const toast = useToast();

  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;
  const result = fetcher.state === 'idle' && fetcher.data?.success ? fetcher.data : null;
  const lastResult = useRef<CalendarActionResult | null>(null);

  React.useEffect(() => {
    if (!result || lastResult.current === result) return;
    lastResult.current = result;
    setShowModal(false);

    if (result.success) {
      toast.show('Success', 'Calendar event deleted successfully', { variant: 'success' });
      navigate('/calendarEvents');
    }
  }, [result, navigate, toast]);

  const handleDelete = () => {
    setShowModal(true);
  };

  const confirmDelete = () => {
    fetcher.submit(
      {
        id: calendarEvent.id,
        revision: String(calendarEvent.revision),
      },
      {
        method: 'post',
        action: `/calendarEvents/${encodeURIComponent(calendarEvent.title)}`,
      }
    );
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString();
  };

  return (
    <>
      <PageHeader title="Calendar Event Details" backLink="/calendarEvents" />
      <GenericErrorList errors={errors?.generics} />
      {fetcher.data?.conflict && (
        <Button
          variant="outline-secondary"
          className="mb-3"
          onClick={() => navigate('/calendarEvents')}
        >
          Return to Calendar Events
        </Button>
      )}

      <Card className="mb-4">
        <Card.Body>
          <div className="d-flex flex-wrap gap-2 justify-content-between align-items-center mb-3">
            <h2 className="h4 text-break">{calendarEvent.title}</h2>
            <div>
              <Link
                to={`/calendarEvents/${encodeURIComponent(calendarEvent.title)}/edit`}
                className="btn btn-primary me-2"
              >
                Edit
              </Link>
              <Button variant="danger" onClick={handleDelete}>
                Delete
              </Button>
            </div>
          </div>

          <dl className="row">
            <dt className="col-sm-3">Start Date</dt>
            <dd className="col-sm-9">{formatDate(calendarEvent.startDate)}</dd>

            <dt className="col-sm-3">End Date</dt>
            <dd className="col-sm-9">{formatDate(calendarEvent.endDate)}</dd>

            <dt className="col-sm-3">Description</dt>
            <dd className="col-sm-9 text-break">
              {calendarEvent.description || <em>No description provided</em>}
            </dd>

            <dt className="col-sm-3">Categories</dt>
            <dd className="col-sm-9">
              {calendarEvent.categories && calendarEvent.categories.length > 0 ? (
                calendarEvent.categories.map(cat => (
                  <Badge key={cat.id} bg="secondary" className="me-1">
                    {cat.title}
                  </Badge>
                ))
              ) : (
                <em>No categories provided</em>
              )}
            </dd>

            <dt className="col-sm-3">Match ID</dt>
            <dd className="col-sm-9">{calendarEvent.matchId || <em>No match ID provided</em>}</dd>
          </dl>
        </Card.Body>
      </Card>

      <Modal show={showModal} onHide={() => setShowModal(false)} centered>
        <Modal.Header closeButton>
          <Modal.Title>Confirm Delete</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          Are you sure you want to delete the calendar event "{calendarEvent.title}"?
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowModal(false)}>
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

export default CalendarEventDetail;
