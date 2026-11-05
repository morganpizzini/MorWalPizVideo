import { renderToStaticMarkup } from 'react-dom/server';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { describe, expect, it } from 'vitest';
import { routes } from '../config';

describe('public application shell', () => {
  it('renders shared navigation and exactly one main landmark on direct navigation', () => {
    const router = createMemoryRouter(routes, { initialEntries: ['/privacy'] });
    const html = renderToStaticMarkup(<RouterProvider router={router} />);

    expect(html).toContain('href="#main-content"');
    expect(html).toContain('<footer');
    expect(html.match(/<main\b/g)).toHaveLength(1);
    expect(html).toContain('data-bs-theme="dark"');
  });
});
