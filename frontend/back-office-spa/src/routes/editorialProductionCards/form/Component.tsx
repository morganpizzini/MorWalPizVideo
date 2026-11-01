import React, { useEffect, useRef, useState } from 'react';
import { Button, Form } from 'react-bootstrap';
import { useFetcher, useNavigate, useParams } from 'react-router';
import { useResolvedLoaderData } from '@/router/asyncData';
import type { EditorialProductionCard } from '@morwalpizvideo/models';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';
import PageHeader from '@components/PageHeader';
import { useToast } from '@components/ToastNotification/ToastContext';

const statusOptions = [
  ['0', 'Idea'],
  ['1', 'Planned'],
  ['2', 'In progress'],
  ['3', 'Ready'],
  ['4', 'Published'],
  ['5', 'Archived'],
];

export default function EditorialProductionCardForm(): React.ReactElement {
  const entity = useResolvedLoaderData() as EditorialProductionCard | null;
  const { id } = useParams();
  const navigate = useNavigate();
  const fetcher = useFetcher();
  const toast = useToast();
  const [values, setValues] = useState<Record<string, string>>({});
  const lastResult = useRef<unknown>(undefined);
  const busy = fetcher.state !== 'idle';

  useEffect(() => {
    setValues({
      title: entity?.title ?? '',
      editorialAngle: entity?.editorialAngle ?? '',
      sourceUrls: entity?.sourceUrls.join('\n') ?? '',
      brief: entity?.brief ?? '',
      script: entity?.script ?? '',
      status: String(entity?.status ?? 0),
      ownerUserId: entity?.ownerUserId ?? '',
      dueDateUtc: entity?.dueDateUtc?.slice(0, 16) ?? '',
      topicId: entity?.topicId ?? '',
      newsItemId: entity?.newsItemId ?? '',
      contentPlanId: entity?.contentPlanId ?? '',
      youtubeVideoId: entity?.youtubeVideoId ?? '',
      backOfficeContentId: entity?.backOfficeContentId ?? '',
      revision: String(entity?.revision ?? 0),
    });
  }, [entity]);

  useEffect(() => {
    if (busy || !fetcher.data?.success || lastResult.current === fetcher.data) return;
    lastResult.current = fetcher.data;
    toast.show(
      'Success',
      id ? 'Editorial production card updated' : 'Editorial production card created',
      { variant: 'success' }
    );
    navigate(id ? `/editorialproductioncards/${id}` : '/editorialproductioncards');
  }, [busy, fetcher.data, id, navigate, toast]);

  const set = (name: string, value: string) =>
    setValues(current => ({ ...current, [name]: value }));
  const field = (name: string, label: string, type = 'text') => (
    <Form.Group className="mb-3" key={name}>
      <Form.Label>{label}</Form.Label>
      <Form.Control
        type={type}
        name={name}
        value={values[name] ?? ''}
        onChange={event => set(name, event.target.value)}
      />
      <FieldError error={fetcher.data?.errors?.[name]} />
    </Form.Group>
  );

  return (
    <>
      <PageHeader
        title={id ? 'Edit editorial production card' : 'Create editorial production card'}
      />
      <GenericErrorList errors={fetcher.data?.errors?.generics} />
      <fetcher.Form method="post">
        {field('title', 'Title')}
        {field('editorialAngle', 'Editorial angle')}
        <Form.Group className="mb-3">
          <Form.Label>Sources, one URL per line</Form.Label>
          <Form.Control
            as="textarea"
            rows={3}
            name="sourceUrls"
            value={values.sourceUrls ?? ''}
            onChange={event => set('sourceUrls', event.target.value)}
          />
        </Form.Group>
        {field('brief', 'Brief', 'textarea')}
        {field('script', 'Script', 'textarea')}
        <div className="row">
          <Form.Group className="col-md-4 mb-3">
            <Form.Label>Status</Form.Label>
            <Form.Select
              name="status"
              value={values.status ?? '0'}
              onChange={event => set('status', event.target.value)}
            >
              {statusOptions.map(([value, label]) => (
                <option key={value} value={value}>
                  {label}
                </option>
              ))}
            </Form.Select>
          </Form.Group>
          {field('dueDateUtc', 'Due date UTC', 'datetime-local')}
        </div>
        {field('ownerUserId', 'Owner user ID')}
        <div className="row">
          {field('topicId', 'Topic ID')}
          {field('newsItemId', 'News item ID')}
          {field('contentPlanId', 'Content plan ID')}
        </div>
        <div className="row">
          {field('youtubeVideoId', 'YouTube video ID')}
          {field('backOfficeContentId', 'BackOffice content ID')}
        </div>
        {id && <input type="hidden" name="revision" value={values.revision ?? '0'} />}
        <div className="d-flex gap-2">
          <Button type="submit" disabled={busy}>
            {busy ? 'Saving...' : 'Save card'}
          </Button>
          <Button
            type="button"
            variant="secondary"
            disabled={busy}
            onClick={() => navigate('/editorialproductioncards')}
          >
            Cancel
          </Button>
        </div>
      </fetcher.Form>
    </>
  );
}
