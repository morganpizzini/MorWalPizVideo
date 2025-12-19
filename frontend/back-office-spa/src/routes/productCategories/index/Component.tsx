import React, { useState, useEffect, useMemo } from 'react';
import { Button, Modal } from 'react-bootstrap';
import { Link, useLoaderData, useFetcher } from 'react-router';
import type { ProductCategory } from '@morwalpizvideo/models';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';
import GenericTable from '@components/Table';
import { ColumnDef } from '@tanstack/react-table';

const ProductCategories: React.FC = () => {
  const [showModal, setShowModal] = useState(false);
  const [selectedCategory, setSelectedCategory] = useState<ProductCategory | null>(null);
  const toast = useToast();

  const entities = useLoaderData<ProductCategory[]>();

  const fetcher = useFetcher();
  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;
  const result =
    fetcher.data != undefined &&
    (fetcher.data.errors == undefined || fetcher.data.errors.length == 0)
      ? fetcher.data
      : null;

  useEffect(() => {
    if (!result || busy) return;
    setShowModal(false);

    if (result.success) {
      toast.show('Success', 'Product category deleted successfully', { variant: 'success' });
    }
  }, [result, busy]);

  const handleDelete = (category: ProductCategory) => {
    setSelectedCategory(category);
    setShowModal(true);
  };

  const confirmDelete = () => {
    if (!selectedCategory) return;
    fetcher.submit(
      {
        id: selectedCategory.id,
      },
      {
        method: 'post',
        action: location.pathname,
      }
    );
  };

  // Column definitions
  const columns = useMemo<ColumnDef<ProductCategory>[]>(
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
        id: 'actions',
        header: () => <div className="text-end">Actions</div>,
        cell: props => {
          const category = props.row.original;
          return (
            <div className="text-end">
              <Link className="btn btn-link px-1" to={`/productcategories/${category.id}/edit`}>
                Edit
              </Link>
              <Button variant="link" className="px-1" onClick={() => handleDelete(category)}>
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
      <PageHeader title="Product Categories" createLink={`./create`} />
      <GenericErrorList errors={errors?.generics} />

      <GenericTable
        data={entities}
        columns={columns}
        pageSize={10}
        searchPlaceholder="Search product categories..."
        emptyMessage="No product categories found"
      />

      <Modal show={showModal} onHide={() => setShowModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>Confirm Delete</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <p>Are you sure you want to delete the following product category?</p>
          <p>
            <strong>Title:</strong> {selectedCategory?.title}
          </p>
          <p>
            <strong>Description:</strong> {selectedCategory?.description}
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

export default ProductCategories;
