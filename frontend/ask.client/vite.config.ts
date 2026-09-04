import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react-swc';

const target = process.env.ASPNETCORE_HTTPS_PORT
  ? `https://localhost:${process.env.ASPNETCORE_HTTPS_PORT}`
  : process.env.ASPNETCORE_URLS?.split(';')[0] ?? 'https://localhost:7140';

export default defineConfig(({ mode }) => ({
  plugins: [react()],
  resolve: { alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) } },
  server: { port: Number(process.env.PORT) || 5180, proxy: { '^/api': { target, secure: false } } },
  ssr: { noExternal: true },
  build: { outDir: mode === 'ssr' ? 'dist-ssr' : 'dist' },
}));
