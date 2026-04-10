import React, { useState } from 'react';
import { Card, Badge, Button, Tabs, Tab } from 'react-bootstrap';
import { Link, useLoaderData, useFetcher } from 'react-router';
import { InsightTopic, InsightNewsItem, InsightContentPlan, InsightNewsStatus } from '@morwalpizvideo/models';
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
  const toast = useToast();
  const fetcher = useFetcher();

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
    };
    return <Badge bg={variants[status]}>{InsightNewsStatus[status]}</Badge>;
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

      <Tabs defaultActiveKey="news" className="mb-3">
        <Tab eventKey="news" title={`News Items (${newsItems.length})`}>
          <Card>
            <Card.Header className="d-flex justify-content-between align-items-center">
              <span>Discovered News</span>
              <Button
                variant="primary"
                size="sm"
                onClick={handleScanNews}
                disabled={scanning}
              >
                {scanning ? 'Scanning...' : 'Scan for News'}
              </Button>
            </Card.Header>
            <Card.Body>
              {newsItems.length > 0 ? (
                <div className="d-flex flex-column gap-3">
                  {newsItems.map(item => (
                    <Card key={item.id} className="border">
                      <Card.Body>
                        <div className="d-flex justify-content-between align-items-start mb-2">
                          <h6 className="mb-0">{item.title}</h6>
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
                            to={`/insights/plans/${plan.id}`}
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

      <div className="d-flex gap-2">
        <Link to={`/insights/${topic.id}/edit`} className="btn btn-primary">
          Edit
        </Link>
        <Link to="/insights" className="btn btn-secondary">
          Back to List
        </Link>
      </div>
    </>
  );
};

export default InsightTopicDetail;