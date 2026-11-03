import { useResolvedLoaderData } from '@/router/asyncData';
import { useAppStore } from '@/state/appStore';
import { useToast } from '@components/ToastNotification/ToastContext';
import type {
  AiExecutionRecord,
  AiPromptDefinition,
  AiPromptExperiment,
  AiPromptMessage,
  AiPromptRevision,
} from '@morwalpizvideo/models';
import {
  bootstrapAiPrompts,
  cancelAiPromptCanary,
  createAiEvaluationCase,
  createAiPromptCandidate,
  createAiPromptRevision,
  evaluateAiPrompt,
  getAiExecutionRecords,
  getAiPromptDefinitions,
  getAiPromptExperiments,
  getAiPromptRevisions,
  promoteAiPromptCanary,
  rollbackAiPrompt,
  startAiPromptCanary,
} from '@morwalpizvideo/services';
import {
  CheckCircle2,
  CircleHelp,
  GitBranch,
  History,
  RotateCcw,
  Save,
  ShieldCheck,
  Sparkles,
} from 'lucide-react';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { Alert, Badge, Button, Col, Form, Modal, Row, Spinner, Table } from 'react-bootstrap';
import { hasPermission, permissions } from '../../authorization/permissions';

type ConfirmAction = 'promote' | 'cancel-canary' | 'rollback' | null;
type WorkflowData = {
  revisions: AiPromptRevision[];
  experiments: AiPromptExperiment[];
  executions: AiExecutionRecord[];
};

const emptyData: WorkflowData = { revisions: [], experiments: [], executions: [] };
const defaultCaseInput = JSON.stringify(
  { transcript: 'IPSC match stage briefing and competitor run', context: 'Sport shooting' },
  null,
  2
);

function lifecycleVariant(lifecycle: string): 'success' | 'warning' | 'secondary' | 'danger' {
  if (lifecycle === 'Active') return 'success';
  if (lifecycle === 'Canary' || lifecycle === 'Draft') return 'warning';
  if (lifecycle === 'Rejected') return 'danger';
  return 'secondary';
}

export default function AiWorkflowsPage() {
  const initialDefinitions = useResolvedLoaderData() as AiPromptDefinition[];
  const effectivePermissions = useAppStore(state => state.effectivePermissions);
  const toast = useToast();
  const canEdit = hasPermission(effectivePermissions, [permissions.aiPrompts.edit]);
  const canEvaluate = hasPermission(effectivePermissions, [permissions.aiPrompts.evaluate]);
  const canActivate = hasPermission(effectivePermissions, [permissions.aiPrompts.activate]);
  const [definitions, setDefinitions] = useState(initialDefinitions);
  const [selectedKey, setSelectedKey] = useState('');
  const [data, setData] = useState<WorkflowData>(emptyData);
  const [selectedRevisionId, setSelectedRevisionId] = useState('');
  const [messages, setMessages] = useState<AiPromptMessage[]>([]);
  const [rationale, setRationale] = useState('');
  const [datasetVersion, setDatasetVersion] = useState('reviewed-v1');
  const [caseInput, setCaseInput] = useState(defaultCaseInput);
  const [expectedContains, setExpectedContains] = useState('');
  const [caseSource, setCaseSource] = useState('curated');
  const [consentGranted, setConsentGranted] = useState(false);
  const [contentRedacted, setContentRedacted] = useState(false);
  const [canaryPercentage, setCanaryPercentage] = useState(10);
  const [busy, setBusy] = useState(false);
  const [loadingDetails, setLoadingDetails] = useState(false);
  const [error, setError] = useState('');
  const [confirmAction, setConfirmAction] = useState<ConfirmAction>(null);

  const selectedDefinition = useMemo(
    () => definitions.find(item => `${item.workflowKey}/${item.stageKey}` === selectedKey) ?? null,
    [definitions, selectedKey]
  );
  const selectedRevision = useMemo(
    () => data.revisions.find(item => item.id === selectedRevisionId) ?? null,
    [data.revisions, selectedRevisionId]
  );
  const latestPassingExperiment = useMemo(
    () =>
      data.experiments.find(
        item =>
          item.status === 'Passed' &&
          item.candidateRevisionId === selectedRevisionId &&
          item.baselineRevisionId === selectedDefinition?.activeRevisionId
      ) ?? null,
    [data.experiments, selectedDefinition?.activeRevisionId, selectedRevisionId]
  );

  const refreshDefinitions = useCallback(async () => {
    const updated = await getAiPromptDefinitions();
    setDefinitions(updated);
    return updated;
  }, []);

  const refreshDetails = useCallback(async (definition: AiPromptDefinition) => {
    setLoadingDetails(true);
    setError('');
    try {
      const [revisions, experiments, executions] = await Promise.all([
        getAiPromptRevisions(definition.workflowKey, definition.stageKey),
        getAiPromptExperiments(definition.workflowKey, definition.stageKey),
        getAiExecutionRecords(definition.workflowKey, definition.stageKey),
      ]);
      setData({ revisions, experiments, executions });
      setSelectedRevisionId(current =>
        revisions.some(item => item.id === current)
          ? current
          : (definition.canaryRevisionId ?? definition.activeRevisionId ?? revisions[0]?.id ?? '')
      );
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Workflow details could not be loaded.');
    } finally {
      setLoadingDetails(false);
    }
  }, []);

  useEffect(() => {
    if (!selectedDefinition) {
      setData(emptyData);
      setSelectedRevisionId('');
      setMessages([]);
      return;
    }
    void refreshDetails(selectedDefinition);
  }, [refreshDetails, selectedDefinition]);

  useEffect(() => {
    if (!selectedRevision) {
      setMessages([]);
      return;
    }
    setMessages(selectedRevision.messages.map(message => ({ ...message })));
    setRationale('');
  }, [selectedRevision]);

  const runAction = async (label: string, action: () => Promise<unknown>) => {
    setBusy(true);
    setError('');
    try {
      await action();
      toast.show('AI workflows', label, { variant: 'success' });
      const updatedDefinitions = await refreshDefinitions();
      const refreshedDefinition =
        updatedDefinitions.find(item => `${item.workflowKey}/${item.stageKey}` === selectedKey) ??
        selectedDefinition;
      if (refreshedDefinition) await refreshDetails(refreshedDefinition);
    } catch (reason) {
      const message = reason instanceof Error ? reason.message : `${label} failed.`;
      setError(message);
      toast.show('AI workflows', message, { variant: 'danger' });
    } finally {
      setBusy(false);
    }
  };

  const saveDraft = async () => {
    if (!selectedDefinition) return;
    await runAction('Draft revision saved.', () =>
      createAiPromptRevision(selectedDefinition.workflowKey, selectedDefinition.stageKey, {
        messages,
        variables: selectedRevision?.variables ?? ['payload'],
        responseSchema: selectedRevision?.responseSchema ?? selectedDefinition.responseSchema,
        allowedToolNames: selectedRevision?.allowedToolNames ?? [],
        rationale,
      })
    );
  };

  const addEvaluationCase = async () => {
    if (!selectedDefinition) return;
    await runAction('Evaluation case added.', () =>
      createAiEvaluationCase(selectedDefinition.workflowKey, selectedDefinition.stageKey, {
        datasetVersion,
        inputJson: caseInput,
        expectedContains: expectedContains
          .split(',')
          .map(item => item.trim())
          .filter(Boolean),
        sourceClassification: caseSource,
        contentConsentGranted: consentGranted,
        contentRedacted,
      })
    );
  };

  const runEvaluation = async () => {
    if (!selectedDefinition || !selectedRevision) return;
    await runAction('Evaluation completed.', () =>
      evaluateAiPrompt(selectedDefinition.workflowKey, selectedDefinition.stageKey, {
        candidateRevisionId: selectedRevision.id,
        datasetVersion,
        caseIds: [],
      })
    );
  };

  const generateCandidate = async (experiment: AiPromptExperiment) => {
    await runAction('Draft candidate created from failed cases.', () =>
      createAiPromptCandidate(experiment.id)
    );
  };

  const startCanary = async () => {
    if (!selectedDefinition || !selectedRevision || !latestPassingExperiment) return;
    await runAction('Canary started.', () =>
      startAiPromptCanary(selectedDefinition.workflowKey, selectedDefinition.stageKey, {
        candidateRevisionId: selectedRevision.id,
        expectedActiveRevisionId: selectedDefinition.activeRevisionId ?? '',
        experimentId: latestPassingExperiment.id,
        percentage: canaryPercentage,
      })
    );
  };

  const executeConfirmedAction = async () => {
    if (!selectedDefinition) return;
    const definition = selectedDefinition;
    const action = confirmAction;
    setConfirmAction(null);
    if (action === 'promote' && definition.canaryRevisionId && definition.canaryExperimentId) {
      await runAction('Canary promoted to active.', () =>
        promoteAiPromptCanary(definition.workflowKey, definition.stageKey, {
          candidateRevisionId: definition.canaryRevisionId!,
          expectedCanaryRevisionId: definition.canaryRevisionId!,
          expectedActiveRevisionId: definition.activeRevisionId ?? '',
          experimentId: definition.canaryExperimentId!,
        })
      );
    } else if (action === 'cancel-canary' && definition.canaryRevisionId) {
      await runAction('Canary cancelled.', () =>
        cancelAiPromptCanary(definition.workflowKey, definition.stageKey, {
          candidateRevisionId: definition.canaryRevisionId!,
          expectedCanaryRevisionId: definition.canaryRevisionId!,
          expectedActiveRevisionId: definition.activeRevisionId ?? '',
          experimentId: definition.canaryExperimentId ?? '',
        })
      );
    } else if (action === 'rollback' && definition.activeRevisionId) {
      await runAction('Active prompt rolled back.', () =>
        rollbackAiPrompt(definition.workflowKey, definition.stageKey, {
          expectedActiveRevisionId: definition.activeRevisionId!,
        })
      );
    }
  };

  const changeMessage = (index: number, template: string) =>
    setMessages(current =>
      current.map((message, itemIndex) =>
        itemIndex === index ? { ...message, template } : message
      )
    );

  return (
    <main className="container-fluid py-4">
      <header className="d-flex flex-wrap justify-content-between align-items-start gap-3 mb-4">
        <div>
          <div className="text-uppercase small fw-semibold text-primary mb-1">AI operations</div>
          <h1 className="h3 mb-1">Workflow prompts</h1>
          <p className="text-muted mb-0">
            Review immutable prompt revisions, compare candidates, and control rollout.
          </p>
        </div>
        {canEdit && (
          <Button
            variant="outline-primary"
            disabled={busy}
            onClick={() => void runAction('Baseline prompts initialized.', bootstrapAiPrompts)}
          >
            <Sparkles size={16} className="me-2" />
            Initialize baselines
          </Button>
        )}
      </header>

      {error && (
        <Alert variant="danger" role="alert" dismissible onClose={() => setError('')}>
          {error}
        </Alert>
      )}

      {definitions.length === 0 ? (
        <section className="border rounded p-4 bg-body-tertiary" aria-labelledby="empty-title">
          <h2 id="empty-title" className="h5">
            No workflow baselines initialized
          </h2>
          <p className="mb-0 text-muted">
            An authorized prompt editor can initialize the reviewed Chat baselines. This does not
            enable production prompt activation.
          </p>
        </section>
      ) : (
        <Row className="g-4 align-items-start">
          <Col xl={3} lg={4}>
            <section className="border rounded bg-body p-3" aria-labelledby="workflow-list-title">
              <h2 id="workflow-list-title" className="h6 mb-3">
                Workflow stages
              </h2>
              <div className="d-grid gap-2">
                {definitions.map(definition => {
                  const key = `${definition.workflowKey}/${definition.stageKey}`;
                  const selected = key === selectedKey;
                  return (
                    <button
                      type="button"
                      key={key}
                      className={`btn text-start border ${selected ? 'border-primary bg-primary-subtle' : 'btn-light'}`}
                      aria-pressed={selected}
                      onClick={() => setSelectedKey(key)}
                    >
                      <span className="d-block fw-semibold">{definition.displayName}</span>
                      <span className="small text-muted">{definition.workflowKey}</span>
                      <span className="d-flex flex-wrap gap-1 mt-2">
                        <Badge bg={definition.activeRevisionId ? 'success' : 'secondary'}>
                          {definition.activeRevisionId ? 'Active' : 'Uninitialized'}
                        </Badge>
                        {definition.canaryRevisionId && (
                          <Badge bg="warning" text="dark">
                            {definition.canaryPercentage}% canary
                          </Badge>
                        )}
                      </span>
                    </button>
                  );
                })}
              </div>
            </section>
          </Col>

          <Col xl={9} lg={8}>
            {!selectedDefinition ? (
              <section className="border rounded p-4 text-muted">
                <CircleHelp className="me-2" size={18} />
                Select a workflow stage to inspect its revisions and evaluation evidence.
              </section>
            ) : loadingDetails ? (
              <div className="border rounded p-4" role="status">
                Loading workflow evidence…
              </div>
            ) : (
              <div className="d-grid gap-4">
                <section className="border rounded p-3 p-lg-4" aria-labelledby="revision-title">
                  <div className="d-flex flex-wrap align-items-start justify-content-between gap-3 mb-3">
                    <div>
                      <h2 id="revision-title" className="h5 mb-1">
                        {selectedDefinition.displayName}
                      </h2>
                      <div className="small text-muted">
                        Schema <code>{selectedDefinition.responseSchema}</code>
                        <span className="mx-2">·</span>
                        Active <code>{selectedDefinition.activeRevisionId ?? 'none'}</code>
                      </div>
                    </div>
                    <Form.Group>
                      <Form.Label htmlFor="revision-select" className="small mb-1">
                        Revision history
                      </Form.Label>
                      <Form.Select
                        id="revision-select"
                        size="sm"
                        value={selectedRevisionId}
                        onChange={event => setSelectedRevisionId(event.target.value)}
                      >
                        {data.revisions.map(revision => (
                          <option key={revision.id} value={revision.id}>
                            {revision.lifecycle} ·{' '}
                            {new Date(revision.createdAtUtc).toLocaleString()} ·{' '}
                            {revision.id.slice(-8)}
                          </option>
                        ))}
                      </Form.Select>
                    </Form.Group>
                  </div>

                  {selectedRevision ? (
                    <>
                      <div className="d-flex flex-wrap gap-2 align-items-center mb-3">
                        <Badge bg={lifecycleVariant(selectedRevision.lifecycle)}>
                          {selectedRevision.lifecycle}
                        </Badge>
                        <span className="small text-muted">
                          Hash <code>{selectedRevision.contentHash.slice(0, 16)}</code>
                        </span>
                        {selectedRevision.parentRevisionId && (
                          <span className="small text-muted">
                            Parent <code>{selectedRevision.parentRevisionId.slice(-8)}</code>
                          </span>
                        )}
                      </div>
                      {messages.map((message, index) => (
                        <Form.Group className="mb-3" key={`${selectedRevision.id}-${message.role}`}>
                          <Form.Label htmlFor={`message-${index}`} className="text-capitalize">
                            {message.role} message
                          </Form.Label>
                          <Form.Control
                            id={`message-${index}`}
                            as="textarea"
                            rows={message.role === 'system' ? 8 : 3}
                            value={message.template}
                            disabled={!canEdit || busy}
                            onChange={event => changeMessage(index, event.target.value)}
                          />
                        </Form.Group>
                      ))}
                      <Row className="g-3 align-items-end">
                        <Col md={8}>
                          <Form.Group>
                            <Form.Label htmlFor="revision-rationale">Change rationale</Form.Label>
                            <Form.Control
                              id="revision-rationale"
                              value={rationale}
                              maxLength={500}
                              disabled={!canEdit || busy}
                              onChange={event => setRationale(event.target.value)}
                            />
                          </Form.Group>
                        </Col>
                        <Col md={4} className="d-grid">
                          <Button
                            variant="primary"
                            disabled={
                              !canEdit ||
                              busy ||
                              selectedRevision.lifecycle === 'Active' ||
                              !rationale.trim()
                            }
                            onClick={() => void saveDraft()}
                          >
                            {busy ? (
                              <Spinner size="sm" className="me-2" />
                            ) : (
                              <Save size={16} className="me-2" />
                            )}
                            Save immutable draft
                          </Button>
                        </Col>
                      </Row>
                    </>
                  ) : (
                    <Alert variant="info" className="mb-0">
                      No prompt revision is available for this stage.
                    </Alert>
                  )}
                </section>

                <section className="border rounded p-3 p-lg-4" aria-labelledby="evaluation-title">
                  <div className="d-flex justify-content-between align-items-start gap-3 mb-3">
                    <div>
                      <h2 id="evaluation-title" className="h5 mb-1">
                        Evaluation evidence
                      </h2>
                      <p className="text-muted small mb-0">
                        Evaluation inputs require explicit consent and redaction. Dataset versions
                        become immutable after evaluation.
                      </p>
                    </div>
                    <Form.Group className="flex-shrink-0">
                      <Form.Label htmlFor="dataset-version" className="small mb-1">
                        Dataset version
                      </Form.Label>
                      <Form.Control
                        id="dataset-version"
                        size="sm"
                        value={datasetVersion}
                        maxLength={80}
                        onChange={event => setDatasetVersion(event.target.value)}
                      />
                    </Form.Group>
                  </div>

                  {canEvaluate && (
                    <>
                      <Row className="g-3">
                        <Col lg={7}>
                          <Form.Group className="mb-3">
                            <Form.Label htmlFor="evaluation-input">
                              Curated test input (JSON)
                            </Form.Label>
                            <Form.Control
                              id="evaluation-input"
                              as="textarea"
                              rows={5}
                              value={caseInput}
                              onChange={event => setCaseInput(event.target.value)}
                            />
                          </Form.Group>
                        </Col>
                        <Col lg={5}>
                          <Form.Group className="mb-3">
                            <Form.Label htmlFor="expected-checks">
                              Required output checks (comma-separated)
                            </Form.Label>
                            <Form.Control
                              id="expected-checks"
                              value={expectedContains}
                              onChange={event => setExpectedContains(event.target.value)}
                            />
                          </Form.Group>
                          <Form.Group className="mb-3">
                            <Form.Label htmlFor="case-source">Source classification</Form.Label>
                            <Form.Select
                              id="case-source"
                              value={caseSource}
                              onChange={event => setCaseSource(event.target.value)}
                            >
                              <option value="curated">Curated</option>
                              <option value="consented-feedback">Consented feedback</option>
                            </Form.Select>
                          </Form.Group>
                          <Form.Check
                            id="content-consent"
                            className="mb-2"
                            label="I confirm the content is approved for evaluation."
                            checked={consentGranted}
                            onChange={event => setConsentGranted(event.target.checked)}
                          />
                          <Form.Check
                            id="content-redacted"
                            label="I confirm the input has been reviewed and redacted."
                            checked={contentRedacted}
                            onChange={event => setContentRedacted(event.target.checked)}
                          />
                        </Col>
                      </Row>
                      <div className="d-flex flex-wrap gap-2 mt-3">
                        <Button
                          variant="outline-primary"
                          disabled={
                            busy ||
                            !consentGranted ||
                            !contentRedacted ||
                            !datasetVersion.trim() ||
                            !expectedContains.trim()
                          }
                          onClick={() => void addEvaluationCase()}
                        >
                          Add reviewed case
                        </Button>
                        <Button
                          variant="primary"
                          disabled={
                            busy ||
                            !selectedRevision ||
                            selectedRevision.lifecycle !== 'Draft' ||
                            !datasetVersion.trim()
                          }
                          onClick={() => void runEvaluation()}
                        >
                          {busy ? (
                            <Spinner size="sm" className="me-2" />
                          ) : (
                            <CheckCircle2 size={16} className="me-2" />
                          )}
                          Compare with active baseline
                        </Button>
                      </div>
                    </>
                  )}

                  <div className="table-responsive mt-4">
                    <Table hover size="sm" className="align-middle mb-0">
                      <caption className="caption-top">
                        Immutable baseline-versus-candidate evaluation history
                      </caption>
                      <thead>
                        <tr>
                          <th scope="col">Status</th>
                          <th scope="col">Candidate</th>
                          <th scope="col">Dataset</th>
                          <th scope="col">Passed cases</th>
                          <th scope="col">Latency delta</th>
                          <th scope="col">
                            <span className="visually-hidden">Actions</span>
                          </th>
                        </tr>
                      </thead>
                      <tbody>
                        {data.experiments.map(experiment => {
                          const latencyDelta = experiment.baselineDurationMilliseconds
                            ? Math.round(
                                (experiment.candidateDurationMilliseconds /
                                  experiment.baselineDurationMilliseconds -
                                  1) *
                                  100
                              )
                            : 0;
                          return (
                            <tr key={experiment.id}>
                              <td>
                                <Badge
                                  bg={
                                    experiment.status === 'Passed'
                                      ? 'success'
                                      : experiment.status === 'Rejected'
                                        ? 'danger'
                                        : 'secondary'
                                  }
                                >
                                  {experiment.status}
                                </Badge>
                              </td>
                              <td>
                                <code>{experiment.candidateRevisionId.slice(-8)}</code>
                              </td>
                              <td>
                                {experiment.datasetVersion}{' '}
                                <span className="text-muted">({experiment.casesEvaluated})</span>
                              </td>
                              <td>
                                {experiment.candidatePassCount}/{experiment.casesEvaluated}{' '}
                                <span className="text-muted">
                                  vs {experiment.baselinePassCount}
                                </span>
                              </td>
                              <td>
                                {latencyDelta > 0 ? '+' : ''}
                                {latencyDelta}%
                              </td>
                              <td className="text-end">
                                {!experiment.candidatePassCount && canEdit && (
                                  <Button
                                    variant="link"
                                    size="sm"
                                    disabled={busy}
                                    onClick={() => void generateCandidate(experiment)}
                                  >
                                    Create draft from failures
                                  </Button>
                                )}
                              </td>
                            </tr>
                          );
                        })}
                        {data.experiments.length === 0 && (
                          <tr>
                            <td colSpan={6} className="text-muted">
                              No evaluations have been run for this stage.
                            </td>
                          </tr>
                        )}
                      </tbody>
                    </Table>
                  </div>
                </section>

                <section className="border rounded p-3 p-lg-4" aria-labelledby="rollout-title">
                  <div className="d-flex align-items-center gap-2 mb-2">
                    <ShieldCheck size={18} aria-hidden="true" />
                    <h2 id="rollout-title" className="h5 mb-0">
                      Controlled rollout
                    </h2>
                  </div>
                  <p className="small text-muted">
                    A passing deterministic evaluation is required. Candidate allocation is stable
                    per request and never self-promotes.
                  </p>
                  {selectedDefinition.canaryRevisionId ? (
                    <Alert variant="warning">
                      Canary <code>{selectedDefinition.canaryRevisionId}</code> receives{' '}
                      {selectedDefinition.canaryPercentage}% of requests.
                      {canActivate && (
                        <div className="d-flex flex-wrap gap-2 mt-3">
                          <Button
                            variant="success"
                            disabled={busy}
                            onClick={() => setConfirmAction('promote')}
                          >
                            Promote canary
                          </Button>
                          <Button
                            variant="outline-danger"
                            disabled={busy}
                            onClick={() => setConfirmAction('cancel-canary')}
                          >
                            Cancel canary
                          </Button>
                        </div>
                      )}
                    </Alert>
                  ) : (
                    <Row className="g-3 align-items-end">
                      <Col sm={4} md={3}>
                        <Form.Group>
                          <Form.Label htmlFor="canary-percent">Canary allocation (%)</Form.Label>
                          <Form.Control
                            id="canary-percent"
                            type="number"
                            min={1}
                            max={50}
                            value={canaryPercentage}
                            disabled={!canActivate}
                            onChange={event => setCanaryPercentage(Number(event.target.value))}
                          />
                        </Form.Group>
                      </Col>
                      <Col>
                        <Button
                          variant="primary"
                          disabled={
                            !canActivate ||
                            busy ||
                            !selectedRevision ||
                            selectedRevision.lifecycle !== 'Draft' ||
                            !latestPassingExperiment ||
                            !selectedDefinition.activeRevisionId
                          }
                          onClick={() => void startCanary()}
                        >
                          <GitBranch size={16} className="me-2" />
                          Start evaluated canary
                        </Button>
                        {!latestPassingExperiment && (
                          <div className="small text-muted mt-2">
                            Select a draft with a passing evaluation against the current active
                            revision.
                          </div>
                        )}
                      </Col>
                    </Row>
                  )}
                  {selectedDefinition.previousRevisionId && canActivate && (
                    <div className="mt-3 pt-3 border-top">
                      <Button
                        variant="outline-danger"
                        disabled={busy || Boolean(selectedDefinition.canaryRevisionId)}
                        onClick={() => setConfirmAction('rollback')}
                      >
                        <RotateCcw size={16} className="me-2" />
                        Roll back to previous revision
                      </Button>
                    </div>
                  )}
                </section>

                <section className="border rounded p-3 p-lg-4" aria-labelledby="health-title">
                  <div className="d-flex align-items-center gap-2 mb-3">
                    <History size={18} aria-hidden="true" />
                    <h2 id="health-title" className="h5 mb-0">
                      Execution health
                    </h2>
                  </div>
                  <p className="small text-muted">
                    Metadata only. Prompt, transcript, output, tool payloads, and credentials are
                    not displayed.
                  </p>
                  <div className="table-responsive">
                    <Table hover size="sm" className="align-middle mb-0">
                      <thead>
                        <tr>
                          <th scope="col">Time</th>
                          <th scope="col">Status</th>
                          <th scope="col">Revision</th>
                          <th scope="col">Items</th>
                          <th scope="col">Duration</th>
                          <th scope="col">Error code</th>
                        </tr>
                      </thead>
                      <tbody>
                        {data.executions.map(execution => (
                          <tr key={execution.executionId}>
                            <td>{new Date(execution.createdAtUtc).toLocaleString()}</td>
                            <td>
                              <Badge bg={execution.status === 'Succeeded' ? 'success' : 'danger'}>
                                {execution.status}
                              </Badge>
                            </td>
                            <td>
                              <code>{execution.revisionId.slice(-8)}</code>
                            </td>
                            <td>
                              {execution.outputCount}/{execution.inputCount}
                            </td>
                            <td>{execution.durationMilliseconds} ms</td>
                            <td>{execution.errorCode ?? '—'}</td>
                          </tr>
                        ))}
                        {data.executions.length === 0 && (
                          <tr>
                            <td colSpan={6} className="text-muted">
                              No executions recorded yet.
                            </td>
                          </tr>
                        )}
                      </tbody>
                    </Table>
                  </div>
                </section>
              </div>
            )}
          </Col>
        </Row>
      )}

      <Modal show={confirmAction !== null} onHide={() => setConfirmAction(null)} centered>
        <Modal.Header closeButton>
          <Modal.Title>
            {confirmAction === 'promote'
              ? 'Promote evaluated canary?'
              : confirmAction === 'rollback'
                ? 'Roll back active prompt?'
                : 'Cancel active canary?'}
          </Modal.Title>
        </Modal.Header>
        <Modal.Body>
          {confirmAction === 'promote'
            ? 'The canary revision will become active for all requests. The current active revision remains available for rollback.'
            : confirmAction === 'rollback'
              ? 'The previous immutable revision will become active. The current revision remains in history.'
              : 'The canary pointer will be cleared. The active baseline will remain unchanged.'}
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" disabled={busy} onClick={() => setConfirmAction(null)}>
            Keep current state
          </Button>
          <Button
            variant={confirmAction === 'cancel-canary' ? 'danger' : 'primary'}
            disabled={busy}
            onClick={() => void executeConfirmedAction()}
          >
            {busy && <Spinner size="sm" className="me-2" />}
            Confirm
          </Button>
        </Modal.Footer>
      </Modal>
    </main>
  );
}
