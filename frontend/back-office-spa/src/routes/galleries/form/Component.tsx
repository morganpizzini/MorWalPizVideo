import { useResolvedLoaderData } from '@/router/asyncData';
import React, { useEffect, useRef, useState } from 'react';
import { Button, Form } from 'react-bootstrap';
import { useFetcher, useNavigate, useParams } from 'react-router';
import type { Gallery } from '@morwalpizvideo/models';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';
import PageHeader from '@components/PageHeader';

export default function GalleryForm(): React.ReactElement {
  const gallery = useResolvedLoaderData<Gallery | null>();
  const { id } = useParams();
  const isEdit = Boolean(id);
  const [name, setName] = useState('');
  const [slug, setSlug] = useState('');
  const [description, setDescription] = useState('');
  const [expirationDays, setExpirationDays] = useState('7');
  const fetcher = useFetcher();
  const navigate = useNavigate();
  const toast = useToast();
  const lastResult = useRef<unknown>(undefined);
  const busy = fetcher.state !== 'idle';

  useEffect(() => {
    setName(gallery?.name ?? '');
    setSlug(gallery?.slug ?? '');
    setDescription(gallery?.description ?? '');
    setExpirationDays('7');
  }, [gallery]);

  useEffect(() => {
    if (busy || !fetcher.data || lastResult.current === fetcher.data) return;
    lastResult.current = fetcher.data;
    if (fetcher.data.success) {
      toast.show(
        'Success',
        isEdit ? 'Gallery updated successfully' : 'Gallery created successfully',
        { variant: 'success' }
      );
      navigate(isEdit ? `/galleries/${id}` : '/galleries');
    }
  }, [busy, fetcher.data, id, isEdit, navigate, toast]);

  return (
    <>
      <PageHeader
        title={isEdit ? `Edit gallery: ${gallery?.name ?? ''}` : 'Create gallery'}
        backLink="/galleries"
      />
      <GenericErrorList errors={fetcher.data?.errors?.generics} />
      <fetcher.Form method="post">
        <Form.Group className="mb-3">
          <Form.Label>Name</Form.Label>
          <Form.Control
            name="name"
            value={name}
            onChange={event => setName(event.target.value)}
            required
          />
          <FieldError error={fetcher.data?.errors?.name} />
        </Form.Group>
        <Form.Group className="mb-3">
          <Form.Label>Public slug</Form.Label>
          <Form.Control
            name="slug"
            value={slug}
            onChange={event => setSlug(event.target.value)}
            required
          />
          <FieldError error={fetcher.data?.errors?.slug} />
        </Form.Group>
        <Form.Group className="mb-3">
          <Form.Label>Description</Form.Label>
          <Form.Control
            as="textarea"
            name="description"
            value={description}
            onChange={event => setDescription(event.target.value)}
          />
        </Form.Group>
        <Form.Group className="mb-3">
          <Form.Label>Expiration</Form.Label>
          <Form.Select
            name="expirationDays"
            value={expirationDays}
            onChange={event => setExpirationDays(event.target.value)}
          >
            <option value="3">3 days</option>
            <option value="7">7 days</option>
            <option value="21">21 days</option>
          </Form.Select>
          <FieldError error={fetcher.data?.errors?.expirationDays} />
        </Form.Group>
        <div className="d-flex gap-2">
          <Button type="submit" disabled={busy}>
            {isEdit ? 'Update gallery' : 'Create gallery'}
          </Button>
          <Button variant="secondary" as="a" href="/galleries">
            Cancel
          </Button>
        </div>
      </fetcher.Form>
    </>
  );
}
