import { renderToString } from 'react-dom/server';
import { createStaticHandler, createStaticRouter, StaticRouterProvider } from 'react-router';
import { HelmetProvider } from 'react-helmet-async';
import { GoogleReCaptchaProvider } from 'react-google-recaptcha-v3';
import { setRequestCredentialsMode } from '@morwalpizvideo/services';
import { routes } from './routes';
import './styles.css';

setRequestCredentialsMode('omit');
export async function render(request: Request): Promise<{ html: string; head: string }> {
  const handler = createStaticHandler(routes);
  const context = await handler.query(request);
  if (context instanceof Response) throw context;
  const router = createStaticRouter(handler.dataRoutes, context);
  const helmetContext: { helmet?: import('react-helmet-async').HelmetServerState | null } = {};
  const html = renderToString(<HelmetProvider context={helmetContext as { helmet?: import('react-helmet-async').HelmetServerState | null }}><GoogleReCaptchaProvider reCaptchaKey={import.meta.env.VITE_SITE_KEY ?? ''}><StaticRouterProvider router={router} context={context} /></GoogleReCaptchaProvider></HelmetProvider>);
  const { helmet } = helmetContext;
  return { html, head: helmet ? `${helmet.title.toString()}${helmet.meta.toString()}${helmet.link.toString()}` : '' };
}
