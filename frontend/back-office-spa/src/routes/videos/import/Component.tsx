import React, { useEffect, useState } from 'react';
import { useLoaderData } from 'react-router';
import { Alert, Button, Form, ListGroup, Tab, Tabs } from 'react-bootstrap';
import type { Category } from '@morwalpizvideo/models';
import GenericErrorList from '@components/GenericErrorList';
import { useToast } from '@components/ToastNotification/ToastContext';
import PageHeader from '@components/PageHeader';
import { useChannelContext } from '../../../contexts/ChannelContext';
import { VideoService, type BulkImportItem, type BulkImportResult, type ImportCandidate } from '../../../services/videoService';
import type { LoaderData } from './loader';

interface ImportSelection extends BulkImportItem { selected: boolean; }
const formatDate = (date: Date) => date.toISOString().slice(0, 10);

const ImportVideo: React.FC = () => {
  const { categories: initialCategories, targets } = useLoaderData<LoaderData>();
  const { selectedChannelId } = useChannelContext();
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
    if (!selectedChannelId || !startDate || !endDate) return;
    let cancelled = false;
    setLoadingCandidates(true); setError('');
    VideoService.getImportCandidates(startDate, endDate).then(value => {
      if (cancelled) return;
      const newCandidates = value.filter(candidate => !candidate.alreadyImported);
      setCandidates(newCandidates);
      setSelections(Object.fromEntries(newCandidates.map(candidate => [candidate.videoId, { videoId: candidate.videoId, categories: [], target: '', selected: false }])));
    }).catch(() => { if (!cancelled) setError('Unable to load YouTube candidates.'); })
      .finally(() => { if (!cancelled) setLoadingCandidates(false); });
    return () => { cancelled = true; };
  }, [selectedChannelId, startDate, endDate]);

  const updateSelection = (videoId: string, update: Partial<ImportSelection>) => setSelections(current => ({ ...current, [videoId]: { ...current[videoId], ...update } }));
  const toggleCategory = (videoId: string, categoryId: string) => {
    const current = selections[videoId]?.categories ?? [];
    updateSelection(videoId, { categories: current.includes(categoryId) ? current.filter(id => id !== categoryId) : [...current, categoryId] });
  };
  const toggleSingleCategory = (categoryId: string) => setSingleCategories(current => current.includes(categoryId) ? current.filter(id => id !== categoryId) : [...current, categoryId]);

  const handleBulkSubmit = async (event: React.FormEvent) => {
    event.preventDefault(); setError(''); setResults([]);
    const items = candidates.map(candidate => selections[candidate.videoId]).filter(selection => selection?.selected).map(({ selected, ...item }) => item);
    if (!items.length || items.some(item => item.categories.length === 0)) { setError('Select at least one candidate and one category for every selected video.'); return; }
    setSaving(true);
    try { setResults(await VideoService.bulkImport({ items })); toast.show('Import complete', 'The per-video result is shown below.', { variant: 'success' }); }
    catch { setError('Bulk import failed before results could be returned.'); }
    finally { setSaving(false); }
  };

  const handleSingleSubmit = async (event: React.FormEvent) => {
    event.preventDefault(); setError('');
    if (!singleVideoId.trim() || !singleCategories.length) { setError('Enter a video ID and select at least one category.'); return; }
    setSingleSaving(true);
    try { await VideoService.importVideo({ videoId: singleVideoId.trim(), categories: singleCategories }); setSingleVideoId(''); toast.show('Import complete', 'The video was imported successfully.', { variant: 'success' }); }
    catch { setError('Single video import failed.'); }
    finally { setSingleSaving(false); }
  };

  const selectedCandidateIds = candidates.filter(candidate => selections[candidate.videoId]?.selected);
  return <>
    <PageHeader title="Import YouTube videos" />
    <GenericErrorList errors={error ? [error] : []} />
    <Tabs defaultActiveKey="bulk" className="mb-4">
      <Tab eventKey="bulk" title="Bulk import">
        <Form onSubmit={handleBulkSubmit}>
          <div className="row g-3 mb-3">
            <Form.Group className="col-md-6" controlId="importStartDate"><Form.Label>Published from (UTC) *</Form.Label><Form.Control type="date" value={startDate} onChange={event => setStartDate(event.target.value)} required /></Form.Group>
            <Form.Group className="col-md-6" controlId="importEndDate"><Form.Label>Published to (UTC) *</Form.Label><Form.Control type="date" value={endDate} onChange={event => setEndDate(event.target.value)} required /></Form.Group>
          </div>
          {!selectedChannelId ? <Alert variant="warning">Select an authorized channel from the application menu.</Alert> : null}
          {loadingCandidates ? <Alert variant="light">Loading candidates...</Alert> : null}
          <Form.Label>Candidates</Form.Label>
          <div className="border rounded p-3">
            {candidates.map((candidate, index) => {
              const selection = selections[candidate.videoId];
              const previousCandidates = candidates.slice(0, index).filter(previous => selections[previous.videoId]?.selected);
              return <div key={candidate.videoId} className="border-bottom pb-3 mb-3">
                <Form.Check id={`candidate-${candidate.videoId}`} label={`${candidate.title || candidate.videoId} (${new Date(candidate.publishedAt).toLocaleDateString()})`} checked={selection?.selected ?? false} onChange={event => updateSelection(candidate.videoId, { selected: event.target.checked })} className="mb-2" />
                {selection?.selected ? <div className="ms-4">
                  <Form.Label className="small">Categories *</Form.Label>
                  <div className="d-flex flex-wrap gap-3 mb-2">{availableCategories.map(category => <Form.Check key={`${candidate.videoId}-${category.categoryId}`} type="checkbox" id={`category-${candidate.videoId}-${category.categoryId}`} label={category.title} checked={selection.categories.includes(category.categoryId)} onChange={() => toggleCategory(candidate.videoId, category.categoryId)} />)}</div>
                  <Form.Select aria-label={`Target for ${candidate.title || candidate.videoId}`} value={selection.target ?? ''} onChange={event => updateSelection(candidate.videoId, { target: event.target.value })}>
                    <option value="">Create new YouTubeContent</option>
                    {targets.map(target => <option key={target.contentId} value={target.contentId}>Append to {target.title || target.contentId}</option>)}
                    {previousCandidates.map(previous => <option key={previous.videoId} value={previous.videoId}>Append to {previous.title || previous.videoId}</option>)}
                  </Form.Select>
                </div> : null}
              </div>;
            })}
            {!candidates.length && !loadingCandidates ? <p className="text-muted mb-0">No new candidates found for this date range.</p> : null}
          </div>
          <div className="d-flex justify-content-end mt-3"><Button variant="success" type="submit" disabled={saving || loadingCandidates || !selectedChannelId || selectedCandidateIds.length === 0}>{saving ? 'Importing...' : 'Import selected videos'}</Button></div>
        </Form>
      </Tab>
      <Tab eventKey="single" title="Single import">
        <Form onSubmit={handleSingleSubmit}>
          <Form.Group className="mb-3" controlId="singleVideoId"><Form.Label>Video ID *</Form.Label><Form.Control value={singleVideoId} onChange={event => setSingleVideoId(event.target.value)} required /></Form.Group>
          <Form.Group className="mb-3" controlId="singleCategories"><Form.Label>Categories *</Form.Label><div className="d-flex flex-wrap gap-3">{availableCategories.map(category => <Form.Check key={category.categoryId} type="checkbox" id={`single-category-${category.categoryId}`} label={category.title} checked={singleCategories.includes(category.categoryId)} onChange={() => toggleSingleCategory(category.categoryId)} />)}</div></Form.Group>
          <Button variant="success" type="submit" disabled={singleSaving}>{singleSaving ? 'Importing...' : 'Import video'}</Button>
        </Form>
      </Tab>
    </Tabs>
    {results.length ? <ListGroup className="mt-4">{results.map(result => <ListGroup.Item key={result.videoId}><strong>{result.videoId}</strong>: {result.status}{result.error ? ` - ${result.error}` : ''}</ListGroup.Item>)}</ListGroup> : null}
  </>;
};

export default ImportVideo;
