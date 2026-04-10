import React, { useState, useEffect } from 'react';
import { Form, Button } from 'react-bootstrap';
import { useFetcher, useLoaderData, useNavigate, Link } from 'react-router';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import MultiSelectWithBadges from '@components/MultiSelectWithBadges';
import type { Product, VideoProductCategory } from '@morwalpizvideo/models';

const EditProduct: React.FC = () => {
  const { product, categories } = useLoaderData() as { product: Product; categories: VideoProductCategory[] };
  
  const [title, setTitle] = useState(product.title);
  const [description, setDescription] = useState(product.description);
  const [url, setUrl] = useState(product.url);
  const [selectedCategories, setSelectedCategories] = useState<VideoProductCategory[]>(
    (product.categories || []).map(cat => categories.find(c => c.id === cat.id)).filter(Boolean) as VideoProductCategory[]
  );
  
  const fetcher = useFetcher();
  const navigate = useNavigate();
  const toast = useToast();

  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;
  const result = fetcher.data != undefined && !fetcher.data.errors ? fetcher.data : null;

  useEffect(() => {
    if (!result || busy) return;
    if (result.success) {
      toast.show('Success', 'Product updated successfully', { variant: 'success' });
      navigate('/products');
    }
  }, [result, busy, navigate]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const categoryIds = selectedCategories.map(cat => cat.id);
    fetcher.submit(
      { title, description, url, categoryIds: JSON.stringify(categoryIds) },
      { method: 'post' }
    );
  };

  return (
    <>
      <div className="d-flex justify-content-between align-items-center mb-3">
        <h1>Edit Product</h1>
        <Button as={Link} to="/products" variant="secondary">
          Back to List
        </Button>
      </div>
      
      <GenericErrorList errors={errors?.generics} />

      <fetcher.Form method="post" onSubmit={handleSubmit}>
        <Form.Group className="mb-3">
          <Form.Label>Title</Form.Label>
          <Form.Control type="text" value={title} onChange={(e) => setTitle(e.target.value)} required />
        </Form.Group>

        <Form.Group className="mb-3">
          <Form.Label>Description</Form.Label>
          <Form.Control as="textarea" rows={3} value={description} onChange={(e) => setDescription(e.target.value)} required />
        </Form.Group>

        <Form.Group className="mb-3">
          <Form.Label>URL</Form.Label>
          <Form.Control type="url" value={url} onChange={(e) => setUrl(e.target.value)} required />
        </Form.Group>

        <MultiSelectWithBadges
          label="Categories"
          items={categories}
          selectedItems={selectedCategories}
          onSelectionChange={setSelectedCategories}
          getItemId={(cat) => cat.id}
          getItemDisplay={(cat) => cat.title}
          placeholder="Select a category"
        />

        <div className="d-flex gap-2">
          <Button type="submit" variant="primary" disabled={busy}>
            {busy ? 'Saving...' : 'Save Changes'}
          </Button>
          <Button type="button" variant="secondary" onClick={() => navigate('/products')} disabled={busy}>
            Cancel
          </Button>
        </div>
      </fetcher.Form>
    </>
  );
};

export default EditProduct;
