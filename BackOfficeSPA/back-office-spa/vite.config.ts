/// <reference types="vitest" />
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react-swc';
import { fileURLToPath, URL } from 'node:url';
import { env } from 'process';

const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
    env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'https://localhost:7140';

// https://vite.dev/config/
export default defineConfig({
    plugins: [react()],
    server: {
        port: process.env.PORT || 5173,
        proxy: {
            '^/api': {
                target,
                secure: false
            }
        },
    },
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
      '@models': fileURLToPath(new URL('./src/models', import.meta.url)),
      '@components': fileURLToPath(new URL('./src/components', import.meta.url)),
      '@config': fileURLToPath(new URL('./src/config', import.meta.url)),
      '@services': fileURLToPath(new URL('./src/services', import.meta.url))
    },
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
  },
});
