import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { CategoryVideoRow } from '@morwalpiz/layout';

describe('CategoryVideoRow', () => {
    it('renders title, channel, description, and publish time', () => {
        render(<CategoryVideoRow
            youtubeId="r1"
            title="A row"
            thumbnailUrl="https://example.test/t.jpg"
            duration="3:14"
            channel={{ channelName: 'Channel One' }}
            description="A rich description that should appear in the row."
            publishedAt="3 days ago"
        />);
        expect(screen.getByText('A row')).toBeInTheDocument();
        expect(screen.getByText('Channel One')).toBeInTheDocument();
        expect(screen.getByText(/rich description/)).toBeInTheDocument();
        expect(screen.getByText('3 days ago')).toBeInTheDocument();
    });

    it('falls back gracefully when description is missing', () => {
        render(<CategoryVideoRow
            youtubeId="r2"
            title="A row"
            channel={{ channelName: 'Channel One' }}
        />);
        expect(screen.getByText('A row')).toBeInTheDocument();
        // No throw, no description element.
        expect(screen.queryByText(/description/i)).not.toBeInTheDocument();
    });
});
