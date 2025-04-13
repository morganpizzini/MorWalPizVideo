import React, { useState, useEffect, useMemo } from 'react';
import { Button, Modal } from 'react-bootstrap';
import { Link, useLoaderData, useFetcher, useLocation } from 'react-router';
import { CalendarEvent } from '@models/CalendarEvent';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';
import GenericTable from '@components/Table';
import { ColumnDef } from '@tanstack/react-table';

const CalendarEvents: React.FC = () => {
  const [showModal, setShowModal] = useState(false);
  const [selectedEvent, setSelectedEvent] = useState<CalendarEvent | null>(null);
  const toast = useToast();
  const location = useLocation();

  const events = useLoaderData<CalendarEvent[]>();

  const fetcher = useFetcher();
  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;
  const result =
    fetcher.data != undefined &&
      (fetcher.data.errors == undefined || fetcher.data.errors.length == 0)
      ? fetcher.data
      : null;

  // Define columns
  const columns = useMemo<ColumnDef<CalendarEvent>[]>(
    () => [
      {
        accessorKey: 'title',
        header: 'Title',
        cell: info => info.getValue(),
      },
      {
        accessorKey: 'description',
        header: 'Description',
        cell: info => info.getValue(),
      },
      {
        accessorKey: 'date',
        header: 'Date',
        cell: info => {
          const dateValue = info.getValue() as string;
          return new Date(dateValue).toLocaleDateString();
        },
      },
      {
        accessorKey: 'category',
        header: 'Category',
        cell: info => info.getValue(),
      },
      {
        accessorKey: 'matchId',
        header: 'Match ID',
        cell: info => info.getValue(),
      },
      {
        id: 'actions',
        header: () => <div className="text-end">Actions</div>,
        cell: props => {
          const event = props.row.original;
          return (
            <div className="text-end">
              <Link className="btn btn-link px-1" to={`/calendarEvents/${encodeURIComponent(event.title)}`}>
                Detail
              </Link>
              <Link className="btn btn-link px-1" to={`/calendarEvents/${encodeURIComponent(event.title)}/edit`}>
                Edit
              </Link>
              <Button variant="link" className="px-1" onClick={() => handleDelete(event)}>
                Delete
              </Button>
            </div>
          );
        },
      },
    ],
    []
  );

  useEffect(() => {
    if (!result) return;
    setShowModal(false);

    if (result.success) {
      toast.show('Success', 'Calendar event deleted successfully', { variant: 'success' });
    }
  }, [result, toast]);

  const handleDelete = (event: CalendarEvent) => {
    setSelectedEvent(event);
    setShowModal(true);
  };

  const confirmDelete = () => {
    if (!selectedEvent) return;
    fetcher.submit(
      {
        title: selectedEvent.title,
      },
      {
        method: 'post',
        action: location.pathname,
      }
    );
  };

  return (
    <>
      <PageHeader title="Calendar Events" createLink="./create" />
      <GenericErrorList errors={errors?.generics} />

      <GenericTable
        data={events}
        columns={columns}
        pageSize={10}
        searchPlaceholder="Search calendar events..."
        searchFields={['title', 'description', 'category']}
      />

      <Modal show={showModal} onHide={() => setShowModal(false)} centered>
        <Modal.Header closeButton>
          <Modal.Title>Confirm Delete</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          Are you sure you want to delete the calendar event "{selectedEvent?.title}"?
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

export default CalendarEvents;
