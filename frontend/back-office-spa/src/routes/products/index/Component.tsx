import { useResolvedLoaderData } from '@/router/asyncData';
import React, { useEffect, useMemo, useRef, useState } from 'react';
import { Badge, Button, Dropdown, Modal } from 'react-bootstrap';
import { Link, useFetcher, useRevalidator } from 'react-router';
import { Tags, ToggleLeft } from 'lucide-react';
import type {
  BulkProductOperationOutcome,
  Product,
  VideoProductCategory,
} from '@morwalpizvideo/models';
import { assignProductCategoriesBulk, createProductsBulk } from '@morwalpizvideo/services';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';
import GenericTable from '@components/Table';
import MultiSelectWithBadges from '@components/MultiSelectWithBadges';
import CsvImportModal from '@components/CsvImportModal';
import { ColumnDef } from '@tanstack/react-table';
import { parseCsv, parseProductImport } from './csvImport';

const Products: React.FC = () => {
  const { products: entities, categories } = useResolvedLoaderData<{
    products: Product[];
    categories: VideoProductCategory[];
  }>();
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [showImportModal, setShowImportModal] = useState(false);
  const [showCategoryModal, setShowCategoryModal] = useState(false);
  const [selectedProduct, setSelectedProduct] = useState<Product | null>(null);
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [selectionMode, setSelectionMode] = useState(false);
  const [selectedCategories, setSelectedCategories] = useState<VideoProductCategory[]>([]);
  const [operationMessage, setOperationMessage] = useState<{
    success: number;
    failures: BulkProductOperationOutcome[];
  } | null>(null);
  const [modalError, setModalError] = useState('');
  const [bulkBusy, setBulkBusy] = useState(false);
  const toast = useToast();
  const revalidator = useRevalidator();
  const fetcher = useFetcher();
  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;
  const lastDeleteData = useRef<unknown>(undefined);

  useEffect(() => {
    if (
      fetcher.state !== 'idle' ||
      !fetcher.data?.success ||
      lastDeleteData.current === fetcher.data
    ) {
      return;
    }
    lastDeleteData.current = fetcher.data;
    setShowDeleteModal(false);
    toast.show('Success', 'Product deleted successfully', { variant: 'success' });
  }, [fetcher.state, fetcher.data, toast]);

  const runBulk = async (operation: () => Promise<{ results: BulkProductOperationOutcome[] }>) => {
    setBulkBusy(true);
    try {
      const response = await operation();
      const failures = response.results.filter(result => !result.success);
      setOperationMessage({ success: response.results.length - failures.length, failures });
      toast.show(
        failures.length ? 'Import completed with errors' : 'Success',
        `${response.results.length - failures.length} product operation(s) succeeded.`,
        { variant: failures.length ? 'warning' : 'success' }
      );
      revalidator.revalidate();
    } catch (error) {
      setModalError(error instanceof Error ? error.message : 'The bulk operation failed.');
    } finally {
      setBulkBusy(false);
    }
  };

  const handleImport = async (file: File) => {
    setModalError('');
    try {
      const rows = parseProductImport(await file.text());
      await runBulk(() => createProductsBulk({ items: rows }));
      setShowImportModal(false);
    } catch (error) {
      setModalError(error instanceof Error ? error.message : 'The CSV could not be parsed.');
    }
  };

  const handleCategoryAssignment = async () => {
    if (selectedCategories.length === 0) return;
    setModalError('');
    await runBulk(() =>
      assignProductCategoriesBulk({
        items: Array.from(selectedIds).map(productId => ({
          productId,
          categoryIds: selectedCategories.map(category => category.id),
        })),
      })
    );
    setShowCategoryModal(false);
    setSelectedCategories([]);
  };

  const columns = useMemo<ColumnDef<Product>[]>(
    () => [
      { accessorKey: 'title', header: 'Title', cell: info => info.getValue() as string },
      { accessorKey: 'description', header: 'Description', cell: info => info.getValue() },
      {
        accessorKey: 'categories',
        header: 'Categories',
        cell: info => (
          <div>
            {((info.getValue() as Product['categories']) ?? []).map(category => (
              <Badge key={category.id} bg="secondary" className="me-1">
                {category.title}
              </Badge>
            ))}
          </div>
        ),
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
              <Link className="btn btn-link px-1" to={`/products/${product.id}`}>
                Detail
              </Link>
              <Link className="btn btn-link px-1" to={`/products/${product.id}/edit`}>
                Edit
              </Link>
              <Button
                variant="link"
                className="px-1"
                onClick={() => {
                  setSelectedProduct(product);
                  setShowDeleteModal(true);
                }}
              >
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
      <PageHeader
        title="Products"
        createLink="./create"
        actions={
          <>
            <Button
              variant="outline-primary"
              className="me-2"
              onClick={() => {
                setModalError('');
                setShowImportModal(true);
              }}
            >
              <span aria-hidden="true">&#8593;</span> Import
            </Button>
            <Button
              variant={selectionMode ? 'primary' : 'outline-secondary'}
              className="me-2"
              onClick={() => {
                setSelectionMode(value => !value);
                setSelectedIds(new Set());
              }}
            >
              <ToggleLeft size={16} className="me-1" aria-hidden="true" /> Select
            </Button>
            {selectionMode && (
              <Dropdown>
                <Dropdown.Toggle variant="outline-primary" disabled={selectedIds.size === 0}>
                  <Tags size={16} className="me-1" aria-hidden="true" /> Actions ({selectedIds.size}
                  )
                </Dropdown.Toggle>
                <Dropdown.Menu>
                  <Dropdown.Item onClick={() => setShowCategoryModal(true)}>
                    Add categories
                  </Dropdown.Item>
                </Dropdown.Menu>
              </Dropdown>
            )}
          </>
        }
      />
      <GenericErrorList errors={errors?.generics} />
      {operationMessage && (
        <div
          className={`alert ${operationMessage.failures.length ? 'alert-warning' : 'alert-success'}`}
          role="status"
        >
          {operationMessage.success} operation(s) succeeded. {operationMessage.failures.length}{' '}
          failed.
          {operationMessage.failures.length > 0 && (
            <ul className="mb-0 mt-2">
              {operationMessage.failures.map((failure, index) => (
                <li key={`${failure.rowNumber ?? failure.productId ?? index}-${index}`}>
                  Row {failure.rowNumber ?? failure.productId}: {failure.error}
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
      <GenericTable
        data={entities}
        columns={columns}
        pageSize={10}
        searchPlaceholder="Search products..."
        emptyMessage="No products found"
        enableRowSelection={selectionMode}
        selectedRowIds={selectedIds}
        onSelectedRowIdsChange={setSelectedIds}
        getRowId={product => product.id}
      />
      <CsvImportModal
        show={showImportModal}
        title="Import products"
        subTitle="Choose a CSV file to create products in one request."
        legend="Columns: Title, Description, Url, CategoryIds, CategoryNames. Category lists use semicolons; both category columns are merged and deduplicated. Maximum 100 data rows."
        busy={bulkBusy}
        error={modalError}
        onHide={() => setShowImportModal(false)}
        onImport={handleImport}
      />
      <Modal show={showCategoryModal} onHide={() => setShowCategoryModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>Add categories</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <p>
            Add categories to {selectedIds.size} selected product(s). Existing categories are kept.
          </p>
          <MultiSelectWithBadges
            label="Categories"
            items={categories}
            selectedItems={selectedCategories}
            onSelectionChange={setSelectedCategories}
            getItemId={category => category.id}
            getItemDisplay={category => category.title}
            placeholder="Select a category"
            disabled={bulkBusy}
          />
          {modalError && <div className="alert alert-danger">{modalError}</div>}
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowCategoryModal(false)}>
            Cancel
          </Button>
          <Button
            variant="primary"
            disabled={selectedCategories.length === 0 || bulkBusy}
            onClick={handleCategoryAssignment}
          >
            Add categories
          </Button>
        </Modal.Footer>
      </Modal>
      <Modal show={showDeleteModal} onHide={() => setShowDeleteModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>Confirm Delete</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <p>
            Are you sure you want to delete <strong>{selectedProduct?.title}</strong>?
          </p>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowDeleteModal(false)}>
            Cancel
          </Button>
          <Button
            variant="danger"
            disabled={busy}
            onClick={() =>
              selectedProduct &&
              fetcher.submit(
                { productId: selectedProduct.id },
                { method: 'post', action: location.pathname }
              )
            }
            data-testid="delete-modal-confirm"
          >
            Delete
          </Button>
        </Modal.Footer>
      </Modal>
    </>
  );
};

export { parseCsv, parseProductImport };
export default Products;
