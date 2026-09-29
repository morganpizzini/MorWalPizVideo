import React, { useEffect, useState } from 'react';
import { Button, Form, Modal } from 'react-bootstrap';
import { Upload } from 'lucide-react';

export interface CsvImportModalProps {
  show: boolean;
  title: string;
  subTitle: string;
  legend: string;
  busy?: boolean;
  error?: string;
  onHide: () => void;
  onImport: (file: File) => void;
}

const CsvImportModal: React.FC<CsvImportModalProps> = ({
  show,
  title,
  subTitle,
  legend,
  busy = false,
  error,
  onHide,
  onImport,
}) => {
  const [file, setFile] = useState<File | null>(null);

  useEffect(() => {
    if (!show) setFile(null);
  }, [show]);

  return (
    <Modal show={show} onHide={onHide} centered>
      <Modal.Header closeButton>
        <Modal.Title>{title}</Modal.Title>
      </Modal.Header>
      <Modal.Body>
        <p className="mb-2">{subTitle}</p>
        <Form.Label htmlFor="csv-import-file">CSV file</Form.Label>
        <Form.Control
          id="csv-import-file"
          type="file"
          accept=".csv,text/csv"
          disabled={busy}
          onChange={event => setFile((event.target as HTMLInputElement).files?.[0] ?? null)}
        />
        <Form.Text className="d-block mt-2 text-muted">{legend}</Form.Text>
        {error && <div className="alert alert-danger mt-3 mb-0">{error}</div>}
      </Modal.Body>
      <Modal.Footer>
        <Button variant="secondary" onClick={onHide} disabled={busy}>
          Cancel
        </Button>
        <Button variant="primary" onClick={() => file && onImport(file)} disabled={!file || busy}>
          <Upload size={16} className="me-1" aria-hidden="true" />
          {busy ? 'Importing...' : 'Import'}
        </Button>
      </Modal.Footer>
    </Modal>
  );
};

export default CsvImportModal;
