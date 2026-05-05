import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { Container, Row, Col, Card, Badge, Spinner, Alert, ListGroup, Button } from 'react-bootstrap';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faCalendar, faMapMarkerAlt, faUsers, faClock, faArrowLeft, faExternalLinkAlt, faBullseye } from '@fortawesome/free-solid-svg-icons';
import { competitionService, handleApiError } from '../services/competitionService';
import type { Competition } from '../types/competition';
import { getStatusLabel, getStatusBadgeClass, formatDate, formatDateTime } from '../types/competition';

export default function CompetitionDetailPage() {
  const { id } = useParams<{ id: string }>();
  const [competition, setCompetition] = useState<Competition | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (id) {
      loadCompetition(id);
    }
  }, [id]);

  const loadCompetition = async (competitionId: string) => {
    try {
      setLoading(true);
      setError(null);
      const data = await competitionService.getById(competitionId);
      setCompetition(data);
    } catch (err) {
      setError(handleApiError(err));
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <Container className="py-5 text-center">
        <Spinner animation="border" variant="primary" />
        <p className="mt-3">Caricamento dettagli...</p>
      </Container>
    );
  }

  if (error || !competition) {
    return (
      <Container className="py-5">
        <Alert variant="danger">
          {error || 'Competizione non trovata'}
        </Alert>
        <Link to="/competitions" className="btn btn-secondary">
          <FontAwesomeIcon icon={faArrowLeft} className="me-2" />
          Torna alle competizioni
        </Link>
      </Container>
    );
  }

  return (
    <Container className="py-4">
      <Row className="mb-4">
        <Col>
          <Link to="/competitions" className="btn btn-outline-secondary mb-3">
            <FontAwesomeIcon icon={faArrowLeft} className="me-2" />
            Torna alle competizioni
          </Link>
        </Col>
      </Row>

      {competition.imageUrl && (
        <Row className="mb-4">
          <Col>
            <img
              src={competition.imageUrl}
              alt={competition.name}
              className="img-fluid rounded shadow"
              style={{ width: '100%', maxHeight: '400px', objectFit: 'cover' }}
            />
          </Col>
        </Row>
      )}

      <Row>
        <Col lg={8}>
          <Card className="shadow-sm mb-4">
            <Card.Body>
              <div className="mb-3">
                <Badge bg="" className={getStatusBadgeClass(competition.status)}>
                  {getStatusLabel(competition.status)}
                </Badge>
              </div>
              <h1 className="display-5 mb-3">{competition.name}</h1>
              {competition.description && (
                <p className="lead text-muted">{competition.description}</p>
              )}
            </Card.Body>
          </Card>

          {competition.rules && (
            <Card className="shadow-sm mb-4">
              <Card.Header>
                <h5 className="mb-0">Regolamento</h5>
              </Card.Header>
              <Card.Body>
                <div dangerouslySetInnerHTML={{ __html: competition.rules }} />
              </Card.Body>
            </Card>
          )}

          {competition.stages.length > 0 && (
            <Card className="shadow-sm mb-4">
              <Card.Header>
                <h5 className="mb-0">
                  <FontAwesomeIcon icon={faBullseye} className="me-2" />
                  Prove ({competition.stages.length})
                </h5>
              </Card.Header>
              <ListGroup variant="flush">
                {competition.stages
                  .sort((a, b) => a.order - b.order)
                  .map((stage) => (
                    <ListGroup.Item key={stage.stageNumber}>
                      <div className="d-flex justify-content-between align-items-start">
                        <div>
                          <h6 className="mb-1">
                            Prova {stage.stageNumber}: {stage.name}
                          </h6>
                          <div className="text-muted small">
                            <span className="me-3">
                              🎯 {stage.targetCount} bersagli
                            </span>
                            <span className="me-3">
                              🔄 {stage.roundCount} colpi
                            </span>
                            <span>
                              📊 {stage.minScore}-{stage.maxScore} punti
                            </span>
                            {stage.timeLimitSeconds && (
                              <span className="ms-3">
                                ⏱️ {stage.timeLimitSeconds}s
                              </span>
                            )}
                          </div>
                          {stage.briefing && (
                            <p className="mt-2 mb-0 small">{stage.briefing}</p>
                          )}
                        </div>
                      </div>
                    </ListGroup.Item>
                  ))}
              </ListGroup>
            </Card>
          )}
        </Col>

        <Col lg={4}>
          <Card className="shadow-sm mb-4">
            <Card.Header>
              <h5 className="mb-0">Informazioni</h5>
            </Card.Header>
            <ListGroup variant="flush">
              <ListGroup.Item>
                <div className="d-flex align-items-center">
                  <FontAwesomeIcon icon={faCalendar} className="me-3 text-primary" />
                  <div>
                    <div className="small text-muted">Data inizio</div>
                    <strong>{formatDateTime(competition.startDate)}</strong>
                  </div>
                </div>
              </ListGroup.Item>
              {competition.endDate && (
                <ListGroup.Item>
                  <div className="d-flex align-items-center">
                    <FontAwesomeIcon icon={faCalendar} className="me-3 text-primary" />
                    <div>
                      <div className="small text-muted">Data fine</div>
                      <strong>{formatDateTime(competition.endDate)}</strong>
                    </div>
                  </div>
                </ListGroup.Item>
              )}
              {competition.location && (
                <ListGroup.Item>
                  <div className="d-flex align-items-center">
                    <FontAwesomeIcon icon={faMapMarkerAlt} className="me-3 text-primary" />
                    <div>
                      <div className="small text-muted">Luogo</div>
                      <strong>{competition.location}</strong>
                    </div>
                  </div>
                </ListGroup.Item>
              )}
              {competition.maxParticipants && (
                <ListGroup.Item>
                  <div className="d-flex align-items-center">
                    <FontAwesomeIcon icon={faUsers} className="me-3 text-primary" />
                    <div>
                      <div className="small text-muted">Posti disponibili</div>
                      <strong>{competition.maxParticipants} partecipanti</strong>
                    </div>
                  </div>
                </ListGroup.Item>
              )}
              {competition.registrationDeadline && (
                <ListGroup.Item>
                  <div className="d-flex align-items-center">
                    <FontAwesomeIcon icon={faClock} className="me-3 text-primary" />
                    <div>
                      <div className="small text-muted">Scadenza iscrizioni</div>
                      <strong>{formatDate(competition.registrationDeadline)}</strong>
                    </div>
                  </div>
                </ListGroup.Item>
              )}
            </ListGroup>
          </Card>

          {competition.websiteUrl && (
            <Card className="shadow-sm mb-4">
              <Card.Body>
                <Button
                  variant="primary"
                  href={competition.websiteUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="w-100"
                >
                  Sito web ufficiale
                  <FontAwesomeIcon icon={faExternalLinkAlt} className="ms-2" />
                </Button>
              </Card.Body>
            </Card>
          )}
        </Col>
      </Row>
    </Container>
  );
}