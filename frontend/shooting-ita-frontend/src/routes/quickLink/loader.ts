import type { LoaderFunction } from 'react-router-dom';
import { getShootingItaQuickLinks } from '../../services/quickLinks';

export const quickLinkLoader: LoaderFunction = async ({ params }) => {
    const slug = params['custom-linktree'];
    if (!slug) throw new Response('QuickLinks not found', { status: 404 });
    return getShootingItaQuickLinks(slug);
};