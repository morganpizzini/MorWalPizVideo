import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react-swc';
import { fileURLToPath, URL } from 'node:url';
import { env } from 'process';

const target = env.ASPNETCORE_HTTPS_PORT
    ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}`
    : env.ASPNETCORE_URLS
        ? env.ASPNETCORE_URLS.split(';')[0]
        : 'https://localhost:7140';

// https://vite.dev/config/
export default defineConfig({
    plugins: [react()],
    resolve: {
        alias: {
            '@morwalpiz/layout': fileURLToPath(new URL('../fe-packages/layout/dist/index.js', import.meta.url)),
            '@morwalpizvideo/models': fileURLToPath(new URL('../fe-packages/models/dist/index.js', import.meta.url)),
            '@morwalpizvideo/services': fileURLToPath(new URL('../fe-packages/services/dist/index.js', import.meta.url)),
        },
        dedupe: ['react', 'react-dom', 'react-router'],
    },
    css: {
        preprocessorOptions: {
            scss: {
                api: 'legacy',
                includePaths: [fileURLToPath(new URL('../fe-packages/layout/dist/styles', import.meta.url))],
                importer: [
                    (url: string) => {
                        if (url === '@morwalpiz/layout/styles') {
                            return {
                                file: fileURLToPath(new URL('../fe-packages/layout/dist/styles/index.css', import.meta.url)),
                            };
                        }
                        return null;
                    },
                ],
            },
        },
    },
    server: {
        port: Number(process.env.PORT) || 5174,
        proxy: {
            '^/api': {
                target,
                secure: false,
            },
        }
    }
});
