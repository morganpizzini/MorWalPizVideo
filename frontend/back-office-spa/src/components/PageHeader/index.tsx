import React from 'react';
import { Link } from 'react-router';
import { Button } from 'react-bootstrap';

interface PageHeaderProps {
  title: string;
  backLink?: string;
  editLink?: string;
  createLink?: string;
  deleteCallback?: () => void;
  actions?: React.ReactNode;
}

const PageHeader: React.FC<PageHeaderProps> = ({
  title,
  backLink,
  editLink,
  createLink,
  deleteCallback,
  actions,
}) => {
  return (
    <div className="d-flex justify-content-between align-items-center bg-light p-3 rounded shadow-sm mb-3">
      <h4 className="mb-0 flex-shrink-0">{title}</h4>
      <div className="d-flex flex-grow-1 justify-content-end align-items-center flex-nowrap overflow-auto ms-3">
        {actions}
        {backLink && (
          <Link className="btn btn-secondary me-2" to={backLink}>
            Indietro
          </Link>
        )}
        {createLink && (
          <Link className="btn btn-success me-2" to={createLink}>
            ➕ Crea
          </Link>
        )}
        {editLink && (
          <Link className="btn btn-primary me-2" to={editLink}>
            ✏️ Modifica
          </Link>
        )}
        {deleteCallback && (
          <Button variant="danger" onClick={deleteCallback}>
            🗑️ Elimina
          </Button>
        )}
      </div>
    </div>
  );
};

export default PageHeader;
