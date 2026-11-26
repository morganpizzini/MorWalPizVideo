import { fileURLToPath, URL } from "node:url";
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react-swc";
import { getWorkshopViteServerSettings } from "./viteServerConfig";

const serverSettings = getWorkshopViteServerSettings(process.env);

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: { "@": fileURLToPath(new URL("./src", import.meta.url)) },
  },
  build: {
    rollupOptions: {
      output: {
        manualChunks: {
          react: ["react", "react-dom", "react-router"],
          qrcode: ["qrcode"],
        },
      },
    },
  },
  server: {
    host: "0.0.0.0",
    port: serverSettings.port,
    strictPort: true,
    // Keep account cookies and CSRF requests same-origin in the browser.
    proxy: { "^/api": { target: serverSettings.apiTarget, secure: false } },
  },
});
