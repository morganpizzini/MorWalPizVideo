import React from 'react';
import { Link } from 'react-router';
import { Button } from 'react-bootstrap';

interface PageHeaderProps {
  title: string;
  editLink?: string;
  createLink?: string;
  deleteCallback?: () => void;
}

const PageHeader: React.FC<PageHeaderProps> = ({ title, editLink, createLink, deleteCallback }) => {
  return (
    <div className="d-flex justify-content-between align-items-center bg-light p-3 rounded shadow-sm mb-3">
      <h4 className="mb-0">{title}</h4>
      <div>
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
