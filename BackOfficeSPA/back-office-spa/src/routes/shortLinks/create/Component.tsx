import React, { useState, useEffect } from 'react';
import { Form, Button, Modal } from 'react-bootstrap';
import { useNavigate, useFetcher } from 'react-router';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';
import { useToast } from '@components/ToastNotification/ToastContext';
import PageHeader from '@components/PageHeader';

const CreateShortLink: React.FC = () => {
  const [videoId, setVideoId] = useState('');
  const [queryString, setQueryString] = useState('');
  const [message, setMessage] = useState('');
  const [showModal, setShowModal] = useState(false);
  const navigate = useNavigate();
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
    setShowModal(false);

    if (result.success) {
      toast.show('Success', 'Short link created successfully', { variant: 'success' });
      navigate('..');
    }
  }, [result, navigate, toast]);

  const isDisabled = () => videoId.length === 0 || busy;

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    setShowModal(true);
  };

  const confirmCreate = () => {
    fetcher.submit(
      {
        videoId,
        queryString,
        message,
      },
      {
        method: 'post',
        action: location.pathname,
      }
    );
  };

  return (
    <>
      <PageHeader title="Create Short Link" />
      <GenericErrorList errors={errors?.generics} />
      <Form onSubmit={handleSubmit}>
        <Form.Group controlId="formVideoId">
          <Form.Label>
            Video ID <span className="text-danger">*</span>
          </Form.Label>
          <Form.Control type="text" value={videoId} onChange={e => setVideoId(e.target.value)} />
          <FieldError error={errors?.videoId} />
        </Form.Group>
        <Form.Group controlId="formQueryString">
          <Form.Label>Query String</Form.Label>
          <Form.Control
            type="text"
            value={queryString}
            onChange={e => setQueryString(e.target.value)}
          />
        </Form.Group>
        <Form.Group controlId="formMessage">
          <Form.Label>Message</Form.Label>
          <Form.Control type="text" value={message} onChange={e => setMessage(e.target.value)} />
        </Form.Group>
        <Button variant="success" disabled={isDisabled()} type="submit" className="mt-2">
          Create
        </Button>
      </Form>

      <Modal show={showModal} onHide={() => setShowModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>Confirm Create</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <p>Are you sure you want to create the following short link?</p>
          <p>
            <strong>Video ID:</strong> {videoId}
          </p>
          {queryString && (
            <p>
              <strong>Query String:</strong> {queryString}
            </p>
          )}
          {message && (
            <p>
              <strong>Message:</strong> {message}
            </p>
          )}
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowModal(false)}>
            Cancel
          </Button>
          <Button variant="success" onClick={confirmCreate} data-testid="create-modal-confirm">
            Create
          </Button>
        </Modal.Footer>
      </Modal>
    </>
  );
};

export default CreateShortLink;
