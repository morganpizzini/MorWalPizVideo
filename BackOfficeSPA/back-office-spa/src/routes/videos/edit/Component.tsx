import React from 'react';
import { useLoaderData, Form } from 'react-router';
import { Card, Row, Col, Form as BootstrapForm, Button } from 'react-bootstrap';
import PageHeader from '@components/PageHeader';
import { Match } from '@models/video/types';

const Component: React.FC = () => {
  const { match } = useLoaderData() as { match: Match };

  return (
    <>
      <PageHeader title={`Edit Video: ${match.title}`} />
      
      <div className="mb-3">
        <Button 
          variant="outline-secondary"
          onClick={() => window.location.href = `/videos/${match.id}`}
        >
          ← Back to Details
        </Button>
      </div>

      <Row>
        <Col md={8}>
          <Card>
            <Card.Header>
              <h5>Edit Video Information</h5>
            </Card.Header>
            <Card.Body>
              <Form method="post">
                <Row className="mb-3">
                  <Col sm={3}>
                    <BootstrapForm.Label htmlFor="title">Title</BootstrapForm.Label>
                  </Col>
                  <Col sm={9}>
                    <BootstrapForm.Control
                      type="text"
                      id="title"
                      name="title"
                      defaultValue={match.title}
                      required
                    />
                  </Col>
                </Row>

                <Row className="mb-3">
                  <Col sm={3}>
                    <BootstrapForm.Label htmlFor="description">Description</BootstrapForm.Label>
                  </Col>
                  <Col sm={9}>
                    <BootstrapForm.Control
                      as="textarea"
                      rows={3}
                      id="description"
                      name="description"
                      defaultValue={match.description || ''}
                    />
                  </Col>
                </Row>

                <Row className="mb-3">
                  <Col sm={3}>
                    <BootstrapForm.Label htmlFor="url">URL</BootstrapForm.Label>
                  </Col>
                  <Col sm={9}>
                    <BootstrapForm.Control
                      type="url"
                      id="url"
                      name="url"
                      defaultValue={match.url}
                    />
                  </Col>
                </Row>

                <Row className="mb-3">
                  <Col sm={3}>
                    <BootstrapForm.Label htmlFor="category">Category</BootstrapForm.Label>
                  </Col>
                  <Col sm={9}>
                    <BootstrapForm.Control
                      type="text"
                      id="category"
                      name="category"
                      defaultValue={match.category}
                      required
                    />
                  </Col>
                </Row>

                <Row className="mb-3">
                  <Col sm={3}>
                    <BootstrapForm.Label htmlFor="matchType">Match Type</BootstrapForm.Label>
                  </Col>
                  <Col sm={9}>
                    <BootstrapForm.Select
                      id="matchType"
                      name="matchType"
                      defaultValue={match.matchType.toString()}
                    >
                      <option value="0">Single Video</option>
                      <option value="1">Collection</option>
                    </BootstrapForm.Select>
                  </Col>
                </Row>

                <Row className="mb-3">
                  <Col sm={3}>
                    <BootstrapForm.Label htmlFor="thumbnailVideoId">Thumbnail Video ID</BootstrapForm.Label>
                  </Col>
                  <Col sm={9}>
                    <BootstrapForm.Control
                      type="text"
                      id="thumbnailVideoId"
                      name="thumbnailVideoId"
                      defaultValue={match.thumbnailVideoId}
                    />
                  </Col>
                </Row>

                <div className="d-flex justify-content-end gap-2">
                  <Button 
                    type="button" 
                    variant="secondary"
                    onClick={() => window.location.href = `/videos/${match.id}`}
                  >
                    Cancel
                  </Button>
                  <Button type="submit" variant="primary">
                    Save Changes
                  </Button>
                </div>
              </Form>
            </Card.Body>
          </Card>
        </Col>
        
        <Col md={4}>
          <Card>
            <Card.Header>
              <h5>Current Information</h5>
            </Card.Header>
            <Card.Body>
              <div className="mb-2">
                <strong>Match ID:</strong><br />
                <code>{match.matchId}</code>
              </div>
              <div className="mb-2">
                <strong>Current Title:</strong><br />
                {match.title}
              </div>
              <div className="mb-2">
                <strong>Associated Videos:</strong><br />
                {match.videoRefs?.length || 0} video(s)
              </div>
              <div className="mb-2">
                <strong>Current Category:</strong><br />
                <span className="badge bg-secondary">{match.category}</span>
              </div>
            </Card.Body>
          </Card>

          <Card className="mt-3">
            <Card.Header>
              <h5>Note</h5>
            </Card.Header>
            <Card.Body>
              <p className="small text-muted">
                This form allows you to edit the basic match information. 
                To modify associated videos or add/remove video references, 
                use the specific video management tools.
              </p>
            </Card.Body>
          </Card>
        </Col>
      </Row>
    </>
  );
};

export default Component;
