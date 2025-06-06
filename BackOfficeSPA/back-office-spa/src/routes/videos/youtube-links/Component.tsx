import React, { useState, useEffect } from 'react';
import { useLoaderData } from 'react-router';
import { Container, Row, Col, Card, Button, Form, Table, Alert, Modal, Badge } from 'react-bootstrap';
import PageHeader from '../../../components/PageHeader';
import { Match } from '../../../services/matchesService';
import { YouTubeVideoLinksService } from '../../../services/youTubeVideoLinksService';
import { UtilityService } from '../../../services/utilityService';
import { CreateYouTubeVideoLinkRequest, YouTubeVideoLinkResponse } from '../../../models/youTubeVideoLink';
import { FontListResponse } from '../../../models/font';

interface LoaderData {
  matches: Match[];
}

const Component: React.FC = () => {
  const { matches } = useLoaderData() as LoaderData;
  const [selectedMatch, setSelectedMatch] = useState<Match | null>(null);
  const [videoLinks, setVideoLinks] = useState<YouTubeVideoLinkResponse[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [fonts, setFonts] = useState<FontListResponse>({ categories: [] });
  const [fontsLoading, setFontsLoading] = useState(false);
  const [createForm, setCreateForm] = useState<Omit<CreateYouTubeVideoLinkRequest, 'matchId'>>({
    contentCreatorName: '',
    youTubeVideoId: '',
    fontStyle: 'Arial',
    fontSize: 48,
    textColor: '#FFFFFF',
    outlineColor: '#000000',
    outlineThickness: 2,
  });

  // Load video links when a match is selected
  useEffect(() => {
    if (selectedMatch) {
      loadVideoLinks(selectedMatch.id);
    }
  }, [selectedMatch]);

  // Load fonts when modal is opened
  useEffect(() => {
    if (showCreateModal && fonts.categories.length === 0) {
      loadFonts();
    }
  }, [showCreateModal]);

  const loadFonts = async () => {
    setFontsLoading(true);
    try {
      const fontData = await UtilityService.getFonts();
      setFonts(fontData);
    } catch (err) {
      console.error('Failed to load fonts:', err);
      // Set a fallback if fonts fail to load
      setFonts({
        categories: [{
          categoryName: 'Default',
          fontFiles: ['Arial.ttf', 'Times New Roman.ttf', 'Helvetica.ttf']
        }]
      });
    } finally {
      setFontsLoading(false);
    }
  };

  const loadVideoLinks = async (matchId: string) => {
    setLoading(true);
    setError(null);
    try {
      const links = await YouTubeVideoLinksService.getVideoLinks(matchId);
      setVideoLinks(links);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load video links');
      setVideoLinks([]);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateVideoLink = async () => {
    if (!selectedMatch) return;

    try {
      const request: CreateYouTubeVideoLinkRequest = {
        ...createForm,
        matchId: selectedMatch.id,
      };

      await YouTubeVideoLinksService.createVideoLink(request);
      
      // Refresh the list
      await loadVideoLinks(selectedMatch.id);
      
      // Reset form and close modal
      setCreateForm({
        contentCreatorName: '',
        youTubeVideoId: '',
        fontStyle: 'Arial',
        fontSize: 48,
        textColor: '#FFFFFF',
        outlineColor: '#000000',
        outlineThickness: 2,
      });
      setShowCreateModal(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create video link');
    }
  };

  const handleRemoveVideoLink = async (youtubeVideoId: string) => {
    if (!selectedMatch) return;
    
    if (confirm('Sei sicuro di voler rimuovere questo link video?')) {
      try {
        await YouTubeVideoLinksService.removeVideoLink(selectedMatch.id, youtubeVideoId);
        await loadVideoLinks(selectedMatch.id);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to remove video link');
      }
    }
  };

  const getLinktreeUrl = (matchId: string) => {
    return `${window.location.origin}/linktree/${matchId}`;
  };

  return (
    <Container fluid>
      <PageHeader title="Gestione YouTube Video Links" />
      
      <Row>
        {/* Match Selection */}
        <Col md={4}>
          <Card>
            <Card.Header>
              <h5>Seleziona Match</h5>
            </Card.Header>
            <Card.Body>
              <Form.Select 
                value={selectedMatch?.id || ''} 
                onChange={(e) => {
                  const match = matches.find(m => m.id === e.target.value);
                  setSelectedMatch(match || null);
                }}
              >
                <option value="">-- Seleziona un match --</option>
                {matches.map(match => (
                  <option key={match.id} value={match.id}>
                    {match.title}
                  </option>
                ))}
              </Form.Select>
              
              {selectedMatch && (
                <div className="mt-3">
                  <h6>Match Selezionato:</h6>
                  <p><strong>{selectedMatch.title}</strong></p>
                  <p className="text-muted">{selectedMatch.description}</p>
                  <Badge bg="secondary">{selectedMatch.category}</Badge>
                  
                  <div className="mt-2">
                    <small className="text-muted">Linktree URL:</small>
                    <br />
                    <code>{getLinktreeUrl(selectedMatch.id)}</code>
                    <Button 
                      size="sm" 
                      variant="outline-primary" 
                      className="ms-2"
                      onClick={() => window.open(getLinktreeUrl(selectedMatch.id), '_blank')}
                    >
                      Apri
                    </Button>
                  </div>
                </div>
              )}
            </Card.Body>
          </Card>
        </Col>

        {/* Video Links Management */}
        <Col md={8}>
          {selectedMatch ? (
            <Card>
              <Card.Header className="d-flex justify-content-between align-items-center">
                <h5>YouTube Video Links per "{selectedMatch.title}"</h5>
                <Button 
                  variant="primary" 
                  onClick={() => setShowCreateModal(true)}
                >
                  Aggiungi Video Link
                </Button>
              </Card.Header>
              <Card.Body>
                {error && (
                  <Alert variant="danger" dismissible onClose={() => setError(null)}>
                    {error}
                  </Alert>
                )}

                {loading ? (
                  <div className="text-center">Caricamento...</div>
                ) : videoLinks.length === 0 ? (
                  <div className="text-center text-muted">
                    <p>Nessun video link trovato per questo match.</p>
                    <p>Aggiungi il primo video link usando il pulsante sopra.</p>
                  </div>
                ) : (
                  <Table responsive striped>
                    <thead>
                      <tr>
                        <th>Creator</th>
                        <th>Video ID</th>
                        <th>Immagine</th>
                        <th>Short Link</th>
                        <th>Azioni</th>
                      </tr>
                    </thead>
                    <tbody>
                      {videoLinks.map((link, index) => (
                        <tr key={index}>
                          <td>{link.contentCreatorName}</td>
                          <td>
                            <a 
                              href={`https://www.youtube.com/watch?v=${link.youTubeVideoId}`} 
                              target="_blank" 
                              rel="noopener noreferrer"
                            >
                              {link.youTubeVideoId}
                            </a>
                          </td>
                          <td>
                            {link.imageName && (
                              <img 
                                src={YouTubeVideoLinksService.getCreatorImageUrl(link.imageName)}
                                alt={link.contentCreatorName}
                                style={{ width: '50px', height: '50px', objectFit: 'cover' }}
                                className="rounded"
                              />
                            )}
                          </td>
                          <td>
                            {link.shortLinkCode && (
                              <code>morwal.tv/sl/{link.shortLinkCode}</code>
                            )}
                          </td>
                          <td>
                            <Button 
                              variant="danger" 
                              size="sm"
                              onClick={() => handleRemoveVideoLink(link.youTubeVideoId)}
                            >
                              Rimuovi
                            </Button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </Table>
                )}
              </Card.Body>
            </Card>
          ) : (
            <Card>
              <Card.Body className="text-center text-muted">
                <h5>Seleziona un match per gestire i video links</h5>
                <p>Scegli un match dalla lista a sinistra per iniziare a gestire i YouTube video links.</p>
              </Card.Body>
            </Card>
          )}
        </Col>
      </Row>

      {/* Create Video Link Modal */}
      <Modal show={showCreateModal} onHide={() => setShowCreateModal(false)} size="lg">
        <Modal.Header closeButton>
          <Modal.Title>Aggiungi YouTube Video Link</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <Form>
            <Row>
              <Col md={6}>
                <Form.Group className="mb-3">
                  <Form.Label>Nome Creator</Form.Label>
                  <Form.Control
                    type="text"
                    value={createForm.contentCreatorName}
                    onChange={(e) => setCreateForm({...createForm, contentCreatorName: e.target.value})}
                    placeholder="es. PewDiePie"
                  />
                </Form.Group>
              </Col>
              <Col md={6}>
                <Form.Group className="mb-3">
                  <Form.Label>YouTube Video ID</Form.Label>
                  <Form.Control
                    type="text"
                    value={createForm.youTubeVideoId}
                    onChange={(e) => setCreateForm({...createForm, youTubeVideoId: e.target.value})}
                    placeholder="es. dQw4w9WgXcQ"
                  />
                </Form.Group>
              </Col>
            </Row>
            
            <Row>
              <Col md={6}>
                <Form.Group className="mb-3">
                  <Form.Label>Font Style</Form.Label>
                  {fontsLoading ? (
                    <Form.Control
                      type="text"
                      value="Caricamento fonts..."
                      disabled
                    />
                  ) : (
                    <Form.Select
                      value={createForm.fontStyle}
                      onChange={(e) => setCreateForm({...createForm, fontStyle: e.target.value})}
                    >
                      <option value="">-- Seleziona un font --</option>
                      {fonts.categories.map((category) => (
                        <optgroup key={category.categoryName} label={category.categoryName}>
                          {category.fontFiles.map((fontFile) => {
                            // Remove .ttf extension for display and use as value
                            const fontName = fontFile.replace('.ttf', '');
                            return (
                                <option key={fontFile} value={`${category.categoryName}/${fontName}`}>
                                {fontName}
                              </option>
                            );
                          })}
                        </optgroup>
                      ))}
                    </Form.Select>
                  )}
                </Form.Group>
              </Col>
              <Col md={6}>
                <Form.Group className="mb-3">
                  <Form.Label>Font Size</Form.Label>
                  <Form.Control
                    type="number"
                    value={createForm.fontSize}
                    onChange={(e) => setCreateForm({...createForm, fontSize: parseInt(e.target.value)})}
                  />
                </Form.Group>
              </Col>
            </Row>
            
            <Row>
              <Col md={4}>
                <Form.Group className="mb-3">
                  <Form.Label>Colore Testo</Form.Label>
                  <Form.Control
                    type="color"
                    value={createForm.textColor}
                    onChange={(e) => setCreateForm({...createForm, textColor: e.target.value})}
                  />
                </Form.Group>
              </Col>
              <Col md={4}>
                <Form.Group className="mb-3">
                  <Form.Label>Colore Bordo</Form.Label>
                  <Form.Control
                    type="color"
                    value={createForm.outlineColor}
                    onChange={(e) => setCreateForm({...createForm, outlineColor: e.target.value})}
                  />
                </Form.Group>
              </Col>
              <Col md={4}>
                <Form.Group className="mb-3">
                  <Form.Label>Spessore Bordo</Form.Label>
                  <Form.Control
                    type="number"
                    value={createForm.outlineThickness}
                    onChange={(e) => setCreateForm({...createForm, outlineThickness: parseInt(e.target.value)})}
                  />
                </Form.Group>
              </Col>
            </Row>
          </Form>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowCreateModal(false)}>
            Annulla
          </Button>
          <Button 
            variant="primary" 
            onClick={handleCreateVideoLink}
            disabled={!createForm.contentCreatorName || !createForm.youTubeVideoId}
          >
            Crea Video Link
          </Button>
        </Modal.Footer>
      </Modal>
    </Container>
  );
};

export default Component;
