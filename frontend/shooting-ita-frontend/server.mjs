globalThis.window = globalThis.window ?? { location: { href: '', pathname: '/', hostname: 'localhost', protocol: 'https:', host: 'localhost' }, localStorage: { getItem: () => null, setItem: () => {}, removeItem: () => {} } };
globalThis.document = globalThis.document ?? { getElementById: () => null, createElement: () => ({ style: {}, setAttribute: () => {}, appendChild: () => {} }), head: { appendChild: () => {} }, body: { appendChild: () => {} } };

import express from 'express';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const { render } = await import('./dist-ssr/entry-server.js');
const app = express();
const port = process.env.PORT ?? 3000;
const distDir = resolve('dist');
const template = readFileSync(resolve(distDir, 'index.html'), 'utf8');
app.use(express.static(distDir, { index: false }));
app.use(async (request, response) => {
    try {
        const protocol = request.headers['x-forwarded-proto'] ?? request.protocol ?? 'https';
        const host = request.headers['x-forwarded-host'] ?? request.headers.host ?? 'localhost';
        const result = await render(new Request(`${protocol}://${host}${request.originalUrl}`));
        response.status(200).type('html').send(template.replace('<!--ssr-head-->', result.head).replace('<div id="root"></div>', `<div id="root">${result.html}</div>`));
    } catch (error) {
        console.error('Shooting ITA SSR render error:', error);
        response.status(200).type('html').send(template);
    }
});
app.listen(port, () => console.log(`Shooting ITA SSR server listening on :${port}`));