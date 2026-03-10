import React, { useState, useEffect } from 'react';
import { Form, Button, Modal, Badge } from 'react-bootstrap';
import { useNavigate, useFetcher, useLoaderData, useParams } from 'react-router';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';
import { useToast } from '@components/ToastNotification/ToastContext';
import PageHeader from '@components/PageHeader';
import { Compilation } from '@morwalpizvideo/models';
import { fetchMatches, Match } from '@/services/matchesService';

interface VideoRef {
  youtubeId: string;
  title?: string;
  description?: string;
  categories?: any[];
  publishedAt?: string;
}

const CompilationForm: React.FC = () => {
  const params = useParams();
  const isEditMode = !!params.id;
  const existingCompilation = useLoaderData<Compilation | null>();

  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [url, setUrl] = useState('');
  const [selectedVideoIds, setSelectedVideoIds] = useState<string[]>([]);
  const [availableVideos, setAvailableVideos] = useState<VideoRef[]>([]);
  const [showModal, setShowModal] = useState(false);
  const [loading, setLoading] = useState(false);
  const [selectedVideoId, setSelectedVideoId] = useState('');

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

  // Load existing compilation data in edit mode
  useEffect(() => {
    if (isEditMode && existingCompilation) {
      setTitle(existingCompilation.title || '');
      setDescription(existingCompilation.description || '');
      setUrl(existingCompilation.url || '');
      // Extract just the youtubeIds from the existing compilation videos
      const videoIds = existingCompilation.videos?.map(v => v.youtubeId) || [];
      setSelectedVideoIds(videoIds);
    }
  }, [isEditMode, existingCompilation]);

  // Load available videos from matches
  useEffect(() => {
    setLoading(true);
    fetchMatches()
      .then(data => {
        const videos: VideoRef[] = [];
        data.forEach((match: Match) => {
          if (match.videoRefs) {
              match.videoRefs.forEach(video => {
              videos.push({
                youtubeId: video.youtubeId,
                title: video.title || video.youtubeId,
                description: video.description,
                categories: video.categories,
                publishedAt: video.publishedAt,
              });
            });
          }
        });
        setAvailableVideos(videos);
      })
      .catch(error => {
        console.error('Failed to load videos:', error);
      })
      .finally(() => {
        setLoading(false);
      });
  }, []);

  useEffect(() => {
    if (!result) return;
    setShowModal(false);

    if (result.success) {
      toast.show(
        'Success',
        `Compilation ${isEditMode ? 'updated' : 'created'} successfully`,
        { variant: 'success' }
      );
      navigate('/compilations');
    }
  }, [result, navigate, isEditMode]);

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    setShowModal(true);
  };

  const confirmSubmit = () => {
    const payload = {
      title,
      description,
      url,
      videos: JSON.stringify(selectedVideoIds),
      id: isEditMode ? params.id : undefined,
    };

    fetcher.submit(payload, {
      method: 'post',
      action: location.pathname,
    });
  };

  const handleAddVideo = () => {
    if (!selectedVideoId) return;

    // Check if video is already selected
    if (selectedVideoIds.includes(selectedVideoId)) {
      toast.show('Warning', 'Video already added', { variant: 'warning' });
      return;
    }

    setSelectedVideoIds([...selectedVideoIds, selectedVideoId]);
    setSelectedVideoId('');
  };

  const handleRemoveVideo = (youtubeId: string) => {
    setSelectedVideoIds(selectedVideoIds.filter(id => id !== youtubeId));
  };

  const isDisabled = () => {
    return title.trim().length === 0 || description.trim().length === 0 || url.trim().length === 0 || busy;
  };

  return (
    <>
      <PageHeader title={isEditMode ? 'Edit Compilation' : 'Create Compilation'} />
      <GenericErrorList errors={errors?.generics} />

      <Form onSubmit={handleSubmit}>
        <Form.Group controlId="formTitle" className="mb-3">
          <Form.Label>
            Title <span className="text-danger">*</span>
          </Form.Label>
          <Form.Control
            type="text"
            value={title}
            onChange={e => setTitle(e.target.value)}
            placeholder="Enter compilation title"
          />
          <FieldError error={errors?.title} />
        </Form.Group>

        <Form.Group controlId="formDescription" className="mb-3">
          <Form.Label>
            Description <span className="text-danger">*</span>
          </Form.Label>
          <Form.Control
            as="textarea"
            rows={3}
            value={description}
            onChange={e => setDescription(e.target.value)}
            placeholder="Enter compilation description"
          />
          <FieldError error={errors?.description} />
        </Form.Group>

        <Form.Group controlId="formUrl" className="mb-3">
          <Form.Label>
            URL <span className="text-danger">*</span>
          </Form.Label>
          <Form.Control
            type="text"
            value={url}
            onChange={e => setUrl(e.target.value)}
            placeholder="Enter compilation URL"
          />
          <FieldError error={errors?.url} />
          <Form.Text className="text-muted">
            The URL where this compilation will be accessible
          </Form.Text>
        </Form.Group>

        <Form.Group controlId="formVideos" className="mb-3">
          <Form.Label>Videos</Form.Label>
          <div className="d-flex gap-2 mb-2">
            <Form.Select
              value={selectedVideoId}
              onChange={e => setSelectedVideoId(e.target.value)}
              disabled={loading}
              className="flex-grow-1"
            >
              <option value="">Select a video to add</option>
              {availableVideos.map(video => (
                <option key={video.youtubeId} value={video.youtubeId}>
                  {video.title || video.youtubeId}
                </option>
              ))}
            </Form.Select>
            <Button
              variant="primary"
              onClick={handleAddVideo}
              disabled={!selectedVideoId || loading}
            >
              Add Video
            </Button>
          </div>
          {loading && <div className="text-muted mb-2">Loading videos...</div>}
          
          {selectedVideoIds.length > 0 && (
            <div className="border rounded p-3 bg-light">
              <strong className="d-block mb-2">Selected Videos ({selectedVideoIds.length}):</strong>
              <div className="d-flex flex-wrap gap-2">
                {selectedVideoIds.map(videoId => {
                  const video = availableVideos.find(v => v.youtubeId === videoId);
                  return (
                    <Badge
                      key={videoId}
                      bg="primary"
                      className="d-flex align-items-center gap-2 p-2"
                    >
                      <span>{video?.title || videoId}</span>
                      <button
                        type="button"
                        className="btn-close btn-close-white"
                        aria-label="Remove"
                        onClick={() => handleRemoveVideo(videoId)}
                        style={{ fontSize: '0.6rem' }}
                      />
                    </Badge>
                  );
                })}
              </div>
            </div>
          )}
          <FieldError error={errors?.videos} />
        </Form.Group>

        <Button variant="success" disabled={isDisabled()} type="submit" className="mt-2">
          {isEditMode ? 'Update Compilation' : 'Create Compilation'}
        </Button>
      </Form>

      <Modal show={showModal} onHide={() => setShowModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>Confirm {isEditMode ? 'Update' : 'Create'}</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <p>Are you sure you want to {isEditMode ? 'update' : 'create'} this compilation?</p>
          <p>
            <strong>Title:</strong> {title}
          </p>
          <p>
            <strong>Description:</strong> {description}
          </p>
          <p>
            <strong>URL:</strong> {url}
          </p>
          <p>
            <strong>Videos:</strong> {selectedVideoIds.length}
          </p>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowModal(false)}>
            Cancel
          </Button>
          <Button
            variant="success"
            onClick={confirmSubmit}
            disabled={busy}
            data-testid="submit-modal-confirm"
          >
            {isEditMode ? 'Update' : 'Create'}
          </Button>
        </Modal.Footer>
      </Modal>
    </>
  );
};

export default CompilationForm;
