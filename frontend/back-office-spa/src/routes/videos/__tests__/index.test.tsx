import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen } from '@testing-library/react';
import { render } from '../../../test/test-utils';
import { useLoaderData, useNavigate } from 'react-router';
import { Match } from '@morwalpizvideo/models';

vi.mock('react-router', async () => {
  const actual = await vi.importActual<typeof import('react-router')>('react-router');
  return {
    ...actual,
    useLoaderData: vi.fn(),
    useNavigate: vi.fn(),
    useRevalidator: vi.fn().mockReturnValue({ revalidate: vi.fn(), state: 'idle' }),
  };
});

const mockMatches: Match[] = [
  {
    matchId: 'm1',
    title: 'Italy vs Spain',
    videoId: 'vid1',
  } as Match,
  {
    matchId: 'm2',
    title: 'France vs Germany',
    videoId: 'vid2',
  } as Match,
];

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(useNavigate).mockReturnValue(vi.fn());
  vi.mocked(useLoaderData).mockReturnValue({ matches: mockMatches });
});

async function renderComponent() {
  const { default: Component } = await import('../index/Component');
  return render(<Component />);
}

describe('Videos Index', () => {
  it('renders the feature cards for video operations', async () => {
    await renderComponent();
    expect(screen.getByText('Importa Video')).toBeInTheDocument();
    expect(screen.getByText('Traduci Video')).toBeInTheDocument();
    expect(screen.getByText('Crea Root Video')).toBeInTheDocument();
  });

  it('renders the Crea Sub-Video card', async () => {
    await renderComponent();
    expect(screen.getByText('Crea Sub-Video')).toBeInTheDocument();
  });

  it('renders the Converti in Root card', async () => {
    await renderComponent();
    expect(screen.getByText('Converti in Root')).toBeInTheDocument();
  });

  it('renders the Cambia Thumbnail card', async () => {
    await renderComponent();
    expect(screen.getByText('Cambia Thumbnail')).toBeInTheDocument();
  });

  it('renders the YouTube Video Links card', async () => {
    await renderComponent();
    expect(screen.getByText('YouTube Video Links')).toBeInTheDocument();
  });
});
