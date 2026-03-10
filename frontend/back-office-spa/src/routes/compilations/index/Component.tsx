import React, { useState, useEffect, useMemo } from 'react';
import { Button, Modal } from 'react-bootstrap';
import { Link, useLoaderData, useFetcher } from 'react-router';
import { Compilation } from '@morwalpizvideo/models';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';
import GenericTable from '@components/Table';
import { ColumnDef } from '@tanstack/react-table';

const Compilations: React.FC = () => {
  const [showModal, setShowModal] = useState(false);
  const [selectedCompilation, setSelectedCompilation] = useState<Compilation | null>(null);
  const toast = useToast();

  const entities = useLoaderData<Compilation[]>();

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
      toast.show('Success', 'Compilation deleted successfully', { variant: 'success' });
    }
  }, [result]);

  const handleDelete = (compilation: Compilation) => {
    setSelectedCompilation(compilation);
    setShowModal(true);
  };

  const confirmDelete = () => {
    if (!selectedCompilation) return;
    const actionPath = location.pathname.substring(0, location.pathname.lastIndexOf('/'));
    fetcher.submit(
      {
        id: selectedCompilation.id,
      },
      {
        method: 'post',
        action: actionPath,
      }
    );
  };

  // Define columns
  const columns = useMemo<ColumnDef<Compilation>[]>(
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
        accessorKey: 'url',
        header: 'URL',
        cell: info => info.getValue(),
      },
      {
        accessorKey: 'videos',
        header: 'Videos',
        cell: info => {
          const videos = info.getValue() as any[];
          return videos ? videos.length : 0;
        },
      },
      {
        id: 'actions',
        header: () => <div className="text-end">Actions</div>,
        cell: props => {
          const compilation = props.row.original;
          return (
            <div className="text-end">
              <Link className="btn btn-link px-1" to={`/compilations/${compilation.id}`}>
                Detail
              </Link>
              <Link className="btn btn-link px-1" to={`/compilations/${compilation.id}/edit`}>
                Edit
              </Link>
              <Button variant="link" className="px-1" onClick={() => handleDelete(compilation)}>
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
      <PageHeader title="Compilations" createLink={`./create`} />
      <GenericErrorList errors={errors?.generics} />

      <GenericTable
        data={entities}
        columns={columns}
        pageSize={10}
        searchPlaceholder="Search compilations..."
        emptyMessage="No compilations found"
      />

      <Modal show={showModal} onHide={() => setShowModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>Confirm Delete</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <p>Are you sure you want to delete the following compilation?</p>
          <p>
            <strong>Title:</strong> {selectedCompilation?.title}
          </p>
          <p>
            <strong>Description:</strong> {selectedCompilation?.description}
          </p>
          <p>
            <strong>Videos:</strong> {selectedCompilation?.videos?.length || 0}
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

export default Compilations;
