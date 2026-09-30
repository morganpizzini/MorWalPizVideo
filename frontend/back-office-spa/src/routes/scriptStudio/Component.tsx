import { useResolvedLoaderData } from '@/router/asyncData';
import { useEffect, useState } from 'react';
import { Button, Col, Form, Row, Spinner } from 'react-bootstrap';
import { Copy, Download, Save, WandSparkles } from 'lucide-react';
import {} from 'react-router';
import type {
  ScriptStudioDocument,
  ScriptStudioFormat,
  ScriptStudioGenerationResponse,
  ScriptStudioOperation,
} from '@morwalpizvideo/models';
import {
  generateScriptStudio,
  getScriptStudioGlobalPrompt,
  saveScriptStudio,
  saveScriptStudioGlobalPrompt,
} from '@morwalpizvideo/services';
import { hasPermission, permissions } from '../../authorization/permissions';
import { useAppStore } from '../../state/appStore';

type LoaderData = { document: ScriptStudioDocument };

export default function Component() {
  const { document: initial } = useResolvedLoaderData() as LoaderData;
  const effectivePermissions = useAppStore(state => state.effectivePermissions);
  const [draft, setDraft] = useState<ScriptStudioDocument>(initial);
  const [globalPrompt, setGlobalPrompt] = useState('');
  const [operation, setOperation] = useState<ScriptStudioOperation>('expand');
  const [result, setResult] = useState('');
  const [status, setStatus] = useState('');
  const [busy, setBusy] = useState(false);
  const canManagePrompt = hasPermission(effectivePermissions, [
    permissions.scripts.globalPromptManage,
  ]);

  useEffect(() => setDraft(initial), [initial]);

  useEffect(() => {
    if (!canManagePrompt) return;
    getScriptStudioGlobalPrompt()
      .then(prompt => setGlobalPrompt(prompt.prompt))
      .catch(error =>
        setStatus(error instanceof Error ? error.message : 'Global prompt loading failed.')
      );
  }, [canManagePrompt]);

  const update = <K extends keyof ScriptStudioDocument>(key: K, value: ScriptStudioDocument[K]) =>
    setDraft(current => ({ ...current, [key]: value }));

  const save = async () => {
    setBusy(true);
    setStatus('');
    try {
      await saveScriptStudio(draft);
      if (canManagePrompt) await saveScriptStudioGlobalPrompt(globalPrompt);
      setStatus('Saved.');
    } catch (error) {
      setStatus(error instanceof Error ? error.message : 'Save failed.');
    } finally {
      setBusy(false);
    }
  };

  const generate = async () => {
    setBusy(true);
    setStatus('Generating...');
    try {
      const response: ScriptStudioGenerationResponse = await generateScriptStudio({
        operation,
        script: draft.script,
        prompt: draft.savedPrompt,
        examples: draft.examples,
        style: draft.style,
        generalContext: draft.generalContext,
        format: draft.format,
      });
      setResult(response.result);
      setStatus(
        `Generated. ${response.quotaUsed}/${response.quotaLimit} requests used this month.`
      );
    } catch (error) {
      setStatus(error instanceof Error ? error.message : 'Generation failed.');
    } finally {
      setBusy(false);
    }
  };

  const useResult = () => update('script', result);
  const copyResult = async () => {
    await navigator.clipboard.writeText(result);
    setStatus('Result copied.');
  };
  const downloadResult = () => {
    const extension = draft.format === 'markdown' ? 'md' : 'txt';
    const link = document.createElement('a');
    link.href = URL.createObjectURL(new Blob([result], { type: 'text/plain;charset=utf-8' }));
    link.download = `script-studio-result.${extension}`;
    link.click();
    URL.revokeObjectURL(link.href);
  };

  return (
    <main className="container-fluid py-4">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <div>
          <h1 className="h3 mb-1">Script Studio</h1>
          <p className="text-muted mb-0">Draft, refine, and review channel scripts.</p>
        </div>
        <Button variant="outline-primary" onClick={save} disabled={busy}>
          <Save size={16} /> Save
        </Button>
      </div>
      <Row className="g-4">
        <Col lg={7}>
          <Form.Group className="mb-3">
            <Form.Label>Script</Form.Label>
            <Form.Control
              as="textarea"
              rows={14}
              value={draft.script}
              onChange={event => update('script', event.target.value)}
            />
          </Form.Group>
          <Row className="g-3">
            <Col md={6}>
              <Form.Group>
                <Form.Label>Prompt</Form.Label>
                <Form.Control
                  as="textarea"
                  rows={4}
                  value={draft.savedPrompt}
                  onChange={event => update('savedPrompt', event.target.value)}
                />
              </Form.Group>
            </Col>
            <Col md={6}>
              <Form.Group>
                <Form.Label>Examples</Form.Label>
                <Form.Control
                  as="textarea"
                  rows={4}
                  value={draft.examples}
                  onChange={event => update('examples', event.target.value)}
                />
              </Form.Group>
            </Col>
            <Col md={6}>
              <Form.Group>
                <Form.Label>Style</Form.Label>
                <Form.Control
                  value={draft.style}
                  onChange={event => update('style', event.target.value)}
                />
              </Form.Group>
            </Col>
            <Col md={6}>
              <Form.Group>
                <Form.Label>General context</Form.Label>
                <Form.Control
                  value={draft.generalContext}
                  onChange={event => update('generalContext', event.target.value)}
                />
              </Form.Group>
            </Col>
          </Row>
          {canManagePrompt && (
            <Form.Group className="mt-3">
              <Form.Label>Global admin prompt</Form.Label>
              <Form.Control
                as="textarea"
                rows={3}
                value={globalPrompt}
                onChange={event => setGlobalPrompt(event.target.value)}
              />
            </Form.Group>
          )}
        </Col>
        <Col lg={5}>
          <div className="border rounded p-3 h-100">
            <Row className="g-3 align-items-end">
              <Col>
                <Form.Label>Operation</Form.Label>
                <Form.Select
                  value={operation}
                  onChange={event => setOperation(event.target.value as ScriptStudioOperation)}
                >
                  <option value="expand">Expand</option>
                  <option value="rewrite">Rewrite</option>
                  <option value="structure">Structure</option>
                </Form.Select>
              </Col>
              <Col>
                <Form.Label>Format</Form.Label>
                <Form.Select
                  value={draft.format}
                  onChange={event => update('format', event.target.value as ScriptStudioFormat)}
                >
                  <option value="markdown">Markdown</option>
                  <option value="plain">Plain text</option>
                </Form.Select>
              </Col>
            </Row>
            <Button className="mt-3" onClick={generate} disabled={busy || !draft.script.trim()}>
              <WandSparkles size={16} />{' '}
              {busy ? (
                <>
                  <Spinner size="sm" /> Working...
                </>
              ) : (
                'Generate'
              )}
            </Button>
            {status && <p className="small text-muted mt-3">{status}</p>}
            <Form.Group className="mt-3">
              <Form.Label>Current result</Form.Label>
              <Form.Control
                as="textarea"
                rows={16}
                value={result}
                onChange={event => setResult(event.target.value)}
                placeholder="Generated text appears here."
              />
            </Form.Group>
            <div className="d-flex gap-2 mt-3">
              <Button variant="outline-secondary" onClick={useResult} disabled={!result}>
                Use result
              </Button>
              <Button
                variant="outline-secondary"
                onClick={copyResult}
                disabled={!result}
                title="Copy result"
              >
                <Copy size={16} />
              </Button>
              <Button
                variant="outline-secondary"
                onClick={downloadResult}
                disabled={!result}
                title="Download result"
              >
                <Download size={16} />
              </Button>
            </div>
          </div>
        </Col>
      </Row>
    </main>
  );
}
