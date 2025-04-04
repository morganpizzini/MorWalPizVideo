import React, { useEffect, useState, useRef } from 'react';
import { useFetcher, useLoaderData } from 'react-router';
import { Form, Button, Card, Image, Modal } from 'react-bootstrap';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';
import { useToast } from '@components/ToastNotification/ToastContext';
import PageHeader from '@components/PageHeader';
import { LoaderData } from './loader';

const ImageUpload: React.FC = () => {
  const [image, setImage] = useState<File | null>(null);
  const [folderName, setFolderName] = useState('');
  const [loadInMatchFolder, setLoadInMatchFolder] = useState(true);
  const [imagePreview, setImagePreview] = useState<string | null>(null);
  const [showConfirmModal, setShowConfirmModal] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const toast = useToast();

  // Load matches from the API using the loader
  const { matches } = useLoaderData() as LoaderData;

  const fetcher = useFetcher();
  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;
  const result =
    fetcher.data != undefined &&
      (fetcher.data.errors == undefined || fetcher.data.errors.length == 0)
      ? fetcher.data
      : null;

  // Get selected match name
  const selectedMatch = matches.find(match => match.url === folderName);
  const selectedMatchName = selectedMatch?.title || folderName;

  useEffect(() => {
    if (!result) return;

    if (result.success) {
      toast.show('Success', 'Image uploaded successfully', { variant: 'success' });
      // Reset form
      setImage(null);
      setImagePreview(null);
      setFolderName('');
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    }
  }, [result, toast]);

  const handleImageChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      const selectedFile = e.target.files[0];
      setImage(selectedFile);

      // Create a preview URL for the selected image
      const reader = new FileReader();
      reader.onloadend = () => {
        setImagePreview(reader.result as string);
      };
      reader.readAsDataURL(selectedFile);
    }
  };

  const handleSubmitConfirm = () => {
    if (!image) return;

    const formData = new FormData();
    formData.append('image', image);
    formData.append('folderName', folderName);
    formData.append('loadInMatchFolder', loadInMatchFolder.toString());

    fetcher.submit(formData, {
      method: 'post',
      action: location.pathname,
      encType: 'multipart/form-data',
    });

    setShowConfirmModal(false);
  };

  const handleFormSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    setShowConfirmModal(true);
  };

  const isDisabled = () => !image || !folderName || busy;

  return (
    <>
      <PageHeader title="Upload Image" />
      <GenericErrorList errors={errors?.generics} />
      <Form onSubmit={handleFormSubmit}>
        <Form.Group controlId="formImage" className="mb-3">
          <Form.Label>
            Image File <span className="text-danger">*</span>
          </Form.Label>
          <Form.Control
            type="file"
            onChange={handleImageChange}
            accept="image/*"
            ref={fileInputRef}
          />
          <FieldError error={errors?.image} />

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

        <Form.Group controlId="formFolderName" className="mb-3">
          <Form.Label>
            Match Folder <span className="text-danger">*</span>
          </Form.Label>
          <Form.Select
            value={folderName}
            onChange={e => setFolderName(e.target.value)}
          >
            <option value="">Select a match folder</option>
            {matches.map(match => (
              <option key={match.id} value={match.url}>
                {match.title}
              </option>
            ))}
          </Form.Select>
          <FieldError error={errors?.folderName} />
        </Form.Group>

        <Form.Group controlId="formLoadInMatchFolder" className="mb-3">
          <Form.Check
            type="checkbox"
            label="Load In Match Folder"
            checked={loadInMatchFolder}
            onChange={e => setLoadInMatchFolder(e.target.checked)}
          />
        </Form.Group>

        <div className="d-flex justify-content-end mt-3">
          <Button variant="success" type="submit" disabled={isDisabled()}>
            {busy ? 'Uploading...' : 'Upload Image'}
          </Button>
        </div>
      </Form>

      {/* Confirmation Modal */}
      <Modal show={showConfirmModal} onHide={() => setShowConfirmModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>Confirm Image Upload</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <p><strong>Please confirm the following details:</strong></p>
          <p><strong>Image:</strong> {image?.name}</p>
          <p><strong>Size:</strong> {image ? (image.size / 1024).toFixed(2) + ' KB' : ''}</p>
          <p><strong>Match Folder:</strong> {selectedMatchName}</p>
          <p><strong>Load In Match Folder:</strong> {loadInMatchFolder ? 'Yes' : 'No'}</p>

          {imagePreview && (
            <div className="text-center mt-3">
              <h6>Image Preview</h6>
              <Image
                src={imagePreview}
                alt="Preview"
                style={{ maxHeight: '200px', maxWidth: '100%' }}
                thumbnail
              />
            </div>
          )}
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowConfirmModal(false)}>
            Cancel
          </Button>
          <Button variant="primary" onClick={handleSubmitConfirm} disabled={busy}>
            {busy ? 'Uploading...' : 'Confirm Upload'}
          </Button>
        </Modal.Footer>
      </Modal>
    </>
  );
};

export default ImageUpload;