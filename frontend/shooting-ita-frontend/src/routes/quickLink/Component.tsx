import { QuickLinksRenderer } from '@morwalpiz/layout';
import type { QuickLinks } from '@morwalpizvideo/models';
import { useLoaderData } from 'react-router-dom';
import '../../styles/quick-links.scss';

export default function QuickLinkRoute() {
    const page = useLoaderData() as QuickLinks;
    return <QuickLinksRenderer page={page} className="shooting-quick-links" kicker="Shooting ITA" />;
}