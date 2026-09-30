import { describe, expect, it, vi } from 'vitest';
import { fireEvent, screen } from '@testing-library/react';
import { render } from '../../test/test-utils';
import CsvImportModal from '.';

const defaultProps = {
  show: true,
  title: 'Import products',
  subTitle: 'Choose a CSV source.',
  legend: 'CSV columns',
  onHide: vi.fn(),
  onImport: vi.fn(),
};

describe('CsvImportModal', () => {
  it('keeps file upload as the default import mode', () => {
    render(<CsvImportModal {...defaultProps} />);

    expect(screen.getByLabelText('CSV file')).toBeInTheDocument();
    expect(screen.getByRole('radio', { name: 'Upload file' })).toBeChecked();
    expect(screen.queryByLabelText('CSV text')).not.toBeInTheDocument();
  });

  it('imports pasted CSV text as a file and blocks empty text', () => {
    const onImport = vi.fn();
    render(<CsvImportModal {...defaultProps} onImport={onImport} />);

    fireEvent.click(screen.getByRole('radio', { name: 'Paste text' }));
    const importButton = screen.getByRole('button', { name: 'Import' });
    expect(importButton).toBeDisabled();

    fireEvent.change(screen.getByLabelText('CSV text'), {
      target: { value: '  title,description\nExample,Text  ' },
    });
    expect(importButton).toBeEnabled();
    fireEvent.click(importButton);

    expect(onImport).toHaveBeenCalledTimes(1);
    const importedFile = onImport.mock.calls[0][0] as File;
    expect(importedFile.name).toBe('pasted-import.csv');
    expect(importedFile.type).toBe('text/csv');
    expect(importedFile.size).toBe(new Blob(['  title,description\nExample,Text  ']).size);
  });

  it('resets the mode and text when the modal closes', () => {
    const { rerender } = render(<CsvImportModal {...defaultProps} />);

    fireEvent.click(screen.getByRole('radio', { name: 'Paste text' }));
    fireEvent.change(screen.getByLabelText('CSV text'), { target: { value: 'title' } });
    rerender(<CsvImportModal {...defaultProps} show={false} />);
    rerender(<CsvImportModal {...defaultProps} show />);

    expect(screen.getByRole('radio', { name: 'Upload file' })).toBeChecked();
    expect(screen.getByLabelText('CSV file')).toBeInTheDocument();
  });
});
