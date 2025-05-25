import React, { useState, useCallback, useEffect } from 'react';
import { Container, Form, Button, Card, Alert } from 'react-bootstrap';
import { useGoogleReCaptcha } from 'react19-google-recaptcha-v3';
import apiService from '../services/apiService'; // Assuming apiService exists


interface FormData {
  name: string;
  email: string;
  recordingDate: string;
  recordingTime: string;
  shooterDescription: string;
}

const RequestVideoPage: React.FC = () => {
  const [formData, setFormData] = useState<FormData>({
    name: '',
    email: '',
    recordingDate: '',
    recordingTime: '',
    shooterDescription: '',
  });

  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [loading, setLoading] = useState<boolean>(false);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };


  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);
    setSuccess(null);

    if (!token) {
      setError('Recaptcha verification failed. Please try again.');
      return;
    }

    setLoading(true);
    try {
      // Replace 'requestVideo' with your actual backend endpoint function
      await apiService.requestVideo({ ...formData, recaptchaToken: token });
      setSuccess('Video request submitted successfully!');
      setFormData({ name: '', email: '', recordingDate: '', recordingTime: '', shooterDescription: '' }); // Reset form
      // Consider adding a refreshReCaptcha() call if the library provides it
    } catch (err) {
      setError('Failed to submit request. Please try again.');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const { executeRecaptcha } = useGoogleReCaptcha();
  const [token, setToken] = useState('');
  const handleReCaptchaVerify = useCallback(async () => {
    if (!executeRecaptcha) {
      return;
    }
    const token = await executeRecaptcha('requestVideoForm');
    setToken(token);
  }, [executeRecaptcha]);
  useEffect(() => {
    handleReCaptchaVerify();
  }, [handleReCaptchaVerify]);

  return (
    <Card>
      <Card.Header as="h2">Request Video</Card.Header>
      <Card.Body>
        <Card.Text>Request a video from the creator.</Card.Text>
        {error && <Alert variant="danger">{error}</Alert>}
        {success && <Alert variant="success">{success}</Alert>}
        <Form onSubmit={handleSubmit}>
          <Form.Group className="mb-3" controlId="formVideoRequestName">
            <Form.Label>Name</Form.Label>
            <Form.Control
              type="text"
              placeholder="Enter your name"
              name="name"
              value={formData.name}
              onChange={handleInputChange}
              required
            />
          </Form.Group>

          <Form.Group className="mb-3" controlId="formVideoRequestEmail">
            <Form.Label>Email address</Form.Label>
            <Form.Control
              type="email"
              placeholder="Enter email"
              name="email"
              value={formData.email}
              onChange={handleInputChange}
              required
            />
          </Form.Group>

          <Form.Group className="mb-3" controlId="formVideoRequestDate">
            <Form.Label>Registration date</Form.Label>
            <Form.Control
              type="date"
              name="recordingDate"
              value={formData.recordingDate}
              onChange={handleInputChange}
              required
            />
          </Form.Group>

          <Form.Group className="mb-3" controlId="formVideoRequestTime">
            <Form.Label>Registration time</Form.Label>
            <Form.Control
              type="time"
              name="recordingTime"
              value={formData.recordingTime}
              onChange={handleInputChange}
              required
            />
          </Form.Group>

          <Form.Group className="mb-3" controlId="formVideoRequestDescription">
            <Form.Label>Shooter description</Form.Label>
            <Form.Control
              as="textarea"
              rows={3}
              placeholder="Describe the shooter"
              name="shooterDescription"
              value={formData.shooterDescription}
              onChange={handleInputChange}
              required
            />
          </Form.Group>
          <input type="hidden" name="token" value={token} />

          <Button variant="primary" type="submit" disabled={loading || !token}>
            {loading ? 'Submitting...' : 'Submit Request'}
          </Button>
        </Form>
      </Card.Body>
    </Card>
  );
};

export default RequestVideoPage;
