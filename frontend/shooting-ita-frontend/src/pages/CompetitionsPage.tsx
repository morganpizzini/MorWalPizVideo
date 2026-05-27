import React, { useEffect, useState } from 'react';
import { Container, Row, Col, Card, Badge, Spinner, Alert } from 'react-bootstrap';
import { Link } from 'react-router-dom';
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

const CompetitionsPage: React.FC = () => {
  const [competitions, setCompetitions] = useState<Competition[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    apiService
      .getCompetitions()
      .then(setCompetitions)
      .catch(() => setError('Errore nel caricamento delle competizioni.'))
      .finally(() => setLoading(false));
  }, []);

  if (loading) {
    return (
      <div className="text-center py-5">
        <Spinner animation="border" role="status" />
      </div>
    );
  }

  if (error) {
    return <Alert variant="danger">{error}</Alert>;
  }

  return (
    <Container>
      <h2 className="mb-4">Competizioni</h2>
      {competitions.length === 0 ? (
        <Alert variant="info">Nessuna competizione disponibile al momento.</Alert>
      ) : (
        <Row xs={1} md={2} lg={3} className="g-4">
          {competitions.map((comp) => (
            <Col key={comp.id}>
              <Card className="h-100 shadow-sm">
                {comp.imageUrl && (
                  <Card.Img
                    variant="top"
                    src={comp.imageUrl}
                    style={{ height: '180px', objectFit: 'cover' }}
                  />
                )}
                <Card.Body className="d-flex flex-column">
                  <div className="d-flex justify-content-between align-items-start mb-2">
                    <Card.Title className="mb-0">{comp.name}</Card.Title>
                    <Badge bg={statusVariant[comp.status]} className="ms-2 text-nowrap">
                      {CompetitionStatusLabels[comp.status]}
                    </Badge>
                  </div>
                  {comp.location && (
                    <Card.Subtitle className="mb-2 text-muted small">
                      📍 {comp.location}
                    </Card.Subtitle>
                  )}
                  <Card.Text className="text-muted small flex-grow-1">
                    {comp.description}
                  </Card.Text>
                  <div className="text-muted small mb-3">
                    🗓️ {new Date(comp.startDate).toLocaleDateString('it-IT')}
                    {comp.endDate && ` – ${new Date(comp.endDate).toLocaleDateString('it-IT')}`}
                  </div>
                  {comp.stages.length > 0 && (
                    <div className="text-muted small mb-3">
                      🎯 {comp.stages.length} stage{comp.stages.length !== 1 ? 's' : ''}
                    </div>
                  )}
                  <Link
                    to={`/competitions/${comp.id}`}
                    className="btn btn-outline-primary btn-sm mt-auto"
                  >
                    Dettagli
                  </Link>
                </Card.Body>
              </Card>
            </Col>
          ))}
        </Row>
      )}
    </Container>
  );
};

export default CompetitionsPage;
