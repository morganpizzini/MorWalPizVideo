import React, { useEffect, useState } from 'react';
import { useFetcher } from 'react-router';
import { Form, Button, InputGroup } from 'react-bootstrap';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';
import { useToast } from '@components/ToastNotification/ToastContext';
import PageHeader from '@components/PageHeader';

const TranslateVideo: React.FC = () => {
  const [videoIds, setVideoIds] = useState<string[]>(['']);
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
    if (!result) return;

    if (result.success) {
      toast.show('Success', 'Video translation process started successfully', {
        variant: 'success',
      });
      // Reset form
      setVideoIds(['']);
    }
  }, [result, toast]);

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();

    const formData = new FormData();
    videoIds.forEach(id => {
      if (id.trim()) {
        formData.append('videoIds', id);
      }
    });

    fetcher.submit(formData, {
      method: 'post',
      action: location.pathname,
    });
  };

  const handleAddVideoId = () => {
    setVideoIds([...videoIds, '']);
  };

  const handleRemoveVideoId = (index: number) => {
    if (videoIds.length > 1) {
      const updatedVideoIds = [...videoIds];
      updatedVideoIds.splice(index, 1);
      setVideoIds(updatedVideoIds);
    }
  };

  const handleVideoIdChange = (index: number, value: string) => {
    const updatedVideoIds = [...videoIds];
    updatedVideoIds[index] = value;
    setVideoIds(updatedVideoIds);
  };

  const isDisabled = () => videoIds.every(id => !id.trim()) || busy;

  return (
    <>
      <PageHeader title="Translate Videos" />
      <p className="text-muted mb-4">
        Enter the YouTube video IDs you want to translate. This will process translations for titles
        and descriptions.
      </p>
      <GenericErrorList errors={errors?.generics} />
      <Form onSubmit={handleSubmit}>
        {videoIds.map((videoId, index) => (
          <Form.Group key={index} controlId={`formVideoId-${index}`} className="mb-3">
            <Form.Label>
              {index === 0 ? (
                <>
                  Video ID <span className="text-danger">*</span>
                </>
              ) : (
                <>Additional Video ID</>
              )}
            </Form.Label>
            <InputGroup>
              <Form.Control
                type="text"
                value={videoId}
                onChange={e => handleVideoIdChange(index, e.target.value)}
                placeholder="Enter the YouTube video ID"
              />
              {index > 0 && (
                <Button variant="outline-danger" onClick={() => handleRemoveVideoId(index)}>
                  Remove
                </Button>
              )}
            </InputGroup>
          </Form.Group>
        ))}

        <Button
          variant="outline-secondary"
          className="mb-4"
          onClick={handleAddVideoId}
          type="button"
        >
          Add Another Video ID
        </Button>

        <FieldError error={errors?.videoIds} />

        <div className="d-flex justify-content-end mt-4">
          <Button variant="success" type="submit" disabled={isDisabled()}>
            {busy ? 'Processing...' : 'Translate Videos'}
          </Button>
        </div>
      </Form>
    </>
  );
};

export default TranslateVideo;
