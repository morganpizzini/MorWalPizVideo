import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { Container, Row, Col, Card, Badge, Spinner, Alert, Form, InputGroup } from 'react-bootstrap';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faSearch, faCalendar, faMapMarkerAlt, faTrophy } from '@fortawesome/free-solid-svg-icons';
import { competitionService, handleApiError } from '../services/competitionService';
import type { Competition } from '../types/competition';
import { getStatusLabel, getStatusBadgeClass, formatDate } from '../types/competition';

export default function CompetitionsPage() {
  const [competitions, setCompetitions] = useState<Competition[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState('');

  useEffect(() => {
    loadCompetitions();
  }, []);

  const loadCompetitions = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await competitionService.getUpcoming();
      setCompetitions(data);
    } catch (err) {
      setError(handleApiError(err));
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!searchQuery.trim()) {
      loadCompetitions();
      return;
    }

    try {
      setLoading(true);
      setError(null);
      const data = await competitionService.search(searchQuery);
      setCompetitions(data);
    } catch (err) {
      setError(handleApiError(err));
    } finally {
      setLoading(false);
    }
  };

  return (
    <Container className="py-4">
      <Row className="mb-4">
        <Col>
          <h1 className="display-4 mb-3">
            <FontAwesomeIcon icon={faTrophy} className="me-3" />
            Competizioni di Tiro
          </h1>
          <p className="lead">Scopri e partecipa alle competizioni ufficiali</p>
        </Col>
      </Row>

      <Row className="mb-4">
        <Col md={8} lg={6}>
          <Form onSubmit={handleSearch}>
            <InputGroup>
              <Form.Control
                type="text"
                placeholder="Cerca per nome o località..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
              />
              <button className="btn btn-primary" type="submit">
                <FontAwesomeIcon icon={faSearch} />
              </button>
            </InputGroup>
          </Form>
        </Col>
      </Row>

      {error && (
        <Alert variant="danger" dismissible onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      {loading ? (
        <div className="text-center py-5">
          <Spinner animation="border" variant="primary" />
          <p className="mt-3">Caricamento competizioni...</p>
        </div>
      ) : competitions.length === 0 ? (
        <Alert variant="info">
          Nessuna competizione trovata.
        </Alert>
      ) : (
        <Row>
          {competitions.map((competition) => (
            <Col key={competition.id} md={6} lg={4} className="mb-4">
              <Card className="h-100 shadow-sm">
                {competition.imageUrl && (
                  <Card.Img
                    variant="top"
                    src={competition.imageUrl}
                    alt={competition.name}
                    style={{ height: '200px', objectFit: 'cover' }}
                  />
                )}
                <Card.Body className="d-flex flex-column">
                  <div className="mb-2">
                    <Badge bg="" className={getStatusBadgeClass(competition.status)}>
                      {getStatusLabel(competition.status)}
                    </Badge>
                  </div>
                  <Card.Title>{competition.name}</Card.Title>
                  {competition.description && (
                    <Card.Text className="text-muted small">
                      {competition.description.substring(0, 100)}
                      {competition.description.length > 100 && '...'}
                    </Card.Text>
                  )}
                  <div className="mt-auto">
                    <div className="d-flex align-items-center text-muted small mb-2">
                      <FontAwesomeIcon icon={faCalendar} className="me-2" />
                      {formatDate(competition.startDate)}
                    </div>
                    {competition.location && (
                      <div className="d-flex align-items-center text-muted small mb-3">
                        <FontAwesomeIcon icon={faMapMarkerAlt} className="me-2" />
                        {competition.location}
                      </div>
                    )}
                    <Link
                      to={`/competitions/${competition.id}`}
                      className="btn btn-primary w-100"
                    >
                      Dettagli
                    </Link>
                  </div>
                </Card.Body>
              </Card>
            </Col>
          ))}
        </Row>
      )}
    </Container>
  );
}