import React, { useState, useEffect, useMemo } from 'react';
import { Button, Modal, Badge } from 'react-bootstrap';
import { Link, useLoaderData, useFetcher } from 'react-router';
import { InsightTopic } from '@morwalpizvideo/models';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';
import GenericTable from '@components/Table';
import { ColumnDef } from '@tanstack/react-table';

const InsightTopics: React.FC = () => {
  const [showModal, setShowModal] = useState(false);
  const [selectedTopic, setSelectedTopic] = useState<InsightTopic | null>(null);
  const toast = useToast();

  const entities = useLoaderData<InsightTopic[]>();

  const fetcher = useFetcher();
  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;
  const result =
    fetcher.data != undefined &&
    (fetcher.data.errors == undefined || fetcher.data.errors.length == 0)
      ? fetcher.data
      : null;

  useEffect(() => {
    if (!result) return;
    setShowModal(false);

    if (result.success) {
      toast.show('Success', 'Topic deleted successfully', { variant: 'success' });
    }
  }, [result]);

  const handleDelete = (topic: InsightTopic) => {
    setSelectedTopic(topic);
    setShowModal(true);
  };

  const confirmDelete = () => {
    if (!selectedTopic) return;
    const actionPath = location.pathname.substring(0, location.pathname.lastIndexOf('/'));
    fetcher.submit(
      {
        id: selectedTopic.id,
      },
      {
        method: 'post',
        action: actionPath,
      }
    );
  };

  // Define columns
  const columns = useMemo<ColumnDef<InsightTopic>[]>(
    () => [
      {
        accessorKey: 'title',
        header: 'Title',
        cell: info => info.getValue(),
      },
      {
        accessorKey: 'description',
        header: 'Description',
        cell: info => {
          const value = info.getValue() as string;
          return value && value.length > 100 ? `${value.substring(0, 100)}...` : value;
        },
      },
      {
        accessorKey: 'seedArguments',
        header: 'Arguments',
        cell: info => {
          const args = info.getValue() as string[];
          return args ? args.length : 0;
        },
      },
      {
        accessorKey: 'preferredSources',
        header: 'Sources',
        cell: info => {
          const sources = info.getValue() as string[];
          return sources ? sources.length : 0;
        },
      },
      {
        id: 'actions',
        header: () => <div className="text-end">Actions</div>,
        cell: props => {
          const topic = props.row.original;
          return (
            <div className="text-end">
              <Link className="btn btn-link px-1" to={`/insights/${topic.id}`}>
                Detail
              </Link>
              <Link className="btn btn-link px-1" to={`/insights/${topic.id}/edit`}>
                Edit
              </Link>
              <Button variant="link" className="px-1" onClick={() => handleDelete(topic)}>
                Delete
              </Button>
            </div>
          );
        },
      },
    ],
    []
  );

  return (
    <>
      <PageHeader title="Insight Topics" createLink={`./create`} />
      <GenericErrorList errors={errors?.generics} />

      <GenericTable
        data={entities}
        columns={columns}
        pageSize={10}
        searchPlaceholder="Search topics..."
        emptyMessage="No topics found"
      />

      <Modal show={showModal} onHide={() => setShowModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>Confirm Delete</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <p>Are you sure you want to delete the following topic?</p>
          <p>
            <strong>Title:</strong> {selectedTopic?.title}
          </p>
          <p>
            <strong>Description:</strong> {selectedTopic?.description}
          </p>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowModal(false)}>
            Cancel
          </Button>
          <Button
            variant="danger"
            disabled={busy}
            onClick={confirmDelete}
            data-testid="delete-modal-confirm"
          >
            Delete
          </Button>
        </Modal.Footer>
      </Modal>
    </>
  );
};

export default InsightTopics;