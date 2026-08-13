import type { JSX } from 'react';
import { Helmet } from 'react-helmet-async';
import { useLoaderData } from 'react-router';
import type { QuickLink, QuickLinks as QuickLinksModel } from '@morwalpizvideo/models';
import { QuickLinkKind } from '@morwalpizvideo/models';
import './style.scss';

function kindLabel(kind: QuickLink['kind']): string {
    switch (kind) {
        case QuickLinkKind.Telegram:
            return 'Telegram';
        case QuickLinkKind.Instagram:
            return 'Instagram';
        case QuickLinkKind.Facebook:
            return 'Facebook';
        case QuickLinkKind.Video:
            return 'Video';
        default:
            return 'External link';
    }
}

function linkTitle(link: QuickLink): string {
    return link.title?.trim() || link.label?.trim() || link.provider?.trim() || kindLabel(link.kind);
}

export default function QuickLinks(): JSX.Element {
    const page = useLoaderData() as QuickLinksModel;

    return (
        <main className="quick-links-page">
            <Helmet>
                <title>{page.title} | MorWalPiz</title>
                {page.subtitle && <meta name="description" content={page.subtitle} />}
            </Helmet>
            <section className="quick-links-content" aria-labelledby="quick-links-title">
                <header className="quick-links-header">
                    <p className="quick-links-kicker">Quick links</p>
                    <h1 id="quick-links-title">{page.title}</h1>
                    {page.subtitle && <p className="quick-links-subtitle">{page.subtitle}</p>}
                </header>
                <ol className="quick-links-list">
                    {page.links.map((link, index) => (
                        <li key={`${link.targetUrl}-${index}`}>
                            <a
                                className="quick-links-link"
                                href={link.targetUrl}
                                target="_blank"
                                rel="noopener noreferrer"
                            >
                                {link.imageUrl && <img src={link.imageUrl} alt="" className="quick-links-image" />}
                                {link.icon && <span className="quick-links-icon" aria-hidden="true">{link.icon}</span>}
                                <span className="quick-links-copy">
                                    <span className="quick-links-title">{linkTitle(link)}</span>
                                    {link.subtitle && <span className="quick-links-description">{link.subtitle}</span>}
                                    {link.label && link.label !== link.title && <span className="quick-links-label">{link.label}</span>}
                                    {link.provider && <span className="quick-links-provider">{link.provider} - {kindLabel(link.kind)}</span>}
                                </span>
                                <span className="quick-links-arrow" aria-hidden="true">-&gt;</span>
                            </a>
                        </li>
                    ))}
                </ol>
            </section>
        </main>
    );
}