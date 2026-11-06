import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { VitePWA } from "vite-plugin-pwa";

export default defineConfig({
  server: {
    port: Number(process.env.PORT) || 5178,
    strictPort: Boolean(process.env.PORT),
    proxy: {
      "/api": {
        target: process.env.VITE_FREEZER_API_PROXY_TARGET,
        changeOrigin: true,
        secure: false,
      },
      "/health": {
        target: process.env.VITE_FREEZER_API_PROXY_TARGET,
        changeOrigin: true,
        secure: false,
      },
    },
  },
  plugins: [
    react(),
    VitePWA({
      registerType: "autoUpdate",
      includeAssets: ["favicon.svg"],
      manifest: {
        name: "FreezerAnalysis",
        short_name: "Freezer",
        description: "Preparazione gara, analisi e appunti di campo IPSC.",
        theme_color: "#172720",
        background_color: "#f5f2e9",
        display: "standalone",
        start_url: "/",
        icons: [
          {
            src: "/favicon.svg",
            sizes: "any",
            type: "image/svg+xml",
            purpose: "any",
          },
        ],
      },
      workbox: {
        globPatterns: ["**/*.{js,css,html,svg,woff2}"],
        navigateFallback: "index.html",
      },
    }),
  ],
});
