import React, { useState } from 'react';
import { Card, Badge, Button, Tabs, Tab, Form } from 'react-bootstrap';
import { Link, useLoaderData, useFetcher } from 'react-router';
import { InsightTopic, InsightNewsItem, InsightContentPlan, InsightNewsStatus, InsightSourceKind, ContentPlanType } from '@morwalpizvideo/models';
import { insightsContentPlansApi, insightsTopicsApi } from '@morwalpizvideo/services';
import PageHeader from '@components/PageHeader';
import { useToast } from '@components/ToastNotification/ToastContext';

interface LoaderData {
  topic: InsightTopic;
  newsItems: InsightNewsItem[];
  contentPlans: InsightContentPlan[];
}

const InsightTopicDetail: React.FC = () => {
  const { topic, newsItems, contentPlans } = useLoaderData<LoaderData>();
  const [scanning, setScanning] = useState(false);
  const [selectedNewsIds, setSelectedNewsIds] = useState<string[]>([]);
  const [contentType, setContentType] = useState(ContentPlanType.Article);
  const [targetPlatforms, setTargetPlatforms] = useState<string[]>([]);
  const [generatingPlan, setGeneratingPlan] = useState(false);
  const hasCommentDerivedInsights = newsItems.some(item => item.sourceKind === InsightSourceKind.ShortContent);
  const toast = useToast();
  const fetcher = useFetcher();

  const acceptedNews = newsItems.filter(item => item.status === InsightNewsStatus.Accepted);
  const availablePlatforms = ['YouTube', 'Instagram', 'TikTok', 'Newsletter'];

  const generateContentPlan = async () => {
    if (selectedNewsIds.length === 0 || targetPlatforms.length === 0) return;
    setGeneratingPlan(true);
    try {
      await insightsContentPlansApi.generate({ topicId: topic.id, newsItemIds: selectedNewsIds, contentType, targetPlatforms });
      toast.show('Success', 'Content plan generated', { variant: 'success' });
      window.location.reload();
    } catch (error) {
      toast.show('Error', error instanceof Error ? error.message : 'Content plan generation failed', { variant: 'danger' });
    } finally {
      setGeneratingPlan(false);
    }
  };

  const downloadCsv = async () => {
    try {
      const blob = await insightsTopicsApi.exportCsv(topic.id);
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `insight-topic-${topic.id}.csv`;
      link.click();
      URL.revokeObjectURL(url);
    } catch (error) {
      toast.show('Error', error instanceof Error ? error.message : 'CSV export failed', { variant: 'danger' });
    }
  };

  const handleScanNews = async () => {
    setScanning(true);
    fetcher.submit(
      { topicId: topic.id },
      { method: 'post', action: `/insights/${topic.id}/scan-news` }
    );
  };

  React.useEffect(() => {
    if (fetcher.state === 'idle' && fetcher.data) {
      setScanning(false);
      if (fetcher.data.success) {
        toast.show('Success', 'News scan completed', { variant: 'success' });
      } else {
        toast.show('Error', fetcher.data.errors?.generics?.[0] || 'News scan failed', { variant: 'danger' });
      }
    }
  }, [fetcher.state, fetcher.data]);

  const getStatusBadge = (status: InsightNewsStatus) => {
    const variants: Record<InsightNewsStatus, string> = {
      [InsightNewsStatus.Pending]: 'warning',
      [InsightNewsStatus.Accepted]: 'success',
      [InsightNewsStatus.Rejected]: 'danger',
      [InsightNewsStatus.Generated]: 'info',
      [InsightNewsStatus.AutoDetected]: 'primary',
    };
    const labels: Record<InsightNewsStatus, string> = {
      [InsightNewsStatus.Pending]: 'Pending',
      [InsightNewsStatus.Accepted]: 'Accepted',
      [InsightNewsStatus.Rejected]: 'Rejected',
      [InsightNewsStatus.Generated]: 'Generated',
      [InsightNewsStatus.AutoDetected]: 'Auto-Detected',
    };
    return <Badge bg={variants[status]}>{labels[status]}</Badge>;
  };

  const renderStars = (rating: number) => {
    return Array.from({ length: 5 }, (_, i) => (
      <span key={i} className={i < rating ? 'text-warning' : 'text-muted'}>
        ★
      </span>
    ));
  };

  return (
    <>
      <PageHeader title="Topic Details" />

      <Card className="mb-3">
        <Card.Header as="h5">Basic Information</Card.Header>
        <Card.Body>
          <div className="mb-3">
            <strong>ID:</strong>
            <p className="mb-0">{topic.id}</p>
          </div>
          <div className="mb-3">
            <strong>Title:</strong>
            <p className="mb-0">{topic.title}</p>
          </div>
          <div className="mb-3">
            <strong>Description:</strong>
            <p className="mb-0">{topic.description}</p>
          </div>
          <div className="mb-3">
            <strong>Seed Arguments:</strong>
            <div className="d-flex gap-1 flex-wrap">
              {topic.seedArguments?.map((arg, idx) => (
                <Badge key={idx} bg="primary">
                  {arg}
                </Badge>
              )) || <span className="text-muted">No arguments</span>}
            </div>
          </div>
          <div className="mb-3">
            <strong>Preferred Sources:</strong>
            <div className="d-flex gap-1 flex-wrap">
              {topic.preferredSources?.map((source, idx) => (
                <Badge key={idx} bg="secondary">
                  {source}
                </Badge>
              )) || <span className="text-muted">No sources</span>}
            </div>
          </div>
        </Card.Body>
      </Card>

      <div className="d-flex gap-2 mb-3">
        <Link to={`/insights/${topic.id}/edit`} className="btn btn-primary">
          Edit
        </Link>
        <Link to="/insights" className="btn btn-secondary">
          Back to List
        </Link>
        <Button variant="outline-primary" onClick={downloadCsv}>Download CSV</Button>
      </div>

      <Tabs defaultActiveKey="news">
        <Tab eventKey="news" title={`News Items (${newsItems.length})`}>
          <Card>
            <Card.Header className="d-flex justify-content-between align-items-center">
              <span>Discovered News</span>
              {!hasCommentDerivedInsights && (
                <Button
                  variant="primary"
                  size="sm"
                  onClick={handleScanNews}
                  disabled={scanning}
                >
                  {scanning ? 'Scanning...' : 'Scan for News'}
                </Button>
              )}
            </Card.Header>
            <Card.Body>
              {acceptedNews.length > 0 && <Card className="border mb-3"><Card.Body>
                <h6>Generate a content plan from accepted news</h6>
                <Form.Group className="mb-2"><Form.Label>Accepted news items</Form.Label>{acceptedNews.map(item => <Form.Check key={item.id} type="checkbox" label={item.title} checked={selectedNewsIds.includes(item.id)} onChange={event => setSelectedNewsIds(current => event.target.checked ? [...current, item.id] : current.filter(id => id !== item.id))} />)}</Form.Group>
                <div className="d-flex gap-2 flex-wrap">
                  <Form.Select aria-label="Content type" value={contentType} onChange={event => setContentType(Number(event.target.value) as ContentPlanType)} className="w-auto"><option value={ContentPlanType.Article}>Article</option><option value={ContentPlanType.Podcast}>Podcast</option><option value={ContentPlanType.SocialPost}>Social post</option><option value={ContentPlanType.VideoScript}>Video script</option><option value={ContentPlanType.Newsletter}>Newsletter</option></Form.Select>
                  {availablePlatforms.map(platform => <Form.Check key={platform} inline type="checkbox" label={platform} checked={targetPlatforms.includes(platform)} onChange={event => setTargetPlatforms(current => event.target.checked ? [...current, platform] : current.filter(value => value !== platform))} />)}
                  <Button size="sm" onClick={generateContentPlan} disabled={generatingPlan || selectedNewsIds.length === 0 || targetPlatforms.length === 0}>{generatingPlan ? 'Generating...' : 'Generate content plan'}</Button>
                </div>
              </Card.Body></Card>}
              {newsItems.length > 0 ? (
                <div className="d-flex flex-column gap-3">
                  {newsItems.map(item => (
                    <Card key={item.id} className="border">
                      <Card.Body>
                        <div className="d-flex justify-content-between align-items-start mb-2">
                          <h6 className="mb-0">
                            {item.title}
                            {item.sourceKind === InsightSourceKind.ShortContent && (
                              <Badge bg="dark" className="ms-2">Short Content</Badge>
                            )}
                          </h6>
                          {item.status === InsightNewsStatus.Accepted && <Form.Check aria-label={`Select ${item.title}`} checked={selectedNewsIds.includes(item.id)} onChange={event => setSelectedNewsIds(current => event.target.checked ? [...current, item.id] : current.filter(id => id !== item.id))} />}
                          {getStatusBadge(item.status)}
                        </div>
                        <p className="mb-2">{item.summary}</p>
                        <div className="d-flex justify-content-between align-items-center">
                          <div>
                            <small className="text-muted">
                              Source: <a href={item.sourceUrl} target="_blank" rel="noopener noreferrer">{item.sourceName}</a>
                            </small>
                            <br />
                            <small className="text-muted">
                              AI Score: {item.aiRelevanceScore.toFixed(2)} | Stars: {renderStars(item.starRating)}
                            </small>
                          </div>
                          <Link
                            to={`/insights/news/${item.id}`}
                            className="btn btn-sm btn-outline-primary"
                          >
                            Review
                          </Link>
                        </div>
                      </Card.Body>
                    </Card>
                  ))}
                </div>
              ) : (
                <p className="text-muted mb-0">No news items found. Try scanning for news.</p>
              )}
            </Card.Body>
          </Card>
        </Tab>

        <Tab eventKey="plans" title={`Content Plans (${contentPlans.length})`}>
          <Card>
            <Card.Header>Generated Content Plans</Card.Header>
            <Card.Body>
              {contentPlans.length > 0 ? (
                <div className="d-flex flex-column gap-3">
                  {contentPlans.map(plan => (
                    <Card key={plan.id} className="border">
                      <Card.Body>
                        <h6>{plan.title}</h6>
                        <Badge bg="info" className="mb-2">{plan.type}</Badge>
                        <p className="mb-2" style={{ whiteSpace: 'pre-line' }}>
                          {plan.outline.length > 200 ? `${plan.outline.substring(0, 200)}...` : plan.outline}
                        </p>
                        <div className="d-flex gap-2">
                          <small className="text-muted">
                            Platforms: {plan.targetPlatforms.join(', ')}
                          </small>
                          <Link
                            to={`/insights/${topic.id}`}
                            className="btn btn-sm btn-outline-primary ms-auto"
                          >
                            View Details
                          </Link>
                        </div>
                      </Card.Body>
                    </Card>
                  ))}
                </div>
              ) : (
                <p className="text-muted mb-0">No content plans generated yet.</p>
              )}
            </Card.Body>
          </Card>
        </Tab>
      </Tabs>

    </>
  );
};

export default InsightTopicDetail;