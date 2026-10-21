import { useEffect, useRef, useState } from 'react';
import { Button, Form } from 'react-bootstrap';
import { useFetcher, useNavigate, useParams } from 'react-router';
import { CampaignStatus, type Campaign, type Partner } from '@/models';
import { useResolvedLoaderData } from '@/router/asyncData';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';
import PageHeader from '@components/PageHeader';
import { useToast } from '@components/ToastNotification/ToastContext';

interface FormData {
  campaign: Campaign | null;
  partners: Partner[];
}
export default function CampaignForm() {
  const { campaign, partners } = useResolvedLoaderData<FormData>();
  const edit = Boolean(useParams().id);
  const fetcher = useFetcher();
  const navigate = useNavigate();
  const toast = useToast();
  const lastResult = useRef<unknown>(undefined);
  const [form, setForm] = useState({
    partnerId: '',
    title: '',
    description: '',
    status: CampaignStatus.Draft,
    startAt: '',
    endAt: '',
    contentReferences: '',
  });
  useEffect(
    () =>
      setForm({
        partnerId: campaign?.partnerId ?? '',
        title: campaign?.title ?? '',
        description: campaign?.description ?? '',
        status: campaign?.status ?? CampaignStatus.Draft,
        startAt: campaign?.startAt?.slice(0, 16) ?? '',
        endAt: campaign?.endAt?.slice(0, 16) ?? '',
        contentReferences: campaign?.contentReferences.join('\n') ?? '',
      }),
    [campaign]
  );
  useEffect(() => {
    if (fetcher.state === 'idle' && fetcher.data && fetcher.data !== lastResult.current) {
      lastResult.current = fetcher.data;
      if (fetcher.data.success) {
        toast.show(
          'Success',
          edit ? 'Campaign updated successfully.' : 'Campaign created successfully.',
          { variant: 'success' }
        );
        navigate(edit ? '..' : '/campaigns');
      }
    }
  }, [edit, fetcher.data, fetcher.state, navigate, toast]);
  const update = (key: keyof typeof form, value: string | number) =>
    setForm(current => ({ ...current, [key]: key === 'status' ? Number(value) : value }));
  const errors = fetcher.data?.errors;
  return (
    <>
      <PageHeader title={edit ? `Edit Campaign: ${campaign?.title ?? ''}` : 'Create Campaign'} />
      <GenericErrorList errors={errors?.generics} />
      <Form
        onSubmit={event => {
          event.preventDefault();
          fetcher.submit(form, { method: 'post' });
        }}
      >
        <Form.Group className="mb-3">
          <Form.Label>Partner</Form.Label>
          <Form.Select
            value={form.partnerId}
            onChange={event => update('partnerId', event.target.value)}
            required
          >
            <option value="">Select a partner</option>
            {partners.map(partner => (
              <option key={partner.id} value={partner.id}>
                {partner.name}
              </option>
            ))}
          </Form.Select>
          <FieldError error={errors?.partnerId} />
        </Form.Group>
        <Form.Group className="mb-3">
          <Form.Label>Title</Form.Label>
          <Form.Control
            value={form.title}
            onChange={event => update('title', event.target.value)}
            required
          />
          <FieldError error={errors?.title} />
        </Form.Group>
        <Form.Group className="mb-3">
          <Form.Label>Status</Form.Label>
          <Form.Select value={form.status} onChange={event => update('status', event.target.value)}>
            {Object.entries(CampaignStatus)
              .filter(([, value]) => typeof value === 'number')
              .map(([label, value]) => (
                <option key={value} value={value}>
                  {label}
                </option>
              ))}
          </Form.Select>
        </Form.Group>
        <Form.Group className="mb-3">
          <Form.Label>Start</Form.Label>
          <Form.Control
            type="datetime-local"
            value={form.startAt}
            onChange={event => update('startAt', event.target.value)}
          />
        </Form.Group>
        <Form.Group className="mb-3">
          <Form.Label>End</Form.Label>
          <Form.Control
            type="datetime-local"
            value={form.endAt}
            onChange={event => update('endAt', event.target.value)}
          />
        </Form.Group>
        <Form.Group className="mb-3">
          <Form.Label>Description</Form.Label>
          <Form.Control
            as="textarea"
            rows={4}
            value={form.description}
            onChange={event => update('description', event.target.value)}
          />
        </Form.Group>
        <Form.Group className="mb-3">
          <Form.Label>Content references</Form.Label>
          <Form.Control
            as="textarea"
            rows={4}
            value={form.contentReferences}
            onChange={event => update('contentReferences', event.target.value)}
          />
        </Form.Group>
        <Button
          type="submit"
          disabled={fetcher.state !== 'idle' || !form.partnerId || !form.title.trim()}
        >
          {edit ? 'Update' : 'Create'}
        </Button>
      </Form>
    </>
  );
}
