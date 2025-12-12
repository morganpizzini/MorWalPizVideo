import React, { useEffect, useState, useRef } from 'react';
import { Form, Button, Card, Image } from 'react-bootstrap';
import { useLoaderData, useNavigate, useFetcher } from 'react-router';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';
import type { Sponsor } from '@models';

const EditSponsor: React.FC = () => {
  const sponsor = useLoaderData() as Sponsor;
  const navigate = useNavigate();
  const toast = useToast();
  const fetcher = useFetcher();
  const [imageFile, setImageFile] = useState<File | null>(null);
  const [imagePreview, setImagePreview] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;
  const result =
    fetcher.data != undefined &&
    (fetcher.data.errors == undefined || fetcher.data.errors.length == 0)
      ? fetcher.data
      : null;

  useEffect(() => {
    if (!result || busy) return;

    toast.show('Success', 'Sponsor updated successfully', { variant: 'success' });
    navigate('/sponsors');
  }, [result, busy, navigate]);

  const handleImageChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      const selectedFile = e.target.files[0];
      setImageFile(selectedFile);

      const reader = new FileReader();
      reader.onloadend = () => {
        setImagePreview(reader.result as string);
      };
      reader.readAsDataURL(selectedFile);
    }
  };

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    const formData = new FormData(e.currentTarget);
    if (imageFile) {
      formData.append('image', imageFile);
    }

    fetcher.submit(formData, {
      method: 'post',
      encType: 'multipart/form-data',
    });
  };

  return (
    <>
      <PageHeader title="Edit Sponsor" />
      <GenericErrorList errors={errors?.generics} />

      <Form onSubmit={handleSubmit} className="mt-4">
        <Form.Group className="mb-3">
          <Form.Label>Title</Form.Label>
          <Form.Control
            type="text"
            name="title"
            required
            defaultValue={sponsor.title}
            placeholder="Enter sponsor name"
          />
        </Form.Group>

        <Form.Group className="mb-3">
          <Form.Label>URL</Form.Label>
          <Form.Control
            type="url"
            name="url"
            required
            defaultValue={sponsor.url}
            placeholder="https://example.com"
          />
        </Form.Group>

        {sponsor.imgSrc && (
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
          <Form.Label>Update Sponsor Logo Image (Optional)</Form.Label>
          <Form.Control
            type="file"
            accept="image/*"
            onChange={handleImageChange}
            ref={fileInputRef}
          />
          <Form.Text className="text-muted">
            Upload a new logo image (JPG, PNG, etc.). Leave empty to keep the current image.
          </Form.Text>

          {imagePreview && (
            <Card className="mt-3">
              <Card.Body className="text-center">
                <Image
                  src={imagePreview}
                  alt="New Preview"
                  style={{ maxHeight: '200px', maxWidth: '100%' }}
                  thumbnail
                />
              </Card.Body>
            </Card>
          )}
        </Form.Group>

        <Button variant="primary" type="submit" disabled={busy}>
          {busy ? 'Updating...' : 'Update Sponsor'}
        </Button>
        <Button
          variant="secondary"
          className="ms-2"
          onClick={() => navigate('/sponsors')}
        >
          Cancel
        </Button>
      </Form>
    </>
  );
};

export default EditSponsor;
