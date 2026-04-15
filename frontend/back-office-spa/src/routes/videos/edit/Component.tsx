import React, { useState } from 'react';
import { useLoaderData, Form } from 'react-router';
import { Card, Row, Col, Form as BootstrapForm, Button, Badge, Table } from 'react-bootstrap';
import PageHeader from '@components/PageHeader';
import { Match, VideoRef, CategoryRef } from '@morwalpizvideo/models';
import VideoRefEditModal from '@components/VideoRefEditModal';

const Component: React.FC = () => {
  const { match, categories } = useLoaderData() as { match: Match; categories: CategoryRef[] };
  const [showModal, setShowModal] = useState(false);
  const [selectedVideoRef, setSelectedVideoRef] = useState<VideoRef | null>(null);
  const [videoRefs, setVideoRefs] = useState<VideoRef[]>(match.videoRefs || []);
  const [selectedCategories, setSelectedCategories] = useState<string[]>(
    match.categories?.map(c => c.id) || []
  );

  const handleCategoryChange = (categoryId: string) => {
    setSelectedCategories(prev =>
      prev.includes(categoryId)
        ? prev.filter(id => id !== categoryId)
        : [...prev, categoryId]
    );
  };

  const handleEditVideoRef = (videoRef: VideoRef) => {
    setSelectedVideoRef(videoRef);
    setShowModal(true);
  };

  const handleSaveVideoRef = (updatedVideoRef: VideoRef) => {
    setVideoRefs(videoRefs.map(ref =>
      ref.youtubeId === updatedVideoRef.youtubeId ? updatedVideoRef : ref
    ));
    setShowModal(false);
    setSelectedVideoRef(null);
  };

  const handleSetThumbnail = (youtubeId: string) => {
    // Update thumbnail via form submission or state management
    const thumbnailInput = document.getElementById('thumbnailVideoId') as HTMLInputElement;
    if (thumbnailInput) {
      thumbnailInput.value = youtubeId;
    }
  };

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
                      type="text"
                      id="url"
                      name="url"
                      defaultValue={match.url}
                    />
                  </Col>
                </Row>

                <Row className="mb-3">
                  <Col sm={3}>
                    <BootstrapForm.Label>Categories</BootstrapForm.Label>
                  </Col>
                  <Col sm={9}>
                    <input type="hidden" name="categories" value={JSON.stringify(selectedCategories)} />
                    {categories && categories.length > 0 ? (
                      <div className="d-flex flex-column gap-2">
                        {categories.map((category) => (
                          <BootstrapForm.Check
                            key={category.categoryId}
                            type="checkbox"
                            id={`category-${category.categoryId}`}
                            label={category.title}
                            checked={selectedCategories.includes(category.categoryId)}
                            onChange={() => handleCategoryChange(category.categoryId)}
                          />
                        ))}
                      </div>
                    ) : (
                      <p className="text-muted small">No categories available</p>
                    )}
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
                      defaultValue={match.contentType.toString()}
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

          {videoRefs && videoRefs.length > 0 && (
            <Card className="mt-3">
              <Card.Header>
                <h5>Manage Video References ({videoRefs.length})</h5>
              </Card.Header>
              <Card.Body>
                <Table striped bordered hover size="sm">
                  <thead>
                    <tr>
                      <th>YouTube ID</th>
                      <th>Categories</th>
                      <th>Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {videoRefs.map((videoRef, index) => (
                      <tr key={index}>
                        <td>
                          <code className="text-primary">{videoRef.youtubeId}</code>
                          {videoRef.youtubeId === match.thumbnailVideoId && (
                            <Badge bg="success" className="ms-2">Thumbnail</Badge>
                          )}
                        </td>
                        <td>
                          <div className="d-flex gap-1 flex-wrap">
                            {videoRef.categories && videoRef.categories.length > 0 ? (
                              videoRef.categories.map((cat, catIdx) => (
                                <Badge key={catIdx} bg="secondary">{cat.title}</Badge>
                              ))
                            ) : (
                              <span className="text-muted small">No categories</span>
                            )}
                          </div>
                        </td>
                        <td>
                          <div className="d-flex gap-1">
                            <Button
                              variant="outline-primary"
                              size="sm"
                              onClick={() => handleEditVideoRef(videoRef)}
                            >
                              Edit
                            </Button>
                            <Button
                              variant="outline-info"
                              size="sm"
                              onClick={() => handleSetThumbnail(videoRef.youtubeId)}
                              disabled={videoRef.youtubeId === match.thumbnailVideoId}
                            >
                              Set Thumbnail
                            </Button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </Table>
                <p className="small text-muted mb-0">
                  Changes to VideoRefs are saved immediately when you click "Save Changes" above.
                </p>
              </Card.Body>
            </Card>
          )}
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
                <strong>Current Categories:</strong><br />
                <div className="d-flex gap-1 flex-wrap">
                  {match.categories && match.categories.length > 0 ? (
                    match.categories.map((cat, idx) => (
                      <Badge key={idx} bg="secondary">{cat.title}</Badge>
                    ))
                  ) : (
                    <em className="text-muted">No categories</em>
                  )}
                </div>
              </div>
            </Card.Body>
          </Card>

          <Card className="mt-3">
            <Card.Header>
              <h5>Note</h5>
            </Card.Header>
            <Card.Body>
              <p className="small text-muted">
                Use the VideoRefs section to edit categories for each individual video reference.
                Click "Edit" next to a video to modify its categories using a modal dialog.
              </p>
            </Card.Body>
          </Card>
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
