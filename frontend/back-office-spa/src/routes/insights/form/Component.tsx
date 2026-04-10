import React, { useState, useEffect } from 'react';
import { Form, Button, Modal, Badge } from 'react-bootstrap';
import { useNavigate, useFetcher, useLoaderData, useParams } from 'react-router';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';
import { useToast } from '@components/ToastNotification/ToastContext';
import PageHeader from '@components/PageHeader';
import { InsightTopic } from '@morwalpizvideo/models';

const InsightTopicForm: React.FC = () => {
  const params = useParams();
  const isEditMode = !!params.id;
  const existingTopic = useLoaderData<InsightTopic | null>();

  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [seedArguments, setSeedArguments] = useState<string[]>([]);
  const [preferredSources, setPreferredSources] = useState<string[]>([]);
  const [newArgument, setNewArgument] = useState('');
  const [newSource, setNewSource] = useState('');
  const [showModal, setShowModal] = useState(false);

  const navigate = useNavigate();
  const toast = useToast();
  const fetcher = useFetcher();
  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;
  const result =
    fetcher.data != undefined &&
    (fetcher.data.errors == undefined || fetcher.data.errors.length == 0)
      ? fetcher.data
      : null;

  // Load existing topic data in edit mode
  useEffect(() => {
    if (isEditMode && existingTopic) {
      setTitle(existingTopic.title || '');
      setDescription(existingTopic.description || '');
      setSeedArguments(existingTopic.seedArguments || []);
      setPreferredSources(existingTopic.preferredSources || []);
    }
  }, [isEditMode, existingTopic]);

  useEffect(() => {
    if (!result) return;
    setShowModal(false);

    if (result.success) {
      toast.show(
        'Success',
        `Topic ${isEditMode ? 'updated' : 'created'} successfully`,
        { variant: 'success' }
      );
      navigate('/insights');
    }
  }, [result, navigate, isEditMode]);

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    setShowModal(true);
  };

  const confirmSubmit = () => {
    const payload = {
      title,
      description,
      seedArguments: JSON.stringify(seedArguments),
      preferredSources: JSON.stringify(preferredSources),
      id: isEditMode ? params.id : undefined,
    };

    fetcher.submit(payload, {
      method: 'post',
      action: location.pathname,
    });
  };

  const handleAddArgument = () => {
    if (!newArgument.trim()) return;
    if (seedArguments.includes(newArgument.trim())) {
      toast.show('Warning', 'Argument already exists', { variant: 'warning' });
      return;
    }
    setSeedArguments([...seedArguments, newArgument.trim()]);
    setNewArgument('');
  };

  const handleRemoveArgument = (arg: string) => {
    setSeedArguments(seedArguments.filter(a => a !== arg));
  };

  const handleAddSource = () => {
    if (!newSource.trim()) return;
    if (preferredSources.includes(newSource.trim())) {
      toast.show('Warning', 'Source already exists', { variant: 'warning' });
      return;
    }
    setPreferredSources([...preferredSources, newSource.trim()]);
    setNewSource('');
  };

  const handleRemoveSource = (source: string) => {
    setPreferredSources(preferredSources.filter(s => s !== source));
  };

  const isDisabled = () => {
    return title.trim().length === 0 || description.trim().length === 0 || busy;
  };

  return (
    <>
      <PageHeader title={isEditMode ? 'Edit Topic' : 'Create Topic'} />
      <GenericErrorList errors={errors?.generics} />

      <Form onSubmit={handleSubmit}>
        <Form.Group controlId="formTitle" className="mb-3">
          <Form.Label>
            Title <span className="text-danger">*</span>
          </Form.Label>
          <Form.Control
            type="text"
            value={title}
            onChange={e => setTitle(e.target.value)}
            placeholder="Enter topic title"
          />
          <FieldError error={errors?.title} />
        </Form.Group>

        <Form.Group controlId="formDescription" className="mb-3">
          <Form.Label>
            Description <span className="text-danger">*</span>
          </Form.Label>
          <Form.Control
            as="textarea"
            rows={3}
            value={description}
            onChange={e => setDescription(e.target.value)}
            placeholder="Enter topic description"
          />
          <FieldError error={errors?.description} />
        </Form.Group>

        <Form.Group controlId="formArguments" className="mb-3">
          <Form.Label>Seed Arguments</Form.Label>
          <div className="d-flex gap-2 mb-2">
            <Form.Control
              type="text"
              value={newArgument}
              onChange={e => setNewArgument(e.target.value)}
              placeholder="Enter an argument keyword"
              onKeyPress={e => e.key === 'Enter' && (e.preventDefault(), handleAddArgument())}
            />
            <Button variant="primary" onClick={handleAddArgument}>
              Add
            </Button>
          </div>
          {seedArguments.length > 0 && (
            <div className="border rounded p-3 bg-light">
              <div className="d-flex flex-wrap gap-2">
                {seedArguments.map(arg => (
                  <Badge key={arg} bg="primary" className="d-flex align-items-center gap-2 p-2">
                    <span>{arg}</span>
                    <button
                      type="button"
                      className="btn-close btn-close-white"
                      aria-label="Remove"
                      onClick={() => handleRemoveArgument(arg)}
                      style={{ fontSize: '0.6rem' }}
                    />
                  </Badge>
                ))}
              </div>
            </div>
          )}
          <Form.Text className="text-muted">
            Keywords that help AI discover relevant news
          </Form.Text>
          <FieldError error={errors?.seedArguments} />
        </Form.Group>

        <Form.Group controlId="formSources" className="mb-3">
          <Form.Label>Preferred Sources</Form.Label>
          <div className="d-flex gap-2 mb-2">
            <Form.Control
              type="text"
              value={newSource}
              onChange={e => setNewSource(e.target.value)}
              placeholder="Enter a source URL or name"
              onKeyPress={e => e.key === 'Enter' && (e.preventDefault(), handleAddSource())}
            />
            <Button variant="primary" onClick={handleAddSource}>
              Add
            </Button>
          </div>
          {preferredSources.length > 0 && (
            <div className="border rounded p-3 bg-light">
              <div className="d-flex flex-wrap gap-2">
                {preferredSources.map(source => (
                  <Badge key={source} bg="secondary" className="d-flex align-items-center gap-2 p-2">
                    <span>{source}</span>
                    <button
                      type="button"
                      className="btn-close btn-close-white"
                      aria-label="Remove"
                      onClick={() => handleRemoveSource(source)}
                      style={{ fontSize: '0.6rem' }}
                    />
                  </Badge>
                ))}
              </div>
            </div>
          )}
          <Form.Text className="text-muted">
            URLs or names of trusted news sources
          </Form.Text>
          <FieldError error={errors?.preferredSources} />
        </Form.Group>

        <Button variant="success" disabled={isDisabled()} type="submit" className="mt-2">
          {isEditMode ? 'Update Topic' : 'Create Topic'}
        </Button>
      </Form>

      <Modal show={showModal} onHide={() => setShowModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>Confirm {isEditMode ? 'Update' : 'Create'}</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <p>Are you sure you want to {isEditMode ? 'update' : 'create'} this topic?</p>
          <p>
            <strong>Title:</strong> {title}
          </p>
          <p>
            <strong>Description:</strong> {description}
          </p>
          <p>
            <strong>Arguments:</strong> {seedArguments.length}
          </p>
          <p>
            <strong>Sources:</strong> {preferredSources.length}
          </p>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowModal(false)}>
            Cancel
          </Button>
          <Button
            variant="success"
            onClick={confirmSubmit}
            disabled={busy}
            data-testid="submit-modal-confirm"
          >
            {isEditMode ? 'Update' : 'Create'}
          </Button>
        </Modal.Footer>
      </Modal>
    </>
  );
};

export default InsightTopicForm;