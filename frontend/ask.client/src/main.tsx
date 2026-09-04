import { StrictMode } from 'react';
import { createRoot, hydrateRoot } from 'react-dom/client';
import { createBrowserRouter } from 'react-router';
import { RouterProvider } from 'react-router/dom';
import { HelmetProvider } from 'react-helmet-async';
import { GoogleReCaptchaProvider } from 'react-google-recaptcha-v3';
import { setRequestCredentialsMode } from '@morwalpizvideo/services';
import { routes } from './routes';
import './styles.css';

setRequestCredentialsMode('omit');
const app = <StrictMode><HelmetProvider><GoogleReCaptchaProvider reCaptchaKey={import.meta.env.VITE_SITE_KEY ?? ''}><RouterProvider router={createBrowserRouter(routes)} /></GoogleReCaptchaProvider></HelmetProvider></StrictMode>;
const root = document.getElementById('root')!;
root.innerHTML.trim() ? hydrateRoot(root, app) : createRoot(root).render(app);
