import { renderToStaticMarkup } from 'react-dom/server';
import { createMemoryRouter, MemoryRouter, RouterProvider } from 'react-router';
import { afterEach, describe, expect, it, vi } from 'vitest';
import TitleHeader from './title-header';

const accountState = vi.hoisted(() => ({
  session: {
    authenticated: false,
    account: undefined as { displayName: string; username: string } | undefined,
  },
}));

vi.mock('../routes/layout/account', () => ({
  usePublicAccount: () => accountState,
}));

vi.mock('../routes/layout/navigation', () => ({
  usePublicNavigation: () => ({ navigation: { headerItems: [] }, loading: false, error: null }),
}));

vi.mock('../components/SocialLinks', () => ({ default: () => null }));

afterEach(() => {
  accountState.session = { authenticated: false, account: undefined };
});

describe('public header', () => {
  it('renders the logo-only error and compilation header without account providers', () => {
    const html = renderToStaticMarkup(<MemoryRouter><TitleHeader hideLink /></MemoryRouter>);
    expect(html).toContain('MorWalPiz, homepage');
    expect(html).toContain('/images/logo-300.png');
    expect(html).not.toContain('site-navigation');
  });

  it('includes the current page as the login returnUrl', () => {
    const router = createMemoryRouter(
      [{ path: '*', element: <TitleHeader /> }],
      { initialEntries: ['/blog/story?source=home#comments'] }
    );
    const html = renderToStaticMarkup(
      <RouterProvider router={router} />
    );

    expect(html).toContain(
      'href="/account/login?returnUrl=%2Fblog%2Fstory%3Fsource%3Dhome%23comments"'
    );
  });

  it('shows the account display name for an authenticated user', () => {
    accountState.session = {
      authenticated: true,
      account: { displayName: 'Morgan Pizzini', username: 'morgan' },
    };

    const router = createMemoryRouter([{ path: '*', element: <TitleHeader /> }]);
    const html = renderToStaticMarkup(<RouterProvider router={router} />);

    expect(html).toContain('Morgan Pizzini');
    expect(html).toContain('aria-label="Profilo di Morgan Pizzini"');
  });
});