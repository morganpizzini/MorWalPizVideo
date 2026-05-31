import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { CategoryBanner } from '@morwalpiz/layout';

describe('CategoryBanner', () => {
    it('renders the supplied title', () => {
        render(<CategoryBanner title="Latest Videos" />);
        expect(screen.getByText('Latest Videos')).toBeInTheDocument();
    });

    it('applies the artwork URL as a background', () => {
        const url = 'https://example.test/banner.jpg';
        render(<CategoryBanner title="X" artworkUrl={url} />);
        const banner = screen.getByTestId('pb-category-banner');
        expect(banner.getAttribute('style') ?? '').toContain(url);
    });
});
