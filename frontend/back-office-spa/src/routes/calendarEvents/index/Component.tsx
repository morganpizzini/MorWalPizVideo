import { useResolvedLoaderData } from '@/router/asyncData';
import React, { useState, useEffect, useMemo, useRef } from 'react';
import { Button, Modal, Badge } from 'react-bootstrap';
import { Link, useFetcher, useLocation, useRevalidator } from 'react-router';
import type { AdminCalendarEvent } from '@morwalpizvideo/models';
import type { CalendarActionResult } from '../form';
import { RefreshCw } from 'lucide-react';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';
import GenericTable from '@components/Table';
import { ColumnDef } from '@tanstack/react-table';

const CalendarEvents: React.FC = () => {
  const [showModal, setShowModal] = useState(false);
  const [selectedEvent, setSelectedEvent] = useState<AdminCalendarEvent | null>(null);
  const revalidator = useRevalidator();
  const toast = useToast();
  const location = useLocation();

  const events = useResolvedLoaderData<AdminCalendarEvent[]>();

  const fetcher = useFetcher<CalendarActionResult>();
  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;
  const result = fetcher.state === 'idle' && fetcher.data?.success ? fetcher.data : null;
  const lastResult = useRef<CalendarActionResult | null>(null);

  // Define columns
  const columns = useMemo<ColumnDef<AdminCalendarEvent>[]>(
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
        accessorKey: 'startDate',
        header: 'Start Date',
        cell: info => {
          const dateValue = info.getValue() as string;
          return new Date(dateValue).toLocaleDateString();
        },
      },
      {
        accessorKey: 'endDate',
        header: 'End Date',
        cell: info => {
          const dateValue = info.getValue() as string;
          return new Date(dateValue).toLocaleDateString();
        },
      },
      {
        accessorKey: 'categories',
        header: 'Categories',
        cell: info => {
          const categories = (info.getValue() as AdminCalendarEvent['categories']) || [];
          return (
            <div>
              {categories.map(cat => (
                <Badge key={cat.id} bg="secondary" className="me-1">
                  {cat.title}
                </Badge>
              ))}
            </div>
          );
        },
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
              <Link
                className="btn btn-link px-1"
                to={`/calendarEvents/${encodeURIComponent(event.title)}`}
              >
                Detail
              </Link>
              <Link
                className="btn btn-link px-1"
                to={`/calendarEvents/${encodeURIComponent(event.title)}/edit`}
              >
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
    if (!result || lastResult.current === result) return;
    lastResult.current = result;
    setShowModal(false);

    if (result.success) {
      toast.show('Success', 'Calendar event deleted successfully', { variant: 'success' });
    }
  }, [result, toast]);

  const handleDelete = (event: AdminCalendarEvent) => {
    setSelectedEvent(event);
    setShowModal(true);
  };

  const confirmDelete = () => {
    if (!selectedEvent) return;
    fetcher.submit(
      {
        id: selectedEvent.id,
        revision: String(selectedEvent.revision),
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
      {fetcher.data?.conflict && (
        <Button
          variant="outline-secondary"
          className="mb-3"
          disabled={revalidator.state !== 'idle'}
          onClick={() => {
            setShowModal(false);
            setSelectedEvent(null);
            void revalidator.revalidate();
          }}
        >
          <RefreshCw size={16} aria-hidden="true" className="me-2" />
          Reload Events
        </Button>
      )}

      <GenericTable
        data={events}
        columns={columns}
        pageSize={10}
        searchPlaceholder="Search calendar events..."
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
