import { describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { render } from '../../../test/test-utils';
import Component from '../index/Component';

vi.mock('@/router/asyncData', () => ({
  useResolvedLoaderData: () => [],
}));

describe('Campaigns Index', () => {
  it('renders the Campaigns heading', () => {
    render(<Component />);

    expect(screen.getByRole('heading', { name: 'Campaigns' })).toBeInTheDocument();
  });
});
