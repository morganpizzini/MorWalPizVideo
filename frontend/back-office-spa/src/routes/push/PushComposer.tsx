import { useState } from 'react';
import { Button, Form } from 'react-bootstrap';
import { Plus, Trash2 } from 'lucide-react';
import type { PushNotificationActionRequest } from '@morwalpizvideo/models';

export interface PushMessageDraft {
  title: string;
  body: string;
  destination: string;
  actions: PushNotificationActionRequest[];
}

interface PushComposerProps {
  /** Disambiguates control ids and labels; both sections render a composer on the same page. */
  idPrefix: string;
  maxActions: number;
  submitLabel: string;
  busy: boolean;
  disabled?: boolean;
  onSend: (message: PushMessageDraft) => void;
}

/**
 * Message composition for a single push section. Each section owns its own draft so the platform and channel
 * broadcasts never share state or duplicate DOM ids.
 */
export default function PushComposer({
  idPrefix,
  maxActions,
  submitLabel,
  busy,
  disabled = false,
  onSend,
}: PushComposerProps): React.ReactElement {
  const [title, setTitle] = useState('');
  const [body, setBody] = useState('');
  const [destination, setDestination] = useState('/');
  const [actions, setActions] = useState<PushNotificationActionRequest[]>([]);

  const updateAction = (index: number, patch: Partial<PushNotificationActionRequest>) =>
    setActions(
      actions.map((action, position) => (position === index ? { ...action, ...patch } : action))
    );

  return (
    <>
      <Form.Group className="mb-3" controlId={`${idPrefix}-title`}>
        <Form.Label>Title</Form.Label>
        <Form.Control
          value={title}
          onChange={event => setTitle(event.target.value)}
          maxLength={120}
        />
      </Form.Group>
      <Form.Group className="mb-3" controlId={`${idPrefix}-body`}>
        <Form.Label>Body</Form.Label>
        <Form.Control
          as="textarea"
          rows={3}
          value={body}
          onChange={event => setBody(event.target.value)}
          maxLength={400}
        />
      </Form.Group>
      <Form.Group className="mb-3" controlId={`${idPrefix}-destination`}>
        <Form.Label>Destination</Form.Label>
        <Form.Control value={destination} onChange={event => setDestination(event.target.value)} />
        <Form.Text>
          Relative path on this site, for example /videos/latest. External URLs are rejected.
        </Form.Text>
      </Form.Group>

      <fieldset className="mb-3">
        <legend className="h6">Action buttons</legend>
        {actions.length === 0 && (
          <p className="text-body-secondary mb-2">
            No buttons. Tapping the notification opens the destination above.
          </p>
        )}
        {actions.map((action, index) => (
          <div className="row g-2 mb-2" key={index}>
            <div className="col-md-3">
              <Form.Control
                aria-label={`${idPrefix} action ${index + 1} id`}
                placeholder="id"
                value={action.action}
                onChange={event => updateAction(index, { action: event.target.value })}
              />
            </div>
            <div className="col-md-4">
              <Form.Control
                aria-label={`${idPrefix} action ${index + 1} label`}
                placeholder="Label"
                value={action.title}
                onChange={event => updateAction(index, { title: event.target.value })}
              />
            </div>
            <div className="col-md-4">
              <Form.Control
                aria-label={`${idPrefix} action ${index + 1} destination`}
                placeholder="/path"
                value={action.destination}
                onChange={event => updateAction(index, { destination: event.target.value })}
              />
            </div>
            <div className="col-md-1 d-grid">
              <Button
                variant="outline-danger"
                aria-label={`Remove ${idPrefix} action ${index + 1}`}
                onClick={() => setActions(actions.filter((_, position) => position !== index))}
              >
                <Trash2 size={16} />
              </Button>
            </div>
          </div>
        ))}
        <Button
          variant="outline-secondary"
          size="sm"
          disabled={actions.length >= maxActions}
          onClick={() =>
            setActions([...actions, { action: '', title: '', destination: '/' }])
          }
        >
          <Plus size={16} className="me-1" />
          Add {idPrefix} action
        </Button>
        <Form.Text className="d-block">
          At most {maxActions} buttons; browsers may display fewer.
        </Form.Text>
      </fieldset>

      <Button
        disabled={busy || disabled || !title || !body}
        onClick={() =>
          onSend({
            title,
            body,
            destination,
            actions: actions.filter(action => action.action && action.title),
          })
        }
      >
        {busy ? 'Sending…' : submitLabel}
      </Button>
    </>
  );
}
