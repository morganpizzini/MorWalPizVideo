import React from 'react';
import { Link, useLoaderData } from 'react-router';
import type { QuickLinks } from '@morwalpizvideo/models';
import PageHeader from '@components/PageHeader';
import DetailPanel from '@components/DetailPanel';

const QuickLinksDetail: React.FC = () => {
  const entity = useLoaderData() as QuickLinks;
  return <>
    <PageHeader title="QuickLinks detail" editLink={`/quicklinks/${entity.id}/edit`} />
    <DetailPanel title={entity.title}>
      {entity.subtitle && <p>{entity.subtitle}</p>}
      <p><strong>Public URL:</strong> <Link to={`/quick-links/${entity.url}`} target="_blank" rel="noreferrer">{entity.url}</Link></p>
      <ol>{entity.links.map((link, index) => <li key={`${link.targetUrl}-${index}`}>{link.title || link.label || link.targetUrl} <span className="text-muted">({link.kind})</span></li>)}</ol>
    </DetailPanel>
  </>;
};

export default QuickLinksDetail;
