import { useResolvedLoaderData } from '@/router/asyncData';
import React, { useEffect, useRef, useState } from 'react';
import {} from 'react-router';
import { Card, Row, Col, Badge, Button, Form } from 'react-bootstrap';
import PageHeader from '@components/PageHeader';
import { Match, ContentType, Channel, LinkType } from '@morwalpizvideo/models';
import type { MatchImage } from '@morwalpizvideo/models';
import { deleteVideoImage, uploadVideoImages } from '@morwalpizvideo/services';
import { composeShortLinkUrl } from '@components/VideoList';
import { useToast } from '@components/ToastNotification/ToastContext';

interface Category {
  categoryId: string;
  title: string;
}

interface LoaderData {
  match: Match;
  channels: Channel[];
  categories: Category[];
}

const Component: React.FC = () => {
  const { match, channels } = useResolvedLoaderData() as LoaderData;
  const channel = channels.find(candidate => candidate.channelId === match.ownerChannelId);
  const toast = useToast();
  const [images, setImages] = useState<MatchImage[]>(match.images ?? []);
  const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
  const [imageError, setImageError] = useState('');
  const [imageBusy, setImageBusy] = useState(false);
  const imageInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    setImages(match.images ?? []);
    setSelectedFiles([]);
    setImageError('');
  }, [match.id, match.images]);

  const handleFileSelection = (event: React.ChangeEvent<HTMLInputElement>) => {
    setSelectedFiles(Array.from(event.target.files ?? []));
    setImageError('');
  };

  const uploadSelectedFiles = async () => {
    if (!selectedFiles.length) return;
    setImageBusy(true);
    setImageError('');
    try {
      const response = await uploadVideoImages(match.id, selectedFiles);
      setImages(response.images ?? []);
      setSelectedFiles([]);
      if (imageInputRef.current) imageInputRef.current.value = '';
      toast.show('Success', 'Video images uploaded successfully', { variant: 'success' });
    } catch {
      setImageError('Unable to upload video images.');
    } finally {
      setImageBusy(false);
    }
  };

  const deleteImage = async (imageIndex: number) => {
    setImageBusy(true);
    setImageError('');
    try {
      const response = await deleteVideoImage(match.id, imageIndex);
      setImages(response.images ?? []);
      toast.show('Success', 'Video image deleted successfully', { variant: 'success' });
    } catch {
      setImageError('Unable to delete the video image.');
    } finally {
      setImageBusy(false);
    }
  };

  return (
    <>
      <PageHeader title={`Video Details: ${match.title}`} />

      <div className="mb-3">
        <Button variant="outline-secondary" onClick={() => (window.location.href = '/videos')}>
          ← Back to Videos
        </Button>
        <Button
          variant="primary"
          className="ms-2"
          onClick={() => (window.location.href = `/videos/${match.id}/edit`)}
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
                <Col sm={3}>
                  <strong>Match ID:</strong>
                </Col>
                <Col sm={9}>
                  <code>{match.id}</code>
                </Col>
              </Row>
              <hr />
              <Row>
                <Col sm={3}>
                  <strong>Title:</strong>
                </Col>
                <Col sm={9}>{match.title}</Col>
              </Row>
              <hr />
              <Row>
                <Col sm={3}>
                  <strong>Description:</strong>
                </Col>
                <Col sm={9}>{match.description || <em>No description</em>}</Col>
              </Row>
              <hr />
              <Row>
                <Col sm={3}>
                  <strong>URL:</strong>
                </Col>
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
                <Col sm={3}>
                  <strong>Categories:</strong>
                </Col>
                <Col sm={9}>
                  <div className="d-flex gap-1 flex-wrap">
                    {match.categories && match.categories.length > 0 ? (
                      match.categories.map((cat, idx) => (
                        <Badge key={idx} bg="secondary">
                          {cat.title}
                        </Badge>
                      ))
                    ) : (
                      <em className="text-muted">No categories</em>
                    )}
                  </div>
                </Col>
              </Row>
              <hr />
              <Row>
                <Col sm={3}>
                  <strong>Tags:</strong>
                </Col>
                <Col sm={9}>
                  <div className="d-flex gap-1 flex-wrap">
                    {match.tags && match.tags.length > 0 ? (
                      match.tags.map((tag, idx) => (
                        <Badge key={idx} bg="info">
                          {tag}
                        </Badge>
                      ))
                    ) : (
                      <em className="text-muted">No tags</em>
                    )}
                  </div>
                </Col>
              </Row>
              <hr />
              <Row>
                <Col sm={3}>
                  <strong>Match Type:</strong>
                </Col>
                <Col sm={9}>
                  <Badge bg={match.contentType === ContentType.SingleVideo ? 'info' : 'warning'}>
                    {match.contentType === ContentType.SingleVideo ? 'Single Video' : 'Collection'}
                  </Badge>
                </Col>
              </Row>
              <hr />
              <Row>
                <Col sm={3}>
                  <strong>Thumbnail Video:</strong>
                </Col>
                <Col sm={9}>
                  <code>{match.thumbnailVideoId}</code>
                </Col>
              </Row>
              {match.creationDateTime && (
                <>
                  <hr />
                  <Row>
                    <Col sm={3}>
                      <strong>Created:</strong>
                    </Col>
                    <Col sm={9}>{new Date(match.creationDateTime).toLocaleString()}</Col>
                  </Row>
                </>
              )}
            </Card.Body>
          </Card>
        </Col>

        <Col md={4}>
          <Card className="mb-3">
            <Card.Header>
              <h5>Associated Images ({images.length})</h5>
            </Card.Header>
            <Card.Body>
              <Form.Control
                ref={imageInputRef}
                type="file"
                accept="image/*"
                multiple
                onChange={handleFileSelection}
                disabled={imageBusy}
                aria-label="Select video images"
              />
              {selectedFiles.length > 0 && (
                <div className="small mt-2">
                  Selected: {selectedFiles.map(file => file.name).join(', ')}
                </div>
              )}
              <Button
                type="button"
                className="mt-2"
                variant="outline-primary"
                onClick={uploadSelectedFiles}
                disabled={selectedFiles.length === 0 || imageBusy}
              >
                {imageBusy ? 'Uploading...' : 'Upload selected images'}
              </Button>
              {imageError && (
                <div className="text-danger mt-2" role="alert">
                  {imageError}
                </div>
              )}
              {images.length > 0 && (
                <div className="row g-2 mt-2" aria-label="Video image previews">
                  {images.map((image, index) => (
                    <div className="col-6" key={`${image.publicUrl}-${index}`}>
                      <div className="border rounded p-1 h-100">
                        <img
                          src={image.publicUrl}
                          alt={image.altText || `Video image ${index + 1}`}
                          className="img-fluid"
                        />
                        <Button
                          type="button"
                          variant="outline-danger"
                          size="sm"
                          className="mt-1 w-100"
                          onClick={() => deleteImage(index)}
                          disabled={imageBusy}
                        >
                          Delete
                        </Button>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </Card.Body>
          </Card>
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
                          <div>
                            <strong>YouTube ID:</strong>
                          </div>
                          <code className="text-primary">{videoRef.youtubeId}</code>
                          {(() => {
                            const shortLinkUrl = composeShortLinkUrl(
                              channel?.shortLinkUrl,
                              match.shortLinks?.find(
                                link =>
                                  link.linkType === LinkType.YouTubeVideo &&
                                  link.target === videoRef.youtubeId
                              )?.code
                            );
                            return shortLinkUrl ? (
                              <div className="mt-1 d-flex align-items-center gap-2">
                                <a
                                  href={shortLinkUrl}
                                  target="_blank"
                                  rel="noopener noreferrer"
                                  className="text-truncate"
                                >
                                  {shortLinkUrl}
                                </a>
                                <Button
                                  size="sm"
                                  variant="outline-secondary"
                                  onClick={() => void navigator.clipboard.writeText(shortLinkUrl)}
                                >
                                  Copy short link
                                </Button>
                              </div>
                            ) : null;
                          })()}
                          <div className="mt-1 d-flex gap-1 flex-wrap">
                            {videoRef.categories && videoRef.categories.length > 0 ? (
                              videoRef.categories.map((cat, catIdx) => (
                                <Badge key={catIdx} bg="secondary">
                                  {cat.title}
                                </Badge>
                              ))
                            ) : (
                              <span className="text-muted small">No categories</span>
                            )}
                            {videoRef.youtubeId === match.thumbnailVideoId && (
                              <Badge bg="success">Thumbnail</Badge>
                            )}
                          </div>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-muted">No associated videos</p>
              )}
            </Card.Body>
          </Card>
        </Col>
      </Row>
    </>
  );
};

export default Component;
