// @vitest-environment jsdom
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { PublicAccountProvider } from '../layout/account';
import BlogInteractions from './Interactions';

const mocks = vi.hoisted(() => ({
  getPublicAccountSession: vi.fn(),
  getBlogInteractionSummary: vi.fn(),
  getBlogComments: vi.fn(),
  getBlogViewerState: vi.fn(),
}));

vi.mock('@morwalpizvideo/services', () => ({
  getPublicAccountSession: mocks.getPublicAccountSession,
  getBlogInteractionSummary: mocks.getBlogInteractionSummary,
  getBlogComments: mocks.getBlogComments,
  getBlogViewerState: mocks.getBlogViewerState,
}));

describe('blog interactions', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getPublicAccountSession.mockResolvedValue({
      authenticated: true,
      account: {
        id: 'account-1',
        username: 'reader',
        email: 'reader@example.test',
        displayName: 'Reader',
      },
    });
    mocks.getBlogInteractionSummary.mockResolvedValue({
      likes: 2,
      dislikes: 1,
      approvedComments: 1,
    });
    mocks.getBlogComments.mockResolvedValue({
      items: [
        {
          id: 'comment-1',
          displayName: 'Reader',
          body: 'Visible approved comment',
          createdAt: '2026-10-01T00:00:00Z',
        },
      ],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    });
    mocks.getBlogViewerState.mockResolvedValue({
      commentsBlocked: true,
      portalAccessRevoked: false,
      reaction: null,
    });
  });

  it('keeps approved comments readable when the signed-in account is blocked from writing', async () => {
    render(
      <PublicAccountProvider>
        <MemoryRouter>
          <BlogInteractions slug="article" />
        </MemoryRouter>
      </PublicAccountProvider>
    );

    expect(await screen.findByText('Visible approved comment')).not.toBeNull();
    expect(
      await screen.findByText('Non puoi pubblicare commenti su questo portale.')
    ).not.toBeNull();
    expect(screen.queryByLabelText('Il tuo commento')).toBeNull();
  });
});
