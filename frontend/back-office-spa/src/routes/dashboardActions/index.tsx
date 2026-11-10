import { useEffect, useRef, useState } from 'react';
import { Alert, Button, Form, Spinner } from 'react-bootstrap';
import { ArrowDown, ArrowUp, Plus, Trash2 } from 'lucide-react';
import { Link, useFetcher, type ActionFunctionArgs } from 'react-router';
import type {
  QuickActionEntry,
  QuickActionsResponse,
  SaveQuickActionsRequest,
} from '@morwalpizvideo/models';
import {
  getSelectedChannelId,
  resetDashboardQuickActions,
  saveDashboardQuickActions,
} from '@morwalpizvideo/services';
import { useChannelContext } from '../../contexts/ChannelContext';
import { useDashboardQuickActions } from '../../hooks/useDashboardQuickActions';
import { authService } from '../../services/authService';

interface ActionResult {
  scope: string;
  response?: QuickActionsResponse;
  error?: string;
}

export async function Loader() {
  return null;
}

export async function Action({ request }: ActionFunctionArgs): Promise<ActionResult> {
  const form = await request.formData();
  const scope = String(form.get('scope') ?? '');
  const session = await authService.validateSession();
  if (
    !session ||
    form.get('userId') !== session.userId ||
    form.get('channelId') !== getSelectedChannelId()
  )
    return { scope, error: 'Il contesto è cambiato. Ricarica le attività prima di salvare.' };
  try {
    const response =
      form.get('intent') === 'reset'
        ? await resetDashboardQuickActions()
        : await saveDashboardQuickActions(
            JSON.parse(String(form.get('payload'))) as SaveQuickActionsRequest
          );
    return { scope, response };
  } catch (error) {
    return {
      scope,
      error:
        error instanceof Error ? error.message : 'Impossibile salvare le attività rapide. Riprova.',
    };
  }
}

export function Component() {
  const resource = useDashboardQuickActions();
  const { channels, setChannelSwitchLocked } = useChannelContext();
  const fetcher = useFetcher<ActionResult>();
  const [draft, setDraft] = useState<{ scope: string; actions: QuickActionEntry[] }>({
    scope: '',
    actions: [],
  });
  const [baseline, setBaseline] = useState<QuickActionEntry[]>([]);
  const [notice, setNotice] = useState('');
  const [feedback, setFeedback] = useState<{
    scope: string;
    error: boolean;
    message: string;
  } | null>(null);
  const previousScope = useRef(resource.scope);
  const dirtyRef = useRef(false);
  const pendingRef = useRef(false);
  const focusRefs = useRef(new Map<string, HTMLButtonElement>());
  const pending = fetcher.state !== 'idle';
  const ready = resource.available && resource.state.status === 'ready';
  const data = resource.state.status === 'ready' ? resource.state.data : null;
  const inScope = draft.scope === resource.scope;
  const actions = inScope ? draft.actions : [];
  const dirty = inScope && JSON.stringify(actions) !== JSON.stringify(baseline);
  if (inScope) dirtyRef.current = dirty;

  useEffect(() => {
    if (previousScope.current !== resource.scope) {
      if (dirtyRef.current)
        setNotice('Contesto cambiato: le modifiche non salvate sono state scartate.');
      previousScope.current = resource.scope;
      dirtyRef.current = false;
      setFeedback(null);
    }
    if (resource.state.status === 'ready' && !dirtyRef.current) {
      setDraft({ scope: resource.scope, actions: resource.state.data.actions });
      setBaseline(resource.state.data.actions);
    }
  }, [resource.scope, resource.state]);

  useEffect(() => {
    if (fetcher.state !== 'idle') return;
    pendingRef.current = false;
    setChannelSwitchLocked(false);
    const result = fetcher.data;
    if (!result || result.scope !== resource.scope) return;
    if (result.response) {
      setDraft({ scope: resource.scope, actions: result.response.actions });
      setBaseline(result.response.actions);
      setFeedback({ scope: resource.scope, error: false, message: 'Attività rapide salvate.' });
    } else if (result.error) {
      setFeedback({ scope: resource.scope, error: true, message: result.error });
    }
  }, [fetcher.state, fetcher.data, resource.scope, setChannelSwitchLocked]);

  useEffect(() => () => setChannelSwitchLocked(false), [setChannelSwitchLocked]);

  const update = (next: QuickActionEntry[]) => setDraft({ scope: resource.scope, actions: next });
  const submit = (intent: 'save' | 'reset') => {
    if (!ready || !data?.canEdit || pendingRef.current) return;
    if (
      intent === 'reset' &&
      !window.confirm('Ripristinare le attività predefinite per questo canale?')
    )
      return;
    pendingRef.current = true;
    setChannelSwitchLocked(true);
    setFeedback(null);
    void fetcher.submit(
      {
        intent,
        scope: resource.scope,
        userId: resource.userId ?? '',
        channelId: resource.channelId ?? '',
        payload: JSON.stringify({ actions }),
      },
      { method: 'post' }
    );
  };
  const move = (index: number, offset: number) => {
    const next = [...actions];
    [next[index], next[index + offset]] = [next[index + offset], next[index]];
    update(next);
    setNotice('Ordine aggiornato. Salva per confermare.');
    focusRefs.current.get(next[index + offset].id)?.focus();
  };
  const remove = (index: number) => {
    const next = actions.filter((_, position) => position !== index);
    update(next);
    setNotice('Attività rimossa. Salva per confermare.');
    const focusId = next[Math.min(index, next.length - 1)]?.id;
    if (focusId) focusRefs.current.get(focusId)?.focus();
    else document.getElementById('add-custom-action')?.focus();
  };

  return (
    <div className="quick-actions-editor">
      <div className="d-flex flex-wrap justify-content-between align-items-start gap-3 mb-4">
        <div>
          <h1 className="h3 mb-1">Attività rapide</h1>
          <p className="text-muted mb-1">
            Scegli i collegamenti della tua dashboard, nell'ordine che preferisci.
          </p>
          <p className="mb-0">
            Preferenze personali per{' '}
            <strong>
              {channels.find(channel => channel.channelId === resource.channelId)?.channelName ??
                'il canale selezionato'}
            </strong>
            .
          </p>
        </div>
        <Link to="/" className="btn btn-outline-secondary">
          Torna alla dashboard
        </Link>
      </div>
      {notice ? (
        <p role="status" className="text-muted">
          {notice}
        </p>
      ) : null}
      {feedback?.scope === resource.scope ? (
        <Alert
          variant={feedback.error ? 'danger' : 'success'}
          role={feedback.error ? 'alert' : 'status'}
        >
          {feedback.message}
        </Alert>
      ) : null}
      {!resource.available ? (
        <Alert variant="secondary">
          Seleziona un canale accessibile per configurare le attività rapide.
        </Alert>
      ) : resource.state.status === 'loading' || (resource.state.status === 'ready' && !inScope) ? (
        <div role="status">
          <Spinner animation="border" size="sm" /> Caricamento attività rapide…
        </div>
      ) : resource.state.status === 'error' ? (
        <Alert variant="danger">
          {resource.state.message}{' '}
          <Button variant="outline-danger" size="sm" onClick={resource.reload}>
            Riprova
          </Button>
        </Alert>
      ) : data ? (
        <>
          {!data.canEdit ? (
            <Alert variant="secondary">
              Le preferenze sono in sola lettura durante l'impersonazione.
            </Alert>
          ) : null}
          <Form
            onSubmit={event => {
              event.preventDefault();
              submit('save');
            }}
          >
            <fieldset disabled={pending || !data.canEdit}>
              <section aria-labelledby="available-actions-title" className="mb-4">
                <h2 id="available-actions-title" className="h5">
                  Azioni predefinite
                </h2>
                <p className="text-muted">
                  Sono disponibili solo le azioni consentite dai tuoi permessi.
                </p>
                <div className="d-flex flex-wrap gap-2">
                  {data.availableActions.map(definition => (
                    <Button
                      key={definition.key}
                      type="button"
                      variant="outline-primary"
                      size="sm"
                      disabled={actions.some(
                        action =>
                          action.kind === 'predefined' && action.actionKey === definition.key
                      )}
                      onClick={() =>
                        update([
                          ...actions,
                          {
                            id: crypto.randomUUID(),
                            kind: 'predefined',
                            actionKey: definition.key,
                          },
                        ])
                      }
                    >
                      <Plus size={16} aria-hidden="true" /> {definition.label}
                    </Button>
                  ))}
                </div>
                {data.availableActions.length === 0 ? (
                  <p className="text-muted">
                    Nessuna azione predefinita disponibile. Puoi aggiungere un link personalizzato.
                  </p>
                ) : null}
              </section>
              <section aria-labelledby="selected-actions-title" className="mb-4">
                <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
                  <h2 id="selected-actions-title" className="h5 mb-0">
                    La tua selezione
                  </h2>
                  <Button
                    id="add-custom-action"
                    type="button"
                    variant="outline-primary"
                    onClick={() =>
                      update([
                        ...actions,
                        { id: crypto.randomUUID(), kind: 'custom', label: '', url: '' },
                      ])
                    }
                  >
                    <Plus size={16} aria-hidden="true" /> Aggiungi link personalizzato
                  </Button>
                </div>
                {actions.length === 0 ? (
                  <p className="text-muted">
                    Nessuna attività selezionata. Salvando, la dashboard non mostrerà pulsanti
                    rapidi.
                  </p>
                ) : null}
                <ol className="quick-actions-list list-unstyled">
                  {actions.map((action, index) => {
                    const definition =
                      action.kind === 'predefined'
                        ? data.availableActions.find(item => item.key === action.actionKey)
                        : undefined;
                    return (
                      <li key={action.id} className="quick-action-row">
                        <div className="quick-action-fields">
                          {action.kind === 'custom' ? (
                            <>
                              <Form.Group controlId={`label-${action.id}`}>
                                <Form.Label>Etichetta</Form.Label>
                                <Form.Control
                                  type="text"
                                  value={action.label}
                                  onChange={event =>
                                    update(
                                      actions.map(item =>
                                        item.id === action.id
                                          ? { ...action, label: event.target.value }
                                          : item
                                      )
                                    )
                                  }
                                />
                              </Form.Group>
                              <Form.Group controlId={`url-${action.id}`}>
                                <Form.Label>URL</Form.Label>
                                <Form.Control
                                  type="text"
                                  value={action.url}
                                  aria-describedby="custom-link-help"
                                  onChange={event =>
                                    update(
                                      actions.map(item =>
                                        item.id === action.id
                                          ? { ...action, url: event.target.value }
                                          : item
                                      )
                                    )
                                  }
                                />
                              </Form.Group>
                            </>
                          ) : (
                            <>
                              <strong>{definition?.label ?? action.actionKey}</strong>
                              <p className="text-muted mb-0">
                                {definition?.url ??
                                  'Non disponibile con i permessi attuali o ritirata dal catalogo.'}
                              </p>
                            </>
                          )}
                        </div>
                        <div
                          className="d-flex flex-wrap gap-2"
                          role="group"
                          aria-label={`Controlli attività ${index + 1}`}
                        >
                          <Button
                            type="button"
                            variant="outline-secondary"
                            disabled={index === 0}
                            aria-label={`Sposta su attività ${index + 1}`}
                            onClick={() => move(index, -1)}
                          >
                            <ArrowUp size={17} aria-hidden="true" />
                          </Button>
                          <Button
                            type="button"
                            variant="outline-secondary"
                            disabled={index === actions.length - 1}
                            aria-label={`Sposta giù attività ${index + 1}`}
                            onClick={() => move(index, 1)}
                          >
                            <ArrowDown size={17} aria-hidden="true" />
                          </Button>
                          <Button
                            ref={(node: HTMLButtonElement | null) => {
                              if (node) focusRefs.current.set(action.id, node);
                              else focusRefs.current.delete(action.id);
                            }}
                            type="button"
                            variant="outline-danger"
                            aria-label={`Rimuovi attività ${index + 1}`}
                            onClick={() => remove(index)}
                          >
                            <Trash2 size={17} aria-hidden="true" />
                          </Button>
                        </div>
                      </li>
                    );
                  })}
                </ol>
                <p id="custom-link-help" className="text-muted small">
                  Etichetta e URL dei link personalizzati vengono salvati esattamente come inseriti,
                  senza controlli sulla destinazione. I collegamenti si aprono nella stessa scheda.
                </p>
              </section>
              <div className="d-flex flex-wrap gap-2">
                <Button type="submit" disabled={!dirty}>
                  {pending ? 'Salvataggio…' : 'Salva attività'}
                </Button>
                <Button
                  type="button"
                  variant="outline-secondary"
                  disabled={!dirty}
                  onClick={() => {
                    update(baseline);
                    setFeedback(null);
                    setNotice('Modifiche annullate.');
                  }}
                >
                  Annulla modifiche
                </Button>
                <Button type="button" variant="outline-secondary" onClick={() => submit('reset')}>
                  Ripristina predefinite
                </Button>
              </div>
            </fieldset>
          </Form>
        </>
      ) : null}
    </div>
  );
}
