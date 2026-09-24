import { useEffect, useState } from 'react';
import { Badge, Button, Modal } from 'react-bootstrap';
import { Link, useFetcher, useLoaderData, useLocation } from 'react-router';
import type { Survey } from '@morwalpizvideo/models';
import PageHeader from '@components/PageHeader';
import GenericTable from '@components/Table';
import { useToast } from '@components/ToastNotification/ToastContext';

export default function Surveys() {
  const surveys = useLoaderData<Survey[]>();
  const fetcher = useFetcher();
  const location = useLocation();
  const toast = useToast();
  const [selected, setSelected] = useState<Survey | null>(null);
  const columns = [
    { accessorKey: 'title', header: 'Title' },
    { accessorKey: 'url', header: 'URL' },
    {
      id: 'status',
      header: 'Status',
      cell: ({ row }: any) => (
        <Badge bg={row.original.lifecycle === 'Online' ? 'success' : 'secondary'}>
          {row.original.lifecycle}
        </Badge>
      ),
    },
    { id: 'forms', header: 'Forms', cell: ({ row }: any) => (row.original.formIds ?? []).length },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }: any) => (
        <div className="text-end">
          <Link className="btn btn-link px-1" to={`/surveys/${row.original.id}/edit`}>
            Edit
          </Link>
          <Button variant="link" onClick={() => setSelected(row.original)}>
            Archive
          </Button>
        </div>
      ),
    },
  ];

  useEffect(() => {
    if (fetcher.data?.success) {
      setSelected(null);
      toast.show('Success', 'Survey archived successfully', { variant: 'success' });
    }
  }, [fetcher.data, toast]);

  return (
    <>
      <PageHeader title="Surveys" createLink="./create" />
      <GenericTable
        data={surveys}
        columns={columns}
        pageSize={10}
        searchPlaceholder="Search surveys..."
      />
      <Modal show={selected !== null} onHide={() => setSelected(null)} centered>
        <Modal.Header closeButton>
          <Modal.Title>Archive survey</Modal.Title>
        </Modal.Header>
        <Modal.Body>Archive "{selected?.title}"?</Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setSelected(null)}>
            Cancel
          </Button>
          <fetcher.Form method="post" action={location.pathname}>
            <input type="hidden" name="id" value={selected?.id ?? ''} />
            <Button variant="danger" type="submit">
              Archive
            </Button>
          </fetcher.Form>
        </Modal.Footer>
      </Modal>
    </>
  );
}
