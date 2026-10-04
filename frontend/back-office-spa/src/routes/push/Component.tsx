import { useEffect, useState } from 'react';
import { Alert, Button, Card, Form, Table } from 'react-bootstrap';
import { Edit3, Trash2 } from 'lucide-react';
import type { PushDispatch, PushTargets, PushNotificationTemplate } from '@morwalpizvideo/models';
import {
  createPushAudience,
  deletePushAudience,
  sendPushChannel,
  sendPushPlatform,
  fetchPushNotificationTemplates,
  createPushNotificationTemplate,
  updatePushNotificationTemplate,
  deletePushNotificationTemplate,
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
  const [templates, setTemplates] = useState<PushNotificationTemplate[]>([]);
  const [templateDraft, setTemplateDraft] = useState<{
    id: string; name: string; title: string; body: string; destination: string;
    actions: PushNotificationTemplate['actions'];
  }>({ id: '', name: '', title: '', body: '', destination: '/', actions: [] });
  useEffect(() => { void fetchPushNotificationTemplates().then(setTemplates).catch(() => undefined); }, []);

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

  const saveTemplate = () => void run(async () => {
    const request = { name: templateDraft.name, title: templateDraft.title, body: templateDraft.body, destination: templateDraft.destination, actions: templateDraft.actions };
    const saved = templateDraft.id
      ? await updatePushNotificationTemplate(templateDraft.id, request)
      : await createPushNotificationTemplate(request);
    setTemplates(current => templateDraft.id ? current.map(item => item.id === saved.id ? saved : item) : [...current, saved]);
    setTemplateDraft({ id: '', name: '', title: '', body: '', destination: '/', actions: [] });
  });
  const removeTemplate = (id: string) => void run(async () => {
    await deletePushNotificationTemplate(id);
    setTemplates(current => current.filter(item => item.id !== id));
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

              <Card className="mb-4">
                <Card.Body>
                  <Card.Title as="h2" className="h5">Reusable templates</Card.Title>
                  <Table responsive className="align-middle">
                    <thead><tr><th>Name</th><th>Title</th><th>Version</th><th aria-label="Actions" /></tr></thead>
                    <tbody>{templates.map(template => <tr key={template.id}><td>{template.name}</td><td>{template.title}</td><td>{template.version}</td><td className="text-end">
                      <Button size="sm" variant="outline-secondary" aria-label={`Edit template ${template.name}`} onClick={() => setTemplateDraft({ id: template.id, name: template.name, title: template.title, body: template.body, destination: template.destination, actions: [...template.actions] })}><Edit3 size={16} /></Button>{' '}
                      <Button size="sm" variant="outline-danger" aria-label={`Delete template ${template.name}`} onClick={() => removeTemplate(template.id)}><Trash2 size={16} /></Button>
                    </td></tr>)}</tbody>
                  </Table>
                  <Form.Group className="mb-2" controlId="template-name"><Form.Label>Name</Form.Label><Form.Control value={templateDraft.name} onChange={event => setTemplateDraft({ ...templateDraft, name: event.target.value })} /></Form.Group>
                  <div className="row g-2"><div className="col-md-6"><Form.Group controlId="template-title"><Form.Label>Title</Form.Label><Form.Control value={templateDraft.title} onChange={event => setTemplateDraft({ ...templateDraft, title: event.target.value })} /></Form.Group></div><div className="col-md-6"><Form.Group controlId="template-destination"><Form.Label>Destination</Form.Label><Form.Control value={templateDraft.destination} onChange={event => setTemplateDraft({ ...templateDraft, destination: event.target.value })} /></Form.Group></div></div>
                  <Form.Group className="my-2" controlId="template-body"><Form.Label>Body</Form.Label><Form.Control as="textarea" rows={3} value={templateDraft.body} onChange={event => setTemplateDraft({ ...templateDraft, body: event.target.value })} /></Form.Group>
                  <fieldset className="mb-2">
                    <legend className="h6">Action buttons</legend>
                    {templateDraft.actions.map((action, index) => <div className="row g-2 mb-2" key={index}>
                      <div className="col-md-3"><Form.Control aria-label={`Template action ${index + 1} id`} placeholder="id" value={action.action} onChange={event => setTemplateDraft({ ...templateDraft, actions: templateDraft.actions.map((item, position) => position === index ? { ...item, action: event.target.value } : item) })} /></div>
                      <div className="col-md-4"><Form.Control aria-label={`Template action ${index + 1} label`} placeholder="Label" value={action.title} onChange={event => setTemplateDraft({ ...templateDraft, actions: templateDraft.actions.map((item, position) => position === index ? { ...item, title: event.target.value } : item) })} /></div>
                      <div className="col-md-4"><Form.Control aria-label={`Template action ${index + 1} destination`} placeholder="/path" value={action.destination} onChange={event => setTemplateDraft({ ...templateDraft, actions: templateDraft.actions.map((item, position) => position === index ? { ...item, destination: event.target.value } : item) })} /></div>
                      <div className="col-md-1 d-grid"><Button variant="outline-danger" aria-label={`Remove template action ${index + 1}`} onClick={() => setTemplateDraft({ ...templateDraft, actions: templateDraft.actions.filter((_, position) => position !== index) })}><Trash2 size={16} /></Button></div>
                    </div>)}
                    <Button size="sm" variant="outline-secondary" disabled={templateDraft.actions.length >= targets.maxActions} onClick={() => setTemplateDraft({ ...templateDraft, actions: [...templateDraft.actions, { action: '', title: '', destination: '/' }] })}>Add action</Button>
                  </fieldset>
                  <Button disabled={busy || !templateDraft.name || !templateDraft.title || !templateDraft.body} onClick={saveTemplate}>{templateDraft.id ? 'Save template' : 'Create template'}</Button>
                  {templateDraft.id && <Button variant="link" onClick={() => setTemplateDraft({ id: '', name: '', title: '', body: '', destination: '/', actions: [] })}>Cancel</Button>}
                </Card.Body>
              </Card>
            </fieldset>

            <PushComposer
              idPrefix="platform"
              maxActions={targets.maxActions}
              submitLabel="Send to platform"
              busy={busy}
              onSend={sendPlatform}
              templates={templates}
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
            templates={templates}
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
