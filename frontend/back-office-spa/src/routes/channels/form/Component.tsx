import React, { useState, useEffect } from 'react';
import { Form, Button, Modal } from 'react-bootstrap';
import { useNavigate, useFetcher, useLoaderData, useParams } from 'react-router';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';
import { useToast } from '@components/ToastNotification/ToastContext';
import { Channel } from '@morwalpizvideo/models';
import PageHeader from '@components/PageHeader';

const ChannelForm: React.FC = () => {
  const entity = useLoaderData() as Channel | null;
  const params = useParams();
  const isEditMode = !!params.id;

  const [channelName, setChannelName] = useState(entity?.channelName || '');
  const [yTChannelId, setYTChannelId] = useState('');
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
    if (entity) {
      setChannelName(entity.channelName);
    }
  }, [entity]);

  useEffect(() => {
    if (!result) return;
    setShowModal(false);
    if (result.success) {
      toast.show(
        'Success',
        isEditMode ? 'Channel updated successfully' : 'Channel created successfully',
        { variant: 'success' }
      );
      navigate('..');
    }
  }, [result, navigate, isEditMode]);

  const isDisabled = () => {
    if (!channelName || channelName.trim().length === 0 || busy) return true;
    if (!isEditMode && (!yTChannelId || yTChannelId.trim().length === 0)) return true;
    return false;
  };

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    setShowModal(true);
  };

  const confirmSubmit = () => {
    const payload: Record<string, string> = { channelName };
    if (!isEditMode) {
      payload.yTChannelId = yTChannelId;
    }
    fetcher.submit(payload, { method: 'post', action: location.pathname });
  };

  return (
    <>
      <PageHeader title={isEditMode ? 'Edit Channel' : 'Create Channel'} />
      <GenericErrorList errors={errors?.generics} />
      <Form onSubmit={handleSubmit}>
        <Form.Group controlId="formChannelName" className="mb-3">
          <Form.Label>
            Channel Name <span className="text-danger">*</span>
          </Form.Label>
          <Form.Control
            type="text"
            value={channelName}
            onChange={e => setChannelName(e.target.value)}
          />
          <FieldError error={errors?.channelName} />
        </Form.Group>

        {!isEditMode && (
          <Form.Group controlId="formYTChannelId" className="mb-3">
            <Form.Label>
              YouTube Channel ID <span className="text-danger">*</span>
            </Form.Label>
            <Form.Control
              type="text"
              value={yTChannelId}
              onChange={e => setYTChannelId(e.target.value)}
            />
            <FieldError error={errors?.yTChannelId} />
          </Form.Group>
        )}

        <Button variant="success" disabled={isDisabled()} type="submit" className="mt-2">
          {isEditMode ? 'Save Changes' : 'Create'}
        </Button>
      </Form>

      <Modal show={showModal} onHide={() => setShowModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>{isEditMode ? 'Confirm Edit' : 'Confirm Create'}</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <p>
            Are you sure you want to{' '}
            {isEditMode ? 'save the changes to' : 'create'} the following channel?
          </p>
          <p>
            <strong>Channel Name:</strong> {channelName}
            {isEditMode && channelName !== entity?.channelName && (
              <>
                {' '}
                (<s>{entity?.channelName}</s>)
              </>
            )}
          </p>
          {!isEditMode && (
            <p>
              <strong>YouTube Channel ID:</strong> {yTChannelId}
            </p>
          )}
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowModal(false)}>
            Cancel
          </Button>
          <Button
            variant="success"
            onClick={confirmSubmit}
            disabled={busy}
            data-testid={isEditMode ? 'edit-modal-confirm' : 'create-modal-confirm'}
          >
            {isEditMode ? 'Save Changes' : 'Create'}
          </Button>
        </Modal.Footer>
      </Modal>
    </>
  );
};

export default ChannelForm;
