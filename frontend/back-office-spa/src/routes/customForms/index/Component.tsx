import React, { useState, useEffect, useMemo } from 'react';
import { Button, Modal, Badge } from 'react-bootstrap';
import { Link, useLoaderData, useFetcher, useLocation } from 'react-router';
import { CustomForm, QuestionType } from '@morwalpizvideo/models';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';
import GenericTable from '@components/Table';
import { ColumnDef } from '@tanstack/react-table';

const CustomForms: React.FC = () => {
  const [showModal, setShowModal] = useState(false);
  const [selectedForm, setSelectedForm] = useState<CustomForm | null>(null);
  const toast = useToast();
  const location = useLocation();

  const forms = useLoaderData<CustomForm[]>();

  const fetcher = useFetcher();
  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;
  const result =
    fetcher.data != undefined &&
      (fetcher.data.errors == undefined || fetcher.data.errors.length == 0)
      ? fetcher.data
      : null;

  // Define columns
  const columns = useMemo<ColumnDef<CustomForm>[]>(
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
          const desc = info.getValue() as string;
          return desc ? (desc.length > 100 ? desc.substring(0, 100) + '...' : desc) : '';
        },
      },
      {
        accessorKey: 'questions',
        header: 'Questions',
        cell: info => {
          const questions = info.getValue() as any[];
          return (
            <div>
              <Badge bg="info">{questions.length} questions</Badge>
            </div>
          );
        },
      },
      {
        accessorKey: 'responseCount',
        header: 'Responses',
        cell: info => {
          const count = info.getValue() as number;
          return (
            <Badge bg={count > 0 ? 'success' : 'secondary'}>
              {count} {count === 1 ? 'response' : 'responses'}
            </Badge>
          );
        },
      },
      {
        accessorKey: 'creationDateTime',
        header: 'Created',
        cell: info => {
          const dateValue = info.getValue() as string;
          return new Date(dateValue).toLocaleDateString();
        },
      },
      {
        id: 'actions',
        header: () => <div className="text-end">Actions</div>,
        cell: props => {
          const form = props.row.original;
          return (
            <div className="text-end">
              <Link className="btn btn-link px-1" to={`/customforms/${form.id}`}>
                Detail
              </Link>
              <Link className="btn btn-link px-1" to={`/customforms/${form.id}/edit`}>
                Edit
              </Link>
              <Button variant="link" className="px-1" onClick={() => handleDelete(form)}>
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
      toast.show('Success', 'Custom form deleted successfully', { variant: 'success' });
    }
  }, [result]);

  const handleDelete = (form: CustomForm) => {
    setSelectedForm(form);
    setShowModal(true);
  };

  const confirmDelete = () => {
    if (!selectedForm) return;
    fetcher.submit(
      {
        id: selectedForm.id,
      },
      {
        method: 'post',
        action: location.pathname,
      }
    );
  };

  return (
    <>
      <PageHeader title="Custom Forms" createLink="./create" />
      <GenericErrorList errors={errors?.generics} />

      <GenericTable
        data={forms}
        columns={columns}
        pageSize={10}
        searchPlaceholder="Search custom forms..."
      />

      <Modal show={showModal} onHide={() => setShowModal(false)} centered>
        <Modal.Header closeButton>
          <Modal.Title>Confirm Delete</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          Are you sure you want to delete the custom form "{selectedForm?.title}"?
          {selectedForm && selectedForm.responseCount > 0 && (
            <div className="alert alert-warning mt-3">
              <strong>Warning:</strong> This form has {selectedForm.responseCount} response(s) that will also be deleted.
            </div>
          )}
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

export default CustomForms;
