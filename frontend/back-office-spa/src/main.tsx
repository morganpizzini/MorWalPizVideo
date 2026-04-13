// filepath: c:\Users\morga\OneDrive\Desktop\MorWalPizVideo\BackOfficeSPA\back-office-spa\src\main.tsx
import 'bootstrap/dist/css/bootstrap.min.css';
import { createRoot } from 'react-dom/client';
import { ToastProvider } from './components/ToastNotification';


// Load runtime environment configuration (injected by Docker entrypoint)
// This dynamically loads /env-config.js to avoid Vite trying to bundle it
const loadEnvConfig = () => {
  return new Promise<void>((resolve, reject) => {
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
};

// Load env config then mount the React app
async function bootstrap() {
  await loadEnvConfig();
  const [{ RouterProvider }, { default: router }] = await Promise.all([
    import('react-router'),
    import('./router'),
  ]);

  createRoot(document.getElementById('root')!).render(
    <ToastProvider>
      <RouterProvider router={router} />
    </ToastProvider>
  );
}

bootstrap();