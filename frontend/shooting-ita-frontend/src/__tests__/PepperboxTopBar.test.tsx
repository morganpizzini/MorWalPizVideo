import { render, screen, fireEvent, act } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { PepperboxTopBar } from '@morwalpiz/layout';

beforeEach(() => { vi.useFakeTimers(); });
afterEach(() => { vi.useRealTimers(); });

describe('PepperboxTopBar', () => {
    it('shows a "coming soon" notice when Log in is clicked and does not navigate', () => {
        const navigateSpy = vi.fn();
        // capture window.location calls; clicks here are pure buttons with no href
        render(<PepperboxTopBar />);
        fireEvent.click(screen.getByTestId('pb-topbar-login'));
        expect(screen.getByTestId('pb-topbar-notice')).toHaveTextContent(/log in.*coming soon/i);
        expect(navigateSpy).not.toHaveBeenCalled();
    });

    it('shows a "coming soon" notice when Sign up is clicked', () => {
        render(<PepperboxTopBar />);
        fireEvent.click(screen.getByTestId('pb-topbar-signup'));
        expect(screen.getByTestId('pb-topbar-notice')).toHaveTextContent(/sign up.*coming soon/i);
    });

    it('clears the notice after the timeout', () => {
        render(<PepperboxTopBar />);
        fireEvent.click(screen.getByTestId('pb-topbar-login'));
        expect(screen.getByTestId('pb-topbar-notice')).toBeInTheDocument();
        act(() => { vi.advanceTimersByTime(3500); });
        expect(screen.queryByTestId('pb-topbar-notice')).not.toBeInTheDocument();
    });
});
