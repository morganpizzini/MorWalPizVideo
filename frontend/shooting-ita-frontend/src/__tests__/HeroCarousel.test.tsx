import { render, screen, fireEvent, act } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { HeroCarousel } from '@morwalpiz/layout';

let matchesValue = false;

beforeEach(() => {
    vi.useFakeTimers();
    matchesValue = false;
    Object.defineProperty(window, 'matchMedia', {
        writable: true,
        value: vi.fn().mockImplementation((query: string) => ({
            matches: matchesValue,
            media: query,
            addEventListener: vi.fn(),
            removeEventListener: vi.fn(),
            addListener: vi.fn(),
            removeListener: vi.fn(),
            dispatchEvent: vi.fn(),
        })),
    });
});

afterEach(() => {
    vi.useRealTimers();
});

const slides = [
    { youtubeId: 'a', title: 'A', channelName: 'Ch' },
    { youtubeId: 'b', title: 'B', channelName: 'Ch' },
    { youtubeId: 'c', title: 'C', channelName: 'Ch' },
];

describe('HeroCarousel', () => {
    it('auto-advances when prefers-reduced-motion is no-preference', () => {
        matchesValue = false;
        render(<HeroCarousel slides={slides} intervalMs={1000} />);
        expect(screen.getByText('A')).toBeInTheDocument();
        act(() => { vi.advanceTimersByTime(1000); });
        expect(screen.getByText('B')).toBeInTheDocument();
    });

    it('does NOT auto-advance when prefers-reduced-motion is reduce', () => {
        matchesValue = true;
        render(<HeroCarousel slides={slides} intervalMs={1000} />);
        expect(screen.getByText('A')).toBeInTheDocument();
        act(() => { vi.advanceTimersByTime(5000); });
        expect(screen.getByText('A')).toBeInTheDocument();
    });

    it('manual prev/next always works regardless of reduced motion', () => {
        matchesValue = true;
        render(<HeroCarousel slides={slides} intervalMs={1000} />);
        fireEvent.click(screen.getByTestId('pb-hero-next'));
        expect(screen.getByText('B')).toBeInTheDocument();
        fireEvent.click(screen.getByTestId('pb-hero-prev'));
        expect(screen.getByText('A')).toBeInTheDocument();
    });
});
