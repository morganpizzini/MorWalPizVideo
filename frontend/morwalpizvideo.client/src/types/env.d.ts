/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL?: string;
  readonly VITE_API_URL?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}

// Runtime environment variables injected by Docker entrypoint
interface RuntimeEnv {
  VITE_API_BASE_URL?: string;
  API_BASE_URL?: string;
  REACT_APP_API_URL?: string;
}

declare global {
  interface Window {
    ENV?: RuntimeEnv;
  }
}

export {};