import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react-swc';
import path from 'path';
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
            '@morwalpiz/layout': path.resolve(__dirname, '../fe-packages/layout/dist/index.js'),
            '@morwalpizvideo/models': path.resolve(__dirname, '../fe-packages/models/dist/index.js'),
            '@morwalpizvideo/services': path.resolve(__dirname, '../fe-packages/services/dist/index.js'),
        },
        dedupe: ['react', 'react-dom', 'react-router'],
    },
    css: {
        preprocessorOptions: {
            scss: {
                api: 'legacy',
                includePaths: [path.resolve(__dirname, '../fe-packages/layout/dist/styles')],
                importer: [
                    (url: string) => {
                        if (url === '@morwalpiz/layout/styles') {
                            return {
                                file: path.resolve(__dirname, '../fe-packages/layout/dist/styles/index.css'),
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
