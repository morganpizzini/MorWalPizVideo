import { StrictMode } from 'react'
import { createRoot, hydrateRoot } from 'react-dom/client'
import './main.scss'
import { setRequestCredentialsMode } from '@morwalpizvideo/services'

// Configure API service to omit credentials for public client BEFORE any other imports
// This prevents CORS errors when the ServerAPI CORS policy doesn't allow credentials
// MUST be called before importing any modules that might make API calls
setRequestCredentialsMode('omit');

import { routes } from './routes/config';
import { createBrowserRouter } from "react-router";
import { RouterProvider } from "react-router/dom";
import { HelmetProvider } from 'react-helmet-async';
import ReactGA from 'react-ga4';
import BuyMeWidget from "@utils/buy-me-widget";
import { GoogleReCaptchaProvider } from 'react-google-recaptcha-v3';
import { registerSW } from 'virtual:pwa-register';

const router = createBrowserRouter(routes);

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

const gaTrackingId = "G-ST9GQYL925";
if (window.location.hostname != "localhost") {
    ReactGA.initialize(gaTrackingId)
}

const app = (
    <StrictMode>
        <HelmetProvider>
            <GoogleReCaptchaProvider reCaptchaKey={import.meta.env.VITE_SITE_KEY}>
                <RouterProvider router={router} />
            </GoogleReCaptchaProvider>
        </HelmetProvider>
        <BuyMeWidget />
    </StrictMode>
);

const rootElement = document.getElementById('root')!;
const hasSSRContent = rootElement.innerHTML.trim().length > 0;

if (hasSSRContent) {
    hydrateRoot(rootElement, app);
} else {
    createRoot(rootElement).render(app);
}
