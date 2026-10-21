import { useEffect, useState } from 'react';
import { Button, Modal } from 'react-bootstrap';
import { useFetcher, useNavigate } from 'react-router';
import type { Partner } from '@/models';
import { useResolvedLoaderData } from '@/router/asyncData';
import DetailPanel from '@components/DetailPanel';
import PageHeader from '@components/PageHeader';
import { PageSkeleton } from '@components/LoadingSkeleton';
import { useToast } from '@components/ToastNotification/ToastContext';

export default function PartnerDetail() {
  const partner = useResolvedLoaderData<Partner>();
  const fetcher = useFetcher();
  const navigate = useNavigate();
  const toast = useToast();
  const [showDelete, setShowDelete] = useState(false);
  useEffect(() => {
    if (fetcher.data?.success) {
      toast.show('Success', 'Partner deleted successfully.', { variant: 'success' });
      navigate('..');
    }
  }, [fetcher.data, navigate, toast]);
  if (!partner) return <PageSkeleton />;
  return (
    <>
      <PageHeader
        title={partner.name}
        editLink={`/partners/${partner.id}/edit`}
        deleteCallback={() => setShowDelete(true)}
      />
      <DetailPanel title="Partner Details">
        <p>
          <strong>Status:</strong> {String(partner.status)}
        </p>
        <p>
          <strong>Contact:</strong> {partner.contactName || 'Not provided'}
        </p>
        <p>
          <strong>Email:</strong> {partner.contactEmail || 'Not provided'}
        </p>
        <p>
          <strong>Phone:</strong> {partner.contactPhone || 'Not provided'}
        </p>
        <p>
          <strong>Website:</strong> {partner.websiteUrl || 'Not provided'}
        </p>
        <p>
          <strong>Notes:</strong> {partner.notes || 'None'}
        </p>
      </DetailPanel>
      <Modal show={showDelete} onHide={() => setShowDelete(false)}>
        <Modal.Header closeButton>
          <Modal.Title>Confirm Delete</Modal.Title>
        </Modal.Header>
        <Modal.Body>Are you sure you want to delete {partner.name}?</Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowDelete(false)}>
            Cancel
          </Button>
          <Button
            variant="danger"
            disabled={fetcher.state !== 'idle'}
            onClick={() =>
              fetcher.submit({ id: partner.id }, { method: 'post', action: '/partners' })
            }
          >
            Delete
          </Button>
        </Modal.Footer>
      </Modal>
    </>
  );
}
