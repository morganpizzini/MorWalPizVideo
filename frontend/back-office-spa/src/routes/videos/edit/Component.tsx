import React, { useEffect, useRef, useState } from 'react';
import { useLoaderData, useFetcher, useNavigate } from 'react-router';
import { Card, Row, Col, Form as BootstrapForm, Button, Badge, Table } from 'react-bootstrap';
import PageHeader from '@components/PageHeader';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import { Match, VideoRef, CategoryRef } from '@morwalpizvideo/models';
import VideoRefEditModal from '@components/VideoRefEditModal';
import TagInput from '@components/TagInput';
import { normalizeTags } from '@components/TagInput/tagRules';

type CategoryWithFallbackId = CategoryRef & { categoryId?: string };

const getCategoryId = (category: CategoryWithFallbackId): string =>
  category.id ?? category.categoryId ?? '';

const Component: React.FC = () => {
  const { match, categories, tagSuggestions } = useLoaderData() as {
    match: Match;
    categories: CategoryRef[];
    tagSuggestions?: string[];
  };
  const saveFetcher = useFetcher();
  const addFetcher = useFetcher();
  const navigate = useNavigate();
  const toast = useToast();
  const normalizedCategories = (categories as CategoryWithFallbackId[])
    .map((category): CategoryRef => ({
      id: getCategoryId(category),
      title: category.title,
    }))
    .filter(category => category.id.length > 0);

  const [showModal, setShowModal] = useState(false);
  const [selectedVideoRef, setSelectedVideoRef] = useState<VideoRef | null>(null);
  const [videoRefs, setVideoRefs] = useState<VideoRef[]>(match.videoRefs || []);
  const [newVideoRefId, setNewVideoRefId] = useState('');
  const [newVideoRefCategories, setNewVideoRefCategories] = useState<string[]>([]);
  const [addVideoRefAttempted, setAddVideoRefAttempted] = useState(false);
  const [selectedCategories, setSelectedCategories] = useState<string[]>(
    (match.categories as CategoryWithFallbackId[] | undefined)?.map(c => getCategoryId(c)).filter(Boolean) || []
  );
  const [tags, setTags] = useState<string[]>(() => normalizeTags(match.tags ?? []));
  const saveBusy = saveFetcher.state !== 'idle';
  const saveErrors = saveFetcher.data?.errors;
  const lastSaveData = useRef<unknown>(undefined);
  const lastAddData = useRef<unknown>(undefined);

  useEffect(() => {
    if (saveBusy || !saveFetcher.data || lastSaveData.current === saveFetcher.data) {
      return;
    }
    lastSaveData.current = saveFetcher.data;

    if (saveFetcher.data.success) {
      toast.show('Success', 'Video updated successfully', { variant: 'success' });
      navigate('..');
      return;
    }

    const message = saveErrors?.generics?.[0] ?? 'Unable to update video.';
    toast.show('Video update failed', message, { variant: 'danger' });
  }, [navigate, saveBusy, saveErrors, saveFetcher.data, toast]);

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    saveFetcher.submit(new FormData(event.currentTarget), {
      method: 'post',
      action: location.pathname,
    });
  };

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

  const handleNewVideoRefCategoryChange = (categoryId: string) => {
    setNewVideoRefCategories(prev =>
      prev.includes(categoryId)
        ? prev.filter(id => id !== categoryId)
        : [...prev, categoryId]
    );
  };

  const handleAddVideoRef = () => {
    setAddVideoRefAttempted(true);
    const youtubeId = newVideoRefId.trim();
    if (!youtubeId || newVideoRefCategories.length === 0) {
      return;
    }

    if (videoRefs.some(ref => ref.youtubeId === youtubeId)) {
      return;
    }

    addFetcher.submit(
      {
        _intent: 'addVideoReference',
        youtubeId,
        categories: JSON.stringify(newVideoRefCategories),
      },
      {
        method: 'post',
        action: location.pathname,
      }
    );
  };

  const isAddVideoRefDisabled =
    addFetcher.state !== 'idle' ||
    newVideoRefId.trim().length === 0 ||
    newVideoRefCategories.length === 0 ||
    videoRefs.some(ref => ref.youtubeId === newVideoRefId.trim());

  const isDuplicateNewVideoRefId =
    newVideoRefId.trim().length > 0 &&
    videoRefs.some(ref => ref.youtubeId === newVideoRefId.trim());

  const shouldShowVideoRefIdRequired = addVideoRefAttempted && newVideoRefId.trim().length === 0;
  const shouldShowVideoRefCategoriesRequired = addVideoRefAttempted && newVideoRefCategories.length === 0;

  useEffect(() => {
    if (addFetcher.state !== 'idle' || !addFetcher.data || lastAddData.current === addFetcher.data) {
      return;
    }
    lastAddData.current = addFetcher.data;

    if (addFetcher.data.success && addFetcher.data.videoRef) {
      setVideoRefs(prev => [...prev, addFetcher.data.videoRef as VideoRef]);
      setNewVideoRefId('');
      setNewVideoRefCategories([]);
      setAddVideoRefAttempted(false);
      toast.show('Success', 'Video reference added successfully', { variant: 'success' });
      return;
    }

    const message = addFetcher.data.errors?.generics?.[0] ?? 'Unable to add video reference.';
    toast.show('Video reference add failed', message, { variant: 'danger' });
  }, [addFetcher.data, addFetcher.state, toast]);

  return (
    <>
      <PageHeader title={`Edit Video: ${match.title}`} />
      <GenericErrorList errors={saveErrors?.generics} />

      <div className="mb-3">
        <Button
          variant="outline-secondary"
          onClick={() => navigate(`/videos/${match.id}`)}
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
              <BootstrapForm method="post" onSubmit={handleSubmit}>
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
                    {normalizedCategories.length > 0 ? (
                      <div className="d-flex flex-column gap-2">
                        {normalizedCategories.map((category) => (
                          <BootstrapForm.Check
                            key={category.id}
                            type="checkbox"
                            id={`category-${category.id}`}
                            label={category.title}
                            checked={selectedCategories.includes(category.id)}
                            onChange={() => handleCategoryChange(category.id)}
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
                    <BootstrapForm.Label htmlFor="video-tags">Tags</BootstrapForm.Label>
                  </Col>
                  <Col sm={9}>
                    <input type="hidden" name="tags" value={JSON.stringify(tags)} />
                    <TagInput
                      id="video-tags"
                      value={tags}
                      onChange={setTags}
                      suggestions={tagSuggestions ?? []}
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
                      required
                    />
                  </Col>
                </Row>

                <div className="d-flex justify-content-end gap-2">
                  <Button
                    type="button"
                    variant="secondary"
                    onClick={() => navigate(`/videos/${match.id}`)}
                  >
                    Cancel
                  </Button>
                  <Button type="submit" variant="primary" disabled={saveBusy}>
                    {saveBusy ? 'Saving...' : 'Save Changes'}
                  </Button>
                </div>
              </BootstrapForm>
            </Card.Body>
          </Card>

          <Card className="mt-3">
            <Card.Header>
              <h5>Manage Video References ({videoRefs.length})</h5>
            </Card.Header>
            <Card.Body>
              <div className="border rounded p-3 mb-3">
                <h6 className="mb-3">Add New Video Reference</h6>
                <Row className="mb-3">
                  <Col md={6}>
                    <BootstrapForm.Label htmlFor="newVideoRefId">YouTube ID</BootstrapForm.Label>
                    <BootstrapForm.Control
                      id="newVideoRefId"
                      type="text"
                      placeholder="Enter YouTube video ID"
                      value={newVideoRefId}
                      onChange={e => setNewVideoRefId(e.target.value)}
                    />
                    {shouldShowVideoRefIdRequired && (
                      <div className="text-danger small mt-1">Video ID is required.</div>
                    )}
                    {isDuplicateNewVideoRefId && (
                      <div className="text-danger small mt-1">This video reference already exists.</div>
                    )}
                  </Col>
                  <Col md={6}>
                    <BootstrapForm.Label>Categories</BootstrapForm.Label>
                    <div className="border rounded p-2" style={{ maxHeight: '160px', overflowY: 'auto' }}>
                      {normalizedCategories.map(category => (
                        <BootstrapForm.Check
                          key={`new-videoref-category-${category.id}`}
                          type="checkbox"
                          id={`new-videoref-category-${category.id}`}
                          label={category.title}
                          checked={newVideoRefCategories.includes(category.id)}
                          onChange={() => handleNewVideoRefCategoryChange(category.id)}
                          className="mb-1"
                        />
                      ))}
                    </div>
                    {shouldShowVideoRefCategoriesRequired && (
                      <div className="text-danger small mt-1">Select at least one category.</div>
                    )}
                  </Col>
                </Row>
                <div className="d-flex justify-content-end">
                  <Button
                    type="button"
                    variant="outline-success"
                    onClick={handleAddVideoRef}
                    disabled={isAddVideoRefDisabled}
                  >
                    {addFetcher.state === 'submitting' ? 'Adding...' : 'Add Video Reference'}
                  </Button>
                </div>
              </div>

              {videoRefs.length > 0 ? (
                <Table striped bordered hover size="sm">
                  <thead>
                    <tr>
                      <th>YouTube ID</th>
                      <th>Categories</th>
                      <th>Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {videoRefs.map(videoRef => (
                      <tr key={videoRef.youtubeId}>
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
              ) : (
                <p className="small text-muted mb-0">No video references have been added yet.</p>
              )}
              <p className="small text-muted mt-3 mb-0">
                New references are validated and saved immediately. Existing reference edits retain the modal's local behavior.
              </p>
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
                <code>{match.id}</code>
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
              <div className="mb-2">
                <strong>Current Tags:</strong><br />
                <div className="d-flex gap-1 flex-wrap">
                  {match.tags && match.tags.length > 0 ? (
                    match.tags.map((tag, idx) => (
                      <Badge key={idx} bg="info">{tag}</Badge>
                    ))
                  ) : (
                    <em className="text-muted">No tags</em>
                  )}
                </div>
              </div>
            </Card.Body>
          </Card>
        </Col>
      </Row>

      <VideoRefEditModal
        show={showModal}
        videoRef={selectedVideoRef}
        onHide={() => setShowModal(false)}
        onSave={handleSaveVideoRef}
        availableCategories={normalizedCategories}
      />
    </>
  );
};

export default Component;
