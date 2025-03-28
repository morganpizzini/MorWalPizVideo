import React, { useState, useEffect, useMemo } from 'react';
import { Button, Modal } from 'react-bootstrap';
import { Link, useLoaderData, useFetcher, useLocation } from 'react-router';
import { Channel } from '@models';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';
import GenericTable from '@components/Table';
import { ColumnDef } from '@tanstack/react-table';

const ChannelLinks: React.FC = () => {
  const [showModal, setShowModal] = useState(false);
  const [selectedChannel, setSelectedChannel] = useState<Channel | null>(null);
  const toast = useToast();
  const location = useLocation();

  const entities = useLoaderData<Channel[]>();

  const fetcher = useFetcher();
  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;
  const result =
    fetcher.data != undefined &&
    (fetcher.data.errors == undefined || fetcher.data.errors.length == 0)
      ? fetcher.data
      : null;

  // Definizione delle colonne
  const columns = useMemo<ColumnDef<Channel>[]>(
    () => [
      {
        accessorKey: 'channelName',
        header: 'Channel Name',
        cell: info => info.getValue(),
      },
      {
        accessorKey: 'yTChannelId',
        header: 'YT Channel ID',
        cell: info => info.getValue(),
      },
      {
        id: 'actions',
        header: () => <div className="text-end">Actions</div>,
        cell: props => {
          const channel = props.row.original;
          return (
            <div className="text-end">
              <Link className="btn btn-link px-1" to={`/channels/${channel.channelId}`}>
                Detail
              </Link>
              <Link className="btn btn-link px-1" to={`/channels/${channel.channelId}/edit`}>
                Edit
              </Link>
              <Button variant="link" className="px-1" onClick={() => handleDelete(channel)}>
                Delete
              </Button>
            </div>
          );
        },
      },
    ],
    []
  );

  useEffect(() => {
    if (!result) return;
    setShowModal(false);

    if (result.success) {
      toast.show('Success', 'Channel deleted successfully', { variant: 'success' });
    }
  }, [result, toast]);

  const handleDelete = (channel: Channel) => {
    setSelectedChannel(channel);
    setShowModal(true);
  };

  const confirmDelete = () => {
    if (!selectedChannel) return;
    fetcher.submit(
      {
        id: selectedChannel.channelId,
      },
      {
        method: 'post',
        action: location.pathname,
      }
    );
  };

  return (
    <>
      <PageHeader title="Channels" createLink={`./create`} />
      <GenericErrorList errors={errors?.generics} />

      <GenericTable
        data={entities}
        columns={columns}
        pageSize={10}
        searchPlaceholder="Search channels..."
        emptyMessage="No channels found"
      />

      <Modal show={showModal} onHide={() => setShowModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>Confirm Delete</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <p>Are you sure you want to delete the following channel?</p>
          <p>
            <strong>Channel Name:</strong> {selectedChannel?.channelName}
          </p>
          <p>
            <strong>YT Channel ID:</strong> {selectedChannel?.yTChannelId}
          </p>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowModal(false)}>
            Cancel
          </Button>
          <Button
            variant="danger"
            disabled={busy}
            onClick={confirmDelete}
            data-testid="delete-modal-confirm"
          >
            Delete
          </Button>
        </Modal.Footer>
      </Modal>
    </>
  );
};

export default ChannelLinks;
