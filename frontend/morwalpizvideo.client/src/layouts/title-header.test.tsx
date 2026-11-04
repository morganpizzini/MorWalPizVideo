import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router';
import { describe, expect, it } from 'vitest';
import TitleHeader from './title-header';

describe('public header', () => {
  it('renders the logo-only error and compilation header without account providers', () => {
    const html = renderToStaticMarkup(<MemoryRouter><TitleHeader hideLink /></MemoryRouter>);
    expect(html).toContain('MorWalPiz, homepage');
    expect(html).toContain('/images/logo-300.png');
    expect(html).not.toContain('site-navigation');
  });
});