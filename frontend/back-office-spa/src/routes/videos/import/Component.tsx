import { useResolvedLoaderData } from '@/router/asyncData';
import React, { useEffect, useState } from 'react';
import { Alert, Button, Form, ListGroup, Tab, Tabs } from 'react-bootstrap';
import type { Category } from '@morwalpizvideo/models';
import GenericErrorList from '@components/GenericErrorList';
import { useToast } from '@components/ToastNotification/ToastContext';
import PageHeader from '@components/PageHeader';
import { useChannelContext } from '../../../contexts/ChannelContext';
import { VideoService, type BulkImportRoot, type BulkImportResult, type ImportCandidate, type VideoImportResponse } from '../../../services/videoService';
import type { LoaderData } from './loader';
import { useAppStore } from '../../../state/appStore';

interface ImportSelection extends BulkImportRoot { selected: boolean; }
const formatDate = (date: Date) => date.toISOString().slice(0, 10);
const isImportResponse = (value: unknown): value is VideoImportResponse => typeof value === 'object' && value !== null && 'status' in value;

const ImportVideo: React.FC = () => {
  const { categories: initialCategories } = useResolvedLoaderData<LoaderData>();
  const { selectedChannelId } = useChannelContext();
  const bulkImportEnabled = useAppStore(state => state.featureFlags.videoBulkImportEnabled);
  const [availableCategories] = useState<Category[]>(initialCategories);
  const [startDate, setStartDate] = useState(() => { const date = new Date(); date.setMonth(date.getMonth() - 1); return formatDate(date); });
  const [endDate, setEndDate] = useState(() => formatDate(new Date()));
  const [candidates, setCandidates] = useState<ImportCandidate[]>([]);
  const [selections, setSelections] = useState<Record<string, ImportSelection>>({});
  const [singleVideoId, setSingleVideoId] = useState('');
  const [singleCategories, setSingleCategories] = useState<string[]>([]);
  const [results, setResults] = useState<BulkImportResult[]>([]);
  const [loadingCandidates, setLoadingCandidates] = useState(false);
  const [saving, setSaving] = useState(false);
  const [singleSaving, setSingleSaving] = useState(false);
  const [error, setError] = useState('');
  const toast = useToast();

  useEffect(() => {
    if (!bulkImportEnabled || !selectedChannelId) return;
    let cancelled = false;
    setLoadingCandidates(true);
    VideoService.getImportCandidates(startDate, endDate).then(value => {
      if (cancelled) return;
      const available = value.filter(candidate => !candidate.alreadyImported);
      setCandidates(available);
      setSelections(Object.fromEntries(available.map(candidate => [candidate.videoId, { videoId: candidate.videoId, categories: [], references: [], selected: false }])));
    }).catch(() => { if (!cancelled) setError('Unable to load YouTube candidates.'); }).finally(() => { if (!cancelled) setLoadingCandidates(false); });
    return () => { cancelled = true; };
  }, [bulkImportEnabled, selectedChannelId, startDate, endDate]);

  const updateSelection = (videoId: string, update: Partial<ImportSelection>) => setSelections(current => ({ ...current, [videoId]: { ...current[videoId], ...update } }));
  const toggleCategory = (videoId: string, categoryId: string) => {
    const categories = selections[videoId]?.categories ?? [];
    updateSelection(videoId, { categories: categories.includes(categoryId) ? categories.filter(id => id !== categoryId) : [...categories, categoryId] });
  };
  const toggleSingleCategory = (categoryId: string) => setSingleCategories(current => current.includes(categoryId) ? current.filter(id => id !== categoryId) : [...current, categoryId]);
  const addReference = (rootId: string, referenceId: string) => updateSelection(rootId, { references: [...(selections[rootId]?.references ?? []), { videoId: referenceId, categories: [] }] });
  const removeReference = (rootId: string, referenceId: string) => updateSelection(rootId, { references: selections[rootId].references.filter(reference => reference.videoId !== referenceId) });
  const toggleReferenceCategory = (rootId: string, referenceId: string, categoryId: string) => updateSelection(rootId, { references: selections[rootId].references.map(reference => reference.videoId === referenceId ? { ...reference, categories: reference.categories.includes(categoryId) ? reference.categories.filter(id => id !== categoryId) : [...reference.categories, categoryId] } : reference) });

  const handleBulkSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError('');
    const roots = candidates.map(candidate => selections[candidate.videoId]).filter(selection => selection?.selected).map(({ selected: _selected, ...root }) => root);
    const nodes = roots.flatMap(root => [root, ...root.references]);
    if (!roots.length || nodes.some(node => !node.categories.length)) { setError('Select at least one root and one category for every root and reference.'); return; }
    setSaving(true);
    try {
      const importResults = await VideoService.bulkImport({ roots });
      setResults(importResults);
      const items = importResults.flatMap(result => [result.root, ...result.references]);
      const hasErrors = items.some(item => item.status === 'error');
      toast.show(hasErrors ? 'Bulk import completed with errors' : 'Import complete', 'The per-video result is shown below.', { variant: hasErrors ? 'warning' : 'success' });
    } catch { setError('Bulk import failed before results could be returned.'); } finally { setSaving(false); }
  };

  const handleSingleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!singleVideoId.trim() || !singleCategories.length) { setError('Enter a video ID and select at least one category.'); return; }
    setSingleSaving(true);
    try {
      const result = await VideoService.importVideo({ videoId: singleVideoId.trim(), categories: singleCategories });
      if (isImportResponse(result) && result.status === 'alreadyExists') toast.show('Video already exists', 'The video was already imported.', { variant: 'warning' });
      else if (isImportResponse(result) && result.status === 'imported' && result.shortLinkStatus === 'failed') { setSingleVideoId(''); setSingleCategories([]); toast.show('Import completed with warning', result.error ?? 'The video was imported, but its short link could not be created.', { variant: 'warning' }); }
      else if (isImportResponse(result) && result.status === 'imported') { setSingleVideoId(''); setSingleCategories([]); toast.show('Import complete', 'The video was imported successfully.', { variant: 'success' }); }
      else toast.show('Import failed', 'The video could not be imported.', { variant: 'danger' });
    } catch { toast.show('Import failed', 'Single video import failed.', { variant: 'danger' }); } finally { setSingleSaving(false); }
  };

  const selectedRoots = candidates.filter(candidate => selections[candidate.videoId]?.selected);
  return <>
    <PageHeader title="Import YouTube videos" />
    <GenericErrorList errors={error ? [error] : []} />
    <Tabs defaultActiveKey={bulkImportEnabled ? 'bulk' : 'single'} className="mb-4">
      {bulkImportEnabled ? <Tab eventKey="bulk" title="Bulk import"><Form onSubmit={handleBulkSubmit}>
        <div className="row g-3 mb-3"><Form.Group className="col-md-6" controlId="importStartDate"><Form.Label>Published from (UTC) *</Form.Label><Form.Control type="date" value={startDate} onChange={event => setStartDate(event.target.value)} disabled={saving} required /></Form.Group><Form.Group className="col-md-6" controlId="importEndDate"><Form.Label>Published to (UTC) *</Form.Label><Form.Control type="date" value={endDate} onChange={event => setEndDate(event.target.value)} disabled={saving} required /></Form.Group></div>
        {!selectedChannelId ? <Alert variant="warning">Select an authorized channel from the application menu.</Alert> : null}
        {loadingCandidates ? <Alert variant="light">Loading candidates...</Alert> : null}
        <div className="border rounded p-3">{candidates.map(candidate => {
          const selection = selections[candidate.videoId];
          const assigned = new Set(selectedRoots.flatMap(root => [root.videoId, ...selections[root.videoId].references.map(reference => reference.videoId)]));
          const available = candidates.filter(item => item.videoId !== candidate.videoId && !assigned.has(item.videoId));
          return <div key={candidate.videoId} className="border-bottom pb-3 mb-3"><Form.Check id={`candidate-${candidate.videoId}`} label={`${candidate.title || candidate.videoId} (${new Date(candidate.publishedAt).toLocaleDateString()})`} checked={selection?.selected ?? false} onChange={event => updateSelection(candidate.videoId, { selected: event.target.checked })} disabled={saving} />
            {selection?.selected ? <div className="ms-4 mt-2"><Form.Label>Root categories *</Form.Label><div className="d-flex flex-wrap gap-3">{availableCategories.map(category => <Form.Check key={`${candidate.videoId}-${category.categoryId}`} id={`root-category-${candidate.videoId}-${category.categoryId}`} type="checkbox" label={category.title} checked={selection.categories.includes(category.categoryId)} onChange={() => toggleCategory(candidate.videoId, category.categoryId)} disabled={saving} />)}</div>
              <Form.Select className="mt-2" aria-label={`Add reference to ${candidate.title || candidate.videoId}`} value="" onChange={event => event.target.value && addReference(candidate.videoId, event.target.value)} disabled={saving || !available.length}><option value="">Add reference...</option>{available.map(item => <option key={item.videoId} value={item.videoId}>{item.title || item.videoId}</option>)}</Form.Select>
              {selection.references.map(reference => <div key={reference.videoId} className="border rounded p-2 mt-2"><div className="d-flex justify-content-between"><span>{candidates.find(item => item.videoId === reference.videoId)?.title || reference.videoId}</span><Button variant="outline-danger" size="sm" onClick={() => removeReference(candidate.videoId, reference.videoId)} disabled={saving}>Remove</Button></div><div className="d-flex flex-wrap gap-3 mt-2">{availableCategories.map(category => <Form.Check key={`${reference.videoId}-${category.categoryId}`} id={`reference-category-${candidate.videoId}-${reference.videoId}-${category.categoryId}`} type="checkbox" label={category.title} checked={reference.categories.includes(category.categoryId)} onChange={() => toggleReferenceCategory(candidate.videoId, reference.videoId, category.categoryId)} disabled={saving} />)}</div></div>)}</div> : null}</div>;
        })}</div><Button className="mt-3" variant="success" type="submit" disabled={saving || loadingCandidates || !selectedChannelId || !selectedRoots.length}>{saving ? 'Importing...' : 'Import selected videos'}</Button>
      </Form></Tab> : null}
      <Tab eventKey="single" title="Single import"><Form onSubmit={handleSingleSubmit}><Form.Group className="mb-3" controlId="singleVideoId"><Form.Label>Video ID *</Form.Label><Form.Control value={singleVideoId} onChange={event => setSingleVideoId(event.target.value)} required /></Form.Group><Form.Label>Categories *</Form.Label><div className="d-flex flex-wrap gap-2 mb-3">{availableCategories.map(category => <Button key={category.categoryId} size="sm" variant={singleCategories.includes(category.categoryId) ? 'primary' : 'outline-secondary'} aria-pressed={singleCategories.includes(category.categoryId)} onClick={() => toggleSingleCategory(category.categoryId)}>{category.title}</Button>)}</div><Button variant="success" type="submit" disabled={singleSaving}>{singleSaving ? 'Importing...' : 'Import video'}</Button></Form></Tab>
    </Tabs>
    {results.length ? <ListGroup className="mt-4">{results.map(result => <ListGroup.Item key={result.root.videoId}><strong>{result.root.videoId}</strong>: {result.root.status}{result.root.error ? ` - ${result.root.error}` : ''}{result.references.map(reference => <div key={reference.videoId} className="ms-3">{reference.videoId}: {reference.status}{reference.error ? ` - ${reference.error}` : ''}</div>)}</ListGroup.Item>)}</ListGroup> : null}
  </>;
};

export default ImportVideo;
