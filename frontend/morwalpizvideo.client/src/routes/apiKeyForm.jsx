import { useState, useEffect } from 'react';
import { useLoaderData, useNavigate, Link } from 'react-router';
import { createApiKey, updateApiKey } from '../services/apiKeys';

export default function ApiKeyForm() {
  const { apiKey, isEdit } = useLoaderData();
  const navigate = useNavigate();
  
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    rateLimitPerMinute: '60',
    allowedIpAddresses: '',
    expiresAt: ''
  });
  
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState(null);
  const [generatedKey, setGeneratedKey] = useState(null);
  const [showKeyModal, setShowKeyModal] = useState(false);

  useEffect(() => {
    if (isEdit && apiKey) {
      setFormData({
        name: apiKey.name,
        description: apiKey.description || '',
        rateLimitPerMinute: apiKey.rateLimitPerMinute?.toString() || '60',
        allowedIpAddresses: apiKey.allowedIpAddresses?.join(', ') || '',
        expiresAt: apiKey.expiresAt ? new Date(apiKey.expiresAt).toISOString().slice(0, 16) : ''
      });
    }
  }, [isEdit, apiKey]);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setIsSubmitting(true);
    setError(null);

    try {
      // Prepare data for submission
      const submitData = {
        name: formData.name.trim(),
        description: formData.description.trim() || undefined,
        rateLimitPerMinute: formData.rateLimitPerMinute ? parseInt(formData.rateLimitPerMinute) : undefined,
        allowedIpAddresses: formData.allowedIpAddresses
          ? formData.allowedIpAddresses.split(',').map(ip => ip.trim()).filter(ip => ip)
          : undefined,
        expiresAt: formData.expiresAt ? new Date(formData.expiresAt).toISOString() : undefined
      };

      if (isEdit) {
        // Update existing key
        await updateApiKey(apiKey.id, submitData);
        navigate('/apikeys');
      } else {
        // Create new key
        const result = await createApiKey(submitData);
        setGeneratedKey(result);
        setShowKeyModal(true);
      }
    } catch (err) {
      setError(err.message);
      setIsSubmitting(false);
    }
  };

  const handleCopyKey = () => {
    if (generatedKey?.key) {
      navigator.clipboard.writeText(generatedKey.key);
      // Show a temporary success message
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

  const handleCloseModal = () => {
    setShowKeyModal(false);
    navigate('/apikeys');
  };

  return (
    <div className="container">
      <div className="row justify-content-center">
        <div className="col-md-8">
          <div className="card shadow-sm">
            <div className="card-header bg-primary text-white">
              <h2 className="h4 mb-0">
                <i className="fas fa-key me-2"></i>
                {isEdit ? 'Edit API Key' : 'Create New API Key'}
              </h2>
            </div>
            <div className="card-body">
              {error && (
                <div className="alert alert-danger" role="alert">
                  <i className="fas fa-exclamation-circle me-2"></i>
                  {error}
                </div>
              )}

              <form onSubmit={handleSubmit}>
                <div className="mb-3">
                  <label htmlFor="name" className="form-label">
                    Name <span className="text-danger">*</span>
                  </label>
                  <input
                    type="text"
                    className="form-control"
                    id="name"
                    name="name"
                    value={formData.name}
                    onChange={handleChange}
                    required
                    placeholder="e.g., VideoImporter Desktop App"
                  />
                  <div className="form-text">
                    A descriptive name to identify this API key
                  </div>
                </div>

                <div className="mb-3">
                  <label htmlFor="description" className="form-label">
                    Description
                  </label>
                  <textarea
                    className="form-control"
                    id="description"
                    name="description"
                    value={formData.description}
                    onChange={handleChange}
                    rows="3"
                    placeholder="Optional description of what this key is used for"
                  />
                </div>

                <div className="mb-3">
                  <label htmlFor="rateLimitPerMinute" className="form-label">
                    Rate Limit (requests per minute)
                  </label>
                  <input
                    type="number"
                    className="form-control"
                    id="rateLimitPerMinute"
                    name="rateLimitPerMinute"
                    value={formData.rateLimitPerMinute}
                    onChange={handleChange}
                    min="1"
                    max="1000"
                    placeholder="60"
                  />
                  <div className="form-text">
                    Maximum number of requests allowed per minute (default: 60)
                  </div>
                </div>

                <div className="mb-3">
                  <label htmlFor="allowedIpAddresses" className="form-label">
                    Allowed IP Addresses
                  </label>
                  <input
                    type="text"
                    className="form-control"
                    id="allowedIpAddresses"
                    name="allowedIpAddresses"
                    value={formData.allowedIpAddresses}
                    onChange={handleChange}
                    placeholder="e.g., 192.168.1.1, 10.0.0.1"
                  />
                  <div className="form-text">
                    Optional: Comma-separated list of allowed IP addresses. Leave empty to allow all IPs.
                  </div>
                </div>

                <div className="mb-4">
                  <label htmlFor="expiresAt" className="form-label">
                    Expiration Date
                  </label>
                  <input
                    type="datetime-local"
                    className="form-control"
                    id="expiresAt"
                    name="expiresAt"
                    value={formData.expiresAt}
                    onChange={handleChange}
                  />
                  <div className="form-text">
                    Optional: When this key should expire. Leave empty for no expiration.
                  </div>
                </div>

                <div className="d-flex gap-2">
                  <button
                    type="submit"
                    className="btn btn-primary"
                    disabled={isSubmitting}
                  >
                    {isSubmitting ? (
                      <>
                        <span className="spinner-border spinner-border-sm me-2" role="status"></span>
                        {isEdit ? 'Updating...' : 'Creating...'}
                      </>
                    ) : (
                      <>
                        <i className={`fas ${isEdit ? 'fa-save' : 'fa-plus'} me-2`}></i>
                        {isEdit ? 'Update API Key' : 'Create API Key'}
                      </>
                    )}
                  </button>
                  <Link to="/apikeys" className="btn btn-secondary">
                    <i className="fas fa-times me-2"></i>
                    Cancel
                  </Link>
                </div>
              </form>
            </div>
          </div>
        </div>
      </div>

      {/* One-Time Key Display Modal */}
      {showKeyModal && generatedKey && (
        <div className="modal show d-block" tabIndex="-1" style={{ backgroundColor: 'rgba(0,0,0,0.8)' }}>
          <div className="modal-dialog modal-dialog-centered modal-lg">
            <div className="modal-content">
              <div className="modal-header bg-success text-white">
                <h5 className="modal-title">
                  <i className="fas fa-check-circle me-2"></i>
                  API Key Created Successfully
                </h5>
              </div>
              <div className="modal-body">
                <div className="alert alert-warning mb-4">
                  <h6 className="alert-heading">
                    <i className="fas fa-exclamation-triangle me-2"></i>
                    IMPORTANT: Save This Key Now!
                  </h6>
                  <p className="mb-0">
                    This is the only time you will see this key. Copy it now and store it securely.
                    If you lose this key, you will need to regenerate it.
                  </p>
                </div>

                <div className="mb-3">
                  <label className="form-label fw-bold">API Key Name:</label>
                  <p className="mb-0">{generatedKey.name}</p>
                </div>

                <div className="mb-4">
                  <label className="form-label fw-bold">Your API Key:</label>
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

                <div className="row g-3">
                  <div className="col-md-6">
                    <label className="form-label fw-bold">Rate Limit:</label>
                    <p className="mb-0">{generatedKey.rateLimitPerMinute} requests/minute</p>
                  </div>
                  {generatedKey.expiresAt && (
                    <div className="col-md-6">
                      <label className="form-label fw-bold">Expires:</label>
                      <p className="mb-0">{new Date(generatedKey.expiresAt).toLocaleString('it-IT')}</p>
                    </div>
                  )}
                  {generatedKey.allowedIpAddresses && generatedKey.allowedIpAddresses.length > 0 && (
                    <div className="col-12">
                      <label className="form-label fw-bold">Allowed IPs:</label>
                      <p className="mb-0">{generatedKey.allowedIpAddresses.join(', ')}</p>
                    </div>
                  )}
                </div>
              </div>
              <div className="modal-footer">
                <button
                  type="button"
                  className="btn btn-success"
                  onClick={handleCloseModal}
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