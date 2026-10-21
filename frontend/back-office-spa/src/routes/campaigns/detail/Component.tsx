import { useEffect, useState } from 'react';
import { Button, Modal, Table } from 'react-bootstrap';
import { useFetcher, useNavigate } from 'react-router';
import { Campaign, CampaignStatus, TimelineEntryKind } from '@/models';
import { useResolvedLoaderData } from '@/router/asyncData';
import DetailPanel from '@components/DetailPanel';
import PageHeader from '@components/PageHeader';
import { PageSkeleton } from '@components/LoadingSkeleton';
import { useToast } from '@components/ToastNotification/ToastContext';

export default function CampaignDetail() {
  const campaign = useResolvedLoaderData<Campaign>();
  const fetcher = useFetcher();
  const navigate = useNavigate();
  const toast = useToast();
  const [showDelete, setShowDelete] = useState(false);
  useEffect(() => {
    if (fetcher.data?.success) {
      toast.show('Success', 'Campaign deleted successfully.', { variant: 'success' });
      navigate('..');
    }
  }, [fetcher.data, navigate, toast]);
  if (!campaign) return <PageSkeleton />;
  return (
    <>
      <PageHeader
        title={campaign.title}
        editLink={`/campaigns/${campaign.id}/edit`}
        deleteCallback={() => setShowDelete(true)}
      />
      <DetailPanel title="Campaign Details">
        <p>
          <strong>Partner:</strong> {campaign.partnerId}
        </p>
        <p>
          <strong>Status:</strong> {CampaignStatus[campaign.status] ?? String(campaign.status)}
        </p>
        <p>
          <strong>Schedule:</strong> {campaign.startAt?.slice(0, 10) ?? 'Not scheduled'} to{' '}
          {campaign.endAt?.slice(0, 10) ?? 'Open ended'}
        </p>
        <p>
          <strong>Description:</strong> {campaign.description || 'None'}
        </p>
        <p>
          <strong>Content references:</strong>{' '}
          {campaign.contentReferences.length ? campaign.contentReferences.join(', ') : 'None'}
        </p>
      </DetailPanel>
      <DetailPanel title="Timeline">
        {campaign.timelineEntries.length === 0 ? (
          <p>No timeline entries.</p>
        ) : (
          <Table responsive>
            <thead>
              <tr>
                <th>Title</th>
                <th>Kind</th>
                <th>Due</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {campaign.timelineEntries.map(entry => (
                <tr key={entry.id}>
                  <td>{entry.title}</td>
                  <td>{TimelineEntryKind[entry.kind] ?? String(entry.kind)}</td>
                  <td>{entry.dueAt?.slice(0, 10) ?? 'None'}</td>
                  <td>{entry.isComplete ? 'Complete' : 'Open'}</td>
                </tr>
              ))}
            </tbody>
          </Table>
        )}
      </DetailPanel>
      <Modal show={showDelete} onHide={() => setShowDelete(false)}>
        <Modal.Header closeButton>
          <Modal.Title>Confirm Delete</Modal.Title>
        </Modal.Header>
        <Modal.Body>Are you sure you want to delete {campaign.title}?</Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowDelete(false)}>
            Cancel
          </Button>
          <Button
            variant="danger"
            disabled={fetcher.state !== 'idle'}
            onClick={() =>
              fetcher.submit({ id: campaign.id }, { method: 'post', action: '/campaigns' })
            }
          >
            Delete
          </Button>
        </Modal.Footer>
      </Modal>
    </>
  );
}
