import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { createBrowserRouter, RouterProvider } from 'react-router';
import { HelmetProvider } from 'react-helmet-async';
import { GoogleReCaptchaProvider } from 'react-google-recaptcha-v3';
import './main.scss';

// Routes
import Root from './routes/root';
import Index from './routes/index';
import Login from './routes/login';
import TermsAndConditions from './routes/terms';
import PrivacyPolicy from './routes/privacy';
import CookiePolicy from './routes/cookie-policy';
import Catalog, { loader as catalogLoader } from './routes/catalog';
import ProductDetail, { loader as productDetailLoader } from './routes/product-detail';
import Cart, { loader as cartLoader } from './routes/cart';
import CheckoutSuccess from './routes/checkout-success';
import ErrorPage from './routes/error-page';

const router = createBrowserRouter([
  {
    path: '/',
    element: <Root />,
    errorElement: <ErrorPage />,
    children: [
      {
        index: true,
        element: <Index />,
      },
      {
        path: 'login',
        element: <Login />,
      },
      {
        path: 'terms',
        element: <TermsAndConditions />,
      },
      {
        path: 'privacy',
        element: <PrivacyPolicy />,
      },
      {
        path: 'cookie-policy',
        element: <CookiePolicy />,
      },
      {
        path: 'catalog',
        loader: catalogLoader,
        element: <Catalog />,
      },
      {
        path: 'products/:productId',
        loader: productDetailLoader,
        element: <ProductDetail />,
      },
      {
        path: 'cart',
        loader: cartLoader,
        element: <Cart />,
      },
      {
        path: 'checkout-success',
        element: <CheckoutSuccess />,
      },
    ],
  },
]);

const recaptchaKey =
  (typeof window !== 'undefined' && (window as any).ENV?.VITE_RECAPTCHA_KEY) ||
  import.meta.env.VITE_RECAPTCHA_KEY ||
  '';

// Load runtime environment configuration (injected by Docker entrypoint)
// This dynamically loads /env-config.js to avoid Vite trying to bundle it
function loadEnvConfig(): Promise<void> {
  return new Promise((resolve) => {
    const script = document.createElement('script');
    script.src = '/env-config.js';
    script.onload = () => resolve();
    script.onerror = () => {
      // In development, env-config.js may not exist - that's OK
      console.warn('env-config.js not found - using build-time environment variables');
      resolve();
    };
    document.head.appendChild(script);
  });
}

async function bootstrap() {
  await loadEnvConfig();

  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <HelmetProvider>
        <GoogleReCaptchaProvider reCaptchaKey={recaptchaKey}>
          <RouterProvider router={router} />
        </GoogleReCaptchaProvider>
      </HelmetProvider>
    </StrictMode>
  );
}

bootstrap();