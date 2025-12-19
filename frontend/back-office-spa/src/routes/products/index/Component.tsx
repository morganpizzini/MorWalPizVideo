import React, { useState, useEffect, useMemo } from 'react';
import { Button, Modal, Badge } from 'react-bootstrap';
import { Link, useLoaderData, useFetcher } from 'react-router';
import type { Product } from '@morwalpizvideo/models';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';
import GenericTable from '@components/Table';
import { ColumnDef } from '@tanstack/react-table';

const Products: React.FC = () => {
  const [showModal, setShowModal] = useState(false);
  const [selectedProduct, setSelectedProduct] = useState<Product | null>(null);
  const toast = useToast();

    const { products: entities } = useLoaderData<{ products: Product[] }>();
    
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
      toast.show('Success', 'Product deleted successfully', { variant: 'success' });
    }
  }, [result, busy]);

  const handleDelete = (product: Product) => {
    setSelectedProduct(product);
    setShowModal(true);
  };

  const confirmDelete = () => {
    if (!selectedProduct) return;
    fetcher.submit(
      {
        productId: selectedProduct.id,
      },
      {
        method: 'post',
        action: location.pathname,
      }
    );
  };

  // Column definitions
  const columns = useMemo<ColumnDef<Product>[]>(
    () => [
      {
        accessorKey: 'title',
        header: 'Title',
        cell: info => (
          <Link to={`/products/${info.row.original.id}`}>
            {info.getValue() as string}
          </Link>
        ),
      },
      {
        accessorKey: 'description',
        header: 'Description',
        cell: info => info.getValue(),
      },
      {
        accessorKey: 'categories',
        header: 'Categories',
        cell: info => {
          const categories = info.getValue() as any[] || [];
          return (
            <div>
              {categories.map((cat: any) => (
                <Badge key={cat.id} bg="secondary" className="me-1">
                  {cat.title}
                </Badge>
              ))}
            </div>
          );
        },
      },
      {
        accessorKey: 'url',
        header: 'URL',
        cell: info => (
          <a href={info.getValue() as string} target="_blank" rel="noopener noreferrer">
            Link
          </a>
        ),
      },
      {
        id: 'actions',
        header: () => <div className="text-end">Actions</div>,
        cell: props => {
          const product = props.row.original;
          return (
            <div className="text-end">
              <Link className="btn btn-link px-1" to={`/products/${product.id}/edit`}>
                Edit
              </Link>
              <Button variant="link" className="px-1" onClick={() => handleDelete(product)}>
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
      <PageHeader title="Products" createLink={`./create`} />
      <GenericErrorList errors={errors?.generics} />

      <GenericTable
        data={entities}
        columns={columns}
        pageSize={10}
        searchPlaceholder="Search products..."
        emptyMessage="No products found"
      />

      <Modal show={showModal} onHide={() => setShowModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>Confirm Delete</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <p>Are you sure you want to delete the following product?</p>
          <p>
            <strong>Title:</strong> {selectedProduct?.title}
          </p>
          <p>
            <strong>Description:</strong> {selectedProduct?.description}
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

export default Products;
