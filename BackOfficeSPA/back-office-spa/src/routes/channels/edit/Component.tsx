import React, { useEffect, useState } from 'react';
import { useFetcher, useNavigate, useLoaderData } from 'react-router';
import { Form, Button, Modal } from 'react-bootstrap';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';
import { useToast } from '@components/ToastNotification/ToastContext';
import { Channel, UpdateChannelDTO } from '@models';
import PageHeader from '@components/PageHeader';

const EditChannel: React.FC = () => {
  const [model, setModel] = useState<UpdateChannelDTO | null>(null);
  const [showModal, setShowModal] = useState(false);
  const navigate = useNavigate();
  const toast = useToast();

  const entity = useLoaderData<Channel>();

  useEffect(() => {
    if (entity) {
      setModel({
        channelId: entity.channelId,
        channelName: entity.channelName,
      });
    }
  }, [entity]);

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
      toast.show('Success', 'Channel updated successfully', { variant: 'success' });
      navigate('..');
    }
  }, [result, navigate, toast]);

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    setShowModal(true);
  };

  const confirmEdit = () => {
    fetcher.submit(
      { ...model },
      {
        method: 'post',
        action: location.pathname,
      }
    );
  };

  const isDisabled = () => !model || !model.channelName || model.channelName.length === 0 || busy;

  if (!model) {
    return <div>Loading...</div>;
  }

  return (
    <>
      <PageHeader title="Edit Channel" />
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
          <FieldError error={errors?.ChannelName} />
        </Form.Group>

        <Button variant="success" disabled={isDisabled()} type="submit" className="mt-2">
          Save Changes
        </Button>
      </Form>

      <Modal show={showModal} onHide={() => setShowModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>Confirm Edit</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <p>Are you sure you want to save the following changes?</p>
          <p>
            <strong>Channel Name:</strong> {model.channelName}{' '}
            {model.channelName !== entity.channelName && (
              <>
                (<s>{entity.channelName}</s>)
              </>
            )}
          </p>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowModal(false)}>
            Cancel
          </Button>
          <Button variant="primary" onClick={confirmEdit} data-testid="edit-modal-confirm">
            Save Changes
          </Button>
        </Modal.Footer>
      </Modal>
    </>
  );
};

export default EditChannel;
