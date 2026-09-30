import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { PageSkeleton, TableSkeleton } from './index';

describe('loading skeletons', () => {
  it('renders an accessible page skeleton without visible loading copy', () => {
    render(<PageSkeleton />);

    const status = screen.getByRole('status');
    expect(status).toHaveAttribute('aria-live', 'polite');
    expect(status).toHaveTextContent('Page content is loading');
    expect(status).not.toHaveTextContent('Loading page...');
    expect(status.querySelectorAll('.loading-skeleton__line')).toHaveLength(4);
  });

  it('renders stable table rows and columns', () => {
    render(<TableSkeleton rows={3} columns={5} />);

    expect(screen.getByRole('status')).toHaveTextContent('Table content is loading');
    expect(document.querySelectorAll('.loading-skeleton__table-row')).toHaveLength(4);
    expect(document.querySelector('.loading-skeleton__table-row')?.children).toHaveLength(5);
  });
});
