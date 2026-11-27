import '@morwalpizvideo/services/account-mail-link';
import { StrictMode } from 'react';
import { createRoot, hydrateRoot } from 'react-dom/client';
import { AppearanceProvider, initializeAppearance } from '@morwalpiz/layout';
import '@morwalpiz/layout/theme';
import './main.scss';

import { routes } from './routes/config';
import { createBrowserRouter } from 'react-router';
import { RouterProvider } from 'react-router/dom';
import { HelmetProvider } from 'react-helmet-async';
import ConsentBanner from './analytics/ConsentBanner';
import BuyMeWidget from '@utils/buy-me-widget';
import { GoogleReCaptchaProvider } from 'react-google-recaptcha-v3';
import { registerSW } from 'virtual:pwa-register';

const router = createBrowserRouter(routes);
const accountMailSurface = ['/account/reset-password', '/account/confirm-email'].includes(window.location.pathname);
initializeAppearance();

// Configurazione del registro SW con callback
const updateSW = registerSW({
  onNeedRefresh() {
    const userConsent = window.confirm(
      "Una nuova versione � disponibile. Vuoi aggiornare l'applicazione?"
    );
    if (userConsent) {
      updateSW();
    }
  },
  onOfflineReady() {
    //alert("L'app � pronta per funzionare offline.");
  },
});

const app = (
  <StrictMode>
    <AppearanceProvider>
      <HelmetProvider>
        {accountMailSurface ? <RouterProvider router={router} /> : <GoogleReCaptchaProvider reCaptchaKey={import.meta.env.VITE_SITE_KEY}>
          <RouterProvider router={router} />
        </GoogleReCaptchaProvider>}
      </HelmetProvider>
      {!accountMailSurface && <BuyMeWidget />}
      <ConsentBanner />
    </AppearanceProvider>
  </StrictMode>
);

const rootElement = document.getElementById('root')!;
const hasSSRContent = rootElement.innerHTML.trim().length > 0;

if (hasSSRContent && !accountMailSurface) {
  hydrateRoot(rootElement, app);
} else {
  createRoot(rootElement).render(app);
}
