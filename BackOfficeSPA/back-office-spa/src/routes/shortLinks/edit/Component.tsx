import React, { useEffect, useState } from 'react';
import { useFetcher, useNavigate, useLoaderData } from 'react-router';
import { Form, Button, Modal } from 'react-bootstrap';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';
import { useToast } from '@components/ToastNotification/ToastContext';
import { ShortLink, LinkType } from '@/models';
import PageHeader from '@components/PageHeader';
import { fetchMatches, Match } from '@/services/matchesService';

const EditShortLink: React.FC = () => {
  const [model, setModel] = useState<ShortLink | null>(null);
  const [showModal, setShowModal] = useState(false);
  const [matches, setMatches] = useState<Match[]>([]);
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();
  const toast = useToast();

  const entity = useLoaderData<ShortLink>();
  useEffect(() => {
    setModel(entity);
  }, [entity]);

  // Load matches when component mounts or when model.linkType changes to YouTubeVideo
  useEffect(() => {
    if (model?.linkType === LinkType.YouTubeVideo) {
      setLoading(true);
      fetchMatches()
        .then(data => {
          setMatches(data);
        })
        .finally(() => {
          setLoading(false);
        });
    }
  }, [model?.linkType]);

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
      toast.show('Success', 'Short link updated successfully', { variant: 'success' });
      navigate('..');
    }
  }, [result, navigate, toast]);

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    setShowModal(true);
  };

  const confirmEdit = () => {
    fetcher.submit(
      { ...model },
      {
        method: 'post',
        action: location.pathname,
      }
    );
  };
  const isDisabled = () => !model || model.target?.length === 0 || busy;

  // Get label text based on link type
  const getTargetFieldLabel = () => {
    switch (model?.linkType) {
      case LinkType.YouTubeVideo:
        return 'YouTube Video ID';
      case LinkType.YouTubeChannel:
        return 'YouTube Channel Handle (e.g. @channelname)';
      case LinkType.YouTubePlaylist:
        return 'YouTube Playlist ID';
      case LinkType.Instagram:
        return 'Instagram Post ID or Username';
      case LinkType.Facebook:
        return 'Facebook Post ID or Username';
      case LinkType.CustomUrl:
        return 'Custom URL';
      default:
        return 'Target';
    }
  };

  if (!model) {
    return <div>Loading...</div>;
  }

  return (
    <>
      <PageHeader title="Edit Short Link" />
      <GenericErrorList errors={errors?.generics} />
      <Form onSubmit={handleSubmit}>
        <Form.Group controlId="formLinkType" className="mb-3">
          <Form.Label>
            Link Type <span className="text-danger">*</span>
          </Form.Label>
          <Form.Select 
            value={model.linkType} 
            onChange={e => setModel({ ...model, linkType: parseInt(e.target.value) })}
          >
            <option value={LinkType.YouTubeVideo}>YouTube Video</option>
            <option value={LinkType.YouTubeChannel}>YouTube Channel</option>
            <option value={LinkType.YouTubePlaylist}>YouTube Playlist</option>
            <option value={LinkType.Instagram}>Instagram</option>
            <option value={LinkType.Facebook}>Facebook</option>
            <option value={LinkType.CustomUrl}>Custom URL</option>
          </Form.Select>
        </Form.Group>
          <Form.Group controlId="formTarget" className="mb-3">
          <Form.Label>
            {getTargetFieldLabel()} <span className="text-danger">*</span>
          </Form.Label>
          
          {model.linkType === LinkType.YouTubeVideo && matches.length > 0 ? (
            <>
              <Form.Select 
                value={model.target} 
                onChange={e => setModel({ ...model, target: e.target.value, videoId: e.target.value })}
                disabled={loading}
              >
                <option value="">Select a video</option>
                {matches.map(match => (
                  <React.Fragment key={match.matchId}>
                    {/* If it's a direct video link */}
                    {match.isLink && (
                      <option value={match.thumbnailUrl}>
                        {match.title || match.thumbnailUrl}
                      </option>
                    )}
                    {/* If it's a collection with multiple videos */}
                    {!match.isLink && match.videos?.map(video => (
                      <option key={video.youtubeId} value={video.youtubeId}>
                        {video.title || video.youtubeId}
                      </option>
                    ))}
                  </React.Fragment>
                ))}
              </Form.Select>
              {loading && <div className="text-muted mt-1">Loading videos...</div>}
            </>
          ) : (
            <Form.Control
              type="text"
              value={model.target}
              onChange={e => setModel({ ...model, target: e.target.value, videoId: e.target.value })}
            />
          )}
          
          <FieldError error={errors?.target} />
          <Form.Text className="text-muted">
            {model.linkType === LinkType.CustomUrl && "Enter the full URL including http:// or https://"}
            {model.linkType === LinkType.YouTubeVideo && !matches.length && "Loading available videos..."}
          </Form.Text>
        </Form.Group>
        
        <Form.Group controlId="formQueryString" className="mb-3">
          <Form.Label>Query String</Form.Label>
          <Form.Control
            type="text"
            value={model.queryString}
            onChange={e => setModel({ ...model, queryString: e.target.value })}
          />
          <Form.Text className="text-muted">
            Additional parameters to append to the URL (without the ? or & prefix)
          </Form.Text>
          <FieldError error={errors?.queryString} />
        </Form.Group>
        
        <Button variant="success" disabled={isDisabled()} type="submit" className="mt-2">
          Save Changes
        </Button>
      </Form>

      <Modal show={showModal} onHide={() => setShowModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>Confirm Edit</Modal.Title>
        </Modal.Header>        <Modal.Body>
          <p>Are you sure you want to save the following changes?</p>
          <p>
            <strong>Link Type:</strong> {LinkType[model.linkType]}{' '}
            {model.linkType !== entity.linkType && (
              <>
                (<s>{LinkType[entity.linkType]}</s>)
              </>
            )}
          </p>
          <p>
            <strong>Target:</strong> {model.target}{' '}
            {model.target !== entity.target && (
              <>
                (<s>{entity.target}</s>)
              </>
            )}
          </p>
          <p>
            <strong>Query String:</strong> {model.queryString}{' '}
            {model.queryString !== entity.queryString && (
              <>
                (<s>{entity.queryString}</s>)
              </>
            )}
          </p>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowModal(false)}>
            Cancel
          </Button>
          <Button variant="primary" onClick={confirmEdit} data-testid="edit-modal-confirm">
            Save Changes
          </Button>
        </Modal.Footer>
      </Modal>
    </>
  );
};

export default EditShortLink;
