import '@testing-library/jest-dom';
import { URLSearchParams as NodeURLSearchParams } from 'node:url';

Object.defineProperty(globalThis, 'URLSearchParams', {
  configurable: true,
  value: NodeURLSearchParams,
});
Object.defineProperty(window, 'URLSearchParams', {
  configurable: true,
  value: NodeURLSearchParams,
});

Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: (query: string): MediaQueryList => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: () => undefined,
    removeListener: () => undefined,
    addEventListener: () => undefined,
    removeEventListener: () => undefined,
    dispatchEvent: () => false,
  }),
});

Object.defineProperty(window, 'scrollTo', {
  configurable: true,
  value: () => undefined,
});
