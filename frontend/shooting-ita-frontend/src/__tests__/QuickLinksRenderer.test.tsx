import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { QuickLinksRenderer } from '@morwalpiz/layout';
import { QuickLinkKind } from '@morwalpizvideo/models';

describe('QuickLinksRenderer', () => {
    it('renders linktree metadata and links for a client theme', () => {
        render(
            <QuickLinksRenderer
                className="shooting-quick-links"
                kicker="Shooting ITA"
                page={{
                    title: 'Shooting ITA links',
                    subtitle: 'Follow the latest coverage',
                    url: 'shooting-ita',
                    links: [{ kind: 0, targetUrl: 'https://example.test', title: 'Coverage' }],
                }}
            />,
        );

        expect(screen.getByText('Shooting ITA')).toBeInTheDocument();
        expect(screen.getByRole('heading', { name: 'Shooting ITA links' })).toBeInTheDocument();
        expect(screen.getByRole('link', { name: /Coverage/ })).toHaveAttribute('href', 'https://example.test');
    });

    it('renders automatic kind icons and preserves explicitly configured media', () => {
        const { container } = render(
            <QuickLinksRenderer
                page={{
                    title: 'Follow',
                    url: 'follow',
                    links: [
                        { kind: QuickLinkKind.External, targetUrl: '/external' },
                        { kind: QuickLinkKind.Telegram, targetUrl: '/telegram', title: 'Telegram' },
                        { kind: QuickLinkKind.Instagram, targetUrl: '/instagram', title: 'Instagram' },
                        { kind: QuickLinkKind.Facebook, targetUrl: '/facebook', title: 'Facebook' },
                        { kind: QuickLinkKind.Video, targetUrl: '/video', title: 'Video' },
                        { kind: QuickLinkKind.YouTube, targetUrl: '/youtube', title: 'YouTube' },
                        {
                            kind: QuickLinkKind.Instagram,
                            targetUrl: '/image',
                            title: 'Image override',
                            imageUrl: 'https://example.test/image.png',
                        },
                        {
                            kind: QuickLinkKind.YouTube,
                            targetUrl: '/custom-icon',
                            title: 'Custom icon override',
                            icon: 'custom',
                        },
                        {
                            kind: QuickLinkKind.YouTube,
                            targetUrl: '/blank-icon',
                            title: 'Blank icon override',
                            icon: '  ',
                        },
                        {
                            kind: QuickLinkKind.Facebook,
                            targetUrl: '/both',
                            title: 'Both overrides',
                            imageUrl: 'https://example.test/image.png',
                            icon: 'custom',
                        },
                    ],
                }}
            />,
        );

        expect(container.querySelector('.lucide-link-2')).toBeInTheDocument();
        expect(container.querySelector('[data-icon="telegram"]')).toBeInTheDocument();
        expect(container.querySelector('[data-icon="instagram"]')).toBeInTheDocument();
        expect(container.querySelector('[data-icon="facebook"]')).toBeInTheDocument();
        expect(container.querySelector('.lucide-play')).toBeInTheDocument();
        expect(container.querySelector('[data-icon="youtube"]')).toBeInTheDocument();
        expect(screen.getByText('External link')).toBeInTheDocument();
        expect(screen.getByRole('link', { name: /YouTube/ })).toHaveAttribute('href', '/youtube');
        expect(container.querySelector('a[href="/image"] .quick-links-icon')).toBeNull();
        expect(container.querySelector('a[href="/custom-icon"] [data-icon="youtube"]')).toBeNull();
        expect(container.querySelector('a[href="/blank-icon"] [data-icon="youtube"]')).toBeInTheDocument();
        expect(container.querySelector('a[href="/both"] .quick-links-image')).toBeInTheDocument();
        expect(container.querySelector('a[href="/both"] .quick-links-icon')).toHaveTextContent('custom');
    });
});