import React, { useEffect } from 'react';
import { Form, Button } from 'react-bootstrap';
import { useNavigate, useFetcher } from 'react-router';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';

const CreateProductCategory: React.FC = () => {
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

    toast.show('Success', 'Product category created successfully', { variant: 'success' });
    navigate('/productcategories');
  }, [result, busy, navigate]);

  return (
    <>
      <PageHeader title="Create Product Category" />
      <GenericErrorList errors={errors?.generics} />

      <fetcher.Form method="post" className="mt-4">
        <Form.Group className="mb-3">
          <Form.Label>Title</Form.Label>
          <Form.Control
            type="text"
            name="title"
            required
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
            placeholder="Enter category description"
          />
        </Form.Group>

        <Button variant="primary" type="submit" disabled={busy}>
          {busy ? 'Creating...' : 'Create Product Category'}
        </Button>
      </fetcher.Form>
    </>
  );
};

export default CreateProductCategory;
