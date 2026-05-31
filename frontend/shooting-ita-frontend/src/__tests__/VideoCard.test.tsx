import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { VideoCard } from '@morwalpiz/layout';

describe('VideoCard', () => {
    it('renders full metadata', () => {
        render(<VideoCard
            youtubeId="yt1"
            title="Full title"
            thumbnailUrl="https://example.test/t.jpg"
            duration="5:42"
            channel={{ channelName: 'Acme', avatarUrl: 'https://example.test/a.jpg' }}
            publishedAt="2 hours ago"
        />);
        expect(screen.getByText('Full title')).toBeInTheDocument();
        expect(screen.getByText('Acme')).toBeInTheDocument();
        expect(screen.getByTestId('pb-card-duration')).toHaveTextContent('5:42');
        expect(screen.getByTestId('pb-card-published')).toHaveTextContent('2 hours ago');
    });

    it('falls back to placeholder thumbnail when missing', () => {
        render(<VideoCard youtubeId="yt2" title="t" channel={{ channelName: 'Acme' }} />);
        expect(screen.getByTestId('pb-card-thumb-placeholder')).toBeInTheDocument();
    });

    it('hides duration and publishedAt when missing', () => {
        render(<VideoCard youtubeId="yt3" title="t" channel={{ channelName: 'Acme' }} />);
        expect(screen.queryByTestId('pb-card-duration')).not.toBeInTheDocument();
        expect(screen.queryByTestId('pb-card-published')).not.toBeInTheDocument();
    });

    it('renders channel initials when avatarUrl is missing', () => {
        render(<VideoCard youtubeId="yt4" title="t" channel={{ channelName: 'John Doe' }} />);
        expect(screen.getByTestId('pb-card-avatar-initials')).toHaveTextContent('JD');
    });
});
