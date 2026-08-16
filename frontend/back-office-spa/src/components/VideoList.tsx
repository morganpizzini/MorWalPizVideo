import React, { useState } from 'react';
import { Button, Table, Form, InputGroup, Badge, Modal, Alert, Dropdown } from 'react-bootstrap';
import { useRevalidator, Link } from 'react-router';
import type { Match } from '@morwalpizvideo/models';
import type { Channel } from '@morwalpizvideo/models';
import { publishVideoToSocial, refreshVideoYouTubeData } from '../services/videoService';
import { ComposeUrl, Delete, endpoints } from '@morwalpizvideo/services';
import { hasPermission, permissions } from '../authorization/permissions';
import { useAppStore } from '../state/appStore';

interface VideoListProps {
  matches: Match[];
  channels: Channel[];
}

interface ExpandedState {
  [key: string]: boolean;
}

interface RefreshingState {
  [key: string]: boolean;
}

export function composeShortLinkUrl(baseUrl: string | undefined, code: string | undefined): string | undefined {
  if (!baseUrl || !code) return undefined;
  return `${baseUrl.replace(/\/+$/, '')}/${code.replace(/^\/+/, '')}`;
}

export function shouldShowMainUrl(videoRefCount: number): boolean {
  return videoRefCount !== 1;
}

const VideoList: React.FC<VideoListProps> = ({ matches, channels }) => {
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
  const effectivePermissions = useAppStore(state => state.effectivePermissions);
  const canEdit = hasPermission(effectivePermissions, [permissions.videos.update, permissions.videos.manage]);
  const canPublish = hasPermission(effectivePermissions, [permissions.videos.publish, permissions.videos.manage]);
  const canDelete = hasPermission(effectivePermissions, [permissions.videos.delete, permissions.videos.manage]);

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

  const handleDelete = async (matchId: string) => {
    if (window.confirm('Are you sure you want to delete this video content?')) {
      try {
        await Delete(ComposeUrl(endpoints.VIDEOS_DETAIL, { videoId: matchId }));
        revalidator.revalidate();
      } catch {
        setRefreshError('Failed to delete video content');
      }
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
    <div>
      <h2 className="h5">Video library</h2>
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

      <Table responsive hover className="align-middle">
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
            filteredMatches.map((match, matchIndex) => (
              <React.Fragment key={match.id || `match-${matchIndex}`}>
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
                    {shouldShowMainUrl(match.videoRefs?.length || 0) && (match.url ? (
                      <a href={`https://morwalpiz.com/matches/${match.url}`} target="_blank" rel="noopener noreferrer" className="text-truncate d-block" style={{ maxWidth: '150px' }}>
                        {match.url}
                      </a>
                    ) : (
                      <em className="text-muted">No URL</em>
                    ))}
                  </td>
                  <td>
                    <Badge bg="info">{match.videoRefs?.length || 0} video(s)</Badge>
                  </td>
                  <td>
                    <Dropdown data-bs-boundary="window">
                      <Dropdown.Toggle variant="outline-primary" size="sm" id={`dropdown-${match.id}`}>
                        Actions
                      </Dropdown.Toggle>
                      <Dropdown.Menu renderOnMount popperConfig={{ strategy: 'fixed' }}>
                        <Dropdown.Item onClick={() => handleView(match.id)}>
                          View
                        </Dropdown.Item>
                        {canEdit ? <Dropdown.Item onClick={() => handleEdit(match.id)}>Edit</Dropdown.Item> : null}
                        {canPublish ? <Dropdown.Item onClick={() => handleOpenPublishModal(match.id)}>Publish to Social</Dropdown.Item> : null}
                        {canEdit ? (
                          <Dropdown.Item onClick={() => handleRefresh(match.id)} disabled={refreshing[match.id]}>
                            {refreshing[match.id] ? 'Refreshing...' : 'Refresh YouTube Data'}
                          </Dropdown.Item>
                        ) : null}
                        {canDelete ? <Dropdown.Divider /> : null}
                        {canDelete ? (
                          <Dropdown.Item onClick={() => void handleDelete(match.id)} className="text-danger">
                            Delete
                          </Dropdown.Item>
                        ) : null}
                      </Dropdown.Menu>
                    </Dropdown>
                  </td>
                </tr>

                {/* Sub-rows - VideoRefs */}
                {expanded[match.id] && match.videoRefs && match.videoRefs.length > 0 && (
                  match.videoRefs.map((videoRef) => {
                    // Find shortlink for this specific video
                    const videoShortLink = match.shortLinks?.find(
                      sl => sl.target === videoRef.youtubeId
                    );
                    const channel = channels.find(candidate => candidate.channelId === match.ownerChannelId);
                    const shortLinkUrl = composeShortLinkUrl(channel?.shortLinkUrl, videoShortLink?.code);

                    return (
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
                        <td colSpan={2}>
                          <div className="d-flex gap-1 flex-wrap">
                            {videoRef.categories && videoRef.categories.length > 0 ? (
                              videoRef.categories.map((cat, catIdx) => (
                                <Badge key={`${videoRef.youtubeId}-${cat.title}-${catIdx}`} bg="secondary">
                                  {cat.title}
                                </Badge>
                              ))
                            ) : (
                              <span className="text-muted">No categories</span>
                            )}
                          </div>
                        </td>
                        <td>
                          <div className="d-flex align-items-center gap-2">
                            {videoShortLink ? (
                              <>
                                {shortLinkUrl ? <>
                                  <a href={shortLinkUrl} target="_blank" rel="noopener noreferrer" className="text-truncate" style={{ maxWidth: '180px' }}>{shortLinkUrl}</a>
                                  <Button size="sm" variant="outline-secondary" onClick={() => void navigator.clipboard.writeText(shortLinkUrl)} title="Copy short link">Copy</Button>
                                </> : <Badge bg="success">{videoShortLink.code}</Badge>}
                                <Link
                                  to={`/shortlinks/${videoShortLink.code}/edit?videoId=${videoRef.youtubeId}`}
                                  className="btn btn-sm btn-outline-primary"
                                  title={`Edit shortlink (${videoShortLink.queryLinkIds?.length || 0} query params)`}
                                >
                                  Edit
                                </Link>
                              </>
                            ) : (
                              <Link
                                to={`/shortlinks/create?target=${videoRef.youtubeId}&linkType=0`}
                                className="btn btn-sm btn-outline-success"
                              >
                                <i className="bi bi-plus-circle"></i>  + ShortLink
                              </Link>
                            )}
                          </div>
                        </td>
                      </tr>
                    );
                  })
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