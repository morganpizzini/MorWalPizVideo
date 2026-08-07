import { FormEvent, useEffect, useState } from 'react';
import { Alert, Button, Card, Col, Form, Row } from 'react-bootstrap';
import { endpoints, get, put } from '@morwalpizvideo/services';
import { authService } from '../../services/authService';

interface UserProfile {
  id: string;
  username: string;
  email: string;
  role: string;
}

export default function ProfilePage() {
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [username, setUsername] = useState('');
  const [email, setEmail] = useState('');
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    void loadProfile();
  }, []);

  async function loadProfile() {
    setLoading(true);
    setError(null);

    try {
      const response = await get(endpoints.USER_ME);
      const loadedProfile = response as UserProfile;
      setProfile(loadedProfile);
      setUsername(loadedProfile.username ?? '');
      setEmail(loadedProfile.email ?? '');
    } catch {
      setError('Impossibile caricare il profilo.');
    } finally {
      setLoading(false);
    }
  }

  async function submitProfile(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setLoading(true);
    setError(null);
    setSuccess(null);

    try {
      await put(endpoints.USER_ME, { username, email });

      if (profile) {
        const next = { ...profile, username, email };
        setProfile(next);
        authService.setUser({
          id: next.id,
          username: next.username,
          email: next.email,
          role: next.role,
        });
      }

      setSuccess('Profilo aggiornato con successo.');
    } catch {
      setError('Aggiornamento profilo non riuscito.');
    } finally {
      setLoading(false);
    }
  }

  async function submitPassword(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setLoading(true);
    setError(null);
    setSuccess(null);

    try {
      await put(endpoints.USER_ME_PASSWORD, {
        currentPassword,
        newPassword,
      });

      setCurrentPassword('');
      setNewPassword('');
      setSuccess('Password aggiornata con successo.');
    } catch {
      setError('Aggiornamento password non riuscito. Verifica la password corrente.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div>
      <h1>Profile</h1>
      <p className="text-muted">Gestisci i tuoi dati personali e la password.</p>

      {error ? <Alert variant="danger">{error}</Alert> : null}
      {success ? <Alert variant="success">{success}</Alert> : null}

      <Row className="g-4">
        <Col lg={6}>
          <Card>
            <Card.Header>Dati personali</Card.Header>
            <Card.Body>
              <Form onSubmit={submitProfile}>
                <Form.Group className="mb-3" controlId="profile-username">
                  <Form.Label>Username</Form.Label>
                  <Form.Control
                    value={username}
                    onChange={(event) => setUsername(event.target.value)}
                    required
                  />
                </Form.Group>

                <Form.Group className="mb-3" controlId="profile-email">
                  <Form.Label>Email</Form.Label>
                  <Form.Control
                    type="email"
                    value={email}
                    onChange={(event) => setEmail(event.target.value)}
                    required
                  />
                </Form.Group>

                <Button type="submit" disabled={loading || profile === null}>
                  Salva profilo
                </Button>
              </Form>
            </Card.Body>
          </Card>
        </Col>

        <Col lg={6}>
          <Card>
            <Card.Header>Cambia password</Card.Header>
            <Card.Body>
              <Form onSubmit={submitPassword}>
                <Form.Group className="mb-3" controlId="profile-current-password">
                  <Form.Label>Password corrente</Form.Label>
                  <Form.Control
                    type="password"
                    value={currentPassword}
                    onChange={(event) => setCurrentPassword(event.target.value)}
                    required
                  />
                </Form.Group>

                <Form.Group className="mb-3" controlId="profile-new-password">
                  <Form.Label>Nuova password</Form.Label>
                  <Form.Control
                    type="password"
                    value={newPassword}
                    onChange={(event) => setNewPassword(event.target.value)}
                    required
                  />
                </Form.Group>

                <Button type="submit" disabled={loading || profile === null}>
                  Aggiorna password
                </Button>
              </Form>
            </Card.Body>
          </Card>
        </Col>
      </Row>
    </div>
  );
}
