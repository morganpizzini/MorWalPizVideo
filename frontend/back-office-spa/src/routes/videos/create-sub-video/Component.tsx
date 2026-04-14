import React, { useEffect, useState } from 'react';
import { useFetcher, useLoaderData } from 'react-router';
import { Form, Button } from 'react-bootstrap';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';
import { useToast } from '@components/ToastNotification/ToastContext';
import PageHeader from '@components/PageHeader';
import { LoaderData } from './loader';

const CreateSubVideo: React.FC = () => {
  const [matchId, setMatchId] = useState('');
  const [videoId, setVideoId] = useState('');
  const [categories, setCategories] = useState<string[]>([]);
  const toast = useToast();

  // Load categories and videos from the API using the loader
  const { categories: availableCategories, videos } = useLoaderData() as LoaderData;

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
      toast.show('Success', 'Sub-video created successfully', { variant: 'success' });
      // Reset form
      setMatchId('');
      setVideoId('');
      setCategories([]);
    }
  }, [result]);

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
    formData.append('matchId', matchId);
    formData.append('videoId', videoId);
    formData.append('categories', JSON.stringify(categories));

    fetcher.submit(formData, {
      method: 'post',
      action: location.pathname,
    });
  };

  const isDisabled = () => !matchId || !videoId || categories.length === 0 || busy;

  return (
    <>
      <PageHeader title="Create Sub-Video" />
      <GenericErrorList errors={errors?.generics} />
      <Form onSubmit={handleSubmit}>
        <Form.Group controlId="formMatchId" className="mb-3">
          <Form.Label>
            Root Video (Match) <span className="text-danger">*</span>
          </Form.Label>
          <Form.Select value={matchId} onChange={e => setMatchId(e.target.value)}>
            <option value="">Select a root video</option>
            {videos.map((video: any) => (
              <option key={video.id} value={video.isLink ? video.thumbnailVideoId : video.id}>
                {video.title || video.id}
              </option>
            ))}
          </Form.Select>
          <FieldError error={errors?.matchId} />
        </Form.Group>

        <Form.Group controlId="formVideoId" className="mb-3">
          <Form.Label>
            Sub-Video ID <span className="text-danger">*</span>
          </Form.Label>
          <Form.Control
            type="text"
            value={videoId}
            onChange={e => setVideoId(e.target.value)}
            placeholder="Enter the sub-video ID"
          />
          <FieldError error={errors?.videoId} />
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
            {busy ? 'Creating...' : 'Create Sub-Video'}
          </Button>
        </div>
      </Form>
    </>
  );
};

export default CreateSubVideo;
