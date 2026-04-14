import React, { useState } from 'react';
import { Button, Table, Form, InputGroup, Badge } from 'react-bootstrap';
import { Match } from '../models/video/types';

interface VideoListProps {
  matches: Match[];
}

interface ExpandedState {
  [key: string]: boolean;
}

const VideoList: React.FC<VideoListProps> = ({ matches }) => {
  const [expanded, setExpanded] = useState<ExpandedState>({});
  const [searchTerm, setSearchTerm] = useState('');

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

  return (
    <div className="mt-5">
      <h3>Existing Videos</h3>
      <p className="text-muted mb-3">
        {matches.length} YouTube content(s) with {matches.reduce((sum, m) => sum + (m.videoRefs?.length || 0), 0)} total video(s)
      </p>

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
            <th style={{ width: '200px' }}>Actions</th>
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
                    <div className="d-flex gap-2">
                      <Button
                        variant="outline-primary"
                        size="sm"
                        onClick={() => window.location.href = `/videos/${match.id}`}
                      >
                        View
                      </Button>
                      <Button
                        variant="outline-secondary"
                        size="sm"
                        onClick={() => window.location.href = `/videos/${match.id}/edit`}
                      >
                        Edit
                      </Button>
                      <Button
                        variant="outline-danger"
                        size="sm"
                        onClick={() => handleDelete(match.id)}
                      >
                        Delete
                      </Button>
                    </div>
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
    </div>
  );
};

export default VideoList;
