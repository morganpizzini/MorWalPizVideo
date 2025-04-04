import React, { useEffect, useState, useRef } from 'react';
import { useFetcher, useLoaderData } from 'react-router';
import { Form, Button, Card, Row, Col, Image, CloseButton, Modal } from 'react-bootstrap';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';
import { useToast } from '@components/ToastNotification/ToastContext';
import PageHeader from '@components/PageHeader';
import { LoaderData } from './loader';

interface ImagePreview {
  file: File;
  preview: string;
}

const MultipleImageUpload: React.FC = () => {
  const [images, setImages] = useState<ImagePreview[]>([]);
  const [folderName, setFolderName] = useState('');
  const [loadInMatchFolder, setLoadInMatchFolder] = useState(true);
  const [showConfirmModal, setShowConfirmModal] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const toast = useToast();

  // Load matches from the API using the loader
  const { matches } = useLoaderData() as LoaderData;

  // Get selected match name
  const selectedMatch = matches.find(match => match.url === folderName);
  const selectedMatchName = selectedMatch?.title || folderName;

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
      toast.show('Success', 'Images uploaded successfully', { variant: 'success' });
      // Reset form
      setImages([]);
      setFolderName('');
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    }
  }, [result, toast]);

  const handleImagesChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0) {
      const selectedFiles = Array.from(e.target.files);

      // Create preview URLs for all selected images
      const newImages = selectedFiles.map(file => {
        return {
          file,
          preview: URL.createObjectURL(file)
        };
      });

      setImages(prev => [...prev, ...newImages]);
    }
  };

  const handleRemoveImage = (index: number) => {
    setImages(prev => {
      const updatedImages = [...prev];

      // Revoke the URL to avoid memory leaks
      URL.revokeObjectURL(updatedImages[index].preview);

      // Remove the image from the array
      updatedImages.splice(index, 1);
      return updatedImages;
    });
  };

  const handleFormSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    setShowConfirmModal(true);
  };

  const handleSubmitConfirm = () => {
    if (images.length === 0) return;

    const formData = new FormData();

    // Add all images to the FormData
    images.forEach(image => {
      formData.append('images', image.file);
    });

    formData.append('folderName', folderName);
    formData.append('loadInMatchFolder', loadInMatchFolder.toString());

    fetcher.submit(formData, {
      method: 'post',
      action: location.pathname,
      encType: 'multipart/form-data',
    });

    setShowConfirmModal(false);
  };

  const isDisabled = () => images.length === 0 || !folderName || busy;

  // Calculate total size of all images
  const totalSizeKB = images.reduce((total, img) => total + img.file.size / 1024, 0).toFixed(2);

  return (
    <>
      <PageHeader title="Upload Multiple Images" />
      <GenericErrorList errors={errors?.generics} />
      <Form onSubmit={handleFormSubmit}>
        <Form.Group controlId="formImages" className="mb-3">
          <Form.Label>
            Image Files <span className="text-danger">*</span>
          </Form.Label>
          <Form.Control
            type="file"
            onChange={handleImagesChange}
            accept="image/*"
            multiple
            ref={fileInputRef}
          />
          <FieldError error={errors?.images} />

          {images.length > 0 && (
            <Card className="mt-3">
              <Card.Body>
                <Card.Title>Selected Images ({images.length})</Card.Title>
                <Row xs={2} md={4} className="g-3">
                  {images.map((image, index) => (
                    <Col key={index}>
                      <Card>
                        <div style={{ position: 'relative' }}>
                          <CloseButton
                            style={{
                              position: 'absolute',
                              top: '5px',
                              right: '5px',
                              backgroundColor: 'rgba(255,255,255,0.7)',
                              borderRadius: '50%'
                            }}
                            onClick={() => handleRemoveImage(index)}
                          />
                          <Card.Img
                            variant="top"
                            src={image.preview}
                            alt={`Preview ${index}`}
                            style={{ height: '120px', objectFit: 'cover' }}
                          />
                        </div>
                        <Card.Body>
                          <small className="text-muted">{image.file.name}</small>
                        </Card.Body>
                      </Card>
                    </Col>
                  ))}
                </Row>
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
            {busy ? 'Uploading...' : `Upload ${images.length} Images`}
          </Button>
        </div>
      </Form>

      {/* Confirmation Modal */}
      <Modal show={showConfirmModal} onHide={() => setShowConfirmModal(false)} size="lg">
        <Modal.Header closeButton>
          <Modal.Title>Confirm Multiple Image Upload</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <p><strong>Please confirm the following details:</strong></p>
          <p><strong>Number of Images:</strong> {images.length}</p>
          <p><strong>Total Size:</strong> {totalSizeKB} KB</p>
          <p><strong>Match Folder:</strong> {selectedMatchName}</p>
          <p><strong>Load In Match Folder:</strong> {loadInMatchFolder ? 'Yes' : 'No'}</p>

          {images.length > 0 && (
            <div className="mt-3">
              <h6>Image Previews</h6>
              <Row xs={2} md={4} className="g-2">
                {images.map((image, index) => (
                  <Col key={index}>
                    <Card>
                      <Card.Img
                        variant="top"
                        src={image.preview}
                        alt={`Preview ${index}`}
                        style={{ height: '100px', objectFit: 'cover' }}
                      />
                      <Card.Body className="p-2">
                        <small className="text-muted" style={{ fontSize: '0.7rem' }}>{image.file.name.substring(0, 15)}{image.file.name.length > 15 ? '...' : ''}</small>
                      </Card.Body>
                    </Card>
                  </Col>
                ))}
              </Row>
            </div>
          )}
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowConfirmModal(false)}>
            Cancel
          </Button>
          <Button variant="primary" onClick={handleSubmitConfirm} disabled={busy}>
            {busy ? 'Uploading...' : `Confirm Upload (${images.length} Images)`}
          </Button>
        </Modal.Footer>
      </Modal>
    </>
  );
};

export default MultipleImageUpload;