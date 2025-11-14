import React from 'react';
import { useLoaderData, Link } from 'react-router';
import { Card, Row, Col, Badge, Button } from 'react-bootstrap';
import PageHeader from '@components/PageHeader';
import { Match, MatchType } from '@models/video/types';

const Component: React.FC = () => {
  const { match } = useLoaderData() as { match: Match };

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
                <Col sm={3}><strong>Category:</strong></Col>
                <Col sm={9}>
                  <Badge bg="secondary">{match.category}</Badge>
                </Col>
              </Row>
              <hr />
              <Row>
                <Col sm={3}><strong>Match Type:</strong></Col>
                <Col sm={9}>
                  <Badge bg={match.matchType === MatchType.SingleVideo ? "info" : "warning"}>
                    {match.matchType === MatchType.SingleVideo ? "Single Video" : "Collection"}
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
                      <div><strong>YouTube ID:</strong></div>
                      <code className="text-primary">{videoRef.youtubeId}</code>
                      <div className="mt-1">
                        <Badge bg="secondary" className="me-1">{videoRef.category}</Badge>
                        {videoRef.youtubeId === match.thumbnailVideoId && (
                          <Badge bg="success">Thumbnail</Badge>
                        )}
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
    </>
  );
};

export default Component;
