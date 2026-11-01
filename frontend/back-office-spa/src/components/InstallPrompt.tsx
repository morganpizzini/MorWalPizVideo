import React from 'react';
import { Alert, Button } from 'react-bootstrap';
import { Download, X } from 'lucide-react';
import { useInstallPrompt } from '../services/installPrompt';

const InstallPrompt: React.FC = () => {
  const { canInstall, dismissed, installed, dismiss, install } = useInstallPrompt();

  if (!canInstall || dismissed || installed) {
    return null;
  }

  const handleInstall = async () => {
    await install();
  };

  return (
    <Alert
      variant="info"
      className="install-prompt d-flex flex-column flex-md-row align-items-md-center gap-3 mb-4"
    >
      <Download size={22} aria-hidden="true" className="flex-shrink-0" />
      <div className="flex-grow-1">
        <Alert.Heading as="h2" className="h6 mb-1">
          Install MorWalPiz Admin
        </Alert.Heading>
        <p className="mb-0">
          Open the BackOffice from your device like an app, while keeping your current sign-in flow.
        </p>
      </div>
      <div className="d-flex flex-wrap gap-2">
        <Button variant="primary" onClick={handleInstall}>
          Install
        </Button>
        <Button
          variant="outline-secondary"
          onClick={dismiss}
          aria-label="Dismiss install invitation"
        >
          <X size={16} aria-hidden="true" />
          <span className="ms-1">Not now</span>
        </Button>
      </div>
    </Alert>
  );
};

export default InstallPrompt;
