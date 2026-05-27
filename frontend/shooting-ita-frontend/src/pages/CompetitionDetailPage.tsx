import React, { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import {
  Container,
  Row,
  Col,
  Badge,
  Spinner,
  Alert,
  Card,
  ListGroup,
  Button,
} from 'react-bootstrap';
import { type Competition, CompetitionStatus, CompetitionStatusLabels } from '../types/competition';
import apiService from '../services/apiService';

const statusVariant: Record<CompetitionStatus, string> = {
  [CompetitionStatus.Draft]: 'secondary',
  [CompetitionStatus.Published]: 'info',
  [CompetitionStatus.RegistrationOpen]: 'success',
  [CompetitionStatus.RegistrationClosed]: 'warning',
  [CompetitionStatus.InProgress]: 'primary',
  [CompetitionStatus.Completed]: 'dark',
  [CompetitionStatus.Cancelled]: 'danger',
};

const CompetitionDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const [competition, setCompetition] = useState<Competition | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    apiService
      .getCompetitionById(id)
      .then(setCompetition)
      .catch(() => setError('Competizione non trovata.'))
      .finally(() => setLoading(false));
  }, [id]);

  if (loading) {
    return (
      <div className="text-center py-5">
        <Spinner animation="border" role="status" />
      </div>
    );
  }

  if (error || !competition) {
    return (
      <Container>
        <Alert variant="danger">{error ?? 'Competizione non trovata.'}</Alert>
        <Button as={Link as any} to="/competitions" variant="outline-secondary" size="sm">
          ← Torna alle competizioni
        </Button>
      </Container>
    );
  }

  const sortedStages = [...competition.stages].sort((a, b) => a.order - b.order);

  return (
    <Container>
      <div className="d-flex align-items-center gap-3 mb-4 flex-wrap">
        <Link to="/competitions" className="btn btn-outline-secondary btn-sm">
          ← Competizioni
        </Link>
        <h2 className="mb-0">{competition.name}</h2>
        <Badge bg={statusVariant[competition.status]}>
          {CompetitionStatusLabels[competition.status]}
        </Badge>
      </div>

      <Row className="mb-4">
        <Col md={8}>
          {competition.description && (
            <p className="text-muted">{competition.description}</p>
          )}
          {competition.rules && (
            <Card className="mb-3">
              <Card.Header>📜 Regolamento</Card.Header>
              <Card.Body>
                <p className="mb-0" style={{ whiteSpace: 'pre-line' }}>
                  {competition.rules}
                </p>
              </Card.Body>
            </Card>
          )}
        </Col>
        <Col md={4}>
          <ListGroup variant="flush" className="border rounded">
            {competition.location && (
              <ListGroup.Item>
                <strong>📍 Luogo</strong>
                <br />
                {competition.location}
              </ListGroup.Item>
            )}
            <ListGroup.Item>
              <strong>🗓️ Data inizio</strong>
              <br />
              {new Date(competition.startDate).toLocaleString('it-IT')}
            </ListGroup.Item>
            {competition.endDate && (
              <ListGroup.Item>
                <strong>🗓️ Data fine</strong>
                <br />
                {new Date(competition.endDate).toLocaleString('it-IT')}
              </ListGroup.Item>
            )}
            {competition.maxParticipants && (
              <ListGroup.Item>
                <strong>👥 Max partecipanti</strong>
                <br />
                {competition.maxParticipants}
              </ListGroup.Item>
            )}
            {competition.registrationDeadline && (
              <ListGroup.Item>
                <strong>⏰ Scadenza iscrizioni</strong>
                <br />
                {new Date(competition.registrationDeadline).toLocaleString('it-IT')}
              </ListGroup.Item>
            )}
            {competition.websiteUrl && (
              <ListGroup.Item>
                <strong>🌐 Sito web</strong>
                <br />
                <a href={competition.websiteUrl} target="_blank" rel="noopener noreferrer">
                  {competition.websiteUrl}
                </a>
              </ListGroup.Item>
            )}
          </ListGroup>
        </Col>
      </Row>

      {sortedStages.length > 0 && (
        <>
          <h4 className="mb-3">🎯 Stage ({sortedStages.length})</h4>
          <Row xs={1} md={2} className="g-3">
            {sortedStages.map((stage) => (
              <Col key={stage.stageNumber}>
                <Card className="h-100">
                  <Card.Header className="d-flex justify-content-between">
                    <strong>Stage {stage.stageNumber} – {stage.name}</strong>
                    {stage.timeLimitSeconds && (
                      <span className="text-muted small">⏱ {stage.timeLimitSeconds}s</span>
                    )}
                  </Card.Header>
                  <Card.Body>
                    {stage.description && (
                      <p className="text-muted small mb-2">{stage.description}</p>
                    )}
                    <div className="d-flex gap-3 text-muted small mb-2">
                      <span>🎯 Bersagli: {stage.targetCount}</span>
                      <span>🔫 Colpi: {stage.roundCount}</span>
                      <span>📊 Score max: {stage.maxScore}</span>
                    </div>
                    {stage.briefing && (
                      <p className="mb-0 small border-top pt-2" style={{ whiteSpace: 'pre-line' }}>
                        {stage.briefing}
                      </p>
                    )}
                  </Card.Body>
                </Card>
              </Col>
            ))}
          </Row>
        </>
      )}
    </Container>
  );
};

export default CompetitionDetailPage;
