import React from 'react';
import { describe, expect, it, vi } from 'vitest';
import { render } from '../test/test-utils';
import PrimaryLayout from './PrimaryLayout';

vi.mock('react-router', async () => {
  const actual = await vi.importActual<typeof import('react-router')>('react-router');
  return {
    ...actual,
    Outlet: () => <section>Route content</section>,
  };
});

vi.mock('@components/Breadcrumbs', () => ({
  default: () => <nav aria-label="Breadcrumbs">Breadcrumbs</nav>,
}));

vi.mock('./Header', () => ({
  default: ({ onToggleSidebar }: { onToggleSidebar?: () => void }) => (
    <header>
      <button type="button" onClick={onToggleSidebar}>Toggle sidebar</button>
    </header>
  ),
}));

vi.mock('./AdminSidebar', () => ({
  default: ({ show }: { show?: boolean }) => <aside data-open={show ? 'true' : 'false'}>Sidebar</aside>,
}));

describe('PrimaryLayout', () => {
  it('keeps the protected shell composition and renders the footer after content', () => {
    const { container } = render(<PrimaryLayout />);

    const shell = container.querySelector('.admin-shell');
    const mainColumn = container.querySelector('.admin-main-layout');
    const content = container.querySelector('.admin-content');
    const footer = container.querySelector('.admin-footer');

    expect(shell).not.toBeNull();
    expect(mainColumn).not.toBeNull();
    expect(content).not.toBeNull();
    expect(footer).not.toBeNull();
    expect(mainColumn?.lastElementChild).toBe(footer);
    expect(content?.textContent).toContain('Breadcrumbs');
    expect(content?.textContent).toContain('Route content');
    expect(footer).not.toHaveClass('mt-5');
  });
});