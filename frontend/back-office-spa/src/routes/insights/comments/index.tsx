import { useEffect, useState, type FormEvent } from 'react';
import { Alert, Button, Form } from 'react-bootstrap';
import { Link } from 'react-router';
import { get, endpoints, insightsTopicsApi } from '@morwalpizvideo/services';
import type { Channel, InsightTopic, AnalyzeInsightCommentsResponse } from '@morwalpizvideo/models';
import { InsightCommentSourceType, InsightSourceKind } from '@morwalpizvideo/models';

export default function InsightCommentsPage() {
  const [topics, setTopics] = useState<InsightTopic[]>([]);
  const [channels, setChannels] = useState<Channel[]>([]);
  const [topicId, setTopicId] = useState('');
  const [sourceType, setSourceType] = useState(InsightCommentSourceType.StoredChannel);
  const [sourceKind, setSourceKind] = useState(InsightSourceKind.ShortContent);
  const [channelId, setChannelId] = useState('');
  const [videoId, setVideoId] = useState('');
  const [commentsNumber, setCommentsNumber] = useState(20);
  const [result, setResult] = useState<AnalyzeInsightCommentsResponse | null>(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    void Promise.all([insightsTopicsApi.getAll(), get(endpoints.CHANNELS_ACCESSIBLE)]).then(([loadedTopics, loadedChannels]) => {
      setTopics(loadedTopics);
      setChannels((loadedChannels as Channel[]) ?? []);
      setTopicId(loadedTopics[0]?.id ?? '');
      setChannelId((loadedChannels as Channel[])[0]?.channelId ?? '');
    }).catch(() => setError('Unable to load Insights sources.')).finally(() => setLoading(false));
  }, []);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(''); setResult(null);
    if (!topicId) return setError('Select a topic.');
    if (sourceType !== InsightCommentSourceType.DirectVideoId && !channelId) return setError('Select a stored channel.');
    if (sourceType !== InsightCommentSourceType.StoredChannel && !videoId.trim()) return setError('Enter a YouTube video ID.');
    setSubmitting(true);
    try { setResult(await insightsTopicsApi.analyzeComments(topicId, { sourceType, sourceKind, channelId: sourceType !== InsightCommentSourceType.DirectVideoId ? channelId : undefined, videoId: sourceType === InsightCommentSourceType.StoredChannel ? undefined : videoId.trim(), commentsNumber })); }
    catch (caught) { setError(caught instanceof Error ? caught.message : 'Comment analysis failed.'); }
    finally { setSubmitting(false); }
  }

  if (loading) return <p>Loading comment sources...</p>;
  return <div className="rbac-form-width">
    <h1 className="h3">YouTube comment analysis</h1>
    {error ? <Alert variant="danger">{error}</Alert> : null}
    <Form onSubmit={submit} className="d-grid gap-3">
      <Form.Group><Form.Label htmlFor="insight-topic">Topic</Form.Label><Form.Select id="insight-topic" value={topicId} onChange={event => setTopicId(event.target.value)}>{topics.map(topic => <option key={topic.id} value={topic.id}>{topic.title}</option>)}</Form.Select></Form.Group>
      <div><Link className="btn btn-outline-secondary btn-sm" to="/insights/create?returnTo=%2Finsights%2Fcomments">Create a topic</Link></div>
      <Form.Group><Form.Label htmlFor="insight-source">Source</Form.Label><Form.Select id="insight-source" value={sourceType} onChange={event => setSourceType(Number(event.target.value) as InsightCommentSourceType)}><option value={InsightCommentSourceType.StoredChannel}>Stored channel</option><option value={InsightCommentSourceType.StoredVideo}>Stored video</option><option value={InsightCommentSourceType.DirectVideoId}>Direct video ID</option></Form.Select></Form.Group>
      <Form.Group><Form.Label htmlFor="insight-source-kind">Video content type</Form.Label><Form.Select id="insight-source-kind" value={sourceKind} onChange={event => setSourceKind(Number(event.target.value) as InsightSourceKind)}><option value={InsightSourceKind.Content}>Long-form / context-heavy video</option><option value={InsightSourceKind.ShortContent}>Short-form video</option></Form.Select></Form.Group>
      {sourceType !== InsightCommentSourceType.DirectVideoId ? <Form.Group><Form.Label htmlFor="insight-channel">Channel</Form.Label><Form.Select id="insight-channel" value={channelId} onChange={event => { setChannelId(event.target.value); setVideoId(''); }}>{channels.map(channel => <option key={channel.channelId} value={channel.channelId}>{channel.channelName}</option>)}</Form.Select></Form.Group> : null}
      {sourceType === InsightCommentSourceType.StoredVideo ? <Form.Group><Form.Label htmlFor="insight-video">YouTube video ID</Form.Label><Form.Select id="insight-video" value={videoId} onChange={event => setVideoId(event.target.value)} required><option value="">Select a stored video</option>{(channels.find(channel => channel.channelId === channelId)?.videos ?? []).map(video => <option key={video.videoId} value={video.videoId}>{video.title || video.videoId}</option>)}</Form.Select></Form.Group> : null}
      {sourceType === InsightCommentSourceType.DirectVideoId ? <Form.Group><Form.Label htmlFor="insight-direct-video">Direct YouTube video ID</Form.Label><Form.Control id="insight-direct-video" value={videoId} onChange={event => setVideoId(event.target.value)} required /></Form.Group> : null}
      {sourceType === InsightCommentSourceType.StoredVideo ? <Form.Text className="text-muted">The video is analyzed through YouTube; stored channel metadata is used when available.</Form.Text> : null}
      <Form.Group><Form.Label htmlFor="insight-comments-number">Comments per video</Form.Label><Form.Control id="insight-comments-number" type="number" min={1} max={100} value={commentsNumber} onChange={event => setCommentsNumber(Number(event.target.value))} required /></Form.Group>
      <div><Button type="submit" disabled={submitting || topics.length === 0}>{submitting ? 'Analyzing...' : 'Analyze comments'}</Button></div>
    </Form>
    {result ? <Alert variant="success" className="mt-3">Analyzed {result.commentsAnalyzed} comments across {result.videosProcessed} video(s). {result.createdNewsItemIds.length} insight(s) created.<br /><Link to={`/insights/${topicId}`}>View persisted insights for this topic</Link></Alert> : null}
    {!result && !submitting && topics.length === 0 ? <Alert variant="info" className="mt-3">Create a topic before analyzing comments.</Alert> : null}
  </div>;
}
