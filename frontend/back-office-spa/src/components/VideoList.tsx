import React, { useState } from 'react';
import { Button, Table, Form, InputGroup, Badge, Modal, Alert, Dropdown } from 'react-bootstrap';
import { useRevalidator } from 'react-router';
import { Match } from '../models/video/types';
import { publishVideoToSocial, refreshVideoYouTubeData } from '../services/videoService';

interface VideoListProps {
  matches: Match[];
}

interface ExpandedState {
  [key: string]: boolean;
}

interface RefreshingState {
  [key: string]: boolean;
}

const VideoList: React.FC<VideoListProps> = ({ matches }) => {
  const revalidator = useRevalidator();
  const [expanded, setExpanded] = useState<ExpandedState>({});
  const [searchTerm, setSearchTerm] = useState('');
  const [showPublishModal, setShowPublishModal] = useState(false);
  const [selectedVideoId, setSelectedVideoId] = useState<string | null>(null);
  const [publishMessage, setPublishMessage] = useState('');
  const [publishLoading, setPublishLoading] = useState(false);
  const [publishError, setPublishError] = useState<string | null>(null);
  const [publishSuccess, setPublishSuccess] = useState<string | null>(null);
  const [refreshing, setRefreshing] = useState<RefreshingState>({});
  const [refreshError, setRefreshError] = useState<string | null>(null);

  const toggleExpand = (matchId: string) => {
    setExpanded(prev => ({
      ...prev,
      [matchId]: !prev[matchId]
    }));
  };

  // Filter matches based on search term
  const filteredMatches = matches.filter(match => {
    if (!searchTerm) return true;
    const term = searchTerm.toLowerCase();
    return (
      match.title?.toLowerCase().includes(term) ||
      match.description?.toLowerCase().includes(term) ||
      match.id?.toLowerCase().includes(term) ||
      match.videoRefs?.some(ref => 
        ref.youtubeId.toLowerCase().includes(term) ||
        ref.categories?.some(cat => cat.title.toLowerCase().includes(term))
      )
    );
  });

  const handleDelete = (matchId: string) => {
    if (window.confirm('Are you sure you want to delete this video content?')) {
      // TODO: Implement delete functionality
      console.log('Delete match:', matchId);
    }
  };

  const handleOpenPublishModal = (matchId: string) => {
    setSelectedVideoId(matchId);
    setPublishMessage('');
    setPublishError(null);
    setPublishSuccess(null);
    setShowPublishModal(true);
  };

  const handleClosePublishModal = () => {
    setShowPublishModal(false);
    setSelectedVideoId(null);
    setPublishMessage('');
    setPublishError(null);
    setPublishSuccess(null);
  };

  const handlePublishSubmit = async () => {
    if (!selectedVideoId || !publishMessage.trim()) {
      setPublishError('Please enter a message');
      return;
    }

    setPublishLoading(true);
    setPublishError(null);
    setPublishSuccess(null);

    try {
      await publishVideoToSocial(selectedVideoId, publishMessage);
      setPublishSuccess('Successfully published to all social media platforms!');
      setTimeout(() => {
        handleClosePublishModal();
      }, 2000);
    } catch (error: any) {
      setPublishError(error.message || 'Failed to publish to social media');
    } finally {
      setPublishLoading(false);
    }
  };

  const handleRefresh = async (matchId: string) => {
    setRefreshing(prev => ({ ...prev, [matchId]: true }));
    setRefreshError(null);

    try {
      await refreshVideoYouTubeData(matchId);
      // Revalidate the route to refresh the data
      revalidator.revalidate();
    } catch (error: any) {
      setRefreshError(error.message || 'Failed to refresh YouTube data');
      setTimeout(() => setRefreshError(null), 5000);
    } finally {
      setRefreshing(prev => ({ ...prev, [matchId]: false }));
    }
  };

  const handleView = (matchId: string) => {
    window.location.href = `/videos/${matchId}`;
  };

  const handleEdit = (matchId: string) => {
    window.location.href = `/videos/${matchId}/edit`;
  };

  return (
    <div className="mt-5">
      <h3>Existing Videos</h3>
      <p className="text-muted mb-3">
        {matches.length} YouTube content(s) with {matches.reduce((sum, m) => sum + (m.videoRefs?.length || 0), 0)} total video(s)
      </p>

      {refreshError && (
        <Alert variant="danger" dismissible onClose={() => setRefreshError(null)} className="mb-3">
          {refreshError}
        </Alert>
      )}

      <div className="mb-3">
        <InputGroup>
          <Form.Control
            value={searchTerm}
            onChange={e => setSearchTerm(e.target.value)}
            placeholder="Search by title, description, ID, or category..."
          />
        </InputGroup>
      </div>

      <Table striped bordered hover>
        <thead>
          <tr>
            <th style={{ width: '40px' }}></th>
            <th>Title</th>
            <th>Description</th>
            <th>URL</th>
            <th>Videos</th>
            <th style={{ width: '120px' }}>Actions</th>
          </tr>
        </thead>
        <tbody>
          {filteredMatches.length > 0 ? (
            filteredMatches.map(match => (
              <React.Fragment key={match.id}>
                {/* Main row - YouTubeContent */}
                <tr>
                  <td>
                    {match.videoRefs && match.videoRefs.length > 0 && (
                      <Button
                        variant="link"
                        size="sm"
                        className="p-0"
                        onClick={() => toggleExpand(match.id)}
                      >
                        {expanded[match.id] ? '▼' : '▶'}
                      </Button>
                    )}
                  </td>
                  <td>
                    <div className="fw-semibold">{match.title || <em>Untitled</em>}</div>
                  </td>
                  <td>
                    <div className="text-truncate" style={{ maxWidth: '200px' }}>
                      {match.description || <em className="text-muted">No description</em>}
                    </div>
                  </td>
                  <td>
                    {match.url ? (
                      <a href={`https://morwalpiz.com/matches/${match.url}`} target="_blank" rel="noopener noreferrer" className="text-truncate d-block" style={{ maxWidth: '150px' }}>
                        {match.url}
                      </a>
                    ) : (
                      <em className="text-muted">No URL</em>
                    )}
                  </td>
                  <td>
                    <Badge bg="info">{match.videoRefs?.length || 0} video(s)</Badge>
                  </td>
                  <td>
                    <Dropdown>
                      <Dropdown.Toggle variant="outline-primary" size="sm" id={`dropdown-${match.id}`}>
                        Actions
                      </Dropdown.Toggle>

                      <Dropdown.Menu>
                        <Dropdown.Item onClick={() => handleView(match.id)}>
                          View
                        </Dropdown.Item>
                        <Dropdown.Item onClick={() => handleEdit(match.id)}>
                          Edit
                        </Dropdown.Item>
                        <Dropdown.Item onClick={() => handleOpenPublishModal(match.id)}>
                          Publish to Social
                        </Dropdown.Item>
                        <Dropdown.Item 
                          onClick={() => handleRefresh(match.id)}
                          disabled={refreshing[match.id]}
                        >
                          {refreshing[match.id] ? 'Refreshing...' : 'Refresh YouTube Data'}
                        </Dropdown.Item>
                        <Dropdown.Divider />
                        <Dropdown.Item 
                          onClick={() => handleDelete(match.id)}
                          className="text-danger"
                        >
                          Delete
                        </Dropdown.Item>
                      </Dropdown.Menu>
                    </Dropdown>
                  </td>
                </tr>
                
                {/* Sub-rows - VideoRefs */}
                {expanded[match.id] && match.videoRefs && match.videoRefs.length > 0 && (
                  match.videoRefs.map((videoRef) => (
                    <tr key={`${match.id}-${videoRef.youtubeId}`} className="table-light">
                      <td></td>
                      <td colSpan={2} className="ps-5">
                        <div className="d-flex align-items-center gap-2">
                          <code className="text-primary">{videoRef.youtubeId}</code>
                          {videoRef.youtubeId === match.thumbnailVideoId && (
                            <Badge bg="success">Thumbnail</Badge>
                          )}
                        </div>
                      </td>
                      <td colSpan={3}>
                        <div className="d-flex gap-1 flex-wrap">
                          {videoRef.categories && videoRef.categories.length > 0 ? (
                            videoRef.categories.map((cat, catIdx) => (
                              <Badge key={catIdx} bg="secondary">
                                {cat.title}
                              </Badge>
                            ))
                          ) : (
                            <span className="text-muted">No categories</span>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))
                )}
              </React.Fragment>
            ))
          ) : (
            <tr>
              <td colSpan={6} className="text-center">
                {searchTerm ? 'No matching videos found' : 'No videos found'}
              </td>
            </tr>
          )}
        </tbody>
      </Table>

      {/* Publish to Social Media Modal */}
      <Modal show={showPublishModal} onHide={handleClosePublishModal}>
        <Modal.Header closeButton>
          <Modal.Title>Publish to Social Media</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          {publishError && (
            <Alert variant="danger" dismissible onClose={() => setPublishError(null)}>
              {publishError}
            </Alert>
          )}
          {publishSuccess && (
            <Alert variant="success">
              {publishSuccess}
            </Alert>
          )}
          <Form>
            <Form.Group className="mb-3">
              <Form.Label>Message</Form.Label>
              <Form.Control
                as="textarea"
                rows={4}
                value={publishMessage}
                onChange={(e) => setPublishMessage(e.target.value)}
                placeholder="Enter your message to post on Facebook, Telegram, and Discord..."
                disabled={publishLoading || !!publishSuccess}
              />
              <Form.Text className="text-muted">
                This message will be posted to Facebook, Telegram, and Discord with the video shortlink.
              </Form.Text>
            </Form.Group>
          </Form>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={handleClosePublishModal} disabled={publishLoading}>
            Cancel
          </Button>
          <Button 
            variant="primary" 
            onClick={handlePublishSubmit} 
            disabled={publishLoading || !publishMessage.trim() || !!publishSuccess}
          >
            {publishLoading ? 'Publishing...' : 'Publish'}
          </Button>
        </Modal.Footer>
      </Modal>
    </div>
  );
};

export default VideoList;