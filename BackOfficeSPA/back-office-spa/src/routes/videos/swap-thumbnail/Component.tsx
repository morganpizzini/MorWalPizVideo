import React, { useEffect, useState } from 'react';
import { useFetcher, useLoaderData } from 'react-router';
import { Form, Button } from 'react-bootstrap';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';
import { useToast } from '@components/ToastNotification/ToastContext';
import PageHeader from '@components/PageHeader';
import { LoaderData } from './loader';

const SwapThumbnail: React.FC = () => {

  const [currentVideoId, setCurrentVideoId] = useState('');
  const [newVideoId, setNewVideoId] = useState('');
  const toast = useToast();
  const { videos } = useLoaderData<LoaderData>();
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
      toast.show('Success', 'Thumbnail cambiata con successo!', { variant: 'success' });
      // Reset form
      setCurrentVideoId('');
      setNewVideoId('');
    }
  }, [result, toast]);

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();

    const formData = new FormData();
    formData.append('currentVideoId', currentVideoId);
    formData.append('newVideoId', newVideoId);

    fetcher.submit(formData, {
      method: 'post',
      action: location.pathname,
    });
  };

  const isDisabled = () => !currentVideoId || !newVideoId || busy;

  return (
    <>
      <PageHeader title="Cambia Thumbnail" />
      <GenericErrorList errors={errors?.generics} />
      <Form onSubmit={handleSubmit}>
        <Form.Group controlId="formCurrentVideoId" className="mb-3">
          <Form.Label>
            ID Video esistente <span className="text-danger">*</span>
          </Form.Label>
          <Form.Select value={currentVideoId} onChange={e => setCurrentVideoId(e.target.value)}>
            <option value="">Select a video</option>
            {videos.map(video => (
              <option key={video.id} value={video.id}>
                {video.title}
              </option>
            ))}
          </Form.Select>
          <FieldError error={errors?.currentVideoId} />
        </Form.Group>

        <Form.Group controlId="formNewVideoId" className="mb-3">
          <Form.Label>
            ID Nuovo Video <span className="text-danger">*</span>
          </Form.Label>
          <Form.Control
            type="text"
            value={newVideoId}
            onChange={e => setNewVideoId(e.target.value)}
            placeholder="Inserisci l'ID del nuovo video"
          />
          <FieldError error={errors?.newVideoId} />
        </Form.Group>

        <div className="d-flex justify-content-end mt-3">
          <Button variant="success" type="submit" disabled={isDisabled()}>
            {busy ? 'Cambio in corso...' : 'Cambia Thumbnail'}
          </Button>
        </div>
      </Form>
    </>
  );
};

export default SwapThumbnail;
