import React, { useState } from 'react';
import { useLoaderData, Link, useRevalidator } from 'react-router';
import { Card, Row, Col, Badge, Button } from 'react-bootstrap';
import PageHeader from '@components/PageHeader';
import { Match, ContentType, VideoRef } from '@morwalpizvideo/models';
import VideoRefEditModal from '@components/VideoRefEditModal';
import * as apiService from '@services/apiService';

interface Category {
  categoryId: string;
  title: string;
}

interface LoaderData {
  match: Match;
  categories: Category[];
}

const Component: React.FC = () => {
  const { match, categories } = useLoaderData() as LoaderData;
  const revalidator = useRevalidator();
  const [showModal, setShowModal] = useState(false);
  const [selectedVideoRef, setSelectedVideoRef] = useState<VideoRef | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  const handleEditVideoRef = (videoRef: VideoRef) => {
    setSelectedVideoRef(videoRef);
    setShowModal(true);
  };

  const handleSaveVideoRef = async (updatedVideoRef: VideoRef) => {
    setIsSaving(true);
    try {
      // Update the video ref via API
      await apiService.patch(`api/videos/${match.id}/videorefs/${updatedVideoRef.youtubeId}`, {
        categories: updatedVideoRef.categories
      });
      setShowModal(false);
      setSelectedVideoRef(null);
      // Revalidate to refresh the data
      revalidator.revalidate();
    } catch (error) {
      console.error('Failed to update video ref:', error);
      alert('Failed to update video reference. Please try again.');
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <>
      <PageHeader title={`Video Details: ${match.title}`} />

      <div className="mb-3">
        <Button
          variant="outline-secondary"
          onClick={() => window.location.href = '/videos'}
        >
          ← Back to Videos
        </Button>
        <Button
          variant="primary"
          className="ms-2"
          onClick={() => window.location.href = `/videos/${match.id}/edit`}
        >
          Edit Video
        </Button>
      </div>

      <Row>
        <Col md={8}>
          <Card>
            <Card.Header>
              <h5>General Information</h5>
            </Card.Header>
            <Card.Body>
              <Row>
                <Col sm={3}><strong>Match ID:</strong></Col>
                <Col sm={9}><code>{match.matchId}</code></Col>
              </Row>
              <hr />
              <Row>
                <Col sm={3}><strong>Title:</strong></Col>
                <Col sm={9}>{match.title}</Col>
              </Row>
              <hr />
              <Row>
                <Col sm={3}><strong>Description:</strong></Col>
                <Col sm={9}>{match.description || <em>No description</em>}</Col>
              </Row>
              <hr />
              <Row>
                <Col sm={3}><strong>URL:</strong></Col>
                <Col sm={9}>
                  {match.url ? (
                    <a href={match.url} target="_blank" rel="noopener noreferrer">
                      {match.url}
                    </a>
                  ) : (
                    <em>No URL</em>
                  )}
                </Col>
              </Row>
              <hr />
              <Row>
                <Col sm={3}><strong>Categories:</strong></Col>
                <Col sm={9}>
                  <div className="d-flex gap-1 flex-wrap">
                    {match.categories && match.categories.length > 0 ? (
                      match.categories.map((cat, idx) => (
                        <Badge key={idx} bg="secondary">{cat.title}</Badge>
                      ))
                    ) : (
                      <em className="text-muted">No categories</em>
                    )}
                  </div>
                </Col>
              </Row>
              <hr />
              <Row>
                <Col sm={3}><strong>Match Type:</strong></Col>
                <Col sm={9}>
                  <Badge bg={match.contentType === ContentType.SingleVideo ? "info" : "warning"}>
                    {match.contentType === ContentType.SingleVideo ? "Single Video" : "Collection"}
                  </Badge>
                </Col>
              </Row>
              <hr />
              <Row>
                <Col sm={3}><strong>Thumbnail Video:</strong></Col>
                <Col sm={9}><code>{match.thumbnailVideoId}</code></Col>
              </Row>
              {match.creationDateTime && (
                <>
                  <hr />
                  <Row>
                    <Col sm={3}><strong>Created:</strong></Col>
                    <Col sm={9}>{new Date(match.creationDateTime).toLocaleString()}</Col>
                  </Row>
                </>
              )}
            </Card.Body>
          </Card>
        </Col>

        <Col md={4}>
          <Card>
            <Card.Header>
              <h5>Associated Videos ({match.videoRefs?.length || 0})</h5>
            </Card.Header>
            <Card.Body>
              {match.videoRefs && match.videoRefs.length > 0 ? (
                <div className="d-flex flex-column gap-2">
                  {match.videoRefs.map((videoRef, index) => (
                    <div key={index} className="p-2 border rounded">
                      <div className="d-flex justify-content-between align-items-start">
                        <div className="flex-grow-1">
                          <div><strong>YouTube ID:</strong></div>
                          <code className="text-primary">{videoRef.youtubeId}</code>
                          <div className="mt-1 d-flex gap-1 flex-wrap">
                            {videoRef.categories && videoRef.categories.length > 0 ? (
                              videoRef.categories.map((cat, catIdx) => (
                                <Badge key={catIdx} bg="secondary">{cat.title}</Badge>
                              ))
                            ) : (
                              <span className="text-muted small">No categories</span>
                            )}
                            {videoRef.youtubeId === match.thumbnailVideoId && (
                              <Badge bg="success">Thumbnail</Badge>
                            )}
                          </div>
                        </div>
                        <Button
                          variant="outline-primary"
                          size="sm"
                          onClick={() => handleEditVideoRef(videoRef)}
                        >
                          Edit
                        </Button>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-muted">No associated videos</p>
              )}
            </Card.Body>
          </Card>

          {match.videos && match.videos.length > 0 && (
            <Card className="mt-3">
              <Card.Header>
                <h5>Video Details</h5>
              </Card.Header>
              <Card.Body>
                {match.videos.map((video, index) => (
                  <div key={index} className="mb-3 p-2 border rounded">
                    <h6>{video.title}</h6>
                    {video.description && <p className="small text-muted">{video.description}</p>}
                    <div className="small">
                      {video.views && <span>Views: {video.views.toLocaleString()} </span>}
                      {video.likes && <span>Likes: {video.likes.toLocaleString()} </span>}
                      {video.duration && <span>Duration: {video.duration}</span>}
                    </div>
                    {video.publishedAt && (
                      <div className="small text-muted">
                        Published: {new Date(video.publishedAt).toLocaleDateString()}
                      </div>
                    )}
                  </div>
                ))}
              </Card.Body>
            </Card>
          )}
        </Col>
      </Row>

      <VideoRefEditModal
        show={showModal}
        videoRef={selectedVideoRef}
        onHide={() => setShowModal(false)}
        onSave={handleSaveVideoRef}
        availableCategories={categories}
      />
    </>
  );
};

export default Component;
