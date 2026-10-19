import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vitest/config';

export default defineConfig({
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
      '@layouts': fileURLToPath(new URL('./src/layouts', import.meta.url)),
      '@utils': fileURLToPath(new URL('./src/utils', import.meta.url)),
      '@services': fileURLToPath(new URL('./src/services', import.meta.url)),
    },
  },
  test: { environment: 'node', include: ['src/**/*.test.{ts,tsx}'] },
});
