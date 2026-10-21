import { describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { render } from '../../../test/test-utils';
import Component from '../index/Component';

vi.mock('@/router/asyncData', () => ({
  useResolvedLoaderData: () => [],
}));

describe('Partners Index', () => {
  it('renders the Partners heading', () => {
    render(<Component />);

    expect(screen.getByRole('heading', { name: 'Partners' })).toBeInTheDocument();
  });
});
