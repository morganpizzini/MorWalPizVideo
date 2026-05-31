import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { EmptyState } from '@morwalpiz/layout';

describe('EmptyState', () => {
    it('renders title and message', () => {
        render(<EmptyState title="Nothing here" message="Check back later." />);
        expect(screen.getByText('Nothing here')).toBeInTheDocument();
        expect(screen.getByText('Check back later.')).toBeInTheDocument();
    });

    it('renders without a message', () => {
        render(<EmptyState title="Empty" />);
        expect(screen.getByText('Empty')).toBeInTheDocument();
    });
});
