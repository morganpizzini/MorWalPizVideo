import { useState } from 'react';
import { Alert, Button, Card, Form, Table } from 'react-bootstrap';
import { Trash2 } from 'lucide-react';
import type { PushDispatch, PushTargets } from '@morwalpizvideo/models';
import {
  createPushAudience,
  deletePushAudience,
  sendPushChannel,
  sendPushPlatform,
} from '@morwalpizvideo/services';
import { useResolvedLoaderData } from '@/router/asyncData';
import PageHeader from '@components/PageHeader';
import { hasPermission, permissions } from '@/authorization/permissions';
import { useAppStore } from '@/state/appStore';
import PushComposer, { type PushMessageDraft } from './PushComposer';

/**
 * Two independent sections, matching the server authorization model: the platform composer requires
 * `push.platform.send` and targets named audiences or explicit channels; the channel section broadcasts to every
 * subscriber of the channel currently selected in the channel scope header, with no recipient picker.
 */
export default function Push(): React.ReactElement {
  const targets = useResolvedLoaderData() as PushTargets;
  const effectivePermissions = useAppStore(state => state.effectivePermissions);
  const canSendPlatform = hasPermission(effectivePermissions, [permissions.push.platformSend]);

  const [channelIds, setChannelIds] = useState<string[]>([]);
  const [audienceIds, setAudienceIds] = useState<string[]>([]);
  const [audiences, setAudiences] = useState(targets.audiences);
  const [audienceCode, setAudienceCode] = useState('');
  const [audienceName, setAudienceName] = useState('');
  const [audienceChannelIds, setAudienceChannelIds] = useState<string[]>([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<PushDispatch | null>(null);

  const allChannelsSelected =
    targets.channels.length > 0 && channelIds.length === targets.channels.length;
  const allAudiencesSelected = audiences.length > 0 && audienceIds.length === audiences.length;

  const toggle = (value: string, current: string[], set: (next: string[]) => void) =>
    set(current.includes(value) ? current.filter(item => item !== value) : [...current, value]);

  const run = async (operation: () => Promise<unknown>) => {
    setBusy(true);
    setError(null);
    try {
      await operation();
    } catch (value) {
      setError(value instanceof Error ? value.message : 'Push operation failed.');
    } finally {
      setBusy(false);
    }
  };

  const sendPlatform = (message: PushMessageDraft) =>
    void run(async () => {
      setResult(
        await sendPushPlatform({
          ...message,
          channelIds,
          audienceIds,
          allChannels: allChannelsSelected,
        })
      );
    });

  const sendChannel = (message: PushMessageDraft) =>
    void run(async () => {
      setResult(await sendPushChannel(message));
    });

  const addAudience = () =>
    void run(async () => {
      const created = await createPushAudience({
        code: audienceCode,
        name: audienceName,
        channelIds: audienceChannelIds,
        isActive: true,
      });
      setAudiences([...audiences, created]);
      setAudienceCode('');
      setAudienceName('');
      setAudienceChannelIds([]);
    });

  const removeAudience = (id: string) =>
    void run(async () => {
      await deletePushAudience(id);
      setAudiences(audiences.filter(audience => audience.id !== id));
      setAudienceIds(audienceIds.filter(audienceId => audienceId !== id));
    });

  return (
    <>
      <PageHeader title="Push notifications" />

      {error && (
        <Alert variant="danger" role="alert">
          {error}
        </Alert>
      )}
      {result && (
        <Alert variant="success" role="status">
          Queued for {result.recipientCount} subscription(s).
        </Alert>
      )}

      {canSendPlatform && (
        <Card className="mb-4">
          <Card.Body>
            <Card.Title as="h2" className="h5">
              Platform broadcast
            </Card.Title>
            <Card.Subtitle className="mb-3 text-body-secondary">
              Reaches the consenting subscribers of the selected channels and audiences at the moment of sending.
            </Card.Subtitle>

            <fieldset className="mb-3">
              <legend className="h6">Channels</legend>
              <Form.Check
                type="checkbox"
                id="push-all-channels"
                label="Select all channels"
                checked={allChannelsSelected}
                onChange={() =>
                  setChannelIds(
                    allChannelsSelected ? [] : targets.channels.map(channel => channel.channelId)
                  )
                }
              />
              <hr className="my-2" />
              {targets.channels.map(channel => (
                <Form.Check
                  key={channel.channelId}
                  type="checkbox"
                  id={`push-channel-${channel.channelId}`}
                  label={`${channel.channelName} (${channel.activeSubscriberCount})`}
                  checked={channelIds.includes(channel.channelId)}
                  onChange={() => toggle(channel.channelId, channelIds, setChannelIds)}
                />
              ))}
            </fieldset>

            <fieldset className="mb-3">
              <legend className="h6">Audiences</legend>
              {audiences.length === 0 ? (
                <p className="text-body-secondary mb-0">
                  No audiences yet. Create one below to reuse a named group of channels.
                </p>
              ) : (
                <>
                  <Form.Check
                    type="checkbox"
                    id="push-all-audiences"
                    label="Select all audiences"
                    checked={allAudiencesSelected}
                    onChange={() =>
                      setAudienceIds(
                        allAudiencesSelected ? [] : audiences.map(audience => audience.id)
                      )
                    }
                  />
                  <hr className="my-2" />
                  {audiences.map(audience => (
                    <Form.Check
                      key={audience.id}
                      type="checkbox"
                      id={`push-audience-${audience.id}`}
                      label={`${audience.name} (${audience.channelIds.length} channels)`}
                      checked={audienceIds.includes(audience.id)}
                      onChange={() => toggle(audience.id, audienceIds, setAudienceIds)}
                    />
                  ))}
                </>
              )}
            </fieldset>

            <PushComposer
              idPrefix="platform"
              maxActions={targets.maxActions}
              submitLabel="Send to platform"
              busy={busy}
              disabled={channelIds.length === 0 && audienceIds.length === 0}
              onSend={sendPlatform}
            />
          </Card.Body>
        </Card>
      )}

      <Card className="mb-4">
        <Card.Body>
          <Card.Title as="h2" className="h5">
            Channel broadcast
          </Card.Title>
          <Card.Subtitle className="mb-3 text-body-secondary">
            Reaches every consenting subscriber of the channel you are currently managing. There is no recipient
            picker by design.
          </Card.Subtitle>
          <PushComposer
            idPrefix="channel"
            maxActions={targets.maxActions}
            submitLabel="Send to my subscribers"
            busy={busy}
            onSend={sendChannel}
          />
        </Card.Body>
      </Card>

      {canSendPlatform && (
        <Card>
          <Card.Body>
            <Card.Title as="h2" className="h5">
              Audiences
            </Card.Title>
            <Card.Subtitle className="mb-3 text-body-secondary">
              Named collections of channels. They are independent from roles and permissions.
            </Card.Subtitle>
            {audiences.length > 0 && (
              <Table responsive className="align-middle">
                <thead>
                  <tr>
                    <th>Code</th>
                    <th>Name</th>
                    <th>Channels</th>
                    <th aria-label="Actions" />
                  </tr>
                </thead>
                <tbody>
                  {audiences.map(audience => (
                    <tr key={audience.id}>
                      <td>
                        <code>{audience.code}</code>
                      </td>
                      <td>{audience.name}</td>
                      <td>{audience.channelIds.length}</td>
                      <td className="text-end">
                        <Button
                          variant="outline-danger"
                          size="sm"
                          aria-label={`Delete audience ${audience.name}`}
                          disabled={busy}
                          onClick={() => removeAudience(audience.id)}
                        >
                          <Trash2 size={16} />
                        </Button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </Table>
            )}
            <div className="row g-2 align-items-end">
              <div className="col-md-3">
                <Form.Group controlId="push-audience-code">
                  <Form.Label>Code</Form.Label>
                  <Form.Control
                    value={audienceCode}
                    onChange={event => setAudienceCode(event.target.value)}
                  />
                </Form.Group>
              </div>
              <div className="col-md-4">
                <Form.Group controlId="push-audience-name">
                  <Form.Label>Name</Form.Label>
                  <Form.Control
                    value={audienceName}
                    onChange={event => setAudienceName(event.target.value)}
                  />
                </Form.Group>
              </div>
              <div className="col-md-3">
                <Form.Group controlId="push-audience-channels">
                  <Form.Label>Channels</Form.Label>
                  <Form.Select
                    multiple
                    value={audienceChannelIds}
                    onChange={event =>
                      setAudienceChannelIds(
                        [...event.target.selectedOptions].map(option => option.value)
                      )
                    }
                  >
                    {targets.channels.map(channel => (
                      <option key={channel.channelId} value={channel.channelId}>
                        {channel.channelName}
                      </option>
                    ))}
                  </Form.Select>
                </Form.Group>
              </div>
              <div className="col-md-2 d-grid">
                <Button
                  variant="outline-primary"
                  disabled={
                    busy || !audienceCode || !audienceName || audienceChannelIds.length === 0
                  }
                  onClick={addAudience}
                >
                  Create
                </Button>
              </div>
            </div>
          </Card.Body>
        </Card>
      )}
    </>
  );
}
