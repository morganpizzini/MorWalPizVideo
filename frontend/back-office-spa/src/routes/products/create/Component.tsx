import React, { useState, useEffect } from 'react';
import { Form, Button } from 'react-bootstrap';
import { useFetcher, useNavigate, Link } from 'react-router';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import MultiSelectWithBadges from '@components/MultiSelectWithBadges';
import type { VideoProductCategory } from '@morwalpizvideo/models';
import { fetchProductCategories } from '@morwalpizvideo/services';

const CreateProduct: React.FC = () => {
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [url, setUrl] = useState('');
  const [categories, setCategories] = useState<VideoProductCategory[]>([]);
  const [selectedCategories, setSelectedCategories] = useState<VideoProductCategory[]>([]);
  const [loadingCategories, setLoadingCategories] = useState(true);
  
  const fetcher = useFetcher();
  const navigate = useNavigate();
  const toast = useToast();

  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;
  const result =
    fetcher.data != undefined &&
    (fetcher.data.errors == undefined || fetcher.data.errors.length == 0)
      ? fetcher.data
      : null;

  useEffect(() => {
    fetchProductCategories()
      .then(setCategories)
      .finally(() => setLoadingCategories(false));
  }, []);

  useEffect(() => {
    if (!result || busy) return;

    if (result.success) {
      toast.show('Success', 'Product created successfully', { variant: 'success' });
      navigate('/products');
    }
  }, [result, busy, navigate]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    const categoryIds = selectedCategories.map(cat => cat.id);
    
    fetcher.submit(
      {
        title,
        description,
        url,
        categoryIds: JSON.stringify(categoryIds),
      },
      { method: 'post' }
    );
  };

  return (
    <>
      <div className="d-flex justify-content-between align-items-center mb-3">
        <h1>Create Product</h1>
        <Button as={Link} to="/products" variant="secondary">
          Back to List
        </Button>
      </div>
      
      <GenericErrorList errors={errors?.generics} />

      <fetcher.Form method="post" onSubmit={handleSubmit}>
        <Form.Group className="mb-3">
          <Form.Label>Title</Form.Label>
          <Form.Control
            type="text"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            required
          />
        </Form.Group>

        <Form.Group className="mb-3">
          <Form.Label>Description</Form.Label>
          <Form.Control
            as="textarea"
            rows={3}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            required
          />
        </Form.Group>

        <Form.Group className="mb-3">
          <Form.Label>URL</Form.Label>
          <Form.Control
            type="url"
            value={url}
            onChange={(e) => setUrl(e.target.value)}
            required
          />
        </Form.Group>

        <MultiSelectWithBadges
          label="Categories"
          items={categories}
          selectedItems={selectedCategories}
          onSelectionChange={setSelectedCategories}
          getItemId={(cat) => cat.id}
          getItemDisplay={(cat) => cat.title}
          placeholder="Select a category"
          disabled={loadingCategories}
        />

        <div className="d-flex gap-2">
          <Button type="submit" variant="primary" disabled={busy}>
            {busy ? 'Creating...' : 'Create Product'}
          </Button>
          <Button
            type="button"
            variant="secondary"
            onClick={() => navigate('/products')}
            disabled={busy}
          >
            Cancel
          </Button>
        </div>
      </fetcher.Form>
    </>
  );
};

export default CreateProduct;
