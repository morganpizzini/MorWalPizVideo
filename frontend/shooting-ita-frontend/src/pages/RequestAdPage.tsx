import React, { useState, useCallback, useEffect } from 'react';
import { Container, Form, Button, Card, Alert } from 'react-bootstrap';
import {
  useGoogleReCaptcha
} from 'react19-google-recaptcha-v3';
import apiService from '../services/apiService'; // Assuming apiService exists

// Replace with your actual Recaptcha Site Key
const RECAPTCHA_SITE_KEY = 'YOUR_RECAPTCHA_V3_SITE_KEY';

interface FormData {
  companyName: string;
  contactPerson: string;
  email: string;
  companyDescription: string;
}

const RequestAdPage: React.FC = () => {
  const [formData, setFormData] = useState<FormData>({
    companyName: '',
    contactPerson: '',
    email: '',
    companyDescription: '',
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
      // Replace 'requestAd' with your actual backend endpoint function
      await apiService.requestAd({ ...formData, recaptchaToken: token });
      setSuccess('Ad request submitted successfully! The creator will contact you.');
      setFormData({ companyName: '', contactPerson: '', email: '', companyDescription: '' }); // Reset form
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
    const token = await executeRecaptcha('sponsorForm');
    setToken(token);
  }, [executeRecaptcha]);
  useEffect(() => {
    handleReCaptchaVerify();
  }, [handleReCaptchaVerify]);

  return (
      <Card>
        <Card.Header as="h2">Request Ad Insertion</Card.Header>
        <Card.Body>
          <Card.Text>
            Request an ad insertion in the video. After the form submission, the company will be contacted by the creator.
          </Card.Text>
          {error && <Alert variant="danger">{error}</Alert>}
          {success && <Alert variant="success">{success}</Alert>}

          <Form onSubmit={handleSubmit}>
            <Form.Group className="mb-3" controlId="formAdRequestCompanyName">
              <Form.Label>Company name</Form.Label>
              <Form.Control
                type="text"
                placeholder="Enter company name"
                name="companyName"
                value={formData.companyName}
                onChange={handleInputChange}
                required
              />
            </Form.Group>

            <Form.Group className="mb-3" controlId="formAdRequestContactPerson">
              <Form.Label>Company contact</Form.Label>
              <Form.Control
                type="text"
                placeholder="Enter contact person's name"
                name="contactPerson"
                value={formData.contactPerson}
                onChange={handleInputChange}
                required
              />
            </Form.Group>

            <Form.Group className="mb-3" controlId="formAdRequestEmail">
              <Form.Label>Email address</Form.Label>
              <Form.Control
                type="email"
                placeholder="Enter contact email"
                name="email"
                value={formData.email}
                onChange={handleInputChange}
                required
              />
            </Form.Group>

            <Form.Group className="mb-3" controlId="formAdRequestDescription">
              <Form.Label>Company description and target</Form.Label>
              <Form.Control
                as="textarea"
                rows={3}
                placeholder="Describe the company and the desired ad"
                name="companyDescription"
                value={formData.companyDescription}
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

export default RequestAdPage;
