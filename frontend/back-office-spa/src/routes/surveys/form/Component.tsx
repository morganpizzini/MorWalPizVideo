import { useEffect, useState } from 'react';
import { Button, Form } from 'react-bootstrap';
import { useFetcher, useLoaderData, useNavigate, useParams } from 'react-router';
import { endpoints, get } from '@morwalpizvideo/services';
import type { CustomForm, Survey } from '@morwalpizvideo/models';
import PageHeader from '@components/PageHeader';
import { useToast } from '@components/ToastNotification/ToastContext';

export default function SurveyForm() {
  const existing = useLoaderData() as Survey | null;
  const [forms, setForms] = useState<CustomForm[]>([]);
  const [title, setTitle] = useState(existing?.title ?? '');
  const [description, setDescription] = useState(existing?.description ?? '');
  const [url, setUrl] = useState(existing?.url ?? '');
  const [fromUtc, setFromUtc] = useState(existing?.fromUtc?.slice(0, 16) ?? '');
  const [toUtc, setToUtc] = useState(existing?.toUtc?.slice(0, 16) ?? '');
  const [formIds, setFormIds] = useState<string[]>(existing?.formIds ?? []);
  const [lifecycle, setLifecycle] = useState(existing?.lifecycle ?? 'Draft');
  const params = useParams();
  const navigate = useNavigate();
  const fetcher = useFetcher();
  const toast = useToast();

  useEffect(() => {
    get(endpoints.CUSTOMFORMS).then(setForms);
  }, []);
  useEffect(() => {
    if (fetcher.data?.success === false)
      toast.show('Save failed', fetcher.data.errors.generics.join(' '), { variant: 'danger' });
  }, [fetcher.data, toast]);

  return (
    <>
      <PageHeader title={params.id ? 'Edit Survey' : 'Create Survey'} />
      <fetcher.Form method="post">
        <Form.Group className="mb-3">
          <Form.Label>Title</Form.Label>
          <Form.Control
            value={title}
            onChange={e => setTitle(e.target.value)}
            name="title"
            required
          />
        </Form.Group>
        <Form.Group className="mb-3">
          <Form.Label>Description</Form.Label>
          <Form.Control
            as="textarea"
            value={description}
            onChange={e => setDescription(e.target.value)}
            name="description"
          />
        </Form.Group>
        <Form.Group className="mb-3">
          <Form.Label>URL</Form.Label>
          <Form.Control
            value={url}
            onChange={e => setUrl(e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, '-'))}
            name="url"
            required
          />
        </Form.Group>
        <div className="row">
          <Form.Group className="mb-3 col-md-6">
            <Form.Label>From</Form.Label>
            <Form.Control
              type="datetime-local"
              value={fromUtc}
              onChange={e => setFromUtc(e.target.value)}
              name="fromUtc"
              required
            />
          </Form.Group>
          <Form.Group className="mb-3 col-md-6">
            <Form.Label>To</Form.Label>
            <Form.Control
              type="datetime-local"
              value={toUtc}
              onChange={e => setToUtc(e.target.value)}
              name="toUtc"
              required
            />
          </Form.Group>
        </div>
        <Form.Group className="mb-3">
          <Form.Label>Status</Form.Label>
          <Form.Select
            name="lifecycle"
            value={lifecycle}
            onChange={e => setLifecycle(e.target.value as typeof lifecycle)}
          >
            <option>Draft</option>
            <option>Online</option>
            <option>Closed</option>
            <option>Archived</option>
          </Form.Select>
        </Form.Group>
        <Form.Group className="mb-3">
          <Form.Label>Forms</Form.Label>
          {forms.map(form => (
            <Form.Check
              key={form.id}
              type="checkbox"
              label={`${form.title} (${form.accessMode ?? 'Direct'})`}
              checked={formIds.includes(form.id)}
              onChange={e =>
                setFormIds(current =>
                  e.target.checked ? [...current, form.id] : current.filter(id => id !== form.id)
                )
              }
            />
          ))}
        </Form.Group>
        <input type="hidden" name="formIds" value={JSON.stringify(formIds)} />
        <Button
          variant="secondary"
          type="button"
          onClick={() => navigate('/surveys')}
          className="me-2"
        >
          Cancel
        </Button>
        <Button type="submit" disabled={fetcher.state !== 'idle'}>
          Save
        </Button>
      </fetcher.Form>
    </>
  );
}
