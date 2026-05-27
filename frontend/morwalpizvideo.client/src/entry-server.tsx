import { renderToString } from 'react-dom/server';
import { createStaticHandler, createStaticRouter, StaticRouterProvider } from 'react-router';
import { HelmetProvider } from 'react-helmet-async';
import { GoogleReCaptchaProvider } from 'react-google-recaptcha-v3';
import { setRequestCredentialsMode } from '@morwalpizvideo/services';
import { routes } from './routes/config';
import BuyMeWidget from './utils/buy-me-widget';

setRequestCredentialsMode('omit');

interface HelmetData {
    title: { toString(): string };
    meta: { toString(): string };
    link: { toString(): string };
}

export async function render(request: Request): Promise<{ html: string; head: string }> {
    const handler = createStaticHandler(routes);
    const context = await handler.query(request);

    if (context instanceof Response) {
        throw context;
    }

    const router = createStaticRouter(handler.dataRoutes, context);
    const helmetContext: { helmet?: HelmetData } = {};

    const html = renderToString(
        <HelmetProvider context={helmetContext}>
            <GoogleReCaptchaProvider reCaptchaKey={import.meta.env.VITE_SITE_KEY ?? ''}>
                <StaticRouterProvider router={router} context={context} />
            </GoogleReCaptchaProvider>
            <BuyMeWidget />
        </HelmetProvider>
    );

    const { helmet } = helmetContext;
    const head = helmet
        ? `${helmet.title.toString()}${helmet.meta.toString()}${helmet.link.toString()}`
        : '';

    return { html, head };
}
