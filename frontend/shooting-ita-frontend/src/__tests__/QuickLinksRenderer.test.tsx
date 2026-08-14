import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { QuickLinksRenderer } from '@morwalpiz/layout';

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
});