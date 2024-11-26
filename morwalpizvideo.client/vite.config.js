import { fileURLToPath, URL } from 'node:url';

import { defineConfig } from 'vite';
import { VitePWA } from "vite-plugin-pwa";
import plugin from '@vitejs/plugin-react';
import fs from 'fs';
import path from 'path';
import child_process from 'child_process';
import { env } from 'process';

const baseFolder =
    env.APPDATA !== undefined && env.APPDATA !== ''
        ? `${env.APPDATA}/ASP.NET/https`
        : `${env.HOME}/.aspnet/https`;

const certificateName = "morwalpizvideo.client";
const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

// you can copy the base structure of manifest object.
const manifestForPlugIn = {
    registerType: 'prompt',
    includeAssests: ['favicon.ico', "apple-touch-icon.png", "maskable_icon.png"],
    manifest: {
        name: "MorWalPiz",
        short_name: "MWP",
        description: "MorWalPiz - Morgan Pizzini",
        icons: [{
            src: '/android-chrome-192x192.png',
            sizes: '192x192',
            type: 'image/png',
            purpose: 'favicon'
        },
        {
            src: '/android-chrome-512x512.png',
            sizes: '512x512',
            type: 'image/png',
            purpose: 'favicon'
        },
        {
            src: '/apple-touch-icon.png',
            sizes: '180x180',
            type: 'image/png',
            purpose: 'apple touch icon',
        },
        {
            src: '/maskable_icon.png',
            sizes: '512x512',
            type: 'image/png',
            purpose: 'any maskable',
        }
        ],
        theme_color: '#cacaca',
        background_color: '#7c7c7d',
        display: "standalone",
        scope: '/',
        start_url: "/",
        orientation: 'portrait'
    }
}


if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
    if (0 !== child_process.spawnSync('dotnet', [
        'dev-certs',
        'https',
        '--export-path',
        certFilePath,
        '--format',
        'Pem',
        '--no-password',
    ], { stdio: 'inherit', }).status) {
        throw new Error("Could not create certificate.");
    }
}

const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
    env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'https://localhost:7140';

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [plugin(), VitePWA(manifestForPlugIn)],
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url)),
            '~bootstrap': fileURLToPath(new URL('./node_modules/bootstrap', import.meta.url)),
            '@layouts': fileURLToPath(new URL('./src/layouts', import.meta.url)),
            '@utils': fileURLToPath(new URL('./src/utils', import.meta.url)),
            '@services': fileURLToPath(new URL('./src/services', import.meta.url))
        }
    },
    //css: {
    //    preprocessorOptions: {
    //        scss: {
    //            //api: 'modern-compiler', // or "modern"
    //            silenceDeprecations: ["legacy-js-api"],
    //        }
    //    }
    //},
    server: {
        proxy: {
            '^/api': {
                target,
                secure: false
            }
        },
        port: 5173,
        https: {
            key: fs.readFileSync(keyFilePath),
            cert: fs.readFileSync(certFilePath),
        }
    }
})
