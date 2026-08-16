import React from 'react';
import { Link, useLoaderData } from 'react-router';
import PageHeader from '@components/PageHeader';
import VideoList from '@components/VideoList';
import { Match } from '@morwalpizvideo/models';
import { Download, Languages } from 'lucide-react';
import { hasPermission, permissions } from '../../../authorization/permissions';
import { useAppStore } from '../../../state/appStore';

const Component: React.FC = () => {
  const { matches, channels } = useLoaderData() as { matches: Match[]; channels: import('@morwalpizvideo/models').Channel[] };
  const effectivePermissions = useAppStore(state => state.effectivePermissions);
  const bulkImportEnabled = useAppStore(state => state.featureFlags.videoBulkImportEnabled);
  const canImport = hasPermission(effectivePermissions, [permissions.videos.import, permissions.videos.manage]);
  const canTranslate = hasPermission(effectivePermissions, [permissions.videos.translate, permissions.videos.manage]);

  return (
    <>
      <div className="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-4">
        <PageHeader title="Videos" />
        <div className="d-flex flex-wrap gap-2" aria-label="Video actions">
          {canTranslate ? (
            <Link to="/videos/translate" className="btn btn-outline-secondary">
              <Languages size={17} aria-hidden="true" /> Translate
            </Link>
          ) : null}
          {canImport && bulkImportEnabled ? (
            <Link to="/videos/import" className="btn btn-primary">
              <Download size={17} aria-hidden="true" /> Import
            </Link>
          ) : null}
        </div>
      </div>

      <VideoList matches={matches} channels={channels} />
    </>
  );
};

export default Component;
