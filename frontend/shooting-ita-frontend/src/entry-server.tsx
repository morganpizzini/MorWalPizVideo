import { renderToString } from 'react-dom/server';
import { createStaticHandler, createStaticRouter, StaticRouterProvider } from 'react-router';
import { GoogleReCaptchaProvider } from 'react19-google-recaptcha-v3';
import { setRequestCredentialsMode } from '@morwalpizvideo/services';
import { routes } from './routes/config';

setRequestCredentialsMode('omit');

export async function render(request: Request): Promise<{ html: string; head: string }> {
    const handler = createStaticHandler(routes);
    const context = await handler.query(request);
    if (context instanceof Response) throw context;
    const router = createStaticRouter(handler.dataRoutes, context);
    const html = renderToString(<GoogleReCaptchaProvider reCaptchaKey={import.meta.env.VITE_SITE_KEY ?? ''}><StaticRouterProvider router={router} context={context} /></GoogleReCaptchaProvider>);
    const isFaq = new URL(request.url).pathname === '/faq';
    const head = isFaq
        ? '<title>Domande frequenti | Shooting ITA</title><meta name="description" content="Risposte curate dai canali Shooting ITA."><link rel="canonical" href="/faq">'
        : '<title>Shooting ITA</title>';
    return { html, head };
}