import React, { useState, useEffect } from 'react';
import { Form, Button, Modal } from 'react-bootstrap';
import { useNavigate, useFetcher } from 'react-router';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';
// import { useToast } from '@components/ToastNotification/ToastContext';
import { CreateChannelDTO } from '@models';
import PageHeader from '@components/PageHeader';

const CreateChannel: React.FC = () => {
  const [model, setModel] = useState<CreateChannelDTO>({
    channelName: '',
    yTChannelId: '',
  });
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
      toast.show('Success', 'Channel created successfully', { variant: 'success' });
      navigate('..');
    }
  }, [result, navigate, toast]);

  const isDisabled = () =>
    !model.channelName ||
    model.channelName.length === 0 ||
    !model.yTChannelId ||
    model.yTChannelId.length === 0 ||
    busy;

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    setShowModal(true);
  };

  const confirmCreate = () => {
    fetcher.submit(
      {
        ...model,
      },
      {
        method: 'post',
        action: location.pathname,
      }
    );
  };

  return (
    <>
      <PageHeader title="Create Channel" />
      <GenericErrorList errors={errors?.generics} />
      <Form onSubmit={handleSubmit}>
        <Form.Group controlId="formChannelName">
          <Form.Label>
            Channel Name <span className="text-danger">*</span>
          </Form.Label>
          <Form.Control
            type="text"
            value={model.channelName}
            onChange={e => setModel({ ...model, channelName: e.target.value })}
          />
          <FieldError error={errors?.channelName} />
        </Form.Group>

        <Form.Group controlId="formYTChannelId">
          <Form.Label>
            YouTube Channel ID <span className="text-danger">*</span>
          </Form.Label>
          <Form.Control
            type="text"
            value={model.yTChannelId}
            onChange={e => setModel({ ...model, yTChannelId: e.target.value })}
          />
          <FieldError error={errors?.yTChannelId} />
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
          <p>Are you sure you want to create the following channel?</p>
          <p>
            <strong>Channel Name:</strong> {model.channelName}
          </p>
          <p>
            <strong>YouTube Channel ID:</strong> {model.yTChannelId}
          </p>
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

export default CreateChannel;
