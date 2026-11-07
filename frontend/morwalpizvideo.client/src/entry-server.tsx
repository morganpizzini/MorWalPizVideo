import { renderToString } from 'react-dom/server';
import { createStaticHandler, createStaticRouter, StaticRouterProvider } from 'react-router';
import { HelmetProvider } from 'react-helmet-async';
import { GoogleReCaptchaProvider } from 'react-google-recaptcha-v3';
import { routes } from './routes/config';
import BuyMeWidget from './utils/buy-me-widget';

interface HelmetData {
  title: { toString(): string };
  meta: { toString(): string };
  link: { toString(): string };
}

function extractHoistedHead(html: string): { html: string; head: string } {
  const headTags =
    /<title\b[^>]*>[\s\S]*?<\/title>|<meta\b[^>]*>|<link\b[^>]*\brel="(?:canonical|image_src)"[^>]*>/gi;
  const head = html.match(headTags)?.join('') ?? '';
  return { html: html.replace(headTags, ''), head };
}

export async function render(
  request: Request
): Promise<{ html: string; head: string; status: number }> {
  const handler = createStaticHandler(routes);
  const context = await handler.query(request);

  if (context instanceof Response) {
    throw context;
  }

  const router = createStaticRouter(handler.dataRoutes, context);
  // react-helmet-async expects HelmetServerState; we only read a few subfields, so cast.
  const helmetContext: { helmet?: HelmetData } = {};

  const html = renderToString(
    <HelmetProvider
      context={helmetContext as { helmet?: import('react-helmet-async').HelmetServerState | null }}
    >
      <GoogleReCaptchaProvider reCaptchaKey={import.meta.env.VITE_SITE_KEY ?? ''}>
        <StaticRouterProvider router={router} context={context} />
      </GoogleReCaptchaProvider>
      <BuyMeWidget />
    </HelmetProvider>
  );

  const { helmet } = helmetContext;
  const collectedHead = helmet
    ? `${helmet.title.toString()}${helmet.meta.toString()}${helmet.link.toString()}`
    : '';
  const extracted = extractHoistedHead(html);

  return {
    html: extracted.html,
    head: collectedHead || extracted.head,
    status: context.statusCode ?? 200,
  };
}
