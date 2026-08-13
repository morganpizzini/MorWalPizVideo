import React, { useEffect, useState } from 'react';
import { Button, Form } from 'react-bootstrap';
import { useFetcher, useLoaderData, useNavigate, useParams } from 'react-router';
import type { QuickLink, QuickLinks } from '@morwalpizvideo/models';
import { QuickLinkKind } from '@morwalpizvideo/models';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';
import PageHeader from '@components/PageHeader';

const kindOptions = Object.entries(QuickLinkKind) as [string, number][];

const emptyLink = (): QuickLink => ({ kind: QuickLinkKind.External, targetUrl: '', title: '' });

const QuickLinksForm: React.FC = () => {
  const { quickLinks } = useLoaderData() as { quickLinks: QuickLinks | null };
  const params = useParams();
  const navigate = useNavigate();
  const toast = useToast();
  const fetcher = useFetcher();
  const [title, setTitle] = useState('');
  const [subtitle, setSubtitle] = useState('');
  const [url, setUrl] = useState('');
  const [links, setLinks] = useState<QuickLink[]>([]);
  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;

  useEffect(() => {
    setTitle(quickLinks?.title ?? '');
    setSubtitle(quickLinks?.subtitle ?? '');
    setUrl(quickLinks?.url ?? '');
    setLinks(quickLinks?.links ?? []);
  }, [quickLinks]);

  useEffect(() => {
    if (busy || !fetcher.data?.success) return;
    toast.show('Success', params.id ? 'QuickLinks updated successfully' : 'QuickLinks created successfully', { variant: 'success' });
    navigate('/quicklinks');
  }, [busy, fetcher.data, navigate, params.id, toast]);

  const updateLink = (index: number, patch: Partial<QuickLink>) => {
    setLinks(current => current.map((link, linkIndex) => linkIndex === index ? { ...link, ...patch } : link));
  };

  return (
    <>
      <PageHeader title={params.id ? 'Edit QuickLinks' : 'Create QuickLinks'} />
      <GenericErrorList errors={errors?.generics} />
      <fetcher.Form method="post">
        <Form.Group className="mb-3">
          <Form.Label>Title</Form.Label>
          <Form.Control value={title} onChange={event => setTitle(event.target.value)} name="title" required />
          <FieldError error={errors?.title} />
        </Form.Group>
        <Form.Group className="mb-3">
          <Form.Label>Subtitle</Form.Label>
          <Form.Control value={subtitle} onChange={event => setSubtitle(event.target.value)} name="subtitle" />
        </Form.Group>
        <Form.Group className="mb-3">
          <Form.Label>Public URL slug</Form.Label>
          <Form.Control value={url} onChange={event => setUrl(event.target.value)} name="url" required pattern="[A-Za-z0-9][A-Za-z0-9_-]*" />
          <FieldError error={errors?.url} />
        </Form.Group>

        <fieldset className="mb-3">
          <legend className="h5">Links</legend>
          {links.map((link, index) => (
            <div className="border rounded p-3 mb-3" key={`${index}-${link.targetUrl}`}>
              <div className="row g-2">
                <Form.Group className="col-md-3">
                  <Form.Label>Kind</Form.Label>
                  <Form.Select value={link.kind} onChange={event => updateLink(index, { kind: Number(event.target.value) as QuickLink['kind'] })}>
                    {kindOptions.map(([label, value]) => <option key={label} value={value}>{label}</option>)}
                  </Form.Select>
                </Form.Group>
                <Form.Group className="col-md-9">
                  <Form.Label>Target URL</Form.Label>
                  <Form.Control value={link.targetUrl} onChange={event => updateLink(index, { targetUrl: event.target.value })} required />
                </Form.Group>
                <Form.Group className="col-md-6">
                  <Form.Label>Title</Form.Label>
                  <Form.Control value={link.title ?? ''} onChange={event => updateLink(index, { title: event.target.value })} />
                </Form.Group>
                <Form.Group className="col-md-6">
                  <Form.Label>Label</Form.Label>
                  <Form.Control value={link.label ?? ''} onChange={event => updateLink(index, { label: event.target.value })} />
                </Form.Group>
                <Form.Group className="col-12">
                  <Form.Label>Description</Form.Label>
                  <Form.Control value={link.subtitle ?? ''} onChange={event => updateLink(index, { subtitle: event.target.value })} />
                </Form.Group>
              </div>
              <Button type="button" variant="outline-danger" className="mt-2" onClick={() => setLinks(current => current.filter((_, linkIndex) => linkIndex !== index))}>Remove link</Button>
            </div>
          ))}
          <Button type="button" variant="outline-secondary" onClick={() => setLinks(current => [...current, emptyLink()])}>Add link</Button>
        </fieldset>
        <input type="hidden" name="links" value={JSON.stringify(links)} />
        <div className="d-flex gap-2">
          <Button type="submit" disabled={busy}>{busy ? 'Saving...' : 'Save QuickLinks'}</Button>
          <Button type="button" variant="secondary" onClick={() => navigate('/quicklinks')} disabled={busy}>Cancel</Button>
        </div>
      </fetcher.Form>
    </>
  );
};

export default QuickLinksForm;
