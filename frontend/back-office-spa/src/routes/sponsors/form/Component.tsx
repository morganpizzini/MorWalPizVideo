import React, { useEffect, useRef, useState } from 'react';
import { Form, Button, Card, Image } from 'react-bootstrap';
import { useLoaderData, useNavigate, useFetcher, useParams } from 'react-router';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';
import type { Sponsor } from '@morwalpizvideo/models';
import type { SponsorActionResult } from './action';

const SponsorForm: React.FC = () => {
  const sponsor = useLoaderData() as Sponsor | null;
  const { sponsorId } = useParams<{ sponsorId?: string }>();
  const isEditMode = Boolean(sponsorId);
  const navigate = useNavigate();
  const toast = useToast();
  const fetcher = useFetcher<SponsorActionResult>();
  const [imageFile, setImageFile] = useState<File | null>(null);
  const [imagePreview, setImagePreview] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const busy = fetcher.state !== 'idle';
  const result = fetcher.data;
  const errors = result?.errors;

  useEffect(() => {
    if (busy || result?.success !== true) return;

    toast.show(
      'Success',
      isEditMode ? 'Sponsor updated successfully' : 'Sponsor created successfully',
      { variant: 'success' }
    );
    navigate('/sponsors');
  }, [busy, isEditMode, navigate, result, toast]);

  const handleImageChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = event.target.files?.[0];
    if (!selectedFile) return;

    setImageFile(selectedFile);
    const reader = new FileReader();
    reader.onloadend = () => setImagePreview(reader.result as string);
    reader.readAsDataURL(selectedFile);
  };

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!isEditMode && !imageFile) {
      toast.show('Error', 'Please select an image', { variant: 'danger' });
      return;
    }

    const formData = new FormData(event.currentTarget);
    if (imageFile) formData.append('image', imageFile);

    fetcher.submit(formData, {
      method: 'post',
      encType: 'multipart/form-data',
    });
  };

  return (
    <>
      <PageHeader title={isEditMode ? 'Edit Sponsor' : 'Create Sponsor'} />
      <GenericErrorList errors={errors?.generics} />

      <Form onSubmit={handleSubmit} className="mt-4">
        <Form.Group className="mb-3">
          <Form.Label>Title</Form.Label>
          <Form.Control
            type="text"
            name="title"
            required
            defaultValue={sponsor?.title}
            placeholder="Enter sponsor name"
          />
        </Form.Group>

        <Form.Group className="mb-3">
          <Form.Label>URL</Form.Label>
          <Form.Control
            type="url"
            name="url"
            required
            defaultValue={sponsor?.url}
            placeholder="https://example.com"
          />
        </Form.Group>

        {sponsor?.imgSrc && (
          <Form.Group className="mb-3">
            <Form.Label>Current Logo</Form.Label>
            <div>
              <img
                src={sponsor.imgSrc}
                alt={sponsor.title}
                style={{ maxWidth: '200px', maxHeight: '100px', objectFit: 'contain' }}
              />
            </div>
          </Form.Group>
        )}

        <Form.Group className="mb-3">
          <Form.Label>
            {isEditMode ? 'Update Sponsor Logo Image (Optional)' : 'Sponsor Logo Image'}
          </Form.Label>
          <Form.Control
            type="file"
            accept="image/*"
            onChange={handleImageChange}
            ref={fileInputRef}
            required={!isEditMode}
          />
          <Form.Text className="text-muted">
            {isEditMode
              ? 'Upload a new logo image (JPG, PNG, etc.). Leave empty to keep the current image.'
              : "Upload the sponsor's logo image (JPG, PNG, etc.)"}
          </Form.Text>

          {imagePreview && (
            <Card className="mt-3">
              <Card.Body className="text-center">
                <Image
                  src={imagePreview}
                  alt={isEditMode ? 'New Preview' : 'Preview'}
                  style={{ maxHeight: '200px', maxWidth: '100%' }}
                  thumbnail
                />
              </Card.Body>
            </Card>
          )}
        </Form.Group>

        <Button variant="primary" type="submit" disabled={busy}>
          {busy
            ? isEditMode
              ? 'Updating...'
              : 'Creating...'
            : isEditMode
              ? 'Update Sponsor'
              : 'Create Sponsor'}
        </Button>
        <Button
          variant="secondary"
          className="ms-2"
          onClick={() => navigate('/sponsors')}
          disabled={busy}
        >
          Cancel
        </Button>
      </Form>
    </>
  );
};

export default SponsorForm;
