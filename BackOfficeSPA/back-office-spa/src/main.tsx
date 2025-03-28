// filepath: c:\Users\morga\OneDrive\Desktop\MorWalPizVideo\BackOfficeSPA\back-office-spa\src\main.tsx
import 'bootstrap/dist/css/bootstrap.min.css';
import { RouterProvider } from 'react-router';
import { createRoot } from 'react-dom/client';
import router from './router';
import { ToastProvider } from './components/ToastNotification';

createRoot(document.getElementById('root')).render(
  <ToastProvider>
    <RouterProvider router={router} />
  </ToastProvider>
);
