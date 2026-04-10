import React, { useState, useEffect } from 'react';
import { Card, Form, Button, Modal } from 'react-bootstrap';
import { useLoaderData, useFetcher, useNavigate } from 'react-router';
import { InsightNewsItem, InsightNewsStatus } from '@morwalpizvideo/models';
import PageHeader from '@components/PageHeader';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';

const InsightNewsReview: React.FC = () => {
  const newsItem = useLoaderData<InsightNewsItem>();
  const [starRating, setStarRating] = useState(newsItem.starRating);
  const [status, setStatus] = useState(newsItem.status);
  const [showModal, setShowModal] = useState(false);
  const navigate = useNavigate();
  const toast = useToast();
  const fetcher = useFetcher();
  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;
  const result =
    fetcher.data != undefined &&
    (fetcher.data.errors == undefined || fetcher.data.errors.length == 0)
      ? fetcher.data
      : null;

  useEffect(() => {
    if (!result) return;
    setShowModal(false);

    if (result.success) {
      toast.show('Success', 'News item reviewed successfully', { variant: 'success' });
      navigate(`/insights/${newsItem.topicId}`);
    }
  }, [result, navigate]);

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    setShowModal(true);
  };

  const confirmSubmit = () => {
    fetcher.submit(
      {
        starRating: starRating.toString(),
        status: status.toString(),
      },
      {
        method: 'post',
        action: location.pathname,
      }
    );
  };

  const renderStars = () => {
    return Array.from({ length: 5 }, (_, i) => (
      <span
        key={i}
        onClick={() => setStarRating(i + 1)}
        style={{ cursor: 'pointer', fontSize: '2rem' }}
        className={i < starRating ? 'text-warning' : 'text-muted'}
      >
        ★
      </span>
    ));
  };

  return (
    <>
      <PageHeader title="Review News Item" />
      <GenericErrorList errors={errors?.generics} />

      <Card className="mb-3">
        <Card.Header as="h5">News Details</Card.Header>
        <Card.Body>
          <h4>{newsItem.title}</h4>
          <p className="lead">{newsItem.summary}</p>
          <div className="mb-3">
            <strong>Source:</strong>{' '}
            <a href={newsItem.sourceUrl} target="_blank" rel="noopener noreferrer">
              {newsItem.sourceName}
            </a>
          </div>
          <div className="mb-3">
            <strong>Discovered:</strong> {new Date(newsItem.discoveredAt).toLocaleString()}
          </div>
          <div className="mb-3">
            <strong>AI Relevance Score:</strong> {newsItem.aiRelevanceScore.toFixed(2)}
          </div>
        </Card.Body>
      </Card>

      <Form onSubmit={handleSubmit}>
        <Card className="mb-3">
          <Card.Header as="h5">Your Review</Card.Header>
          <Card.Body>
            <Form.Group className="mb-3">
              <Form.Label>Star Rating</Form.Label>
              <div>{renderStars()}</div>
              <Form.Text className="text-muted">
                Click to rate (1-5 stars) - helps improve news discovery algorithm
              </Form.Text>
            </Form.Group>

            <Form.Group>
              <Form.Label>Status</Form.Label>
              <div>
                <Form.Check
                  type="radio"
                  label="Pending - Review later"
                  name="status"
                  value={InsightNewsStatus.Pending}
                  checked={status === InsightNewsStatus.Pending}
                  onChange={e => setStatus(Number(e.target.value))}
                />
                <Form.Check
                  type="radio"
                  label="Accept - Worth analyzing"
                  name="status"
                  value={InsightNewsStatus.Accepted}
                  checked={status === InsightNewsStatus.Accepted}
                  onChange={e => setStatus(Number(e.target.value))}
                />
                <Form.Check
                  type="radio"
                  label="Reject - Not relevant"
                  name="status"
                  value={InsightNewsStatus.Rejected}
                  checked={status === InsightNewsStatus.Rejected}
                  onChange={e => setStatus(Number(e.target.value))}
                />
              </div>
            </Form.Group>
          </Card.Body>
        </Card>

        <div className="d-flex gap-2">
          <Button variant="success" type="submit" disabled={busy}>
            Submit Review
          </Button>
          <Button
            variant="secondary"
            onClick={() => navigate(`/insights/${newsItem.topicId}`)}
          >
            Cancel
          </Button>
        </div>
      </Form>

      <Modal show={showModal} onHide={() => setShowModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>Confirm Review</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <p>Are you sure you want to submit this review?</p>
          <p>
            <strong>Star Rating:</strong> {starRating} / 5
          </p>
          <p>
            <strong>Status:</strong> {InsightNewsStatus[status]}
          </p>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowModal(false)}>
            Cancel
          </Button>
          <Button variant="success" onClick={confirmSubmit} disabled={busy}>
            Confirm
          </Button>
        </Modal.Footer>
      </Modal>
    </>
  );
};

export default InsightNewsReview;