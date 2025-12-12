import React, { useEffect } from 'react';
import { Form, Button } from 'react-bootstrap';
import { useLoaderData, useNavigate, useFetcher } from 'react-router';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';
import type { ProductCategory } from '@models';

const EditProductCategory: React.FC = () => {
  const { productCategory } = useLoaderData() as { productCategory: ProductCategory };
  const navigate = useNavigate();
  const toast = useToast();
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

    toast.show('Success', 'Product category updated successfully', { variant: 'success' });
    navigate('/productcategories');
  }, [result, busy, navigate]);

  return (
    <>
      <PageHeader title="Edit Product Category" />
      <GenericErrorList errors={errors?.generics} />

      <fetcher.Form method="post" className="mt-4">
        <Form.Group className="mb-3">
          <Form.Label>Title</Form.Label>
          <Form.Control
            type="text"
            name="title"
            required
            defaultValue={productCategory.title}
            placeholder="Enter category title"
          />
        </Form.Group>

        <Form.Group className="mb-3">
          <Form.Label>Description</Form.Label>
          <Form.Control
            as="textarea"
            name="description"
            required
            rows={3}
            defaultValue={productCategory.description}
            placeholder="Enter category description"
          />
        </Form.Group>

        <Button variant="primary" type="submit" disabled={busy}>
          {busy ? 'Updating...' : 'Update Product Category'}
        </Button>
        <Button
          variant="secondary"
          className="ms-2"
          onClick={() => navigate('/productcategories')}
        >
          Cancel
        </Button>
      </fetcher.Form>
    </>
  );
};

export default EditProductCategory;
