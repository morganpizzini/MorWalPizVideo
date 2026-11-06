import '@testing-library/jest-dom/vitest';
import { transferableAbortController } from 'node:util';

const jsdomWindow = (globalThis as typeof globalThis & {
  jsdom?: { window: Window };
}).jsdom?.window;

if (jsdomWindow) {
  Object.defineProperty(globalThis, 'localStorage', {
    configurable: true,
    value: jsdomWindow.localStorage
  });
  Object.defineProperty(globalThis, 'sessionStorage', {
    configurable: true,
    value: jsdomWindow.sessionStorage
  });
}

const nodeAbortController = transferableAbortController();
Object.defineProperty(globalThis, 'AbortController', {
  configurable: true,
  value: nodeAbortController.constructor
});
Object.defineProperty(globalThis, 'AbortSignal', {
  configurable: true,
  value: nodeAbortController.signal.constructor
});
