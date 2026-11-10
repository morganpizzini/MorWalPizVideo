import { useResolvedLoaderData } from '@/router/asyncData';
import React from 'react';
import type { QuickLinks } from '@morwalpizvideo/models';
import PageHeader from '@components/PageHeader';
import DetailPanel from '@components/DetailPanel';
import { useChannelContext } from '@/contexts/ChannelContext';
import { normalizePublicBaseUrl } from '../../channels/publicBaseUrl';

const QuickLinksDetail: React.FC = () => {
  const entity = useResolvedLoaderData() as QuickLinks;
  const { channels, selectedChannelId } = useChannelContext();
  const channel = channels.find(candidate => candidate.channelId === selectedChannelId);
  const origin = normalizePublicBaseUrl(channel?.publicBaseUrl ?? '');
  const unavailable = !channel
    ? 'Select an accessible channel to open the public page.'
    : !entity.channelId
      ? 'Public destination unavailable: this page has no channel owner.'
      : entity.channelId !== channel.channelId
        ? 'Select the owning channel to open this public page.'
        : origin === null
          ? 'The channel public website base URL is invalid. Edit the channel configuration.'
          : !origin
            ? 'Configure the channel public website base URL to open this page.'
            : null;
  const destination = unavailable ? null : `${origin}/quick-links/${encodeURIComponent(entity.url)}`;
  return (
    <>
      <PageHeader title="QuickLinks detail" editLink={`/quicklinks/${entity.id}/edit`} />
      <DetailPanel title={entity.title}>
        {entity.subtitle && <p>{entity.subtitle}</p>}
        <p className="text-break">
          <strong>Public URL:</strong>{' '}
          {destination ? (
            <a href={destination} target="_blank" rel="noopener noreferrer">
              {destination}
            </a>
          ) : (
            <span>{unavailable}</span>
          )}
        </p>
        <ol>
          {entity.links.map((link, index) => (
            <li key={`${link.targetUrl}-${index}`}>
              {link.title || link.label || link.targetUrl}{' '}
              <span className="text-muted">({link.kind})</span>
            </li>
          ))}
        </ol>
      </DetailPanel>
    </>
  );
};

export default QuickLinksDetail;
