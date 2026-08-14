import type { JSX } from 'react';
import { Helmet } from 'react-helmet-async';
import { useLoaderData } from 'react-router';
import type { QuickLinks as QuickLinksModel } from '@morwalpizvideo/models';
import { QuickLinksRenderer } from '@morwalpiz/layout';
import './style.scss';

export default function QuickLinks(): JSX.Element {
    const page = useLoaderData() as QuickLinksModel;

    return (
        <>
        <Helmet>
            <title>{page.title} | MorWalPiz</title>
            {page.subtitle && <meta name="description" content={page.subtitle} />}
        </Helmet>
        <QuickLinksRenderer page={page} />
        </>
    );
}