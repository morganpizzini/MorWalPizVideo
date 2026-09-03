import React, { useState } from 'react';
import { Alert, Button, Form } from 'react-bootstrap';
import { ComposeUrl, endpoints, post } from '@morwalpizvideo/services';

type ShareShortLinkProps = {
  shortLinkId: string;
};

const ShareShortLink: React.FC<ShareShortLinkProps> = ({ shortLinkId }) => {
  const [platform, setPlatform] = useState('telegram');
  const [message, setMessage] = useState('Guarda il mio ultimo video:');
  const [busy, setBusy] = useState(false);
  const [feedback, setFeedback] = useState<{ kind: 'success' | 'danger'; text: string } | null>(null);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setFeedback(null);
    try {
      await post(ComposeUrl(endpoints.SHORTLINKS_SHARE, { querylinkId: shortLinkId }), {
        platform,
        message
      });
      setFeedback({ kind: 'success', text: 'Published successfully.' });
    } catch (error) {
      setFeedback({
        kind: 'danger',
        text: error instanceof Error ? error.message : 'Social publication failed.'
      });
    } finally {
      setBusy(false);
    }
  };

  return (
    <Form onSubmit={handleSubmit} className="mt-3">
      <Form.Group className="mb-3" controlId="sharePlatform">
        <Form.Label>Platform</Form.Label>
        <Form.Select value={platform} onChange={event => setPlatform(event.target.value)} disabled={busy}>
          <option value="telegram">Telegram</option>
          <option value="discord">Discord</option>
          <option value="facebook">Facebook</option>
        </Form.Select>
      </Form.Group>
      <Form.Group className="mb-3" controlId="shareMessage">
        <Form.Label>Message</Form.Label>
        <Form.Control
          as="textarea"
          rows={3}
          value={message}
          onChange={event => setMessage(event.target.value)}
          disabled={busy}
          required
        />
      </Form.Group>
      {feedback && <Alert variant={feedback.kind}>{feedback.text}</Alert>}
      <Button type="submit" disabled={busy || !message.trim()}>
        {busy ? 'Publishing...' : 'Publish'}
      </Button>
    </Form>
  );
};

export default ShareShortLink;