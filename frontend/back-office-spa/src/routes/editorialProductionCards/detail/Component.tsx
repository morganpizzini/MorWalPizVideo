import React from 'react';
import { Link } from 'react-router';
import { useResolvedLoaderData } from '@/router/asyncData';
import type { EditorialProductionCard } from '@morwalpizvideo/models';
import PageHeader from '@components/PageHeader';

const statuses = ['Idea', 'Planned', 'In progress', 'Ready', 'Published', 'Archived'];

export default function EditorialProductionCardDetail(): React.ReactElement {
  const card = useResolvedLoaderData() as EditorialProductionCard;
  return (
    <>
      <PageHeader title={card.title} />
      <div className="d-flex gap-2 mb-3">
        <Link className="btn btn-primary" to="edit">
          Edit
        </Link>
        <Link className="btn btn-outline-secondary" to="/editorialproductioncards">
          Back to cards
        </Link>
      </div>
      <dl className="row">
        <dt className="col-sm-3">Status</dt>
        <dd className="col-sm-9">{statuses[Number(card.status)] ?? 'Unknown'}</dd>
        <dt className="col-sm-3">Editorial angle</dt>
        <dd className="col-sm-9">{card.editorialAngle || 'Not set'}</dd>
        <dt className="col-sm-3">Owner</dt>
        <dd className="col-sm-9">{card.ownerUserId || 'Not assigned'}</dd>
        <dt className="col-sm-3">Due</dt>
        <dd className="col-sm-9">
          {card.dueDateUtc ? new Date(card.dueDateUtc).toLocaleString() : 'Not set'}
        </dd>
        <dt className="col-sm-3">Sources</dt>
        <dd className="col-sm-9">
          {card.sourceUrls.length ? (
            <ul>
              {card.sourceUrls.map(url => (
                <li key={url}>
                  <a href={url} target="_blank" rel="noreferrer">
                    {url}
                  </a>
                </li>
              ))}
            </ul>
          ) : (
            'None'
          )}
        </dd>
      </dl>
      <section className="mb-4">
        <h2 className="h5">Brief</h2>
        <p className="white-space-pre-wrap">{card.brief || 'No brief yet.'}</p>
      </section>
      <section>
        <h2 className="h5">Script</h2>
        <p className="white-space-pre-wrap">{card.script || 'No script yet.'}</p>
      </section>
    </>
  );
}
