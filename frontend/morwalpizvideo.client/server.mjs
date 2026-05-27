// Minimal browser globals shim — must be set BEFORE the SSR bundle is imported,
// since some packages (react-ga4, etc.) reference window at module level.
globalThis.window = globalThis.window ?? {
    location: { href: '', pathname: '/', hostname: 'localhost', protocol: 'https:', host: 'localhost' },
    ENV: {},
    confirm: () => false,
    dispatchEvent: () => false,
    addEventListener: () => {},
    removeEventListener: () => {},
};
globalThis.document = globalThis.document ?? {
    getElementById: () => null,
    createElement: () => ({ style: {}, setAttribute: () => {}, appendChild: () => {} }),
    createEvent: () => ({ initEvent: () => {} }),
    head: { appendChild: () => {} },
    body: { appendChild: () => {} },
};

import express from 'express';
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';

const { render } = await import('./dist-ssr/entry-server.js');

const app = express();
const PORT = process.env.PORT ?? 3000;
const distDir = resolve('dist');

// Inject runtime env vars into env-config.js (replaces docker-entrypoint.sh)
const envConfig = `window.ENV = {
  VITE_API_BASE_URL: '${process.env.VITE_API_BASE_URL ?? ''}',
  API_BASE_URL: '${process.env.API_BASE_URL ?? ''}',
  REACT_APP_API_URL: '${process.env.REACT_APP_API_URL ?? ''}'
};`;
writeFileSync(resolve(distDir, 'env-config.js'), envConfig);

const template = readFileSync(resolve(distDir, 'index.html'), 'utf-8');

// Static assets — long cache for content-hashed files
app.use(
    '/assets',
    express.static(resolve(distDir, 'assets'), { maxAge: '1y', immutable: true })
);

// Other static files (favicon, manifest, env-config.js, public/, etc.)
app.use(express.static(distDir, { index: false }));

// SSR for all HTML requests
app.get('*', async (req, res) => {
    try {
        const proto = req.headers['x-forwarded-proto'] ?? req.protocol ?? 'https';
        const host = req.headers['x-forwarded-host'] ?? req.headers.host ?? 'localhost';
        const fullUrl = `${proto}://${host}${req.originalUrl}`;

        const { html, head } = await render(new Request(fullUrl));

        const page = template
            .replace('<!--ssr-head-->', head)
            .replace('<div id="root"></div>', `<div id="root">${html}</div>`);

        res.status(200).set('Content-Type', 'text/html').end(page);
    } catch (e) {
        // Redirect responses emitted by React Router loaders/actions
        if (e instanceof Response) {
            const location = e.headers.get('Location');
            if (location) {
                return res.redirect(e.status, location);
            }
        }
        console.error('SSR render error:', e);
        // Graceful fallback: serve the SPA shell — client JS takes over
        res.status(200).set('Content-Type', 'text/html').end(template);
    }
});

app.listen(PORT, () => {
    console.log(`SSR server listening on :${PORT}`);
});
