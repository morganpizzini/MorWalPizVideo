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

type ImportMode = 'file' | 'text';

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
  const [mode, setMode] = useState<ImportMode>('file');
  const [file, setFile] = useState<File | null>(null);
  const [csvText, setCsvText] = useState('');

  useEffect(() => {
    if (!show) {
      setMode('file');
      setFile(null);
      setCsvText('');
    }
  }, [show]);

  const importFile =
    mode === 'file'
      ? file
      : csvText.trim()
        ? new File([csvText], 'pasted-import.csv', { type: 'text/csv' })
        : null;

  return (
    <Modal show={show} onHide={onHide} centered>
      <Modal.Header closeButton>
        <Modal.Title>{title}</Modal.Title>
      </Modal.Header>
      <Modal.Body>
        <p className="mb-2">{subTitle}</p>
        <fieldset disabled={busy}>
          <legend className="fs-6 mb-2">Import source</legend>
          <div className="d-flex gap-3 mb-3" role="radiogroup" aria-label="CSV import source">
            <Form.Check
              id="csv-import-mode-file"
              type="radio"
              name="csv-import-mode"
              label="Upload file"
              checked={mode === 'file'}
              onChange={() => setMode('file')}
            />
            <Form.Check
              id="csv-import-mode-text"
              type="radio"
              name="csv-import-mode"
              label="Paste text"
              checked={mode === 'text'}
              onChange={() => setMode('text')}
            />
          </div>
          {mode === 'file' ? (
            <>
              <Form.Label htmlFor="csv-import-file">CSV file</Form.Label>
              <Form.Control
                id="csv-import-file"
                type="file"
                accept=".csv,text/csv"
                onChange={event => setFile((event.target as HTMLInputElement).files?.[0] ?? null)}
              />
            </>
          ) : (
            <>
              <Form.Label htmlFor="csv-import-text">CSV text</Form.Label>
              <Form.Control
                id="csv-import-text"
                as="textarea"
                rows={8}
                value={csvText}
                onChange={event => setCsvText(event.target.value)}
              />
            </>
          )}
        </fieldset>
        <Form.Text className="d-block mt-2 text-muted">{legend}</Form.Text>
        {error && <div className="alert alert-danger mt-3 mb-0">{error}</div>}
      </Modal.Body>
      <Modal.Footer>
        <Button variant="secondary" onClick={onHide} disabled={busy}>
          Cancel
        </Button>
        <Button
          variant="primary"
          onClick={() => importFile && onImport(importFile)}
          disabled={!importFile || busy}
        >
          <Upload size={16} className="me-1" aria-hidden="true" />
          {busy ? 'Importing...' : 'Import'}
        </Button>
      </Modal.Footer>
    </Modal>
  );
};

export default CsvImportModal;
