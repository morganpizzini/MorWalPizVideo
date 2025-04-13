import React, { useState } from 'react';
import { Button, Card, Modal } from 'react-bootstrap';
import { Link, useLoaderData, useFetcher, useNavigate } from 'react-router';
import { CalendarEvent } from '@models/CalendarEvent';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';

const CalendarEventDetail: React.FC = () => {
  const calendarEvent = useLoaderData() as CalendarEvent;
  const [showModal, setShowModal] = useState(false);
  const fetcher = useFetcher();
  const navigate = useNavigate();
  const toast = useToast();

  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;
  const result =
    fetcher.data != undefined &&
      (fetcher.data.errors == undefined || fetcher.data.errors.length == 0)
      ? fetcher.data
      : null;

  React.useEffect(() => {
    if (!result) return;
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
        title: calendarEvent.title,
      },
      {
        method: 'post',
        action: `/calendarEvents/${encodeURIComponent(calendarEvent.title)}`
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

      <Card className="mb-4">
        <Card.Body>
          <div className="d-flex justify-content-between align-items-center mb-3">
            <h2>{calendarEvent.title}</h2>
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
            <dt className="col-sm-3">Date</dt>
            <dd className="col-sm-9">{formatDate(calendarEvent.date)}</dd>

            <dt className="col-sm-3">Description</dt>
            <dd className="col-sm-9">
              {calendarEvent.description || <em>No description provided</em>}
            </dd>

            <dt className="col-sm-3">Category</dt>
            <dd className="col-sm-9">
              {calendarEvent.category || <em>No category provided</em>}
            </dd>

            <dt className="col-sm-3">Match ID</dt>
            <dd className="col-sm-9">
              {calendarEvent.matchId || <em>No match ID provided</em>}
            </dd>
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
