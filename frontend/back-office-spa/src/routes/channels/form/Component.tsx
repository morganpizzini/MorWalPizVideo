import { useResolvedLoaderData } from '@/router/asyncData';
import React, { useState, useEffect, useRef } from 'react';
import { Form, Button, Modal } from 'react-bootstrap';
import { useNavigate, useFetcher } from 'react-router';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';
import { useToast } from '@components/ToastNotification/ToastContext';
import { Channel, ChannelSocial } from '@morwalpizvideo/models';
import PageHeader from '@components/PageHeader';
import {
  hasCacheInvalidationWarning,
  type ChannelActionError,
  type ChannelMutationResult,
} from '../response';

type ChannelFormActionResult = ChannelActionError | (ChannelMutationResult & { success: true });

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
  {
    key: 'facebook',
    name: 'Facebook',
    destinationLabel: 'Page ID',
    credentialLabel: 'Access token',
  },
];

const emptyPublishingProvider = (): EditablePublishingProvider => ({
  destinationId: '',
  credential: '',
  credentialConfigured: false,
  clearCredential: false,
});

const ChannelForm: React.FC = () => {
  const entity = useResolvedLoaderData() as Channel | null;
  const isEditMode = entity !== null;

  const [channelName, setChannelName] = useState(entity?.channelName || '');
  const [yTChannelId, setYTChannelId] = useState(entity?.yTChannelId ?? '');
  const [isSHIT, setIsSHIT] = useState(false);
  const [shortLinkUrl, setShortLinkUrl] = useState('');
  const [publicBaseUrl, setPublicBaseUrl] = useState('');
  const [googleAnalyticsUrl, setGoogleAnalyticsUrl] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [socials, setSocials] = useState<ChannelSocial[]>([]);
  const [socialPublishing, setSocialPublishing] = useState<
    Record<PublishingProviderKey, EditablePublishingProvider>
  >({
    telegram: emptyPublishingProvider(),
    discord: emptyPublishingProvider(),
    facebook: emptyPublishingProvider(),
  });

  const navigate = useNavigate();
  const toast = useToast();
  const fetcher = useFetcher<ChannelFormActionResult>();
  const busy = fetcher.state !== 'idle';
  const lastResult = useRef<unknown>(undefined);
  const formRef = useRef<HTMLFormElement>(null);
  const errorSummaryRef = useRef<HTMLDivElement>(null);
  const focusAfterModalClose = useRef(false);
  const actionResult = fetcher.data;
  const errors = actionResult?.success === false ? actionResult.errors : undefined;
  const result = actionResult?.success === true ? actionResult : null;

  useEffect(() => {
    if (entity) {
      setChannelName(entity.channelName);
      setYTChannelId(entity.yTChannelId ?? '');
      setIsSHIT(entity.isSHIT ?? false);
      setShortLinkUrl(entity.shortLinkUrl ?? '');
      setPublicBaseUrl(entity.publicBaseUrl ?? '');
      setGoogleAnalyticsUrl(entity.googleAnalyticsUrl ?? '');
      setSocials(
        entity.socials?.map(social => ({ provider: social.provider, handler: social.handler })) ??
          []
      );
      setSocialPublishing({
        telegram: { ...emptyPublishingProvider(), ...entity.socialPublishing?.telegram },
        discord: { ...emptyPublishingProvider(), ...entity.socialPublishing?.discord },
        facebook: { ...emptyPublishingProvider(), ...entity.socialPublishing?.facebook },
      });
    }
  }, [entity]);

  const focusFirstError = () => {
    const invalidControl = formRef.current?.querySelector<HTMLElement>(
      'input[aria-invalid="true"], select[aria-invalid="true"], textarea[aria-invalid="true"]'
    );
    const invalidSection = formRef.current?.querySelector<HTMLElement>(
      '[role="group"][aria-invalid="true"], fieldset[aria-invalid="true"]'
    );
    (invalidControl ?? invalidSection ?? errorSummaryRef.current)?.focus();
  };

  useEffect(() => {
    if (busy || !actionResult || lastResult.current === actionResult) return;
    lastResult.current = actionResult;
    setShowModal(false);
    if (actionResult.success === false) {
      if (showModal) {
        focusAfterModalClose.current = true;
      } else {
        focusFirstError();
      }
      return;
    }

    if (actionResult.success === true) {
      toast.show(
        'Success',
        isEditMode ? 'Channel updated successfully' : 'Channel created successfully',
        { variant: 'success' }
      );
      if (hasCacheInvalidationWarning(result)) {
        toast.show(
          'Warning',
          result.cacheInvalidation?.message ??
            'The operation completed, but the public cache was not reset.',
          { variant: 'warning' }
        );
      }
      navigate('..');
    }
  }, [busy, actionResult, navigate, isEditMode, toast, showModal]);

  const isDisabled = () => {
    if (!channelName || channelName.trim().length === 0 || busy) return true;
    if (!yTChannelId || yTChannelId.trim().length === 0) return true;
    return false;
  };

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    setShowModal(true);
  };

  const confirmSubmit = () => {
    const payload: Record<string, string> = { channelName, isSHIT: String(isSHIT) };
    payload.yTChannelId = yTChannelId;
    fetcher.submit(
      {
        ...payload,
        shortLinkUrl,
        publicBaseUrl,
        googleAnalyticsUrl,
        socials: JSON.stringify(socials),
        socialPublishing: JSON.stringify(socialPublishing),
      },
      { method: 'post', action: location.pathname }
    );
  };

  const updateSocial = (index: number, field: keyof ChannelSocial, value: string) => {
    setSocials(current =>
      current.map((social, socialIndex) =>
        socialIndex === index ? { ...social, [field]: value } : social
      )
    );
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
      <div
        ref={errorSummaryRef}
        id="channel-form-errors"
        tabIndex={-1}
        aria-label="Channel form errors"
      >
        <GenericErrorList errors={errors?.generics} />
      </div>
      <Form ref={formRef} onSubmit={handleSubmit}>
        <Form.Group controlId="formChannelName" className="mb-3">
          <Form.Label>
            Channel Name <span className="text-danger">*</span>
          </Form.Label>
          <Form.Control
            id="formChannelName"
            type="text"
            value={channelName}
            onChange={e => setChannelName(e.target.value)}
            aria-invalid={Boolean(errors?.channelName)}
            aria-describedby={errors?.channelName ? 'channelNameError' : undefined}
            isInvalid={Boolean(errors?.channelName)}
          />
          <div id="channelNameError">
            <FieldError error={errors?.channelName} />
          </div>
        </Form.Group>

        <Form.Group controlId="formYTChannelId" className="mb-3">
          <Form.Label>
            YouTube Channel ID <span className="text-danger">*</span>
          </Form.Label>
          <Form.Control
            id="formYTChannelId"
            type="text"
            value={yTChannelId}
            onChange={e => setYTChannelId(e.target.value)}
            aria-invalid={Boolean(errors?.yTChannelId)}
            aria-describedby={errors?.yTChannelId ? 'ytChannelIdError' : undefined}
            isInvalid={Boolean(errors?.yTChannelId)}
          />
          <div id="ytChannelIdError">
            <FieldError error={errors?.yTChannelId} />
          </div>
        </Form.Group>

        <Form.Group controlId="formPublicBaseUrl" className="mb-3">
          <Form.Label>Public website base URL</Form.Label>
          <Form.Control
            type="url"
            value={publicBaseUrl}
            onChange={e => setPublicBaseUrl(e.target.value)}
            placeholder="https://example.com"
            aria-describedby="publicBaseUrlHelp publicBaseUrlError"
            aria-invalid={Boolean(errors?.publicBaseUrl)}
            isInvalid={Boolean(errors?.publicBaseUrl)}
          />
          <Form.Text id="publicBaseUrlHelp">
            Optional public website origin for QuickLinks. HTTPS recommended; no path.
            This is separate from the short link prefix.
          </Form.Text>
          <div id="publicBaseUrlError">
            <FieldError error={errors?.publicBaseUrl} />
          </div>
        </Form.Group>

        <Form.Group controlId="formShortLinkUrl" className="mb-3">
          <Form.Label>Short link base URL</Form.Label>
          <Form.Control
            id="formShortLinkUrl"
            type="url"
            value={shortLinkUrl}
            onChange={e => setShortLinkUrl(e.target.value)}
            placeholder="https://example.com/sl"
            aria-invalid={Boolean(errors?.shortLinkUrl)}
            aria-describedby={errors?.shortLinkUrl ? 'shortLinkUrlError' : undefined}
            isInvalid={Boolean(errors?.shortLinkUrl)}
          />
          <div id="shortLinkUrlError">
            <FieldError error={errors?.shortLinkUrl} />
          </div>
        </Form.Group>

        <Form.Group controlId="formGoogleAnalyticsUrl" className="mb-3">
          <Form.Label>Google Analytics link</Form.Label>
          <Form.Control
            id="formGoogleAnalyticsUrl"
            type="url"
            value={googleAnalyticsUrl}
            onChange={e => setGoogleAnalyticsUrl(e.target.value)}
            placeholder="https://analytics.google.com/..."
            aria-invalid={Boolean(errors?.googleAnalyticsUrl)}
            aria-describedby={
              errors?.googleAnalyticsUrl
                ? 'googleAnalyticsUrlHelp googleAnalyticsUrlError'
                : 'googleAnalyticsUrlHelp'
            }
            isInvalid={Boolean(errors?.googleAnalyticsUrl)}
          />
          <Form.Text id="googleAnalyticsUrlHelp">
            Optional HTTPS link opened from the BackOffice channel context.
          </Form.Text>
          <div id="googleAnalyticsUrlError">
            <FieldError error={errors?.googleAnalyticsUrl} />
          </div>
        </Form.Group>

        <Form.Check
          type="checkbox"
          id="formIsSHIT"
          label="Shooting ITA channel"
          checked={isSHIT}
          onChange={event => setIsSHIT(event.target.checked)}
          className="mb-3"
        />

        <Form.Label id="socialsLabel">Socials</Form.Label>
        <div
          role="group"
          aria-labelledby="socialsLabel"
          aria-describedby={errors?.socials ? 'socialsError' : undefined}
          aria-invalid={Boolean(errors?.socials)}
          tabIndex={errors?.socials ? -1 : undefined}
        >
        {socials.map((social, index) => (
          <div className="row g-2 align-items-center mb-2" key={`${social.provider}-${index}`}>
            <div className="col-12 col-md-3">
              <Form.Select
                aria-label={`Social provider ${index + 1}`}
                value={social.provider}
                onChange={e => updateSocial(index, 'provider', e.target.value)}
                aria-invalid={Boolean(errors?.socials)}
                aria-describedby={errors?.socials ? 'socialsError' : undefined}
              >
                <option value="">Select provider</option>
                <option value="instagram">Instagram</option>
                <option value="youtube">YouTube</option>
                <option value="reddit">Reddit</option>
                <option value="x">X</option>
                <option value="patreon">Patreon</option>
                <option value="facebook">Facebook</option>
                <option value="telegram">Telegram</option>
                <option value="discord">Discord</option>
              </Form.Select>
            </div>
            <div className="col-12 col-md">
              <Form.Control
                aria-label={`Social handler ${index + 1}`}
                value={social.handler}
                onChange={e => updateSocial(index, 'handler', e.target.value)}
                placeholder="Handle or public identifier"
                aria-invalid={Boolean(errors?.socials)}
                aria-describedby={errors?.socials ? 'socialsError' : undefined}
              />
            </div>
            <div className="col-12 col-md-auto">
              <Button
                type="button"
                variant="outline-danger"
                className="w-100"
                aria-label={`Remove social ${index + 1}`}
                onClick={() =>
                  setSocials(current => current.filter((_, socialIndex) => socialIndex !== index))
                }
              >
                Remove
              </Button>
            </div>
          </div>
        ))}
        <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
          <Button
            type="button"
            variant="outline-secondary"
            onClick={() => setSocials(current => [...current, { provider: '', handler: '' }])}
          >
            Add social
          </Button>
        </div>
        <div id="socialsError">
          <FieldError error={errors?.socials} />
        </div>
        </div>

        <h2 className="h5 mt-4 mb-3">Social publishing</h2>
        <div id="socialPublishingError">
          <FieldError error={errors?.socialPublishing} />
        </div>
        {publishingProviders.map(provider => {
          const settings = socialPublishing[provider.key];
          return (
            <fieldset
              className={`mb-4${errors?.socialPublishing ? ' border border-danger rounded p-3' : ''}`}
              key={provider.key}
              aria-invalid={Boolean(errors?.socialPublishing)}
              aria-describedby={errors?.socialPublishing ? 'socialPublishingError' : undefined}
              tabIndex={errors?.socialPublishing ? -1 : undefined}
            >
              <legend className="fs-6 fw-semibold mb-2">{provider.name}</legend>
              <div className="row g-3">
                <Form.Group className="col-12 col-md-6" controlId={`${provider.key}DestinationId`}>
                  <Form.Label>{provider.destinationLabel}</Form.Label>
                  <Form.Control
                    value={settings.destinationId}
                    onChange={event =>
                      updatePublishing(provider.key, { destinationId: event.target.value })
                    }
                    aria-invalid={Boolean(errors?.socialPublishing)}
                    aria-describedby={
                      errors?.socialPublishing ? 'socialPublishingError' : undefined
                    }
                  />
                </Form.Group>
                <Form.Group className="col-12 col-md-6" controlId={`${provider.key}Credential`}>
                  <Form.Label>{provider.credentialLabel}</Form.Label>
                  <Form.Control
                    type="password"
                    autoComplete="new-password"
                    value={settings.credential}
                    placeholder={
                      settings.credentialConfigured ? 'Configured - leave blank to keep' : ''
                    }
                    disabled={settings.clearCredential}
                    onChange={event =>
                      updatePublishing(provider.key, {
                        credential: event.target.value,
                        clearCredential: false,
                      })
                    }
                    aria-invalid={Boolean(errors?.socialPublishing)}
                    aria-describedby={
                      errors?.socialPublishing ? 'socialPublishingError' : undefined
                    }
                  />
                  {settings.credentialConfigured && (
                    <Form.Text className="text-muted">
                      A credential is configured for this channel.
                    </Form.Text>
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
                  onChange={event =>
                    updatePublishing(provider.key, {
                      clearCredential: event.target.checked,
                      credential: event.target.checked ? '' : settings.credential,
                    })
                  }
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

      <Modal
        show={showModal}
        onHide={() => setShowModal(false)}
        onExited={() => {
          if (focusAfterModalClose.current) {
            focusAfterModalClose.current = false;
            focusFirstError();
          }
        }}
      >
        <Modal.Header closeButton>
          <Modal.Title>{isEditMode ? 'Confirm Edit' : 'Confirm Create'}</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <p>
            Are you sure you want to {isEditMode ? 'save the changes to' : 'create'} the following
            channel?
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
          <p className="text-break">
            <strong>YouTube Channel ID:</strong>{' '}
            {isEditMode && yTChannelId.trim() !== entity?.yTChannelId ? (
              <>
                <s>{entity?.yTChannelId}</s> -&gt; {yTChannelId}
              </>
            ) : (
              yTChannelId
            )}
          </p>
          {isEditMode && yTChannelId.trim() !== entity?.yTChannelId && (
            <p>
              Changing this ID affects future YouTube scans only. It does not move channel
              ownership, existing content or history, or upload OAuth credentials.
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
