import { useState } from 'react';
import { useLoaderData, Link, useNavigate } from 'react-router';
import { toggleApiKey, deleteApiKey } from '../services/apiKeys';

export default function ApiKeys() {
  const { apiKeys: initialApiKeys } = useLoaderData();
  const [apiKeys, setApiKeys] = useState(initialApiKeys);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState(null);
  const [successMessage, setSuccessMessage] = useState(null);
  const [deleteConfirm, setDeleteConfirm] = useState(null);
  const navigate = useNavigate();

  const formatDate = (dateString) => {
    if (!dateString) return 'Never';
    const date = new Date(dateString);
    return date.toLocaleDateString('it-IT', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const getStatusBadge = (apiKey) => {
    if (!apiKey.isActive) {
      return <span className="badge bg-secondary">Inactive</span>;
    }
    if (apiKey.expiresAt && new Date(apiKey.expiresAt) < new Date()) {
      return <span className="badge bg-danger">Expired</span>;
    }
    return <span className="badge bg-success">Active</span>;
  };

  const handleToggle = async (id) => {
    setIsLoading(true);
    setError(null);
    setSuccessMessage(null);

    try {
      const result = await toggleApiKey(id);
      
      setApiKeys(prev => prev.map(key => 
        key.id === id ? { ...key, isActive: result.isActive } : key
      ));
      
      setSuccessMessage(result.message);
      setTimeout(() => setSuccessMessage(null), 3000);
    } catch (err) {
      setError(err.message);
    } finally {
      setIsLoading(false);
    }
  };

  const handleDeleteClick = (apiKey) => {
    setDeleteConfirm(apiKey);
  };

  const handleDeleteConfirm = async () => {
    if (!deleteConfirm) return;

    setIsLoading(true);
    setError(null);
    setSuccessMessage(null);

    try {
      const result = await deleteApiKey(deleteConfirm.id);
      
      setApiKeys(prev => prev.filter(key => key.id !== deleteConfirm.id));
      setSuccessMessage(result.message);
      setDeleteConfirm(null);
      setTimeout(() => setSuccessMessage(null), 3000);
    } catch (err) {
      setError(err.message);
      setDeleteConfirm(null);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="container">
      <div className="row mb-4">
        <div className="col">
          <h1 className="h2">API Keys Management</h1>
          <p className="text-muted">Manage authentication keys for external applications</p>
        </div>
        <div className="col-auto">
          <Link to="/apikeys/create" className="btn btn-primary">
            <i className="fas fa-plus me-2"></i>
            Create New API Key
          </Link>
        </div>
      </div>

      {error && (
        <div className="alert alert-danger alert-dismissible fade show" role="alert">
          <i className="fas fa-exclamation-circle me-2"></i>
          {error}
          <button type="button" className="btn-close" onClick={() => setError(null)}></button>
        </div>
      )}

      {successMessage && (
        <div className="alert alert-success alert-dismissible fade show" role="alert">
          <i className="fas fa-check-circle me-2"></i>
          {successMessage}
          <button type="button" className="btn-close" onClick={() => setSuccessMessage(null)}></button>
        </div>
      )}

      <div className="card shadow-sm">
        <div className="card-body">
          {apiKeys.length === 0 ? (
            <div className="text-center py-5">
              <i className="fas fa-key fa-3x text-muted mb-3"></i>
              <p className="text-muted">No API keys found. Create your first API key to get started.</p>
              <Link to="/apikeys/create" className="btn btn-primary">
                Create API Key
              </Link>
            </div>
          ) : (
            <div className="table-responsive">
              <table className="table table-hover">
                <thead>
                  <tr>
                    <th>Name</th>
                    <th>Description</th>
                    <th>Status</th>
                    <th>Rate Limit</th>
                    <th>Last Used</th>
                    <th>Expires</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {apiKeys.map(apiKey => (
                    <tr key={apiKey.id}>
                      <td>
                        <Link to={`/apikeys/${apiKey.id}`} className="text-decoration-none fw-bold">
                          {apiKey.name}
                        </Link>
                      </td>
                      <td>
                        <small className="text-muted">
                          {apiKey.description || '-'}
                        </small>
                      </td>
                      <td>{getStatusBadge(apiKey)}</td>
                      <td>
                        <span className="badge bg-info">
                          {apiKey.rateLimitPerMinute} req/min
                        </span>
                      </td>
                      <td>
                        <small>{formatDate(apiKey.lastUsedAt)}</small>
                      </td>
                      <td>
                        <small>{apiKey.expiresAt ? formatDate(apiKey.expiresAt) : 'Never'}</small>
                      </td>
                      <td>
                        <div className="btn-group btn-group-sm" role="group">
                          <Link 
                            to={`/apikeys/${apiKey.id}`} 
                            className="btn btn-outline-primary"
                            title="View Details"
                          >
                            <i className="fas fa-eye"></i>
                          </Link>
                          <Link 
                            to={`/apikeys/${apiKey.id}/edit`} 
                            className="btn btn-outline-secondary"
                            title="Edit"
                          >
                            <i className="fas fa-edit"></i>
                          </Link>
                          <button
                            onClick={() => handleToggle(apiKey.id)}
                            className={`btn ${apiKey.isActive ? 'btn-outline-warning' : 'btn-outline-success'}`}
                            disabled={isLoading}
                            title={apiKey.isActive ? 'Deactivate' : 'Activate'}
                          >
                            <i className={`fas ${apiKey.isActive ? 'fa-pause' : 'fa-play'}`}></i>
                          </button>
                          <button
                            onClick={() => handleDeleteClick(apiKey)}
                            className="btn btn-outline-danger"
                            disabled={isLoading}
                            title="Delete"
                          >
                            <i className="fas fa-trash"></i>
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>

      {/* Delete Confirmation Modal */}
      {deleteConfirm && (
        <div className="modal show d-block" tabIndex="-1" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content">
              <div className="modal-header bg-danger text-white">
                <h5 className="modal-title">
                  <i className="fas fa-exclamation-triangle me-2"></i>
                  Confirm Delete
                </h5>
                <button 
                  type="button" 
                  className="btn-close btn-close-white" 
                  onClick={() => setDeleteConfirm(null)}
                ></button>
              </div>
              <div className="modal-body">
                <p>Are you sure you want to delete the API key <strong>{deleteConfirm.name}</strong>?</p>
                <p className="text-danger mb-0">
                  <i className="fas fa-exclamation-circle me-2"></i>
                  This action cannot be undone. Applications using this key will lose access.
                </p>
              </div>
              <div className="modal-footer">
                <button 
                  type="button" 
                  className="btn btn-secondary" 
                  onClick={() => setDeleteConfirm(null)}
                  disabled={isLoading}
                >
                  Cancel
                </button>
                <button 
                  type="button" 
                  className="btn btn-danger" 
                  onClick={handleDeleteConfirm}
                  disabled={isLoading}
                >
                  {isLoading ? (
                    <>
                      <span className="spinner-border spinner-border-sm me-2" role="status"></span>
                      Deleting...
                    </>
                  ) : (
                    <>
                      <i className="fas fa-trash me-2"></i>
                      Delete API Key
                    </>
                  )}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}