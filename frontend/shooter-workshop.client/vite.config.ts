import { fileURLToPath, URL } from "node:url";
import { createHash } from "node:crypto";
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react-swc";
import { getWorkshopViteServerSettings } from "./viteServerConfig";

const serverSettings = getWorkshopViteServerSettings(process.env);

export default defineConfig({
  plugins: [
    react(),
    {
      name: "workshop-build-assets",
      generateBundle(_, bundle) {
        const assets = Object.keys(bundle)
          .filter(fileName => /\.(?:js|css)$/.test(fileName))
          .sort()
          .map(fileName => `/${fileName}`);
        const hash = createHash("sha256");
        for (const fileName of assets) {
          const emitted = bundle[fileName.slice(1)];
          const source = emitted && "source" in emitted ? emitted.source : "";
          hash.update(fileName);
          hash.update(typeof source === "string" ? source : Buffer.from(source));
        }
        this.emitFile({
          type: "asset",
          fileName: "asset-manifest.json",
          source: JSON.stringify({
            version: hash.digest("hex").slice(0, 16),
            assets,
          }),
        });
      },
    },
  ],
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
