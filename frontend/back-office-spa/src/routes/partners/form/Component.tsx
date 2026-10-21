import { useEffect, useRef, useState } from 'react';
import { Button, Form } from 'react-bootstrap';
import { useFetcher, useNavigate, useParams } from 'react-router';
import { PartnerStatus, type Partner } from '@/models';
import { useResolvedLoaderData } from '@/router/asyncData';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';
import PageHeader from '@components/PageHeader';
import { useToast } from '@components/ToastNotification/ToastContext';

export default function PartnerForm() {
  const partner = useResolvedLoaderData<Partner | null>();
  const edit = Boolean(useParams().id);
  const fetcher = useFetcher();
  const navigate = useNavigate();
  const toast = useToast();
  const lastResult = useRef<unknown>(undefined);
  const [form, setForm] = useState({
    name: '',
    contactName: '',
    contactEmail: '',
    contactPhone: '',
    websiteUrl: '',
    notes: '',
    status: PartnerStatus.Prospect,
  });

  useEffect(() => {
    setForm({
      name: partner?.name ?? '',
      contactName: partner?.contactName ?? '',
      contactEmail: partner?.contactEmail ?? '',
      contactPhone: partner?.contactPhone ?? '',
      websiteUrl: partner?.websiteUrl ?? '',
      notes: partner?.notes ?? '',
      status: partner?.status ?? PartnerStatus.Prospect,
    });
  }, [partner]);
  useEffect(() => {
    if (fetcher.state === 'idle' && fetcher.data && fetcher.data !== lastResult.current) {
      lastResult.current = fetcher.data;
      if (fetcher.data.success) {
        toast.show(
          'Success',
          edit ? 'Partner updated successfully.' : 'Partner created successfully.',
          { variant: 'success' }
        );
        navigate(edit ? '..' : '/partners');
      }
    }
  }, [edit, fetcher.data, fetcher.state, navigate, toast]);
  const update = (key: keyof typeof form, value: string | number) =>
    setForm(current => ({ ...current, [key]: key === 'status' ? Number(value) : value }));
  const errors = fetcher.data?.errors;
  return (
    <>
      <PageHeader title={edit ? `Edit Partner: ${partner?.name ?? ''}` : 'Create Partner'} />
      <GenericErrorList errors={errors?.generics} />
      <Form
        onSubmit={event => {
          event.preventDefault();
          fetcher.submit(form, { method: 'post' });
        }}
      >
        <Form.Group className="mb-3">
          <Form.Label>Name</Form.Label>
          <Form.Control
            value={form.name}
            onChange={event => update('name', event.target.value)}
            required
          />
          <FieldError error={errors?.name} />
        </Form.Group>
        <Form.Group className="mb-3">
          <Form.Label>Contact name</Form.Label>
          <Form.Control
            value={form.contactName}
            onChange={event => update('contactName', event.target.value)}
          />
        </Form.Group>
        <Form.Group className="mb-3">
          <Form.Label>Email</Form.Label>
          <Form.Control
            type="email"
            value={form.contactEmail}
            onChange={event => update('contactEmail', event.target.value)}
          />
        </Form.Group>
        <Form.Group className="mb-3">
          <Form.Label>Phone</Form.Label>
          <Form.Control
            value={form.contactPhone}
            onChange={event => update('contactPhone', event.target.value)}
          />
        </Form.Group>
        <Form.Group className="mb-3">
          <Form.Label>Website</Form.Label>
          <Form.Control
            type="url"
            value={form.websiteUrl}
            onChange={event => update('websiteUrl', event.target.value)}
          />
        </Form.Group>
        <Form.Group className="mb-3">
          <Form.Label>Status</Form.Label>
          <Form.Select value={form.status} onChange={event => update('status', event.target.value)}>
            {Object.entries(PartnerStatus)
              .filter(([, value]) => typeof value === 'number')
              .map(([label, value]) => (
                <option key={value} value={value}>
                  {label}
                </option>
              ))}
          </Form.Select>
        </Form.Group>
        <Form.Group className="mb-3">
          <Form.Label>Notes</Form.Label>
          <Form.Control
            as="textarea"
            rows={4}
            value={form.notes}
            onChange={event => update('notes', event.target.value)}
          />
        </Form.Group>
        <Button type="submit" disabled={fetcher.state !== 'idle' || !form.name.trim()}>
          {edit ? 'Update' : 'Create'}
        </Button>
      </Form>
    </>
  );
}
