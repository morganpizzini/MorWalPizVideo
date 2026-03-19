import { useState } from 'react';
import { useLoaderData, Link, useNavigate } from 'react-router';
import { toggleApiKey, deleteApiKey, regenerateApiKey } from '../services/apiKeys';

export default function ApiKeyDetail() {
  const { apiKey: initialApiKey } = useLoaderData();
  const [apiKey, setApiKey] = useState(initialApiKey);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState(null);
  const [successMessage, setSuccessMessage] = useState(null);
  const [deleteConfirm, setDeleteConfirm] = useState(false);
  const [regenerateConfirm, setRegenerateConfirm] = useState(false);
  const [generatedKey, setGeneratedKey] = useState(null);
  const [showKeyModal, setShowKeyModal] = useState(false);
  const navigate = useNavigate();

  const formatDate = (dateString) => {
    if (!dateString) return 'Never';
    const date = new Date(dateString);
    return date.toLocaleString('it-IT', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const getStatusBadge = () => {
    if (!apiKey.isActive) {
      return <span className="badge bg-secondary fs-6">Inactive</span>;
    }
    if (apiKey.expiresAt && new Date(apiKey.expiresAt) < new Date()) {
      return <span className="badge bg-danger fs-6">Expired</span>;
    }
    return <span className="badge bg-success fs-6">Active</span>;
  };

  const handleToggle = async () => {
    setIsLoading(true);
    setError(null);
    setSuccessMessage(null);

    try {
      const result = await toggleApiKey(apiKey.id);
      setApiKey(prev => ({ ...prev, isActive: result.isActive }));
      setSuccessMessage(result.message);
      setTimeout(() => setSuccessMessage(null), 3000);
    } catch (err) {
      setError(err.message);
    } finally {
      setIsLoading(false);
    }
  };

  const handleDeleteConfirm = async () => {
    setIsLoading(true);
    setError(null);

    try {
      await deleteApiKey(apiKey.id);
      navigate('/apikeys');
    } catch (err) {
      setError(err.message);
      setDeleteConfirm(false);
      setIsLoading(false);
    }
  };

  const handleRegenerateConfirm = async () => {
    setIsLoading(true);
    setError(null);
    setRegenerateConfirm(false);

    try {
      const result = await regenerateApiKey(apiKey.id);
      setApiKey(prev => ({ ...prev, lastUsedAt: null }));
      setGeneratedKey(result);
      setShowKeyModal(true);
    } catch (err) {
      setError(err.message);
    } finally {
      setIsLoading(false);
    }
  };

  const handleCopyKey = () => {
    if (generatedKey?.key) {
      navigator.clipboard.writeText(generatedKey.key);
      const btn = document.getElementById('copyKeyBtn');
      const originalText = btn.innerHTML;
      btn.innerHTML = '<i class="fas fa-check me-2"></i>Copied!';
      btn.classList.remove('btn-primary');
      btn.classList.add('btn-success');
      setTimeout(() => {
        btn.innerHTML = originalText;
        btn.classList.remove('btn-success');
        btn.classList.add('btn-primary');
      }, 2000);
    }
  };

  return (
    <div className="container">
      <div className="row mb-4">
        <div className="col">
          <Link to="/apikeys" className="btn btn-outline-secondary mb-3">
            <i className="fas fa-arrow-left me-2"></i>
            Back to API Keys
          </Link>
          <h1 className="h2">API Key Details</h1>
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

      <div className="row">
        <div className="col-lg-8">
          <div className="card shadow-sm mb-4">
            <div className="card-header bg-light d-flex justify-content-between align-items-center">
              <h5 className="mb-0">
                <i className="fas fa-key me-2"></i>
                {apiKey.name}
              </h5>
              {getStatusBadge()}
            </div>
            <div className="card-body">
              <div className="row g-3">
                <div className="col-md-6">
                  <label className="text-muted small">Name</label>
                  <p className="mb-0 fw-bold">{apiKey.name}</p>
                </div>

                <div className="col-md-6">
                  <label className="text-muted small">Status</label>
                  <p className="mb-0">{getStatusBadge()}</p>
                </div>

                {apiKey.description && (
                  <div className="col-12">
                    <label className="text-muted small">Description</label>
                    <p className="mb-0">{apiKey.description}</p>
                  </div>
                )}

                <div className="col-md-6">
                  <label className="text-muted small">Rate Limit</label>
                  <p className="mb-0">
                    <span className="badge bg-info fs-6">
                      {apiKey.rateLimitPerMinute} requests/minute
                    </span>
                  </p>
                </div>

                <div className="col-md-6">
                  <label className="text-muted small">Last Used</label>
                  <p className="mb-0">{formatDate(apiKey.lastUsedAt)}</p>
                </div>

                <div className="col-md-6">
                  <label className="text-muted small">Created</label>
                  <p className="mb-0">{formatDate(apiKey.createdAt)}</p>
                </div>

                <div className="col-md-6">
                  <label className="text-muted small">Expires</label>
                  <p className="mb-0">{apiKey.expiresAt ? formatDate(apiKey.expiresAt) : 'Never'}</p>
                </div>

                {apiKey.allowedIpAddresses && apiKey.allowedIpAddresses.length > 0 && (
                  <div className="col-12">
                    <label className="text-muted small">Allowed IP Addresses</label>
                    <div className="d-flex flex-wrap gap-2">
                      {apiKey.allowedIpAddresses.map((ip, index) => (
                        <span key={index} className="badge bg-secondary fs-6">
                          {ip}
                        </span>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>

        <div className="col-lg-4">
          <div className="card shadow-sm">
            <div className="card-header bg-light">
              <h5 className="mb-0">Actions</h5>
            </div>
            <div className="card-body d-grid gap-2">
              <Link 
                to={`/apikeys/${apiKey.id}/edit`} 
                className="btn btn-primary"
              >
                <i className="fas fa-edit me-2"></i>
                Edit API Key
              </Link>

              <button
                onClick={handleToggle}
                className={`btn ${apiKey.isActive ? 'btn-warning' : 'btn-success'}`}
                disabled={isLoading}
              >
                <i className={`fas ${apiKey.isActive ? 'fa-pause' : 'fa-play'} me-2`}></i>
                {apiKey.isActive ? 'Deactivate' : 'Activate'}
              </button>

              <button
                onClick={() => setRegenerateConfirm(true)}
                className="btn btn-info"
                disabled={isLoading}
              >
                <i className="fas fa-sync me-2"></i>
                Regenerate Key
              </button>

              <button
                onClick={() => setDeleteConfirm(true)}
                className="btn btn-danger"
                disabled={isLoading}
              >
                <i className="fas fa-trash me-2"></i>
                Delete API Key
              </button>
            </div>
          </div>

          <div className="card shadow-sm mt-3">
            <div className="card-body">
              <h6 className="card-title">
                <i className="fas fa-info-circle me-2"></i>
                About API Keys
              </h6>
              <p className="card-text small text-muted">
                API keys are used to authenticate external applications. Keep your keys secure and never share them publicly.
              </p>
              <p className="card-text small text-muted mb-0">
                If you suspect a key has been compromised, regenerate or delete it immediately.
              </p>
            </div>
          </div>
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
                  onClick={() => setDeleteConfirm(false)}
                ></button>
              </div>
              <div className="modal-body">
                <p>Are you sure you want to delete the API key <strong>{apiKey.name}</strong>?</p>
                <p className="text-danger mb-0">
                  <i className="fas fa-exclamation-circle me-2"></i>
                  This action cannot be undone. Applications using this key will lose access immediately.
                </p>
              </div>
              <div className="modal-footer">
                <button 
                  type="button" 
                  className="btn btn-secondary" 
                  onClick={() => setDeleteConfirm(false)}
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

      {/* Regenerate Confirmation Modal */}
      {regenerateConfirm && (
        <div className="modal show d-block" tabIndex="-1" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content">
              <div className="modal-header bg-warning">
                <h5 className="modal-title">
                  <i className="fas fa-exclamation-triangle me-2"></i>
                  Confirm Regenerate
                </h5>
                <button 
                  type="button" 
                  className="btn-close" 
                  onClick={() => setRegenerateConfirm(false)}
                ></button>
              </div>
              <div className="modal-body">
                <p>Are you sure you want to regenerate the API key <strong>{apiKey.name}</strong>?</p>
                <p className="text-warning mb-0">
                  <i className="fas fa-exclamation-circle me-2"></i>
                  The old key will stop working immediately. You'll need to update all applications using this key.
                </p>
              </div>
              <div className="modal-footer">
                <button 
                  type="button" 
                  className="btn btn-secondary" 
                  onClick={() => setRegenerateConfirm(false)}
                  disabled={isLoading}
                >
                  Cancel
                </button>
                <button 
                  type="button" 
                  className="btn btn-warning" 
                  onClick={handleRegenerateConfirm}
                  disabled={isLoading}
                >
                  {isLoading ? (
                    <>
                      <span className="spinner-border spinner-border-sm me-2" role="status"></span>
                      Regenerating...
                    </>
                  ) : (
                    <>
                      <i className="fas fa-sync me-2"></i>
                      Regenerate Key
                    </>
                  )}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* New Key Display Modal */}
      {showKeyModal && generatedKey && (
        <div className="modal show d-block" tabIndex="-1" style={{ backgroundColor: 'rgba(0,0,0,0.8)' }}>
          <div className="modal-dialog modal-dialog-centered modal-lg">
            <div className="modal-content">
              <div className="modal-header bg-success text-white">
                <h5 className="modal-title">
                  <i className="fas fa-check-circle me-2"></i>
                  API Key Regenerated Successfully
                </h5>
              </div>
              <div className="modal-body">
                <div className="alert alert-warning mb-4">
                  <h6 className="alert-heading">
                    <i className="fas fa-exclamation-triangle me-2"></i>
                    IMPORTANT: Save This Key Now!
                  </h6>
                  <p className="mb-0">
                    This is the only time you will see this new key. Copy it now and store it securely.
                    The old key has been invalidated and will no longer work.
                  </p>
                </div>

                <div className="mb-4">
                  <label className="form-label fw-bold">Your New API Key:</label>
                  <div className="input-group">
                    <input
                      type="text"
                      className="form-control font-monospace"
                      value={generatedKey.key}
                      readOnly
                      onClick={(e) => e.target.select()}
                      style={{ fontSize: '0.9rem' }}
                    />
                    <button
                      id="copyKeyBtn"
                      className="btn btn-primary"
                      type="button"
                      onClick={handleCopyKey}
                    >
                      <i className="fas fa-copy me-2"></i>
                      Copy
                    </button>
                  </div>
                </div>
              </div>
              <div className="modal-footer">
                <button
                  type="button"
                  className="btn btn-success"
                  onClick={() => setShowKeyModal(false)}
                >
                  <i className="fas fa-check me-2"></i>
                  I've Saved the Key
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}