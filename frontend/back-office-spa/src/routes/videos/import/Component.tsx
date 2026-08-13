import React, { useEffect, useState } from 'react';
import { useLoaderData } from 'react-router';
import { Form, Button, Alert, ListGroup } from 'react-bootstrap';
import { endpoints, get, post, getSelectedChannelId, setSelectedChannelId } from '@morwalpizvideo/services';
import type { Category, Channel } from '@morwalpizvideo/models';
import GenericErrorList from '@components/GenericErrorList';
import { useToast } from '@components/ToastNotification/ToastContext';
import PageHeader from '@components/PageHeader';
import { VideoService, type BulkImportResult, type ImportCandidate } from '../../../services/videoService';
import { LoaderData } from './loader';

const ImportVideo: React.FC = () => {
  const { categories: initialCategories, channels, targets } = useLoaderData<LoaderData>();
  const [availableCategories, setAvailableCategories] = useState<Category[]>(initialCategories);
  const [channelId, setChannelId] = useState(
    channels.find(channel => channel.channelId === getSelectedChannelId())?.channelId ?? channels[0]?.channelId ?? '',
  );
  const [startDate, setStartDate] = useState(new Date().toISOString().slice(0, 10));
  const [candidates, setCandidates] = useState<ImportCandidate[]>([]);
  const [selectedVideoIds, setSelectedVideoIds] = useState<string[]>([]);
  const [categories, setCategories] = useState<string[]>([]);
  const [targetContentId, setTargetContentId] = useState('');
  const [manualTargetContentId, setManualTargetContentId] = useState('');
  const [newCategoryTitle, setNewCategoryTitle] = useState('');
  const [results, setResults] = useState<BulkImportResult[]>([]);
  const [loadingCandidates, setLoadingCandidates] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const toast = useToast();

  useEffect(() => {
    if (!channelId || !startDate) return;
    setSelectedChannelId(channelId);
    let cancelled = false;
    setLoadingCandidates(true);
    VideoService.getImportCandidates(channelId, startDate)
      .then(value => { if (!cancelled) setCandidates(value); })
      .catch(() => { if (!cancelled) setError('Unable to load YouTube candidates.'); })
      .finally(() => { if (!cancelled) setLoadingCandidates(false); });
    return () => { cancelled = true; };
  }, [channelId, startDate]);

  const handleCategoryChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    if (e.target.checked) {
      setCategories([...categories, value]);
    } else {
      setCategories(categories.filter(cat => cat !== value));
    }
  };

  const toggleCandidate = (videoId: string) => {
    setSelectedVideoIds(current => current.includes(videoId)
      ? current.filter(id => id !== videoId)
      : [...current, videoId]);
  };

  const createCategory = async () => {
    const title = newCategoryTitle.trim();
    if (!title) return;
    try {
      const category = await post(endpoints.CATEGORIES, { title, description: '' }) as Category;
      setCategories(current => [...current, category.categoryId]);
      setNewCategoryTitle('');
      const refreshed = await get(endpoints.CATEGORIES) as Category[];
      setAvailableCategories(refreshed);
    } catch {
      setError('Unable to create category.');
    }
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError('');
    setResults([]);
    if (!selectedVideoIds.length || !categories.length) {
      setError('Select at least one candidate and one category.');
      return;
    }
    setSaving(true);
    try {
      const value = await VideoService.bulkImport({
        videoIds: selectedVideoIds,
        categories,
        targetContentId: manualTargetContentId.trim() || targetContentId || undefined,
      });
      setResults(value);
      setSelectedVideoIds([]);
      toast.show('Import complete', 'The per-video result is shown below.', { variant: 'success' });
    } catch {
      setError('Bulk import failed before results could be returned.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <>
      <PageHeader title="Import YouTube videos" />
      <GenericErrorList errors={error ? [error] : []} />
      <Form onSubmit={handleSubmit}>
        <Form.Group className="mb-3" controlId="importChannel">
          <Form.Label>Authorized channel *</Form.Label>
          <Form.Select value={channelId} onChange={e => setChannelId(e.target.value)}>
            {channels.map((channel: Channel) => <option key={channel.channelId} value={channel.channelId}>{channel.channelName}</option>)}
          </Form.Select>
        </Form.Group>
        <Form.Group className="mb-3" controlId="importStartDate">
          <Form.Label>Published from (UTC) *</Form.Label>
          <Form.Control type="date" value={startDate} onChange={e => setStartDate(e.target.value)} />
        </Form.Group>
        <Form.Group className="mb-3" controlId="importCandidates">
          <Form.Label>Candidates</Form.Label>
          {loadingCandidates ? <Alert variant="light">Loading candidates...</Alert> : null}
          <div className="border rounded p-3">
            {candidates.map(candidate => (
              <Form.Check
                key={candidate.videoId}
                type="checkbox"
                id={`candidate-${candidate.videoId}`}
                label={`${candidate.title || candidate.videoId} (${new Date(candidate.publishedAt).toLocaleDateString()})${candidate.alreadyImported ? ' - already imported' : ''}`}
                checked={selectedVideoIds.includes(candidate.videoId)}
                disabled={candidate.alreadyImported}
                onChange={() => toggleCandidate(candidate.videoId)}
                className="mb-2"
              />
            ))}
          </div>
        </Form.Group>
        <Form.Group className="mb-3" controlId="importCategories">
          <Form.Label>Categories *</Form.Label>
          <div className="border rounded p-3">
            {availableCategories.map(cat => <Form.Check key={cat.categoryId} type="checkbox" id={`category-${cat.categoryId}`} label={cat.title} value={cat.categoryId} checked={categories.includes(cat.categoryId)} onChange={handleCategoryChange} className="mb-2" />)}
          </div>
          <div className="d-flex gap-2 mt-2">
            <Form.Control value={newCategoryTitle} onChange={e => setNewCategoryTitle(e.target.value)} placeholder="New category title" />
            <Button type="button" variant="outline-secondary" onClick={createCategory}>Create category</Button>
          </div>
        </Form.Group>
        <Form.Group className="mb-3" controlId="importTarget">
          <Form.Label>Append to existing content (optional)</Form.Label>
          <Form.Select value={targetContentId} onChange={e => setTargetContentId(e.target.value)}>
            <option value="">Create one new YouTubeContent per video</option>
            {targets.map(target => <option key={target.contentId} value={target.contentId}>{target.title || target.contentId} ({target.videoCount} videos)</option>)}
          </Form.Select>
          <Form.Control className="mt-2" value={manualTargetContentId} onChange={e => setManualTargetContentId(e.target.value)} placeholder="Or enter a content ID" />
        </Form.Group>
        <div className="d-flex justify-content-end mt-3">
          <Button variant="success" type="submit" disabled={saving || loadingCandidates || !channelId}>
            {saving ? 'Importing...' : 'Import selected videos'}
          </Button>
        </div>
      </Form>
      {results.length ? <ListGroup className="mt-4">{results.map(result => <ListGroup.Item key={result.videoId}><strong>{result.videoId}</strong>: {result.status}{result.error ? ` - ${result.error}` : ''}</ListGroup.Item>)}</ListGroup> : null}
    </>
  );
};

export default ImportVideo;
