import React, { useState, useEffect } from 'react';
import { useLoaderData, useFetcher, useNavigate, useLocation } from 'react-router';
import { Button, Modal } from 'react-bootstrap';
import { useToast } from '@components/ToastNotification/ToastContext';
import DetailPanel from '@components/DetailPanel';
import PageHeader from '@components/PageHeader';
import { LinkType } from '@morwalpizvideo/models';
import { ComposeUrl, endpoints, get } from '@morwalpizvideo/services';
import type { AuditLog } from '@/models/auditLog';
import AuditLogList from '@components/AuditLogList';
import ShareShortLink from './ShareShortLink';

const ShortLinkDetail: React.FC = () => {
  const entity = useLoaderData();
  const [showModal, setShowModal] = useState(false);
  const [showShare, setShowShare] = useState(false);
  const [logs, setLogs] = useState<AuditLog[]>([]);
  const [logsLoading, setLogsLoading] = useState(true);
  const [logsError, setLogsError] = useState('');
  const navigate = useNavigate();
  const toast = useToast();
  const location = useLocation();

  const fetcher = useFetcher();
  const busy = fetcher.state !== 'idle';
  const result =
    fetcher.data != undefined &&
    (fetcher.data.errors == undefined || fetcher.data.errors.length == 0)
      ? fetcher.data
      : null;

  useEffect(() => {
    if (!result) return;

    setShowModal(false);

    if (result.success) {
      toast.show('Success', 'Short link deleted successfully', { variant: 'success' });
      navigate('..');
    }
  }, [result, navigate]);

  useEffect(() => {
    if (!entity?.shortLinkId) return;
    void get(ComposeUrl(endpoints.SHORTLINKS_LOGS, { querylinkId: entity.shortLinkId }))
      .then(value => setLogs(value as AuditLog[]))
      .catch(error => setLogsError(error instanceof Error ? error.message : 'Unable to load logs.'))
      .finally(() => setLogsLoading(false));
  }, [entity]);

  const handleDelete = () => {
    setShowModal(true);
  };

  const confirmDelete = () => {
    const actionPath = location.pathname.substring(0, location.pathname.lastIndexOf('/'));
    console.log(`actionPath: ${actionPath}`);
    fetcher.submit(
      { id: entity.shortLinkId },
      {
        method: 'post',
        action: actionPath,
      }
    );
  };

  if (!entity) {
    return <div>Loading...</div>;
  }

  return (
    <>
      <PageHeader
        title="Short Link Detail"
        editLink={`/shortlinks/${entity.code}/edit`}
        deleteCallback={handleDelete}
      />      <DetailPanel title="Dettagli dell'entità">
        <p>
          <strong>Code:</strong> {entity.code}
        </p>
        {entity.videoTitle && (
          <p>
            <strong>Video Title:</strong> {entity.videoTitle}
          </p>
        )}
        <p>
          <strong>Link Type:</strong> {LinkType[entity.linkType]}
        </p>
        <p>
          <strong>Target:</strong> {entity.target}
        </p>
        <p>
          <strong>Query String:</strong> {entity.queryString}
        </p>
        <p>
          <strong>Created:</strong> {new Date(entity.creationDateTime).toLocaleString()}
        </p>
        <p>
          <strong>Clicks Count:</strong> {entity.clicksCount}
        </p>
      </DetailPanel>

      <Button variant="primary" onClick={() => setShowShare(value => !value)} className="mt-3">
        {showShare ? 'Close Share' : 'Share'}
      </Button>
      {showShare && <ShareShortLink shortLinkId={entity.shortLinkId} />}

      <section className="mt-4">
        <h2 className="h5">History</h2>
        <AuditLogList logs={logs} loading={logsLoading} error={logsError} />
      </section>

      <Modal show={showModal} onHide={() => setShowModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>Confirm Delete</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <p>Are you sure you want to delete the following short link?</p>
          <p>
            <strong>Link Type:</strong> {LinkType[entity.linkType]}
          </p>
          <p>
            <strong>Target:</strong> {entity.target}
          </p>
          <p>
            <strong>Query String:</strong> {entity.queryString}
          </p>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowModal(false)}>
            Cancel
          </Button>
          <Button
            variant="danger"
            disabled={busy}
            onClick={confirmDelete}
            data-testid="delete-modal-confirm"
          >
            Delete
          </Button>
        </Modal.Footer>
      </Modal>
    </>
  );
};

export default ShortLinkDetail;
