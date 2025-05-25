import React from 'react'
import ReactDOM from 'react-dom/client'
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Layout from './components/Layout.tsx';
import HomePage from './pages/HomePage.tsx';
import RequestVideoPage from './pages/RequestVideoPage.tsx';
import RequestAdPage from './pages/RequestAdPage.tsx';
import {
  GoogleReCaptchaProvider
} from 'react19-google-recaptcha-v3';
import 'bootstrap/dist/css/bootstrap.min.css'; // Import Bootstrap CSS
import './index.css' // Keep your global styles if any

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <BrowserRouter>
      <GoogleReCaptchaProvider reCaptchaKey={import.meta.env.VITE_SITE_KEY}>
        <Routes>
          <Route path="/" element={<Layout />}> {/* Use Layout for all routes */}
            <Route index element={<HomePage />} />
            <Route path="request-video" element={<RequestVideoPage />} />
            <Route path="request-ad" element={<RequestAdPage />} />
            {/* Add other routes here */}
            {/* Example: <Route path="about" element={<AboutPage />} /> */}
            <Route path="*" element={<div>404 Not Found</div>} /> {/* Catch-all route */}
          </Route>
        </Routes>
      </GoogleReCaptchaProvider>
    </BrowserRouter>
  </React.StrictMode>,
)
