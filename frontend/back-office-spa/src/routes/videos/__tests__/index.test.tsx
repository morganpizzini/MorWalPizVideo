import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen } from '@testing-library/react';
import { render } from '../../../test/test-utils';
import { useLoaderData, useNavigate } from 'react-router';
import { Match } from '@morwalpizvideo/models';
import { useAppStore } from '../../../state/appStore';
import { permissions } from '../../../authorization/permissions';

vi.mock('../../../services/authService', () => ({
  authService: {
    getPermissions: vi.fn(() => ['videos.view', 'videos.import', 'videos.translate']),
  },
}));

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
  } as unknown as Match,
  {
    matchId: 'm2',
    title: 'France vs Germany',
    videoId: 'vid2',
  } as unknown as Match,
];

beforeEach(() => {
  vi.clearAllMocks();
  useAppStore.setState({
    effectivePermissions: ['videos.view', 'videos.import', 'videos.translate'],
    featureFlags: { videoBulkImportEnabled: true },
  });
  vi.mocked(useNavigate).mockReturnValue(vi.fn());
  vi.mocked(useLoaderData).mockReturnValue({ matches: mockMatches });
});

async function renderComponent() {
  const { default: Component } = await import('../index/Component');
  return render(<Component />);
}

describe('Videos Index', () => {
  it.each([
    ['manage-all', [permissions.backoffice.manageAll], true, true],
    ['videos.manage', [permissions.videos.manage], true, true],
    ['videos.import', [permissions.videos.import], true, false],
    ['videos.translate', [permissions.videos.translate], false, true],
    ['unrelated permissions', [permissions.users.view, permissions.users.manage], false, false],
  ] as const)(
    'renders the expected actions for %s',
    async (_label, effectivePermissions, showImport, showTranslate) => {
      useAppStore.setState({ effectivePermissions });

      await renderComponent();
      const importLink = screen.queryByRole('link', { name: /import/i });
      const translateLink = screen.queryByRole('link', { name: /translate/i });
      expect(importLink !== null).toBe(showImport);
      expect(translateLink !== null).toBe(showTranslate);
    }
  );

  it('renders compact toolbar actions instead of feature cards', async () => {
    await renderComponent();
    expect(screen.queryByText(/utilizza questa dashboard/i)).not.toBeInTheDocument();
  });

  it('does not render obsolete video management cards', async () => {
    await renderComponent();
    expect(screen.queryByText('Crea Root Video')).not.toBeInTheDocument();
    expect(screen.queryByText('Crea Sub-Video')).not.toBeInTheDocument();
    expect(screen.queryByText('Converti in Root')).not.toBeInTheDocument();
    expect(screen.queryByText('Cambia Thumbnail')).not.toBeInTheDocument();
    expect(screen.queryByText('YouTube Video Links')).not.toBeInTheDocument();
  });
});
