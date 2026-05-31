import { useEffect, useRef, useState } from 'react';
import { prefersReducedMotion, onPrefersReducedMotionChange } from '../utils/prefersReducedMotion.js';

export interface HeroSlide {
    youtubeId: string;
    title: string;
    channelName: string;
    artworkUrl?: string;
    onPlay?: (youtubeId: string) => void;
}

export interface HeroCarouselProps {
    slides: HeroSlide[];
    /** Auto-advance interval in milliseconds. Default 6000. */
    intervalMs?: number;
}

const MAX_SLIDES = 5;

export function HeroCarousel({ slides, intervalMs = 6000 }: HeroCarouselProps) {
    const bounded = slides.slice(0, MAX_SLIDES);
    const [index, setIndex] = useState(0);
    const [reduced, setReduced] = useState(() => prefersReducedMotion());
    const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);

    useEffect(() => onPrefersReducedMotionChange(setReduced), []);

    useEffect(() => {
        if (reduced || bounded.length <= 1) return;
        timerRef.current = setInterval(() => {
            setIndex(i => (i + 1) % bounded.length);
        }, intervalMs);
        return () => {
            if (timerRef.current) clearInterval(timerRef.current);
        };
    }, [reduced, bounded.length, intervalMs]);

    if (bounded.length === 0) {
        return null;
    }

    const current = bounded[index];
    const go = (next: number) => setIndex(((next % bounded.length) + bounded.length) % bounded.length);

    return (
        <section className="pb-hero" data-testid="pb-hero" aria-roledescription="carousel">
            <div
                className="pb-hero__artwork"
                style={current.artworkUrl ? {
                    backgroundImage: `linear-gradient(180deg, rgba(0,0,0,0) 0%, rgba(0,0,0,0.7) 100%), url(${current.artworkUrl})`,
                    backgroundSize: 'cover',
                    backgroundPosition: 'center',
                    position: 'absolute',
                    inset: 0,
                } : { position: 'absolute', inset: 0, background: 'var(--pb-bg-card)' }}
            />
            <div className="pb-hero__body">
                <div className="pb-hero__title">{current.title}</div>
                <div className="pb-hero__channel">{current.channelName}</div>
                <div style={{ marginTop: 16, display: 'flex', gap: 8 }}>
                    <button
                        type="button"
                        className="pb-topbar__btn pb-topbar__btn--primary"
                        onClick={() => current.onPlay?.(current.youtubeId)}
                        data-testid="pb-hero-play"
                    >
                        ▶ Play
                    </button>
                    {bounded.length > 1 ? (
                        <>
                            <button
                                type="button"
                                className="pb-topbar__btn"
                                aria-label="Previous slide"
                                onClick={() => go(index - 1)}
                                data-testid="pb-hero-prev"
                            >‹</button>
                            <button
                                type="button"
                                className="pb-topbar__btn"
                                aria-label="Next slide"
                                onClick={() => go(index + 1)}
                                data-testid="pb-hero-next"
                            >›</button>
                        </>
                    ) : null}
                </div>
            </div>
            {bounded.length > 1 ? (
                <div className="pb-hero__dots" role="tablist">
                    {bounded.map((s, i) => (
                        <button
                            key={s.youtubeId}
                            type="button"
                            className={`pb-hero__dot ${i === index ? 'is-active' : ''}`}
                            aria-label={`Slide ${i + 1}`}
                            aria-selected={i === index}
                            role="tab"
                            onClick={() => go(i)}
                        />
                    ))}
                </div>
            ) : null}
        </section>
    );
}

export default HeroCarousel;
