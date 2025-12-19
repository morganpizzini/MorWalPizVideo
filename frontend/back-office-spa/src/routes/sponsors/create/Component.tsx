import React, { useEffect, useState, useRef } from 'react';
import { Form, Button, Card, Image } from 'react-bootstrap';
import { useNavigate, useFetcher } from 'react-router';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';

const CreateSponsor: React.FC = () => {
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

    toast.show('Success', 'Sponsor created successfully', { variant: 'success' });
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
    
    if (!imageFile) {
      toast.show('Error', 'Please select an image', { variant: 'danger' });
      return;
    }

    const formData = new FormData(e.currentTarget);
    formData.append('image', imageFile);

    fetcher.submit(formData, {
      method: 'post',
      encType: 'multipart/form-data',
    });
  };

  return (
    <>
      <PageHeader title="Create Sponsor" />
      <GenericErrorList errors={errors?.generics} />

      <Form onSubmit={handleSubmit} className="mt-4">
        <Form.Group className="mb-3">
          <Form.Label>Title</Form.Label>
          <Form.Control
            type="text"
            name="title"
            required
            placeholder="Enter sponsor name"
          />
        </Form.Group>

        <Form.Group className="mb-3">
          <Form.Label>URL</Form.Label>
          <Form.Control
            type="url"
            name="url"
            required
            placeholder="https://example.com"
          />
        </Form.Group>

        <Form.Group className="mb-3">
          <Form.Label>Sponsor Logo Image</Form.Label>
          <Form.Control
            type="file"
            accept="image/*"
            onChange={handleImageChange}
            ref={fileInputRef}
            required
          />
          <Form.Text className="text-muted">
            Upload the sponsor's logo image (JPG, PNG, etc.)
          </Form.Text>

          {imagePreview && (
            <Card className="mt-3">
              <Card.Body className="text-center">
                <Image
                  src={imagePreview}
                  alt="Preview"
                  style={{ maxHeight: '200px', maxWidth: '100%' }}
                  thumbnail
                />
              </Card.Body>
            </Card>
          )}
        </Form.Group>

        <Button variant="primary" type="submit" disabled={busy}>
          {busy ? 'Creating...' : 'Create Sponsor'}
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

export default CreateSponsor;
