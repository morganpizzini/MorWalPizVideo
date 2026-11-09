import { renderToStaticMarkup } from 'react-dom/server';
import { describe, expect, it, vi } from 'vitest';
import SocialLinks from './SocialLinks';

vi.mock('../routes/layout/socials', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../routes/layout/socials')>();
  return {
    ...actual,
    usePublicSocials: () => [
      { provider: 'facebook', url: 'https://facebook.com/morwalpiz' },
      { provider: 'x', url: 'https://x.com/morwalpiz' },
    ],
  };
});

describe('public social links', () => {
  it('renders Facebook and X as matching social buttons with their brand icons', () => {
    const html = renderToStaticMarkup(<SocialLinks />);

    expect(html).toContain('class="social-link social-link--facebook"');
    expect(html).toContain('class="fab fa-facebook"');
    expect(html).toContain('class="social-link social-link--x"');
    expect(html).toContain('class="fab fa-x-twitter"');
  });
});
