import React from 'react'
import ReactDOM, { hydrateRoot } from 'react-dom/client'
import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import {
  GoogleReCaptchaProvider
} from 'react19-google-recaptcha-v3';
import 'bootstrap/dist/css/bootstrap.min.css';
import './index.css'

import { routes } from './routes/config';

const router = createBrowserRouter(routes);

const app = <React.StrictMode>
  <GoogleReCaptchaProvider reCaptchaKey={import.meta.env.VITE_SITE_KEY}>
    <RouterProvider router={router} />
  </GoogleReCaptchaProvider>
</React.StrictMode>;
const rootElement = document.getElementById('root')!;
if (rootElement.innerHTML.trim()) hydrateRoot(rootElement, app);
else ReactDOM.createRoot(rootElement).render(app);
