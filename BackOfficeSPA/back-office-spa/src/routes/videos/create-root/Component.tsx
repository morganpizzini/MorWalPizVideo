import React, { useEffect, useState } from 'react';
import { useFetcher, useLoaderData } from 'react-router';
import { Form, Button } from 'react-bootstrap';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';
import { useToast } from '@components/ToastNotification/ToastContext';
import PageHeader from '@components/PageHeader';
import { LoaderData } from './loader';

const CreateRootVideo: React.FC = () => {
  const [videoId, setVideoId] = useState('');
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [url, setUrl] = useState('');
  const [categories, setCategories] = useState<string[]>([]);
  const toast = useToast();

  // Load categories from the API using the loader
  const { categories: availableCategories } = useLoaderData() as LoaderData;

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

    if (result.success) {
      toast.show('Success', 'Root video created successfully', { variant: 'success' });
      // Reset form
      setVideoId('');
      setTitle('');
      setDescription('');
      setUrl('');
      setCategories([]);
    }
  }, [result, toast]);

  // Handle category checkbox changes
  const handleCategoryChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    if (e.target.checked) {
      setCategories([...categories, value]);
    } else {
      setCategories(categories.filter(cat => cat !== value));
    }
  };

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();

    const formData = new FormData();
    formData.append('videoId', videoId);
    formData.append('title', title);
    formData.append('description', description);
    formData.append('url', url);
    formData.append('categories', JSON.stringify(categories));

    fetcher.submit(formData, {
      method: 'post',
      action: location.pathname,
    });
  };

  const isDisabled = () =>
    !videoId || !title || !description || !url || categories.length === 0 || busy;

  return (
    <>
      <PageHeader title="Create Root Video" />
      <GenericErrorList errors={errors?.generics} />
      <Form onSubmit={handleSubmit}>
        <Form.Group controlId="formVideoId" className="mb-3">
          <Form.Label>
            Video ID <span className="text-danger">*</span>
          </Form.Label>
          <Form.Control
            type="text"
            value={videoId}
            onChange={e => setVideoId(e.target.value)}
            placeholder="Enter the video ID"
          />
          <FieldError error={errors?.videoId} />
        </Form.Group>

        <Form.Group controlId="formTitle" className="mb-3">
          <Form.Label>
            Title <span className="text-danger">*</span>
          </Form.Label>
          <Form.Control
            type="text"
            value={title}
            onChange={e => setTitle(e.target.value)}
            placeholder="Enter the video title"
          />
          <FieldError error={errors?.title} />
        </Form.Group>

        <Form.Group controlId="formDescription" className="mb-3">
          <Form.Label>
            Description <span className="text-danger">*</span>
          </Form.Label>
          <Form.Control
            as="textarea"
            rows={4}
            value={description}
            onChange={e => setDescription(e.target.value)}
            placeholder="Enter the video description"
          />
          <FieldError error={errors?.description} />
        </Form.Group>

        <Form.Group controlId="formUrl" className="mb-3">
          <Form.Label>
            URL <span className="text-danger">*</span>
          </Form.Label>
          <Form.Control
            type="text"
            value={url}
            onChange={e => setUrl(e.target.value)}
            placeholder="Enter the video URL"
          />
          <FieldError error={errors?.url} />
        </Form.Group>

        <Form.Group controlId="formCategory" className="mb-3">
          <Form.Label>
            Categories <span className="text-danger">*</span> (Select multiple)
          </Form.Label>
          <div className="border rounded p-3">
            {availableCategories.map(cat => (
              <Form.Check
                key={cat.categoryId}
                type="checkbox"
                id={`category-${cat.categoryId}`}
                label={cat.title}
                value={cat.categoryId}
                checked={categories.includes(cat.categoryId)}
                onChange={handleCategoryChange}
                className="mb-2"
              />
            ))}
          </div>
          <FieldError error={errors?.categories} />
        </Form.Group>

        <div className="d-flex justify-content-end mt-3">
          <Button variant="success" type="submit" disabled={isDisabled()}>
            {busy ? 'Creating...' : 'Create Root Video'}
          </Button>
        </div>
      </Form>
    </>
  );
};

export default CreateRootVideo;
