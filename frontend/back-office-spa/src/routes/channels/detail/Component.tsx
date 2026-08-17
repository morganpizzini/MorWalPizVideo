import React, { useState, useEffect } from 'react';
import { useLoaderData, useFetcher, useNavigate, useLocation } from 'react-router';
import { Button, Modal } from 'react-bootstrap';
import { ComposeUrl, Delete, endpoints, postFormData } from '@morwalpizvideo/services';
import { useToast } from '@components/ToastNotification/ToastContext';
import { Channel } from '@morwalpizvideo/models';
import DetailPanel from '@components/DetailPanel';
import PageHeader from '@components/PageHeader';
import GenericErrorList from '@components/GenericErrorList';
import { useChannelContext } from '../../../contexts/ChannelContext';

const ChannelDetail: React.FC = () => {
  const entity = useLoaderData<Channel>();
  const [showModal, setShowModal] = useState(false);
  const [logoFile, setLogoFile] = useState<File | null>(null);
  const [logoUrl, setLogoUrl] = useState(entity?.channelLogoUrl ?? '');
  const [logoBusy, setLogoBusy] = useState(false);
  const navigate = useNavigate();
  const toast = useToast();
  const location = useLocation();
  const { selectChannel } = useChannelContext();
  const isSelfRoute = location.pathname.startsWith('/my-channel');

  const fetcher = useFetcher();
  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;
  const result = fetcher.data?.success ? fetcher.data : null;

  useEffect(() => {
    setLogoUrl(entity?.channelLogoUrl ?? '');
  }, [entity]);

  useEffect(() => {
    if (!result) return;

    setShowModal(false);

    if (result.success) {
      toast.show('Success', 'Channel deleted successfully', { variant: 'success' });
      navigate('..');
    }
  }, [result, navigate]);

  const handleDelete = () => {
    setShowModal(true);
  };

  const navigateToChannelManagement = (path: string) => {
    selectChannel(entity.channelId);
    navigate(path);
  };

  const confirmDelete = () => {
    const actionPath = location.pathname.substring(0, location.pathname.lastIndexOf('/'));
    fetcher.submit(
      { id: entity.channelId },
      {
        method: 'post',
        action: actionPath,
      }
    );
  };

  const uploadLogo = async () => {
    if (!logoFile) return;

    setLogoBusy(true);
    try {
      const formData = new FormData();
      formData.append('logo', logoFile);
      const response = await postFormData(
        ComposeUrl(endpoints.CHANNEL_LOGO, { channelId: entity.channelId }),
        formData
      );
      if (response?.errors) {
        toast.show('Logo upload failed', String(response.errors[0] ?? 'Unable to upload channel logo'), { variant: 'danger' });
        return;
      }

      setLogoUrl(response?.channelLogoUrl ?? '');
      setLogoFile(null);
      toast.show('Success', 'Channel logo uploaded successfully', { variant: 'success' });
    } catch {
      toast.show('Logo upload failed', 'Unable to upload channel logo', { variant: 'danger' });
    } finally {
      setLogoBusy(false);
    }
  };

  const removeLogo = async () => {
    setLogoBusy(true);
    try {
      const response = await Delete(ComposeUrl(endpoints.CHANNEL_LOGO, { channelId: entity.channelId }));
      if (response?.errors) {
        toast.show('Logo removal failed', String(response.errors[0] ?? 'Unable to remove channel logo'), { variant: 'danger' });
        return;
      }

      setLogoUrl('');
      toast.show('Success', 'Channel logo removed successfully', { variant: 'success' });
    } catch {
      toast.show('Logo removal failed', 'Unable to remove channel logo', { variant: 'danger' });
    } finally {
      setLogoBusy(false);
    }
  };

  if (!entity) {
    return <div>Loading...</div>;
  }

  return (
    <>
      <PageHeader
        title="Channel Detail"
        editLink={isSelfRoute ? '/my-channel/edit' : `/channels/${entity.channelId}/edit`}
        deleteCallback={handleDelete}
      />
      <GenericErrorList errors={errors?.generics} />
      <DetailPanel title="Dettagli dell'entità">
        <p>
          <strong>Channel Name:</strong> {entity.channelName}
        </p>
        <p>
          <strong>YouTube Channel ID:</strong> {entity.yTChannelId}
        </p>
        <p>
          <strong>Short link base URL:</strong> {entity.shortLinkUrl || 'Not configured'}
        </p>
        <div>
          <strong>Socials:</strong>
          {entity.socials?.length ? (
            <ul>
              {entity.socials.map((social, index) => (
                <li key={`${social.provider}-${index}`}>{social.provider}: {social.handler}</li>
              ))}
            </ul>
          ) : <span> None configured</span>}
        </div>
        <div className="mt-3">
          <strong>Channel logo:</strong>
          {logoUrl && <img src={logoUrl} alt={`${entity.channelName} logo`} className="d-block my-2" style={{ maxWidth: 250, maxHeight: 150 }} />}
          <div className="d-flex gap-2 align-items-center mt-2">
            <input
              type="file"
              accept="image/png"
              aria-label="Channel logo PNG"
              onChange={event => setLogoFile(event.target.files?.[0] ?? null)}
              disabled={logoBusy}
            />
            <Button type="button" variant="outline-primary" onClick={uploadLogo} disabled={!logoFile || logoBusy}>
              Upload PNG logo
            </Button>
            {logoUrl && <Button type="button" variant="outline-danger" onClick={removeLogo} disabled={logoBusy}>Remove logo</Button>}
          </div>
        </div>
      </DetailPanel>
      <DetailPanel title="Channel content management">
        <div className="d-flex flex-wrap gap-2">
          <Button type="button" variant="outline-primary" onClick={() => navigateToChannelManagement('/pages')}>
            Pages
          </Button>
          <Button type="button" variant="outline-primary" onClick={() => navigateToChannelManagement('/navigation')}>
            Navigation
          </Button>
        </div>
      </DetailPanel>
      <Modal show={showModal} onHide={() => setShowModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>Confirm Delete</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <p>Are you sure you want to delete the following channel?</p>
          <p>
            <strong>Channel Name:</strong> {entity.channelName}
          </p>
          <p>
            <strong>YouTube Channel ID:</strong> {entity.channelId}
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

export default ChannelDetail;
