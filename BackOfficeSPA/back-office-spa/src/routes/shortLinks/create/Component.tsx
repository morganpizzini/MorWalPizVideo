import React, { useState, useEffect } from 'react';
import { Form, Button, Modal } from 'react-bootstrap';
import { useNavigate, useFetcher } from 'react-router';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';
import { useToast } from '@components/ToastNotification/ToastContext';
import PageHeader from '@components/PageHeader';
import { LinkType } from '@models';
import { fetchMatches, Match } from '@/services/matchesService';

const CreateShortLink: React.FC = () => {
  const [target, setTarget] = useState('');
  const [queryString, setQueryString] = useState('');
  const [message, setMessage] = useState('');
  const [linkType, setLinkType] = useState<LinkType>(LinkType.YouTubeVideo);
  const [showModal, setShowModal] = useState(false);
  const [matches, setMatches] = useState<Match[]>([]);
  const [loading, setLoading] = useState(false);
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

  // Load matches when component mounts or when linkType changes to YouTubeVideo
  useEffect(() => {
    if (linkType === LinkType.YouTubeVideo) {
      setLoading(true);
      fetchMatches()
        .then(data => {
          setMatches(data);
        })
        .finally(() => {
          setLoading(false);
        });
    }
  }, [linkType]);

  useEffect(() => {
    if (!result) return;
    setShowModal(false);

    if (result.success) {
      toast.show('Success', 'Short link created successfully', { variant: 'success' });
      navigate('..');
    }
  }, [result, navigate, toast]);
  const isDisabled = () => target.length === 0 || busy;

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    setShowModal(true);
  };

  const confirmCreate = () => {
    fetcher.submit(
      {
        target,
        linkType,
        queryString,
        message,
      },
      {
        method: 'post',
        action: location.pathname,
      }
    );
  };
  
  // Get label text based on link type
  const getTargetFieldLabel = () => {
    switch (linkType) {
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

  return (
    <>
      <PageHeader title="Create Short Link" />
      <GenericErrorList errors={errors?.generics} />      <Form onSubmit={handleSubmit}>
        <Form.Group controlId="formLinkType" className="mb-3">
          <Form.Label>
            Link Type <span className="text-danger">*</span>
          </Form.Label>
          <Form.Select 
            value={linkType} 
            onChange={e => setLinkType(parseInt(e.target.value))}
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
          
          {linkType === LinkType.YouTubeVideo && matches.length > 0 ? (
            <>
              <Form.Select 
                value={target} 
                onChange={e => setTarget(e.target.value)}
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
            <Form.Control type="text" value={target} onChange={e => setTarget(e.target.value)} />
          )}
          
          <FieldError error={errors?.target} />
          <Form.Text className="text-muted">
            {linkType === LinkType.CustomUrl && "Enter the full URL including http:// or https://"}
            {linkType === LinkType.YouTubeVideo && !matches.length && "Loading available videos..."}
          </Form.Text>
        </Form.Group>
        
        <Form.Group controlId="formQueryString" className="mb-3">
          <Form.Label>Query String</Form.Label>
          <Form.Control
            type="text"
            value={queryString}
            onChange={e => setQueryString(e.target.value)}
          />
          <Form.Text className="text-muted">
            Additional parameters to append to the URL (without the ? or & prefix)
          </Form.Text>
        </Form.Group>
        
        <Form.Group controlId="formMessage" className="mb-3">
          <Form.Label>Message</Form.Label>
          <Form.Control 
            as="textarea" 
            rows={3} 
            value={message} 
            onChange={e => setMessage(e.target.value)} 
          />
          <Form.Text className="text-muted">
            Optional message to be sent with the link when shared
          </Form.Text>
        </Form.Group>
        
        <Button variant="success" disabled={isDisabled()} type="submit" className="mt-2">
          Create
        </Button>
      </Form>      <Modal show={showModal} onHide={() => setShowModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>Confirm Create</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <p>Are you sure you want to create the following short link?</p>
          <p>
            <strong>Link Type:</strong> {LinkType[linkType]}
          </p>
          <p>
            <strong>Target:</strong> {target}
          </p>
          {queryString && (
            <p>
              <strong>Query String:</strong> {queryString}
            </p>
          )}
          {message && (
            <p>
              <strong>Message:</strong> {message}
            </p>
          )}
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowModal(false)}>
            Cancel
          </Button>
          <Button variant="success" onClick={confirmCreate} data-testid="create-modal-confirm">
            Create
          </Button>
        </Modal.Footer>
      </Modal>
    </>
  );
};

export default CreateShortLink;
