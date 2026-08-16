import React from 'react';
import { Link, useLoaderData } from 'react-router';
import type { PageAdmin } from '@morwalpizvideo/models';
import { PageStatus } from '@morwalpizvideo/models';
import PageHeader from '@components/PageHeader';

export default function PageDetail(): React.ReactElement {
  const page = useLoaderData() as PageAdmin;
  return <>
    <PageHeader title={page.title} backLink="/pages" editLink={`/pages/${page.id}/edit`} />
    <dl className="row"><dt className="col-sm-3">Public URL</dt><dd className="col-sm-9">/pages/{page.url}</dd><dt className="col-sm-3">Status</dt><dd className="col-sm-9">{page.status === PageStatus.Published ? 'Published' : 'Draft'}</dd></dl>
    <div className="border rounded p-3" dangerouslySetInnerHTML={{ __html: page.content }} />
    <Link className="btn btn-link mt-3" to="/pages">Back to pages</Link>
  </>;
}