import React, { useState, useEffect } from 'react';
import { Form, Button, Modal } from 'react-bootstrap';
import { useNavigate, useFetcher, useLoaderData } from 'react-router';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';
import { useToast } from '@components/ToastNotification/ToastContext';
import { Channel, ChannelSocial } from '@morwalpizvideo/models';
import PageHeader from '@components/PageHeader';
import { hasCacheInvalidationWarning } from '../response';

type PublishingProviderKey = 'telegram' | 'discord' | 'facebook';
type EditablePublishingProvider = {
  destinationId: string;
  credential: string;
  credentialConfigured: boolean;
  clearCredential: boolean;
};

const publishingProviders: ReadonlyArray<{
  key: PublishingProviderKey;
  name: string;
  destinationLabel: string;
  credentialLabel: string;
}> = [
  { key: 'telegram', name: 'Telegram', destinationLabel: 'Chat ID', credentialLabel: 'Bot token' },
  { key: 'discord', name: 'Discord', destinationLabel: 'Channel ID', credentialLabel: 'Bot token' },
  { key: 'facebook', name: 'Facebook', destinationLabel: 'Page ID', credentialLabel: 'Access token' },
];

const emptyPublishingProvider = (): EditablePublishingProvider => ({
  destinationId: '',
  credential: '',
  credentialConfigured: false,
  clearCredential: false,
});

const ChannelForm: React.FC = () => {
  const entity = useLoaderData() as Channel | null;
  const isEditMode = entity !== null;

  const [channelName, setChannelName] = useState(entity?.channelName || '');
  const [yTChannelId, setYTChannelId] = useState('');
  const [isSHIT, setIsSHIT] = useState(false);
  const [shortLinkUrl, setShortLinkUrl] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [socials, setSocials] = useState<ChannelSocial[]>([]);
  const [socialPublishing, setSocialPublishing] = useState<Record<PublishingProviderKey, EditablePublishingProvider>>({
    telegram: emptyPublishingProvider(),
    discord: emptyPublishingProvider(),
    facebook: emptyPublishingProvider(),
  });

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
      setIsSHIT(entity.isSHIT ?? false);
      setShortLinkUrl(entity.shortLinkUrl ?? '');
      setSocials(entity.socials?.map(social => ({ provider: social.provider, handler: social.handler })) ?? []);
      setSocialPublishing({
        telegram: { ...emptyPublishingProvider(), ...entity.socialPublishing?.telegram },
        discord: { ...emptyPublishingProvider(), ...entity.socialPublishing?.discord },
        facebook: { ...emptyPublishingProvider(), ...entity.socialPublishing?.facebook },
      });
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
      if (hasCacheInvalidationWarning(result)) {
        toast.show('Warning', result.cacheInvalidation?.message ?? 'The operation completed, but the public cache was not reset.', { variant: 'warning' });
      }
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
    const payload: Record<string, string> = { channelName, isSHIT: String(isSHIT) };
    if (!isEditMode) {
      payload.yTChannelId = yTChannelId;
    }
    fetcher.submit(
      {
        ...payload,
        shortLinkUrl,
        socials: JSON.stringify(socials),
        socialPublishing: JSON.stringify(socialPublishing),
      },
      { method: 'post', action: location.pathname }
    );
  };

  const updateSocial = (index: number, field: keyof ChannelSocial, value: string) => {
    setSocials(current => current.map((social, socialIndex) =>
      socialIndex === index ? { ...social, [field]: value } : social
    ));
  };

  const updatePublishing = (
    provider: PublishingProviderKey,
    values: Partial<EditablePublishingProvider>
  ) => {
    setSocialPublishing(current => ({
      ...current,
      [provider]: { ...current[provider], ...values },
    }));
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

        <Form.Group controlId="formShortLinkUrl" className="mb-3">
          <Form.Label>Short link base URL</Form.Label>
          <Form.Control type="url" value={shortLinkUrl} onChange={e => setShortLinkUrl(e.target.value)} placeholder="https://example.com/sl" />
        </Form.Group>

        <Form.Check
          type="checkbox"
          id="formIsSHIT"
          label="Shooting ITA channel"
          checked={isSHIT}
          onChange={event => setIsSHIT(event.target.checked)}
          className="mb-3"
        />

        <Form.Label>Socials</Form.Label>
        {socials.map((social, index) => (
          <div className="row g-2 align-items-center mb-2" key={`${social.provider}-${index}`}>
            <div className="col-12 col-md-3">
              <Form.Select aria-label={`Social provider ${index + 1}`} value={social.provider} onChange={e => updateSocial(index, 'provider', e.target.value)}>
              <option value="">Select provider</option>
              <option value="instagram">Instagram</option>
              <option value="youtube">YouTube</option>
              <option value="reddit">Reddit</option>
              <option value="x">X</option>
              <option value="patreon">Patreon</option>
              </Form.Select>
            </div>
            <div className="col-12 col-md">
              <Form.Control aria-label={`Social handler ${index + 1}`} value={social.handler} onChange={e => updateSocial(index, 'handler', e.target.value)} placeholder="Handle or public identifier" />
            </div>
            <div className="col-12 col-md-auto">
              <Button type="button" variant="outline-danger" className="w-100" aria-label={`Remove social ${index + 1}`} onClick={() => setSocials(current => current.filter((_, socialIndex) => socialIndex !== index))}>Remove</Button>
            </div>
          </div>
        ))}
        <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
          <Button type="button" variant="outline-secondary" onClick={() => setSocials(current => [...current, { provider: '', handler: '' }])}>Add social</Button>
        </div>

        <h2 className="h5 mt-4 mb-3">Social publishing</h2>
        <FieldError error={errors?.socialPublishing} />
        {publishingProviders.map(provider => {
          const settings = socialPublishing[provider.key];
          return (
            <fieldset className="mb-4" key={provider.key}>
              <legend className="fs-6 fw-semibold mb-2">{provider.name}</legend>
              <div className="row g-3">
                <Form.Group className="col-12 col-md-6" controlId={`${provider.key}DestinationId`}>
                  <Form.Label>{provider.destinationLabel}</Form.Label>
                  <Form.Control
                    value={settings.destinationId}
                    onChange={event => updatePublishing(provider.key, { destinationId: event.target.value })}
                  />
                </Form.Group>
                <Form.Group className="col-12 col-md-6" controlId={`${provider.key}Credential`}>
                  <Form.Label>{provider.credentialLabel}</Form.Label>
                  <Form.Control
                    type="password"
                    autoComplete="new-password"
                    value={settings.credential}
                    placeholder={settings.credentialConfigured ? 'Configured - leave blank to keep' : ''}
                    disabled={settings.clearCredential}
                    onChange={event => updatePublishing(provider.key, {
                      credential: event.target.value,
                      clearCredential: false,
                    })}
                  />
                  {settings.credentialConfigured && (
                    <Form.Text className="text-muted">A credential is configured for this channel.</Form.Text>
                  )}
                </Form.Group>
              </div>
              {settings.credentialConfigured && (
                <Form.Check
                  className="mt-2"
                  id={`${provider.key}ClearCredential`}
                  type="checkbox"
                  label={`Remove the existing ${provider.credentialLabel.toLowerCase()}`}
                  checked={settings.clearCredential}
                  onChange={event => updatePublishing(provider.key, {
                    clearCredential: event.target.checked,
                    credential: event.target.checked ? '' : settings.credential,
                  })}
                />
              )}
            </fieldset>
          );
        })}

        <div className="d-flex justify-content-end mb-3">
          <Button variant="success" disabled={isDisabled()} type="submit">
            {isEditMode ? 'Save Changes' : 'Create'}
          </Button>
        </div>
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
